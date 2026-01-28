# 技術研究：客服來電問題紀錄與分析系統

**日期**: 2026-01-28  
**目的**: 解決實作計劃中的技術決策點，研究最佳實踐，為 Phase 1 設計提供依據。

---

## 1. .NET Core 8 + EF Core Code First 最佳實踐

### 決策

**Entity 設計模式**: 使用 Rich Domain Model + Fluent API Configuration 分離模式

### 理由

- Rich Domain Model: Entity 類別包含業務邏輯方法（如 `CallRecord.UpdateStatus()`），提升封裝性
- Fluent API 分離: 在 `EntityTypeConfiguration<T>` 類別中定義資料庫映射，保持 Entity 乾淨
- Migration 策略: 使用 Add-Migration 自動生成，但人工審查和調整索引、預設值

### Entity Configuration 範例模式

```csharp
// Entity 類別 (Core/Entities/CallRecord.cs)
public class CallRecord
{
    public int Id { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public ProcessStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // 導覽屬性
    public InquirySystem InquirySystem { get; set; } = null!;
    public int InquirySystemId { get; set; }
    public List<Handler> Handlers { get; set; } = new();
    
    // 業務邏輯方法
    public void UpdateStatus(ProcessStatus newStatus, string userId)
    {
        if (Status == newStatus) return;
        Status = newStatus;
        // 記錄變更歷史...
    }
}

// Fluent API Configuration (Infrastructure/Data/Configurations/CallRecordConfiguration.cs)
public class CallRecordConfiguration : IEntityTypeConfiguration<CallRecord>
{
    public void Configure(EntityTypeBuilder<CallRecord> builder)
    {
        builder.ToTable("CallRecords");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(150);
            
        // 索引策略
        builder.HasIndex(x => new { x.CreatedAt, x.Status, x.UrgencyLevel })
            .HasDatabaseName("IX_CallRecords_Search");
            
        // 關聯關係
        builder.HasOne(x => x.InquirySystem)
            .WithMany(x => x.CallRecords)
            .HasForeignKey(x => x.InquirySystemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 索引策略建議

1. **搜尋查詢組合索引**: `(CreatedAt DESC, Status, UrgencyLevel)` - 支援主要篩選條件
2. **主旨全文檢索**: 考慮使用 `CONTAINS` 或應用層 LIKE 查詢（Azure SQL 支援全文檢索）
3. **外鍵索引**: EF Core 自動建立，無需手動
4. **避免過度索引**: 初期只建立必要索引，根據實際查詢效能調整

### 查詢優化技巧

```csharp
// ❌ 避免 N+1 查詢
var records = _context.CallRecords.ToList(); // 載入所有紀錄
foreach (var record in records)
{
    var system = record.InquirySystem; // 每次觸發額外查詢
}

// ✅ 使用 Include 預先載入
var records = _context.CallRecords
    .Include(x => x.InquirySystem)
    .Include(x => x.Handlers)
    .Where(x => x.Status == ProcessStatus.Pending)
    .ToListAsync();

// ✅ 使用 Select 投影（更高效）
var records = _context.CallRecords
    .Where(x => x.Status == ProcessStatus.Pending)
    .Select(x => new CallRecordListDto
    {
        Id = x.Id,
        Subject = x.Subject,
        SystemName = x.InquirySystem.Name,
        HandlerNames = x.Handlers.Select(h => h.Name).ToList()
    })
    .ToListAsync();
```

### Connection Resiliency

```csharp
// Program.cs 設定
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));
```

### 替代方案評估

| 方案 | 優點 | 缺點 | 選擇 |
|------|------|------|------|
| Anemic Domain Model | 簡單直接 | 業務邏輯分散在 Service 層 | ❌ |
| Rich Domain Model | 封裝性好，符合 DDD | 略微複雜 | ✅ 選擇 |
| Data Annotations | 配置在 Entity 類別內 | Entity 類別混雜映射邏輯 | ❌ |
| Fluent API | 分離關注點 | 需要額外的 Configuration 類別 | ✅ 選擇 |

---

## 2. LINE Messaging API 整合

### 決策

使用 **LINE Messaging API SDK for .NET** + **Flex Message Builder** 模式

### 理由

- 官方 SDK 提供完整的型別安全和 IntelliSense 支援
- Flex Message 格式靈活，可設計美觀的通知卡片
- Push Message API 適合主動通知場景（vs Reply Message 需要 Webhook）

### LINE Bot Channel 設定步驟

1. **建立 Provider 和 Channel**:
   - 登入 LINE Developers Console (https://developers.line.biz/)
   - 建立新 Provider（組織名稱）
   - 建立 Messaging API Channel
   - 記錄 Channel Secret 和 Channel Access Token

2. **基本設定**:
   - 啟用 "Use webhooks": 否（初期不需要雙向對話）
   - 關閉 "Auto-reply messages"
   - 關閉 "Greeting messages"
   - 取得使用者的 LINE User ID（需要使用者先加為好友）

3. **安全性設定**:
   - Channel Access Token 存放於 Azure Key Vault 或 appsettings.json (加密)
   - IP whitelist（選配）

### Flex Message 範本設計

```csharp
// LineNotificationService.cs
public class LineNotificationService : ILineNotificationService
{
    private readonly LineMessagingClient _lineClient;
    
