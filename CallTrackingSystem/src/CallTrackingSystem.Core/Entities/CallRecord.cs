using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 客服來電問題紀錄實體
/// </summary>
public class CallRecord
{
    /// <summary>
    /// 主鍵識別碼
    /// </summary>
    public int Id { get; private set; }
    
    /// <summary>
    /// 問題主旨（最多 50 字）
    /// </summary>
    public string Subject { get; private set; } = string.Empty;
    
    /// <summary>
    /// 問題內容（最多 150 字）
    /// </summary>
    public string Content { get; private set; } = string.Empty;
    
    /// <summary>
    /// 處理狀態
    /// </summary>
    public ProcessStatus Status { get; private set; }
    
    /// <summary>
    /// 緊急程度
    /// </summary>
    public UrgencyLevel UrgencyLevel { get; private set; }
    
    /// <summary>
    /// 聯絡人姓名
    /// </summary>
    public string ContactName { get; private set; } = string.Empty;
    
    /// <summary>
    /// 聯絡電話
    /// </summary>
    public string ContactPhone { get; private set; } = string.Empty;
    
    /// <summary>
    /// FAQ 連結或說明
    /// </summary>
    public string? FaqReference { get; set; }
    
    /// <summary>
    /// 建立時間（來電日期時間）
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime UpdatedAt { get; private set; }
    
    /// <summary>
    /// 建立者 User ID
    /// </summary>
    public string CreatedByUserId { get; private set; } = string.Empty;
    
    /// <summary>
    /// 編輯鎖定者 User ID（null 表示未鎖定）
    /// </summary>
    public string? LockedByUserId { get; set; }
    
    /// <summary>
    /// 編輯鎖定時間
    /// </summary>
    public DateTime? LockedAt { get; set; }
    
    /// <summary>
    /// 並發控制版本號（EF Core RowVersion）
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
    
    // ===== 導覽屬性 =====
    
    /// <summary>
    /// 所屬詢問系統
    /// </summary>
    public InquirySystem InquirySystem { get; set; } = null!;
    
    /// <summary>
    /// 詢問系統外鍵
    /// </summary>
    public int InquirySystemId { get; set; }
    
    /// <summary>
    /// 負責處理人員清單
    /// </summary>
    public List<Handler> Handlers { get; set; } = new();
    
    /// <summary>
    /// 變更歷史紀錄
    /// </summary>
    public List<ChangeHistory> ChangeHistories { get; set; } = new();
    
    /// <summary>
    /// 通知發送記錄
    /// </summary>
    public List<NotificationLog> NotificationLogs { get; set; } = new();
    
    // ===== 業務邏輯方法 =====
    
    /// <summary>
    /// 建立新來電紀錄（Factory Method）
    /// </summary>
    public static CallRecord Create(
        string subject,
        string content,
        int inquirySystemId,
        UrgencyLevel urgencyLevel,
        string contactName,
        string contactPhone,
        string createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("主旨不可為空", nameof(subject));
        if (subject.Length > 50)
            throw new ArgumentException("主旨不可超過 50 字", nameof(subject));
        if (content.Length > 150)
            throw new ArgumentException("內容不可超過 150 字", nameof(content));
        
        var record = new CallRecord
        {
            Subject = subject.Trim(),
            Content = content.Trim(),
            InquirySystemId = inquirySystemId,
            UrgencyLevel = urgencyLevel,
            ContactName = contactName.Trim(),
            ContactPhone = contactPhone.Trim(),
            Status = ProcessStatus.Pending,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return record;
    }
    
    /// <summary>
    /// 更新處理狀態
    /// </summary>
    public void UpdateStatus(ProcessStatus newStatus, string userId)
    {
        if (Status == newStatus) return;
        
        var oldStatus = Status;
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// 更新紀錄內容
    /// </summary>
    public void Update(
        string subject,
        string content,
        UrgencyLevel urgencyLevel,
        string contactName,
        string contactPhone,
        string? faqReference)
    {
        if (subject.Length > 50)
            throw new ArgumentException("主旨不可超過 50 字", nameof(subject));
        if (content.Length > 150)
            throw new ArgumentException("內容不可超過 150 字", nameof(content));
        
        Subject = subject.Trim();
        Content = content.Trim();
        UrgencyLevel = urgencyLevel;
        ContactName = contactName.Trim();
        ContactPhone = contactPhone.Trim();
        FaqReference = faqReference?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// 取得編輯鎖定
    /// </summary>
    public bool TryAcquireLock(string userId, int lockTimeoutMinutes = 30)
    {
        // 如果已被其他人鎖定且未過期
        if (LockedByUserId != null && 
            LockedByUserId != userId && 
            LockedAt.HasValue &&
            DateTime.UtcNow <= LockedAt.Value.AddMinutes(lockTimeoutMinutes))
        {
            return false;
        }
        
        LockedByUserId = userId;
        LockedAt = DateTime.UtcNow;
        return true;
    }
    
    /// <summary>
    /// 釋放編輯鎖定
    /// </summary>
    public void ReleaseLock(string userId)
    {
        if (LockedByUserId == userId)
        {
            LockedByUserId = null;
            LockedAt = null;
        }
    }
    
    /// <summary>
    /// 檢查是否被鎖定中
    /// </summary>
    public bool IsLockedByOther(string userId, int lockTimeoutMinutes = 30)
    {
        if (LockedByUserId == null) return false;
        if (LockedByUserId == userId) return false;
        
        if (!LockedAt.HasValue) return false;
        
        return DateTime.UtcNow <= LockedAt.Value.AddMinutes(lockTimeoutMinutes);
    }
}
