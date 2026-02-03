# Data Model: LINE Bot 整合功能

**Feature**: LINE Bot Integration  
**Branch**: `2-line-bot-integration`  
**Date**: 2026-02-03  
**Status**: Phase 1 Complete

---

## 概述

本文檔定義 LINE Bot 整合功能的資料模型設計，包含：
1. **Entity 擴充**: 擴充現有 `User` Entity，新增 LINE 綁定欄位
2. **對話狀態實體**: 定義 In-Memory 對話狀態結構（不持久化至資料庫）
3. **Repository 擴充**: 擴充 `IUserRepository` 介面，新增 LINE 相關查詢方法
4. **EF Core Configuration**: 設定欄位限制、索引、資料驗證規則

**設計原則**:
- ✅ 遵循棕地專案最小化變更原則
- ✅ 所有新增欄位均為 `nullable`，不影響現有資料
- ✅ 使用 EF Core Fluent API 分離 Configuration
- ✅ 新增唯一索引防止重複綁定

---

## 1. Entity 擴充

### 1.1 User Entity（擴充）

**檔案位置**: `CallTrackingSystem.Core/Entities/User.cs`

**變更類型**: **[EXTEND]** - 新增 LINE 綁定欄位，不修改現有欄位

```csharp
// CallTrackingSystem.Core/Entities/User.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.Entities
{
    public class User
    {
        // ==================== [EXISTING] 現有欄位（不修改）====================
        
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = null!;
        
        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = null!;
        
        [Required]
        public UserRole Role { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // ==================== [NEW] LINE 整合欄位 ====================
        
        /// <summary>
        /// LINE User ID（由 LINE Platform 提供，固定 33 字元）
        /// 可為 null（使用者未綁定 LINE）
        /// </summary>
        [MaxLength(50)]  // 實際 33 字元，預留空間
        public string? LineUserId { get; set; }
        
        /// <summary>
        /// LINE Display Name（LINE 帳號顯示名稱，最多 20 字元）
        /// 可為 null（使用者未綁定 LINE）
        /// </summary>
        [MaxLength(100)]  // 預留空間
        public string? LineDisplayName { get; set; }
        
        /// <summary>
        /// LINE 綁定時間戳記
        /// 可為 null（使用者未綁定 LINE）
        /// </summary>
        public DateTime? LineBoundAt { get; set; }
        
        // ==================== [EXISTING] Navigation Properties（不修改）====================
        
        // 未來可能新增的關聯：public virtual ICollection<CallRecord> CreatedCallRecords { get; set; }
    }
}
```

**設計說明**:
1. **LineUserId**: 
   - LINE Platform 提供的唯一識別碼（格式：`U` + 32 字元十六進位）
   - 實際長度固定 33 字元，設定 `varchar(50)` 預留空間
   - 可為 null（未綁定狀態）
   - 新增唯一索引（允許多個 NULL 值）

2. **LineDisplayName**: 
   - LINE 帳號的公開顯示名稱
   - 用於 UI 顯示「已綁定：XXX」
   - 可為 null（未綁定狀態）

3. **LineBoundAt**: 
   - 綁定時間戳記，用於追蹤綁定歷史
   - 可為 null（未綁定狀態）

---

### 1.2 EF Core Configuration（新增）

**檔案位置**: `CallTrackingSystem.Infrastructure/Data/Configurations/UserConfiguration.cs`

**變更類型**: **[NEW]** - 新建 Fluent API Configuration 檔案

