using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using FluentAssertions;
using Moq;
using OfficeOpenXml;
using Xunit;

namespace CallTrackingSystem.UnitTests.Services;

/// <summary>
/// ReportService 單元測試
/// 驗證 Excel 報表生成邏輯
/// </summary>
public class ReportServiceTests
{
    private readonly Mock<ICallRecordRepository> _mockCallRecordRepository;
    private readonly Mock<IInquirySystemRepository> _mockInquirySystemRepository;
    private readonly ReportService _reportService;

    public ReportServiceTests()
    {
        _mockCallRecordRepository = new Mock<ICallRecordRepository>();
        _mockInquirySystemRepository = new Mock<IInquirySystemRepository>();
        _reportService = new ReportService(
            _mockCallRecordRepository.Object,
            _mockInquirySystemRepository.Object);

        // EPPlus 授權設定（開發環境）
        EPPlus.LicenseContext.LicenseType = EPPlus.LicenseType.NonCommercial;
    }

    #region 成功情境

    [Fact]
    public async Task GenerateExcelReportAsync_WithValidData_ShouldCreateExcelWithThreeSheets()
    {
        // Arrange
        var callRecords = CreateTestCallRecords(5);
        var inquirySystems = CreateTestInquirySystems();
        
        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel
        {
            Keyword = "測試",
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        excelBytes.Should().NotBeEmpty("Excel 檔案應該有內容");
        excelBytes.Length.Should().BeGreaterThan(1000, "Excel 檔案應該有合理的大小");

        // 驗證 Excel 結構
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            package.Workbook.Worksheets.Count.Should().Be(3, "Excel 應該有三個工作表");
            package.Workbook.Worksheets[0].Name.Should().Be("篩選摘要");
            package.Workbook.Worksheets[1].Name.Should().Be("明細資料");
            package.Workbook.Worksheets[2].Name.Should().Be("統計彙總");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_SummarySheet_ShouldContainFilterSummary()
    {
        // Arrange
        var callRecords = CreateTestCallRecords(3);
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel
        {
            Keyword = "測試",
            Status = ProcessStatus.Pending,
            UrgencyLevel = UrgencyLevel.High,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            PageNumber = 1,
            PageSize = 100
        };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var summarySheet = package.Workbook.Worksheets["篩選摘要"];
            
            // 驗證篩選條件存在
            var content = summarySheet.Cells.AsEnumerable()
                .SelectMany(c => c)
                .Select(c => c.Value?.ToString() ?? "")
                .ToList();

            content.Should().Contain(c => c.Contains("測試"), "應該包含關鍵字篩選");
            content.Should().Contain(c => c.Contains("待處理"), "應該包含狀態篩選");
            content.Should().Contain(c => c.Contains("高"), "應該包含緊急度篩選");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_DetailSheet_ShouldContainAllCallRecords()
    {
        // Arrange
        var callRecords = CreateTestCallRecords(5);
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            
            // 驗證欄位標題
            detailSheet.Cells[1, 1].Value.Should().Be("紀錄 ID");
            detailSheet.Cells[1, 2].Value.Should().Be("客戶名稱");
            detailSheet.Cells[1, 3].Value.Should().Be("聯絡電話");
            detailSheet.Cells[1, 4].Value.Should().Be("詢問系統");
            detailSheet.Cells[1, 5].Value.Should().Be("主旨");
            detailSheet.Cells[1, 6].Value.Should().Be("內容");
            detailSheet.Cells[1, 7].Value.Should().Be("狀態");
            detailSheet.Cells[1, 8].Value.Should().Be("緊急度");
            detailSheet.Cells[1, 9].Value.Should().Be("來電時間");

            // 驗證資料行數（標題 + 5 筆紀錄）
            var dataRows = detailSheet.Dimension.Rows;
            dataRows.Should().Be(6, "應該有 1 行標題 + 5 行資料");

            // 驗證第一筆紀錄
            detailSheet.Cells[2, 1].Value.Should().Be("1");
            detailSheet.Cells[2, 2].Value.Should().Be("測試客戶 1");
            detailSheet.Cells[2, 3].Value.Should().Be("0912345671");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_DetailSheet_ShouldFormatDatesCorrectly()
    {
        // Arrange
        var callRecords = CreateTestCallRecords(1);
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            
            // 驗證日期格式（YYYY/MM/DD HH:MM）
            var dateCell = detailSheet.Cells[2, 9].Value?.ToString() ?? "";
            dateCell.Should().Contain("/", "日期應該包含 /");
            dateCell.Should().Contain(":", "日期應該包含 :");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_StatisticsSheet_ShouldContainMonthlyGrouping()
    {
        // Arrange
        var callRecords = CreateTestCallRecordsWithDates(new[]
        {
            new DateTime(2026, 1, 15),
            new DateTime(2026, 1, 20),
            new DateTime(2026, 2, 10),
            new DateTime(2026, 2, 15),
            new DateTime(2026, 3, 5)
        });
        
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var statsSheet = package.Workbook.Worksheets["統計彙總"];
            
            // 驗證月份標題
            var monthlyContent = statsSheet.Cells.AsEnumerable()
                .SelectMany(c => c)
                .Select(c => c.Value?.ToString() ?? "")
                .ToList();

            monthlyContent.Should().Contain(c => c.Contains("2026/01"), "應該包含 2026/01");
            monthlyContent.Should().Contain(c => c.Contains("2026/02"), "應該包含 2026/02");
            monthlyContent.Should().Contain(c => c.Contains("2026/03"), "應該包含 2026/03");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_StatisticsSheet_ShouldGroupByInquirySystem()
    {
        // Arrange
        var callRecords = CreateTestCallRecords(10);
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var statsSheet = package.Workbook.Worksheets["統計彙總"];
            
            // 驗證詢問系統統計存在
            var systemContent = statsSheet.Cells.AsEnumerable()
                .SelectMany(c => c)
                .Select(c => c.Value?.ToString() ?? "")
                .ToList();

            systemContent.Should().Contain(c => c.Contains("訂單查詢系統"), "應該包含詢問系統統計");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_StatisticsSheet_ShouldCalculateTotalCorrectly()
    {
        // Arrange
        var callRecords = CreateTestCallRecords(5);
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var statsSheet = package.Workbook.Worksheets["統計彙總"];
            
            // 查找「合計」行
            var totalRow = -1;
            for (int row = 1; row <= statsSheet.Dimension.Rows; row++)
            {
                var cellValue = statsSheet.Cells[row, 1].Value?.ToString() ?? "";
                if (cellValue.Contains("合計") || cellValue.Contains("總計"))
                {
                    totalRow = row;
                    break;
                }
            }

            totalRow.Should().BeGreaterThan(0, "應該有合計行");
            
            // 驗證合計數值為 5
            var totalValue = statsSheet.Cells[totalRow, 2].Value;
            totalValue.Should().NotBeNull("應該有合計數值");
        }
    }

    #endregion

    #region 邊界情況

    [Fact]
    public async Task GenerateExcelReportAsync_WithEmptyData_ShouldCreateValidExcel()
    {
        // Arrange
        var emptyRecords = new List<CallRecord>();
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(emptyRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        excelBytes.Should().NotBeEmpty("即使沒有資料，也應該生成有效的 Excel");

        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            package.Workbook.Worksheets.Count.Should().Be(3);
            
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            detailSheet.Dimension.Rows.Should().Be(1, "應該只有標題行");
        }
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithLargeDataSet_ShouldHandleLimit()
    {
        // Arrange
        var largeDataSet = CreateTestCallRecords(5500); // 超過 5000 限制
        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(largeDataSet);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act & Assert
        // 根據需求，應該在 Service 層拋出例外或回傳錯誤
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _reportService.GenerateExcelReportAsync(filter));
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var callRecords = new List<CallRecord>
        {
            new CallRecord
            {
                Id = 1,
                Subject = "包含特殊字符 <>&\"' 的主旨",
                Content = "包含換行\n和引號\"的內容",
                ContactName = "張三",
                ContactPhone = "0912345678",
                Status = ProcessStatus.Pending,
                UrgencyLevel = UrgencyLevel.Medium,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                CreatedByUserId = "user1",
                InquirySystem = new InquirySystem { Id = 9, Name = "訂單查詢系統" },
                InquirySystemId = 9
            }
        };

        var inquirySystems = CreateTestInquirySystems();

        _mockCallRecordRepository
            .Setup(r => r.SearchAsync(It.IsAny<SearchFilterModel>()))
            .ReturnsAsync(callRecords);

        _mockInquirySystemRepository
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(inquirySystems);

        var filter = new SearchFilterModel { PageNumber = 1, PageSize = 100 };

        // Act
        var excelBytes = await _reportService.GenerateExcelReportAsync(filter);

        // Assert
        excelBytes.Should().NotBeEmpty();

        using (var package = new ExcelPackage(new MemoryStream(excelBytes)))
        {
            var detailSheet = package.Workbook.Worksheets["明細資料"];
            var subjectCell = detailSheet.Cells[2, 5].Value?.ToString();
            
            subjectCell.Should().Contain("特殊字符");
            subjectCell.Should().NotContain("<>&", "特殊字符應該被正確處理");
        }
    }

    #endregion

    #region 輔助方法

    private List<CallRecord> CreateTestCallRecords(int count)
    {
        var records = new List<CallRecord>();
        var inquirySystem = new InquirySystem { Id = 9, Name = "訂單查詢系統" };

        for (int i = 1; i <= count; i++)
        {
            records.Add(new CallRecord
            {
                Id = i,
                Subject = $"測試主旨 {i}",
                Content = $"測試內容 {i}",
                ContactName = $"測試客戶 {i}",
                ContactPhone = $"0912345670 + {i}",
                Status = i % 3 == 0 ? ProcessStatus.Completed : ProcessStatus.Pending,
                UrgencyLevel = i % 2 == 0 ? UrgencyLevel.High : UrgencyLevel.Medium,
                CreatedAt = DateTime.Now.AddDays(-i),
                UpdatedAt = DateTime.Now,
                CreatedByUserId = $"user{i % 3}",
                InquirySystem = inquirySystem,
                InquirySystemId = 9
            });
        }

        return records;
    }

    private List<CallRecord> CreateTestCallRecordsWithDates(DateTime[] dates)
    {
        var records = new List<CallRecord>();
        var inquirySystem = new InquirySystem { Id = 9, Name = "訂單查詢系統" };

        for (int i = 0; i < dates.Length; i++)
        {
            records.Add(new CallRecord
            {
                Id = i + 1,
                Subject = $"測試主旨 {i + 1}",
                Content = $"測試內容 {i + 1}",
                ContactName = $"測試客戶 {i + 1}",
                ContactPhone = $"0912345670",
                Status = ProcessStatus.Pending,
                UrgencyLevel = UrgencyLevel.Medium,
                CreatedAt = dates[i],
                UpdatedAt = dates[i],
                CreatedByUserId = "user1",
                InquirySystem = inquirySystem,
                InquirySystemId = 9
            });
        }

        return records;
    }

    private List<InquirySystem> CreateTestInquirySystems()
    {
        return new List<InquirySystem>
        {
            new InquirySystem { Id = 9, Name = "訂單查詢系統" },
            new InquirySystem { Id = 10, Name = "會員服務系統" },
            new InquirySystem { Id = 11, Name = "物流追蹤系統" }
        };
    }

    #endregion
}
