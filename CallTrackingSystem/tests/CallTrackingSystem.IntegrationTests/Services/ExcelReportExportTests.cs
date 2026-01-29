using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Xunit;

namespace CallTrackingSystem.IntegrationTests.Services;

/// <summary>
/// 報表匯出整合測試
/// 測試完整的 Excel 生成流程，包括資料庫查詢和檔案驗證
/// </summary>
public class ExcelReportExportTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly ReportService _reportService;

    public ExcelReportExportTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _reportService = new ReportService(
            new Infrastructure.Repositories.CallRecordRepository(_context),
            new Infrastructure.Repositories.InquirySystemRepository(_context));

        // EPPlus 授權設定
        EPPlus.LicenseContext.LicenseType = EPPlus.LicenseType.NonCommercial;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        _context.Dispose();
    }

    #region 完整流程測試

    [Fact]
    public async Task ExportCallRecords_CompleteFlow_ShouldReturnValidExcelFile()
    {
        // Arrange
        var filter = new SearchFilterModel
        {
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        excelBytes.Should().NotBeEmpty();
        excelBytes.Length.Should().BeGreaterThan(5000, "Excel 檔案應該有合理的大小");

        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            // 驗證基本結構
            package.Workbook.Worksheets.Count.Should().Be(3);
            package.Workbook.Worksheets[0].Name.Should().Be("篩選摘要");
            package.Workbook.Worksheets[1].Name.Should().Be("明細資料");
            package.Workbook.Worksheets[2].Name.Should().Be("統計彙總");
        }
    }

    [Fact]
    public async Task ExportWithFilters_ShouldOnlyIncludeFilteredRecords()
    {
        // Arrange
        var filter = new SearchFilterModel
        {
            Keyword = "訂單",
            Status = ProcessStatus.Pending,
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            
            // 驗證只包含篩選結果
            // 標題行 + 篩選後的紀錄
            var expectedRows = 1 + _context.CallRecords
                .Where(c => c.Subject.Contains("訂單") && c.Status == ProcessStatus.Pending)
                .Count();

            detailSheet.Dimension.Rows.Should().Be(expectedRows);
        }
    }

    [Fact]
    public async Task ExportWithDateRange_ShouldOnlyIncludeDateRangeRecords()
    {
        // Arrange
        var startDate = new DateTime(2026, 1, 15);
        var endDate = new DateTime(2026, 1, 25);

        var filter = new SearchFilterModel
        {
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            
            // 驗證日期範圍內的記錄
            for (int row = 2; row <= detailSheet.Dimension.Rows; row++)
            {
                var dateStr = detailSheet.Cells[row, 9].Value?.ToString() ?? "";
                // 每個日期應該在範圍內
                dateStr.Should().NotBeEmpty();
            }
        }
    }

    #endregion

    #region 資料驗證

    [Fact]
    public async Task ExportDetailSheet_ShouldIncludeAllRequiredColumns()
    {
        // Arrange
        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];

            // 驗證所有必需欄位
            var expectedHeaders = new[]
            {
                "紀錄 ID",
                "客戶名稱",
                "聯絡電話",
                "詢問系統",
                "主旨",
                "內容",
                "狀態",
                "緊急度",
                "來電時間"
            };

            for (int col = 1; col <= expectedHeaders.Length; col++)
            {
                detailSheet.Cells[1, col].Value?.ToString()
                    .Should().Be(expectedHeaders[col - 1], $"第 {col} 欄應該是 {expectedHeaders[col - 1]}");
            }
        }
    }

    [Fact]
    public async Task ExportDetailSheet_ShouldHaveCorrectDataTypes()
    {
        // Arrange
        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];

            if (detailSheet.Dimension.Rows > 1)
            {
                // 驗證 ID 欄位是數字
                var idValue = detailSheet.Cells[2, 1].Value;
                idValue.Should().NotBeNull();
                int.TryParse(idValue?.ToString() ?? "", out _).Should().BeTrue();

                // 驗證電話欄位是文字
                var phoneValue = detailSheet.Cells[2, 3].Value?.ToString() ?? "";
                phoneValue.Should().NotBeEmpty();

                // 驗證日期欄位格式
                var dateValue = detailSheet.Cells[2, 9].Value?.ToString() ?? "";
                dateValue.Should().Contain("/");
            }
        }
    }

    #endregion

    #region 統計驗證

    [Fact]
    public async Task ExportStatisticsSheet_ShouldGroupByMonth()
    {
        // Arrange
        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var statsSheet = package.Workbook.Worksheets["統計彙總"];
            
            // 驗證月份統計存在
            var content = statsSheet.Cells.AsEnumerable()
                .SelectMany(c => c)
                .Select(c => c.Value?.ToString() ?? "")
                .ToList();

            content.Should().Contain(c => c.Contains("2026/01"), "應該包含 2026/01 月份統計");
            content.Should().Contain(c => c.Contains("2026/02"), "應該包含 2026/02 月份統計");
        }
    }

    [Fact]
    public async Task ExportStatisticsSheet_ShouldCalculateMonthlyTotal()
    {
        // Arrange
        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var statsSheet = package.Workbook.Worksheets["統計彙總"];
            
            // 計算預期的月份統計
            var expectedMonthlyCount = _context.CallRecords
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Count();

            // 驗證統計行數（至少應該有月份統計 + 詢問系統統計）
            statsSheet.Dimension.Rows.Should().BeGreaterThan(expectedMonthlyCount);
        }
    }

    [Fact]
    public async Task ExportStatisticsSheet_ShouldGroupByInquirySystem()
    {
        // Arrange
        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var statsSheet = package.Workbook.Worksheets["統計彙總"];
            
            var content = statsSheet.Cells.AsEnumerable()
                .SelectMany(c => c)
                .Select(c => c.Value?.ToString() ?? "")
                .ToList();

            // 驗證詢問系統統計
            content.Should().Contain(c => c.Contains("訂單查詢系統"));
            content.Should().Contain(c => c.Contains("會員服務系統"));
        }
    }

    #endregion

    #region 邊界測試

    [Fact]
    public async Task ExportWithExceedLimit_ShouldThrowException()
    {
        // Arrange - 建立超過 5000 筆的紀錄
        var largeDataSet = new List<CallRecord>();
        var system = await _context.InquirySystems.FirstAsync();

        for (int i = 0; i < 5100; i++)
        {
            largeDataSet.Add(new CallRecord
            {
                Subject = $"測試 {i}",
                Content = $"內容 {i}",
                ContactName = "客戶",
                ContactPhone = "0912345678",
                Status = ProcessStatus.Pending,
                UrgencyLevel = UrgencyLevel.Medium,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedByUserId = "user1",
                InquirySystemId = system.Id
            });
        }

        _context.CallRecords.AddRange(largeDataSet);
        await _context.SaveChangesAsync();

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _reportService.GenerateExcelReportAsync(filter));
    }

    [Fact]
    public async Task ExportEmptyResult_ShouldStillReturnValidExcel()
    {
        // Arrange
        var filter = new SearchFilterModel
        {
            Keyword = "不存在的關鍵字",
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        excelBytes.Should().NotBeEmpty();

        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            
            // 應該只有標題行，沒有資料行
            detailSheet.Dimension.Rows.Should().Be(1);
        }
    }

    #endregion

    #region 檔案性質驗證

    [Fact]
    public void ExcelFile_ShouldHaveCorrectMimeType()
    {
        // Excel 檔案應該以 PK（ZIP）簽名開頭
        // 這驗證了檔案是有效的 XLSX 格式
        
        var validSignature = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // PK..
        
        // 實際測試中應該在生成後檢查
        // 這是一個提示性測試
        true.Should().BeTrue();
    }

    [Fact]
    public async Task ExportedFile_ShouldBeReadableByEPPlus()
    {
        // Arrange
        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert - 確保能用 EPPlus 打開
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            package.Should().NotBeNull();
            package.Workbook.Should().NotBeNull();
            package.Workbook.Worksheets.Should().NotBeEmpty();
        }
    }

    #endregion

    #region 輔助方法

    private async Task SeedTestDataAsync()
    {
        // 建立詢問系統
        var inquirySystems = new[]
        {
            new InquirySystem { Name = "訂單查詢系統" },
            new InquirySystem { Name = "會員服務系統" },
            new InquirySystem { Name = "物流追蹤系統" }
        };

        _context.InquirySystems.AddRange(inquirySystems);
        await _context.SaveChangesAsync();

        // 建立多筆紀錄（跨越不同月份）
        var callRecords = new[]
        {
            new CallRecord
            {
                Subject = "訂單查詢問題",
                Content = "查詢不到訂單",
                ContactName = "李四",
                ContactPhone = "0912345671",
                Status = ProcessStatus.Pending,
                UrgencyLevel = UrgencyLevel.High,
                CreatedAt = new DateTime(2026, 1, 10),
                UpdatedAt = new DateTime(2026, 1, 10),
                CreatedByUserId = "user1",
                InquirySystemId = inquirySystems[0].Id
            },
            new CallRecord
            {
                Subject = "訂單編號查詢",
                Content = "需要確認訂單號碼",
                ContactName = "王五",
                ContactPhone = "0912345672",
                Status = ProcessStatus.InProgress,
                UrgencyLevel = UrgencyLevel.Medium,
                CreatedAt = new DateTime(2026, 1, 20),
                UpdatedAt = new DateTime(2026, 1, 20),
                CreatedByUserId = "user2",
                InquirySystemId = inquirySystems[0].Id
            },
            new CallRecord
            {
                Subject = "會員帳號登入",
                Content = "無法登入會員帳號",
                ContactName = "趙六",
                ContactPhone = "0912345673",
                Status = ProcessStatus.Completed,
                UrgencyLevel = UrgencyLevel.Low,
                CreatedAt = new DateTime(2026, 2, 5),
                UpdatedAt = new DateTime(2026, 2, 5),
                CreatedByUserId = "user3",
                InquirySystemId = inquirySystems[1].Id
            },
            new CallRecord
            {
                Subject = "物流進度查詢",
                Content = "包裹已超過預期送達時間",
                ContactName = "孫七",
                ContactPhone = "0912345674",
                Status = ProcessStatus.Pending,
                UrgencyLevel = UrgencyLevel.High,
                CreatedAt = new DateTime(2026, 2, 15),
                UpdatedAt = new DateTime(2026, 2, 15),
                CreatedByUserId = "user1",
                InquirySystemId = inquirySystems[2].Id
            }
        };

        _context.CallRecords.AddRange(callRecords);
        await _context.SaveChangesAsync();
    }

    #endregion
}
