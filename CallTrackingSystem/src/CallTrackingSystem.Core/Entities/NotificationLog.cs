namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// LINE 通知發送記錄實體
/// </summary>
public class NotificationLog
{
    /// <summary>
    /// 主鍵識別碼
    /// </summary>
    public int Id { get; private set; }
    
    /// <summary>
    /// 來電紀錄外鍵
    /// </summary>
    public int CallRecordId { get; private set; }
    
    /// <summary>
    /// 接收者 LINE User ID
    /// </summary>
    public string LineUserId { get; private set; } = string.Empty;
    
    /// <summary>
    /// 訊息類型（文字訊息或 Flex Message）
    /// </summary>
    public NotificationMessageType MessageType { get; private set; }
    
    /// <summary>
    /// 是否成功發送
    /// </summary>
    public bool Success { get; private set; }
    
    /// <summary>
    /// 錯誤訊息（失敗時記錄）
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 發送時間
    /// </summary>
    public DateTime SentAt { get; private set; }
    
    // ===== 導覽屬性 =====
    
    public CallRecord CallRecord { get; set; } = null!;
    
    // ===== 業務邏輯方法 =====
    
    public static NotificationLog CreateSuccess(
        int callRecordId,
        string lineUserId,
        NotificationMessageType messageType)
    {
        return new NotificationLog
        {
            CallRecordId = callRecordId,
            LineUserId = lineUserId,
            MessageType = messageType,
            Success = true,
            SentAt = DateTime.UtcNow
        };
    }
    
    public static NotificationLog CreateFailure(
        int callRecordId,
        string lineUserId,
        NotificationMessageType messageType,
        string errorMessage)
    {
        return new NotificationLog
        {
            CallRecordId = callRecordId,
            LineUserId = lineUserId,
            MessageType = messageType,
            Success = false,
            ErrorMessage = errorMessage,
            SentAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// 通知訊息類型枚舉
/// </summary>
public enum NotificationMessageType
{
    /// <summary>
    /// 文字訊息
    /// </summary>
    Text = 0,
    
    /// <summary>
    /// Flex Message（結構化訊息）
    /// </summary>
    FlexMessage = 1
}
