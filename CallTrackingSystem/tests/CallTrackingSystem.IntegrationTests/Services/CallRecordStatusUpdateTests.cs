using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CallTrackingSystem.IntegrationTests.Services;

public class CallRecordStatusUpdateTests
{
    [Fact]
    public async Task UpdateStatusAsync_records_change_history()
    {
        var context = CreateContext();
        var callRecordRepository = new CallRecordRepository(context);
        var inquiryRepository = new InquirySystemRepository(context);
        var handlerRepository = new HandlerRepository(context);
        var changeHistoryRepository = new ChangeHistoryRepository(context);
        var notificationLogRepository = new NotificationLogRepository(context);
        var notificationLogService = new NotificationLogService(notificationLogRepository);

        var inquirySystem = InquirySystem.Create("帳務系統");
        context.InquirySystems.Add(inquirySystem);
        await context.SaveChangesAsync();

        var record = CallRecord.Create("帳務異常", "內容", inquirySystem.Id, UrgencyLevel.High, "張三", "0912", "user-1");
        record.InquirySystem = inquirySystem;
        context.CallRecords.Add(record);
        await context.SaveChangesAsync();

        var service = new CallRecordService(
            callRecordRepository,
            inquiryRepository,
            handlerRepository,
            changeHistoryRepository,
            new FakeLineNotificationService(),
            notificationLogService);

        var updated = await service.UpdateStatusAsync(record.Id, ProcessStatus.Completed, "user-2");

        Assert.Equal(ProcessStatus.Completed, updated.Status);

        var histories = await changeHistoryRepository.GetByCallRecordIdAsync(record.Id);
        Assert.Single(histories);
        Assert.Equal("Status", histories[0].FieldName);
        Assert.Equal("Pending", histories[0].OldValue);
        Assert.Equal("Completed", histories[0].NewValue);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private sealed class FakeLineNotificationService : ILineNotificationService
    {
        public Task SendCallRecordNotificationAsync(CallRecord callRecord, IReadOnlyCollection<string> lineUserIds, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
