# Research Report: LINE Bot 整合功能

**Feature**: LINE Bot Integration  
**Branch**: `2-line-bot-integration`  
**Date**: 2026-02-03  
**Status**: Phase 0 Complete

---

## 概述

本研究報告針對 LINE Bot 整合功能的 8 個關鍵技術議題進行深入調查，所有決策均基於：
1. **.NET 8 LTS 相容性**
2. **棕地專案最小化變更原則**
3. **小型團隊（20-50 人）規模考量**
4. **不使用 Redis 的憲法限制**

所有決策均已解決 plan.md 中標註的 NEEDS CLARIFICATION 項目。

---

## 1. LINE Messaging SDK 版本與相容性

### Decision（決策）

**選擇**: `LineDC.Messaging` v2.5.0（社群維護版本）或直接使用 `HttpClient` 呼叫 LINE Messaging API

### Rationale（理由）

1. **官方 SDK 停止維護**: Line.Messaging（官方 NuGet 套件）最後更新為 2019 年，不支援 .NET Core 3.1 以上版本
2. **社群 SDK 相容性**: LineDC.Messaging 是社群維護的分支，支援 .NET 6/8，但文檔較少且更新頻率低
3. **HttpClient 方案優勢**:
   - 完全控制 HTTP 請求與回應格式
   - 不依賴第三方套件維護狀態
   - 符合憲法「最小化依賴」原則
   - LINE Messaging API 為標準 REST API，實作成本低

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| 官方 Line.Messaging SDK | 官方支援、文檔完整 | 不支援 .NET 8、已停止維護 | ❌ 拒絕 |
| LineDC.Messaging SDK | 支援 .NET 8、社群活躍 | 非官方、文檔不足、Breaking Changes 風險 | ⚠️ 備選 |
| 直接使用 HttpClient | 完全控制、無第三方依賴 | 需手動實作 JSON 序列化、簽章驗證 | ✅ **採用** |

### Implementation Guidance（實作指引）

#### 建立 LINE Messaging API Client

```csharp
// CallTrackingSystem.Infrastructure/Services/LineMessagingApiClient.cs [NEW]
public class LineMessagingApiClient : ILineMessagingApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _channelAccessToken;
    private readonly string _channelSecret;
    private readonly ILogger<LineMessagingApiClient> _logger;

    public LineMessagingApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<LineMessagingApiClient> logger)
    {
        _httpClient = httpClient;
        _channelAccessToken = configuration["LineMessaging:ChannelAccessToken"];
        _channelSecret = configuration["LineMessaging:ChannelSecret"];
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri("https://api.line.me/v2/bot/");
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", _channelAccessToken);
    }

    // 推送訊息
    public async Task<bool> PushMessageAsync(string lineUserId, string message)
    {
        var request = new
        {
            to = lineUserId,
            messages = new[] { new { type = "text", text = message } }
        };

        var response = await _httpClient.PostAsJsonAsync("message/push", request);
        return response.IsSuccessStatusCode;
    }

    // Webhook 簽章驗證
    public bool VerifySignature(string requestBody, string signature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
        var computedSignature = Convert.ToBase64String(hash);
        return computedSignature == signature;
    }
}
```

#### DI 註冊

```csharp
// CallTrackingSystem.Web/Program.cs [EXTEND]
builder.Services.AddHttpClient<ILineMessagingApiClient, LineMessagingApiClient>();
```

---

## 2. LINE Login OAuth 2.0 最佳實作

### Decision（決策）

**選擇**: 使用 ASP.NET Core 原生 OAuth 2.0 流程 + Session Storage 儲存 State 參數

### Rationale（理由）

1. **State 參數安全性**: 使用 `Guid.NewGuid()` 生成隨機字串，搭配 Session Storage 驗證，防止 CSRF 攻擊
2. **Access Token 管理**: LINE Login Access Token 有效期 30 天，儲存在 User Entity 的 `LineUserId` 欄位即可（不需 Refresh Token）
3. **HTTPS Callback URL**: 本地開發使用 ngrok，Production 使用正式域名
4. **錯誤處理**: Authorization Code 失敗時記錄 error_description，導回設定頁並顯示錯誤訊息

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| IdentityServer4 整合 | 統一 OAuth 管理、標準化流程 | 過度設計、增加系統複雜度 | ❌ 拒絕 |
| 自行實作 OAuth 流程 | 完全控制、簡單直接 | 需手動處理 State 驗證 | ✅ **採用** |
| Refresh Token 機制 | 長期有效、無需重新授權 | LINE Login Token 有效期已達 30 天，小型團隊重新綁定成本低 | ❌ 不需要 |

