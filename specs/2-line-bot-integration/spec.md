# 功能規範：LINE Bot 整合功能

**功能分支**: `2-line-bot-integration`  
**建立日期**: 2026-02-03  
**狀態**: Draft  
**使用者輸入**: "針對已完成的顧客問題紀錄追蹤網站進行功能追加，新增 LINE 官方帳號整合功能，提升系統即時性與使用便利性。"

---

## Clarifications

### Session 2026-02-03

- Q: 當應用程式重啟或部署多實例（負載平衡）時，正在進行中的 LINE 對話應如何處理？ → A: 接受遺失風險 - 維持 In-Memory Dictionary，重啟時清空所有對話狀態，使用者需重新啟動回報流程。理由：符合 Constitution「不使用 Redis」原則，對話逾時僅 5 分鐘且小型團隊重啟頻率低，影響範圍可接受。此功能明確限制為單實例部署環境。
- Q: 處理人員（Handler）的 LINE 綁定資訊應該儲存在哪裡？ → A: 透過 User 關聯查詢 - Handler 與 User 建立關聯（Handler.UserId），通知時透過 User.LineUserId 查詢。理由：符合現有架構（User 已有 LineUserId 欄位）、避免重複儲存、Handler 本質上應是可登入的系統使用者、遵循最小化變更原則。
- Q: LINE Login 綁定完成後，使用者應該被導向哪個頁面？ → A: 返回個人設定頁面 - Callback 處理完成後重定向至個人設定頁（例：/user/settings），並使用 TempData 顯示「綁定成功」或錯誤訊息。理由：符合使用者直覺操作流程、實作簡單、錯誤處理清晰。Callback URL 範例：/auth/line-callback。
- Q: 當問題所屬單位（InquirySystem）超過 13 個選項時，LINE Bot 應如何呈現選單？ → A: 拒絕回報並引導至網頁 - 顯示「選項過多，請至網頁端回報」並提供網頁連結。理由：符合最小化變更原則、小型團隊通常不會超過 13 個單位、實作成本最低、避免複雜的分頁或卡片邏輯。
- Q: 推送通知失敗時，是否需要實作重試機制或使用者通知？ → A: 維持現有記錄邏輯 - 僅記錄到 NotificationLog 表（status = Failed），管理者可透過後台查詢通知歷史。理由：符合 Constitution「不過度設計」原則、現有 LineNotificationService 已有失敗記錄機制、小型團隊可手動處理異常、避免增加系統複雜度。

---

## Goal

為現有的客服來電問題紀錄系統新增 LINE Bot 雙向互動能力，使已綁定 LINE 的使用者可以：
1. 在網頁端完成 LINE 官方帳號綁定與解綁
2. 直接在 LINE 對話介面中填寫並提交問題回報單
3. 透過 LINE 接收問題回報通知（此功能部分已實作，需擴充綁定機制）

此功能擴充現有系統的使用場景，讓使用者無需開啟網頁即可完成回報與接收通知。

---

## User Capability

完成此功能後，使用者將能夠：

- **使用者帳號管理員**: 在個人設定頁面綁定/解綁 LINE 官方帳號，查看綁定狀態
- **處理人員（已綁定 LINE）**: 透過 LINE 接收新問題回報的即時通知，點擊連結直接查看詳情
- **回報人員（已綁定 LINE）**: 在 LINE 對話視窗中透過引導式對話完成問題回報，無需切換到網頁介面
- **系統管理員**: 查看 LINE 綁定使用者清單，管理綁定狀態（選配功能）

---

## Scope

### Included（本功能包含）

#### 第一項功能 - LINE 官方帳號綁定機制
1. 網頁端個人設定頁面新增「綁定 LINE 官方帳號」按鈕
2. 點擊後開啟 LINE Login OAuth 2.0 流程，完成授權後取得 LINE User ID
3. 系統在 `User` Entity 中儲存 LINE User ID，建立帳號對應關係
4. OAuth 2.0 Callback 端點（例：`/auth/line-callback`）處理完成後重定向至個人設定頁面，並顯示「綁定成功」或錯誤訊息（使用 TempData）
5. 介面顯示目前綁定狀態（已綁定：顯示 LINE Display Name；未綁定：顯示綁定按鈕）
6. 提供「解除綁定」功能，清除對應關係
7. 訪客角色（Role = Guest）禁止綁定 LINE

