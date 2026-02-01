using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using CallTrackingSystem.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CallTrackingSystem.IntegrationTests.Controllers;

/// <summary>
/// 來電紀錄 API 整合測試
/// </summary>
public class CallRecordsControllerTests
{
    [Fact]
    public async Task CreateAndGet_ShouldReturnRecord()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var inquirySystem = InquirySystem.Create("測試系統");
        context.InquirySystems.Add(inquirySystem);

        var handler = Handler.Create("處理人員", "U123");
        context.Handlers.Add(handler);
        await context.SaveChangesAsync();

        context.HandlerMappings.Add(HandlerMapping.Create(handler.Id, inquirySystem.Id));
        await context.SaveChangesAsync();

        var callRecordRepository = new CallRecordRepository(context);
        var inquirySystemRepository = new InquirySystemRepository(context);
        var handlerRepository = new HandlerRepository(context);
        var changeHistoryRepository = new ChangeHistoryRepository(context);
        var notificationLogRepository = new NotificationLogRepository(context);
        var notificationLogService = new NotificationLogService(notificationLogRepository);
        var lineService = new FakeLineNotificationService();

        var service = new CallRecordService(
            callRecordRepository,
            inquirySystemRepository,
            handlerRepository,
            changeHistoryRepository,
            lineService,
            notificationLogService);

        var controller = new CallRecordsController(service, NullLogger<CallRecordsController>.Instance);

        var createRequest = new CreateCallRecordRequest
        {
            Subject = "主旨",
            Content = "內容",
            InquirySystemId = inquirySystem.Id,
            UrgencyLevel = UrgencyLevel.Medium,
            ContactName = "聯絡人",
            ContactPhone = "0912345678",
            FaqReference = null
        };

        var createResult = await controller.CreateCallRecord(createRequest);
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var createdBody = Assert.IsType<CallRecordResponse>(created.Value);

        var getResult = await controller.GetCallRecord(createdBody.Id);
        var okResult = Assert.IsType<OkObjectResult>(getResult);
        var record = Assert.IsType<CallRecordResponse>(okResult.Value);

        Assert.Equal("主旨", record.Subject);
        Assert.Equal(inquirySystem.Id, record.InquirySystem.Id);
        Assert.Single(record.Handlers);
    }

    private sealed class FakeLineNotificationService : ILineNotificationService
    {
        public Task SendCallRecordNotificationAsync(CallRecord callRecord, IReadOnlyCollection<string> lineUserIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