    public LineNotificationService(IConfiguration config)
    {
        var channelAccessToken = config["Line:ChannelAccessToken"];
        _lineClient = new LineMessagingClient(channelAccessToken);
    }
    
    public async Task<bool> SendCallRecordNotificationAsync(
        CallRecord record, 
        List<string> lineUserIds)
    {
        try
        {
            var flexMessage = BuildCallRecordFlexMessage(record);
            
            foreach (var userId in lineUserIds)
            {
                await _lineClient.PushMessageAsync(
                    userId, 
                    new List<ISendMessage> { flexMessage });
            }
            
            return true;
        }
        catch (LineResponseException ex)
        {
            // 記錄失敗原因
            _logger.LogError(ex, "LINE 通知發送失敗: {Message}", ex.Message);
            return false;
        }
    }
    
    private FlexMessage BuildCallRecordFlexMessage(CallRecord record)
    {
        var bubble = new BubbleContainer
        {
            Body = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new List<IFlexComponent>
                {
                    new TextComponent
                    {
                        Text = "新來電問題",
                        Weight = Weight.Bold,
                        Size = ComponentSize.Xl
                    },
                    new BoxComponent
                    {
                        Layout = BoxLayout.Vertical,
                        Margin = Spacing.Lg,
                        Spacing = Spacing.Sm,
                        Contents = new List<IFlexComponent>
                        {
                            CreateInfoRow("詢問系統", record.InquirySystem.Name),
                            CreateInfoRow("主旨", record.Subject),
                            CreateInfoRow("緊急度", record.UrgencyLevel.ToString()),
                            CreateInfoRow("聯絡人", record.ContactName),
                            CreateInfoRow("電話", record.ContactPhone)
                        }
                    }
                }
            },
            Footer = new BoxComponent
            {
                Layout = BoxLayout.Vertical,
                Contents = new List<IFlexComponent>
                {
                    new ButtonComponent
                    {
                        Style = ButtonStyle.Link,
                        Action = new UriAction
                        {
                            Label = "查看詳情",
                            Uri = $"https://your-domain.com/CallRecord/Details/{record.Id}"
                        }
                    }
                }
            }
        };
        
        return new FlexMessage("新來電問題通知")
        {
            Contents = bubble
        };
    }
    
    private BoxComponent CreateInfoRow(string label, string value)
    {
        return new BoxComponent
        {
            Layout = BoxLayout.Baseline,
            Spacing = Spacing.Sm,
            Contents = new List<IFlexComponent>
            {
                new TextComponent
                {
                    Text = label,
                    Color = "#aaaaaa",
                    Size = ComponentSize.Sm,
                    Flex = 1
                },
                new TextComponent
                {
                    Text = value,
                    Wrap = true,
                    Color = "#666666",
                    Size = ComponentSize.Sm,
                    Flex = 5
                }
            }
        };
    }
}
```

### 錯誤處理策略

1. **記錄失敗到 NotificationLog 資料表**
2. **不阻塞主流程**: 通知發送使用 async/await 但捕捉例外
3. **重試策略**: 初期不實作自動重試，記錄失敗日誌供管理者查看
4. **常見錯誤碼處理**:
   - 401: Token 無效 → 檢查設定
   - 400: 請求格式錯誤 → 驗證 Flex Message JSON
   - 429: Rate limit → 記錄警告，稍後重試

### 替代方案評估

| 方案 | 優點 | 缺點 | 選擇 |
|------|------|------|------|
| LINE Notify API | 更簡單，只需 Token | 功能受限，無 Flex Message | ❌ |
| LINE Messaging API | 功能完整，Flex Message 美觀 | 需要建立 Channel，設定較複雜 | ✅ 選擇 |
| 手動 HTTP 請求 | 完全控制 | 需要自行處理序列化和錯誤 | ❌ |

---

## 3. LINE Login 整合

### 決策

使用 **LINE Login v2.1** + **ASP.NET Core Authentication Middleware** 整合模式

### 理由

- OAuth 2.0 標準流程，安全可靠
- .NET Core 內建 OAuth Handler 支援，易於整合
- 取得使用者基本資料（顯示名稱、頭像）用於綁定內部帳號

### LINE Login Channel 設定步驟

1. **建立 LINE Login Channel**:
   - LINE Developers Console → 建立 LINE Login Channel（與 Messaging API 是不同的 Channel）
   - 設定 Callback URL: `https://your-domain.com/signin-line`
   - 記錄 Channel ID 和 Channel Secret