```csharp
// CallTrackingSystem.Infrastructure/Data/Configurations/UserConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // ==================== [EXISTING] 現有設定（若已存在則保留）====================
            
            builder.ToTable("Users");
            
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.HasIndex(u => u.Username)
                .IsUnique();
                
            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
                
            builder.Property(u => u.Role)
                .IsRequired()
                .HasConversion<string>();  // Enum 儲存為字串
                
            builder.Property(u => u.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");  // SQL Server 預設值
                
            // ==================== [NEW] LINE 整合欄位設定 ====================
            
            // LineUserId 欄位設定
            builder.Property(u => u.LineUserId)
                .HasMaxLength(50)
                .IsUnicode(false)  // varchar (ASCII only)
                .IsRequired(false);  // 可為 null
                
            // LineUserId 唯一索引（防止一個 LINE 帳號綁定多個系統帳號）
            builder.HasIndex(u => u.LineUserId)
                .IsUnique()
                .HasDatabaseName("IX_Users_LineUserId")
                .HasFilter("LineUserId IS NOT NULL");  // 允許多個 NULL 值（SQL Server Filtered Index）
                
            // LineDisplayName 欄位設定
            builder.Property(u => u.LineDisplayName)
                .HasMaxLength(100)
                .IsUnicode(true)  // nvarchar（支援多國語言）
                .IsRequired(false);  // 可為 null
                
            // LineBoundAt 欄位設定
            builder.Property(u => u.LineBoundAt)
                .IsRequired(false);  // 可為 null
        }
    }
}
```

**註冊 Configuration**（若尚未註冊）:

```csharp
// CallTrackingSystem.Infrastructure/Data/ApplicationDbContext.cs [EXTEND]
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // [EXISTING] 現有 Configuration（不修改）
    // ...
    
    // [NEW] 註冊 UserConfiguration
    modelBuilder.ApplyConfiguration(new UserConfiguration());
}
```

---

## 2. 對話狀態實體（In-Memory Only）

### 2.1 ConversationState（不持久化）

**檔案位置**: `CallTrackingSystem.Core/DTOs/ConversationStateDto.cs`

**變更類型**: **[NEW]** - 新建 DTO 類別（僅用於記憶體儲存）

```csharp
// CallTrackingSystem.Core/DTOs/ConversationStateDto.cs
using System;
using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.DTOs
{
    /// <summary>
    /// LINE Bot 對話狀態（僅儲存於記憶體，不持久化至資料庫）
    /// </summary>
    public class ConversationState
    {
        /// <summary>
        /// LINE User ID（Dictionary Key）
        /// </summary>
        public string LineUserId { get; set; } = null!;
        
        /// <summary>
        /// 當前對話步驟
        /// </summary>
        public ConversationStep CurrentStep { get; set; }
        
        /// <summary>
        /// 暫存表單資料
        /// </summary>
        public CallRecordFormData FormData { get; set; } = new();
        
        /// <summary>
        /// 對話開始時間
        /// </summary>
        public DateTime StartedAt { get; set; }
        
        /// <summary>
        /// 最後活動時間（用於逾時檢查）
        /// </summary>
        public DateTime LastActivityAt { get; set; }
    }
    
    /// <summary>
    /// 對話流程步驟定義
    /// </summary>
    public enum ConversationStep
    {
        /// <summary>
        /// 閒置狀態（無對話進行中）
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 等待使用者輸入問題標題
        /// </summary>
        AwaitingSubject = 1,
        
        /// <summary>
        /// 等待使用者輸入問題內容
        /// </summary>
        AwaitingContent = 2,
        
        /// <summary>
        /// 等待使用者選擇問題所屬單位（InquirySystem）
        /// </summary>
        AwaitingInquirySystem = 3,
        
        /// <summary>
        /// 等待使用者選擇緊急程度（UrgencyLevel）
        /// </summary>
        AwaitingUrgencyLevel = 4,
        
        /// <summary>
        /// 等待使用者輸入聯絡人姓名
        /// </summary>
        AwaitingContactName = 5,
        
        /// <summary>
        /// 等待使用者輸入聯絡電話
        /// </summary>
        AwaitingContactPhone = 6,
        
        /// <summary>
        /// 等待使用者確認送出（顯示摘要）
        /// </summary>
        AwaitingConfirmation = 7
    }
    
    /// <summary>
    /// 回報單暫存資料（對話過程中累積）
    /// </summary>
    public class CallRecordFormData
    {
        public string? Subject { get; set; }
        public string? Content { get; set; }
        public int? InquirySystemId { get; set; }
        public UrgencyLevel? UrgencyLevel { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
    }
}
```

