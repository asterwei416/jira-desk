# 資料模型設計：客服來電問題紀錄與分析系統

**日期**: 2026-01-28  
**基於**: [spec.md](./spec.md) 的 7 個核心實體  
**參考**: [research.md](./research.md) 的 EF Core 最佳實踐

---

## 實體關係圖（ER Diagram）

```
┌─────────────────┐         ┌──────────────────┐
│  InquirySystem  │1       *│   CallRecord     │
│                 ├─────────┤                  │
│ - Id (PK)       │         │ - Id (PK)        │
│ - Name          │         │ - Subject        │
│ - IsActive      │         │ - Content        │
│ - CreatedAt     │         │ - Status         │
└─────────────────┘         │ - UrgencyLevel   │
                            │ - ContactName    │
         ┌──────────────────┤ - ContactPhone   │
         │                  │ - CreatedAt      │
         │                  │ - LockedByUserId │
         │                  │ - LockedAt       │
         │                  │ - RowVersion     │
         │                  └──────────────────┘
         │                           │
         │                           │1
         │                           │
         │                           │*
         │                  ┌──────────────────┐
         │                  │ ChangeHistory    │
         │                  │                  │
         │                  │ - Id (PK)        │
         │                  │ - FieldName      │
         │                  │ - OldValue       │
         │                  │ - NewValue       │
         │                  │ - ChangedAt      │
         │                  │ - ChangedByUserId│
         │                  └──────────────────┘
         │
         │*               *┌──────────────────┐
         └─────────────────┤ Handler          │
                           │                  │
                           │ - Id (PK)        │
                           │ - Name           │
                           │ - LineUserId     │
                           │ - IsActive       │
                           └──────────────────┘
                                    │1
                                    │
                                    │*
                           ┌──────────────────┐
                           │ HandlerMapping   │
                           │                  │
                           │ - Id (PK)        │
                           │ - HandlerId (FK) │
                           │ - InquirySystemId│
                           │ - CreatedAt      │
                           └──────────────────┘
                                    │*
                                    │
                                    │1
                           ┌──────────────────┐
                           │ InquirySystem    │
                           │ (已在上方)       │
                           └──────────────────┘

         ┌──────────────────┐
         │ NotificationLog  │
         │                  │
         │ - Id (PK)        │
         │ - CallRecordId   │
         │ - LineUserId     │
         │ - MessageType    │
         │ - Success        │
         │ - ErrorMessage   │
         │ - SentAt         │
         └──────────────────┘

         ┌──────────────────┐
         │ User             │
         │                  │
         │ - Id (PK)        │
         │ - Username       │
         │ - PasswordHash   │
         │ - Name           │
         │ - Role           │
         │ - LineUserId     │
         │ - IsActive       │
         │ - CreatedAt      │
         └──────────────────┘
```

---

## 1. CallRecord（來電紀錄）

### 實體類別

```csharp
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
        
        // 記錄變更歷史（由 Service 層處理）
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

/// <summary>
/// 處理狀態枚舉
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// 待處理
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// 處理中
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// 已關閉
    /// </summary>
    Closed = 3
}

/// <summary>
/// 緊急程度枚舉
/// </summary>
public enum UrgencyLevel
{
    /// <summary>
    /// 低（一般諮詢）
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// 中（影響使用）
    /// </summary>
    Medium = 1,
    
    /// <summary>
    /// 高（系統無法使用）
    /// </summary>
    High = 2
}
```

### EF Core 配置

