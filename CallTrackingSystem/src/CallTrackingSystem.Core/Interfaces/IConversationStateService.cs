using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 對話狀態管理服務介面（In-Memory）
/// </summary>
public interface IConversationStateService
{
    /// <summary>
    /// 啟動新對話流程
    /// </summary>
    Task StartConversationAsync(string lineUserId);

    /// <summary>
    /// 取得對話狀態
    /// </summary>
    Task<ConversationStateDto?> GetConversationAsync(string lineUserId);

    /// <summary>
    /// 更新對話狀態
    /// </summary>
    Task UpdateConversationAsync(ConversationStateDto conversation);

    /// <summary>
    /// 清除對話狀態
    /// </summary>
    Task ClearConversationAsync(string lineUserId);

    /// <summary>
    /// 取得所有過期的對話（超過 5 分鐘未活動）
    /// </summary>
    Task<List<string>> GetExpiredConversationsAsync();

    /// <summary>
    /// 批量清除對話
    /// </summary>
    Task ClearMultipleConversationsAsync(List<string> lineUserIds);
}
