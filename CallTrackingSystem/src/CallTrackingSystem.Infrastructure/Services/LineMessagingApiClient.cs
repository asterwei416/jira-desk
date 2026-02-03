using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace CallTrackingSystem.Infrastructure.Services;

/// <summary>
/// LINE Messaging API Client 介面
/// </summary>
public interface ILineMessagingApiClient
{
    /// <summary>
    /// 推送訊息給指定使用者
    /// </summary>
    Task<bool> PushMessageAsync(string lineUserId, string message);
    
    /// <summary>
    /// 推送 Flex Message 給指定使用者
    /// </summary>
    Task<bool> PushFlexMessageAsync(string lineUserId, object flexMessage);
    
    /// <summary>
    /// 回覆訊息（Reply Message API）
    /// </summary>
    Task<bool> ReplyMessageAsync(string replyToken, string message);
    
    /// <summary>
    /// 驗證 Webhook 簽章
    /// </summary>
    bool VerifySignature(string requestBody, string signature);
}

/// <summary>
/// LINE Messaging API HTTP Client
/// </summary>
public class LineMessagingApiClient : ILineMessagingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _channelAccessToken;
    private readonly string _channelSecret;
    private readonly ILogger<LineMessagingApiClient> _logger;

    public LineMessagingApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LineMessagingApiClient> logger)
    {
        _httpClient = httpClient;
        _channelAccessToken = configuration["LineMessaging:ChannelAccessToken"] 
            ?? throw new InvalidOperationException("LINE Messaging ChannelAccessToken 未設定");
        _channelSecret = configuration["LineMessaging:ChannelSecret"] 
            ?? throw new InvalidOperationException("LINE Messaging ChannelSecret 未設定");
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://api.line.me/v2/bot/");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _channelAccessToken);
    }

    public async Task<bool> PushMessageAsync(string lineUserId, string message)
    {
        try
        {
            var request = new
            {
                to = lineUserId,
                messages = new[] { new { type = "text", text = message } }
            };

            var response = await _httpClient.PostAsJsonAsync("message/push", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("LINE Push Message 失敗: {StatusCode}, {Error}", 
                    response.StatusCode, error);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE Push Message 發生例外: {LineUserId}", lineUserId);
            return false;
        }
    }

    public async Task<bool> PushFlexMessageAsync(string lineUserId, object flexMessage)
    {
        try
        {
            var request = new
            {
                to = lineUserId,
                messages = new[] { flexMessage }
            };

            var response = await _httpClient.PostAsJsonAsync("message/push", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("LINE Push Flex Message 失敗: {StatusCode}, {Error}", 
                    response.StatusCode, error);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE Push Flex Message 發生例外: {LineUserId}", lineUserId);
            return false;
        }
    }

    public async Task<bool> ReplyMessageAsync(string replyToken, string message)
    {
        try
        {
            var request = new
            {
                replyToken = replyToken,
                messages = new[] { new { type = "text", text = message } }
            };

            var response = await _httpClient.PostAsJsonAsync("message/reply", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("LINE Reply Message 失敗: {StatusCode}, {Error}", 
                    response.StatusCode, error);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE Reply Message 發生例外");
            return false;
        }
    }

    public bool VerifySignature(string requestBody, string signature)
    {
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
            var computedSignature = Convert.ToBase64String(hash);
            
            return computedSignature == signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE Webhook 簽章驗證發生例外");
            return false;
        }
    }
}
