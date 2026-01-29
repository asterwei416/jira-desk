using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 變更歷史服務介面
/// </summary>
public interface IChangeHistoryService
{
    Task LogChangeAsync(int callRecordId, string fieldName, string? oldValue, string? newValue, string userId, CancellationToken cancellationToken = default);
    Task<List<ChangeHistory>> GetHistoryAsync(int callRecordId, CancellationToken cancellationToken = default);
}
