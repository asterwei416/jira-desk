using System.Reflection;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CallTrackingSystem.IntegrationTests.Services;

/// <summary>
/// 報表匯出整合測試
/// </summary>
public class ExcelReportExportTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly ReportService _reportService;

    public ExcelReportExportTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        var callRecordRepository = new CallRecordRepository(_context);
        var inquirySystemRepository = new InquirySystemRepository(_context);

        _reportService = new ReportService(callRecordRepository, inquirySystemRepository);
    }

    public async Task InitializeAsync()
    {
        await SeedTestDataAsync();
    }

    public Task DisposeAsync()
    {
        _context.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ExportCallRecords_CompleteFlow_ShouldReturnValidExcel()
    {
        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        using var workbook = LoadWorkbook(bytes);
        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.NotNull(workbook.Worksheet("篩選摘要"));
        Assert.NotNull(workbook.Worksheet("明細資料"));
        Assert.NotNull(workbook.Worksheet("統計彙總"));
    }

    [Fact]
    public async Task ExportWithFilters_ShouldOnlyIncludeFilteredRecords()
    {
        // Arrange
        var filter = new ExcelReportRequest
        {
            Keyword = "付款",
            Status = "Pending"
        };

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using var workbook = LoadWorkbook(bytes);
        var detailSheet = workbook.Worksheet("明細資料");

        var lastRow = detailSheet.LastRowUsed()?.RowNumber() ?? 1;
        // 標題列 + 1 筆符合條件資料
        Assert.Equal(2, lastRow);
        Assert.Equal("付款失敗", detailSheet.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task ExportWithDateRange_ShouldFilterByCreatedAt()
    {
        // Arrange
        var filter = new ExcelReportRequest
        {
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31, 23, 59, 59)
        };

        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using var workbook = LoadWorkbook(bytes);
        var detailSheet = workbook.Worksheet("明細資料");

        var lastRow = detailSheet.LastRowUsed()?.RowNumber() ?? 1;
        Assert.Equal(3, lastRow); // 標題列 + 2 筆 2026/01
    }

    [Fact]
    public async Task ExportStatisticsSheet_ShouldCalculateMonthlyTotal()
    {
        // Act
        var bytes = await _reportService.GenerateExcelReportAsync(new ExcelReportRequest());

        // Assert
        using var workbook = LoadWorkbook(bytes);
        var sheet = workbook.Worksheet("統計彙總");

        var januaryRow = FindRowByFirstColumn(sheet, "2026/01");
        Assert.Equal(2, sheet.Cell(januaryRow, 2).GetValue<int>());

        var februaryRow = FindRowByFirstColumn(sheet, "2026/02");
        Assert.Equal(1, sheet.Cell(februaryRow, 2).GetValue<int>());
    }

    private async Task SeedTestDataAsync()
    {
        var systemA = CreateInquirySystem(1, "訂單查詢系統");
        var systemB = CreateInquirySystem(2, "會員服務系統");

        _context.InquirySystems.AddRange(systemA, systemB);

        var record1 = CreateCallRecord(1, systemA, "付款失敗", "付款失敗，請協助", ProcessStatus.Pending, UrgencyLevel.High,
            new DateTime(2026, 1, 5, 10, 0, 0));
        var record2 = CreateCallRecord(2, systemA, "登入問題", "登入失敗", ProcessStatus.Completed, UrgencyLevel.Low,
            new DateTime(2026, 1, 20, 9, 0, 0));
        var record3 = CreateCallRecord(3, systemB, "物流查詢", "查詢不到物流", ProcessStatus.InProgress, UrgencyLevel.Medium,
            new DateTime(2026, 2, 10, 14, 0, 0));

        _context.CallRecords.AddRange(record1, record2, record3);
        await _context.SaveChangesAsync();
    }

    private static XLWorkbook LoadWorkbook(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new XLWorkbook(stream);
    }

    private static int FindRowByFirstColumn(IXLWorksheet worksheet, string value)
    {
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 1; row <= lastRow; row++)
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
        DateTime createdAt)
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
        SetPrivateProperty(record, "CreatedAt", createdAt);
        SetPrivateProperty(record, "UpdatedAt", createdAt);

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
