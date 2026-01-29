namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 處理人員實體
/// </summary>
public class Handler
{
    /// <summary>
    /// 主鍵識別碼
    /// </summary>
    public int Id { get; private set; }
    
    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// LINE User ID（用於接收通知）
    /// </summary>
    public string? LineUserId { get; set; }
    
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
    /// 負責的來電紀錄
    /// </summary>
    public List<CallRecord> CallRecords { get; set; } = new();
    
    /// <summary>
    /// 系統對應關係
    /// </summary>
    public List<HandlerMapping> HandlerMappings { get; set; } = new();
    
    // ===== 業務邏輯方法 =====
    
    public static Handler Create(string name, string? lineUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("姓名不可為空", nameof(name));
        
        return new Handler
        {
            Name = name.Trim(),
            LineUserId = lineUserId?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateInfo(string name, string? lineUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("姓名不可為空", nameof(name));
        
        Name = name.Trim();
        LineUserId = lineUserId?.Trim();
    }
    
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