#### 第二項功能 - 擴充 LINE 推送通知機制（基於現有實作）
1. **注意**：系統已實作 `LineNotificationService.SendCallRecordNotificationAsync()` 與 Flex Message 格式
2. 本功能僅需擴充：確保只推送給「已綁定 LINE」的處理人員（透過 `Handler.UserId` 關聯查詢 `User.LineUserId` 非空）
3. 推送訊息格式保持現有設計（包含回報單編號、標題、緊急程度、所屬單位、聯絡人、電話、日期、回報人）
4. 訊息包含快速連結按鈕，導向網頁端詳情頁（`{DetailUrlBase}/{callRecordId}`）
5. 發送失敗記錄至 `NotificationLog`，不阻斷主流程

#### 第三項功能 - LINE Bot 對話式回報問題
1. 使用者在 LINE 輸入「回報問題」關鍵字或點擊 Rich Menu 按鈕，啟動對話流程
2. Bot 依序引導使用者填寫：
   - 問題標題（Subject）
   - 問題內容（Content）
   - 問題所屬單位（InquirySystem）：以 Quick Reply 按鈕顯示可選項目
   - 緊急程度（UrgencyLevel）：低/中/高，以 Quick Reply 按鈕顯示
   - 聯絡人姓名（ContactName）
   - 聯絡電話（ContactPhone）
3. 系統自動填入：
   - 紀錄日期（CreatedAt）：當下時間
   - 回報人（CreatedByUserId）：從 LINE User ID 反查綁定的系統帳號
   - 處理狀態（Status）：預設為 `Pending`
   - 指派處理人員（Handlers）：根據 `HandlerMapping` 自動指派
4. 填寫完成後顯示摘要，提供「確認送出」、「重新填寫」、「取消」按鈕
5. 確認後呼叫 `CallRecordService.CreateAsync()`，功能與網頁端新增紀錄完全相同
6. 建立成功後回覆回報單編號與查看連結
7. 使用者可隨時輸入「取消」中斷流程，清除暫存資料

#### 對話狀態管理
- 使用 In-Memory 狀態儲存（`Dictionary<string, ConversationState>`），key 為 LINE User ID
- 狀態包含：當前步驟（CurrentStep）、已填寫資料（FormData）、逾時設定（5 分鐘無回應自動清除）
- 狀態清除時機：完成送出、取消、逾時
- **重要限制**：應用程式重啟時所有對話狀態將遺失，使用者需重新啟動回報流程（此為刻意的設計取捨，符合單實例部署環境）

#### 技術限制與設計決策
- 使用 LINE Messaging API Webhook 接收訊息事件
- Quick Reply 按鈕最多 13 個選項，若 InquirySystem 超過此數量則顯示「選項過多，請至網頁端回報」並提供網頁連結（不實作分頁或 Flex Message 複雜選單）
- 電話號碼驗證：支援台灣手機格式（09xx-xxx-xxx）與市話格式（0x-xxxx-xxxx）
- 錯誤處理：輸入格式錯誤時提示使用者重新輸入，不中斷流程

### Out of scope（本功能不包含）

- ❌ LINE Bot 查詢歷史回報單功能（需另外規劃，避免功能範圍膨脹）
- ❌ LINE Bot 更新回報單狀態功能（需權限控管設計，暫不納入）
- ❌ LINE Bot 推送回報單狀態變更通知（僅通知新建立的回報單）
- ❌ 多國語言支援（本期僅支援繁體中文）
- ❌ LINE LIFF（LINE Front-end Framework）整合（使用原生 Messaging API）
- ❌ 圖片/檔案上傳功能（僅支援文字輸入）
- ❌ 批次綁定或匯入功能（僅支援個別使用者自行綁定）

