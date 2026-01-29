using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 通知記錄服務介面
/// </summary>
public interface INotificationLogService
{
    Task LogNotificationAsync(
        int callRecordId,
        string lineUserId,
        NotificationMessageType messageType,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);

    Task<List<NotificationLog>> GetByCallRecordIdAsync(int callRecordId, CancellationToken cancellationToken = default);

    Task<List<NotificationLog>> GetFailedNotificationsAsync(CancellationToken cancellationToken = default);
}