### Implementation Guidance（實作指引）

#### LINE Login Service 介面

```csharp
// CallTrackingSystem.Core/Services/ILineLoginService.cs [NEW]
public interface ILineLoginService
{
    string GenerateAuthorizationUrl(string state);  // 生成 LINE Login 授權 URL
    Task<LineLoginResult> ExchangeCodeForTokenAsync(string code, string state);  // 交換 Access Token
    Task<bool> BindUserAsync(int userId, string lineUserId, string lineDisplayName);  // 綁定使用者
    Task<bool> UnbindUserAsync(int userId);  // 解除綁定
}

public class LineLoginResult
{
    public bool Success { get; set; }
    public string? LineUserId { get; set; }
    public string? LineDisplayName { get; set; }
    public string? ErrorMessage { get; set; }
}
```

#### LINE Login Service 實作

```csharp
// CallTrackingSystem.Core/Services/LineLoginService.cs [NEW]
public class LineLoginService : ILineLoginService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _callbackUrl;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<LineLoginService> _logger;

    public string GenerateAuthorizationUrl(string state)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = _clientId,
            ["redirect_uri"] = _callbackUrl,
            ["state"] = state,
            ["scope"] = "profile openid"  // 僅需基本資料
        };

        var query = string.Join("&", queryParams.Select(kv => 
            $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        
        return $"https://access.line.me/oauth2/v2.1/authorize?{query}";
    }

    public async Task<LineLoginResult> ExchangeCodeForTokenAsync(string code, string state)
    {
        try
        {
            // 1. 交換 Access Token
            var tokenRequest = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _callbackUrl,
                ["client_id"] = _clientId,
                ["client_secret"] = _clientSecret
            };

            var tokenResponse = await _httpClient.PostAsync(
                "https://api.line.me/oauth2/v2.1/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!tokenResponse.IsSuccessStatusCode)
            {
                var error = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("LINE Token Exchange 失敗: {Error}", error);
                return new LineLoginResult { Success = false, ErrorMessage = "授權失敗，請稍後再試" };
            }

            var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = tokenData.GetProperty("access_token").GetString();

            // 2. 取得 Profile 資訊
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            var profileResponse = await _httpClient.GetAsync("https://api.line.me/v2/profile");
            var profileData = await profileResponse.Content.ReadFromJsonAsync<JsonElement>();

            var lineUserId = profileData.GetProperty("userId").GetString();
            var lineDisplayName = profileData.GetProperty("displayName").GetString();

            return new LineLoginResult
            {
                Success = true,
                LineUserId = lineUserId,
                LineDisplayName = lineDisplayName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE Login 處理異常");
            return new LineLoginResult { Success = false, ErrorMessage = "系統錯誤，請聯繫管理員" };
        }
    }

    public async Task<bool> BindUserAsync(int userId, string lineUserId, string lineDisplayName)
    {
        // 1. 檢查 LINE 帳號是否已被綁定
        if (await _userRepository.IsLineUserIdBoundAsync(lineUserId))
        {
            _logger.LogWarning("LINE 帳號 {LineUserId} 已被其他使用者綁定", lineUserId);
            return false;
        }

        // 2. 更新使用者綁定資訊
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogError("使用者 {UserId} 不存在", userId);
            return false;
        }

        user.LineUserId = lineUserId;
        user.LineDisplayName = lineDisplayName;
        user.LineBoundAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> UnbindUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.LineUserId = null;
        user.LineDisplayName = null;
        user.LineBoundAt = null;

        await _userRepository.UpdateAsync(user);
        return true;
    }
}
```

#### Controller 實作