2. **Scope 設定**:
   - 必要: `profile`（取得顯示名稱、頭像）
   - 選配: `openid`, `email`（如果需要 email 綁定）

### OAuth 2.0 流程圖

```
使用者                          系統                           LINE
  |                              |                              |
  |---(1) 點擊 LINE 登入-------->|                              |
  |                              |---(2) 重定向到 LINE--------->|
  |<-------------------------(3) LINE 登入頁面------------------|
  |---(4) 同意授權-------------->|                              |
  |                              |<--(5) 回調 + Authorization Code-|
  |                              |---(6) 交換 Code 取得 Token-->|
  |                              |<--(7) Access Token-----------|
  |                              |---(8) 取得使用者資料-------->|
  |                              |<--(9) 使用者 Profile---------|
  |<--(10) 登入成功/綁定頁面-----|                              |
```

### 實作範例

```csharp
// Program.cs 設定
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "Line";
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    .AddOAuth("Line", options =>
    {
        options.ClientId = builder.Configuration["Line:Login:ClientId"];
        options.ClientSecret = builder.Configuration["Line:Login:ClientSecret"];
        options.CallbackPath = new PathString("/signin-line");
        
        options.AuthorizationEndpoint = "https://access.line.me/oauth2/v2.1/authorize";
        options.TokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
        options.UserInformationEndpoint = "https://api.line.me/v2/profile";
        
        options.Scope.Add("profile");
        options.SaveTokens = true;
        
        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "userId");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "displayName");
        options.ClaimActions.MapJsonKey("picture", "pictureUrl");
        
        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                // 使用 Access Token 取得使用者資料
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                
                var response = await context.Backchannel.SendAsync(request);
                var user = await response.Content.ReadFromJsonAsync<JsonElement>();
                
                context.RunClaimActions(user);
                
                // 檢查是否為首次登入
                var lineUserId = user.GetProperty("userId").GetString();
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var existingUser = await userService.GetByLineUserIdAsync(lineUserId);
                
                if (existingUser == null)
                {
                    // 首次登入，導向綁定頁面
                    context.Properties.Items["RequireBinding"] = "true";
                }
            }
        };
    });

// AuthController.cs
public class AuthController : Controller
{
    [HttpGet]
    public IActionResult LineLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action("LineCallback")
        };
        return Challenge(properties, "Line");
    }
    
    [HttpGet]
    public async Task<IActionResult> LineCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        if (result.Properties.Items.TryGetValue("RequireBinding", out var requireBinding) && requireBinding == "true")
        {
            // 首次登入，導向綁定頁面
            var lineUserId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var displayName = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
            
            return RedirectToAction("Bind", new { lineUserId, displayName });
        }
        
        // 已綁定，直接登入
        return RedirectToAction("Index", "Home");
    }
    
    [HttpGet]
    public IActionResult Bind(string lineUserId, string displayName)
    {
        var model = new BindLineAccountViewModel
        {
            LineUserId = lineUserId,
            DisplayName = displayName
        };
        return View(model);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")] // 需要管理者批准
    public async Task<IActionResult> ApproveBind(BindApprovalModel model)
    {
        await _userService.BindLineAccountAsync(model.UserId, model.LineUserId, model.AssignedRole);
        return RedirectToAction("Index", "Admin");
    }
}
```

### 安全性考量