**狀態轉移圖**:

```
Idle (閒置)
  ↓ 使用者輸入「回報問題」
AwaitingSubject (等待問題標題)
  ↓ 使用者輸入標題
AwaitingContent (等待問題內容)
  ↓ 使用者輸入內容
AwaitingInquirySystem (等待選擇所屬單位)
  ↓ 使用者點擊 Quick Reply 選擇單位
AwaitingUrgencyLevel (等待選擇緊急程度)
  ↓ 使用者點擊 Quick Reply 選擇緊急程度
AwaitingContactName (等待輸入聯絡人姓名)
  ↓ 使用者輸入姓名
AwaitingContactPhone (等待輸入聯絡電話)
  ↓ 使用者輸入電話
AwaitingConfirmation (等待確認送出)
  ↓ 使用者點擊「確認送出」
呼叫 CallRecordService.CreateAsync() → 建立回報單
  ↓
回到 Idle (對話結束)
```

**逾時處理**:
- 對話狀態在記憶體中保留最多 5 分鐘（從 `LastActivityAt` 計算）
- BackgroundService 每 1 分鐘檢查一次，清除逾時對話
- 使用者可隨時輸入「取消」中斷流程

---

## 3. Repository 擴充

### 3.1 IUserRepository 介面擴充

**檔案位置**: `CallTrackingSystem.Core/Interfaces/IUserRepository.cs`

**變更類型**: **[EXTEND]** - 新增 LINE 相關查詢方法

```csharp
// CallTrackingSystem.Core/Interfaces/IUserRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces
{
    public interface IUserRepository
    {
        // ==================== [EXISTING] 現有方法（不修改）====================
        
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(int id);
        
        // ==================== [NEW] LINE 整合方法 ====================
        
        /// <summary>
        /// 根據 LINE User ID 查詢已綁定的使用者
        /// </summary>
        /// <param name="lineUserId">LINE User ID（33 字元）</param>
        /// <returns>已綁定的使用者，若未找到則返回 null</returns>
        Task<User?> GetByLineUserIdAsync(string lineUserId);
        
        /// <summary>
        /// 檢查 LINE User ID 是否已被綁定
        /// </summary>
        /// <param name="lineUserId">LINE User ID（33 字元）</param>
        /// <returns>true 表示已被綁定，false 表示未被綁定</returns>
        Task<bool> IsLineUserIdBoundAsync(string lineUserId);
        
        /// <summary>
        /// 查詢所有已綁定 LINE 的使用者（選配功能，用於管理員後台）
        /// </summary>
        /// <returns>已綁定 LINE 的使用者清單</returns>
        Task<IEnumerable<User>> GetUsersWithLineBoundAsync();
    }
}
```

---

### 3.2 UserRepository 實作擴充

**檔案位置**: `CallTrackingSystem.Infrastructure/Repositories/UserRepository.cs`

**變更類型**: **[EXTEND]** - 實作 LINE 相關查詢方法

```csharp
// CallTrackingSystem.Infrastructure/Repositories/UserRepository.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Infrastructure.Data;

namespace CallTrackingSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==================== [EXISTING] 現有方法（不顯示，保持不變）====================
        
        // GetByIdAsync, GetByUsernameAsync, GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync
        // ...（現有實作不修改）
        
        // ==================== [NEW] LINE 整合方法實作 ====================
        
        /// <summary>
        /// 根據 LINE User ID 查詢已綁定的使用者
        /// </summary>
        public async Task<User?> GetByLineUserIdAsync(string lineUserId)
        {
            if (string.IsNullOrWhiteSpace(lineUserId))
            {
                return null;
            }
            
            return await _context.Users
                .FirstOrDefaultAsync(u => u.LineUserId == lineUserId);
        }
        
        /// <summary>
        /// 檢查 LINE User ID 是否已被綁定
        /// </summary>
        public async Task<bool> IsLineUserIdBoundAsync(string lineUserId)
        {
            if (string.IsNullOrWhiteSpace(lineUserId))
            {
                return false;
            }
            
            return await _context.Users
                .AnyAsync(u => u.LineUserId == lineUserId);
        }
        
        /// <summary>
        /// 查詢所有已綁定 LINE 的使用者（選配功能）
        /// </summary>
        public async Task<IEnumerable<User>> GetUsersWithLineBoundAsync()
        {
            return await _context.Users
                .Where(u => u.LineUserId != null)
                .OrderByDescending(u => u.LineBoundAt)
                .ToListAsync();
        }
    }
}
```