---

## Constraints

### 技術限制
- **LINE Messaging API 配額**: 
  - 免費方案每月 500 則推送訊息（需評估團隊規模是否足夠）
  - 回覆訊息（Reply Message）無限制，但需在 Webhook 事件 30 秒內回應
- **LINE Login 憑證有效期**: Access Token 有效期 30 天，需實作 Refresh Token 機制（選配）
- **Webhook 接收端點**: 需提供 HTTPS 端點供 LINE Platform 呼叫，開發環境可使用 ngrok
- **部署限制**: 本功能僅支援單實例部署環境，不支援負載平衡或多實例部署（因對話狀態使用 In-Memory 儲存）

### 資料限制
- LINE User ID 長度：固定 33 字元（User.LineUserId 欄位需調整為 `varchar(50)`）
- LINE Display Name 長度：最多 20 字元
- Quick Reply 選項數量：最多 13 個
- Flex Message 大小限制：10 KB

### 業務規則
- 訪客帳號（UserRole.Guest）禁止綁定 LINE
- 一個系統帳號僅能綁定一個 LINE 帳號（1:1 對應）
- 一個 LINE 帳號僅能綁定一個系統帳號（防止重複綁定）
- 未綁定的 LINE 使用者嘗試回報問題時，引導其先完成綁定
- LINE Bot 回報的問題與網頁端回報的問題享有相同的處理流程與權限

### 安全性
- LINE Login OAuth 2.0 State 參數需使用隨機字串防止 CSRF 攻擊
- Webhook 簽章驗證：驗證 `X-Line-Signature` Header，確保請求來自 LINE Platform
- 對話狀態資料不持久化，避免敏感資料外洩

### 錯誤處理原則
- LINE API 呼叫失敗時記錄錯誤但不中斷業務流程（例：推送通知失敗時仍完成來電紀錄建立）
- 推送通知失敗僅記錄到 NotificationLog 表（status = Failed），不實作自動重試或主動警示管理員
- 使用者輸入格式錯誤時提供清晰的錯誤訊息與範例
- OAuth 綁定失敗時記錄詳細錯誤（包含 LINE 回傳的 error code）並在前端顯示使用者友善訊息

---

## Relationship to Existing Features

### Builds upon（擴充自現有功能）
- **1-customer-call-tracking / US-001 來電紀錄 CRUD**: LINE Bot 回報功能使用相同的 `CallRecordService.CreateAsync()` 方法建立紀錄
- **1-customer-call-tracking / US-004 LINE 通知整合**: 擴充現有的 `LineNotificationService`，確保只推送給已綁定的處理人員

### Depends on（依賴於現有功能）
- **User Entity & AuthService**: 需要 `User.LineUserId` 欄位儲存綁定資訊（需新增 Migration）
- **Handler Entity & HandlerMapping**: 依賴現有的處理人員指派邏輯；需建立 Handler 與 User 的關聯（`Handler.UserId`）以查詢 LINE 綁定狀態
- **InquirySystem Repository**: 查詢可選的問題所屬單位清單
- **CallRecordService**: 完全依賴現有的來電紀錄建立邏輯，不重複實作

### Does not modify（明確不修改的現有功能）
- **來電紀錄 CRUD 核心邏輯**: 不修改 `CallRecordService` 的現有方法簽章與業務規則
- **編輯鎖定機制**: LINE Bot 建立的紀錄使用相同的鎖定規則，不引入新的鎖定邏輯
- **Excel 報表功能**: LINE Bot 建立的紀錄自動納入報表，無需修改 `ReportService`
- **變更歷史追蹤**: 使用現有的 `ChangeHistory` 機制，不修改其記錄方式
- **認證策略（Cookie + JWT）**: LINE Login 僅用於綁定，不取代現有的登入機制

---

## 使用者情境與測試