```csharp
namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class CallRecordConfiguration : IEntityTypeConfiguration<CallRecord>
{
    public void Configure(EntityTypeBuilder<CallRecord> builder)
    {
        builder.ToTable("CallRecords");
        builder.HasKey(x => x.Id);
        
        // 欄位設定
        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(150);
        
        builder.Property(x => x.ContactName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.ContactPhone)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(x => x.FaqReference)
            .HasMaxLength(500);
        
        builder.Property(x => x.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.LockedByUserId)
            .HasMaxLength(50);
        
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.UrgencyLevel)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt)
            .IsRequired();
        
        // 並發控制
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
        
        // 索引策略
        builder.HasIndex(x => new { x.CreatedAt, x.Status, x.UrgencyLevel })
            .HasDatabaseName("IX_CallRecords_Search")
            .IsDescending(true, false, false); // CreatedAt 降序
        
        builder.HasIndex(x => x.InquirySystemId)
            .HasDatabaseName("IX_CallRecords_InquirySystem");
        
        builder.HasIndex(x => x.LockedByUserId)
            .HasDatabaseName("IX_CallRecords_Lock")
            .HasFilter("[LockedByUserId] IS NOT NULL"); // 僅索引已鎖定的紀錄
        
        // 關聯關係
        builder.HasOne(x => x.InquirySystem)
            .WithMany(x => x.CallRecords)
            .HasForeignKey(x => x.InquirySystemId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.Handlers)
            .WithMany(x => x.CallRecords)
            .UsingEntity(j => j.ToTable("CallRecordHandlers"));
        
        builder.HasMany(x => x.ChangeHistories)
            .WithOne(x => x.CallRecord)
            .HasForeignKey(x => x.CallRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasMany(x => x.NotificationLogs)
            .WithOne(x => x.CallRecord)
            .HasForeignKey(x => x.CallRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## 2. InquirySystem（詢問系統）

### 實體類別

```csharp
namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 詢問系統實體
/// </summary>
public class InquirySystem
{
    /// <summary>
    /// 主鍵識別碼
    /// </summary>
    public int Id { get; private set; }
    
