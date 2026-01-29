using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using ClosedXML.Excel;
using System.Globalization;
using System.Linq;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 報表生成服務
/// 負責 Excel 報表的生成邏輯
/// </summary>
public class ReportService : IReportService
{
    private const int MAX_EXPORT_ROWS = 5000;

    private readonly ICallRecordRepository _callRecordRepository;
    private readonly IInquirySystemRepository _inquirySystemRepository;

    public ReportService(
        ICallRecordRepository callRecordRepository,
        IInquirySystemRepository inquirySystemRepository)
    {
        _callRecordRepository = callRecordRepository;
        _inquirySystemRepository = inquirySystemRepository;
    }

    /// <summary>
    /// 生成 Excel 報表（包含篩選摘要、明細、統計）
    /// </summary>
    public async Task<byte[]> GenerateExcelReportAsync(
        ExcelReportRequest filter,
        CancellationToken cancellationToken = default)
    {
        // 1. 轉換篩選條件
        var criteria = ConvertFilterToCriteria(filter);

        // 2. 查詢資料
        var records = await _callRecordRepository.SearchAsync(criteria, cancellationToken);

        // 3. 驗證限制
        if (records.Count > MAX_EXPORT_ROWS)
        {
            throw new InvalidOperationException(
                $"查詢結果超過 {MAX_EXPORT_ROWS} 筆限制。請使用更具體的篩選條件。");
        }

        // 4. 轉換為 DTO
        var recordDtos = records.Select(r => MapToDto(r)).ToList();

        // 5. 建立工作簿
        using (var workbook = new XLWorkbook())
        {
            // 6. 建立三個工作表
            CreateSummarySheet(workbook, filter);
            CreateDetailSheet(workbook, recordDtos);
            CreateStatisticsSheet(workbook, recordDtos);

            // 7. 回傳 Excel 位元組
            using (var memoryStream = new MemoryStream())
            {
                workbook.SaveAs(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }

    /// <summary>
    /// 建立篩選摘要工作表
    /// </summary>
    private void CreateSummarySheet(XLWorkbook workbook, ExcelReportRequest filter)
    {
        var worksheet = workbook.Worksheets.Add("篩選摘要");

        // 標題
        worksheet.Cell(1, 1).Value = "篩選摘要";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        var row = 3;

        // 篩選條件
        if (!string.IsNullOrEmpty(filter.Keyword))
        {
            worksheet.Cell(row, 1).Value = "關鍵字";
            worksheet.Cell(row, 2).Value = filter.Keyword;
            row++;
        }

        if (filter.InquirySystemId.HasValue)
        {
            worksheet.Cell(row, 1).Value = "詢問系統";
            worksheet.Cell(row, 2).Value = filter.InquirySystemId;
            row++;
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            worksheet.Cell(row, 1).Value = "狀態";
            worksheet.Cell(row, 2).Value = filter.Status;
            row++;
        }

        if (!string.IsNullOrEmpty(filter.UrgencyLevel))
        {
            worksheet.Cell(row, 1).Value = "緊急程度";
            worksheet.Cell(row, 2).Value = filter.UrgencyLevel;
            row++;
        }

        if (filter.StartDate.HasValue)
        {
            worksheet.Cell(row, 1).Value = "開始日期";
            worksheet.Cell(row, 2).Value = filter.StartDate.Value.ToString("yyyy/MM/dd");
            row++;
        }

        if (filter.EndDate.HasValue)
        {
            worksheet.Cell(row, 1).Value = "結束日期";
            worksheet.Cell(row, 2).Value = filter.EndDate.Value.ToString("yyyy/MM/dd");
            row++;
        }

        if (!string.IsNullOrEmpty(filter.ReportMonth))
        {
            worksheet.Cell(row, 1).Value = "報表月份";
            worksheet.Cell(row, 2).Value = filter.ReportMonth;
            row++;
        }

        // 調整欄寬
        worksheet.Column(1).Width = 15;
        worksheet.Column(2).Width = 30;
    }

    /// <summary>
    /// 建立明細資料工作表
    /// </summary>
    private void CreateDetailSheet(XLWorkbook workbook, List<CallRecordDto> records)
    {
        var worksheet = workbook.Worksheets.Add("明細資料");

        // 標題列
        var headers = new[] 
        { 
            "紀錄 ID", 
            "主旨", 
            "內容", 
            "詢問系統", 
            "狀態", 
            "緊急程度",
            "聯絡人", 
            "聯絡電話", 
            "建立時間" 
        };

        for (int col = 0; col < headers.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // 寫入資料
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var row = i + 2;

            worksheet.Cell(row, 1).Value = record.Id;
            worksheet.Cell(row, 2).Value = record.Subject;
            worksheet.Cell(row, 3).Value = record.Content;
            worksheet.Cell(row, 4).Value = record.InquirySystem.Name;
            worksheet.Cell(row, 5).Value = GetStatusDisplayText(record.Status);
            worksheet.Cell(row, 6).Value = GetUrgencyLevelDisplayText(record.UrgencyLevel);
            worksheet.Cell(row, 7).Value = record.ContactName;
            worksheet.Cell(row, 8).Value = record.ContactPhone;
            
            // 格式化日期時間
            var dateCell = worksheet.Cell(row, 9);
            dateCell.Value = record.CreatedAt;
            dateCell.Style.DateFormat.Format = "yyyy/mm/dd hh:mm";
        }

        // 調整欄寬
        worksheet.Column(1).Width = 10;
        worksheet.Column(2).Width = 15;
        worksheet.Column(3).Width = 25;
        worksheet.Column(4).Width = 15;
        worksheet.Column(5).Width = 12;
        worksheet.Column(6).Width = 12;
        worksheet.Column(7).Width = 12;
        worksheet.Column(8).Width = 15;
        worksheet.Column(9).Width = 18;
    }

    /// <summary>
    /// 建立統計彙總工作表
    /// </summary>
    private void CreateStatisticsSheet(XLWorkbook workbook, List<CallRecordDto> records)
    {
        var worksheet = workbook.Worksheets.Add("統計彙總");

        var currentRow = 1;

        // ==========第一部分：按月份統計 ==========
        worksheet.Cell(currentRow, 1).Value = "按月份統計";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
        currentRow++;

        // 月份統計標題
        worksheet.Cell(currentRow, 1).Value = "月份";
        worksheet.Cell(currentRow, 2).Value = "筆數";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
        currentRow++;

        // 月份分組
        var monthlyGroups = records
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .ToList();

        foreach (var group in monthlyGroups)
        {
            worksheet.Cell(currentRow, 1).Value = 
                $"{group.Key.Year:D4}/{group.Key.Month:D2}";
            worksheet.Cell(currentRow, 2).Value = group.Count();
            currentRow++;
        }

        // 月份總計
        worksheet.Cell(currentRow, 1).Value = "合計";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 2).Value = records.Count;
        worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
        currentRow += 3;

        // ==========第二部分：按詢問系統統計 ==========
        worksheet.Cell(currentRow, 1).Value = "按詢問系統統計";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
        currentRow++;

        // 詢問系統統計標題
        worksheet.Cell(currentRow, 1).Value = "詢問系統";
        worksheet.Cell(currentRow, 2).Value = "筆數";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 2).Style.Font.Bold = true;
        currentRow++;

        // 詢問系統分組
        var systemGroups = records
            .GroupBy(r => r.InquirySystem.Name)
            .OrderBy(g => g.Key)
            .ToList();

        foreach (var group in systemGroups)
        {
            worksheet.Cell(currentRow, 1).Value = group.Key;
            worksheet.Cell(currentRow, 2).Value = group.Count();
            currentRow++;
        }

        // 詢問系統總計
        worksheet.Cell(currentRow, 1).Value = "合計";
        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
        worksheet.Cell(currentRow, 2).Value = records.Count;
        worksheet.Cell(currentRow, 2).Style.Font.Bold = true;

        // 調整欄寬
        worksheet.Column(1).Width = 20;
        worksheet.Column(2).Width = 10;
    }

    /// <summary>
    /// 將篩選條件轉換為搜尋條件
    /// </summary>
    private CallRecordSearchCriteria ConvertFilterToCriteria(ExcelReportRequest filter)
    {
        var criteria = new CallRecordSearchCriteria
        {
            Keyword = filter.Keyword,
            InquirySystemId = filter.InquirySystemId,
            StartDateUtc = filter.StartDate,
            EndDateUtc = filter.EndDate
        };

        // 處理狀態篩選
        if (!string.IsNullOrEmpty(filter.Status) && 
            Enum.TryParse<ProcessStatus>(filter.Status, true, out var status))
        {
            criteria = criteria with { Status = status };
        }

        // 處理緊急程度篩選
        if (!string.IsNullOrEmpty(filter.UrgencyLevel) && 
            Enum.TryParse<UrgencyLevel>(filter.UrgencyLevel, true, out var urgency))
        {
            criteria = criteria with { UrgencyLevel = urgency };
        }

        // 處理報表月份（如果有的話）
        if (!string.IsNullOrEmpty(filter.ReportMonth))
        {
            // 格式: yyyy/MM
            if (DateTime.TryParseExact(
                filter.ReportMonth + "/01",
                "yyyy/MM/dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var monthStart))
            {
                criteria = criteria with
                {
                    StartDateUtc = monthStart,
                    EndDateUtc = monthStart.AddMonths(1).AddSeconds(-1)
                };
            }
        }

        return criteria;
    }

    /// <summary>
    /// 將 CallRecord 實體轉換為 DTO
    /// </summary>
    private CallRecordDto MapToDto(CallRecord record)
    {
        return new CallRecordDto
        {
            Id = record.Id,
            Subject = record.Subject,
            Content = record.Content,
            Status = record.Status,
            UrgencyLevel = record.UrgencyLevel,
            ContactName = record.ContactName,
            ContactPhone = record.ContactPhone,
            FaqReference = record.FaqReference,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            CreatedByUserId = record.CreatedByUserId,
            LockedByUserId = record.LockedByUserId,
            LockedAt = record.LockedAt,
            InquirySystem = new InquirySystemDto
            {
                Id = record.InquirySystem.Id,
                Name = record.InquirySystem.Name,
                IsActive = record.InquirySystem.IsActive
            },
            Handlers = record.Handlers?.Select(h => new HandlerDto
            {
                Id = h.Id,
                Name = h.Name,
                LineUserId = h.LineUserId
            }).ToList() ?? new List<HandlerDto>()
        };
    }

    /// <summary>
    /// 取得狀態的顯示文字
    /// </summary>
    private string GetStatusDisplayText(ProcessStatus status)
    {
        return status switch
        {
            ProcessStatus.Pending => "待處理",
            ProcessStatus.InProgress => "處理中",
            ProcessStatus.Completed => "已完成",
            ProcessStatus.Closed => "已關閉",
            _ => "未知"
        };
    }

    /// <summary>
    /// 取得緊急程度的顯示文字
    /// </summary>
    private string GetUrgencyLevelDisplayText(UrgencyLevel level)
    {
        return level switch
        {
            UrgencyLevel.Low => "低",
            UrgencyLevel.Medium => "中",
            UrgencyLevel.High => "高",
            _ => "未知"
        };
    }
}

/// <summary>
/// 用於報表匯出的 CallRecord DTO
/// </summary>
public record CallRecordDto
{
    public required int Id { get; init; }
    public required string Subject { get; init; }
    public required string Content { get; init; }
    public required ProcessStatus Status { get; init; }
    public required UrgencyLevel UrgencyLevel { get; init; }
    public required string ContactName { get; init; }
    public required string ContactPhone { get; init; }
    public string? FaqReference { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required string CreatedByUserId { get; init; }
    public string? LockedByUserId { get; init; }
    public DateTime? LockedAt { get; init; }
    public required InquirySystemDto InquirySystem { get; init; }
    public required List<HandlerDto> Handlers { get; init; }
}