### 使用者故事 1 - 網頁端綁定 LINE 官方帳號（優先級: P1）

使用者可以在網頁端個人設定頁面綁定 LINE 官方帳號，完成後系統記錄對應關係，並可隨時解除綁定。

**為何此優先級**: 綁定機制是後續 LINE Bot 互動的基礎，沒有綁定就無法識別使用者身份。

**獨立測試**: 使用者可以完成綁定流程並在介面上看到綁定狀態，即可獨立驗證此功能。

**驗收情境**:

1. **Given** 使用者已登入系統（非訪客角色），**When** 進入個人設定頁面，**Then** 看到「綁定 LINE 官方帳號」按鈕
2. **Given** 使用者點擊「綁定 LINE 官方帳號」按鈕，**When** 完成 LINE Login 授權流程，**Then** 系統記錄 LINE User ID 並顯示「已綁定」狀態與 LINE Display Name
3. **Given** 使用者已綁定 LINE，**When** 再次進入個人設定頁面，**Then** 顯示「已綁定：{Display Name}」與「解除綁定」按鈕
4. **Given** 使用者點擊「解除綁定」按鈕，**When** 確認操作，**Then** 系統清除 LINE User ID，恢復為「未綁定」狀態
5. **Given** 訪客角色使用者進入個人設定頁面，**When** 檢視介面，**Then** 不顯示 LINE 綁定功能區塊
6. **Given** 一個 LINE 帳號已綁定至帳號 A，**When** 帳號 B 嘗試綁定相同的 LINE 帳號，**Then** 顯示錯誤訊息「此 LINE 帳號已被其他使用者綁定」

---

### 使用者故事 2 - LINE Bot 對話式回報問題（優先級: P1）

已綁定 LINE 的使用者可以在 LINE 對話視窗中透過引導式對話完成問題回報，系統自動建立回報單並通知處理人員。

**為何此優先級**: 這是 LINE Bot 的核心價值功能，讓使用者無需開啟網頁即可完成回報。

**獨立測試**: 使用者可以在 LINE 中完成一次完整的回報流程，並在網頁端查詢到該筆紀錄。

**驗收情境**:

1. **Given** 已綁定的使用者在 LINE 中輸入「回報問題」，**When** Bot 收到訊息，**Then** 回覆「您好！請問要回報什麼問題呢？請輸入問題標題。」
2. **Given** 使用者輸入問題標題「客戶反應系統異常」，**When** Bot 收到回覆，**Then** 回覆「收到，問題標題為『客戶反應系統異常』。請描述問題的詳細內容：」
3. **Given** 使用者輸入問題內容，**When** Bot 收到回覆，**Then** 以 Quick Reply 按鈕顯示所有可選的問題所屬單位（InquirySystem）
4. **Given** 使用者選擇所屬單位「資訊部」，**When** Bot 收到選擇，**Then** 以 Quick Reply 按鈕顯示緊急程度選項（🟢 低、🟡 中、🔴 高）
5. **Given** 使用者選擇緊急程度「🔴 高」，**When** Bot 收到選擇，**Then** 回覆「請提供顧客聯絡人姓名：」
6. **Given** 使用者輸入聯絡人「王小明」與電話「0912-345-678」，**When** Bot 收到最後一項資料，**Then** 顯示摘要並提供「確認送出」、「重新填寫」、「取消」按鈕
7. **Given** 使用者點擊「確認送出」，**When** Bot 呼叫 `CallRecordService.CreateAsync()`，**Then** 回報單建立成功，回覆「✅ 回報單已成功建立！回報單號：#20260203-001」與查看連結
8. **Given** 使用者在填寫過程中輸入「取消」，**When** Bot 收到取消指令，**Then** 清除暫存資料並回覆「已取消回報流程」
9. **Given** 未綁定的 LINE 使用者輸入「回報問題」，**When** Bot 收到訊息，**Then** 回覆「請先在網頁端完成 LINE 帳號綁定才能使用此功能」並提供網頁連結

---

