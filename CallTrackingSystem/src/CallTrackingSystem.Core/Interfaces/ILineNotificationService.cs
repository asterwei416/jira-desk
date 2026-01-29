using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// LINE 通知服務介面
/// </summary>
public interface ILineNotificationService
{
    Task SendCallRecordNotificationAsync(
        CallRecord callRecord,
        IReadOnlyCollection<string> lineUserIds,
        CancellationToken cancellationToken = default);
}
