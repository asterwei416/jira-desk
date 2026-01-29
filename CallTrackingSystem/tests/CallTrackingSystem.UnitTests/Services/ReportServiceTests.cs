using System.Reflection;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using ClosedXML.Excel;
using FluentAssertions;
using Moq;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// ReportService 單元測試
/// </summary>
public class ReportServiceTests
{
    private readonly Mock<ICallRecordRepository> _callRecordRepository;
    private readonly Mock<IInquirySystemRepository> _inquirySystemRepository;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _callRecordRepository = new Mock<ICallRecordRepository>();
        _inquirySystemRepository = new Mock<IInquirySystemRepository>();
        _reportService = new ReportService(
            _callRecordRepository.Object,
            _inquirySystemRepository.Object);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithValidData_ShouldCreateThreeSheets()
    {
        // Arrange
        var system = CreateInquirySystem(1, "訂單查詢系統");
        var records = new List<CallRecord>
        {
            CreateCallRecord(1, system, "主旨1", "內容1", ProcessStatus.Pending, UrgencyLevel.Low),
            CreateCallRecord(2, system, "主旨2", "內容2", ProcessStatus.InProgress, UrgencyLevel.High)
        };

        _callRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<CallRecordSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);

        using var workbook = LoadWorkbook(bytes);
        workbook.Worksheets.Count.Should().Be(3);
        workbook.Worksheets.Select(w => w.Name).Should().Contain(new[]
        {
            "篩選摘要", "明細資料", "統計彙總"
        });
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithFilters_ShouldWriteSummarySheet()
    {
        // Arrange
        var system = CreateInquirySystem(3, "會員服務系統");
        var records = new List<CallRecord>
        {
            CreateCallRecord(3, system, "主旨3", "內容3", ProcessStatus.Completed, UrgencyLevel.Medium)
        };

        _callRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<CallRecordSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        var filter = new ExcelReportRequest
        {
            Keyword = "測試",
            InquirySystemId = 3,
            Status = "Completed",
            UrgencyLevel = "Medium",
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            ReportMonth = "2026/01"
        };

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using var workbook = LoadWorkbook(bytes);
        var sheet = workbook.Worksheet("篩選摘要");

        var summaryPairs = GetSummaryPairs(sheet, 3, 12);
        summaryPairs.Should().ContainKey("關鍵字").WhoseValue.Should().Be("測試");
        summaryPairs.Should().ContainKey("詢問系統").WhoseValue.Should().Be("3");
        summaryPairs.Should().ContainKey("狀態").WhoseValue.Should().Be("Completed");
        summaryPairs.Should().ContainKey("緊急程度").WhoseValue.Should().Be("Medium");
        summaryPairs.Should().ContainKey("開始日期").WhoseValue.Should().Be("2026/01/01");
        summaryPairs.Should().ContainKey("結束日期").WhoseValue.Should().Be("2026/01/31");
        summaryPairs.Should().ContainKey("報表月份").WhoseValue.Should().Be("2026/01");
    }

    [Fact]
    public async Task GenerateExcelReportAsync_ShouldCreateDetailSheetWithHeadersAndData()
    {
        // Arrange
        var system = CreateInquirySystem(5, "物流追蹤系統");
        var createdAt = new DateTime(2026, 1, 10, 9, 30, 0);
        var record = CreateCallRecord(10, system, "主旨A", "內容A", ProcessStatus.Pending, UrgencyLevel.Low, createdAt);

        _callRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<CallRecordSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CallRecord> { record });

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        using var workbook = LoadWorkbook(bytes);
        var sheet = workbook.Worksheet("明細資料");

        var expectedHeaders = new[]
        {
            "紀錄 ID", "主旨", "內容", "詢問系統", "狀態",
            "緊急程度", "聯絡人", "聯絡電話", "建立時間"
        };

        for (var i = 0; i < expectedHeaders.Length; i++)
        {
            sheet.Cell(1, i + 1).GetString().Should().Be(expectedHeaders[i]);
        }

