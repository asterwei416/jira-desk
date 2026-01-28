# 任務清單：客服來電問題紀錄與分析系統

**功能**: 客服來電問題紀錄與分析系統  
**生成日期**: 2026-01-28  
**基於文檔**: [spec.md](./spec.md), [data-model.md](./data-model.md), [contracts/](./contracts/)

---

## 任務組織原則

本任務清單依照 **User Story 優先級** 組織，每個 User Story 視為一個獨立可交付的增量（increment）。各階段完成後可獨立測試和驗證價值。

**標記說明**:
- `[P]`: 可並行執行的任務（不同檔案，無依賴）
- `[US1]` ~ `[US6]`: 對應的 User Story 編號
- 無標記: 必須依序執行的任務

---

## Phase 1: 專案設定與基礎架構

**目標**: 建立 .NET Core 8 專案結構、設定資料庫連線、完成基礎設施層

**完成標準**: 
- ✅ Solution 建立成功，三層架構專案編譯通過
- ✅ 資料庫連線測試成功
- ✅ 健康檢查端點正常回應

### 任務清單

- [ ] T001 建立 Solution 和專案結構
  - 建立 `CallTrackingSystem.sln`
  - 建立專案: `CallTrackingSystem.Web`, `CallTrackingSystem.Core`, `CallTrackingSystem.Infrastructure`
  - 建立測試專案: `CallTrackingSystem.UnitTests`, `CallTrackingSystem.IntegrationTests`
  - 設定專案間參考關係（Web → Core + Infrastructure, Infrastructure → Core）

