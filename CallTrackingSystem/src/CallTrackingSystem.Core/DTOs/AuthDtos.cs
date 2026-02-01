using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.DTOs;

/// <summary>
/// 登入請求
/// </summary>
public record LoginRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

/// <summary>
/// 登入回應
/// </summary>
public record LoginResponse
{
    public required string AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }
    public required UserProfile User { get; init; }
}

/// <summary>
/// 使用者資料
/// </summary>
public record UserProfile
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public required string Name { get; init; }
    public required UserRole Role { get; init; }
    public string? LineUserId { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// 更新個人資料請求
/// </summary>
public record UpdateProfileRequest
{
    public required string Name { get; init; }
}

/// <summary>
/// 變更密碼請求
/// </summary>
public record ChangePasswordRequest
{
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
}

/// <summary>
/// LINE 綁定請求
/// </summary>
public record BindLineAccountRequest
{
    public required string UserId { get; init; }
    public required string LineUserId { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>
/// LINE 解除綁定請求
/// </summary>
public record UnbindLineAccountRequest
{
    public required string UserId { get; init; }
}

/// <summary>
/// LINE Login 使用者資料
/// </summary>
public record LineLoginProfile
{
    public required string LineUserId { get; init; }
    public required string DisplayName { get; init; }
}