    /// <summary>
    /// 系統名稱
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
```

### EF Core 配置

```csharp
public class InquirySystemConfiguration : IEntityTypeConfiguration<InquirySystem>
{
    public void Configure(EntityTypeBuilder<InquirySystem> builder)
    {
        builder.ToTable("InquirySystems");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("IX_InquirySystems_Name");
        
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_InquirySystems_Active");
    }
}
```

---

## 3. Handler（處理人員）

### 實體類別

```csharp
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
    /// 詢問系統對應
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
```

### EF Core 配置

```csharp
public class HandlerConfiguration : IEntityTypeConfiguration<Handler>
{
    public void Configure(EntityTypeBuilder<Handler> builder)
    {
        builder.ToTable("Handlers");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.LineUserId)
            .HasMaxLength(100);
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(x => x.LineUserId)
            .IsUnique()
            .HasDatabaseName("IX_Handlers_LineUserId")
            .HasFilter("[LineUserId] IS NOT NULL");
        
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Handlers_Active");
    }
}
```

---

## 4. HandlerMapping（處理人員對應）

### 實體類別

```csharp
namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// 處理人員與詢問系統對應實體
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
```

### EF Core 配置

```csharp
public class HandlerMappingConfiguration : IEntityTypeConfiguration<HandlerMapping>
{
    public void Configure(EntityTypeBuilder<HandlerMapping> builder)
    {
        builder.ToTable("HandlerMappings");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        // 唯一約束：同一個 Handler 不能重複對應同一個 InquirySystem
        builder.HasIndex(x => new { x.HandlerId, x.InquirySystemId })
            .IsUnique()
            .HasDatabaseName("IX_HandlerMappings_Unique");
        
        builder.HasOne(x => x.Handler)
            .WithMany(x => x.HandlerMappings)
            .HasForeignKey(x => x.HandlerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.InquirySystem)
            .WithMany(x => x.HandlerMappings)
            .HasForeignKey(x => x.InquirySystemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## 5. ChangeHistory（變更歷史）

### 實體類別

```csharp
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
```

### EF Core 配置

```csharp
public class ChangeHistoryConfiguration : IEntityTypeConfiguration<ChangeHistory>
{
    public void Configure(EntityTypeBuilder<ChangeHistory> builder)
    {
        builder.ToTable("ChangeHistories");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.OldValue)
            .HasMaxLength(500);
        
        builder.Property(x => x.NewValue)
            .HasMaxLength(500);
        
        builder.Property(x => x.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.ChangedAt)
            .IsRequired();
        
        // 索引：依來電紀錄和時間查詢變更歷史
        builder.HasIndex(x => new { x.CallRecordId, x.ChangedAt })
            .HasDatabaseName("IX_ChangeHistories_Record");
    }
}
```

---

## 6. NotificationLog（通知記錄）

### 實體類別

```csharp
namespace CallTrackingSystem.Core.Entities;

/// <summary>
/// LINE 通知發送記錄實體
/// </summary>
public class NotificationLog
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
    /// 接收者 LINE User ID
    /// </summary>
    public string LineUserId { get; private set; } = string.Empty;
    
    /// <summary>
    /// 訊息類型
    /// </summary>
    public NotificationType MessageType { get; private set; }
    
    /// <summary>
    /// 是否發送成功
    /// </summary>
    public bool Success { get; private set; }
    
    /// <summary>
    /// 錯誤訊息（失敗時記錄）
    /// </summary>
    public string? ErrorMessage { get; private set; }
    
    /// <summary>
    /// 發送時間
    /// </summary>
    public DateTime SentAt { get; private set; }
    
    // ===== 導覽屬性 =====
    
    public CallRecord CallRecord { get; set; } = null!;
    
    // ===== 業務邏輯方法 =====
    
    public static NotificationLog CreateSuccess(
        int callRecordId,
        string lineUserId,
        NotificationType messageType)
    {
        return new NotificationLog
        {
            CallRecordId = callRecordId,
            LineUserId = lineUserId,
            MessageType = messageType,
            Success = true,
            SentAt = DateTime.UtcNow
        };
    }
    
    public static NotificationLog CreateFailure(
        int callRecordId,
        string lineUserId,
        NotificationType messageType,
        string errorMessage)
    {
        return new NotificationLog
        {
            CallRecordId = callRecordId,
            LineUserId = lineUserId,
            MessageType = messageType,
            Success = false,
            ErrorMessage = errorMessage,
            SentAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// 通知類型枚舉
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 新來電問題
    /// </summary>
    NewCallRecord = 0,
    
    /// <summary>
    /// 狀態更新
    /// </summary>
    StatusUpdate = 1
}
```

### EF Core 配置

```csharp
public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.LineUserId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.MessageType)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.Success)
            .IsRequired();
        
        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);
        
        builder.Property(x => x.SentAt)
            .IsRequired();
        
        // 索引：依來電紀錄查詢通知記錄
        builder.HasIndex(x => x.CallRecordId)
            .HasDatabaseName("IX_NotificationLogs_CallRecord");
        
        // 索引：查詢失敗的通知（供管理者追蹤）
        builder.HasIndex(x => new { x.Success, x.SentAt })
            .HasDatabaseName("IX_NotificationLogs_Failure")
            .HasFilter("[Success] = 0");
    }
}
```

---

## 7. User（使用者）

### 實體類別

```csharp
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
    
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }
    
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

/// <summary>
/// 使用者角色枚舉
/// </summary>
public enum UserRole
{
    /// <summary>
    /// 一般客服人員（只能新增和編輯自己的紀錄）
    /// </summary>
    Staff = 0,
    
    /// <summary>
    /// 管理者（完整權限）
    /// </summary>
    Admin = 1
}
```

### EF Core 配置

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.LineUserId)
            .HasMaxLength(100);
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");
        
        builder.HasIndex(x => x.LineUserId)
            .IsUnique()
            .HasDatabaseName("IX_Users_LineUserId")
            .HasFilter("[LineUserId] IS NOT NULL");
    }
}
```

---

## 資料庫初始化種子資料

```csharp
namespace CallTrackingSystem.Infrastructure.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();
        
        // 檢查是否已有資料
        if (context.InquirySystems.Any())
        {
            return; // 已初始化
        }
        
        // 詢問系統
        var systems = new[]
        {
            InquirySystem.Create("Google 表單系統"),
            InquirySystem.Create("Email 系統"),
            InquirySystem.Create("客戶管理系統"),
            InquirySystem.Create("財務系統"),
            InquirySystem.Create("其他")
        };
        context.InquirySystems.AddRange(systems);
        context.SaveChanges();
        
        // 預設管理者帳號（密碼: Admin@123）
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var admin = User.Create("admin", adminPasswordHash, "系統管理員", UserRole.Admin);
        context.Users.Add(admin);
        context.SaveChanges();
    }
}
```

---

## 索引策略總結

| 資料表 | 索引名稱 | 欄位 | 用途 |
|--------|---------|------|------|
| CallRecords | IX_CallRecords_Search | CreatedAt DESC, Status, UrgencyLevel | 主要搜尋查詢 |
| CallRecords | IX_CallRecords_InquirySystem | InquirySystemId | 依系統篩選 |
| CallRecords | IX_CallRecords_Lock | LockedByUserId (Filtered) | 鎖定狀態查詢 |
| InquirySystems | IX_InquirySystems_Name | Name (Unique) | 系統名稱唯一性 |
| InquirySystems | IX_InquirySystems_Active | IsActive | 篩選啟用系統 |
| Handlers | IX_Handlers_LineUserId | LineUserId (Unique, Filtered) | LINE 綁定唯一性 |
| Handlers | IX_Handlers_Active | IsActive | 篩選啟用人員 |
| HandlerMappings | IX_HandlerMappings_Unique | HandlerId, InquirySystemId (Unique) | 防止重複對應 |
| ChangeHistories | IX_ChangeHistories_Record | CallRecordId, ChangedAt | 變更歷史查詢 |
| NotificationLogs | IX_NotificationLogs_CallRecord | CallRecordId | 通知記錄查詢 |
| NotificationLogs | IX_NotificationLogs_Failure | Success, SentAt (Filtered) | 失敗通知追蹤 |
| Users | IX_Users_Username | Username (Unique) | 登入帳號唯一性 |
| Users | IX_Users_LineUserId | LineUserId (Unique, Filtered) | LINE 綁定唯一性 |

---

## 估算資料量與效能考量

### 初期規模（第一年）

- **使用者**: 20-50 人
- **詢問系統**: 5-10 個
- **處理人員**: 10-20 人
- **來電紀錄**: 每日 50-100 筆 → 年約 18,000-36,000 筆
- **變更歷史**: 每筆紀錄平均 3 次修改 → 年約 54,000-108,000 筆
- **通知記錄**: 每筆紀錄通知 2-3 人 → 年約 36,000-108,000 筆

### 資料保留策略

- **來電紀錄**: 永久保留（或 5 年後歸檔）
- **變更歷史**: 與來電紀錄同步
- **通知記錄**: 保留 1 年（可定期清理）

### 效能預估

- **主搜尋查詢** (IX_CallRecords_Search): < 50ms（10,000 筆以內）
- **詳情頁面載入** (含關聯資料): < 100ms
- **Excel 報表生成** (1,000 筆): < 2 秒
- **LINE 通知發送**: < 500ms/人

---

## 憲法檢查

✅ **I. 程式碼品質標準**
- 實體類別遵循 SRP（單一職責原則），業務邏輯封裝在 Entity 內
- EF Core Configuration 分離，保持 Entity 乾淨
- 使用 Factory Method 和 Guard Clauses 提升可讀性

✅ **II. 測試優先開發**
- 實體類別設計可測試，業務邏輯方法獨立
- Repository 介面設計支援 Mock

✅ **III. 使用者體驗一致性**
- 資料驗證在 Entity 層統一處理
- 錯誤訊息使用繁體中文

✅ **IV. 效能要求**
- 索引策略針對主要查詢優化
- 使用 RowVersion 實現樂觀鎖定，避免資料庫阻塞
- 支援分頁和篩選，避免全表掃描

✅ **V. 文件語言標準**
- 所有註解和文檔使用繁體中文

---

## 下一步

Phase 1 持續進行：
1. ✅ data-model.md 完成
2. ⏳ 建立 API Contracts (OpenAPI 3.0 規格)
3. ⏳ 建立 quickstart.md
4. ⏳ 更新 AGENTS.md
