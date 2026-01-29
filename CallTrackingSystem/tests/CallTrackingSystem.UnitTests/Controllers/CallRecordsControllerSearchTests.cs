using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Web.Controllers;
using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CallTrackingSystem.UnitTests.Controllers;

public class CallRecordsControllerSearchTests
{
    [Fact]
    public async Task GetCallRecords_returns_ok_result_with_paged_data()
    {
        var service = CreateServiceWithPagedResult(out var _);
        var controller = new CallRecordsController(service, NullLogger<CallRecordsController>.Instance);

        var filter = new SearchFilterModel
        {
            PageNumber = 1,
            PageSize = 20
        };

        var result = await controller.GetCallRecords(filter);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PagedResult<CallRecordListItemResponse>>(okResult.Value);
        Assert.Single(paged.Items);
        Assert.Equal(1, paged.TotalCount);
    }

    [Fact]
    public async Task GetCallRecords_returns_bad_request_for_invalid_date_range()
    {
        var service = CreateServiceWithPagedResult(out var _);
        var controller = new CallRecordsController(service, NullLogger<CallRecordsController>.Instance);

        var filter = new SearchFilterModel
        {
            StartDate = new DateTime(2026, 1, 10),
            EndDate = new DateTime(2026, 1, 1)
        };

        var result = await controller.GetCallRecords(filter);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static CallRecordService CreateServiceWithPagedResult(out Mock<ICallRecordRepository> callRecordRepo)
    {
        callRecordRepo = new Mock<ICallRecordRepository>();
        var inquiryRepo = new Mock<IInquirySystemRepository>();
        var handlerRepo = new Mock<IHandlerRepository>();
        var changeHistoryRepo = new Mock<IChangeHistoryRepository>();
        var lineNotificationService = new Mock<ILineNotificationService>();
        var notificationLogService = new Mock<INotificationLogService>();

        var system = InquirySystem.Create("帳務系統");
        var record = CallRecord.Create("帳務異常", "內容", 1, UrgencyLevel.High, "張三", "0912", "user-1");
        record.InquirySystem = system;

        callRecordRepo
            .Setup(repo => repo.GetPagedAsync(
                It.IsAny<CallRecordSearchCriteria>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<CallRecord> { record }, 1));

        return new CallRecordService(
            callRecordRepo.Object,
            inquiryRepo.Object,
            handlerRepo.Object,
            changeHistoryRepo.Object,
            lineNotificationService.Object,
            notificationLogService.Object);
    }
}