1. **State 參數防護**: OAuth 自動處理 CSRF 攻擊防護
2. **Token 安全儲存**: 不儲存 Access Token（除非需要代表使用者呼叫 LINE API）
3. **綁定審核**: 首次 LINE 登入需要管理者批准並指派角色
4. **Session 管理**: 使用 ASP.NET Core Identity Cookie，設定適當過期時間

### 替代方案評估

| 方案 | 優點 | 缺點 | 選擇 |
|------|------|------|------|
| LINE Login + Manual HTTP | 完全控制 | 需要自行處理 OAuth 流程 | ❌ |
| LINE Login + .NET OAuth Handler | 標準化，安全性高 | 需要了解 OAuth 原理 | ✅ 選擇 |
| 只用系統內建帳號 | 最簡單 | 缺乏彈性 | ❌ |

---

## 4. Azure SQL 最佳實踐

### 決策

初期使用 **Azure SQL Database - Basic DTU** 或 **Serverless vCore (1 vCore, Auto-pause)**

### 理由

- 初期規模小（20-50 使用者，每日 50-100 筆新紀錄）
- Basic DTU ($5/月) 或 Serverless ($0.52/vCore-hour) 成本低
- 自動備份、高可用性無需額外設定
- 可隨時升級到更高階層

### DTU vs vCore 選擇

| 特性 | Basic DTU (5 DTU) | Serverless (1 vCore) |
|------|-------------------|----------------------|
| 成本 | ~$5/月 固定 | ~$15/月（使用時計費）|
| 效能 | 固定 5 DTU | 1 vCore（可 auto-scale）|
| 自動暫停 | 無 | 有（1小時無活動後暫停）|
| 適用場景 | 持續穩定流量 | 間歇性流量 |
| **建議** | ✅ 選擇（小型系統） | ⚠️ 考量（若夜間無流量）|

### 備份策略

- **自動備份**: Azure SQL 預設啟用，保留 7 天
- **長期保留**: 考慮每月手動備份到 Blob Storage
- **還原測試**: 每季執行一次還原測試

### 連線池設定

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Initial Catalog=CallTrackingDB;Persist Security Info=False;User ID=your-user;Password=your-password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Min Pool Size=5;Max Pool Size=30;"
  }
}
```

- **Min Pool Size=5**: 初期保持 5 個連線活躍（避免冷啟動延遲）
- **Max Pool Size=30**: 限制最大連線數（Basic DTU 支援最多 30 個並發連線）

### 效能監控指標

1. **DTU 使用率**: 應保持在 80% 以下（持續超過則升級）
2. **CPU 使用率**: Serverless 模式下監控 CPU %
3. **查詢效能**: 使用 Azure Portal 的 Query Performance Insight
4. **慢查詢**: 設定閾值（如 >1秒）記錄到日誌

### 成本估算

| 配置 | 月費用 | 適用規模 |
|------|--------|----------|
| Basic (5 DTU) | $5 | 20-50 使用者，輕量查詢 |
| Standard S0 (10 DTU) | $15 | 50-100 使用者，中度查詢 |
| Serverless (1 vCore) | $15-30 | 間歇性使用，自動暫停 |

**初期建議**: Basic DTU $5/月，監控 DTU 使用率後調整

### 替代方案評估

| 方案 | 優點 | 缺點 | 選擇 |
|------|------|------|------|
| SQL Server on VM | 完全控制 | 需要管理備份、更新 | ❌ |
| Azure SQL Database | 全託管，高可用 | 成本略高於 VM | ✅ 選擇 |
| Azure SQL Managed Instance | 相容性最好 | 成本高（$700+/月） | ❌ |

---

## 5. IIS 部署最佳實踐

### 決策

使用 **ASP.NET Core Hosting Bundle** + **In-Process Hosting Model**

### 理由

- In-Process 模式效能更好（無需 Kestrel 反向代理）
- Hosting Bundle 包含 .NET Runtime 和 IIS 模組
- Windows Server 環境原生支援

### IIS 設定檢查清單

1. **安裝前置條件**:
   - ✅ Windows Server 2016+ 或 Windows 10+
   - ✅ IIS 10+ 已啟用
   - ✅ 下載並安裝 .NET 8 Hosting Bundle (https://dotnet.microsoft.com/download/dotnet/8.0)

2. **Application Pool 設定**:
   ```
   Name: CallTrackingSystemAppPool
   .NET CLR Version: No Managed Code（重要！）
   Managed Pipeline Mode: Integrated
   Identity: ApplicationPoolIdentity
   Start Mode: AlwaysRunning（選配，避免冷啟動）
   Idle Timeout: 20 minutes（預設）
   ```

3. **網站設定**:
   ```
   Physical Path: C:\inetpub\wwwroot\CallTrackingSystem
   Application Pool: CallTrackingSystemAppPool
   Binding: https://*:443 (需要 SSL 憑證)
   ```

4. **檔案權限**:
   - `IIS_IUSRS` 需要 Read & Execute 權限
   - `ApplicationPoolIdentity` 需要 logs/ 資料夾 Write 權限

### web.config 範例

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" 
                  arguments=".\CallTrackingSystem.Web.dll" 
                  stdoutLogEnabled="true" 
                  stdoutLogFile=".\logs\stdout" 
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
```