```csharp
// CallTrackingSystem.Web/Controllers/AuthController.cs [EXTEND]
[Authorize]
public async Task<IActionResult> LineLogin()
{
    var state = Guid.NewGuid().ToString();
    HttpContext.Session.SetString("LineLoginState", state);  // 儲存 State 至 Session
    
    var authUrl = _lineLoginService.GenerateAuthorizationUrl(state);
    return Redirect(authUrl);
}

[Authorize]
public async Task<IActionResult> LineCallback(string code, string state)
{
    // 1. 驗證 State
    var sessionState = HttpContext.Session.GetString("LineLoginState");
    if (sessionState != state)
    {
        TempData["Error"] = "驗證失敗，請重試";
        return RedirectToAction("Settings", "User");
    }

    // 2. 交換 Token 並取得 Profile
    var result = await _lineLoginService.ExchangeCodeForTokenAsync(code, state);
    if (!result.Success)
    {
        TempData["Error"] = result.ErrorMessage;
        return RedirectToAction("Settings", "User");
    }

    // 3. 綁定使用者
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    var success = await _lineLoginService.BindUserAsync(userId, result.LineUserId, result.LineDisplayName);

    if (success)
    {
        TempData["Success"] = $"已成功綁定 LINE 帳號：{result.LineDisplayName}";
    }
    else
    {
        TempData["Error"] = "此 LINE 帳號已被其他使用者綁定";
    }

    return RedirectToAction("Settings", "User");
}
```

---

## 3. In-Memory 對話狀態管理設計

### Decision（決策）

**選擇**: `ConcurrentDictionary<string, ConversationState>` + BackgroundService 定期清理

### Rationale（理由）

1. **並行安全**: ConcurrentDictionary 提供執行緒安全的讀寫操作
2. **逾時清理**: BackgroundService 每 1 分鐘檢查一次，清除超過 5 分鐘無活動的對話
3. **記憶體控制**: 小型團隊（20-50 人）同時對話數量預估 ≤ 10 個，記憶體使用量 < 1 MB
4. **單實例限制**: 在 quickstart.md 中明確標註此功能不支援負載平衡

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| Redis 分散式快取 | 支援多實例、持久化 | 違反憲法「不使用 Redis」原則、增加基礎設施複雜度 | ❌ 拒絕 |
| SQL Server Session Table | 持久化、支援多實例 | 效能差（每次對話需查詢資料庫）、不符合對話狀態「不持久化」設計 | ❌ 拒絕 |
| In-Memory Dictionary | 簡單、效能高 | 重啟遺失、不支援多實例 | ✅ **採用** |

### Implementation Guidance（實作指引）

#### Conversation State Service 介面

```csharp
// CallTrackingSystem.Core/Services/IConversationStateService.cs [NEW]
public interface IConversationStateService
{
    ConversationState? GetState(string lineUserId);  // 取得對話狀態
    void SetState(string lineUserId, ConversationState state);  // 設定對話狀態
    void ClearState(string lineUserId);  // 清除對話狀態
    void UpdateLastActivity(string lineUserId);  // 更新最後活動時間
}
```

#### Conversation State Service 實作

```csharp
// CallTrackingSystem.Core/Services/ConversationStateService.cs [NEW]
public class ConversationStateService : IConversationStateService
{
    private readonly ConcurrentDictionary<string, ConversationState> _states = new();
    private readonly ILogger<ConversationStateService> _logger;

    public ConversationState? GetState(string lineUserId)
    {
        return _states.TryGetValue(lineUserId, out var state) ? state : null;
    }

    public void SetState(string lineUserId, ConversationState state)
    {
        _states[lineUserId] = state;
        _logger.LogInformation("LINE 使用者 {LineUserId} 對話狀態已設定為 {Step}", 
            lineUserId, state.CurrentStep);
    }

    public void ClearState(string lineUserId)
    {
        if (_states.TryRemove(lineUserId, out _))
        {
            _logger.LogInformation("LINE 使用者 {LineUserId} 對話狀態已清除", lineUserId);
        }
    }

    public void UpdateLastActivity(string lineUserId)
    {
        if (_states.TryGetValue(lineUserId, out var state))
        {
            state.LastActivityAt = DateTime.UtcNow;
        }
    }

    // 內部方法：清除逾時對話（由 BackgroundService 呼叫）
    public int CleanupExpiredStates(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _states
            .Where(kv => now - kv.Value.LastActivityAt > timeout)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            ClearState(key);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("已清除 {Count} 個逾時對話狀態", expiredKeys.Count);
        }

        return expiredKeys.Count;
    }
}
```

#### Background Service 定期清理