- [ ] T002 [P] 設定 NuGet 套件
  - Web: `Microsoft.AspNetCore.App`, `Microsoft.AspNetCore.Authentication.JwtBearer`
  - Infrastructure: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`, `LineDC.Messaging`
  - 測試: `xUnit`, `Moq`, `FluentAssertions`, `Microsoft.EntityFrameworkCore.InMemory`

- [ ] T003 [P] 建立 appsettings 設定檔案
  - 建立 `appsettings.json` 和 `appsettings.Development.json` 在 Web 專案
  - 設定連線字串範本
  - 設定 JWT、LINE API、日誌層級配置項

- [ ] T004 建立 ApplicationDbContext 基礎
  - 在 `Infrastructure/Data/ApplicationDbContext.cs` 建立 DbContext 類別
  - 設定基本配置（時區、命名慣例）
  - 註冊到 DI 容器

- [ ] T005 [P] 設定健康檢查端點
  - 在 `Program.cs` 註冊 `AddHealthChecks()` 並加入 DbContext 檢查
  - 建立 `/health` 端點
  - 測試端點回應正確的 JSON 格式

---

## Phase 2: 基礎層（Foundational）

**目標**: 完成共用基礎設施，供所有 User Stories 使用

**完成標準**:
- ✅ 所有實體類別和 EF Core Configuration 完成
- ✅ Migration 成功執行，資料庫建立完成
- ✅ 種子資料自動載入
- ✅ 認證中介軟體正常運作
- ✅ 全域錯誤處理器正常捕捉例外

### 任務清單

#### 資料層

- [ ] T006 [P] 建立 User 實體
  - 在 `Core/Entities/User.cs` 建立實體類別
  - 包含業務方法: `Create()`, `UpdateInfo()`, `BindLineAccount()`, `ChangePassword()`
  - 在 `Infrastructure/Data/Configurations/UserConfiguration.cs` 建立 Fluent API 配置

- [ ] T007 [P] 建立 InquirySystem 實體
  - 在 `Core/Entities/InquirySystem.cs` 建立實體類別
  - 包含業務方法: `Create()`, `UpdateName()`, `Activate()`, `Deactivate()`
  - 在 `Infrastructure/Data/Configurations/InquirySystemConfiguration.cs` 建立配置

- [ ] T008 [P] 建立 Handler 實體
  - 在 `Core/Entities/Handler.cs` 建立實體類別
  - 包含業務方法: `Create()`, `UpdateInfo()`, `Activate()`, `Deactivate()`
  - 在 `Infrastructure/Data/Configurations/HandlerConfiguration.cs` 建立配置

- [ ] T009 [P] 建立 HandlerMapping 實體
  - 在 `Core/Entities/HandlerMapping.cs` 建立實體類別
  - 包含業務方法: `Create()`
  - 在 `Infrastructure/Data/Configurations/HandlerMappingConfiguration.cs` 建立配置

- [ ] T010 [P] 建立 CallRecord 實體
  - 在 `Core/Entities/CallRecord.cs` 建立實體類別
  - 包含業務方法: `Create()`, `Update()`, `UpdateStatus()`, `TryAcquireLock()`, `ReleaseLock()`, `IsLockedByOther()`
  - 在 `Infrastructure/Data/Configurations/CallRecordConfiguration.cs` 建立配置（含編輯鎖定欄位和 RowVersion）

- [ ] T011 [P] 建立 ChangeHistory 實體
  - 在 `Core/Entities/ChangeHistory.cs` 建立實體類別
  - 包含業務方法: `Create()`
  - 在 `Infrastructure/Data/Configurations/ChangeHistoryConfiguration.cs` 建立配置

- [ ] T012 [P] 建立 NotificationLog 實體
  - 在 `Core/Entities/NotificationLog.cs` 建立實體類別
  - 包含業務方法: `CreateSuccess()`, `CreateFailure()`
  - 在 `Infrastructure/Data/Configurations/NotificationLogConfiguration.cs` 建立配置

- [ ] T013 在 ApplicationDbContext 註冊所有實體
  - 在 `OnModelCreating` 套用所有 Configuration
  - 設定 Entity 的 DbSet 屬性

- [ ] T014 建立初始 Migration
  - 執行 `dotnet ef migrations add InitialCreate`
  - 檢查生成的 Migration 檔案（資料表、欄位、索引、外鍵）
  - 執行 `dotnet ef database update` 建立資料庫

- [ ] T015 建立種子資料初始化
  - 在 `Infrastructure/Data/DbInitializer.cs` 建立靜態方法 `Initialize()`
  - 種子資料: 預設管理者帳號（admin/Admin@123）、5 個詢問系統
  - 在 `Program.cs` 呼叫種子資料初始化

#### 認證與授權

- [ ] T016 [P] 建立 JWT Token Service
  - 在 `Infrastructure/Services/JwtTokenService.cs` 實作 Token 生成和驗證
  - 介面定義在 `Core/Interfaces/IJwtTokenService.cs`
  - 包含方法: `GenerateToken(User user)`, `ValidateToken(string token)`

- [ ] T017 [P] 設定 JWT 認證中介軟體
  - 在 `Program.cs` 註冊 `AddAuthentication()` 和 `AddJwtBearer()`
  - 設定 Token 驗證參數（SecretKey、Issuer、Audience）
  - 啟用 `UseAuthentication()` 和 `UseAuthorization()`

- [ ] T018 [P] 建立授權原則
  - 定義 Admin 和 Staff 角色原則
  - 在 `Program.cs` 註冊授權原則

#### 全域錯誤處理

- [ ] T019 建立全域例外處理中介軟體
  - 在 `Web/Middleware/GlobalExceptionHandler.cs` 建立中介軟體
  - 捕捉所有例外並回傳標準化 JSON 錯誤格式
  - 記錄錯誤日誌到 ILogger

- [ ] T020 建立標準錯誤回應 DTO
  - 在 `Web/Models/ErrorResponse.cs` 定義標準錯誤格式
  - 屬性: `error`, `code`, `timestamp`

#### Repository Pattern

- [ ] T021 [P] 建立 Generic Repository 基礎類別
  - 在 `Infrastructure/Repositories/GenericRepository.cs` 建立泛型 Repository
  - 實作基本 CRUD: `GetByIdAsync()`, `GetAllAsync()`, `AddAsync()`, `UpdateAsync()`, `DeleteAsync()`
  - 介面定義在 `Core/Interfaces/IGenericRepository.cs`

---

## Phase 3: User Story 1 - 來電紀錄 CRUD

**目標**: 客服人員可快速建立、編輯、刪除、查詢來電紀錄

**完成標準**:
- ✅ 可透過 API 建立新來電紀錄（含必填欄位驗證）
- ✅ 可查詢單筆紀錄詳情
- ✅ 可更新紀錄內容（含 RowVersion 樂觀鎖定）
- ✅ 可刪除紀錄（僅管理者）
- ✅ 自動指派處理人員（依對應表）
- ✅ 首次儲存自動發送 LINE 通知

### 任務清單

#### 後端 Service 層

- [ ] T022 [P] [US1] 建立 CallRecordService
  - 在 `Core/Services/CallRecordService.cs` 建立 Service 類別
  - 介面定義在 `Core/Interfaces/ICallRecordService.cs`
  - 實作方法: `CreateAsync()`, `GetByIdAsync()`, `UpdateAsync()`, `DeleteAsync()`

- [ ] T023 [P] [US1] 建立 HandlerMappingService
  - 在 `Core/Services/HandlerMappingService.cs` 建立 Service
  - 實作方法: `GetHandlersByInquirySystemIdAsync()`
  - 用於自動指派處理人員

- [ ] T024 [P] [US1] 建立 LINE 通知 Service
  - 在 `Infrastructure/Services/LineNotificationService.cs` 建立 Service
  - 介面定義在 `Core/Interfaces/ILineNotificationService.cs`
  - 實作方法: `SendCallRecordNotificationAsync()` 使用 Flex Message 格式
  - 實作失敗記錄到 NotificationLog

- [ ] T025 [US1] CallRecordService 整合 LINE 通知
  - 在 `CreateAsync()` 方法中呼叫 LINE 通知 Service
  - 僅首次建立時發送，編輯時不發送
  - 捕捉通知例外，不阻塞主流程

#### Repository 層

- [ ] T026 [P] [US1] 建立 CallRecordRepository
  - 在 `Infrastructure/Repositories/CallRecordRepository.cs` 繼承 GenericRepository
  - 介面定義在 `Core/Interfaces/ICallRecordRepository.cs`
  - 覆寫方法加入 Include 導覽屬性（InquirySystem, Handlers）

- [ ] T027 [P] [US1] 建立 HandlerMappingRepository
  - 在 `Infrastructure/Repositories/HandlerMappingRepository.cs` 建立 Repository
  - 實作方法: `GetByInquirySystemIdAsync()`

#### Web API 層

- [ ] T028 [P] [US1] 建立 CallRecordController
  - 在 `Web/Controllers/CallRecordController.cs` 建立 API Controller
  - 路由前綴: `/api/call-records`

- [ ] T029 [US1] 實作 POST /api/call-records（建立紀錄）
  - Request DTO: `CreateCallRecordRequest` 定義在 `Web/Models/CallRecordModels.cs`
  - Response DTO: `CallRecordDetailResponse`
  - 驗證: 必填欄位、主旨≤50字、內容≤150字、電話格式
  - 成功回傳 201 Created 和完整紀錄資訊

- [ ] T030 [US1] 實作 GET /api/call-records/{id}（查詢詳情）
  - Response DTO: `CallRecordDetailResponse`
  - 包含關聯資料: InquirySystem, Handlers, ChangeHistories, 編輯鎖定狀態
  - 404 找不到時回傳標準錯誤格式

- [ ] T031 [US1] 實作 PUT /api/call-records/{id}（更新紀錄）
  - Request DTO: `UpdateCallRecordRequest` （含 RowVersion）
  - 檢查編輯鎖定狀態，未鎖定則回傳 409 Conflict
  - 樂觀鎖定: 捕捉 DbUpdateConcurrencyException 回傳 409
  - 記錄變更歷史

- [ ] T032 [US1] 實作 DELETE /api/call-records/{id}（刪除紀錄）
  - 僅 Admin 角色可執行（`[Authorize(Roles = "Admin")]`）
  - 成功回傳 204 No Content
  - 403 無權限時回傳標準錯誤格式

#### 前端 MVC Views（選配）

- [ ] T033 [P] [US1] 建立 CallRecordController（MVC）
  - 在 `Web/Controllers/CallRecordController.cs` 建立 MVC Controller
  - 路由: `/CallRecord/`

- [ ] T034 [P] [US1] 建立新增紀錄表單 View
  - 在 `Web/Views/CallRecord/Create.cshtml` 建立表單
  - 包含所有必填和選填欄位
  - Client-side 驗證（jQuery Validation）

- [ ] T035 [P] [US1] 建立編輯紀錄表單 View
  - 在 `Web/Views/CallRecord/Edit.cshtml` 建立表單
  - 檢查編輯鎖定狀態，顯示鎖定提示
  - RowVersion 隱藏欄位傳遞

- [ ] T036 [P] [US1] 建立紀錄詳情 View
  - 在 `Web/Views/CallRecord/Details.cshtml` 建立頁面
  - 顯示所有欄位和變更歷史
  - 提供編輯、刪除按鈕（依權限顯示）

#### 測試

- [ ] T037 [P] [US1] CallRecordService 單元測試
  - 測試 `CreateAsync()`: 成功建立、驗證失敗、自動指派處理人員
  - 測試 `UpdateAsync()`: 成功更新、樂觀鎖定衝突
  - 使用 Moq 模擬 Repository 和 LINE Service

- [ ] T038 [P] [US1] CallRecordController API 整合測試
  - 測試完整 CRUD 流程
  - 測試驗證錯誤回應
  - 測試權限控制（Admin vs Staff）

---

## Phase 4: User Story 2 - 搜尋與篩選

**目標**: 多條件搜尋、分頁、排序功能

**完成標準**:
- ✅ 可使用關鍵字搜尋主旨/內容
- ✅ 可使用日期區間、詢問系統、狀態、緊急度篩選
- ✅ 支援分頁（20/50/100 筆/頁）
- ✅ 支援欄位排序（來電日期、更新時間、緊急度）

### 任務清單

#### 後端 Service 層

- [ ] T039 [US2] CallRecordService 新增搜尋方法
  - 實作 `SearchAsync(SearchFilterModel filter)` 方法
  - 支援 IQueryable 動態組合查詢條件
  - 支援分頁和排序參數

#### Repository 層

- [ ] T040 [US2] CallRecordRepository 新增搜尋方法
  - 實作 `SearchAsync()` 方法
  - 使用 LINQ 動態查詢
  - 最佳化 Include 策略（避免 N+1 查詢）

#### Web API 層

- [ ] T041 [US2] 實作 GET /api/call-records（搜尋列表）
  - Query Parameters: `keyword`, `inquirySystemId`, `status`, `urgencyLevel`, `startDate`, `endDate`, `pageNumber`, `pageSize`, `sortBy`, `sortOrder`
  - Response DTO: `CallRecordPagedResponse` 包含 `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`
  - 預設排序: 來電日期降序

- [ ] T042 [US2] 建立搜尋條件 ViewModel
  - 在 `Web/Models/SearchFilterModel.cs` 定義搜尋條件
  - 驗證: startDate ≤ endDate, pageSize ≤ 100

#### 前端 MVC Views

- [ ] T043 [P] [US2] 建立搜尋表單 Partial View
  - 在 `Web/Views/CallRecord/_SearchForm.cshtml` 建立部分檢視
  - 包含所有篩選條件欄位
  - 日期快速選項: 今天、本週、本月、上月、近7天、自訂

- [ ] T044 [P] [US2] 建立紀錄清單 View
  - 在 `Web/Views/CallRecord/Index.cshtml` 建立頁面
  - 顯示搜尋表單 + 分頁清單
  - 欄位排序功能（點擊標題切換升降序）
  - 分頁控制項（上一頁、下一頁、頁碼、每頁筆數）

- [ ] T045 [US2] 實作清單 AJAX 更新
  - 使用 jQuery 攔截表單提交
  - AJAX 呼叫搜尋 API
  - 更新清單內容（無需整頁刷新）

#### 測試

- [ ] T046 [P] [US2] CallRecordRepository 搜尋方法測試
  - 測試關鍵字搜尋（主旨、內容）
  - 測試多條件組合篩選
  - 測試分頁正確性
  - 測試排序功能

- [ ] T047 [P] [US2] CallRecordController 搜尋 API 測試
  - 測試各種篩選條件組合
  - 測試分頁參數驗證
  - 測試空結果回應

---

## Phase 5: User Story 3 - 處理狀態更新與變更歷史

**目標**: 狀態流轉、變更追蹤

**完成標準**:
- ✅ 可快速切換處理狀態（Pending ↔ InProgress ↔ Completed）
- ✅ 所有欄位變更自動記錄到 ChangeHistory
- ✅ 可查詢紀錄的完整變更歷史

### 任務清單

#### 後端 Service 層

- [ ] T048 [US3] CallRecordService 新增狀態更新方法
  - 實作 `UpdateStatusAsync(int id, ProcessStatus newStatus, string userId)` 方法
  - 自動記錄狀態變更到 ChangeHistory
  - 記錄完成時間和完成者（當變更為 Completed）

- [ ] T049 [US3] 建立 ChangeHistoryService
  - 在 `Core/Services/ChangeHistoryService.cs` 建立 Service
  - 實作 `LogChangeAsync()` 方法（記錄單一欄位變更）
  - 實作 `GetHistoryAsync(int callRecordId)` 方法（查詢變更歷史）

- [ ] T050 [US3] CallRecordService 整合變更歷史記錄
  - 在 `UpdateAsync()` 方法中比對新舊值
  - 對每個變更欄位呼叫 `LogChangeAsync()`
  - 使用反射或手動比對（依團隊偏好）

#### Repository 層

- [ ] T051 [P] [US3] 建立 ChangeHistoryRepository
  - 在 `Infrastructure/Repositories/ChangeHistoryRepository.cs` 建立 Repository
  - 實作方法: `GetByCallRecordIdAsync()`, `AddAsync()`

#### Web API 層

- [ ] T052 [US3] 實作 PATCH /api/call-records/{id}/status（快速狀態更新）
  - Request Body: `{ "status": "Completed" }`
  - Response DTO: `CallRecordDetailResponse`
  - 驗證狀態值合法性

- [ ] T053 [US3] 實作 GET /api/call-records/{id}/change-history（查詢變更歷史）
  - Response DTO: `ChangeHistoryItem[]` 包含 `fieldName`, `oldValue`, `newValue`, `changedAt`, `changedBy`
  - 依時間降序排列

- [ ] T054 [US3] 實作 PUT /api/call-records/{id}/handlers（更新處理人員）
  - Request Body: `{ "handlerIds": [1, 2, 3] }`
  - 記錄處理人員變更到 ChangeHistory

#### 前端 MVC Views

- [ ] T055 [P] [US3] 在紀錄詳情頁面加入狀態切換按鈕
  - 顯示當前狀態
  - 提供快速切換按鈕（未處理 → 處理中 → 已完成）
  - AJAX 呼叫狀態更新 API

- [ ] T056 [P] [US3] 在紀錄詳情頁面加入變更歷史區塊
  - 顯示時間軸格式的變更歷史
  - 顯示變更者、變更時間、變更欄位、舊值 → 新值

#### 測試

- [ ] T057 [P] [US3] ChangeHistoryService 單元測試
  - 測試 `LogChangeAsync()`: 正確記錄變更
  - 測試 `GetHistoryAsync()`: 正確查詢歷史

- [ ] T058 [P] [US3] 狀態更新整合測試
  - 測試狀態流轉流程
  - 驗證變更歷史正確記錄

---

## Phase 6: User Story 4 - LINE 通知整合

**目標**: 新來電自動通知處理人員

**完成標準**:
- ✅ 新建紀錄時自動發送 LINE 通知
- ✅ 通知內容為 Flex Message 格式（含主旨、緊急度、聯絡人、查看連結）
- ✅ 通知失敗記錄到 NotificationLog
- ✅ 可查詢通知發送記錄

### 任務清單

#### 後端 Service 層

- [ ] T059 [US4] 完善 LineNotificationService
  - 實作 `BuildCallRecordFlexMessage()` 方法（依 research.md 的範本）
  - Flex Message 包含: 標題、詢問系統、主旨、緊急度、聯絡人、電話、查看詳情按鈕
  - 處理 LINE API 錯誤（401, 400, 429 等）

- [ ] T060 [US4] 建立 NotificationLogService
  - 在 `Core/Services/NotificationLogService.cs` 建立 Service
  - 實作 `LogNotificationAsync()` 方法
  - 實作 `GetFailedNotificationsAsync()` 方法（供管理者查詢）

#### Repository 層

- [ ] T061 [P] [US4] 建立 NotificationLogRepository
  - 在 `Infrastructure/Repositories/NotificationLogRepository.cs` 建立 Repository
  - 實作方法: `GetByCallRecordIdAsync()`, `GetFailedAsync()`

#### Web API 層

- [ ] T062 [P] [US4] 在 CallRecordController 新增通知查詢端點
  - 實作 GET /api/call-records/{id}/notifications（查詢通知記錄）
  - Response DTO: `NotificationLogItem[]` 包含 `lineUserId`, `success`, `errorMessage`, `sentAt`

#### 設定與部署

- [ ] T063 [US4] 設定 LINE Messaging API Channel
  - 依 quickstart.md 步驟建立 Channel
  - 取得 Channel Access Token 和 Channel Secret
  - 更新 appsettings.json 的 LINE 設定

- [ ] T064 [US4] 建立 Handler 並填入 LINE User ID
  - 在資料庫新增測試用 Handler
  - 填入實際的 LINE User ID（需先加 Bot 為好友）

#### 測試

- [ ] T065 [P] [US4] LineNotificationService 單元測試
  - Mock LINE API Client
  - 測試成功發送通知
  - 測試失敗處理（記錄錯誤）

- [ ] T066 [P] [US4] 通知整合測試（需實際 LINE Channel）
  - 建立測試紀錄
  - 驗證 LINE 通知實際發送
  - 驗證 NotificationLog 記錄正確

---

## Phase 7: User Story 5 - Excel 報表匯出

**目標**: 匯出搜尋結果為 Excel 檔案

**完成標準**:
- ✅ 可匯出當前搜尋結果（最多 5000 筆）
- ✅ Excel 包含三個工作表: 篩選條件摘要、明細資料、彙總統計
- ✅ 月份統計採用自然月（yyyy/MM）

### 任務清單

#### 後端 Service 層

- [ ] T067 [US5] 建立 ReportService
  - 在 `Core/Services/ReportService.cs` 建立 Service
  - 介面定義在 `Core/Interfaces/IReportService.cs`
  - 實作方法: `GenerateExcelReportAsync(SearchFilterModel filter)`

- [ ] T068 [US5] 實作 EPPlus Excel 生成邏輯
  - 安裝 NuGet: `EPPlus` (版本 7+)
  - 建立工作表 1: 篩選條件摘要（`CreateSummarySheet()`）
  - 建立工作表 2: 明細資料（`CreateDetailSheet()`）
  - 建立工作表 3: 彙總統計（`CreateStatisticsSheet()`）

- [ ] T069 [US5] 實作月份彙總統計邏輯
  - 依自然月份（yyyy/MM）分組
  - 統計: 按月份、詢問系統、處理人員的總筆數
  - 生成圖表（選配）

#### Web API 層

- [ ] T070 [US5] 實作 POST /api/reports/excel（匯出報表）
  - Request Body: `ExcelReportRequest` （篩選條件，與搜尋 API 相同）
  - Response: 二進位檔案（`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`）
  - 檔名格式: `來電紀錄_2026-01.xlsx`
  - 限制最多 5000 筆，超過回傳 400 錯誤

#### 前端 MVC Views

- [ ] T071 [P] [US5] 在搜尋頁面加入「匯出報表」按鈕
  - 取得當前篩選條件
  - AJAX POST 到報表 API
  - 處理檔案下載（使用 Blob URL 或 Form Submit）

- [ ] T072 [P] [US5] 顯示匯出進度提示
  - 點擊按鈕後顯示 Loading 遮罩
  - 完成後自動下載並關閉 Loading

#### 測試

- [ ] T073 [P] [US5] ReportService 單元測試
  - 測試 Excel 生成（使用測試資料）
  - 驗證三個工作表內容正確
  - 驗證月份統計正確

- [ ] T074 [P] [US5] 報表匯出整合測試
  - 測試完整匯出流程
  - 測試超過 5000 筆限制回應

---

## Phase 8: User Story 6 - 編輯鎖定機制

**目標**: 防止並發編輯衝突

**完成標準**:
- ✅ 開始編輯時自動取得鎖定
- ✅ 其他使用者嘗試編輯時顯示鎖定提示
- ✅ 30 分鐘無操作自動解鎖
- ✅ 管理者可強制解鎖

### 任務清單

#### 後端 Service 層

- [ ] T075 [US6] 建立 EditLockManager
  - 在 `Core/Services/EditLockManager.cs` 建立 Service
  - 實作 `AcquireLockAsync(int recordId, string userId)` 方法
  - 實作 `ReleaseLockAsync(int recordId, string userId)` 方法
  - 實作 `ForceUnlockAsync(int recordId)` 方法（僅 Admin）

- [ ] T076 [US6] 建立背景清理服務
  - 在 `Infrastructure/Services/EditLockCleanupService.cs` 建立 Hosted Service
  - 繼承 `BackgroundService`
  - 每 5 分鐘執行一次，清理過期鎖定（30 分鐘無操作）
  - 在 Program.cs 註冊: `builder.Services.AddHostedService<EditLockCleanupService>()`

#### Web API 層

- [ ] T077 [P] [US6] 實作 POST /api/call-records/{id}/lock（取得鎖定）
  - Response: `{ "success": true, "lockedUntil": "2026-01-28T15:30:00Z" }`
  - 失敗時回傳 409: `{ "success": false, "message": "此紀錄正在被張三編輯中", "lockedBy": "張三", "lockedUntil": "..." }`

- [ ] T078 [P] [US6] 實作 DELETE /api/call-records/{id}/lock（釋放鎖定）
  - 成功回傳 204 No Content

- [ ] T079 [P] [US6] 實作強制解鎖端點（僅 Admin）
  - POST /api/admin/call-records/{id}/force-unlock
  - 記錄強制解鎖日誌

#### 前端 MVC Views

- [ ] T080 [US6] 在編輯頁面加入鎖定邏輯
  - 頁面載入時呼叫 `AcquireLockAsync()`
  - 成功則允許編輯，失敗則顯示唯讀提示
  - 儲存或關閉時呼叫 `ReleaseLockAsync()`

- [ ] T081 [US6] 在紀錄詳情頁面顯示鎖定狀態
  - 顯示「正在被 XXX 編輯中」提示
  - Admin 顯示「強制解鎖」按鈕

#### 測試

- [ ] T082 [P] [US6] EditLockManager 單元測試
  - 測試取得鎖定成功/失敗
  - 測試釋放鎖定
  - 測試過期檢查邏輯

- [ ] T083 [P] [US6] 背景清理服務測試
  - 測試自動清理過期鎖定
  - 測試不清理未過期鎖定

- [ ] T084 [P] [US6] 編輯鎖定整合測試
  - 模擬兩個使用者同時編輯
  - 驗證鎖定機制正常運作

---

## Phase 9: 認證功能（內建帳號 + LINE Login）

**目標**: 雙認證模式實作

**完成標準**:
- ✅ 可使用系統內建帳號登入
- ✅ 可使用 LINE Login 登入（OAuth 2.0 流程）
- ✅ 首次 LINE 登入需管理者批准綁定
- ✅ 個人資料管理和密碼變更功能

### 任務清單

#### 後端 Service 層

- [ ] T085 [P] 建立 AuthService
  - 在 `Core/Services/AuthService.cs` 建立 Service
  - 實作 `LoginAsync(username, password)` 方法（驗證並生成 JWT）
  - 實作 `ChangePasswordAsync(userId, oldPassword, newPassword)` 方法

- [ ] T086 [P] 建立 UserService
  - 在 `Core/Services/UserService.cs` 建立 Service
  - 實作 `GetByUsernameAsync()`, `GetByLineUserIdAsync()`, `CreateAsync()`, `UpdateAsync()`
  - 實作 `BindLineAccountAsync(userId, lineUserId, role)` 方法

#### Repository 層

- [ ] T087 [P] 建立 UserRepository
  - 在 `Infrastructure/Repositories/UserRepository.cs` 建立 Repository
  - 實作查詢方法: `GetByUsernameAsync()`, `GetByLineUserIdAsync()`

#### LINE Login 整合

- [ ] T088 設定 LINE Login OAuth 2.0
  - 在 Program.cs 註冊 `AddOAuth("Line", options => { ... })`
  - 設定 Authorization Endpoint, Token Endpoint, UserInformation Endpoint
  - 設定 Callback URL: `/api/auth/line/callback`

- [ ] T089 實作 LINE Login 流程
  - GET /api/auth/line/login: 重定向到 LINE 授權頁面
  - GET /api/auth/line/callback: 處理回調，交換 Code 取得 Token，取得使用者資料
  - 檢查 LINE User ID 是否已綁定，未綁定則導向綁定頁面

- [ ] T090 實作 LINE 帳號綁定審核流程
  - POST /api/auth/line/bind: 管理者批准綁定（Request: `userId`, `lineUserId`, `role`）
  - POST /api/auth/line/unbind: 管理者解除綁定

#### Web API 層

- [ ] T091 [P] 實作 POST /api/auth/login（內建帳號登入）
  - Request: `{ "username": "admin", "password": "Admin@123" }`
  - Response: `{ "accessToken": "...", "expiresIn": 86400, "user": { ... } }`
  - 驗證帳號密碼，生成 JWT Token

- [ ] T092 [P] 實作 POST /api/auth/logout（登出）
  - Client-side 清除 Token 即可，Server 端無狀態

- [ ] T093 [P] 實作 GET /api/auth/profile（查詢個人資料）
  - 需認證
  - Response: `UserProfile`

- [ ] T094 [P] 實作 PUT /api/auth/profile（更新個人資料）
  - Request: `{ "name": "新名稱" }`
  - 只能更新自己的資料

- [ ] T095 [P] 實作 POST /api/auth/change-password（變更密碼）
  - Request: `{ "oldPassword": "...", "newPassword": "..." }`
  - 驗證舊密碼正確性

#### 前端 MVC Views

- [ ] T096 [P] 建立登入頁面
  - 在 `Web/Views/Auth/Login.cshtml` 建立頁面
  - 內建帳號登入表單 + LINE Login 按鈕

- [ ] T097 [P] 建立 LINE 帳號綁定頁面（管理者專用）
  - 在 `Web/Views/Auth/Bind.cshtml` 建立頁面
  - 顯示 LINE User ID、Display Name
  - 選擇要綁定的系統帳號或建立新帳號
  - 指派角色（Admin / Staff）

- [ ] T098 [P] 建立個人資料頁面
  - 在 `Web/Views/Auth/Profile.cshtml` 建立頁面
  - 顯示使用者資訊
  - 提供密碼變更表單

#### 設定與部署

- [ ] T099 設定 LINE Login Channel
  - 依 quickstart.md 步驟建立 LINE Login Channel
  - 取得 Channel ID 和 Channel Secret
  - 更新 appsettings.json 的 LINE Login 設定

#### 測試

- [ ] T100 [P] AuthService 單元測試
  - 測試登入成功/失敗
  - 測試密碼變更

- [ ] T101 [P] LINE Login 整合測試
  - 測試 OAuth 流程（需 Mock LINE API）
  - 測試綁定流程

---

## Phase 10: 管理功能

**目標**: 詢問系統、處理人員、使用者管理

**完成標準**:
- ✅ 可 CRUD 詢問系統
- ✅ 可 CRUD 處理人員
- ✅ 可管理處理人員對應表
- ✅ 可管理使用者帳號（僅 Admin）

### 任務清單

#### 後端 Service 層

- [ ] T102 [P] 建立 InquirySystemService
  - 實作 CRUD 方法: `GetAllAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`
  - 刪除前檢查是否有關聯的 CallRecord

- [ ] T103 [P] 建立 HandlerService
  - 實作 CRUD 方法和啟用/停用方法

- [ ] T104 [P] 建立 HandlerMappingService（完整版）
  - 實作 `CreateMappingAsync()`, `DeleteMappingAsync()`, `GetMappingsAsync()`

- [ ] T105 [P] 建立 UserManagementService
  - 實作 CRUD 方法（僅 Admin 可呼叫）
  - 實作 `ResetPasswordAsync()` 方法

#### Repository 層

- [ ] T106 [P] 建立 InquirySystemRepository
  - 實作查詢方法

- [ ] T107 [P] 建立 HandlerRepository
  - 實作查詢方法

#### Web API 層（Admin 專用）

- [ ] T108 [P] 實作詢問系統管理 API
  - GET /api/admin/inquiry-systems
  - POST /api/admin/inquiry-systems
  - PUT /api/admin/inquiry-systems/{id}
  - DELETE /api/admin/inquiry-systems/{id}

- [ ] T109 [P] 實作處理人員管理 API
  - GET /api/admin/handlers
  - POST /api/admin/handlers
  - PUT /api/admin/handlers/{id}
  - DELETE /api/admin/handlers/{id}

- [ ] T110 [P] 實作處理人員對應管理 API
  - GET /api/admin/handler-mappings
  - POST /api/admin/handler-mappings
  - DELETE /api/admin/handler-mappings/{id}

- [ ] T111 [P] 實作使用者管理 API
  - GET /api/admin/users
  - POST /api/admin/users
  - PUT /api/admin/users/{id}
  - DELETE /api/admin/users/{id}
  - POST /api/admin/users/{id}/reset-password

#### 前端 MVC Views（Admin 專用）

- [ ] T112 [P] 建立詢問系統管理頁面
  - 在 `Web/Views/Admin/InquirySystems.cshtml` 建立頁面
  - CRUD 操作 + 排序功能

- [ ] T113 [P] 建立處理人員管理頁面
  - 在 `Web/Views/Admin/Handlers.cshtml` 建立頁面
  - CRUD 操作

- [ ] T114 [P] 建立處理人員對應管理頁面
  - 在 `Web/Views/Admin/HandlerMappings.cshtml` 建立頁面
  - 顯示對應關係表
  - 新增/刪除對應

- [ ] T115 [P] 建立使用者管理頁面
  - 在 `Web/Views/Admin/Users.cshtml` 建立頁面
  - CRUD 操作 + 重設密碼

#### 測試

- [ ] T116 [P] 管理功能單元測試
  - 測試各 Service 的 CRUD 方法

- [ ] T117 [P] 管理 API 整合測試
  - 測試權限控制（僅 Admin 可存取）

---

## Phase 11: 響應式設計（選配）

**目標**: 支援桌面、平板、手機裝置

**完成標準**:
- ✅ 桌面（≥1280px）顯示表格式清單
- ✅ 手機（≤767px）顯示卡片式清單
- ✅ 手機版搜尋條件可摺疊

### 任務清單

- [ ] T118 [P] 建立響應式 CSS 樣式
  - 使用 Bootstrap 5 響應式 Grid
  - 定義 Breakpoints: 768px, 1280px

- [ ] T119 [P] 實作清單響應式切換
  - 桌面: 表格式（`<table>`）
  - 手機: 卡片式（`<div class="card">`）
  - 使用 CSS Media Query 或 Bootstrap 類別

- [ ] T120 [P] 實作搜尋表單摺疊功能
  - 手機版預設摺疊，點擊「篩選」按鈕展開
  - 使用 Bootstrap Collapse 元件

- [ ] T121 [P] 測試多裝置相容性
  - Chrome DevTools 裝置模擬
  - 實際裝置測試（iPhone, iPad, Android）

---

## Phase 12: 優化與部署

**目標**: 效能調校、部署到 IIS

**完成標準**:
- ✅ API P95 延遲 < 200ms
- ✅ 測試覆蓋率 ≥ 80%
- ✅ 部署到 Windows IIS 並正常運作

### 任務清單

#### 效能優化

- [ ] T122 啟用回應快取
  - 在 Program.cs 註冊 `AddResponseCaching()`
  - 為搜尋 API 加入 `[ResponseCache]` 屬性

- [ ] T123 資料庫查詢優化
  - 檢查慢查詢（使用 SQL Profiler 或 Azure SQL Insights）
  - 調整索引策略
  - 使用 `.AsNoTracking()` 優化唯讀查詢

- [ ] T124 實作分頁最佳化
  - 使用 `Skip()` 和 `Take()` 避免載入全部資料
  - 限制最大每頁筆數（100 筆）

#### 測試與品質

- [ ] T125 執行完整測試套件
  - 單元測試
  - 整合測試
  - API 測試

- [ ] T126 測試覆蓋率報告
  - 執行 `dotnet test --collect:"XPlat Code Coverage"`
  - 檢查覆蓋率 ≥ 80%

- [ ] T127 安全性檢查
  - SQL Injection 防護（使用參數化查詢）
  - XSS 防護（使用 Razor 自動編碼）
  - CSRF 防護（AntiForgeryToken）
  - HTTPS 強制

#### IIS 部署

- [ ] T128 設定 IIS 環境
  - 依 quickstart.md 安裝 .NET 8 Hosting Bundle
  - 建立 Application Pool (No Managed Code)
  - 設定檔案權限

- [ ] T129 發佈專案
  - 執行 `dotnet publish -c Release -o ./publish`
  - 複製檔案到 IIS 實體路徑

- [ ] T130 設定 web.config
  - 設定 In-Process Hosting Model
  - 設定環境變數 `ASPNETCORE_ENVIRONMENT=Production`
  - 啟用 stdout 日誌

- [ ] T131 設定 HTTPS
  - 安裝 SSL 憑證
  - 綁定 HTTPS (443 Port)

- [ ] T132 驗證部署
  - 測試健康檢查端點
  - 測試登入功能
  - 測試 CRUD 操作
  - 測試 LINE 通知

---

## 依賴關係圖

```
Phase 1 (專案設定)
    ↓
