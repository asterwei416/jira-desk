using System.Text.RegularExpressions;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 使用者管理服務
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;

    public UserManagementService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<User>> GetAllAsync(UserRole? role, bool? isActive, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        if (role.HasValue)
        {
            users = users.Where(x => x.Role == role.Value).ToList();
        }

        if (isActive.HasValue)
        {
            users = users.Where(x => x.IsActive == isActive.Value).ToList();
        }

        return users;
    }

    public async Task<User> CreateAsync(
        string username,
        string password,
        string name,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("使用者名稱不可為空");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("名稱不可為空");
        }

        if (!IsValidPassword(password))
        {
            throw new InvalidOperationException("新密碼格式不正確，需至少 8 碼且包含大小寫英文字母與數字");
        }

        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("使用者名稱已存在");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = User.Create(username, hash, name, role);
        return await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<User> UpdateAsync(
        string userId,
        string name,
        UserRole role,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("使用者不存在");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("名稱不可為空");
        }

        user.UpdateInfo(name);
        user.UpdateRole(role);
        if (isActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        return user;
    }

    public async Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("使用者不存在");
        }

        await _userRepository.DeleteAsync(user, cancellationToken);
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("使用者不存在");
        }

        if (!IsValidPassword(newPassword))
        {
            throw new InvalidOperationException("新密碼格式不正確，需至少 8 碼且包含大小寫英文字母與數字");
        }

        user.ChangePassword(BCrypt.Net.BCrypt.HashPassword(newPassword));
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
}