```csharp
// CallTrackingSystem.Infrastructure/Services/ConversationCleanupService.cs [NEW]
public class ConversationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConversationCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("對話狀態清理服務已啟動");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var stateService = scope.ServiceProvider
                    .GetRequiredService<ConversationStateService>();

                var cleanedCount = stateService.CleanupExpiredStates(_timeout);
                
                if (cleanedCount > 0)
                {
                    _logger.LogInformation("已清除 {Count} 個逾時對話", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "對話狀態清理失敗");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("對話狀態清理服務已停止");
    }
}
```

#### DI 註冊

```csharp
// CallTrackingSystem.Web/Program.cs [EXTEND]
builder.Services.AddSingleton<ConversationStateService>();  // 單例模式
builder.Services.AddHostedService<ConversationCleanupService>();
```

---

## 4. LINE Webhook 簽章驗證實作

### Decision（決策）

**選擇**: 使用 ASP.NET Core Middleware 進行 Webhook 簽章驗證

### Rationale（理由）

1. **安全性優先**: 在 Controller 之前驗證簽章，拒絕未經驗證的請求
2. **集中驗證邏輯**: Middleware 統一處理所有 LINE Webhook 端點
3. **Channel Secret 安全儲存**: 儲存在 `appsettings.json`（開發環境）或 Azure Key Vault（正式環境）
4. **驗證失敗回應**: 返回 `401 Unauthorized` 並記錄 Log

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| ActionFilter 驗證 | 可針對特定 Controller 套用 | 驗證時機較晚（已進入 Controller Pipeline） | ❌ 次選 |
| Middleware 驗證 | 驗證時機早、集中管理 | 需手動讀取 Request Body | ✅ **採用** |
| 不驗證簽章 | 實作簡單 | 安全性風險高、可能被偽造請求攻擊 | ❌ 拒絕 |

### Implementation Guidance（實作指引）

#### LINE Signature Validator Middleware

```csharp
// CallTrackingSystem.Web/Middleware/LineSignatureValidator.cs [NEW]
public class LineSignatureValidator
{
    private readonly RequestDelegate _next;
    private readonly string _channelSecret;
    private readonly ILogger<LineSignatureValidator> _logger;

    public LineSignatureValidator(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<LineSignatureValidator> logger)
    {
        _next = next;
        _channelSecret = configuration["LineMessaging:ChannelSecret"];
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 僅驗證 LINE Webhook 端點
        if (!context.Request.Path.StartsWithSegments("/api/line/webhook"))
        {
            await _next(context);
            return;
        }

        // 1. 讀取 Request Body
        context.Request.EnableBuffering();  // 允許多次讀取
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;  // 重置 Stream 位置

        // 2. 取得簽章
        if (!context.Request.Headers.TryGetValue("X-Line-Signature", out var signature))
        {
            _logger.LogWarning("LINE Webhook 請求缺少 X-Line-Signature Header");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing signature");
            return;
        }

        // 3. 驗證簽章
        if (!VerifySignature(requestBody, signature.ToString()))
        {
            _logger.LogWarning("LINE Webhook 簽章驗證失敗");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid signature");
            return;
        }

        _logger.LogInformation("LINE Webhook 簽章驗證成功");
        await _next(context);
    }

    private bool VerifySignature(string requestBody, string signature)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_channelSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
        var computedSignature = Convert.ToBase64String(hash);
        return computedSignature == signature;
    }
}
```

#### 註冊 Middleware

```csharp
// CallTrackingSystem.Web/Program.cs [EXTEND]
app.UseMiddleware<LineSignatureValidator>();  // 在 UseAuthorization 之後
```

---

## 5. Quick Reply 與 Flex Message 設計模式

### Decision（決策）

**選擇**: 
- **Quick Reply**: 用於 InquirySystem 選單、UrgencyLevel 選項、確認按鈕
- **Flex Message**: 用於推送通知（呈現回報單摘要）

### Rationale（理由）