### 使用者故事 3 - 接收 LINE 推送通知（優先級: P2）

當系統建立新的回報單時，已綁定 LINE 的處理人員會收到即時通知，可點擊連結直接查看詳情。

**為何此優先級**: 此功能已部分實作，僅需確保綁定機制整合正確。

**獨立測試**: 可透過網頁端或 LINE Bot 建立一筆新紀錄，驗證處理人員是否收到通知。

**驗收情境**:

1. **Given** 處理人員已綁定 LINE，**When** 系統建立新回報單並指派給該處理人員，**Then** 該處理人員的 LINE 收到 Flex Message 格式的通知
2. **Given** 通知訊息顯示在 LINE，**When** 使用者檢視內容，**Then** 包含回報單編號、問題標題、緊急程度、所屬單位、聯絡人、電話、日期、回報人
3. **Given** 通知訊息包含「查看回報單詳情」按鈕，**When** 使用者點擊按鈕，**Then** 開啟瀏覽器並導向網頁端詳情頁
4. **Given** 處理人員未綁定 LINE，**When** 系統建立新回報單並指派給該處理人員，**Then** 不發送 LINE 通知（記錄至 NotificationLog 並標註「處理人員未綁定 LINE」）
5. **Given** LINE 推送失敗（例如使用者封鎖 Bot），**When** 系統嘗試發送通知，**Then** 記錄失敗原因至 `NotificationLog`，但不影響回報單建立流程

---

### 邊界情況

- **當使用者在對話流程中 5 分鐘無回應時**，系統自動清除暫存資料，使用者需重新啟動流程。
- **當 InquirySystem 超過 13 個選項時**，Bot 回覆「選項過多，請至網頁端回報問題」並提供網頁連結，不繼續對話流程。
- **當電話號碼格式錯誤時**，Bot 提示「電話格式不正確，請輸入正確的手機號碼（例：0912-345-678）或市話（例：02-1234-5678）」，並等待使用者重新輸入。
- **當未綁定的 LINE 使用者嘗試回報問題時**，Bot 回覆引導訊息並提供網頁端綁定連結。
- **當 LINE Webhook 簽章驗證失敗時**，系統拒絕請求並記錄 Log，避免偽造請求。
- **當同一個 LINE 帳號嘗試綁定多個系統帳號時**，系統拒絕並顯示錯誤訊息。
- **當系統帳號已綁定 LINE 帳號 A 時**，若嘗試綁定 LINE 帳號 B，系統自動解除舊綁定並建立新綁定（顯示確認對話框）。

---

## 需求

### 功能需求

#### LINE 官方帳號綁定（FR-LINE-001 ~ FR-LINE-006）

- **FR-LINE-001**: 系統必須在個人設定頁面提供「綁定 LINE 官方帳號」按鈕（訪客角色不顯示）
- **FR-LINE-002**: 系統必須支援 LINE Login OAuth 2.0 流程，取得 LINE User ID 與 Display Name
- **FR-LINE-003**: 系統必須在 `User` Entity 的 `LineUserId` 欄位記錄綁定資訊（需新增 Migration）
- **FR-LINE-004**: 系統必須檢查 LINE 帳號是否已被其他使用者綁定，避免重複綁定
- **FR-LINE-005**: 系統必須提供「解除綁定」功能，清除 `User.LineUserId` 欄位
- **FR-LINE-006**: 系統必須在介面顯示綁定狀態（已綁定：顯示 Display Name；未綁定：顯示綁定按鈕）

#### LINE Bot 對話式回報（FR-LINE-007 ~ FR-LINE-015）