### HTTPS 憑證設定

1. **取得憑證**:
   - 購買商業 SSL 憑證
   - 或使用 Let's Encrypt 免費憑證（需要 win-acme 工具）

2. **綁定憑證**:
   - IIS Manager → 選擇網站 → Bindings → Add
   - Type: https, Port: 443, SSL Certificate: 選擇已安裝的憑證

### 健康檢查端點

```csharp
// Program.cs
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Startup 註冊健康檢查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddUrlGroup(new Uri(builder.Configuration["Line:HealthCheckUrl"]), "LINE API");
```

### 部署流程

1. **發佈**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **停止 App Pool** → **複製檔案** → **啟動 App Pool**

3. **驗證**: 
   - 瀏覽 https://your-domain.com/health
   - 檢查 logs/stdout 日誌

### 替代方案評估

| 方案 | 優點 | 缺點 | 選擇 |
|------|------|------|------|
| Kestrel 直接暴露 | 簡單 | 缺乏 IIS 的管理功能 | ❌ |
| IIS In-Process | 效能好，管理方便 | 需要 Windows Server | ✅ 選擇 |
| IIS Out-of-Process | 彈性高 | 效能略差 | ❌ |
| Docker + Linux | 跨平台 | 偏離專案需求（Windows IIS） | ❌ |

---

## 6. Excel 報表生成

### 決策

使用 **EPPlus 7** (.NET Core 相容版本)

### 理由

- EPPlus 效能好，功能完整（支援樣式、公式、圖表）
- .NET Core 原生支援，無需 Office Interop
- 開源且活躍維護
- 記憶體使用較 ClosedXML 低

### 套件比較

| 套件 | 授權 | 效能 | 功能 | 建議 |
|------|------|------|------|------|
| EPPlus 7 | Polyform Noncommercial + Commercial | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ✅ 選擇 |
| ClosedXML | MIT | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⚠️ 備選 |
| NPOI | Apache 2.0 | ⭐⭐⭐ | ⭐⭐⭐ | ❌ |

**注意**: EPPlus 5+ 採用 Polyform Noncommercial 授權，商業使用需要購買授權。若為商業專案，考慮 ClosedXML（MIT 授權）。

### 實作範例

