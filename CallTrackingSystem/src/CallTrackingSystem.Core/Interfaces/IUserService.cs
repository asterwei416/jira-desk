using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 使用者服務介面
/// </summary>
public interface IUserService
{
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByLineUserIdAsync(string lineUserId, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string username, string password, string name, UserRole role, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(string userId, string name, CancellationToken cancellationToken = default);
    Task<User> BindLineAccountAsync(string userId, string lineUserId, CancellationToken cancellationToken = default);
}
