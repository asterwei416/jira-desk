# LINE 整合模式記憶

**版本**: 1.0.0  
**建立日期**: 2026-01-29  
**完成專案**: 客服來電問題紀錄與分析系統  
**狀態**: ✅ 驗證完成

---

## 📋 概述

本文件記錄 .NET Core 8 + ASP.NET Core 專案中整合 LINE Messaging API 的完整模式，可作為未來類似專案的參考範本。

---

## 🏗️ 架構設計模式

### 三層架構整合方式

```
Web Layer (ASP.NET Core MVC/WebAPI)
├── Controllers/ 
│   └── 建立、更新來電紀錄時觸發通知

Core Layer (Business Logic)
├── Entities/
│   ├── CallRecord (來電紀錄)
│   └── NotificationLog (通知日誌)
├── Interfaces/
│   ├── ILineNotificationService (定義合約)
│   └── INotificationLogService
└── Services/
    └── NotificationLogService (業務邏輯)

Infrastructure Layer (Data Access & External Services)
├── Services/
│   ├── LineNotificationService (實作通知邏輯)
│   └── LineMessagingClientWrapper (SDK 包裝)
├── Repositories/
│   └── NotificationLogRepository (資料持久化)
└── Data/
    └── DbContext (EF Core 配置)
```

### 服務分層原則

| 層級 | 職責 | 範例 |
|------|------|------|
| **Controller** | HTTP 請求處理、驗證 | 建立來電紀錄時調用通知 |
| **Service (Core)** | 業務邏輯、編排 | 查詢通知歷史、驗證權限 |
| **Service (Infrastructure)** | 外部系統整合 | 呼叫 LINE API、錯誤處理 |
| **Repository** | 資料持久化 | CRUD NotificationLog |

**關鍵原則**: 
- Core 層不依賴 Infrastructure 層具體實作
- 使用依賴注入實現鬆耦合
- 所有 LINE API 呼叫應在 Infrastructure 層

---

## 🔧 實作步驟清單

### Step 1: 定義實體模型（Core 層）

**檔案**: `Core/Entities/NotificationLog.cs`

需包含以下欄位：
```csharp
- Id (PK)
- CallRecordId (FK)
- LineUserId (接收者)
- MessageType (enum: FlexMessage, TextMessage)
- Success (bool)
- ErrorMessage (失敗訊息)
- SentAt (時戳)
```

**關鍵決策**:
- 使用 `NotificationMessageType` enum（而不是 string）
- 記錄所有通知嘗試（成功 + 失敗）
- SentAt 使用 `DateTime.UtcNow`

### Step 2: 建立 LINE SDK 包裝類（Infrastructure 層）

**檔案**: `Infrastructure/Services/LineMessagingClientWrapper.cs`

職責：
- 將 NuGet 的 `Line.Messaging` 包裝成單一職責的介面
- 隱藏 SDK 的複雜性
- 便於單元測試

```csharp
public interface ILineMessagingClient
{
    Task PushMessageAsync(
        string userId, 
        IList<ISendMessage> messages, 
        CancellationToken ct = default);
}
```

**重要**: 
- 所有 API 呼叫應該非同步
- 支援 CancellationToken
- 不在介面層定義 Flex Message 構建邏輯

### Step 3: 實作通知服務（Infrastructure 層）

**檔案**: `Infrastructure/Services/LineNotificationService.cs`

實作 `ILineNotificationService`:
```csharp
public interface ILineNotificationService
{
    Task SendCallRecordNotificationAsync(
        CallRecord callRecord,
        IReadOnlyCollection<string> lineUserIds,
        CancellationToken cancellationToken = default);
}
```

**重要邏輯**:
- 批次迴圈：逐個發送給每位處理人員
- 錯誤隔離：一人失敗不影響他人
- 例外捕捉：
  - 特別處理 `LineResponseException`（LINE API 錯誤）
  - 通用 `Exception` 作為後備
