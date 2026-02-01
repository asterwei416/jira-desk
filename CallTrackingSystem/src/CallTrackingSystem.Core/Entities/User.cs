using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 使用者實體
/// </summary>
public class User
{
    /// <summary>
    /// 主鍵識別碼（GUID 字串）
    /// </summary>
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 使用者名稱（登入帳號）
    /// </summary>
    public string Username { get; private set; } = string.Empty;
    
    /// <summary>
    /// 密碼雜湊（使用 BCrypt 或 ASP.NET Core Identity）
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;
    
    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    
    /// <summary>
    /// 角色
    /// </summary>
    public UserRole Role { get; private set; }
    
    /// <summary>
    /// LINE User ID（選配，用於 LINE Login 綁定）
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
    
    // ===== 業務邏輯方法 =====
    
    public static User Create(
        string username,
        string passwordHash,
        string name,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("使用者名稱不可為空", nameof(username));
        
        return new User
        {
            Username = username.Trim(),
            PasswordHash = passwordHash,
            Name = name.Trim(),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public void UpdateInfo(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("名稱不可為空", nameof(name));
        
        Name = name.Trim();
    }
    
    public void BindLineAccount(string lineUserId)
    {
        if (string.IsNullOrWhiteSpace(lineUserId))
            throw new ArgumentException("LINE User ID 不可為空", nameof(lineUserId));
        
        LineUserId = lineUserId.Trim();
    }

    public void UnbindLineAccount()
    {
        LineUserId = null;
    }
    
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }
    
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
