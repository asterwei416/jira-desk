# 測試系統 SOP（CallTrackingSystem）

> 目的：提供一套「照著做就能驗證功能」的測試步驟，包含 Web UI（MVC）與 Web API（Swagger / PowerShell）。

---

## 1. 測試前準備

### 1.1 必要工具

- .NET 8 SDK
- 瀏覽器（Chrome/Edge）
- （選用）Postman
- （選用）PowerShell（Windows 內建即可）

### 1.2 服務啟動（開發模式）

在專案根目錄執行：

```powershell
Set-Location "./CallTrackingSystem/src/CallTrackingSystem.Web"
# 若 5099 被占用，建議固定指定 5000
dotnet run --urls "http://localhost:5000"
```

看到類似訊息表示成功：

- `Now listening on: http://localhost:5000`

### 1.3 常用入口

- 首頁（MVC）：`http://localhost:5000/`
- Swagger（API 文件）：`http://localhost:5000/swagger`
- 健康檢查：`http://localhost:5000/health`

### 1.4 資料庫說明（開發模式）

開發模式預設使用 SQLite 檔案資料庫（Web 專案目錄下可見）：

- `CallTrackingDB_Dev.db`

> 重啟伺服器「資料不會消失」，但若您刪除該檔案，資料會被重建（並重新跑種子資料）。

---

## 2. 最快驗收（Smoke Test，5 分鐘）

### 2.1 驗證服務存活

開啟：`http://localhost:5000/health`

預期：HTTP 200，回傳健康狀態 JSON。

### 2.2 開啟 Swagger

開啟：`http://localhost:5000/swagger`

預期：可看到多個 API Controller（CallRecords、Auth、Reports、Admin...）。

### 2.3 建立一筆來電紀錄（API）

先在 Swagger 找 `InquirySystems` → `GET /api/InquirySystems`，取得可用的 `id`。

再在 `CallRecords` → `POST /api/CallRecords`，送出範例（`inquirySystemId` 請改成你查到的 id）：

```json
{
  "inquirySystemId": 1,
  "subject": "測試主旨",
  "content": "測試內容",
  "urgencyLevel": "Medium",
  "contactName": "王小明",
  "contactPhone": "0912-345678",
  "faqReference": "https://example.com"
}
```

預期：HTTP 201，回傳包含 `id` 的資料。

### 2.4 查詢剛建立的紀錄

在 Swagger 找 `GET /api/CallRecords/{id}`，帶入上一步回傳的 `id`。

預期：HTTP 200，能看到主旨/內容/詢問系統/處理人員等。

### 2.5 一鍵執行 Smoke Test（推薦）

如果你不想手動點 Swagger，可以直接用一鍵腳本跑一輪核心驗收：

```powershell
Set-Location "./CallTrackingSystem"
./scripts/run-smoke-tests.ps1 -BaseUrl "http://localhost:5000"
```

> 若遇到中文亂碼或 PowerShell 解析錯誤，請改用 PowerShell 7（pwsh）：

```powershell
pwsh -NoProfile -File "./scripts/run-smoke-tests.ps1" -BaseUrl "http://localhost:5000"
```

預期：

- 依序驗證 /health、登入、Admin API、建立/查詢/更新狀態/變更歷史、Excel 下載、刪除
- Excel 會輸出到 `CallTrackingSystem/artifacts/smoke/`

---

## 3. API 測試（完整功能）

> 建議使用 Swagger 操作最直覺；若要可重複執行，使用 PowerShell 範例。

### 3.1 來電紀錄 CRUD

#### A. 建立（Create）

- `POST /api/CallRecords`
- 預期：201 Created

#### B. 讀取（Read）

- `GET /api/CallRecords/{id}`
- 預期：200 OK；不存在時：404

#### C. 更新（Update）

- `PUT /api/CallRecords/{id}`
- 預期：200 OK
- 可能情況：
  - 找不到：404
  - 被鎖定：409（訊息包含「編輯中」）

#### D. 刪除（Delete）

- `DELETE /api/CallRecords/{id}`
- 預期：204 No Content；不存在時：404

### 3.2 搜尋、分頁、排序

使用：`GET /api/CallRecords`（QueryString）

常用參數：

- `keyword=基站`
- `inquirySystemId=1`
- `status=Pending`（或 `Completed`）
- `urgencyLevel=High`（Low/Medium/High）
- `startDate=2026-02-01`、`endDate=2026-02-28`
- `pageNumber=1`、`pageSize=20`
- `sortBy=CreatedAt`、`sortOrder=desc`

預期：200 OK，回傳分頁結果（items、totalCount、totalPages...）。

### 3.3 狀態更新

- `PATCH /api/CallRecords/{id}/status`

Body 範例：

```json
{ "status": "Completed" }
```

預期：200 OK；狀態值不合法：400。

### 3.4 變更歷史

- `GET /api/CallRecords/{id}/change-history`

