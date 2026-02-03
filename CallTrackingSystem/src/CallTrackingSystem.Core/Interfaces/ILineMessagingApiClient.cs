namespace CallTrackingSystem.Core.Interfaces;

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