- 自動日誌記錄：每次通知嘗試（成功/失敗）

**Flex Message 構建**:
- 建立專屬方法 `BuildCallRecordFlexMessage()`
- 包含客戶資訊、詢問系統、狀態等
- 支援可點擊的「查看詳情」按鈕（href 指向詳情頁面）

### Step 4: 建立通知日誌服務（Core 層）

**檔案**: `Core/Services/NotificationLogService.cs`

職責：
- 記錄通知成功/失敗
- 查詢特定來電的通知歷史
- 查詢失敗通知（管理用途）

```csharp
public interface INotificationLogService
{
    Task LogNotificationAsync(
        long callRecordId,
        string lineUserId,
        NotificationMessageType messageType,
        bool success,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<NotificationLogDto>> 
        GetNotificationsByCallRecordAsync(
            long callRecordId,
            CancellationToken cancellationToken = default);
}
```

### Step 5: 實作資料庫倉儲（Infrastructure 層）

**檔案**: `Infrastructure/Repositories/Repositories.cs`

操作：
- 插入新通知記錄
- 按 CallRecordId 查詢
- 按 Success = false 查詢（失敗管理）

使用 EF Core:
```csharp
public async Task<NotificationLog> AddAsync(NotificationLog log, CancellationToken ct = default)
{
    _context.NotificationLogs.Add(log);
    await _context.SaveChangesAsync(ct);
    return log;
}
```

### Step 6: 在 Controller 中整合

**觸發時機**: 建立或更新來電紀錄時

```csharp
[HttpPost]
public async Task<IActionResult> CreateCallRecord(CreateCallRecordRequest request)
{
    var callRecord = CallRecord.Create(request);
    await _callRecordRepository.AddAsync(callRecord);
    
    // 查詢應通知的處理人員
    var lineUserIds = await _handlerMappingService
        .GetHandlersByInquirySystemAsync(callRecord.InquirySystemId);
    
    // 發送 LINE 通知（背景任務，不阻塞回應）
    _ = _lineNotificationService.SendCallRecordNotificationAsync(
        callRecord, 
        lineUserIds);
    
    return CreatedAtAction(nameof(GetCallRecord), new { id = callRecord.Id }, callRecord);
}
```

**重要**: 
- 不等待通知完成（使用 `_` 丟棄 Task）
- 通知失敗不應導致 HTTP 請求失敗
- 考慮使用背景任務隊列（如 Hangfire，但本專案暫不需要）

### Step 7: 配置依賴注入

**檔案**: `Web/Program.cs`

```csharp
// Core Services
builder.Services.AddScoped<INotificationLogService, NotificationLogService>();

// Infrastructure Services
builder.Services.AddScoped<ILineMessagingClient, LineMessagingClientWrapper>();
builder.Services.AddScoped<ILineNotificationService, LineNotificationService>();

// Configuration
var lineMessagingConfig = configuration.GetSection("LineMessaging");
builder.Services
    .Configure<LineMessagingOptions>(lineMessagingConfig);
```

### Step 8: 配置文件設定

**檔案**: `Web/appsettings.Development.json`

```json
{
  "LineMessaging": {
    "ChannelAccessToken": "YOUR_CHANNEL_ACCESS_TOKEN",
    "ChannelSecret": "YOUR_CHANNEL_SECRET",
    "DetailUrlBase": "http://localhost:5099/call-records"
  }
}
```

**安全性注意**:
- Token 不應存在版本控制中
- 使用 User Secrets（開發） 或 Azure Key Vault（生產）
- `appsettings.json` 應存儲預設值或佔位符

---

## 📊 資料庫遷移

### EF Core Fluent API 配置

**檔案**: `Infrastructure/Data/ApplicationDbContext.cs`

