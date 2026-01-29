using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 通知記錄服務
/// </summary>
public class NotificationLogService : INotificationLogService
{
    private readonly INotificationLogRepository _notificationLogRepository;

    public NotificationLogService(INotificationLogRepository notificationLogRepository)
    {
        _notificationLogRepository = notificationLogRepository;
    }

    public async Task LogNotificationAsync(
        int callRecordId,
        string lineUserId,
        NotificationMessageType messageType,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var log = success
            ? NotificationLog.CreateSuccess(callRecordId, lineUserId, messageType)
            : NotificationLog.CreateFailure(callRecordId, lineUserId, messageType, errorMessage ?? "未知錯誤");

        await _notificationLogRepository.AddAsync(log, cancellationToken);
    }

    public Task<List<NotificationLog>> GetByCallRecordIdAsync(int callRecordId, CancellationToken cancellationToken = default)
    {
        return _notificationLogRepository.GetByCallRecordIdAsync(callRecordId, cancellationToken);
    }

    public Task<List<NotificationLog>> GetFailedNotificationsAsync(CancellationToken cancellationToken = default)
    {
        return _notificationLogRepository.GetFailedAsync(cancellationToken);
    }
}
