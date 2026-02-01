using System.Reflection;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CallTrackingSystem.IntegrationTests.Services;

/// <summary>
/// EditLockCleanupService 測試
/// </summary>
public class EditLockCleanupServiceTests
{
    [Fact]
    public async Task CleanupExpiredLocksAsync_ShouldClearExpiredLocksOnly()
    {
        var databaseRoot = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("EditLockCleanupTests", databaseRoot));
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var logger = provider.GetRequiredService<ILogger<EditLockCleanupService>>();

        await SeedDataAsync(provider);

        var service = new EditLockCleanupService(scopeFactory, logger);

        var method = typeof(EditLockCleanupService)
            .GetMethod("CleanupExpiredLocksAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        await (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var records = await context.CallRecords.AsNoTracking().ToListAsync();

        Assert.Equal(2, records.Count);

        var expired = records.FirstOrDefault(r => r.Subject == "過期鎖定");
        var active = records.FirstOrDefault(r => r.Subject == "有效鎖定");

        Assert.NotNull(expired);
        Assert.NotNull(active);

        Assert.Null(expired!.LockedByUserId);
        Assert.Null(expired.LockedAt);
        Assert.NotNull(active!.LockedByUserId);
        Assert.NotNull(active.LockedAt);
    }

    private static async Task SeedDataAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var system = InquirySystem.Create("測試系統");
        context.InquirySystems.Add(system);
        await context.SaveChangesAsync();

        var expiredRecord = CreateCallRecord("過期鎖定", system, DateTime.UtcNow.AddMinutes(-40));
        var activeRecord = CreateCallRecord("有效鎖定", system, DateTime.UtcNow.AddMinutes(-10));

        context.CallRecords.AddRange(expiredRecord, activeRecord);
        await context.SaveChangesAsync();
    }

    private static CallRecord CreateCallRecord(string subject, InquirySystem system, DateTime lockedAt)
    {
        var record = CallRecord.Create(
            subject,
            "內容",
            system.Id,
            UrgencyLevel.Low,
            "聯絡人",
            "0912345678",
            "user-1");

        record.InquirySystem = system;
        record.LockedByUserId = "other-user";
        record.LockedAt = lockedAt;
        return record;
    }
}
