namespace CallTrackingSystem.Web.Models;

public record InquirySystemResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateInquirySystemRequest
{
    public string Name { get; init; } = string.Empty;
}

public record UpdateInquirySystemRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
