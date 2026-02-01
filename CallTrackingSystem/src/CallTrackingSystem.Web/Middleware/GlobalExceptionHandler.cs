using System.Net;
using System.Text.Json;
using CallTrackingSystem.Web.Models;

namespace CallTrackingSystem.Web.Middleware;

/// <summary>
/// 全域例外處理中介軟體
/// </summary>
public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "業務錯誤: {Message}", ex.Message);
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message, "BUSINESS_ERROR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未預期錯誤");
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "系統發生未預期錯誤", "UNEXPECTED_ERROR");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string message, string code)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ErrorResponse
        {
            Error = message,
            Code = code,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
