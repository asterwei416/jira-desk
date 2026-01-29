namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 詢問系統（如：帳務系統、會員系統）實體
/// </summary>
public class InquirySystem
{
    /// <summary>
    /// 主鍵識別碼
    /// </summary>
    public int Id { get; private set; }
    
    /// <summary>
    /// 系統名稱（如「帳務系統」、「會員系統」）
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// 是否啟用
    /// </summary>
    public bool IsActive { get; private set; }
    
    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    
    // ===== 導覽屬性 =====
    
    /// <summary>
    /// 相關來電紀錄
    /// </summary>
    public List<CallRecord> CallRecords { get; set; } = new();
    
    /// <summary>
    /// 處理人員對應
    /// </summary>
    public List<HandlerMapping> HandlerMappings { get; set; } = new();
    
    // ===== 業務邏輯方法 =====
    
    public static InquirySystem Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("系統名稱不可為空", nameof(name));
        
        return new InquirySystem
        {
            Name = name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("系統名稱不可為空", nameof(name));
        
        Name = name.Trim();
    }
    
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