**效能考量**:
1. **GetByLineUserIdAsync**: 
   - 使用唯一索引 `IX_Users_LineUserId`，查詢效能為 O(log n)
   - 預期回應時間 < 10ms（小型資料庫）

2. **IsLineUserIdBoundAsync**: 
   - 使用 `AnyAsync` 提前終止查詢（找到第一筆即返回）
   - 同樣受益於唯一索引

3. **GetUsersWithLineBoundAsync**: 
   - 使用 `Where` 篩選 `LineUserId IS NOT NULL`
   - 小型團隊預估綁定使用者數量 < 50 人，無需分頁

---

## 4. Database Migration

### 4.1 Migration 腳本

**檔案位置**: `CallTrackingSystem.Infrastructure/Migrations/20260203_AddLineIntegration.cs`

**變更類型**: **[NEW]** - 新建 EF Core Migration

```csharp
// CallTrackingSystem.Infrastructure/Migrations/20260203_AddLineIntegration.cs
using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CallTrackingSystem.Infrastructure.Migrations
{
    public partial class AddLineIntegration : Migration
    {
        /// <summary>
        /// 套用 Migration：新增 LINE 整合欄位
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 新增 LineUserId 欄位（varchar(50), nullable, ASCII only）
            migrationBuilder.AddColumn<string>(
                name: "LineUserId",
                table: "Users",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true);

            // 新增 LineDisplayName 欄位（nvarchar(100), nullable）
            migrationBuilder.AddColumn<string>(
                name: "LineDisplayName",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // 新增 LineBoundAt 欄位（datetime2, nullable）
            migrationBuilder.AddColumn<DateTime>(
                name: "LineBoundAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            // 新增唯一索引（允許多個 NULL 值）
            migrationBuilder.CreateIndex(
                name: "IX_Users_LineUserId",
                table: "Users",
                column: "LineUserId",
                unique: true,
                filter: "[LineUserId] IS NOT NULL");  // SQL Server Filtered Index
        }

        /// <summary>
        /// 回滾 Migration：移除 LINE 整合欄位
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 移除索引
            migrationBuilder.DropIndex(
                name: "IX_Users_LineUserId",
                table: "Users");

            // 移除欄位
            migrationBuilder.DropColumn(
                name: "LineUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LineDisplayName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LineBoundAt",
                table: "Users");
        }
    }
}
```

**執行 Migration**:

```powershell
# 1. 建立 Migration
dotnet ef migrations add AddLineIntegration `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web

# 2. 套用 Migration
dotnet ef database update `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web

# 3. 驗證欄位新增成功（SQL Server）
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
AND COLUMN_NAME LIKE 'Line%';

# 預期輸出:
# COLUMN_NAME        | DATA_TYPE | CHARACTER_MAXIMUM_LENGTH | IS_NULLABLE
# ----------------------------------------------------------------------
# LineUserId         | varchar   | 50                       | YES
# LineDisplayName    | nvarchar  | 100                      | YES
# LineBoundAt        | datetime2 | NULL                     | YES
```

---

### 4.2 Migration 回滾計劃

若需回滾此 Migration（例如功能上線後出現問題）：