1. **Quick Reply 簡單直觀**: 使用者點擊按鈕即可回覆，適合選擇式問題
2. **Flex Message 資訊豐富**: 支援自訂版面配置，適合呈現結構化資料（回報單摘要）
3. **13 個選項限制**: InquirySystem 超過 13 個時直接拒絕回報，不實作分頁邏輯
4. **文字長度限制**: 問題標題限制 100 字元、內容限制 500 字元（自動截斷）

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| Template Message | 簡單、官方支援 | 版面配置受限、無法自訂樣式 | ❌ 次選 |
| Flex Message | 高度自訂、視覺效果佳 | JSON 結構複雜、開發成本高 | ✅ **採用**（僅用於推送通知） |
| 純文字訊息 | 實作最簡單 | 使用者體驗差、資訊不易閱讀 | ❌ 拒絕 |

### Implementation Guidance（實作指引）

#### Quick Reply 按鈕生成

```csharp
// CallTrackingSystem.Core/Services/LineBotMessageHandler.cs [NEW]
private object CreateQuickReplyMessage(string text, List<(string Label, string Data)> options)
{
    if (options.Count > 13)
    {
        return new
        {
            type = "text",
            text = "選項過多，請至網頁端回報問題\n" +
                   $"{_configuration["WebBaseUrl"]}/call-records/create"
        };
    }

    return new
    {
        type = "text",
        text = text,
        quickReply = new
        {
            items = options.Select(opt => new
            {
                type = "action",
                action = new
                {
                    type = "postback",
                    label = opt.Label,
                    data = opt.Data
                }
            }).ToArray()
        }
    };
}

// 使用範例：InquirySystem 選單
var inquirySystems = await _inquirySystemRepository.GetAllAsync();
var options = inquirySystems.Select(s => (s.Name, $"inquiry_system:{s.Id}")).ToList();
var message = CreateQuickReplyMessage("請選擇問題所屬單位：", options);
```

#### Flex Message 推送通知

```csharp
// CallTrackingSystem.Infrastructure/Services/LineNotificationService.cs [EXTEND]
public async Task SendCallRecordNotificationAsync(CallRecord callRecord, List<Handler> handlers)
{
    var flexMessage = new
    {
        type = "flex",
        altText = $"新來電回報：{callRecord.Subject}",
        contents = new
        {
            type = "bubble",
            header = new
            {
                type = "box",
                layout = "vertical",
                contents = new[]
                {
                    new
                    {
                        type = "text",
                        text = "🔔 新來電回報",
                        weight = "bold",
                        size = "xl",
                        color = "#FFFFFF"
                    }
                },
                backgroundColor = "#FF6B6B"
            },
            body = new
            {
                type = "box",
                layout = "vertical",
                contents = new object[]
                {
                    new { type = "text", text = $"問題標題：{callRecord.Subject}", wrap = true, weight = "bold" },
                    new { type = "separator", margin = "md" },
                    new { type = "text", text = $"所屬單位：{callRecord.InquirySystem.Name}", margin = "md", size = "sm", color = "#999999" },
                    new { type = "text", text = $"緊急程度：{GetUrgencyEmoji(callRecord.UrgencyLevel)} {callRecord.UrgencyLevel}", margin = "md", size = "sm", color = GetUrgencyColor(callRecord.UrgencyLevel) },
                    new { type = "text", text = $"回報時間：{callRecord.CreatedAt:yyyy/MM/dd HH:mm}", margin = "md", size = "sm", color = "#999999" }
                }
            },
            footer = new
            {
                type = "box",
                layout = "vertical",
                contents = new[]
                {
                    new
                    {
                        type = "button",
                        action = new
                        {
                            type = "uri",
                            label = "查看詳情",
                            uri = $"{_webBaseUrl}/call-records/{callRecord.Id}"
                        },
                        style = "primary"
                    }
                }
            }
        }
    };

    // 僅推送給已綁定 LINE 的處理人員
    foreach (var handler in handlers)
    {
        var user = await _userRepository.GetByIdAsync(handler.UserId);
        if (user?.LineUserId != null)
        {
            await _lineApiClient.PushFlexMessageAsync(user.LineUserId, flexMessage);
            await LogNotificationAsync(callRecord.Id, user.LineUserId, "Success");
        }
    }
}

private string GetUrgencyEmoji(UrgencyLevel level) => level switch
{
    UrgencyLevel.High => "🔴",
    UrgencyLevel.Medium => "🟡",
    UrgencyLevel.Low => "🟢",
    _ => "⚪"
};

private string GetUrgencyColor(UrgencyLevel level) => level switch
{
    UrgencyLevel.High => "#FF0000",
    UrgencyLevel.Medium => "#FFA500",
    UrgencyLevel.Low => "#00AA00",
    _ => "#999999"
};
```