```csharp
modelBuilder.Entity<NotificationLog>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).ValueGeneratedOnAdd();
    
    entity.Property(e => e.LineUserId)
        .IsRequired()
        .HasMaxLength(100);
    
    entity.Property(e => e.MessageType)
        .HasConversion<string>();
    
    entity.Property(e => e.ErrorMessage)
        .HasMaxLength(500);
    
    entity.Property(e => e.SentAt)
        .IsRequired();
    
    entity.HasOne<CallRecord>()
        .WithMany()
        .HasForeignKey(e => e.CallRecordId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // 索引：提速通知查詢
    entity.HasIndex(e => new { e.CallRecordId, e.SentAt })
        .IsDescending(false, true); // CallRecordId ASC, SentAt DESC
});
```

### Migration 指令

```powershell
# 建立遷移
dotnet ef migrations add AddNotificationLog `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web

# 套用
dotnet ef database update `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web
```

---

## ✅ 測試策略

### 單元測試（Core 層）

**檔案**: `tests/CallTrackingSystem.UnitTests/Services/NotificationLogServiceTests.cs`

```csharp
[Fact]
public async Task LogNotificationAsync_WithValidInput_ShouldSaveToRepository()
{
    // Arrange
    var mockRepository = new Mock<INotificationLogRepository>();
    var service = new NotificationLogService(mockRepository.Object);
    
    // Act
    await service.LogNotificationAsync(
        callRecordId: 1,
        lineUserId: "U123",
        messageType: NotificationMessageType.FlexMessage,
        success: true);
    
    // Assert
    mockRepository.Verify(
        r => r.AddAsync(It.IsAny<NotificationLog>(), default),
        Times.Once);
}
```

### 整合測試（Infrastructure 層）

**檔案**: `tests/CallTrackingSystem.IntegrationTests/Services/LineNotificationServiceTests.cs`

```csharp
[Fact]
public async Task SendCallRecordNotificationAsync_WithValidUsers_ShouldLogSuccess()
{
    // Arrange
    var callRecord = CallRecord.Create(...);
    var lineUserIds = new[] { "U123", "U456" };
    
    // Act
    await _lineNotificationService.SendCallRecordNotificationAsync(
        callRecord, 
        lineUserIds);
    
    // Assert
    var logs = await _dbContext.NotificationLogs
        .Where(l => l.CallRecordId == callRecord.Id)
        .ToListAsync();
    
    Assert.Equal(2, logs.Count);
    Assert.All(logs, log => Assert.True(log.Success));
}
```

**Mock LINE API**:

```csharp
var mockClient = new Mock<ILineMessagingClient>();
mockClient
    .Setup(c => c.PushMessageAsync(It.IsAny<string>(), It.IsAny<IList<ISendMessage>>(), default))
    .ReturnsAsync(Unit.Value);
```

### 契約測試

**API 端點**: `GET /api/call-records/{id}/notifications`

驗證：
- 回應包含所有 NotificationLog 欄位
- 正確的 HTTP 狀態碼（200, 404）
- 時間戳排序（最新優先）

---

## 🚨 常見問題排除

### 問題 1: 沒有收到 LINE 通知

**檢查清單**:
1. Bot 已加入好友列表？
   - 登入 LINE Developers Console → QR Code → 掃描
2. Channel Access Token 正確？
   - 檢查 `appsettings.Development.json`
3. NotificationLog 顯示成功或失敗？
   - 查詢資料庫確認記錄

**偵錯方式**:
```powershell
# 查詢失敗的通知
SELECT * FROM NotificationLogs WHERE Success = 0 ORDER BY SentAt DESC;

# 檢查應用程式日誌
# 應該看到 LineNotificationService 的 Log 訊息
```

### 問題 2: 429 Too Many Requests（限流）

**原因**: LINE API 有速率限制

**解決**:
- 不要短時間內大量發送
- 考慮實作間隔機制（100ms per user）
- 使用背景隊列系統（Hangfire）

### 問題 3: Flex Message 格式錯誤（400 Bad Request）

