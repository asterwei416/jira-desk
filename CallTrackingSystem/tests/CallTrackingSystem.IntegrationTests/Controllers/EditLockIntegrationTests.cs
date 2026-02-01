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
/// 編輯鎖定整合測試
/// </summary>
public class EditLockIntegrationTests
{
    [Fact]
    public async Task AcquireLock_WhenLockedByOther_ShouldReturnConflict()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        var inquirySystem = InquirySystem.Create("測試系統");
        context.InquirySystems.Add(inquirySystem);
        await context.SaveChangesAsync();

        var record = CallRecord.Create("主旨", "內容", inquirySystem.Id, UrgencyLevel.Low, "聯絡人", "0912345678", "user-1");
        record.LockedByUserId = "other-user";
        record.LockedAt = DateTime.UtcNow;
        context.CallRecords.Add(record);
        await context.SaveChangesAsync();

        var service = BuildService(context);
        var controller = new CallRecordsController(service, NullLogger<CallRecordsController>.Instance);

        var result = await controller.AcquireLock(record.Id);

        Assert.IsType<ConflictObjectResult>(result);
    }

    private static CallRecordService BuildService(ApplicationDbContext context)
    {
        var callRecordRepository = new CallRecordRepository(context);
        var inquirySystemRepository = new InquirySystemRepository(context);
        var handlerRepository = new HandlerRepository(context);
        var changeHistoryRepository = new ChangeHistoryRepository(context);
        var notificationLogRepository = new NotificationLogRepository(context);
        var notificationLogService = new NotificationLogService(notificationLogRepository);
        var lineService = new FakeLineNotificationService();

        return new CallRecordService(
            callRecordRepository,
            inquirySystemRepository,
            handlerRepository,
            changeHistoryRepository,
            lineService,
            notificationLogService);
    }

    private sealed class FakeLineNotificationService : ILineNotificationService
    {
        public Task SendCallRecordNotificationAsync(CallRecord callRecord, IReadOnlyCollection<string> lineUserIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