        sheet.Cell(2, 1).GetValue<int>().Should().Be(10);
        sheet.Cell(2, 2).GetString().Should().Be("主旨A");
        sheet.Cell(2, 3).GetString().Should().Be("內容A");
        sheet.Cell(2, 4).GetString().Should().Be("物流追蹤系統");
        sheet.Cell(2, 5).GetString().Should().Be("待處理");
        sheet.Cell(2, 6).GetString().Should().Be("低");
        sheet.Cell(2, 7).GetString().Should().Be("聯絡人A");
        sheet.Cell(2, 8).GetString().Should().Be("0912345678");
        sheet.Cell(2, 9).GetValue<DateTime>().Should().Be(createdAt);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_ShouldCreateStatisticsSheet()
    {
        // Arrange
        var systemA = CreateInquirySystem(1, "帳務系統");
        var systemB = CreateInquirySystem(2, "會員系統");

        var records = new List<CallRecord>
        {
            CreateCallRecord(1, systemA, "主旨1", "內容1", ProcessStatus.Pending, UrgencyLevel.Low,
                new DateTime(2026, 1, 2, 8, 0, 0)),
            CreateCallRecord(2, systemA, "主旨2", "內容2", ProcessStatus.Pending, UrgencyLevel.Low,
                new DateTime(2026, 1, 15, 9, 0, 0)),
            CreateCallRecord(3, systemB, "主旨3", "內容3", ProcessStatus.Completed, UrgencyLevel.High,
                new DateTime(2026, 2, 1, 10, 0, 0))
        };

        _callRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<CallRecordSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        using var workbook = LoadWorkbook(bytes);
        var sheet = workbook.Worksheet("統計彙總");

        // 月份統計
        sheet.Cell(1, 1).GetString().Should().Be("按月份統計");
        sheet.Cell(2, 1).GetString().Should().Be("月份");
        sheet.Cell(2, 2).GetString().Should().Be("筆數");

        var monthRow1 = FindRowByFirstColumn(sheet, "2026/01");
        sheet.Cell(monthRow1, 2).GetValue<int>().Should().Be(2);

        var monthRow2 = FindRowByFirstColumn(sheet, "2026/02");
        sheet.Cell(monthRow2, 2).GetValue<int>().Should().Be(1);

        var monthTotalRow = FindRowByFirstColumn(sheet, "合計");
        sheet.Cell(monthTotalRow, 2).GetValue<int>().Should().Be(3);

        // 詢問系統統計
        var systemHeaderRow = FindRowByFirstColumn(sheet, "按詢問系統統計");
        sheet.Cell(systemHeaderRow + 1, 1).GetString().Should().Be("詢問系統");
        sheet.Cell(systemHeaderRow + 1, 2).GetString().Should().Be("筆數");

        var systemRowA = FindRowByFirstColumn(sheet, "帳務系統");
        sheet.Cell(systemRowA, 2).GetValue<int>().Should().Be(2);

        var systemRowB = FindRowByFirstColumn(sheet, "會員系統");
        sheet.Cell(systemRowB, 2).GetValue<int>().Should().Be(1);

        var systemTotalRow = FindRowByFirstColumn(sheet, "合計", startRow: systemHeaderRow + 1);
        sheet.Cell(systemTotalRow, 2).GetValue<int>().Should().Be(3);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WhenExceedLimit_ShouldThrow()
    {
        // Arrange
        var system = CreateInquirySystem(1, "訂單查詢系統");
        var records = Enumerable.Range(1, 5001)
            .Select(i => CreateCallRecord(i, system, $"主旨{i}", $"內容{i}", ProcessStatus.Pending, UrgencyLevel.Low))
            .ToList();

        _callRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<CallRecordSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        // Act
        var act = async () => await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*5000*");
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WhenEmptyData_ShouldStillCreateWorkbook()
    {
        // Arrange
        _callRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<CallRecordSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CallRecord>());

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        using var workbook = LoadWorkbook(bytes);
        workbook.Worksheets.Count.Should().Be(3);
        var detailSheet = workbook.Worksheet("明細資料");
        (detailSheet.LastRowUsed()?.RowNumber() ?? 1).Should().Be(1);
    }

    private static XLWorkbook LoadWorkbook(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new XLWorkbook(stream);
    }

    private static Dictionary<string, string> GetSummaryPairs(IXLWorksheet worksheet, int startRow, int endRow)
    {
        var result = new Dictionary<string, string>();
        for (var row = startRow; row <= endRow; row++)
        {
            var key = worksheet.Cell(row, 1).GetString();
            var value = worksheet.Cell(row, 2).GetString();
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }
        return result;
    }

    private static int FindRowByFirstColumn(IXLWorksheet worksheet, string value, int startRow = 1)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = startRow; row <= lastRow; row++)
        {
            if (worksheet.Cell(row, 1).GetString() == value)
            {
                return row;
            }
        }
        throw new InvalidOperationException($"找不到行：{value}");
    }

    private static InquirySystem CreateInquirySystem(int id, string name)
    {
        var system = InquirySystem.Create(name);
        SetPrivateProperty(system, "Id", id);
        return system;
    }

    private static CallRecord CreateCallRecord(
        int id,
        InquirySystem system,
        string subject,
        string content,
        ProcessStatus status,
        UrgencyLevel urgencyLevel,
        DateTime? createdAt = null)
    {
        var record = CallRecord.Create(
            subject,
            content,
            system.Id,
            urgencyLevel,
            "聯絡人A",
            "0912345678",
            "user-1");

        record.InquirySystem = system;
        SetPrivateProperty(record, "Id", id);
        SetPrivateProperty(record, "Status", status);
        SetPrivateProperty(record, "UrgencyLevel", urgencyLevel);

        var createdTime = createdAt ?? DateTime.UtcNow;
        SetPrivateProperty(record, "CreatedAt", createdTime);
        SetPrivateProperty(record, "UpdatedAt", createdTime);

        return record;
    }

    private static void SetPrivateProperty<T>(T target, string propertyName, object? value)
    {
        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property?.SetValue(target, value);
    }
}