- **FR-LINE-007**: 系統必須提供 Webhook 端點接收 LINE Messaging API 事件
- **FR-LINE-008**: 系統必須驗證 Webhook 請求的 `X-Line-Signature` Header，確保來自 LINE Platform
- **FR-LINE-009**: 系統必須支援「回報問題」關鍵字觸發對話流程（不區分大小寫）
- **FR-LINE-010**: 系統必須依序引導使用者填寫：問題標題、問題內容、所屬單位、緊急程度、聯絡人、電話
- **FR-LINE-011**: 系統必須以 Quick Reply 按鈕顯示 InquirySystem 與 UrgencyLevel 選項
- **FR-LINE-012**: 系統必須驗證使用者輸入（電話格式驗證），格式錯誤時提示重新輸入
  - 手機格式: `^09\d{2}-?\d{3}-?\d{3}$` (例: 0912-345-678 或 0912345678)
  - 市話格式: `^0\d{1,2}-?\d{3,4}-?\d{4}$` (例: 02-1234-5678 或 021234567)
  - 驗證失敗時回覆：「電話格式不正確，請輸入正確的手機號碼（例：0912-345-678）或市話（例：02-1234-5678）」
- **FR-LINE-013**: 系統必須在填寫完成後顯示摘要，提供「確認送出」、「重新填寫」、「取消」按鈕
  - **「重新填寫」行為**: 清除所有暫存資料，對話狀態重置為 AwaitingSubject，使用者需重新填寫所有欄位（標題、內容、單位、緊急程度、聯絡人、電話）
- **FR-LINE-014**: 系統必須支援「取消」指令中斷流程，清除暫存資料
- **FR-LINE-015**: 系統必須在確認送出後呼叫 `CallRecordService.CreateAsync()`，功能與網頁端完全相同

#### LINE 推送通知擴充（FR-LINE-016 ~ FR-LINE-018）

- **FR-LINE-016**: 系統必須僅推送 LINE 通知給「已綁定 LINE」的處理人員（透過 `Handler.UserId` 關聯查詢 `User.LineUserId` 非空）
- **FR-LINE-017**: 系統必須使用現有的 Flex Message 格式發送通知（包含回報單編號、標題、緊急程度等）
- **FR-LINE-018**: 系統必須在 LINE 通知中包含「查看回報單詳情」按鈕，導向網頁端詳情頁

### 非功能需求

#### 效能（NFR-LINE-001 ~ NFR-LINE-003）

- **NFR-LINE-001**: Webhook 端點必須在 30 秒內回應 LINE Platform（Reply Message 限制）
- **NFR-LINE-002**: 對話狀態查詢必須在 50ms 內完成（In-Memory Dictionary）
- **NFR-LINE-003**: 綁定流程（OAuth 2.0 Callback）必須在 3 秒內完成

#### 安全性（NFR-LINE-004 ~ NFR-LINE-006）

- **NFR-LINE-004**: 必須驗證 LINE Webhook 簽章，拒絕未經驗證的請求
- **NFR-LINE-005**: LINE Login OAuth 2.0 State 參數必須使用隨機字串（GUID）防止 CSRF
- **NFR-LINE-006**: 對話狀態資料不持久化至資料庫，避免敏感資料外洩

#### 可用性（NFR-LINE-007 ~ NFR-LINE-009）

- **NFR-LINE-007**: Bot 回覆訊息必須使用繁體中文，語氣友善且易於理解
- **NFR-LINE-008**: 錯誤訊息必須明確指出問題與修正方式（例：電話格式錯誤時提供範例）
- **NFR-LINE-009**: 對話流程逾時（5 分鐘）後必須清除狀態並提示使用者重新啟動

#### 可維護性（NFR-LINE-010 ~ NFR-LINE-011）

- **NFR-LINE-010**: 可觀測性 - 所有 LINE API 互動（推送通知、接收訊息、OAuth 流程）必須記錄到 NotificationLog，包含成功/失敗狀態、錯誤訊息、執行時間。失敗記錄不觸發自動重試或警示,由管理者手動查詢處理。
- **NFR-LINE-011**: 資料隱私 - LINE UserId 與電話號碼需加密儲存（選配），綁定關係僅限本人與管理員查看。
  - **實作狀態**: 本期（Phase 1-6）不實作加密儲存，資料庫使用 varchar 明文儲存 LineUserId。綁定關係查看權限透過現有 ASP.NET Core Authorization（Role-based）控制。未來若需加密，建議使用 Azure Key Vault + SQL Server Always Encrypted 或 EF Core ColumnEncryption。此項目標記為 Phase 7 未來增強功能。