```powershell
# 1. 確認當前 Migration 名稱
dotnet ef migrations list `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web

# 2. 回滾至前一個 Migration
dotnet ef database update [PreviousMigrationName] `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web

# 3. 驗證欄位已移除
SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME LIKE 'Line%';
# 預期輸出: 0

# 4. 移除 Migration 檔案
dotnet ef migrations remove `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web
```

---

## 5. 資料驗證規則

### 5.1 欄位驗證

| 欄位名稱 | 驗證規則 | 錯誤訊息 |
|---------|---------|---------|
| LineUserId | 格式：`U[0-9a-f]{32}` | 「LINE User ID 格式不正確」 |
| LineDisplayName | 長度：1-100 字元 | 「LINE 顯示名稱長度必須在 1-100 字元之間」 |
| LineBoundAt | 必須為 UTC 時間 | 「綁定時間格式不正確」 |

### 5.2 業務規則驗證

**在 LineLoginService 中實作**:

```csharp
// CallTrackingSystem.Core/Services/LineLoginService.cs
public async Task<bool> BindUserAsync(int userId, string lineUserId, string lineDisplayName)
{
    // 1. 驗證 LINE User ID 格式
    if (!IsValidLineUserId(lineUserId))
    {
        throw new InvalidOperationException("LINE User ID 格式不正確");
    }
    
    // 2. 檢查 LINE 帳號是否已被綁定
    if (await _userRepository.IsLineUserIdBoundAsync(lineUserId))
    {
        throw new InvalidOperationException("此 LINE 帳號已被其他使用者綁定");
    }
    
    // 3. 檢查使用者是否存在
    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null)
    {
        throw new InvalidOperationException("使用者不存在");
    }
    
    // 4. 檢查使用者角色（訪客禁止綁定）
    if (user.Role == UserRole.Guest)
    {
        throw new InvalidOperationException("訪客帳號無法綁定 LINE");
    }
    
    // 5. 更新綁定資訊
    user.LineUserId = lineUserId;
    user.LineDisplayName = lineDisplayName;
    user.LineBoundAt = DateTime.UtcNow;
    
    await _userRepository.UpdateAsync(user);
    return true;
}

private bool IsValidLineUserId(string lineUserId)
{
    // LINE User ID 格式：U + 32 字元十六進位（小寫）
    return System.Text.RegularExpressions.Regex.IsMatch(
        lineUserId, 
        @"^U[0-9a-f]{32}$");
}
```

---

## 6. 資料模型關聯圖

```
┌─────────────────────────────────────────────────────────────┐
│                        User Entity                          │
├─────────────────────────────────────────────────────────────┤
│ [EXISTING] Id (PK)                                          │
│ [EXISTING] Username (Unique)                                │
│ [EXISTING] PasswordHash                                     │
│ [EXISTING] Role (Enum: Admin/Manager/Staff/Guest)           │
│ [EXISTING] CreatedAt                                        │
│ [EXISTING] UpdatedAt                                        │
│ ───────────────────────────────────────────────────────── │
│ [NEW] LineUserId (Unique, Nullable, Indexed)                │
│ [NEW] LineDisplayName (Nullable)                            │
│ [NEW] LineBoundAt (Nullable)                                │
└─────────────────────────────────────────────────────────────┘
                    │ 1                       ↑
                    │                         │ N
                    │                         │
                    ↓ N                       │ 1
┌──────────────────────────┐      ┌──────────────────────────┐
│ Handler Entity           │      │ ConversationState (DTO)  │
│ [EXISTING]               │      │ [NEW, In-Memory Only]    │
├──────────────────────────┤      ├──────────────────────────┤
│ Id (PK)                  │      │ LineUserId (Key)         │
│ Name                     │      │ CurrentStep (Enum)       │
│ UserId (FK) ───────┐     │      │ FormData (DTO)           │
│ ...                 │     │      │ StartedAt                │
└────────────────────┼─────┘      │ LastActivityAt           │
                     │             └──────────────────────────┘
                     │
                     └─> 查詢路徑：Handler.UserId → User.LineUserId
                         （用於推送通知時檢查處理人員是否已綁定 LINE）
