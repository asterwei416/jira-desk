using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using ClosedXML.Excel;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 來電紀錄服務
/// </summary>
public class CallRecordService
{
    private const int DefaultLockTimeoutMinutes = 30;
    private const int ExportLimit = 5000;
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
    private readonly ICallRecordRepository _callRecordRepository;
    private readonly IInquirySystemRepository _inquirySystemRepository;
    private readonly IHandlerRepository _handlerRepository;
    private readonly IChangeHistoryRepository _changeHistoryRepository;

    public CallRecordService(
        ICallRecordRepository callRecordRepository,
        IInquirySystemRepository inquirySystemRepository,
        IHandlerRepository handlerRepository,
        IChangeHistoryRepository changeHistoryRepository)
    {
        _callRecordRepository = callRecordRepository;
        _inquirySystemRepository = inquirySystemRepository;
        _handlerRepository = handlerRepository;
        _changeHistoryRepository = changeHistoryRepository;
    }

    /// <summary>
    /// 建立新來電紀錄
    /// </summary>
    public async Task<CallRecordResponse> CreateAsync(
        CreateCallRecordRequest request, 
        string userId,
        CancellationToken cancellationToken = default)
    {
        // 驗證詢問系統是否存在
        var inquirySystem = await _inquirySystemRepository.GetByIdAsync(
            request.InquirySystemId, 
            cancellationToken);
        
        if (inquirySystem == null)
        {
            throw new InvalidOperationException("詢問系統不存在");
        }

        // 建立實體
        var callRecord = CallRecord.Create(
            request.Subject,
            request.Content,
            request.InquirySystemId,
            request.UrgencyLevel,
            request.ContactName,
            request.ContactPhone,
            userId);

        if (!string.IsNullOrWhiteSpace(request.FaqReference))
        {
            callRecord.FaqReference = request.FaqReference;
        }

        // 儲存
        var created = await _callRecordRepository.AddAsync(callRecord, cancellationToken);

        // 取得完整資料（含關聯）
        var result = await _callRecordRepository.GetByIdAsync(created.Id, cancellationToken);
        
        return MapToResponse(result!);
    }

    /// <summary>
    /// 取得來電紀錄詳情
    /// </summary>
    public async Task<CallRecordResponse?> GetByIdAsync(
        int id, 
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        
        return callRecord == null ? null : MapToResponse(callRecord);
    }

