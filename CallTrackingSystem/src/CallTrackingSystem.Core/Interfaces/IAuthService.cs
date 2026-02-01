using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 認證服務介面
/// </summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(string userId, string oldPassword, string newPassword, CancellationToken cancellationToken = default);
}