```

**關聯說明**:
1. **Handler → User**: 透過 `Handler.UserId` 外鍵關聯（現有設計）
2. **User ← → LINE Platform**: 透過 `User.LineUserId` 欄位（新增）
3. **ConversationState**: 僅存在於記憶體（ConcurrentDictionary），不持久化

---

## 7. 索引策略

### 7.1 現有索引（不修改）

| 索引名稱 | 類型 | 欄位 | 說明 |
|---------|------|------|------|
| PK_Users | Clustered | Id | 主鍵索引 |
| IX_Users_Username | Unique | Username | 使用者名稱唯一索引 |

### 7.2 新增索引

| 索引名稱 | 類型 | 欄位 | 說明 | Filtered Index |
|---------|------|------|------|---------------|
| IX_Users_LineUserId | Unique | LineUserId | LINE User ID 唯一索引 | `[LineUserId] IS NOT NULL` |

**Filtered Index 說明**:
- SQL Server 支援 Filtered Index，允許多個 NULL 值共存
- 唯一性約束僅適用於非 NULL 值
- 未綁定的使用者（LineUserId = NULL）不會違反唯一性約束

---

## 8. 測試資料（Seed Data）

### 8.1 測試使用者綁定範例

```csharp
// CallTrackingSystem.Infrastructure/Data/DbInitializer.cs [EXTEND]
public static void SeedTestData(ApplicationDbContext context)
{
    // [EXISTING] 現有 Seed Data（不修改）
    // ...
    
    // [NEW] 新增測試 LINE 綁定使用者
    if (!context.Users.Any(u => u.LineUserId != null))
    {
        var testUser = context.Users.FirstOrDefault(u => u.Username == "staff");
        if (testUser != null)
        {
            testUser.LineUserId = "Uabcdef1234567890abcdef1234567890";  // 測試用 LINE User ID
            testUser.LineDisplayName = "測試員工";
            testUser.LineBoundAt = DateTime.UtcNow;
            context.SaveChanges();
        }
    }
}
```

---

## 總結

### Phase 1-A 完成狀態

| 項目 | 狀態 | 說明 |
|------|------|------|
| User Entity 擴充 | ✅ 完成 | 新增 3 個欄位（LineUserId, LineDisplayName, LineBoundAt） |
| EF Core Configuration | ✅ 完成 | 設定欄位限制、唯一索引、Filtered Index |
| IUserRepository 擴充 | ✅ 完成 | 新增 3 個方法（GetByLineUserIdAsync, IsLineUserIdBoundAsync, GetUsersWithLineBoundAsync） |
| UserRepository 實作 | ✅ 完成 | 實作 LINE 相關查詢方法 |
| ConversationState DTO | ✅ 完成 | 定義 In-Memory 對話狀態結構 |
| Migration 腳本 | ✅ 完成 | 提供 Up/Down 方法、回滾計劃 |

### 關鍵設計決策

1. **Nullable 欄位設計**: 所有 LINE 欄位均為 nullable，不影響現有資料
2. **Filtered Index**: 使用 SQL Server Filtered Index，允許多個 NULL 值
3. **In-Memory 對話狀態**: 使用 DTO 而非 Entity，明確標註不持久化
4. **Repository 擴充**: 遵循現有命名慣例（GetByXxxAsync），保持一致性

### 下一步

data-model.md 完成後，可進入 **Phase 1-B: API 契約設計（contracts/）**，產出：
- `contracts/line-bot-api.yaml` - LINE Bot Webhook API 契約
- `contracts/line-login-api.yaml` - LINE Login OAuth 2.0 契約
- `contracts/conversation-flow.yaml` - 對話流程狀態機契約

---

**Generated**: 2026-02-03  
**Status**: Phase 1-A Complete, Ready for Phase 1-B