```csharp
// ReportService.cs
public class ReportService : IReportService
{
    private readonly ICallRecordRepository _recordRepository;
    
    public async Task<byte[]> GenerateExcelReportAsync(SearchFilterModel filter)
    {
        var records = await _recordRepository.SearchAsync(filter);
        
        using var package = new ExcelPackage();
        
        // 工作表 1: 篩選條件摘要
        CreateSummarySheet(package, filter);
        
        // 工作表 2: 明細資料
        CreateDetailSheet(package, records);
        
        // 工作表 3: 彙總統計
        CreateStatisticsSheet(package, records);
        
        return package.GetAsByteArray();
    }
    
    private void CreateDetailSheet(ExcelPackage package, List<CallRecord> records)
    {
        var worksheet = package.Workbook.Worksheets.Add("明細資料");
        
        // 設定標題列
        worksheet.Cells[1, 1].Value = "來電日期";
        worksheet.Cells[1, 2].Value = "詢問系統";
        worksheet.Cells[1, 3].Value = "主旨";
        worksheet.Cells[1, 4].Value = "內容";
        worksheet.Cells[1, 5].Value = "緊急度";
        worksheet.Cells[1, 6].Value = "處理狀態";
        worksheet.Cells[1, 7].Value = "處理人員";
        worksheet.Cells[1, 8].Value = "聯絡人";
        worksheet.Cells[1, 9].Value = "連絡電話";
        worksheet.Cells[1, 10].Value = "最後更新時間";
        
        // 樣式設定
        using (var range = worksheet.Cells[1, 1, 1, 10])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        }
        
        // 填入資料
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            var row = i + 2;
            
            worksheet.Cells[row, 1].Value = record.CreatedAt.ToString("yyyy/MM/dd HH:mm");
            worksheet.Cells[row, 2].Value = record.InquirySystem.Name;
            worksheet.Cells[row, 3].Value = record.Subject;
            worksheet.Cells[row, 4].Value = record.Content;
            worksheet.Cells[row, 5].Value = record.UrgencyLevel.GetDisplayName();
            worksheet.Cells[row, 6].Value = record.Status.GetDisplayName();
            worksheet.Cells[row, 7].Value = string.Join(", ", record.Handlers.Select(h => h.Name));
            worksheet.Cells[row, 8].Value = record.ContactName;
            worksheet.Cells[row, 9].Value = record.ContactPhone;
            worksheet.Cells[row, 10].Value = record.UpdatedAt.ToString("yyyy/MM/dd HH:mm");
        }
        
        // 自動調整欄寬
        worksheet.Cells.AutoFitColumns();
    }
    
    private void CreateStatisticsSheet(ExcelPackage package, List<CallRecord> records)
    {
        var worksheet = package.Workbook.Worksheets.Add("彙總統計");
        
        // 按月份統計
        var monthlyStats = records
            .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Year}/{g.Key.Month:00}",
                Count = g.Count()
            })
            .OrderBy(x => x.Month)
            .ToList();
        
        worksheet.Cells[1, 1].Value = "月份";
        worksheet.Cells[1, 2].Value = "總筆數";
        
        for (int i = 0; i < monthlyStats.Count; i++)
        {
            worksheet.Cells[i + 2, 1].Value = monthlyStats[i].Month;
            worksheet.Cells[i + 2, 2].Value = monthlyStats[i].Count;
        }
        
        // 按詢問系統統計
        var systemStats = records
            .GroupBy(r => r.InquirySystem.Name)
            .Select(g => new { System = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();
        
        int startRow = monthlyStats.Count + 4;
        worksheet.Cells[startRow, 1].Value = "詢問系統";
        worksheet.Cells[startRow, 2].Value = "總筆數";
        
        for (int i = 0; i < systemStats.Count; i++)
        {
            worksheet.Cells[startRow + i + 1, 1].Value = systemStats[i].System;
            worksheet.Cells[startRow + i + 1, 2].Value = systemStats[i].Count;
        }
        
        worksheet.Cells.AutoFitColumns();
    }
}
```

### 大型資料集處理

```csharp
// 對於超過 10,000 筆的報表，使用分批處理
public async Task<byte[]> GenerateLargeExcelReportAsync(SearchFilterModel filter)
{
    using var package = new ExcelPackage();
    var worksheet = package.Workbook.Worksheets.Add("明細資料");
    
    // 設定標題列...
    
    int pageSize = 1000;
    int currentRow = 2;
    
    for (int page = 0; ; page++)
    {
        var records = await _recordRepository.SearchPaginatedAsync(filter, page, pageSize);
        if (!records.Any()) break;
        
        foreach (var record in records)
        {
            // 填入資料...
            currentRow++;
        }
    }
    
    return package.GetAsByteArray();
}
```

### 記憶體使用最佳化

- ✅ 使用 `using` 確保 ExcelPackage 釋放
- ✅ 大型報表考慮背景任務 + 檔案下載連結（避免 HTTP timeout）
- ✅ 限制最大匯出筆數（如 5000 筆）或提供分批匯出

---

## 7. 編輯鎖定機制

### 決策

使用 **資料庫欄位樂觀鎖定** + **定時清理背景任務**

### 理由

- 不使用 Redis，避免額外基礎設施
- 樂觀鎖定適合低衝突場景（客服系統編輯衝突機率低）
- 使用 `RowVersion` (timestamp) 確保並發安全
- 背景任務每 5 分鐘清理過期鎖定

### 資料庫設計

```csharp
// CallRecord Entity 新增欄位
public class CallRecord
{
    // ... 其他欄位
    
    public string? LockedByUserId { get; set; }    // 鎖定者 User ID
    public DateTime? LockedAt { get; set; }         // 鎖定時間
    public byte[] RowVersion { get; set; } = null!; // 並發控制
    
    // EF Core Configuration
    // builder.Property(x => x.RowVersion).IsRowVersion();
}
```

