namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 處理人員對應實體（Handler 與 InquirySystem 的多對多關聯表）
/// </summary>
public class HandlerMapping
{
    /// <summary>
    /// 主鍵識別碼
    /// </summary>
    public int Id { get; private set; }
    
    /// <summary>
    /// 處理人員外鍵
    /// </summary>
    public int HandlerId { get; private set; }
    
    /// <summary>
    /// 詢問系統外鍵
    /// </summary>
    public int InquirySystemId { get; private set; }
    
    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    // ===== 導覽屬性 =====
    
    public Handler Handler { get; set; } = null!;
    public InquirySystem InquirySystem { get; set; } = null!;
    
    // ===== 業務邏輯方法 =====
    
    public static HandlerMapping Create(int handlerId, int inquirySystemId)
    {
        return new HandlerMapping
        {
            HandlerId = handlerId,
            InquirySystemId = inquirySystemId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