Phase 2 (基礎層) ← 所有 User Stories 的前置條件
    ↓
Phase 3 (US1) ← Phase 4-8 的前置條件
    ↓
Phase 4 (US2) ← 獨立
    ↓
Phase 5 (US3) ← 依賴 US1
    ↓
Phase 6 (US4) ← 依賴 US1（LINE 通知在建立紀錄時觸發）
    ↓
Phase 7 (US5) ← 依賴 US2（匯出使用搜尋結果）
    ↓
Phase 8 (US6) ← 依賴 US1（編輯鎖定在更新時使用）
    ↓
Phase 9 (認證) ← 可與 US1-6 並行，但建議先完成基本功能
    ↓
Phase 10 (管理功能) ← 依賴 Phase 9（權限控制）
    ↓
Phase 11 (響應式) ← 可與其他 Phase 並行
    ↓
Phase 12 (優化與部署) ← 所有功能完成後執行
```

---

## 並行執行建議

### Phase 3 (US1) 內部並行
- 前端 Views (T033-T036) 可與後端 Service (T022-T025) 並行開發
- Repository 層 (T026-T027) 可與 Service 層並行開發
- 測試 (T037-T038) 在相應功能完成後立即執行

### Phase 4-8 並行策略
- US2（搜尋）和 US4（LINE 通知）可並行開發（不同模組）
- US5（報表）可在 US2 完成後並行開發
- US6（編輯鎖定）可與其他 US 並行開發（獨立模組）

### Phase 9-10 並行
- 認證功能（Phase 9）與管理功能（Phase 10）可部分並行
- 建議先完成認證，再開發管理功能（依賴權限控制）

---

## 實作策略

### MVP 範圍（最小可行產品）
建議 MVP 包含：
- ✅ Phase 1-2（基礎架構）
- ✅ Phase 3（US1 - 來電紀錄 CRUD）
- ✅ Phase 4（US2 - 搜尋與篩選）
- ✅ Phase 9（認證功能）

**交付價值**: 客服人員可記錄、查詢來電問題，管理者可登入管理系統。

### 第二輪增量
- ✅ Phase 5（US3 - 狀態更新）
- ✅ Phase 6（US4 - LINE 通知）

**交付價值**: 完整的問題追蹤和自動通知流程。

### 第三輪增量
- ✅ Phase 7（US5 - Excel 報表）
- ✅ Phase 8（US6 - 編輯鎖定）
- ✅ Phase 10（管理功能）

**交付價值**: 報表分析、並發控制、完整管理後台。

### 最終優化
- ✅ Phase 11（響應式設計）
- ✅ Phase 12（優化與部署）

**交付價值**: 多裝置支援、生產環境部署。

---

## 任務統計

### 總任務數
- **總計**: 132 個任務

### 各 Phase 任務數
- Phase 1: 5 個任務
- Phase 2: 16 個任務（基礎層）
- Phase 3 (US1): 17 個任務
- Phase 4 (US2): 9 個任務
- Phase 5 (US3): 11 個任務
- Phase 6 (US4): 8 個任務
- Phase 7 (US5): 8 個任務
- Phase 8 (US6): 10 個任務
- Phase 9 (認證): 17 個任務
- Phase 10 (管理): 16 個任務
- Phase 11 (響應式): 4 個任務
- Phase 12 (優化部署): 11 個任務

### 各 User Story 任務數
- US1（來電紀錄 CRUD）: 17 個任務
- US2（搜尋與篩選）: 9 個任務
- US3（狀態更新與變更歷史）: 11 個任務
- US4（LINE 通知）: 8 個任務
- US5（Excel 報表）: 8 個任務
- US6（編輯鎖定）: 10 個任務

### 可並行任務數
- Phase 1-2: 約 50% 可並行（標記 [P]）
- Phase 3-8: 約 40% 可並行
- Phase 9-10: 約 60% 可並行

### 估算工時（參考）
- 1 個開發者全職: 約 8-10 週
- 2 個開發者並行: 約 5-6 週
- 3 個開發者並行: 約 4-5 週

---

## 驗收檢核清單

### Phase 完成檢核
每個 Phase 完成後，檢查以下項目：

- [ ] 所有任務已完成
- [ ] 單元測試通過（覆蓋率 ≥ 80%）
- [ ] 整合測試通過
- [ ] API 端點測試通過（使用 Postman/Swagger）
- [ ] 前端頁面功能正常
- [ ] 無 Lint 錯誤和警告
- [ ] 程式碼已提交到 Git
- [ ] Pull Request 已通過 Code Review

### 最終部署檢核
- [ ] 所有 User Stories 驗收情境通過
- [ ] 效能測試達標（P95 < 200ms）
- [ ] 安全性檢查通過
- [ ] IIS 部署成功
- [ ] 健康檢查端點正常
- [ ] LINE 通知實際發送成功
- [ ] 備份還原測試通過
- [ ] 使用者操作手冊完成
- [ ] 管理者操作手冊完成

---

## 更新記錄

| 日期 | 版本 | 變更內容 |
|------|------|----------|
| 2026-01-28 | 1.0.0 | 初版生成，132 個任務定義完成 |
