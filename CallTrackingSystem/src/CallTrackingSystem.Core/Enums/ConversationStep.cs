namespace CallTrackingSystem.Core.Enums;

/// <summary>
/// 對話流程步驟
/// </summary>
public enum ConversationStep
{
    /// <summary>
    /// 閒置狀態（無進行中的對話）
    /// </summary>
    Idle = 0,

    /// <summary>
    /// 等待輸入問題標題
    /// </summary>
    AwaitingSubject = 1,

    /// <summary>
    /// 等待輸入問題內容
    /// </summary>
    AwaitingContent = 2,

    /// <summary>
    /// 等待選擇問題所屬單位
    /// </summary>
    AwaitingInquirySystem = 3,

    /// <summary>
    /// 等待選擇緊急程度
    /// </summary>
    AwaitingUrgencyLevel = 4,

    /// <summary>
    /// 等待輸入聯絡人姓名
    /// </summary>
    AwaitingContactName = 5,

    /// <summary>
    /// 等待輸入聯絡電話
    /// </summary>
    AwaitingContactPhone = 6,

    /// <summary>
    /// 等待確認送出
    /// </summary>
    AwaitingConfirmation = 7
}
