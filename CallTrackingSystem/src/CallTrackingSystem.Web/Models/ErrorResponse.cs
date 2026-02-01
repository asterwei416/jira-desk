namespace CallTrackingSystem.Web.Models;

/// <summary>
/// 標準錯誤回應
/// </summary>
public record ErrorResponse
{
    public required string Error { get; init; }
    public string? Code { get; init; }
    public required DateTime Timestamp { get; init; }
}