---

## 6. Handler-User 關聯查詢效能優化

### Decision（決策）

**選擇**: 使用 EF Core `Include` + 新增 `User.LineUserId` 索引

### Rationale（理由）

1. **Include 效能優勢**: 單次 JOIN 查詢，避免 N+1 問題
2. **索引加速**: 新增 `User.LineUserId` 非叢集索引（唯一、允許 NULL）
3. **查詢頻率**: 僅在新建立回報單時查詢一次，不需快取
4. **批次通知**: 使用 `Include` 一次載入所有 Handler 的 User 資料

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| Explicit Loading | 可按需載入 | N+1 查詢問題嚴重 | ❌ 拒絕 |
| IMemoryCache 快取 | 減少資料庫查詢 | 增加複雜度、快取失效策略難處理 | ❌ 不需要 |
| Include + 索引 | 效能佳、實作簡單 | 需新增 Migration | ✅ **採用** |

### Implementation Guidance（實作指引）

#### 新增索引 Migration

```csharp
// CallTrackingSystem.Infrastructure/Migrations/20260203_AddLineIntegration.cs [NEW]
public partial class AddLineIntegration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 新增欄位
        migrationBuilder.AddColumn<string>(
            name: "LineUserId",
            table: "Users",
            type: "varchar(50)",
            unicode: false,
            maxLength: 50,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LineDisplayName",
            table: "Users",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LineBoundAt",
            table: "Users",
            type: "datetime2",
            nullable: true);

        // 新增唯一索引（允許多個 NULL）
        migrationBuilder.CreateIndex(
            name: "IX_Users_LineUserId",
            table: "Users",
            column: "LineUserId",
            unique: true,
            filter: "LineUserId IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_LineUserId",
            table: "Users");

        migrationBuilder.DropColumn(name: "LineUserId", table: "Users");
        migrationBuilder.DropColumn(name: "LineDisplayName", table: "Users");
        migrationBuilder.DropColumn(name: "LineBoundAt", table: "Users");
    }
}
```

#### 優化查詢

```csharp
// CallTrackingSystem.Core/Services/LineNotificationService.cs [EXTEND]
public async Task SendCallRecordNotificationAsync(int callRecordId)
{
    var callRecord = await _callRecordRepository
        .GetByIdAsync(callRecordId, includeHandlers: true);

    if (callRecord == null) return;

    // 使用 Include 一次載入所有 Handler 的 User 資料
    var handlersWithUsers = await _context.Handlers
        .Where(h => callRecord.Handlers.Select(ch => ch.Id).Contains(h.Id))
        .Include(h => h.User)  // EF Core Include
        .ToListAsync();

    foreach (var handler in handlersWithUsers)
    {
        if (handler.User?.LineUserId != null)
        {
            await SendPushNotificationAsync(handler.User.LineUserId, callRecord);
        }
    }
}
```

---

## 7. Migration 策略與回滾計劃

### Decision（決策）

**選擇**: 
- **欄位設計**: 所有 LINE 相關欄位均為 `nullable`，不影響現有資料
- **回滾策略**: 提供 `Down()` Migration 方法，可安全回滾

### Rationale（理由）

1. **向下相容**: 新增欄位不影響現有功能（CallRecordService、AuthService 等）
2. **安全回滾**: 若功能上線後需回滾，執行 `dotnet ef database update [PreviousMigration]` 即可
3. **Production 檢查清單**: 在 quickstart.md 中提供部署前檢查項目

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| 非 Nullable 欄位 + 預設值 | 強制資料完整性 | 現有使用者需批次更新、部署風險高 | ❌ 拒絕 |
| Nullable 欄位 | 向下相容、安全 | 需在程式碼中處理 null 檢查 | ✅ **採用** |
| 新增獨立表格 LineBinding | 更清晰的關聯設計 | 過度設計、查詢複雜度增加 | ❌ 不需要 |

### Implementation Guidance（實作指引）

#### Production 部署檢查清單

