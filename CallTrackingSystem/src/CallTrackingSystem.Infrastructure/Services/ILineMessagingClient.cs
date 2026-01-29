using Line.Messaging;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// LINE Messaging Client 介面（便於測試）
/// </summary>
public interface ILineMessagingClient
{
    Task PushMessageAsync(
        string to,
        IEnumerable<ISendMessage> messages,
        CancellationToken cancellationToken = default);
}