**檢查**:
- JSON 結構是否符合 [LINE Flex Message 規格](https://developers.line.biz/en/docs/messaging-api/using-flex-messages/)
- 所有必需欄位都存在
- 字串長度限制（按規格）

---

## 🔒 安全性考量

### 1. Token 管理

```csharp
// ❌ 錯誤：Token 在代碼中
var token = "d47d4afa...";

// ✅ 正確：環境變數或密鑰庫
var token = configuration["LineMessaging:ChannelAccessToken"];
```

### 2. User Secret（本地開發）

```bash
dotnet user-secrets init
dotnet user-secrets set "LineMessaging:ChannelAccessToken" "YOUR_TOKEN"
dotnet user-secrets set "LineMessaging:ChannelSecret" "YOUR_SECRET"
```

### 3. Production 佈署

使用 Azure Key Vault：
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### 4. LINE User ID 隱私

- 不要在前端暴露 USER ID
- 通知應該單向發送（User → Bot）
- 避免 User ID 在日誌中洩露（適當遮蔽）

---

## 📈 效能最佳實踐

### 1. 非同步 I/O

```csharp
// ✅ 正確：不阻塞主執行緒
_ = _lineNotificationService.SendCallRecordNotificationAsync(callRecord, userIds);
return CreatedAtAction(...); // 立即回應用戶
```

### 2. 批次操作

```csharp
// 不要逐個等待，而是併行發送
var tasks = lineUserIds.Select(userId =>
    _lineNotificationService.SendCallRecordNotificationAsync(
        callRecord,
        new[] { userId }));

await Task.WhenAll(tasks); // 在適當的時機等待
```

### 3. 資料庫索引

```csharp
entity.HasIndex(e => new { e.CallRecordId, e.SentAt })
    .IsDescending(false, true);
```

---

## 🔄 擴展性考量

### 新增通知類型

若要支援其他通知管道（Email、SMS），遵循此模式：

```csharp
// 1. 新增 Enum
public enum NotificationMessageType
{
    FlexMessage,
    TextMessage,
    EmailMessage, // 新增
    SmsMessage    // 新增
}

// 2. 新增 Service Interface
public interface IEmailNotificationService
{
    Task SendAsync(...);
}

// 3. 統一調用點
public async Task NotifyHandlersAsync(CallRecord record, IReadOnlyCollection<string> ids)
{
    await _lineNotificationService.SendAsync(...);
    await _emailNotificationService.SendAsync(...);
    // 同時通知多個管道
}
```

---

## 📚 相關參考資料

- [LINE Messaging API 文檔](https://developers.line.biz/en/docs/messaging-api/)
- [LINE Flex Message 格式](https://developers.line.biz/en/docs/messaging-api/using-flex-messages/)
- [Line.Messaging NuGet](https://www.nuget.org/packages/Line.Messaging/)
- [ASP.NET Core 依賴注入](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [EF Core 配置最佳實踐](https://docs.microsoft.com/en-us/ef/core/modeling/)

---

## 🎯 快速檢查清單（新專案應用）

應用此模式到新專案時，確保完成：

- [ ] 定義 `NotificationLog` 實體和 EF Core 配置
- [ ] 建立 `ILineNotificationService` 介面（Core 層）
- [ ] 實作 `LineNotificationService`（Infrastructure 層）
- [ ] 包裝 LINE SDK 為 `ILineMessagingClient`
- [ ] 建立 `INotificationLogService` 和實作
- [ ] 在 `Program.cs` 註冊依賴注入
- [ ] 配置 `appsettings.json` 與環境變數
- [ ] 建立資料庫遷移
- [ ] 在 Controller 中整合通知邏輯
- [ ] 編寫單元測試和整合測試
- [ ] 進行端對端測試（實際發送通知）
- [ ] 實裝安全配置（User Secrets / Key Vault）
- [ ] 文檔化 API 契約

---

**更新記錄**:
| 日期 | 版本 | 說明 |
|------|------|------|
| 2026-01-29 | 1.0.0 | 初版建立，基於客服系統完成的 LINE 整合 |