```markdown
# LINE Bot 整合功能部署檢查清單

## 部署前檢查
- [ ] 備份 Production 資料庫
- [ ] 驗證 Development 環境 Migration 成功
- [ ] 確認 LINE Channel ID、Secret、Access Token 已設定
- [ ] 確認 Webhook URL 為 HTTPS（LINE Platform 要求）
- [ ] 測試 LINE Login OAuth 流程（開發環境）

## 部署步驟
1. 停止 IIS Application Pool（避免並行 Migration）
2. 執行 Migration：`dotnet ef database update`
3. 驗證新增欄位：`SELECT TOP 1 * FROM Users`
4. 更新 appsettings.json（LINE 設定）
5. 啟動 IIS Application Pool
6. 驗證應用程式健康狀態：`/health`

## 部署後驗證
- [ ] 測試 LINE Login 綁定流程
- [ ] 測試 LINE Bot 對話回報
- [ ] 測試推送通知（建立新回報單）
- [ ] 檢查 NotificationLog 表記錄
- [ ] 監控應用程式 Log（是否有異常）

## 回滾計劃
若功能異常需回滾：
1. 停止 IIS Application Pool
2. 回滾 Migration：`dotnet ef database update [PreviousMigration]`
3. 還原舊版 appsettings.json
4. 啟動 IIS Application Pool
5. 驗證現有功能正常運作
```

---

## 8. 單元測試與整合測試策略

### Decision（決策）

**選擇**: 
- **單元測試**: 使用 Moq 模擬 ILineMessagingApiClient、ILineLoginService
- **整合測試**: 使用 WebApplicationFactory 測試 Webhook 端點與 OAuth Callback

### Rationale（理由）

1. **外部依賴模擬**: LINE API 外部依賴使用 Moq 模擬，避免實際 HTTP 呼叫
2. **狀態機測試**: LineBotMessageHandler 的對話流程測試使用狀態機驗證
3. **覆蓋率目標**: Service 層 90%、Controller 層 80%
4. **整合測試範圍**: 驗證 Webhook 簽章、OAuth Callback、對話流程端對端

### Alternatives Considered（評估過的替代方案）

| 方案 | 優點 | 缺點 | 決策 |
|------|------|------|------|
| 手動測試 | 快速驗證 | 無法自動化、回歸測試成本高 | ❌ 僅作為補充 |
| 單元測試 + Moq | 快速、可重複 | 無法測試外部整合 | ✅ **採用** |
| E2E 測試（實際 LINE API） | 最接近真實環境 | 測試環境設定複雜、測試速度慢 | ❌ 不需要 |

### Implementation Guidance（實作指引）

#### 單元測試範例：LineBotMessageHandler

```csharp
// CallTrackingSystem.UnitTests/Services/LineBotMessageHandlerTests.cs [NEW]
public class LineBotMessageHandlerTests
{
    private readonly Mock<IConversationStateService> _mockStateService;
    private readonly Mock<ICallRecordService> _mockCallRecordService;
    private readonly Mock<IInquirySystemRepository> _mockInquirySystemRepo;
    private readonly Mock<ILineMessagingApiClient> _mockLineApiClient;
    private readonly LineBotMessageHandler _handler;

    public LineBotMessageHandlerTests()
    {
        _mockStateService = new Mock<IConversationStateService>();
        _mockCallRecordService = new Mock<ICallRecordService>();
        _mockInquirySystemRepo = new Mock<IInquirySystemRepository>();
        _mockLineApiClient = new Mock<ILineMessagingApiClient>();
        
        _handler = new LineBotMessageHandler(
            _mockStateService.Object,
            _mockCallRecordService.Object,
            _mockInquirySystemRepo.Object,
            _mockLineApiClient.Object);
    }

    [Fact]
    public async Task HandleTextMessage_啟動回報流程_應建立對話狀態()
    {
        // Arrange
        var lineUserId = "U1234567890abcdef1234567890abcdef";
        var message = "回報問題";

        _mockStateService.Setup(s => s.GetState(lineUserId)).Returns((ConversationState?)null);

        // Act
        await _handler.HandleTextMessageAsync(lineUserId, message);

        // Assert
        _mockStateService.Verify(s => s.SetState(lineUserId, 
            It.Is<ConversationState>(state => state.CurrentStep == ConversationStep.AwaitingSubject)), 
            Times.Once);
        
        _mockLineApiClient.Verify(api => api.SendReplyMessageAsync(lineUserId, 
            It.Is<string>(msg => msg.Contains("請輸入問題標題"))), 
            Times.Once);
    }

    [Fact]
    public async Task HandleTextMessage_InquirySystem超過13個_應拒絕回報()
    {
        // Arrange
        var lineUserId = "U1234567890abcdef1234567890abcdef";
        var state = new ConversationState
        {
            LineUserId = lineUserId,
            CurrentStep = ConversationStep.AwaitingInquirySystem,
            FormData = new CallRecordFormData
            {
                Subject = "測試問題",
                Content = "測試內容"
            }
        };

        var inquirySystems = Enumerable.Range(1, 15)
            .Select(i => new InquirySystem { Id = i, Name = $"單位{i}" })
            .ToList();

        _mockStateService.Setup(s => s.GetState(lineUserId)).Returns(state);
        _mockInquirySystemRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(inquirySystems);

        // Act
        await _handler.HandleTextMessageAsync(lineUserId, "");

        // Assert
        _mockLineApiClient.Verify(api => api.SendReplyMessageAsync(lineUserId, 
            It.Is<string>(msg => msg.Contains("選項過多") && msg.Contains("網頁端回報"))), 
            Times.Once);
        
        _mockStateService.Verify(s => s.ClearState(lineUserId), Times.Once);
    }
}
```

