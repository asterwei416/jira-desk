using System.Text.RegularExpressions;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 認證服務
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("帳號或密碼錯誤");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("帳號已停用");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new InvalidOperationException("帳號或密碼錯誤");
        }

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = 86400,
            User = MapToProfile(user)
        };
    }

    public async Task ChangePasswordAsync(
        string userId,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("使用者不存在");
        }

        if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("舊密碼錯誤");
        }

        if (!IsValidPassword(newPassword))
        {
            throw new InvalidOperationException("新密碼格式不正確，需至少 8 碼且包含大小寫英文字母與數字");
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.ChangePassword(newHash);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    private static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        var hasUpper = Regex.IsMatch(password, "[A-Z]");
        var hasLower = Regex.IsMatch(password, "[a-z]");
        var hasDigit = Regex.IsMatch(password, "\\d");

        return hasUpper && hasLower && hasDigit;
    }

    private static UserProfile MapToProfile(User user)
    {
        return new UserProfile
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role,
            LineUserId = user.LineUserId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
