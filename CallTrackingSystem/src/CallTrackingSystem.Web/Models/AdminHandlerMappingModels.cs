namespace CallTrackingSystem.Web.Models;

public record HandlerMappingResponse
{
    public int Id { get; init; }
    public HandlerResponse Handler { get; init; } = new();
    public InquirySystemResponse InquirySystem { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

public record CreateHandlerMappingRequest
{
    public int HandlerId { get; init; }
    public int InquirySystemId { get; init; }
}
