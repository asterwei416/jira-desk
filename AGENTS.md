# AGENTS.md - 專案上下文與代理指南

## 專案概述

**專案名稱**: 客服來電問題紀錄與分析系統  
**技術棧**: .NET Core 8, Azure SQL Database, EF Core, LINE Messaging API, ASP.NET Core MVC/Web API  
**架構**: Clean Architecture (3-layer: Web/Core/Infrastructure)  
**目標**: 小型客服團隊（20-50 人）內部使用的來電問題追蹤與通知系統

---

## 當前狀態

### Phase 0: 研究階段 ✅ 已完成
- ✅ [research.md](specs/1-customer-call-tracking/research.md) - 8 項技術研究完成
  - .NET Core 8 + EF Core Code First 最佳實踐
  - LINE Messaging API 整合（Push Message + Flex Message）
  - LINE Login OAuth 2.0 整合
  - Azure SQL Database 設定（Basic DTU 方案）
  - IIS 部署策略（In-Process Hosting）
  - Excel 報表生成（EPPlus 7）
  - 編輯鎖定機制（資料庫欄位 + 背景清理）
  - 測試策略（xUnit + Moq + FluentAssertions, 80% 覆蓋率目標）

### Phase 1: 設計與契約 ✅ 已完成
- ✅ [data-model.md](specs/1-customer-call-tracking/data-model.md) - 7 個核心實體設計
  - CallRecord（來電紀錄，含編輯鎖定欄位）
  - InquirySystem（詢問系統）
  - Handler（處理人員）
  - HandlerMapping（處理人員對應）
  - ChangeHistory（變更歷史）
  - NotificationLog（通知記錄）
  - User（使用者，支援 LINE 綁定）
  - 完整 EF Core Configuration 與索引策略

- ✅ [contracts/](specs/1-customer-call-tracking/contracts/) - API 契約定義（OpenAPI 3.0）
  - `call-records-api.yaml`: 來電紀錄 CRUD、搜尋、狀態更新、編輯鎖定、變更歷史、Excel 匯出
  - `auth-api.yaml`: 登入（內建帳號 + LINE Login）、登出、個人資料、密碼變更、LINE 帳號綁定
  - `admin-api.yaml`: 詢問系統管理、處理人員管理、對應關係管理、使用者管理（僅 Admin）

- ✅ [quickstart.md](specs/1-customer-call-tracking/quickstart.md) - 開發環境設定指南
  - 前置需求（.NET 8 SDK, SQL Server/LocalDB, Visual Studio）
  - 資料庫設定（LocalDB / SQL Express / Azure SQL）
  - Migration 初始化步驟
  - LINE Channel 設定步驟
  - 常見問題排除

### Phase 2: 任務分解 ⏳ 待執行
- ⏳ 使用 `/speckit.tasks` 生成 [tasks.md](specs/1-customer-call-tracking/tasks.md)
- ⏳ 依 User Story 組織任務清單
- ⏳ 設定任務優先級和依賴關係

---

## 關鍵決策記錄

### 技術決策
1. **不使用 AutoMapper**: 採用手動 DTO 映射，提升可讀性和型別安全
2. **不使用 Redis**: 小規模系統使用 SQL Server Session Storage 和資料庫編輯鎖定
3. **Rich Domain Model**: Entity 類別包含業務邏輯方法（Factory Method, Guard Clauses）
4. **Fluent API 分離**: EF Core Configuration 獨立於 Entity 類別
5. **樂觀鎖定**: 使用 EF Core RowVersion 處理並發編輯
6. **JWT Authentication**: Token 有效期 24 小時，支援 Refresh Token（選配）

### 業務決策（來自 clarification.md）
1. **雙認證模式**: 系統內建帳號 + LINE Login（首次 LINE 登入需管理者批准綁定）
2. **FAQ 整合**: 手動輸入連結或文字（不串接外部 FAQ 系統）
3. **編輯鎖定**: 30 分鐘自動解鎖，管理者可強制解鎖
4. **通知失敗處理**: 僅記錄到 NotificationLog，不自動重試
5. **報表月份格式**: 自然月份（yyyy/MM），如「2026/01」

---

## 專案結構

```
需求紀錄與問題分析系統/
├── .specify/
│   └── memory/
│       └── constitution.md         # v1.1.0 專案憲法（繁體中文）
├── specs/
│   └── 1-customer-call-tracking/
│       ├── spec.md                 # 功能規格（6 User Stories, 47 Requirements）
│       ├── plan.md                 # 實作計劃（Phase 0-1 完成）
│       ├── research.md             # ✅ 技術研究報告
│       ├── data-model.md           # ✅ 資料模型設計
│       ├── quickstart.md           # ✅ 開發環境指南
│       ├── contracts/              # ✅ OpenAPI 3.0 規格
│       │   ├── call-records-api.yaml
│       │   ├── auth-api.yaml
│       │   └── admin-api.yaml
│       ├── checklists/
│       │   └── requirements.md     # 需求檢核清單
│       └── tasks.md                # ⏳ 待生成
├── CallTrackingSystem/             # ⏳ 待建立（實作階段）
│   ├── src/
│   │   ├── CallTrackingSystem.Web/
│   │   ├── CallTrackingSystem.Core/
│   │   └── CallTrackingSystem.Infrastructure/
│   └── tests/
└── AGENTS.md                       # 本檔案

```

