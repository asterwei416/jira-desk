# Copilot Instructions (MANDATORY)

You are contributing to **CallTrackingSystem** (客服來電問題紀錄與分析系統), a brownfield project following Specification-Driven Development (SDD) using Spec Kit.

These rules are **NON-NEGOTIABLE**.

---

## 1. Specification Rules (STRICT)

- ALL specifications MUST be feature-level specs under `/specs/<feature-id>/`
- Each feature MUST contain: `spec.md`, `plan.md`, `tasks.md`
- System-wide or monolithic specs are NOT allowed
- `spec.md` MUST include sections: Goal, User Capability, Scope, Constraints, Relationship to Existing Features

### Feature Relationship Declaration (REQUIRED)

Every `spec.md` MUST declare:
- **Builds upon**: (existing features this extends)
- **Depends on**: (features required to function)
- **Does not modify**: (features explicitly untouched)

If none apply, write "None" explicitly.

---

## 2. Authority Hierarchy

1. `.specify/memory/constitution.md` (highest authority)
2. Feature-level specs (`/specs/*/`)
3. This document (project conventions)

**If conflict detected**: Report clearly, do NOT guess.

---

## 3. Architecture (Clean Architecture)

### Layer Responsibilities
- **Web** (`CallTrackingSystem.Web`): MVC Controllers + API Endpoints + DI configuration ([Program.cs](CallTrackingSystem/src/CallTrackingSystem.Web/Program.cs))
- **Core** (`CallTrackingSystem.Core`): Domain Entities + Business Services + DTOs + Interfaces (例: [CallRecordService.cs](CallTrackingSystem/src/CallTrackingSystem.Core/Services/CallRecordService.cs))
- **Infrastructure** (`CallTrackingSystem.Infrastructure`): EF Core + Repositories + External integrations (LINE, Email)

### Critical Data Flows
1. **新增來電記錄**: `CallRecordService` 建立實體 → 自動指派處理人員 → 寫入變更歷史 → 發送 LINE 通知（失敗不阻斷）
2. **Excel 報表**: `ReportService` 使用 ClosedXML 生成三工作表（篩選摘要/明細/統計），見 [ReportService.cs](CallTrackingSystem/src/CallTrackingSystem.Core/Services/ReportService.cs)
3. **編輯鎖定**: API 端點 `/api/call-records/{id}/lock`，背景清理服務每 5 分鐘執行
4. **LINE Bot 對話**: 8 步驟引導式回報 → `LineBotMessageHandler` 處理狀態機 → In-Memory 對話狀態（5 分鐘逾時）→ 建立 CallRecord

### LINE Integration Architecture (Feature: 2-line-bot-integration)
- **LINE Messaging API**: 使用 HttpClient（`LineMessagingApiClient`），不依賴第三方 SDK
- **Webhook 安全**: `LineSignatureValidatorMiddleware` 驗證 HMAC-SHA256 簽章
- **對話狀態**: `ConversationStateService` 使用 `ConcurrentDictionary` In-Memory 儲存（**單實例限制**）
- **背景清理**: `ConversationCleanupService` 每 5 分鐘清理過期對話
- **LINE 綁定**: OAuth 2.0 透過 `LineLoginService`，State 參數存於 Cookie（5 分鐘有效）
- **通知篩選**: 僅發送給 `Handler.LineUserId` 非空的處理人員

---

## 4. Development Workflow (MANDATORY)

### Testing Workflow
1. **啟動服務**: 見 [TESTING_SOP.md](CallTrackingSystem/docs/TESTING_SOP.md)
   - 一鍵 Smoke Test: `./scripts/run-smoke-tests.ps1 -BaseUrl "http://localhost:5000"`
2. **LINE 通知測試**: 見 [LINE_NOTIFICATION_TEST.md](CallTrackingSystem/docs/LINE_NOTIFICATION_TEST.md)

### Migration Commands
```powershell
# 建立 Migration
dotnet ef migrations add <Name> --project src/CallTrackingSystem.Infrastructure --startup-project src/CallTrackingSystem.Web

# 套用 Migration
dotnet ef database update --project src/CallTrackingSystem.Infrastructure --startup-project src/CallTrackingSystem.Web
```

---

## 5. Project Conventions (STRICT)

### Naming Standards
- Service: `XxxService` + `IXxxService`
- DTO: `XxxRequest` / `XxxResponse`
- Controller: `XxxController`

### Code Rules
- **NO AutoMapper**: 手動 DTO 映射
- **錯誤訊息**: 一律繁體中文，業務例外用 `InvalidOperationException`
- **日期格式**: UI 為 `yyyy/MM/dd HH:mm`，報表月份為 `yyyy/MM`
- **時區**: 統一轉換為 Asia/Taipei

### Data Constraints
- Excel 匯出上限: 5000 筆
- 編輯鎖定逾時: 30 分鐘
- LINE Bot InquirySystem 選項: 最多 13 個（Quick Reply 限制）
- 對話逾時: 5 分鐘無回應自動清除

### LINE Bot Limitations
- **單實例部署**: 對話狀態為 In-Memory，應用程式重啟時所有對話將遺失
- **不支援多實例**: 不使用 Redis 或分散式快取（憲法限制）
- **電話驗證**: 僅支援台灣手機（09xx-xxx-xxx）與市話（0x-xxxx-xxxx）格式
- **通知重試**: 失敗僅記錄至 `NotificationLog`，不自動重試

---

## 6. Environment Setup

### Development Database
- **類型**: SQLite
- **檔案**: `CallTrackingDB_Dev.db` (Web 專案目錄下)
- **種子資料**: 自動執行 [DbInitializer.cs](CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Data/DbInitializer.cs)
  - 預設帳號: `admin/Admin@123`, `staff/Staff@123`

### Authentication
- **策略**: Cookie + JWT 雙通道
- **切換邏輯**: 依 `Authorization: Bearer` Header 自動選擇
- **實作位置**: [Program.cs](CallTrackingSystem/src/CallTrackingSystem.Web/Program.cs) 認證設定區段

---

## 7. Forbidden Behaviors

- Do NOT modify existing features without explicit spec approval
- Do NOT introduce cross-feature dependencies without declaring in spec
- Do NOT create system-wide architectural changes
- Do NOT bypass DTO mapping with reflection or dynamic typing
- Do NOT use English for user-facing error messages

---

## 8. Change Implementation Strategy

**Before modifying code:**
1. 檢查是否有對應的 Service/Repository（例: [CallRecordService.cs](CallTrackingSystem/src/CallTrackingSystem.Core/Services/CallRecordService.cs)）
2. 遵循現有分層結構（Service → Repository → Entity）
3. 參考相同層級的既有實作範例
4. 同步更新單元測試 (`tests/CallTrackingSystem.UnitTests`)

**Controller 範例**: [CallTrackingSystem.Web/Controllers](CallTrackingSystem/src/CallTrackingSystem.Web/Controllers)
