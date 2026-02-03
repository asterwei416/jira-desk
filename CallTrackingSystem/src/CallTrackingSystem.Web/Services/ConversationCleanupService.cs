using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Web.Services;

/// <summary>
/// 對話狀態清理背景服務（每 5 分鐘執行）
/// </summary>
public class ConversationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConversationCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public ConversationCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ConversationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("對話狀態清理服務已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var conversationService = scope.ServiceProvider.GetRequiredService<IConversationStateService>();

                var expiredConversations = await conversationService.GetExpiredConversationsAsync();
                if (expiredConversations.Count > 0)
                {
                    await conversationService.ClearMultipleConversationsAsync(expiredConversations);
                    _logger.LogInformation("已清理 {Count} 個過期對話", expiredConversations.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "對話狀態清理失敗");
            }
        }

        _logger.LogInformation("對話狀態清理服務已停止");
    }
}
