namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// LINE Bot 訊息處理器介面
/// </summary>
public interface ILineBotMessageHandler
{
    /// <summary>
    /// 處理文字訊息
    /// </summary>
    Task HandleTextMessageAsync(string lineUserId, string messageText, string replyToken);

    /// <summary>
    /// 處理 Postback 事件（Quick Reply 按鈕）
    /// </summary>
    Task HandlePostbackAsync(string lineUserId, string postbackData, string replyToken);
}