預期：200 OK，能看到欄位變更紀錄（依時間排序）。

### 3.5 編輯鎖定（重要）

#### A. 取得鎖定

- `POST /api/CallRecords/{id}/lock`

預期：
- 成功：200 OK，`acquired=true`
- 被其他人鎖定：409 Conflict

> 目前 API 端的使用者 ID 為暫時測試值（`test-user-001`），因此「多使用者互斥」較適合用整合測試或改成 JWT 後再做完整驗證。

#### B. 查詢鎖定狀態

- `GET /api/CallRecords/{id}/lock`

#### C. 釋放鎖定

- `DELETE /api/CallRecords/{id}/lock`

### 3.6 更新處理人員

- `PUT /api/CallRecords/{id}/handlers`

Body 範例：

```json
{ "handlerIds": [1, 2] }
```

預期：200 OK；handlerIds 空或不合法：400。

### 3.7 通知記錄查詢

- `GET /api/CallRecords/{id}/notifications`

預期：200 OK，包含成功/失敗、錯誤訊息、發送時間。

### 3.8 Excel 報表匯出

- `POST /api/Reports/excel`

Body 會與搜尋條件相同（例如）：

```json
{
  "keyword": "測試",
  "startDate": "2026-02-01",
  "endDate": "2026-02-28",
  "pageNumber": 1,
  "pageSize": 20
}
```

預期：回傳 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` 並觸發下載。

---

## 4. MVC（畫面）測試

### 4.1 來電紀錄清單

- `GET /CallRecord`

預期：
- 可看到搜尋區、清單、分頁控制。
- 手機寬度（DevTools 模擬）會切換卡片樣式。

### 4.2 新增紀錄

- `GET /CallRecord/Create`

預期：
- 必填欄位有提示
- 送出成功後能回到清單或顯示成功訊息（依目前頁面實作）

### 4.3 編輯紀錄（含鎖定）

- `GET /CallRecord/Edit/{id}`

預期：
- 進入頁面會嘗試取得鎖定
- 若鎖定失敗會顯示唯讀/提示
- 離開頁面或送出時會釋放鎖定

---

## 5. 權限/角色測試（Admin vs Staff）

> Admin 專用 API 路由皆以 `api/admin/...` 開頭。

### 5.1 取得 JWT（內建帳密）

- `POST /api/auth/login`

Body 範例：

```json
{ "username": "admin", "password": "Admin@123" }
```

預期：200 OK，回傳 `accessToken`。

### 5.2 在 Swagger 設定 Bearer Token

Swagger 右上 `Authorize` → 輸入：

`Bearer <accessToken>`

### 5.3 驗證 Admin API

在 Swagger 測試：

- `GET /api/admin/users`
- `GET /api/admin/inquiry-systems`
- `GET /api/admin/handlers`

預期：
- 帶 token：200 OK
- 不帶 token：401
- 帶 Staff token（若有）：403

---

## 6. 測試資料準備（可選）

若需要快速灌入測試資料，可使用腳本專案：

```powershell
# 從專案根目錄
Set-Location "./CallTrackingSystem"

dotnet run --project "./scripts/SeedTestData/SeedTestData.csproj"
```

或使用 SQL/腳本：

- `CallTrackingSystem/scripts/seed-test-data.sql`
- `CallTrackingSystem/scripts/SeedTestData.csx`

---

## 7. 自動化測試（建議每次改完都跑）

### 7.1 跑全部測試

```powershell
Set-Location "./CallTrackingSystem"
dotnet test
```

### 7.2 常見預期

- 全部通過：0 failed
- 若失敗：先看第一個 failing test 的錯誤堆疊

---

## 8. 常見問題排除

### 8.1 Port 被占用（常見）

症狀：`Failed to bind to address http://localhost:xxxx` / `存取權限不足`。

解法：

```powershell
Set-Location "./CallTrackingSystem/src/CallTrackingSystem.Web"
dotnet run --urls "http://localhost:5000"
```

### 8.2 看到 Static files 警告

若出現 `WebRootPath was not found ... wwwroot`：

- 目前已補上空的 `wwwroot/` 目錄，警告應消失。

### 8.3 LINE Login / LINE 通知

這兩項會依賴外部 LINE Developers Console 與 Channel 設定。

- 若沒設定好，系統仍可測 CRUD / 搜尋 / 報表 / 鎖定等核心功能。

---

## 9. 建議的回歸測試清單（每次發佈前）

1. /health 回 200
2. 建立紀錄 → 查詢 → 更新 → 查詢 → 刪除
3. 搜尋（keyword + 日期區間 + 分頁）
4. 狀態更新（Pending → Completed）
5. 變更歷史有記錄
6. 取得鎖定 → 更新 → 釋放鎖定
7. 匯出 Excel 可下載、含三工作表
8. Admin API：帶 token 可用、不帶 token 401

