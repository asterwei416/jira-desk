# 實作計劃：客服來電問題紀錄與分析系統

**分支**: `1-customer-call-tracking` | **日期**: 2026-01-28 | **規範**: [spec.md](./spec.md)  
**輸入**: 功能規範來自 `/specs/1-customer-call-tracking/spec.md`

## 摘要

建置內部客服來電問題紀錄與分析系統，提供即時紀錄、多條件搜尋、報表匯出、LINE@ 通知等核心功能。採用 .NET Core 8 + Azure SQL + EF Core Code First 架構，支援雙重身份驗證（系統內建帳號 + LINE Login），整合 LINE Messaging API 推送 Flex Message 格式通知，部署於 Windows IIS 環境。

## 技術環境

**語言/版本**: .NET Core 8 (C# 12)  
**主要依賴**: 
  - ASP.NET Core MVC/Web API
  - Entity Framework Core 8 (Code First)
  - LINE Messaging API SDK
  - LINE Login SDK
  
**儲存**: Azure SQL Database (關聯式資料庫)  
**測試**: xUnit + Moq + FluentAssertions  
**目標平台**: Windows Server IIS 10+  
**專案類型**: Web Application (前後端整合)  
**效能目標**: 
  - API P95 延遲 < 200ms
  - 支援 10 位並發使用者
  - 1000 筆報表匯出 < 30 秒
  
**限制**: 
  - LINE@ 通知發送 < 5 秒
  - 編輯鎖定 30 分鐘自動解除
  - 檔案上傳無需求（純資料紀錄）
  
**規模/範疇**: 
  - 初期 20-50 位使用者
  - 預估每日 50-100 筆新紀錄
  - 資料保留無期限限制

## 憲法檢查

*關卡: Phase 0 研究前必須通過。Phase 1 設計後重新檢查。*

### I. 程式碼品質標準

- ✅ **可讀性優先**: C# 命名規範（PascalCase for classes/methods, camelCase for variables）
- ✅ **單一職責原則**: Controller/Service/Repository 分層架構，每層職責明確
- ✅ **DRY 原則**: 使用 POCO 類別共用模型，避免重複 DTO 映射
- ✅ **型別安全**: C# 強型別語言，nullable reference types 啟用
- ✅ **錯誤處理**: 全域 Exception Filter + 標準化錯誤回應格式
- ✅ **程式碼審查**: Git Pull Request + Code Review 流程

**評估**: 通過 ✅

### II. 測試優先開發

- ✅ **先寫測試**: TDD 流程，單元測試先行
- ✅ **紅綠重構循環**: xUnit 測試框架支援
- ✅ **覆蓋率要求**: 目標 80% 程式碼覆蓋率（使用 Coverlet）
- ✅ **測試類別**:
  - 單元測試: 服務層業務邏輯（xUnit + Moq）
  - 整合測試: Repository 層資料存取（In-Memory Database）
  - 契約測試: API 端點測試（TestServer + HttpClient）
- ✅ **無測試不實作**: CI/CD Pipeline 強制測試通過

**評估**: 通過 ✅

### III. 使用者體驗一致性

- ✅ **設計系統合規**: Bootstrap 5 UI 框架統一樣式
- ✅ **互動模式**: 標準 CRUD 操作模式一致
- ✅ **無障礙標準**: HTML semantic tags + ARIA labels
- ✅ **錯誤訊息**: ModelState 驗證統一錯誤格式
- ✅ **回應回饋**: AJAX 操作顯示 loading spinner
- ✅ **本地化準備**: 繁體中文為主要語言

**評估**: 通過 ✅

### IV. 效能要求

- ✅ **回應時間目標**: 
  - API 端點使用非同步 async/await 模式
  - EF Core 查詢優化（Include/Select 避免 N+1）
  - 清單分頁降低單次查詢負載
  
- ✅ **資源限制**:
  - Azure SQL 適當索引策略
  - Connection Pooling 預設啟用
  - 無需 Redis（小型系統，Session 使用 SQL Server）
  
- ✅ **優化要求**:
  - 靜態資源（CSS/JS）bundling & minification
  - 圖片優化不適用（無圖片上傳需求）
  - 大型報表匯出使用背景任務（選配）
  
- ✅ **效能測試**: 負載測試計劃（選配，Phase 2 後執行）
- ✅ **監控**: IIS Application Insights 或 Serilog 日誌

**評估**: 通過 ✅

### V. 文檔語言標準

- ✅ **規範文檔**: spec.md 使用繁體中文 ✓
- ✅ **實作計劃**: 本文件使用繁體中文 ✓
- ✅ **任務清單**: tasks.md 將使用繁體中文
- ✅ **使用者文檔**: README、部署文件使用繁體中文
- ✅ **程式碼註解**: 業務邏輯註解使用繁體中文，技術註解可用英文
- ✅ **提交訊息**: Git commit messages 使用繁體中文

**評估**: 通過 ✅

### 整體評估

**狀態**: ✅ 所有憲法原則檢查通過，無違規項目需要說明

## 專案結構

### 文檔 (此功能)

```text
specs/1-customer-call-tracking/
├── spec.md                  # 功能規範
├── plan.md                  # 本文件 (實作計劃)
├── research.md              # Phase 0 輸出 (技術研究)
├── data-model.md            # Phase 1 輸出 (資料模型設計)
├── quickstart.md            # Phase 1 輸出 (快速開始指南)
├── contracts/               # Phase 1 輸出 (API 契約)
│   ├── call-records-api.yaml    # 來電紀錄 API OpenAPI 規範
│   ├── auth-api.yaml            # 身份驗證 API OpenAPI 規範
│   └── admin-api.yaml           # 管理功能 API OpenAPI 規範
└── checklists/
    └── requirements.md      # 需求檢查清單
```

### 原始碼 (儲存庫根目錄)

```text
CallTrackingSystem/
├── src/
│   ├── CallTrackingSystem.Web/              # ASP.NET Core MVC/Web API 專案
│   │   ├── Controllers/                     # MVC Controllers & API Controllers
│   │   │   ├── CallRecordController.cs     # 來電紀錄 CRUD
│   │   │   ├── SearchController.cs         # 搜尋與篩選
│   │   │   ├── ReportController.cs         # 報表匯出
│   │   │   ├── AuthController.cs           # 身份驗證
│   │   │   └── AdminController.cs          # 系統設定管理
│   │   ├── Models/                          # View Models & Request/Response DTOs
│   │   │   ├── CallRecordViewModel.cs
│   │   │   ├── SearchFilterModel.cs
│   │   │   └── LoginViewModel.cs
│   │   ├── Views/                           # Razor Views
│   │   │   ├── CallRecord/                 # 紀錄相關頁面
│   │   │   ├── Search/                     # 搜尋頁面
│   │   │   ├── Admin/                      # 管理後台
│   │   │   └── Shared/                     # 共用 Layout & Partials
│   │   ├── wwwroot/                         # 靜態資源
│   │   │   ├── css/                        # 樣式表
│   │   │   ├── js/                         # JavaScript
│   │   │   └── lib/                        # 第三方 libraries (Bootstrap)
│   │   ├── Filters/                         # Action Filters (Auth, Exception)
│   │   ├── Middleware/                      # 自定義 Middleware
│   │   ├── appsettings.json                # 應用程式設定
│   │   ├── appsettings.Development.json    # 開發環境設定
│   │   └── Program.cs                       # 應用程式進入點
│   │
│   ├── CallTrackingSystem.Core/             # 核心業務邏輯層 (Class Library)
│   │   ├── Entities/                        # Domain Entities (EF Core Models)
│   │   │   ├── CallRecord.cs               # 來電紀錄實體
│   │   │   ├── InquirySystem.cs            # 詢問系統實體
│   │   │   ├── Handler.cs                  # 處理人員實體
│   │   │   ├── HandlerMapping.cs           # 處理人員對應表實體
│   │   │   ├── ChangeHistory.cs            # 變更歷史實體
│   │   │   ├── NotificationLog.cs          # 通知記錄實體
│   │   │   └── User.cs                     # 使用者實體
│   │   ├── Interfaces/                      # 服務與儲存庫介面
│   │   │   ├── ICallRecordService.cs
│   │   │   ├── ISearchService.cs
│   │   │   ├── IReportService.cs
│   │   │   ├── ILineNotificationService.cs
│   │   │   ├── IAuthService.cs
│   │   │   └── IRepository.cs (Generic)
│   │   ├── Services/                        # 業務邏輯服務實作
│   │   │   ├── CallRecordService.cs
│   │   │   ├── SearchService.cs
│   │   │   ├── ReportService.cs
│   │   │   ├── LineNotificationService.cs
│   │   │   └── AuthService.cs
│   │   ├── Enums/                           # 列舉定義
│   │   │   ├── UrgencyLevel.cs             # 緊急度
│   │   │   ├── ProcessStatus.cs            # 處理狀態
│   │   │   └── AuthProvider.cs             # 身份驗證提供者
│   │   └── Constants/                       # 常數定義
│   │
│   └── CallTrackingSystem.Infrastructure/   # 基礎設施層 (Class Library)
│       ├── Data/                            # EF Core 資料存取
│       │   ├── ApplicationDbContext.cs      # DbContext
│       │   ├── Configurations/              # Entity Configurations (Fluent API)
│       │   │   ├── CallRecordConfiguration.cs
│       │   │   └── UserConfiguration.cs
│       │   └── Migrations/                  # EF Core Migrations
│       ├── Repositories/                    # Repository 實作
│       │   ├── GenericRepository.cs
│       │   ├── CallRecordRepository.cs
│       │   └── UserRepository.cs
│       ├── ExternalServices/                # 外部服務整合
│       │   ├── LineMessagingService.cs      # LINE Messaging API
│       │   └── LineLoginService.cs          # LINE Login API
│       └── Helpers/                         # 輔助工具類別
│           ├── ExcelHelper.cs               # Excel 報表生成
│           └── EditLockManager.cs           # 編輯鎖定管理
│
├── tests/
│   ├── CallTrackingSystem.UnitTests/        # 單元測試專案
│   │   ├── Services/                        # 服務層測試
│   │   │   ├── CallRecordServiceTests.cs
│   │   │   ├── SearchServiceTests.cs
│   │   │   └── ReportServiceTests.cs
│   │   └── Helpers/                         # 輔助類別測試
│   │
│   ├── CallTrackingSystem.IntegrationTests/ # 整合測試專案
│   │   ├── Repositories/                    # Repository 層測試
│   │   │   └── CallRecordRepositoryTests.cs
│   │   └── ExternalServices/                # 外部服務整合測試
│   │       └── LineMessagingServiceTests.cs
│   │
│   └── CallTrackingSystem.ApiTests/         # API 契約測試專案
│       ├── Controllers/                     # Controller 端點測試
│       │   ├── CallRecordControllerTests.cs
│       │   └── AuthControllerTests.cs
│       └── TestFixtures/                    # 測試基礎設施
│
├── docs/                                    # 專案文檔
│   ├── deployment/                          # 部署文檔
│   │   ├── iis-setup.md                    # IIS 設定指南
│   │   └── azure-sql-setup.md              # Azure SQL 設定指南
│   └── development/                         # 開發文檔
│       └── coding-standards.md              # 編碼規範
│
├── .gitignore
├── CallTrackingSystem.sln                   # Visual Studio Solution
└── README.md                                # 專案說明文件
```

**結構決策**: 

選擇 **Web Application (Option 2 變體)** 結構，原因如下：

1. **三層架構明確分離**:
   - `Web` 層: 處理 HTTP 請求、View 渲染、API 端點
   - `Core` 層: 純業務邏輯，不依賴任何基礎設施
   - `Infrastructure` 層: EF Core、LINE API、檔案操作等實作細節

2. **符合 Clean Architecture 原則**:
   - Core 不依賴任何外部套件（除 .NET BCL）
   - 依賴反轉：Core 定義介面，Infrastructure 實作
   - 測試友善：可輕鬆 Mock 外部依賴

3. **專案類型對應**:
   - 非純後端 API（有 Razor Views UI）
   - 非前後端完全分離（不需要獨立 React/Vue 專案）
   - 整合式 MVC + Web API 混合架構

4. **不使用 Redis 的理由**:
   - 初期規模小（20-50 使用者）
   - Session 使用 SQL Server 儲存即可
   - 編輯鎖定機制使用資料庫欄位 + 定時清理

5. **不使用 AutoMapper 的理由**:
   - POCO 類別直接在各層間傳遞
   - 手動映射提供更好的型別安全和可讀性
   - 避免隱藏的效能成本和設定複雜度

## 複雜性追蹤

> **僅在憲法檢查有違規需要說明時填寫**

無違規項目。

## Phase 0: 大綱與研究

**目標**: 解決技術環境中所有 "NEEDS CLARIFICATION" 項目，研究最佳實踐與設計模式。

### 研究任務

1. **.NET Core 8 + EF Core Code First 最佳實踐**
   - 研究內容: Entity Configuration Fluent API 模式、Migration 策略、Connection Resiliency
   - 輸出: Entity 設計模式建議、索引策略、查詢優化技巧

2. **LINE Messaging API 整合**
   - 研究內容: Push Message API、Flex Message 格式設計、Webhook 處理（若未來需要雙向）
   - 輸出: LINE Bot Channel 設定步驟、Flex Message 範本、錯誤處理策略

3. **LINE Login 整合**
   - 研究內容: OAuth 2.0 流程、Token 驗證、使用者綁定機制
   - 輸出: 登入流程圖、安全性考量、Session 管理策略

4. **Azure SQL 最佳實踐**
   - 研究內容: DTU vs vCore 選擇、備份策略、連線池設定
   - 輸出: 初期規模建議配置、效能監控指標、成本估算

5. **IIS 部署最佳實踐**
   - 研究內容: ASP.NET Core Hosting Bundle、Application Pool 設定、HTTPS 憑證
   - 輸出: IIS 設定檢查清單、web.config 範例、健康檢查端點

6. **Excel 報表生成**
   - 研究內容: EPPlus vs ClosedXML vs NPOI 比較、大型資料集處理
   - 輸出: 套件選擇建議、效能測試結果、記憶體使用最佳化

7. **編輯鎖定機制**
   - 研究內容: 樂觀鎖 vs 悲觀鎖、分散式鎖定（不使用 Redis 的替代方案）
   - 輸出: 基於資料庫的鎖定設計、自動解鎖實作方式

8. **測試策略**
   - 研究內容: xUnit 最佳實踐、Moq 進階用法、FluentAssertions 語法
   - 輸出: 測試專案結構建議、CI/CD 整合方式

### 輸出

`research.md` - 包含所有研究任務的決策、理由、替代方案評估

## Phase 1: 設計與契約

**前置條件**: `research.md` 完成

### 任務

1. **資料模型設計** → `data-model.md`
   - 從 spec.md 的 7 個關鍵實體提取
   - Entity 類別設計（屬性、導覽屬性、驗證規則）
   - EF Core Fluent API Configuration
   - 索引策略（來電日期、處理狀態、緊急度組合索引）
   - 關聯關係映射（一對多、多對多）

2. **API 契約設計** → `contracts/`
   - `call-records-api.yaml`: 
     - POST /api/call-records (建立紀錄)
     - GET /api/call-records/{id} (取得明細)
     - PUT /api/call-records/{id} (更新紀錄)
     - PATCH /api/call-records/{id}/status (更新狀態)
     - GET /api/call-records/search (搜尋篩選)
     - POST /api/call-records/{id}/lock (取得編輯鎖)
     - DELETE /api/call-records/{id}/lock (釋放編輯鎖)
   
   - `auth-api.yaml`:
     - POST /api/auth/login (本地帳號登入)
     - POST /api/auth/line-login (LINE Login 回調)
     - POST /api/auth/logout (登出)
     - GET /api/auth/profile (取得當前使用者資訊)
   
   - `admin-api.yaml`:
     - GET /api/admin/inquiry-systems (取得詢問系統清單)
     - POST /api/admin/inquiry-systems (新增詢問系統)
     - PUT /api/admin/inquiry-systems/{id} (更新詢問系統)
     - DELETE /api/admin/inquiry-systems/{id} (停用詢問系統)
     - GET /api/admin/handler-mappings (取得處理人員對應表)
     - POST /api/admin/handler-mappings (設定對應關係)

3. **快速開始指南** → `quickstart.md`
   - 開發環境設定步驟
   - 資料庫初始化（dotnet ef migrations add Initial）
   - LINE Channel 設定指南
   - 本地開發啟動指令
   - 常見問題排除

4. **Agent Context 更新**
   - 執行 `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`
   - 新增技術: .NET Core 8, EF Core, LINE Messaging API, Azure SQL
   - 更新專案結構描述

### 輸出

- `data-model.md`: 完整資料模型設計文檔
- `contracts/*.yaml`: OpenAPI 3.0 規範檔案
- `quickstart.md`: 開發者快速上手指南
- 更新的 agent context 檔案

## Phase 1 後憲法重新檢查

**時機**: 設計文檔完成後

重新驗證:
1. 資料模型是否符合單一職責原則？
2. API 契約是否清晰且符合 RESTful 原則？
3. 是否有過度設計的跡象？（YAGNI 原則）
4. 測試策略是否可行？

**預期結果**: 所有項目再次通過 ✅

---

**計劃完成**: Phase 0 和 Phase 1 輸出將在此計劃檔案建立後開始執行。Phase 2（任務分解）將由 `/speckit.tasks` 命令另行建立。