### 編輯鎖定服務

```csharp
public class EditLockManager
{
    private readonly ApplicationDbContext _context;
    private const int LockTimeoutMinutes = 30;
    
    public async Task<(bool Success, string? Message)> AcquireLockAsync(int recordId, string userId)
    {
        var record = await _context.CallRecords.FindAsync(recordId);
        if (record == null)
            return (false, "紀錄不存在");
        
        // 檢查是否已被其他人鎖定
        if (record.LockedByUserId != null && record.LockedByUserId != userId)
        {
            var lockExpired = record.LockedAt.HasValue && 
                DateTime.UtcNow > record.LockedAt.Value.AddMinutes(LockTimeoutMinutes);
            
            if (!lockExpired)
            {
                var lockedBy = await _context.Users
                    .Where(u => u.Id == record.LockedByUserId)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync();
                
                return (false, $"此紀錄正在被 {lockedBy} 編輯中");
            }
        }
        
        // 取得或更新鎖定
        record.LockedByUserId = userId;
        record.LockedAt = DateTime.UtcNow;
        
        try
        {
            await _context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateConcurrencyException)
        {
            return (false, "鎖定失敗，請重試");
        }
    }
    
    public async Task ReleaseLockAsync(int recordId, string userId)
    {
        var record = await _context.CallRecords.FindAsync(recordId);
        if (record == null || record.LockedByUserId != userId)
            return;
        
        record.LockedByUserId = null;
        record.LockedAt = null;
        await _context.SaveChangesAsync();
    }
}
```

### 自動解鎖背景任務

```csharp
// Program.cs 註冊 Hosted Service
builder.Services.AddHostedService<EditLockCleanupService>();

// EditLockCleanupService.cs
public class EditLockCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EditLockCleanupService> _logger;
    
    public EditLockCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<EditLockCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var expiredLocks = await context.CallRecords
                    .Where(r => r.LockedByUserId != null && 
                                r.LockedAt.HasValue && 
                                r.LockedAt.Value < DateTime.UtcNow.AddMinutes(-30))
                    .ToListAsync(stoppingToken);
                
                foreach (var record in expiredLocks)
                {
                    record.LockedByUserId = null;
                    record.LockedAt = null;
                }
                
                if (expiredLocks.Any())
                {
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("清理 {Count} 個過期編輯鎖定", expiredLocks.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理編輯鎖定失敗");
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### 管理者強制解鎖

```csharp
// AdminController.cs
[Authorize(Roles = "Admin")]
[HttpPost]
public async Task<IActionResult> ForceUnlock(int recordId)
{
    var record = await _context.CallRecords.FindAsync(recordId);
    if (record != null)
    {
        record.LockedByUserId = null;
        record.LockedAt = null;
        await _context.SaveChangesAsync();
        
        _logger.LogWarning("管理者 {AdminId} 強制解除紀錄 {RecordId} 的編輯鎖定", 
            User.GetUserId(), recordId);
    }
    
    return RedirectToAction("Details", "CallRecord", new { id = recordId });
}
```

### 替代方案評估

| 方案 | 優點 | 缺點 | 選擇 |
|------|------|------|------|
| Redis 分散式鎖 | 效能好，自動過期 | 需要額外基礎設施 | ❌ |
| 資料庫樂觀鎖定 | 無額外依賴，簡單 | 需要背景清理任務 | ✅ 選擇 |
| 悲觀鎖定 (SELECT FOR UPDATE) | 強一致性 | 可能造成死鎖 | ❌ |

---

## 8. 測試策略

### 決策

**三層測試金字塔**: 單元測試 (70%) > 整合測試 (20%) > API 測試 (10%)

### 理由

- 單元測試快速、易於維護、覆蓋業務邏輯
- 整合測試驗證資料存取和外部服務
- API 測試確保端對端流程正確

### xUnit 最佳實踐

```csharp
// 使用 Theory + InlineData 參數化測試
public class CallRecordServiceTests
{
    private readonly Mock<ICallRecordRepository> _mockRepo;
    private readonly CallRecordService _service;
    
    public CallRecordServiceTests()
    {
        _mockRepo = new Mock<ICallRecordRepository>();
        _service = new CallRecordService(_mockRepo.Object);
    }
    
