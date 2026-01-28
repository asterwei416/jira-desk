# 開發環境快速啟動指南

**專案**: 客服來電問題紀錄與分析系統  
**日期**: 2026-01-28  
**目標**: 讓開發者在 30 分鐘內完成開發環境設定並成功執行系統

---

## 前置需求

### 必要軟體

| 軟體 | 版本 | 下載連結 | 用途 |
|------|------|----------|------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download/dotnet/8.0 | 執行和建置專案 |
| Visual Studio 2022 | 17.8+ | https://visualstudio.microsoft.com/ | IDE（或使用 VS Code + C# Dev Kit） |
| SQL Server | 2019+ 或 Azure SQL | https://www.microsoft.com/sql-server/sql-server-downloads | 資料庫（或使用 LocalDB） |
| Git | 最新版 | https://git-scm.com/ | 版本控制 |

### 選配軟體

| 軟體 | 版本 | 用途 |
|------|------|------|
| SQL Server Management Studio (SSMS) | 19+ | 資料庫管理 |
| Azure Data Studio | 最新版 | 跨平台資料庫工具 |
| Postman / Insomnia | 最新版 | API 測試 |
| ngrok | 最新版 | LINE Webhook 本地測試（選配） |

---

## 步驟 1: 取得原始碼

```powershell
# Clone 專案
git clone https://github.com/your-org/call-tracking-system.git
cd call-tracking-system

# 切換到功能分支
git checkout 1-customer-call-tracking
```

---

## 步驟 2: 資料庫設定

### 選項 A: 使用 SQL Server LocalDB（推薦本地開發）

LocalDB 已隨 Visual Studio 安裝，無需額外設定。

**連線字串** (appsettings.Development.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CallTrackingDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 選項 B: 使用 SQL Server Express

**下載安裝**: https://go.microsoft.com/fwlink/?linkid=866658

**連線字串**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=CallTrackingDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 選項 C: 使用 Azure SQL Database

1. 在 Azure Portal 建立 SQL Database
2. 記錄連線字串（記得加上防火牆規則允許本機 IP）

**連線字串**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:your-server.database.windows.net,1433;Database=CallTrackingDB;User ID=your-user;Password=your-password;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;"
  }
}
```

---

## 步驟 3: 設定專案

### 3.1 還原 NuGet 套件

```powershell
cd src/CallTrackingSystem.Web
dotnet restore
```

### 3.2 設定 appsettings.Development.json

在 `src/CallTrackingSystem.Web/` 目錄建立 `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CallTrackingDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "dev-secret-key-change-in-production-minimum-32-characters",
    "Issuer": "CallTrackingSystem",
    "Audience": "CallTrackingSystem",
    "ExpirationMinutes": 1440
  },
  "Line": {
    "Messaging": {
      "ChannelAccessToken": "",
      "ChannelSecret": ""
    },
    "Login": {
      "ChannelId": "",
      "ChannelSecret": "",
      "CallbackUrl": "https://localhost:5001/api/auth/line/callback"
    }
  },
  "AllowedHosts": "*"
}
```

**注意**: 初期開發可先將 LINE 設定留空，待設定 LINE Channel 後再填入。

---

## 步驟 4: 初始化資料庫

### 4.1 執行 Migrations

```powershell
# 在 Web 專案目錄執行
cd src/CallTrackingSystem.Web

# 建立 Migration（若尚未建立）
dotnet ef migrations add InitialCreate --project ../CallTrackingSystem.Infrastructure --startup-project .

