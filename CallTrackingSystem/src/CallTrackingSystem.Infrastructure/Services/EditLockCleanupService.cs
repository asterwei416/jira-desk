using CallTrackingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// 編輯鎖定背景清理服務
/// 每 5 分鐘清理超過 30 分鐘未更新的鎖定
/// </summary>
public class EditLockCleanupService : BackgroundService
{
    private const int LockTimeoutMinutes = 30;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EditLockCleanupService> _logger;

    public EditLockCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<EditLockCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredLocksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理過期鎖定時發生錯誤");
            }

            await Task.Delay(CleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredLocksAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var expiredTime = DateTime.UtcNow.AddMinutes(-LockTimeoutMinutes);

        var expiredLocks = await context.CallRecords
            .Where(x => x.LockedByUserId != null && x.LockedAt.HasValue && x.LockedAt.Value <= expiredTime)
            .ToListAsync(cancellationToken);

        if (expiredLocks.Count == 0)
        {
            return;
        }

        foreach (var record in expiredLocks)
        {
            record.LockedByUserId = null;
            record.LockedAt = null;
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("已清理過期鎖定數量: {Count}", expiredLocks.Count);
    }
}
