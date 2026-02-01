using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// LINE Login 服務
/// </summary>
public class LineLoginService : ILineLoginService
{
    private const string AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
    private const string TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
    private const string ProfileEndpoint = "https://api.line.me/v2/profile";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LineLoginService> _logger;

    public LineLoginService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LineLoginService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string BuildLoginUrl(string state, string redirectUri)
    {
        var settings = GetSettings();
        var scope = Uri.EscapeDataString("profile openid");
        var encodedRedirect = Uri.EscapeDataString(redirectUri);
        var encodedState = Uri.EscapeDataString(state);
        var nonce = Guid.NewGuid().ToString("N");

        return $"{AuthorizationEndpoint}?response_type=code&client_id={settings.ChannelId}&redirect_uri={encodedRedirect}&state={encodedState}&scope={scope}&nonce={nonce}";
    }

    public async Task<LineLoginProfile> GetUserProfileAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var settings = GetSettings();
        var client = _httpClientFactory.CreateClient();

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = settings.ChannelId,
            ["client_secret"] = settings.ChannelSecret
        };

        var tokenResponse = await client.PostAsync(
            TokenEndpoint,
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var body = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("LINE Login 交換 Token 失敗: {Body}", body);
            throw new InvalidOperationException("LINE Login 授權失敗");
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<LineTokenResponse>(cancellationToken: cancellationToken);
        if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("LINE Login 授權失敗");
        }

        var profileRequest = new HttpRequestMessage(HttpMethod.Get, ProfileEndpoint);
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var profileResponse = await client.SendAsync(profileRequest, cancellationToken);
        if (!profileResponse.IsSuccessStatusCode)
        {
            var body = await profileResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("LINE Login 取得個人資料失敗: {Body}", body);
            throw new InvalidOperationException("LINE Login 取得使用者資料失敗");
        }

        var profile = await profileResponse.Content.ReadFromJsonAsync<LineProfileResponse>(cancellationToken: cancellationToken);
        if (profile == null || string.IsNullOrWhiteSpace(profile.UserId))
        {
            throw new InvalidOperationException("LINE Login 取得使用者資料失敗");
        }

        return new LineLoginProfile
        {
            LineUserId = profile.UserId,
            DisplayName = profile.DisplayName ?? "LINE 使用者"
        };
    }

    private LineLoginSettings GetSettings()
    {
        var section = _configuration.GetSection("Line:Login");
        var channelId = section["ChannelId"] ?? string.Empty;
        var channelSecret = section["ChannelSecret"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(channelId) || string.IsNullOrWhiteSpace(channelSecret))
        {
            throw new InvalidOperationException("LINE Login 設定不完整");
        }

        return new LineLoginSettings(channelId, channelSecret);
    }

    private sealed record LineLoginSettings(string ChannelId, string ChannelSecret);

    private sealed record LineTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed record LineProfileResponse
    {
        [JsonPropertyName("userId")]
        public string? UserId { get; init; }
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; init; }
    }
}
