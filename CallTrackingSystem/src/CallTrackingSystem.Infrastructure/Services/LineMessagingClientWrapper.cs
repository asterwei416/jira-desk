using Line.Messaging;
using Microsoft.Extensions.Configuration;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// LINE Messaging Client 包裝
/// </summary>
public class LineMessagingClientWrapper : ILineMessagingClient
{
    private readonly LineMessagingClient _client;

    public LineMessagingClientWrapper(IConfiguration configuration)
    {
        var channelAccessToken = configuration["LineMessaging:ChannelAccessToken"] ?? string.Empty;
        _client = new LineMessagingClient(channelAccessToken);
    }

    public Task PushMessageAsync(
        string to,
        IEnumerable<ISendMessage> messages,
        CancellationToken cancellationToken = default)
    {
        return _client.PushMessageAsync(to, messages.ToList());
    }
}