    [Theory]
    [InlineData(ProcessStatus.Pending, ProcessStatus.Completed, true)]
    [InlineData(ProcessStatus.Completed, ProcessStatus.Pending, false)]
    public async Task UpdateStatus_ValidTransition_ReturnsExpectedResult(
        ProcessStatus currentStatus, 
        ProcessStatus newStatus, 
        bool expectedSuccess)
    {
        // Arrange
        var record = new CallRecord 
        { 
            Id = 1, 
            Status = currentStatus 
        };
        _mockRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(record);
        
        // Act
        var result = await _service.UpdateStatusAsync(1, newStatus, "user123");
        
        // Assert
        result.Should().Be(expectedSuccess);
    }
    
    [Fact]
    public async Task CreateCallRecord_ValidData_SendsLineNotification()
    {
        // Arrange
        var mockLineService = new Mock<ILineNotificationService>();
        var service = new CallRecordService(_mockRepo.Object, mockLineService.Object);
        
        var newRecord = new CreateCallRecordModel
        {
            Subject = "測試問題",
            InquirySystemId = 1
        };
        
        // Act
        await service.CreateAsync(newRecord);
        
        // Assert
        mockLineService.Verify(
            x => x.SendCallRecordNotificationAsync(
                It.IsAny<CallRecord>(), 
                It.IsAny<List<string>>()),
            Times.Once);
    }
}
```

### Moq 進階用法

```csharp
// 驗證方法被呼叫的次數和參數
_mockRepo.Verify(x => x.SaveAsync(It.Is<CallRecord>(r => r.Subject == "測試")), Times.Once);

// 設定非同步回傳值
_mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync((int id) => new CallRecord { Id = id });

// 設定例外
_mockRepo.Setup(x => x.SaveAsync(It.IsAny<CallRecord>()))
    .ThrowsAsync(new DbUpdateException());
```

### FluentAssertions 語法

```csharp
// 物件屬性斷言
record.Should().NotBeNull();
record.Subject.Should().Be("測試問題");
record.Handlers.Should().HaveCount(2);
record.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

// 集合斷言
records.Should().Contain(r => r.Status == ProcessStatus.Pending);
records.Should().BeInAscendingOrder(r => r.CreatedAt);

// 例外斷言
await act.Should().ThrowAsync<ArgumentNullException>()
    .WithMessage("*subject*");
```

### 整合測試範例

```csharp
// 使用 In-Memory Database
public class CallRecordRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CallRecordRepository _repository;
    
    public CallRecordRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new ApplicationDbContext(options);
        _repository = new CallRecordRepository(_context);
        
        SeedTestData();
    }
    
    [Fact]
    public async Task SearchAsync_FilterByStatus_ReturnsMatchingRecords()
    {
        // Arrange
        var filter = new SearchFilterModel { Status = ProcessStatus.Pending };
        
        // Act
        var results = await _repository.SearchAsync(filter);
        
        // Assert
        results.Should().AllSatisfy(r => r.Status.Should().Be(ProcessStatus.Pending));
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### CI/CD 整合

```yaml
# .github/workflows/ci.yml (若使用 GitHub Actions)
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
      
      - name: Upload coverage
        uses: codecov/codecov-action@v3
```

### 測試覆蓋率目標

- **總體目標**: 80%
- **Service 層**: 90%+（核心業務邏輯）
- **Repository 層**: 70%+（資料存取）
- **Controller 層**: 60%+（API 契約測試）

---

## 總結

所有 8 項研究任務已完成，關鍵決策如下：

1. **.NET Core 8 + EF Core**: Rich Domain Model + Fluent API 分離模式
2. **LINE Messaging API**: 官方 SDK + Flex Message，Push 模式通知
3. **LINE Login**: OAuth 2.0 + .NET OAuth Handler，首次登入需綁定審核
4. **Azure SQL**: Basic DTU (5 DTU, $5/月) 初期配置
5. **IIS 部署**: In-Process Hosting Model + ASP.NET Core Hosting Bundle
6. **Excel 報表**: EPPlus 7（注意商業授權）或 ClosedXML（MIT）
7. **編輯鎖定**: 資料庫欄位 + RowVersion + 背景清理任務
8. **測試策略**: xUnit + Moq + FluentAssertions，目標 80% 覆蓋率

**無未解決的 NEEDS CLARIFICATION 項目**，可進入 Phase 1 設計階段。
