using System.Text.Json.Serialization;

namespace CallTrackingSystem.Core.DTOs;

/// <summary>
/// LINE Webhook 事件 DTO
/// </summary>
public record LineWebhookEventDto
{
    [JsonPropertyName("destination")]
    public required string Destination { get; init; }

    [JsonPropertyName("events")]
    public required List<LineEventDto> Events { get; init; }
}

/// <summary>
/// LINE 事件 DTO
/// </summary>
public record LineEventDto
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("replyToken")]
    public required string ReplyToken { get; init; }

    [JsonPropertyName("source")]
    public required LineSourceDto Source { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("message")]
    public LineMessageDto? Message { get; init; }

    [JsonPropertyName("postback")]
    public LinePostbackDto? Postback { get; init; }
}

/// <summary>
/// LINE 訊息來源 DTO
/// </summary>
public record LineSourceDto
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("userId")]
    public required string UserId { get; init; }
}

/// <summary>
/// LINE 訊息 DTO
/// </summary>
public record LineMessageDto
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

/// <summary>
/// LINE Postback DTO
/// </summary>
public record LinePostbackDto
{
    [JsonPropertyName("data")]
    public required string Data { get; init; }
}