---

## 驗收標準

### 綁定功能驗收
- ✅ 使用者可以在網頁端完成 LINE Login 授權並綁定帳號
- ✅ 系統正確記錄 LINE User ID 至資料庫
- ✅ 綁定後介面顯示「已綁定：{Display Name}」與「解除綁定」按鈕
- ✅ 解除綁定後清除資料庫中的 LINE User ID
- ✅ 訪客角色不顯示綁定功能
- ✅ 防止重複綁定（一個 LINE 帳號僅能綁定一個系統帳號）

### LINE Bot 回報功能驗收
- ✅ 使用者在 LINE 輸入「回報問題」後啟動對話流程
- ✅ Bot 依序引導填寫所有必要欄位（標題、內容、單位、緊急程度、聯絡人、電話）
- ✅ 所屬單位與緊急程度以 Quick Reply 按鈕顯示
- ✅ 填寫完成後顯示摘要並提供確認/取消按鈕
- ✅ 確認送出後成功建立回報單，回覆回報單編號與連結
- ✅ 輸入「取消」可中斷流程
- ✅ 5 分鐘無回應自動清除暫存資料
- ✅ 未綁定的 LINE 使用者嘗試回報時顯示引導訊息

### LINE 通知功能驗收
- ✅ 新建立的回報單會推送通知給已綁定 LINE 的處理人員
- ✅ 通知使用 Flex Message 格式並包含所有必要資訊
- ✅ 通知包含「查看回報單詳情」按鈕，點擊後導向網頁端
- ✅ 未綁定 LINE 的處理人員不會收到推送（記錄至 NotificationLog）
- ✅ 推送失敗不影響回報單建立流程

### 整合測試驗收
- ✅ LINE Bot 建立的回報單與網頁端建立的回報單享有相同的處理流程
- ✅ LINE Bot 建立的回報單會自動指派處理人員並觸發通知
- ✅ LINE Bot 建立的回報單會記錄變更歷史（CreatedByUserId 為綁定的系統帳號）
- ✅ LINE Bot 建立的回報單會納入 Excel 報表統計
- ✅ 所有現有測試（UnitTests + IntegrationTests）持續通過

---

## 技術設計提示

### 建議實作順序
1. **Phase 1**: 新增 `User.LineUserId` Migration 與 `Handler.UserId` 關聯（若尚未建立）
2. **Phase 2**: 實作 LINE Login OAuth 2.0 流程與網頁端綁定 UI（含綁定 API 端點）
3. **Phase 3**: 實作 Webhook 端點與簽章驗證
4. **Phase 4**: 實作對話狀態管理 Service（`ConversationStateService`）
5. **Phase 5**: 實作 LINE Bot 對話邏輯（`LineBotMessageHandler`）
6. **Phase 6**: 整合 `CallRecordService.CreateAsync()`，完成端到端測試
7. **Phase 7**: 擴充 `LineNotificationService`，確保僅推送給已綁定使用者

### 建議 Service 設計
- `ILineLoginService`: 處理 OAuth 2.0 授權流程，取得 LINE User ID
- `ILineBotWebhookService`: 驗證簽章、解析事件、路由至對應的 Handler
- `IConversationStateService`: 管理對話狀態（CRUD 操作）
- `ILineBotMessageHandler`: 處理對話邏輯，引導使用者填寫表單
- 擴充現有的 `LineNotificationService`: 新增「僅推送給已綁定使用者」檢查

### 測試策略
- **單元測試**: 驗證對話狀態轉換邏輯、電話格式驗證、OAuth 2.0 流程
- **整合測試**: 端到端測試綁定流程、LINE Bot 建立回報單、通知發送
- **Mock LINE API**: 使用 Moq 模擬 LINE Messaging API 回應，避免依賴外部服務
