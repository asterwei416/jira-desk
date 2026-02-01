using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 使用者管理服務
/// </summary>
public interface IUserManagementService
{
    Task<List<User>> GetAllAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(string username, string password, string name, UserRole role, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(string userId, string name, UserRole role, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default);
}
