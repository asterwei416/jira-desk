using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 變更歷史服務
/// </summary>
public class ChangeHistoryService : IChangeHistoryService
{
    private readonly IChangeHistoryRepository _changeHistoryRepository;

    public ChangeHistoryService(IChangeHistoryRepository changeHistoryRepository)
    {
        _changeHistoryRepository = changeHistoryRepository;
    }

    public async Task LogChangeAsync(
        int callRecordId,
        string fieldName,
        string? oldValue,
        string? newValue,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var history = ChangeHistory.Create(callRecordId, fieldName, oldValue, newValue, userId);
        await _changeHistoryRepository.AddRangeAsync(new[] { history }, cancellationToken);
    }

    public Task<List<ChangeHistory>> GetHistoryAsync(int callRecordId, CancellationToken cancellationToken = default)
    {
        return _changeHistoryRepository.GetByCallRecordIdAsync(callRecordId, cancellationToken);
    }
}