# 套用 Migration
dotnet ef database update --project ../CallTrackingSystem.Infrastructure --startup-project .
```

### 4.2 驗證資料庫建立

使用 SSMS 或 Azure Data Studio 連線到資料庫，確認以下資料表已建立：

- `Users`
- `InquirySystems`
- `Handlers`
- `HandlerMappings`
- `CallRecords`
- `ChangeHistories`
- `NotificationLogs`
- `CallRecordHandlers` (多對多關聯表)

### 4.3 檢視種子資料

系統應已自動建立：
- **預設管理者帳號**: `admin` / `Admin@123`
- **5 個詢問系統**: Google 表單系統、Email 系統、客戶管理系統、財務系統、其他

可透過查詢驗證：
```sql
SELECT * FROM Users;
SELECT * FROM InquirySystems;
```

---

## 步驟 5: 執行專案

### 選項 A: 使用 Visual Studio

1. 開啟 `CallTrackingSystem.sln`
2. 設定 `CallTrackingSystem.Web` 為啟動專案
3. 按 `F5` 或點擊「執行」

### 選項 B: 使用 .NET CLI

```powershell
cd src/CallTrackingSystem.Web
dotnet run
```

### 選項 C: 使用 VS Code

1. 開啟專案根目錄
2. 安裝 C# Dev Kit 擴充套件
3. 按 `F5` 執行

**預設啟動位址**:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

---

## 步驟 6: 驗證安裝

### 6.1 健康檢查端點

瀏覽器開啟: https://localhost:5001/health

**預期回應**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "ApplicationDbContext",
      "status": "Healthy",
      "description": null
    }
  ]
}
```

### 6.2 測試登入 API

使用 Postman 或 curl 測試：

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"admin\",\"password\":\"Admin@123\"}"
```

**預期回應**:
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 86400,
  "user": {
    "id": "...",
    "username": "admin",
    "name": "系統管理員",
    "role": "Admin",
    "lineUserId": null,
    "isActive": true
  }
}
```

### 6.3 測試來電紀錄 API

先取得 Token（上一步驟的 `accessToken`），然後查詢來電紀錄：

```bash
curl -X GET https://localhost:5001/api/call-records \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**預期回應**:
```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

---

## 步驟 7: LINE Channel 設定（選配）

### 7.1 建立 LINE Messaging API Channel

