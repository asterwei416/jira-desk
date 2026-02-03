using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.DTOs;

/// <summary>
/// LINE Bot 對話暫存表單資料
/// </summary>
public class CallRecordFormData
{
    /// <summary>
    /// 問題標題
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 問題內容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 問題所屬單位 ID
    /// </summary>
    public int? InquirySystemId { get; set; }

    /// <summary>
    /// 緊急程度
    /// </summary>
    public UrgencyLevel? UrgencyLevel { get; set; }

    /// <summary>
    /// 聯絡人姓名
    /// </summary>
    public string? ContactName { get; set; }

    /// <summary>
    /// 聯絡電話
    /// </summary>
    public string? ContactPhone { get; set; }
}
