# Quick Start Guide: LINE Bot 整合功能

**Feature**: LINE Bot Integration  
**Branch**: `2-line-bot-integration`  
**Target Audience**: 開發人員  
**Estimated Setup Time**: 30-45 分鐘

---

## 前置需求

### 必要軟體

- ✅ .NET 8 SDK（已安裝）
- ✅ Visual Studio 2022 或 VS Code（已安裝）
- ✅ SQL Server / SQL Express / SQLite（已安裝）
- ✅ Git（已安裝）
- 🆕 **ngrok**（用於本地 Webhook 測試） - [下載連結](https://ngrok.com/download)

### 必要帳號

- ✅ LINE Developers Console 帳號 - [註冊連結](https://developers.line.biz/)

---

## Step 1: LINE Developers Console 設定

### 1.1 建立 LINE Messaging API Channel

1. 登入 [LINE Developers Console](https://developers.line.biz/console/)
2. 點擊「Create a new provider」或選擇現有 Provider
3. 點擊「Create a Messaging API channel」
4. 填寫 Channel 資訊：
   - **Channel name**: CallTrackingSystem Bot
   - **Channel description**: 客服來電問題回報 Bot
   - **Category**: Productivity
   - **Subcategory**: Other
5. 勾選同意條款，點擊「Create」

### 1.2 取得 Messaging API 憑證

進入 Channel 設定頁面，取得以下資訊：

| 憑證名稱 | 位置 | 說明 |
|---------|------|------|
| **Channel ID** | Basic settings > Channel ID | 數字格式，例：1234567890 |
| **Channel Secret** | Basic settings > Channel secret | 用於 Webhook 簽章驗證 |
| **Channel Access Token** | Messaging API > Channel access token | 點擊「Issue」生成，長期有效（不會過期） |

**重要**: 將這些憑證暫時儲存在安全的地方（稍後會設定到 appsettings.json）。

---

### 1.3 設定 Webhook URL（稍後執行，需等 ngrok 啟動）

在 Messaging API 頁籤中：
- **Webhook URL**: 暫時留空（Step 3 中設定）
- **Use webhook**: 啟用（開啟）
- **Auto-reply messages**: 停用（關閉）
- **Greeting messages**: 停用（關閉）

---

### 1.4 建立 LINE Login Channel

1. 返回 Provider 頁面，點擊「Create a LINE Login channel」
2. 填寫 Channel 資訊：
   - **Channel name**: CallTrackingSystem Login
   - **Channel description**: 客服系統帳號綁定
   - **App types**: 勾選「Web app」
3. 點擊「Create」

### 1.5 取得 LINE Login 憑證

進入 LINE Login Channel 設定頁面，取得以下資訊：

| 憑證名稱 | 位置 | 說明 |
|---------|------|------|
| **Channel ID** | Basic settings > Channel ID | 數字格式 |
| **Channel Secret** | Basic settings > Channel secret | 用於 Token 交換 |

### 1.6 設定 Callback URL（稍後執行）

在 LINE Login 頁籤中：
- **Callback URL**: 暫時留空（Step 3 中設定）

---

## Step 2: 本地開發環境設定

### 2.1 Clone 專案（若尚未 Clone）

```powershell
cd C:\Users\trist\Desktop\Google_Antigravity
git clone <repository-url> 需求紀錄與問題分析系統
cd 需求紀錄與問題分析系統
```

### 2.2 切換到 LINE Bot 分支

```powershell
git checkout 2-line-bot-integration
```

### 2.3 更新 appsettings.Development.json

編輯 `CallTrackingSystem/src/CallTrackingSystem.Web/appsettings.Development.json`：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=CallTrackingDB_Dev.db"
  },
  "LineMessaging": {
    "ChannelId": "YOUR_MESSAGING_CHANNEL_ID",
    "ChannelSecret": "YOUR_MESSAGING_CHANNEL_SECRET",
    "ChannelAccessToken": "YOUR_CHANNEL_ACCESS_TOKEN"
  },
  "LineLogin": {
    "ClientId": "YOUR_LOGIN_CHANNEL_ID",
    "ClientSecret": "YOUR_LOGIN_CHANNEL_SECRET",
    "CallbackUrl": "https://localhost:5000/auth/line-callback"  // 暫時設定，稍後用 ngrok URL 覆蓋
  },
  "WebBaseUrl": "https://localhost:5000"  // 用於生成 LINE Bot 回覆中的連結
}
```

**重要**: 將 `YOUR_*` 部分替換為 Step 1 中取得的實際憑證。

---

### 2.4 執行 Database Migration

```powershell
cd CallTrackingSystem

# 建立 AddLineIntegration Migration
dotnet ef migrations add AddLineIntegration `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web

# 套用 Migration 至資料庫
dotnet ef database update `
    --project src/CallTrackingSystem.Infrastructure `
    --startup-project src/CallTrackingSystem.Web
```

**驗證 Migration 成功**:

```powershell
# SQLite 驗證（開發環境）
sqlite3 src/CallTrackingSystem.Web/CallTrackingDB_Dev.db
> PRAGMA table_info(Users);
# 應該看到 LineUserId, LineDisplayName, LineBoundAt 欄位

# SQL Server 驗證（正式環境）
sqlcmd -S localhost -d CallTrackingDB -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME LIKE 'Line%'"
```

---

### 2.5 安裝 NuGet 套件（若需要）

```powershell
cd src/CallTrackingSystem.Infrastructure
dotnet add package System.Security.Cryptography.Algorithms --version 4.3.1  # HMAC-SHA256 支援

cd ../CallTrackingSystem.Web
dotnet restore
```

---

## Step 3: ngrok 設定與 Webhook 測試

### 3.1 啟動應用程式

```powershell
cd CallTrackingSystem/src/CallTrackingSystem.Web
dotnet run
```

**預期輸出**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

---

### 3.2 啟動 ngrok

**開啟新的 PowerShell 視窗**，執行：

```powershell
ngrok http 5000
```

**預期輸出**:
```
Session Status                online
Account                       your-account (Plan: Free)
Version                       3.x.x
Region                        Japan (jp)
Forwarding                    https://abc123def456.ngrok.io -> http://localhost:5000
```

**記錄 ngrok URL**: 例如 `https://abc123def456.ngrok.io`（每次啟動都會不同）

---

### 3.3 更新 LINE Developers Console

#### 3.3.1 設定 Messaging API Webhook URL

1. 進入 Messaging API Channel 設定頁面
2. 在 Messaging API 頁籤中找到 **Webhook URL**
3. 填入：`https://abc123def456.ngrok.io/api/line/webhook`（替換為你的 ngrok URL）
4. 點擊「Update」
5. 點擊「Verify」測試連線（應顯示「Success」）

#### 3.3.2 設定 LINE Login Callback URL

1. 進入 LINE Login Channel 設定頁面
2. 在 LINE Login 頁籤中找到 **Callback URL**
3. 填入：`https://abc123def456.ngrok.io/auth/line-callback`（替換為你的 ngrok URL）
4. 點擊「Update」

---

### 3.4 更新 appsettings.Development.json（使用 ngrok URL）

編輯 `appsettings.Development.json`，更新以下設定：

```json
{
  "LineLogin": {
    "CallbackUrl": "https://abc123def456.ngrok.io/auth/line-callback"  // 使用 ngrok URL
  },
  "WebBaseUrl": "https://abc123def456.ngrok.io"  // 使用 ngrok URL
}
```

**重新啟動應用程式** 以套用新設定：
```powershell
# 在原本的應用程式視窗按 Ctrl+C 停止
dotnet run
```

---

## Step 4: 測試 LINE Bot 對話

### 4.1 加入 LINE Bot 好友

1. 進入 Messaging API Channel 設定頁面
2. 在 Messaging API 頁籤中找到 **Bot basic ID**（例：@123abcde）
3. 使用 LINE 行動版 App 搜尋此 ID 並加入好友
4. 或掃描 QR Code（在 Channel 設定頁面中顯示）

---

### 4.2 測試對話回報流程

**在 LINE 中與 Bot 對話**：

| 步驟 | 使用者輸入 | Bot 回應 | 說明 |
|------|-----------|---------|------|
| 1 | 輸入「回報問題」 | 「您好！請問要回報什麼問題呢？請輸入問題標題。」 | 啟動對話流程 |
| 2 | 輸入「測試問題」 | 「收到，問題標題為『測試問題』。請描述問題的詳細內容：」 | 儲存標題 |
| 3 | 輸入「這是測試內容」 | 顯示 InquirySystem Quick Reply 選單 | 顯示所屬單位選項 |
| 4 | 點擊「資訊部」 | 顯示緊急程度 Quick Reply（🟢 低、🟡 中、🔴 高） | 選擇所屬單位 |
| 5 | 點擊「🔴 高」 | 「請提供顧客聯絡人姓名：」 | 選擇緊急程度 |
| 6 | 輸入「王小明」 | 「請提供顧客聯絡電話：」 | 儲存聯絡人 |
| 7 | 輸入「0912-345-678」 | 顯示摘要 + 「✅ 確認送出」、「🔄 重新填寫」、「❌ 取消」 | 驗證電話格式 |
| 8 | 點擊「✅ 確認送出」 | 「✅ 回報單已成功建立！回報單號：#1」+ 查看連結 | 建立回報單成功 |

---

### 4.3 測試取消流程

| 步驟 | 使用者輸入 | Bot 回應 |
|------|-----------|---------|
| 1 | 輸入「回報問題」 | 「您好！請問要回報什麼問題呢？請輸入問題標題。」 |
| 2 | 輸入「取消」 | 「已取消回報流程，如需重新回報請輸入『回報問題』」 |

---

### 4.4 測試未綁定使用者

**問題**: 對話回報前需先綁定 LINE 帳號（Handler 透過 User 綁定）

**解決方案**: 
1. 前往網頁端個人設定頁面：`https://abc123def456.ngrok.io/user/settings`
2. 點擊「綁定 LINE 官方帳號」按鈕
3. 完成 LINE Login OAuth 授權
4. 返回 LINE 再次嘗試回報問題

---

## Step 5: 測試 LINE 推送通知

### 5.1 在網頁端建立新回報單

1. 開啟瀏覽器，前往：`https://abc123def456.ngrok.io/call-records/create`
2. 填寫回報單資訊：
   - **問題標題**: 測試推送通知
   - **問題內容**: 測試 LINE Bot 推送功能
   - **所屬單位**: 資訊部
   - **緊急程度**: 高
   - **聯絡人**: 測試人員
   - **電話**: 0912-345-678
3. 點擊「建立」

---

### 5.2 驗證推送通知

**已綁定 LINE 的處理人員應收到 Flex Message 通知**：

```
🔔 新來電回報
━━━━━━━━━━━━━━━
問題標題：測試推送通知
所屬單位：資訊部
緊急程度：🔴 High
回報時間：2026/02/03 15:30

[查看詳情] 按鈕
```

---

## Step 6: 測試 LINE Login 綁定

### 6.1 登入系統

1. 前往：`https://abc123def456.ngrok.io/auth/login`
2. 使用測試帳號登入：
   - **使用者名稱**: `staff`
   - **密碼**: `Staff@123`

---

### 6.2 綁定 LINE 帳號

1. 前往個人設定頁面：`https://abc123def456.ngrok.io/user/settings`
2. 點擊「綁定 LINE 官方帳號」按鈕
3. 瀏覽器跳轉至 LINE 授權頁面
4. 點擊「同意」授權
5. 返回個人設定頁面，應顯示「已成功綁定 LINE 帳號：{Display Name}」

---

### 6.3 解除綁定

1. 在個人設定頁面點擊「解除綁定」按鈕
2. 確認解除綁定
3. 頁面應顯示「已成功解除 LINE 帳號綁定」

---

## Step 7: 驗證資料庫變更

### 7.1 檢查 User Entity 綁定資訊

```powershell
# SQLite（開發環境）
sqlite3 src/CallTrackingSystem.Web/CallTrackingDB_Dev.db
> SELECT Id, Username, LineUserId, LineDisplayName, LineBoundAt FROM Users WHERE LineUserId IS NOT NULL;

# 預期輸出:
# Id | Username | LineUserId                        | LineDisplayName | LineBoundAt
# ---|----------|-----------------------------------|-----------------|-------------------------
# 2  | staff    | Uabcdef1234567890abcdef1234567890 | 測試員工        | 2026-02-03 07:30:00.000
```

---

### 7.2 檢查 NotificationLog（推送通知記錄）

```powershell
sqlite3 src/CallTrackingSystem.Web/CallTrackingDB_Dev.db
> SELECT * FROM NotificationLog ORDER BY CreatedAt DESC LIMIT 5;
```

---

## 常見問題排除

### 問題 1: Webhook 簽章驗證失敗（401 Unauthorized）

**症狀**: LINE Webhook 請求返回 `401 Unauthorized`，Log 顯示「LINE Webhook 簽章驗證失敗」

**解決方案**:
1. 確認 `appsettings.Development.json` 中的 `LineMessaging:ChannelSecret` 正確
2. 確認 Middleware 正確註冊（在 `Program.cs` 中檢查 `app.UseMiddleware<LineSignatureValidator>()`）
3. 測試簽章驗證邏輯：
   ```csharp
   var requestBody = "{\"events\":[]}";
   var signature = ComputeHmacSha256(requestBody, _channelSecret);
   // signature 應與 LINE 傳送的 X-Line-Signature Header 相同
   ```

---

### 問題 2: ngrok URL 變更導致 Callback 失敗

**症狀**: LINE Login OAuth Callback 返回 `redirect_uri_mismatch` 錯誤

**解決方案**:
1. ngrok 免費版每次啟動 URL 都會變更
2. 每次啟動 ngrok 後，需更新 3 個地方：
   - LINE Developers Console → Messaging API Webhook URL
   - LINE Developers Console → LINE Login Callback URL
   - `appsettings.Development.json` → `LineLogin:CallbackUrl` 和 `WebBaseUrl`
3. 重新啟動應用程式以套用新設定

**建議**: 使用 ngrok 付費版取得固定 URL，或本地測試時使用 `http://localhost:5000`（需設定 HTTPS 轉發）

---

### 問題 3: InquirySystem 超過 13 個選項無法回報

**症狀**: Bot 回覆「選項過多，請至網頁端回報問題」

**解決方案**:
這是預期行為（設計決策）。若 InquirySystem 超過 13 個，Bot 會拒絕回報並引導使用者至網頁端。

**驗證 InquirySystem 數量**:
```powershell
sqlite3 src/CallTrackingSystem.Web/CallTrackingDB_Dev.db
> SELECT COUNT(*) FROM InquirySystems;
```

---

### 問題 4: 對話狀態在應用程式重啟後遺失

**症狀**: 應用程式重啟後，使用者正在進行的對話流程中斷

**解決方案**:
這是預期行為（In-Memory 對話狀態設計）。對話狀態不持久化至資料庫，重啟後使用者需重新啟動回報流程。

**重要**: 此功能僅支援單實例部署環境，不支援負載平衡。

---

### 問題 5: LINE 推送通知未收到

**症狀**: 網頁端建立回報單後，處理人員未收到 LINE 通知

**檢查清單**:
1. ✅ 處理人員的 User 帳號是否已綁定 LINE（檢查 `User.LineUserId` 是否為 null）
2. ✅ Handler 是否正確關聯到 User（檢查 `Handler.UserId`）
3. ✅ `LineMessaging:ChannelAccessToken` 是否正確
4. ✅ 檢查 `NotificationLog` 表是否有失敗記錄（Status = Failed）

**驗證處理人員綁定狀態**:
```powershell
sqlite3 src/CallTrackingSystem.Web/CallTrackingDB_Dev.db
> SELECT h.Name, u.Username, u.LineUserId, u.LineDisplayName 
  FROM Handlers h 
  INNER JOIN Users u ON h.UserId = u.Id;
```

---

## 部署到 Production 環境

### Production 環境檢查清單

- [ ] 更新 `appsettings.json`（Production 憑證）
- [ ] 設定正式域名的 Webhook URL（無需 ngrok）
- [ ] 設定正式域名的 LINE Login Callback URL
- [ ] 執行 Migration（`dotnet ef database update`）
- [ ] 驗證 HTTPS 憑證（LINE Platform 要求）
- [ ] 測試 Webhook 簽章驗證
- [ ] 測試 LINE Login OAuth 流程
- [ ] 測試對話回報流程
- [ ] 測試推送通知
- [ ] 設定監控與告警（NotificationLog 失敗率）

---

## 進階設定

### 使用 Azure Key Vault 儲存憑證（選配）

```json
{
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/"
  }
}
```

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:VaultUri"]),
    new DefaultAzureCredential());
```

在 Azure Key Vault 中儲存：
- `LineMessaging--ChannelSecret`
- `LineMessaging--ChannelAccessToken`
- `LineLogin--ClientSecret`

---

## 相關文檔

- [LINE Messaging API 文檔](https://developers.line.biz/en/docs/messaging-api/)
- [LINE Login 文檔](https://developers.line.biz/en/docs/line-login/)
- [data-model.md](data-model.md) - 資料模型設計
- [contracts/line-bot-api.yaml](contracts/line-bot-api.yaml) - LINE Bot API 契約
- [contracts/line-login-api.yaml](contracts/line-login-api.yaml) - LINE Login API 契約
- [research.md](research.md) - 技術研究報告

---

**Generated**: 2026-02-03  
**Status**: Ready for Development  
**Estimated Setup Time**: 30-45 分鐘