    /// <summary>
    /// 取得分頁來電紀錄列表
    /// </summary>
    public async Task<PagedResult<CallRecordListItemResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null,
        int? inquirySystemId = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _callRecordRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            searchKeyword,
            inquirySystemId,
            cancellationToken);

        var responseItems = items.Select(MapToListItemResponse).ToList();

        return new PagedResult<CallRecordListItemResponse>
        {
            Items = responseItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 更新來電紀錄
    /// </summary>
    public async Task<CallRecordResponse> UpdateAsync(
        int id,
        UpdateCallRecordRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        // 檢查編輯鎖定
        if (callRecord.IsLockedByOther(userId))
        {
            throw new InvalidOperationException($"此紀錄正被其他使用者編輯中（鎖定者: {callRecord.LockedByUserId}）");
        }

        // 更新資料
        var histories = BuildChangeHistories(callRecord, request, userId);
        callRecord.Update(
            request.Subject,
            request.Content,
            request.UrgencyLevel,
            request.ContactName,
            request.ContactPhone,
            request.FaqReference);

        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        if (histories.Count > 0)
        {
            await _changeHistoryRepository.AddRangeAsync(histories, cancellationToken);
        }

        var updated = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        return MapToResponse(updated!);
    }

    /// <summary>
    /// 更新處理狀態並記錄變更歷史
    /// </summary>
    public async Task<CallRecordResponse> UpdateStatusAsync(
        int id,
        ProcessStatus newStatus,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        var oldStatus = callRecord.Status;
        if (oldStatus != newStatus)
        {
            callRecord.UpdateStatus(newStatus, userId);
            await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

            var history = ChangeHistory.Create(
                callRecord.Id,
                "Status",
                oldStatus.ToString(),
                newStatus.ToString(),
                userId);

            await _changeHistoryRepository.AddRangeAsync(new[] { history }, cancellationToken);
        }

        var updated = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        return MapToResponse(updated!);
    }

    /// <summary>
    /// 取得變更歷史
    /// </summary>
    public async Task<ChangeHistoryResponse> GetChangeHistoryAsync(
        int callRecordId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(callRecordId, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        var histories = await _changeHistoryRepository.GetByCallRecordIdAsync(callRecordId, cancellationToken);
        return new ChangeHistoryResponse
        {
            CallRecordId = callRecordId,
            Changes = histories.Select(h => new ChangeHistoryItemDto
            {
                FieldName = h.FieldName,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                ChangedAt = h.ChangedAt,
                ChangedByUserId = h.ChangedByUserId
            }).ToList()
        };
    }

    /// <summary>
    /// 取得編輯鎖定狀態
    /// </summary>
    public async Task<CallRecordLockStatusResponse> GetLockStatusAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        var isLocked = IsLocked(callRecord, DefaultLockTimeoutMinutes);

        return new CallRecordLockStatusResponse
        {
            IsLocked = isLocked,
            LockedByUserId = isLocked ? callRecord.LockedByUserId : null,
            LockedAt = isLocked ? callRecord.LockedAt : null
        };
    }

    /// <summary>
    /// 取得編輯鎖定
    /// </summary>
    public async Task<CallRecordLockResponse> AcquireLockAsync(
        int id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        var acquired = callRecord.TryAcquireLock(userId, DefaultLockTimeoutMinutes);
        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        var isLocked = IsLocked(callRecord, DefaultLockTimeoutMinutes);

        return new CallRecordLockResponse
        {
            Acquired = acquired,
            IsLocked = isLocked,
            LockedByUserId = isLocked ? callRecord.LockedByUserId : null,
            LockedAt = isLocked ? callRecord.LockedAt : null
        };
    }

    /// <summary>
    /// 釋放編輯鎖定
    /// </summary>
    public async Task<CallRecordLockStatusResponse> ReleaseLockAsync(
        int id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        callRecord.ReleaseLock(userId);
        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        var isLocked = IsLocked(callRecord, DefaultLockTimeoutMinutes);

        return new CallRecordLockStatusResponse
        {
            IsLocked = isLocked,
            LockedByUserId = isLocked ? callRecord.LockedByUserId : null,
            LockedAt = isLocked ? callRecord.LockedAt : null
        };
    }

    /// <summary>
    /// 刪除來電紀錄
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        await _callRecordRepository.DeleteAsync(callRecord, cancellationToken);
    }

    /// <summary>
    /// 取得所有啟用的詢問系統
    /// </summary>
    public async Task<List<InquirySystemDto>> GetActiveInquirySystemsAsync(
        CancellationToken cancellationToken = default)
    {
        var systems = await _inquirySystemRepository.GetActiveSystemsAsync(cancellationToken);
        
        return systems.Select(s => new InquirySystemDto
        {
            Id = s.Id,
            Name = s.Name,
            IsActive = s.IsActive
        }).ToList();
    }

    /// <summary>
    /// 匯出 Excel 報表
    /// </summary>
    public async Task<byte[]> ExportExcelAsync(
        ExcelReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var criteria = BuildSearchCriteria(request);
        var records = await _callRecordRepository.SearchAsync(criteria, cancellationToken);

        if (records.Count > ExportLimit)
        {
            throw new InvalidOperationException($"匯出筆數超過限制 {ExportLimit} 筆，請縮小篩選範圍");
        }

        using var workbook = new XLWorkbook();

        BuildFilterSheet(workbook, request, records.Count);
        BuildDetailSheet(workbook, records);
        BuildSummarySheet(workbook, records);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // 私有輔助方法
    private static CallRecordResponse MapToResponse(CallRecord callRecord)
    {
        return new CallRecordResponse
        {
            Id = callRecord.Id,
            Subject = callRecord.Subject,
            Content = callRecord.Content,
            Status = callRecord.Status,
            UrgencyLevel = callRecord.UrgencyLevel,
            ContactName = callRecord.ContactName,
            ContactPhone = callRecord.ContactPhone,
            FaqReference = callRecord.FaqReference,
            CreatedAt = callRecord.CreatedAt,
            UpdatedAt = callRecord.UpdatedAt,
            CreatedByUserId = callRecord.CreatedByUserId,
            LockedByUserId = callRecord.LockedByUserId,
            LockedAt = callRecord.LockedAt,
            InquirySystem = new InquirySystemDto
            {
                Id = callRecord.InquirySystem.Id,
                Name = callRecord.InquirySystem.Name,
                IsActive = callRecord.InquirySystem.IsActive
            },
            Handlers = callRecord.Handlers.Select(h => new HandlerDto
            {
                Id = h.Id,
                Name = h.Name,
                LineUserId = h.LineUserId
            }).ToList()
        };
    }

    private static CallRecordListItemResponse MapToListItemResponse(CallRecord callRecord)
    {
        return new CallRecordListItemResponse
        {
            Id = callRecord.Id,
            Subject = callRecord.Subject,
            Status = callRecord.Status,
            UrgencyLevel = callRecord.UrgencyLevel,
            ContactName = callRecord.ContactName,
            CreatedAt = callRecord.CreatedAt,
            InquirySystemName = callRecord.InquirySystem.Name,
            HandlerCount = callRecord.Handlers.Count
        };
    }

    private static CallRecordSearchCriteria BuildSearchCriteria(ExcelReportRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ReportMonth) &&
            (request.StartDate.HasValue || request.EndDate.HasValue))
        {
            throw new InvalidOperationException("reportMonth 與 startDate/endDate 不可同時使用");
        }

        DateTime? startUtc = null;
        DateTime? endUtc = null;

        if (!string.IsNullOrWhiteSpace(request.ReportMonth))
        {
            var parts = request.ReportMonth.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
            {
                throw new InvalidOperationException("reportMonth 格式必須為 yyyy/MM");
            }

            var startLocal = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var endLocal = startLocal.AddMonths(1).AddTicks(-1);
            startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, TaipeiTimeZone);
            endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, TaipeiTimeZone);
        }
        else
        {
            if (request.StartDate.HasValue)
            {
                var startLocal = request.StartDate.Value.Date;
                startUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, TaipeiTimeZone);
            }

            if (request.EndDate.HasValue)
            {
                var endLocal = request.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                endUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, TaipeiTimeZone);
            }
        }

        return new CallRecordSearchCriteria
        {
            Keyword = request.Keyword?.Trim(),
            InquirySystemId = request.InquirySystemId,
            Status = ParseEnum<ProcessStatus>(request.Status),
            UrgencyLevel = ParseEnum<UrgencyLevel>(request.UrgencyLevel),
            StartDateUtc = startUtc,
            EndDateUtc = endUtc
        };
    }

    private static TEnum? ParseEnum<TEnum>(string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"無效的參數值: {value}");
    }

    private static void BuildFilterSheet(XLWorkbook workbook, ExcelReportRequest request, int totalCount)
    {
        var ws = workbook.Worksheets.Add("篩選條件");
        ws.Cell(1, 1).Value = "條件";
        ws.Cell(1, 2).Value = "值";

        var row = 2;
        ws.Cell(row, 1).Value = "關鍵字";
        ws.Cell(row++, 2).Value = request.Keyword ?? "(無)";

        ws.Cell(row, 1).Value = "詢問系統";
        ws.Cell(row++, 2).Value = request.InquirySystemId?.ToString() ?? "(全部)";

        ws.Cell(row, 1).Value = "處理狀態";
        ws.Cell(row++, 2).Value = request.Status ?? "(全部)";

        ws.Cell(row, 1).Value = "緊急程度";
        ws.Cell(row++, 2).Value = request.UrgencyLevel ?? "(全部)";

        ws.Cell(row, 1).Value = "開始日期";
        ws.Cell(row++, 2).Value = request.StartDate?.ToString("yyyy/MM/dd") ?? "(無)";

        ws.Cell(row, 1).Value = "結束日期";
        ws.Cell(row++, 2).Value = request.EndDate?.ToString("yyyy/MM/dd") ?? "(無)";

        ws.Cell(row, 1).Value = "報表月份";
        ws.Cell(row++, 2).Value = request.ReportMonth ?? "(無)";

        ws.Cell(row, 1).Value = "總筆數";
        ws.Cell(row, 2).Value = totalCount;

        ws.Columns().AdjustToContents();
    }

    private static void BuildDetailSheet(XLWorkbook workbook, List<CallRecord> records)
    {
        var ws = workbook.Worksheets.Add("明細");
        var headers = new[]
        {
            "來電日期", "詢問系統", "主旨", "緊急度", "處理狀態",
            "處理人員", "聯絡人", "連絡電話", "最後更新時間",
            "內容", "參考 FAQ"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var row = 2;
        foreach (var record in records)
        {
            var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(record.CreatedAt, TaipeiTimeZone);
            var updatedLocal = TimeZoneInfo.ConvertTimeFromUtc(record.UpdatedAt, TaipeiTimeZone);

            ws.Cell(row, 1).Value = createdLocal;
            ws.Cell(row, 1).Style.DateFormat.Format = "yyyy/MM/dd HH:mm";
            ws.Cell(row, 2).Value = record.InquirySystem.Name;
            ws.Cell(row, 3).Value = record.Subject;
            ws.Cell(row, 4).Value = record.UrgencyLevel.ToString();
            ws.Cell(row, 5).Value = record.Status.ToString();
            ws.Cell(row, 6).Value = record.Handlers.Count == 0
                ? "(未指派)"
                : string.Join(", ", record.Handlers.Select(h => h.Name));
            ws.Cell(row, 7).Value = record.ContactName;
            ws.Cell(row, 8).Value = record.ContactPhone;
            ws.Cell(row, 9).Value = updatedLocal;
            ws.Cell(row, 9).Style.DateFormat.Format = "yyyy/MM/dd HH:mm";
            ws.Cell(row, 10).Value = record.Content;
            ws.Cell(row, 11).Value = record.FaqReference ?? string.Empty;

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static void BuildSummarySheet(XLWorkbook workbook, List<CallRecord> records)
    {
        var ws = workbook.Worksheets.Add("彙總");
        ws.Cell(1, 1).Value = "月份";
        ws.Cell(1, 2).Value = "詢問系統";
        ws.Cell(1, 3).Value = "處理人員";
        ws.Cell(1, 4).Value = "筆數";

        var row = 2;

        var summary = records
            .SelectMany(record =>
            {
                var month = TimeZoneInfo.ConvertTimeFromUtc(record.CreatedAt, TaipeiTimeZone)
                    .ToString("yyyy/MM");
                var handlers = record.Handlers.Count == 0
                    ? new[] { "(未指派)" }
                    : record.Handlers.Select(h => h.Name);

                return handlers.Select(handler => new
                {
                    Month = month,
                    InquirySystem = record.InquirySystem.Name,
                    Handler = handler
                });
            })
            .GroupBy(x => new { x.Month, x.InquirySystem, x.Handler })
            .Select(g => new
            {
                g.Key.Month,
                g.Key.InquirySystem,
                g.Key.Handler,
                Count = g.Count()
            })
            .OrderBy(x => x.Month)
            .ThenBy(x => x.InquirySystem)
            .ThenBy(x => x.Handler)
            .ToList();

        foreach (var item in summary)
        {
            ws.Cell(row, 1).Value = item.Month;
            ws.Cell(row, 2).Value = item.InquirySystem;
            ws.Cell(row, 3).Value = item.Handler;
            ws.Cell(row, 4).Value = item.Count;
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);
    }

    private static bool IsLocked(CallRecord callRecord, int lockTimeoutMinutes)
    {
        if (callRecord.LockedByUserId == null || !callRecord.LockedAt.HasValue)
        {
            return false;
        }

        return DateTime.UtcNow <= callRecord.LockedAt.Value.AddMinutes(lockTimeoutMinutes);
    }

    private static List<ChangeHistory> BuildChangeHistories(
        CallRecord callRecord,
        UpdateCallRecordRequest request,
        string userId)
    {
        var histories = new List<ChangeHistory>();

        AddHistoryIfChanged(histories, callRecord.Id, "Subject", callRecord.Subject, request.Subject, userId);
        AddHistoryIfChanged(histories, callRecord.Id, "Content", callRecord.Content, request.Content, userId);
        AddHistoryIfChanged(histories, callRecord.Id, "UrgencyLevel", callRecord.UrgencyLevel.ToString(), request.UrgencyLevel.ToString(), userId);
        AddHistoryIfChanged(histories, callRecord.Id, "ContactName", callRecord.ContactName, request.ContactName, userId);
        AddHistoryIfChanged(histories, callRecord.Id, "ContactPhone", callRecord.ContactPhone, request.ContactPhone, userId);
        AddHistoryIfChanged(histories, callRecord.Id, "FaqReference", callRecord.FaqReference, request.FaqReference, userId);

        return histories;
    }

    private static void AddHistoryIfChanged(
        List<ChangeHistory> histories,
        int callRecordId,
        string fieldName,
        string? oldValue,
        string? newValue,
        string userId)
    {
        var oldNormalized = oldValue?.Trim();
        var newNormalized = newValue?.Trim();

        if (string.Equals(oldNormalized, newNormalized, StringComparison.Ordinal))
        {
            return;
        }

        histories.Add(ChangeHistory.Create(callRecordId, fieldName, oldNormalized, newNormalized, userId));
    }
}
