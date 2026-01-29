namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 變更歷史實體（審計追蹤）
/// </summary>
public class ChangeHistory
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
    /// 變更欄位名稱
    /// </summary>
    public string FieldName { get; private set; } = string.Empty;
    
    /// <summary>
    /// 舊值
    /// </summary>
    public string? OldValue { get; private set; }
    
    /// <summary>
    /// 新值
    /// </summary>
    public string? NewValue { get; private set; }
    
    /// <summary>
    /// 變更時間
    /// </summary>
    public DateTime ChangedAt { get; private set; }
    
    /// <summary>
    /// 變更者 User ID
    /// </summary>
    public string ChangedByUserId { get; private set; } = string.Empty;
    
    // ===== 導覽屬性 =====
    
    public CallRecord CallRecord { get; set; } = null!;
    
    // ===== 業務邏輯方法 =====
    
    public static ChangeHistory Create(
        int callRecordId,
        string fieldName,
        string? oldValue,
        string? newValue,
        string changedByUserId)
    {
        return new ChangeHistory
        {
            CallRecordId = callRecordId,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedAt = DateTime.UtcNow,
            ChangedByUserId = changedByUserId
        };
    }
}
