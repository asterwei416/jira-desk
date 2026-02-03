using System.Security.Cryptography;
using System.Text;

namespace CallTrackingSystem.Web.Middleware;

/// <summary>
/// LINE Webhook 簽章驗證 Middleware
/// </summary>
public class LineSignatureValidatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LineSignatureValidatorMiddleware> _logger;

    public LineSignatureValidatorMiddleware(
        RequestDelegate next,
        ILogger<LineSignatureValidatorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        // 僅驗證 LINE Webhook endpoint
        if (!context.Request.Path.StartsWithSegments("/api/line/webhook"))
        {
            await _next(context);
            return;
        }

        // 讀取 Request Body
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        // 取得簽章
        var signature = context.Request.Headers["X-Line-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("LINE Webhook 缺少簽章");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "缺少簽章" });
            return;
        }

        // 驗證簽章
        var channelSecret = configuration["Line:Messaging:ChannelSecret"];
        if (string.IsNullOrWhiteSpace(channelSecret))
        {
            _logger.LogError("LINE ChannelSecret 未設定");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "伺服器設定錯誤" });
            return;
        }

        if (!VerifySignature(requestBody, channelSecret, signature))
        {
            _logger.LogWarning("LINE Webhook 簽章驗證失敗");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "簽章驗證失敗" });
            return;
        }

        // 簽章驗證通過，繼續處理
        await _next(context);
    }

    private static bool VerifySignature(string requestBody, string channelSecret, string signature)
    {
        try
        {
            var key = Encoding.UTF8.GetBytes(channelSecret);
            var body = Encoding.UTF8.GetBytes(requestBody);

            using var hmac = new HMACSHA256(key);
            var hash = hmac.ComputeHash(body);
            var computedSignature = Convert.ToBase64String(hash);

            return string.Equals(signature, computedSignature, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
