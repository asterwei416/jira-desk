using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Web.Models;

public record UserResponse
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string? LineUserId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}

public record UpdateUserRequest
{
    public string Name { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
}

public record ResetPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
}