#### 整合測試範例：LINE Webhook Controller

```csharp
// CallTrackingSystem.IntegrationTests/LineIntegrationTests.cs [NEW]
public class LineIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly string _channelSecret = "test-channel-secret";

    public LineIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["LineMessaging:ChannelSecret"] = _channelSecret
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Webhook_簽章驗證失敗_應返回401()
    {
        // Arrange
        var requestBody = "{\"events\":[]}";
        var invalidSignature = "invalid-signature";

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", invalidSignature);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Webhook_簽章驗證成功_應返回200()
    {
        // Arrange
        var requestBody = "{\"events\":[{\"type\":\"message\",\"message\":{\"type\":\"text\",\"text\":\"Hello\"}}]}";
        var signature = ComputeHmacSha256(requestBody, _channelSecret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/line/webhook")
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Line-Signature", signature);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
```

---

## 總結

### Phase 0 完成狀態

| 研究議題 | 決策 | 狀態 |
|---------|------|------|
| 1. LINE Messaging SDK | 使用 HttpClient 直接呼叫 LINE API | ✅ 完成 |
| 2. LINE Login OAuth 2.0 | 使用 ASP.NET Core 原生 OAuth 流程 + Session Storage | ✅ 完成 |
| 3. In-Memory 對話狀態管理 | ConcurrentDictionary + BackgroundService 清理 | ✅ 完成 |
| 4. LINE Webhook 簽章驗證 | 使用 Middleware 進行簽章驗證 | ✅ 完成 |
| 5. Quick Reply 與 Flex Message | Quick Reply 用於選單、Flex Message 用於推送通知 | ✅ 完成 |
| 6. Handler-User 關聯查詢 | EF Core Include + User.LineUserId 索引 | ✅ 完成 |
| 7. Migration 策略 | Nullable 欄位設計、提供回滾計劃 | ✅ 完成 |
| 8. 單元測試策略 | Moq 模擬外部依賴、WebApplicationFactory 整合測試 | ✅ 完成 |

### 關鍵決策摘要

1. **不使用第三方 LINE SDK**: 直接使用 HttpClient 呼叫 LINE API，避免第三方套件維護風險
2. **In-Memory 對話狀態**: 符合憲法「不使用 Redis」原則，明確限制單實例部署
3. **Handler-User 關聯**: 透過 User 查詢 LINE 綁定狀態，避免重複儲存
4. **InquirySystem 超限處理**: 直接拒絕回報並引導至網頁，符合最小化變更原則
5. **通知失敗處理**: 僅記錄到 NotificationLog，不實作自動重試

### 下一步

Phase 0 完成後，可進入 **Phase 1: Data Model & Contracts**，產出：
- `data-model.md` - 實體擴充設計
- `contracts/line-bot-api.yaml` - LINE Bot Webhook API 契約
- `contracts/line-login-api.yaml` - LINE Login OAuth 2.0 契約
- `contracts/conversation-flow.yaml` - 對話流程狀態機契約
- `quickstart.md` - 開發環境設定指南

---

**Generated**: 2026-02-03  
**Status**: Phase 0 Complete, Ready for Phase 1