---

## 憲法原則（Constitution v1.1.0）

所有開發活動必須遵循以下原則：

### I. 程式碼品質標準
- ✅ 可讀性優先：變數命名清晰，避免縮寫
- ✅ 單一職責原則：類別和方法專注單一任務
- ✅ DRY 原則：避免重複邏輯
- ✅ 型別安全：使用強型別，避免 `dynamic` 和 `object`
- ✅ 錯誤處理：明確的例外處理和驗證

### II. 測試優先開發
- ✅ TDD 必修：先寫測試再寫實作
- ✅ 最低覆蓋率：80%（Service 層 90%+）
- ✅ 使用 xUnit + Moq + FluentAssertions

### III. 使用者體驗一致性
- ✅ 設計系統一致
- ✅ WCAG 2.1 AA 無障礙標準
- ✅ 錯誤訊息繁體中文

### IV. 效能要求
- ✅ P95 回應時間 < 200ms
- ✅ Time to Interactive < 3.5s
- ✅ 記憶體使用 < 100MB per request

### V. 文件語言標準
- ✅ 所有使用者面向文檔使用繁體中文（zh-TW）
- ✅ 程式碼註解、API 文檔、錯誤訊息皆需繁體中文

---

## User Stories 優先級

### P1（核心 MVP）
1. **US-001**: 來電紀錄 CRUD（建立、編輯、刪除、查詢詳情）
2. **US-002**: 來電紀錄搜尋與篩選（多條件、分頁、排序）

### P2（重要功能）
3. **US-003**: 處理狀態更新與歷史追蹤（狀態流轉、變更歷史）
4. **US-004**: LINE 通知整合（新來電通知處理人員）

### P3（增強功能）
5. **US-005**: Excel 報表匯出（篩選條件匯出、月報表）
6. **US-006**: 編輯鎖定機制（避免並發編輯衝突）

---

## 開發指令速查

### 資料庫 Migrations
```powershell
# 建立 Migration
dotnet ef migrations add MigrationName --project src/CallTrackingSystem.Infrastructure --startup-project src/CallTrackingSystem.Web

# 套用 Migration
dotnet ef database update --project src/CallTrackingSystem.Infrastructure --startup-project src/CallTrackingSystem.Web

# 移除最後一個 Migration
dotnet ef migrations remove --project src/CallTrackingSystem.Infrastructure --startup-project src/CallTrackingSystem.Web
```

### 測試
```powershell
# 執行所有測試
dotnet test

# 執行特定測試專案
dotnet test tests/CallTrackingSystem.UnitTests

# 測試覆蓋率
dotnet test --collect:"XPlat Code Coverage"
```

### 執行專案
```powershell
cd src/CallTrackingSystem.Web
dotnet run

# 熱重載模式
dotnet watch run
```

---

## 外部資源連結

- **LINE Developers Console**: https://developers.line.biz/
- **LINE Messaging API 文檔**: https://developers.line.biz/en/docs/messaging-api/
- **LINE Login 文檔**: https://developers.line.biz/en/docs/line-login/
- **.NET Core 8 文檔**: https://docs.microsoft.com/dotnet/core/
- **EF Core 8 文檔**: https://docs.microsoft.com/ef/core/
- **Azure SQL 文檔**: https://docs.microsoft.com/azure/azure-sql/

---

## 代理工作流程指南

### 當執行 `/speckit.tasks` 時
1. 讀取 `spec.md` 的 User Stories 和 Acceptance Scenarios
2. 參考 `data-model.md` 的實體設計
3. 參考 `contracts/*.yaml` 的 API 端點定義
4. 生成 `tasks.md` 文件，包含：
   - 依 User Story 組織的任務清單
   - 前端任務（MVC Views, Controllers）
   - 後端任務（Services, Repositories, Entities）
   - 資料庫任務（Migrations, Seed Data）
   - 測試任務（Unit Tests, Integration Tests）
   - 任務間依賴關係
   - 工作量估算

### 當開始實作時
1. 參考 `quickstart.md` 設定開發環境
2. 依 `tasks.md` 的順序逐一實作
3. 遵循 Constitution 的 5 項原則
4. 每完成一個任務，執行測試確保覆蓋率達標
5. 提交 Git Commit，訊息格式: `feat(US-00X): 任務描述`

### 當遇到技術問題時
1. 優先參考 `research.md` 的決策記錄
2. 檢查 `data-model.md` 的實體設計是否符合需求
3. 查閱 `contracts/*.yaml` 確認 API 契約
4. 若需調整設計，更新相關文檔並記錄原因

---

## 更新記錄

| 日期 | 版本 | 變更內容 |
|------|------|----------|
| 2026-01-28 | 1.0.0 | 初版建立，Phase 0-1 完成 |

---

## 下一步行動

⏳ 執行 `/speckit.tasks` 生成詳細任務清單

---

We're going to be using slash command from `.github\prompts\`