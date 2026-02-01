namespace CallTrackingSystem.Web.Models;

public record HandlerResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? LineUserId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateHandlerRequest
{
    public string Name { get; init; } = string.Empty;
    public string? LineUserId { get; init; }
}

public record UpdateHandlerRequest
{
    public string Name { get; init; } = string.Empty;
    public string? LineUserId { get; init; }
    public bool IsActive { get; init; }
}
