using BCrypt.Net;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 使用者服務
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByUsernameAsync(username, cancellationToken);
    }

    public async Task<User?> GetByLineUserIdAsync(string lineUserId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByLineUserIdAsync(lineUserId, cancellationToken);
    }

    public async Task<User> CreateAsync(
        string username,
        string password,
        string name,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("使用者名稱已存在");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        var user = User.Create(username, passwordHash, name, role);
        return await _userRepository.AddAsync(user, cancellationToken);
    }

    public async Task<User> UpdateAsync(
        string userId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("使用者不存在");
        }

        user.UpdateInfo(name);
        await _userRepository.UpdateAsync(user, cancellationToken);
        return user;
    }

    public async Task<User> BindLineAccountAsync(
        string userId,
        string lineUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("使用者不存在");
        }

        var existing = await _userRepository.GetByLineUserIdAsync(lineUserId, cancellationToken);
        if (existing != null && existing.Id != userId)
        {
            throw new InvalidOperationException("此 LINE 帳號已綁定到其他使用者");
        }

        user.BindLineAccount(lineUserId);
        await _userRepository.UpdateAsync(user, cancellationToken);
        return user;
    }
}
