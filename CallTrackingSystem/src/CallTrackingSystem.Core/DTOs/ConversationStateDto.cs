using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.DTOs;

/// <summary>
/// 對話狀態 DTO（僅記憶體使用，不持久化至資料庫）
/// </summary>
public class ConversationStateDto
{
    /// <summary>
    /// LINE User ID（Dictionary Key）
    /// </summary>
    public string LineUserId { get; set; } = string.Empty;

    /// <summary>
    /// 當前對話步驟
    /// </summary>
    public ConversationStep CurrentStep { get; set; }

    /// <summary>
    /// 暫存表單資料
    /// </summary>
    public CallRecordFormData FormData { get; set; } = new();

    /// <summary>
    /// 對話開始時間
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// 最後活動時間（用於逾時檢查）
    /// </summary>
    public DateTime LastActivityAt { get; set; }
}
