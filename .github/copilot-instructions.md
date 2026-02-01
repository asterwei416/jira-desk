# Copilot 開發指引（CallTrackingSystem）

> 專案：客服來電問題紀錄與分析系統
> 技術棧：.NET 8 / ASP.NET Core MVC + Web API / EF Core / Azure SQL / LINE Messaging API
> 架構：Clean Architecture（三層：Web / Core / Infrastructure）
> 語言：繁體中文（錯誤訊息、文件、註解）

---

## 🎯 目標與原則

- **可讀性優先**：命名清楚，避免縮寫
- **單一職責**：服務/方法聚焦單一任務
- **型別安全**：避免 `dynamic` / `object`
- **錯誤處理**：明確例外與訊息（繁中）
- **測試優先**：xUnit + Moq + FluentAssertions
- **WCAG AA**：UI 一致、可讀

---

## 📁 專案結構與責任

```
CallTrackingSystem/
  src/
    CallTrackingSystem.Web/           # MVC + API + DI
    CallTrackingSystem.Core/          # Entities / DTOs / Interfaces / Services
    CallTrackingSystem.Infrastructure/# EF Core / Repositories / 外部服務
  tests/
    CallTrackingSystem.UnitTests/
    CallTrackingSystem.IntegrationTests/
```

### 層級規則
- **Web**：Controllers / Views / API
- **Core**：Domain Entities + Service + DTO + Interface
- **Infrastructure**：Repository / EF Core / 外部 API

---

## ✅ 既定設計決策

- **不使用 AutoMapper**：手動 DTO 映射
- **不使用 Redis**：SQL Session + DB Lock
- **Rich Domain Model**：Entity 含業務方法
- **RowVersion**：EF Core 樂觀鎖定
- **JWT 認證**：Token 有效期 24h
- **LINE 通知**：失敗只記錄，不重試

---

## 🧠 開發慣例（重要）

### 1. API/服務命名
- Service：`XxxService`
- Interface：`IXxxService`
- DTO：`XxxRequest` / `XxxResponse`
- Controller：`XxxController`

### 2. 錯誤訊息
- **一律繁體中文**
- 使用 `InvalidOperationException` 為主要業務例外

### 3. 日期格式
- UI：`yyyy/MM/dd HH:mm`
- 報表月份：`yyyy/MM`

### 4. 報表限制
- Excel 匯出最大 5000 筆
- 三工作表：篩選摘要 / 明細 / 統計

---

## 🧪 測試策略

- 單元測試：Service 層為主
- 整合測試：Repository + Service 流程
- 目標覆蓋率：80%（Service 90%+）

測試工具：
- **xUnit**
- **Moq**
- **FluentAssertions**

---

## 🧱 常用模組位置

| 模組 | 位置 |
|------|------|
| Entities | src/CallTrackingSystem.Core/Entities |
| DTOs | src/CallTrackingSystem.Core/DTOs |
| Services | src/CallTrackingSystem.Core/Services |
| Repositories | src/CallTrackingSystem.Infrastructure/Repositories |
| Web API | src/CallTrackingSystem.Web/Controllers |
| Views | src/CallTrackingSystem.Web/Views |
| Tests | tests/CallTrackingSystem.* |

---

## 🧩 Edit Lock 機制規範

- 取得鎖定：`POST /api/call-records/{id}/lock`
- 釋放鎖定：`DELETE /api/call-records/{id}/lock`
- 強制解鎖：`POST /api/admin/call-records/{id}/force-unlock`
- 鎖定過期：30 分鐘
- 背景清理：每 5 分鐘

---

## 🧷 Commit 規範

格式：`feat(US-00X): 任務描述`
或：`test(T0xx): 任務描述`

---

## 🤖 Copilot 行為建議

- 修改前先查找現有同功能的 Service / Controller
- 保留現有風格與錯誤訊息
- 新增功能需同步更新測試
- 每個 Task 完成後需提交
- 不建立額外說明文件（除非需求）

---

## 📌 參考文件

- [specs/1-customer-call-tracking/spec.md](../specs/1-customer-call-tracking/spec.md)
- [specs/1-customer-call-tracking/tasks.md](../specs/1-customer-call-tracking/tasks.md)
- [specs/1-customer-call-tracking/data-model.md](../specs/1-customer-call-tracking/data-model.md)
- [specs/1-customer-call-tracking/contracts](../specs/1-customer-call-tracking/contracts)
