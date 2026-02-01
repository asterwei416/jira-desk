using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 處理人員管理服務
/// </summary>
public interface IHandlerService
{
    Task<List<Handler>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Handler?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Handler> CreateAsync(string name, string? lineUserId, CancellationToken cancellationToken = default);
    Task<Handler> UpdateAsync(int id, string name, string? lineUserId, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
