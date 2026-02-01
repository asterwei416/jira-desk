using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CallTrackingSystem.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CallTrackingSystem.IntegrationTests.Services;

/// <summary>
/// LINE Login 整合測試（Mock LINE API）
/// </summary>
public class LineLoginServiceTests
{
    [Fact]
    public async Task GetUserProfileAsync_WhenSuccess_ShouldReturnProfile()
    {
        var handler = new FakeHttpMessageHandler(request =>
        {
            if (request.RequestUri!.ToString().Contains("/oauth2/v2.1/token"))
            {
                var tokenBody = JsonSerializer.Serialize(new { access_token = "access-token" });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tokenBody, Encoding.UTF8, "application/json")
                };
            }

            if (request.RequestUri!.ToString().Contains("/v2/profile"))
            {
                var profileBody = JsonSerializer.Serialize(new { userId = "U123", displayName = "測試使用者" });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(profileBody, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = CreateService(handler);

        var profile = await service.GetUserProfileAsync("code", "https://localhost:5001/api/auth/line/callback");

        Assert.Equal("U123", profile.LineUserId);
        Assert.Equal("測試使用者", profile.DisplayName);
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenTokenFails_ShouldThrow()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var service = CreateService(handler);

        var act = async () => await service.GetUserProfileAsync("code", "https://localhost:5001/api/auth/line/callback");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal("LINE Login 授權失敗", exception.Message);
    }

    private static LineLoginService CreateService(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:Login:ChannelId"] = "channel-id",
                ["Line:Login:ChannelSecret"] = "channel-secret"
            })
            .Build();

        var client = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(client);
        return new LineLoginService(factory, configuration, NullLogger<LineLoginService>.Instance);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
