# LINE 通知整合測試指南

## ✅ 已完成配置

### 1. LINE Channel 設定
- ✅ Channel Access Token: `d47d4afa2406a9afb241df2201e0a7b4`
- ✅ Channel Secret: `1.Ej4Y2jc/...（已配置）`
- ✅ 配置檔案: `appsettings.Development.json` 已更新

### 2. 測試資料建立
- ✅ 詢問系統: 3 筆（訂單查詢系統、會員服務系統、物流追蹤系統）
- ✅ 處理人員: 1 筆（測試人員，LINE User ID: `U7d6dea4b033c775bc811eabe558b3607`）
- ✅ 對應關係: 1 筆（訂單查詢系統 → 測試人員）

---

## 🚀 測試步驟

### 前置作業（必須完成）

**重要**: 您必須先將 LINE Bot 加為好友，否則無法接收通知！

1. **登入 LINE Developers Console**
   - 前往: https://developers.line.biz/console/
   - 選擇您的 Channel

2. **掃描 Bot QR Code**
   - 在 Channel 設定頁面，找到 "Messaging API" 標籤
   - 找到 "QR code" 區塊
   - 使用您的 LINE 手機 App 掃描 QR Code
   - 加入 Bot 為好友

3. **驗證好友狀態**
   - 打開 LINE App
   - 確認好友列表中有您的 Bot
   - 可以傳送測試訊息確認 Bot 有回應（如果有設定回應）

---

### 整合測試：T066 通知實際發送

#### 方式 1: 使用 Swagger UI（推薦）

1. **啟動應用程式**
   ```powershell
   cd CallTrackingSystem\src\CallTrackingSystem.Web
   dotnet run
   ```

2. **開啟 Swagger UI**
   - 瀏覽器前往: http://localhost:5099/swagger
   - 找到 `POST /api/call-records` 端點

3. **建立新來電紀錄**
   - 點擊 "Try it out"
   - 使用以下 JSON（填入實際值）:
   
   ```json
   {
     "customerName": "測試客戶",
     "phoneNumber": "0912345678",
     "inquirySystemId": 9,
     "problemDescription": "測試 LINE 通知功能",
     "problemKeywords": "測試,通知",
     "resolutionDescription": ""
   }
   ```
   
   **注意**: `inquirySystemId` 請填入 `9`（訂單查詢系統的 ID，從上方測試資料得知）

4. **檢查回應**
   - 應該會收到 `201 Created` 回應
   - 回應 Body 中會包含新建立的紀錄 ID

#### 方式 2: 使用 PowerShell 腳本

```powershell
# 建立來電紀錄
$body = @{
    customerName = "測試客戶"
    phoneNumber = "0912345678"
    inquirySystemId = 9
    problemDescription = "測試 LINE 通知功能"
    problemKeywords = "測試,通知"
    resolutionDescription = ""
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "http://localhost:5099/api/call-records" `
    -Method POST `
    -ContentType "application/json" `
    -Body $body

Write-Host "建立成功！紀錄 ID: $($response.id)" -ForegroundColor Green
```

---

## ✅ 驗證結果

### 1. LINE 手機 App 檢查

您應該會在 LINE App 中收到一則 Flex Message 通知，格式如下：

```
📞 新來電紀錄通知

客戶姓名: 測試客戶
電話: 0912345678
詢問系統: 訂單查詢系統
問題描述: 測試 LINE 通知功能
關鍵字: 測試, 通知
狀態: 待處理
通報時間: 2026-01-29 14:30:00

[查看詳情] ← 可點擊按鈕
```

### 2. 資料庫檢查

檢查通知記錄是否已儲存：

```powershell
# 在 CallTrackingSystem\scripts\SeedTestData 目錄下執行
dotnet run

# 或手動查詢資料庫
# 使用 VS Code 的 SQLite 擴充套件開啟：
# src\CallTrackingSystem.Web\CallTrackingDB_Dev.db
# 執行查詢:
# SELECT * FROM NotificationLogs ORDER BY SentAt DESC LIMIT 5;
```

預期結果：
- `NotificationLogs` 資料表中應該有新的記錄
- `Success` 欄位為 `1`（成功）
- `LineUserId` 為 `U7d6dea4b033c775bc811eabe558b3607`
- `ErrorMessage` 為 `NULL`

### 3. 應用程式 Log 檢查

在應用程式執行的 PowerShell 視窗中，應該會看到類似的 Log:

```
info: CallTrackingSystem.Infrastructure.Services.LineNotificationService[0]
      正在發送 LINE 通知給 1 位處理人員...
info: CallTrackingSystem.Infrastructure.Services.LineNotificationService[0]
      LINE 通知發送成功: U7d6dea4b033c775bc811eabe558b3607
```

---

## ❌ 常見問題排除

### 問題 1: 沒有收到 LINE 通知

**可能原因**:
- ❌ 沒有將 Bot 加為好友
- ❌ Channel Access Token 過期或錯誤
- ❌ 防火牆阻擋對外連線

**解決方式**:
1. 確認 Bot 在您的 LINE 好友列表中
2. 重新檢查 `appsettings.Development.json` 中的 Token
3. 檢查 NotificationLogs 的 `ErrorMessage` 欄位

### 問題 2: 收到通知但內容不正確

**可能原因**:
- ❌ `DetailUrlBase` 設定錯誤

**解決方式**:
1. 檢查 `appsettings.Development.json`:
   ```json
   "DetailUrlBase": "http://localhost:5099/call-records"
   ```
2. 確認應用程式在 5099 Port 上執行

### 問題 3: NotificationLog 顯示失敗

**檢查步驟**:
1. 查詢錯誤訊息:
   ```sql
   SELECT * FROM NotificationLogs WHERE Success = 0 ORDER BY SentAt DESC;
   ```
2. 常見錯誤:
   - `"Invalid reply token"`: User ID 不正確或未加好友
   - `"401 Unauthorized"`: Channel Access Token 錯誤
   - `"400 Bad Request"`: Flex Message 格式錯誤

---

## 📝 下一步

T066 整合測試完成後，Phase 6（LINE 通知整合）即可標記為完成！

如果測試成功，請繼續以下步驟：
1. 在 [tasks.md](../tasks.md) 中標記 `T066` 為完成
2. 進入 Phase 7（依據專案計劃）
3. 或開始進行其他優化（效能、安全性、UI/UX）

---

## 🔗 相關文件

- [quickstart.md](../quickstart.md) - LINE Channel 申請步驟
- [data-model.md](../data-model.md) - NotificationLog 實體說明
- [contracts/call-records-api.yaml](../contracts/call-records-api.yaml) - API 規格
