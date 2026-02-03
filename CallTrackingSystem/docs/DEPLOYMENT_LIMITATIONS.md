# LINE Bot 整合功能 - 部署限制說明

## 單實例部署限制

### 對話狀態管理

**設計決策**: LINE Bot 對話狀態使用 In-Memory 儲存（`ConcurrentDictionary`），不依賴外部快取系統（如 Redis）。

### 限制影響

#### 1. 應用程式重啟
- **影響**: 所有進行中的 LINE 對話將立即中斷
- **使用者體驗**: 使用者需重新輸入「回報問題」啟動新流程
- **資料完整性**: 已提交的回報單不受影響，僅對話過程中的暫存資料遺失

#### 2. 多實例部署（負載平衡）
- **不支援**: 無法部署多個應用程式實例
- **原因**: 對話狀態無法跨實例共享
- **建議**: 使用單一伺服器垂直擴充（增加 CPU/RAM），而非水平擴充（多實例）

### 適用場景

此設計適合以下情境：
- ✅ 小型團隊（20-50 人）使用
- ✅ 重啟頻率低（每週或每月）
- ✅ 對話逾時短（5 分鐘），使用者習慣快速完成回報
- ✅ 不使用 Redis 的技術限制環境

### 不適用場景

若有以下需求，需要重新設計儲存方案：
- ❌ 大量並發對話（>100 個同時進行）
- ❌ 頻繁重啟需求（CI/CD 每小時部署）
- ❌ 必須負載平衡（高可用性要求）
- ❌ 對話流程複雜（需保留超過 1 小時）

### 部署檢核清單

部署前確認：
- [ ] 確認為單實例部署（僅一個應用程式 process）
- [ ] 通知使用者重啟維護窗口（建議非工作時段）
- [ ] 測試重啟後 LINE Bot 功能恢復正常
- [ ] 監控 `ConversationCleanupService` 背景服務運作狀態

### 緩解措施

1. **重啟通知**:
   ```csharp
   // 在 Program.cs 新增重啟時清空對話的日誌
   app.Lifetime.ApplicationStarted.Register(() => 
   {
       logger.LogWarning("應用程式已啟動，所有 LINE 對話狀態已清空");
   });
   ```

2. **使用者提示**: LINE Bot 啟動訊息可加入提示：
   ```
   「提示：請在 5 分鐘內完成回報，系統維護時對話將中斷。」
   ```

3. **監控對話遺失率**:
   - 記錄每日啟動對話數與完成率
   - 若完成率 <70%，考慮增加逾時時間或優化流程

### 升級路徑

若未來需要多實例支援：
1. **選項 A**: 引入 Redis 儲存對話狀態
   - 修改 `ConversationStateService` 使用 `IDistributedCache`
   - 更新 `ConversationStateDto` 序列化邏輯

2. **選項 B**: 使用 SQL Database 儲存對話
   - 新增 `ConversationState` Entity
   - 修改 Service 改為資料庫讀寫
   - 增加索引優化查詢效能

3. **選項 C**: 改用 Azure SignalR Service 儲存對話
   - 適合雲端部署環境
   - 自動支援多實例

### 相關文件

- [LINE Bot 使用者手冊](LINE_BOT_USER_GUIDE.md) - 對話中斷處理說明
- [quickstart.md](../specs/2-line-bot-integration/quickstart.md) - 開發環境設定
- [憲法限制](../.specify/memory/constitution.md) - 不使用 Redis 的原則說明