1. 前往 [LINE Developers Console](https://developers.line.biz/)
2. 建立新 Provider
3. 建立 Messaging API Channel
4. 記錄 **Channel Secret** 和 **Channel Access Token**

### 7.2 建立 LINE Login Channel

1. 在同一個 Provider 下建立 LINE Login Channel
2. 設定 Callback URL: `https://localhost:5001/api/auth/line/callback`（正式環境需改為實際網址）
3. 記錄 **Channel ID** 和 **Channel Secret**

### 7.3 更新 appsettings.Development.json

```json
{
  "Line": {
    "Messaging": {
      "ChannelAccessToken": "YOUR_MESSAGING_CHANNEL_ACCESS_TOKEN",
      "ChannelSecret": "YOUR_MESSAGING_CHANNEL_SECRET"
    },
    "Login": {
      "ChannelId": "YOUR_LOGIN_CHANNEL_ID",
      "ChannelSecret": "YOUR_LOGIN_CHANNEL_SECRET",
      "CallbackUrl": "https://localhost:5001/api/auth/line/callback"
    }
  }
}
```

### 7.4 測試 LINE 通知（需先建立處理人員和來電紀錄）

1. 加 LINE Bot 為好友（掃描 Messaging API 的 QR Code）
2. 建立處理人員並填入 LINE User ID（從 LINE Bot 接收訊息取得）
3. 建立來電紀錄，系統應自動發送 LINE 通知

---

## 步驟 8: 執行測試

### 單元測試

```powershell
cd tests/CallTrackingSystem.UnitTests
dotnet test
```

### 整合測試

```powershell
cd tests/CallTrackingSystem.IntegrationTests
dotnet test
```

### 測試覆蓋率報告

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

---

## 常見問題排除

### 問題 1: Migration 執行失敗

**錯誤訊息**: `The EntityFramework package is not installed...`

**解決方式**:
```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project ../CallTrackingSystem.Infrastructure --startup-project .
```

### 問題 2: 無法連線到 LocalDB

**錯誤訊息**: `A network-related or instance-specific error...`

**解決方式**:
1. 確認 SQL Server LocalDB 已安裝
2. 執行: `sqllocaldb info` 確認實例存在
3. 若不存在: `sqllocaldb create mssqllocaldb`
4. 啟動實例: `sqllocaldb start mssqllocaldb`

### 問題 3: JWT Token 驗證失敗

**錯誤訊息**: `401 Unauthorized`

**解決方式**:
1. 確認 `appsettings.Development.json` 的 `JwtSettings.SecretKey` 至少 32 字元
2. 重新登入取得新 Token
3. 確認 Authorization Header 格式: `Bearer <token>`

### 問題 4: LINE 通知無法發送

**錯誤訊息**: `LineResponseException: 401`

**解決方式**:
1. 確認 Channel Access Token 正確且有效
2. 檢查 LINE User ID 是否正確（需使用者先加 Bot 為好友）
3. 查看 `NotificationLogs` 資料表的 `ErrorMessage` 欄位

### 問題 5: HTTPS 憑證警告

**警告訊息**: `Your connection is not private`

**解決方式** (開發環境):
```powershell
dotnet dev-certs https --trust
```

---

## 開發工具推薦設定

### Visual Studio 擴充套件

- **ReSharper** 或 **Roslynator**: 程式碼分析
- **Entity Framework Core Power Tools**: EF Core 視覺化工具
- **REST Client** 或 **Postman for VS**: API 測試

### VS Code 擴充套件

- **C# Dev Kit**: C# 開發支援
- **REST Client**: API 測試
- **SQL Server (mssql)**: 資料庫查詢
- **GitLens**: Git 歷史視覺化

---

## 開發工作流程

### 1. 建立新功能

```powershell
# 建立功能分支
git checkout -b feature/new-feature

# 進行開發...

# 執行測試
dotnet test

# 提交變更
git add .
git commit -m "feat: 實作新功能"
git push origin feature/new-feature
```

### 2. 資料庫結構變更

```powershell
# 修改 Entity 類別或 Configuration

# 建立 Migration
dotnet ef migrations add DescriptiveName --project ../CallTrackingSystem.Infrastructure --startup-project .

# 檢視 Migration 檔案（確認變更正確）

# 套用到資料庫
dotnet ef database update --project ../CallTrackingSystem.Infrastructure --startup-project .
```

### 3. API 測試流程

1. 啟動專案 (`dotnet run`)
2. 使用 Postman 匯入 API 契約檔案:
   - `specs/1-customer-call-tracking/contracts/call-records-api.yaml`
   - `specs/1-customer-call-tracking/contracts/auth-api.yaml`
   - `specs/1-customer-call-tracking/contracts/admin-api.yaml`
3. 設定環境變數 `BASE_URL` 為 `https://localhost:5001`
4. 測試各端點

---

## 效能調校建議

### 資料庫查詢監控

在 `appsettings.Development.json` 啟用 EF Core 詳細日誌：

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### 啟用熱重載（Hot Reload）

```powershell
dotnet watch run
```

檔案變更後自動重新編譯和重啟。

---

## 下一步

完成開發環境設定後，建議：

1. ✅ 閱讀 [spec.md](./spec.md) 了解完整需求
2. ✅ 閱讀 [data-model.md](./data-model.md) 了解資料結構
3. ✅ 閱讀 [research.md](./research.md) 了解技術決策
4. ⏳ 開始實作 User Story 1（來電紀錄 CRUD）

---

## 支援資源

- **專案文檔**: `specs/1-customer-call-tracking/`
- **API 規格**: `specs/1-customer-call-tracking/contracts/`
- **.NET Core 文檔**: https://docs.microsoft.com/dotnet/
- **EF Core 文檔**: https://docs.microsoft.com/ef/core/
- **LINE API 文檔**: https://developers.line.biz/

---

## 變更記錄

| 日期 | 版本 | 變更內容 |
|------|------|----------|
| 2026-01-28 | 1.0.0 | 初版建立 |
