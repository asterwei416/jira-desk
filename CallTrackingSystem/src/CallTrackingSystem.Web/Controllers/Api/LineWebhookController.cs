using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers.Api;

/// <summary>
/// LINE Webhook Controller
/// </summary>
[ApiController]
[Route("api/line")]
public class LineWebhookController : ControllerBase
{
    private readonly ILineBotMessageHandler _messageHandler;
    private readonly ILogger<LineWebhookController> _logger;

    public LineWebhookController(
        ILineBotMessageHandler messageHandler,
        ILogger<LineWebhookController> logger)
    {
        _messageHandler = messageHandler;
        _logger = logger;
    }

    /// <summary>
    /// LINE Webhook 接收端點
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Webhook([FromBody] LineWebhookEventDto request)
    {
        try
        {
            if (request?.Events == null || request.Events.Count == 0)
            {
                return Ok(new { message = "沒有事件需要處理" });
            }

            // 處理每個事件
            foreach (var evt in request.Events)
            {
                try
                {
                    switch (evt.Type)
                    {
                        case "message":
                            if (evt.Message?.Type == "text" && !string.IsNullOrWhiteSpace(evt.Message.Text))
                            {
                                await _messageHandler.HandleTextMessageAsync(
                                    evt.Source.UserId,
                                    evt.Message.Text,
                                    evt.ReplyToken);
                            }
                            break;

                        case "postback":
                            if (evt.Postback != null)
                            {
                                await _messageHandler.HandlePostbackAsync(
                                    evt.Source.UserId,
                                    evt.Postback.Data,
                                    evt.ReplyToken);
                            }
                            break;

                        default:
                            _logger.LogInformation("未處理的事件類型: {EventType}", evt.Type);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "處理 LINE 事件失敗: EventType={EventType}, UserId={UserId}",
                        evt.Type, evt.Source.UserId);
                }
            }

            return Ok(new { message = "事件處理完成" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE Webhook 處理失敗");
            return BadRequest(new { error = "處理失敗" });
        }
    }
}
