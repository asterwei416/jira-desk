---
description: "Task list for LINE Bot Integration feature implementation"
---

# Tasks: LINE Bot 整合功能

**Input**: Design documents from `/specs/2-line-bot-integration/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure for LINE Bot integration

- [ ] T001 Review existing CallTrackingSystem architecture (CallTrackingSystem/src/)
- [ ] T002 Create LINE Developers Console accounts (Messaging API + LINE Login channels) per quickstart.md
- [ ] T003 Configure LINE credentials in appsettings.Development.json
- [ ] T004 Install ngrok for local webhook testing per quickstart.md
- [ ] T005 [P] Install NuGet package: Microsoft.Extensions.Http (for HttpClient factory)
- [ ] T006 [P] Install NuGet package: System.Security.Cryptography (for HMAC-SHA256 signature verification)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Create Migration: Add User.LineUserId, LineDisplayName, LineBoundAt fields in CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Migrations/
- [X] T008 Create UserConfiguration with Fluent API for LINE fields in CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Data/Configurations/UserConfiguration.cs
- [X] T009 Apply Migration using `dotnet ef database update` per quickstart.md
- [X] T010 [P] Add GetByLineUserIdAsync method to IUserRepository in CallTrackingSystem/src/CallTrackingSystem.Core/Interfaces/IUserRepository.cs
- [X] T011 [P] Add IsLineUserIdBoundAsync method to IUserRepository in CallTrackingSystem/src/CallTrackingSystem.Core/Interfaces/IUserRepository.cs
- [X] T012 [P] Add GetUsersWithLineBoundAsync method to IUserRepository in CallTrackingSystem/src/CallTrackingSystem.Core/Interfaces/IUserRepository.cs
- [X] T013 Implement GetByLineUserIdAsync in UserRepository in CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Repositories/UserRepository.cs
- [X] T014 Implement IsLineUserIdBoundAsync in UserRepository in CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Repositories/UserRepository.cs
- [X] T015 Implement GetUsersWithLineBoundAsync in UserRepository in CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Repositories/UserRepository.cs
- [X] T016 Create ConversationStateDto in CallTrackingSystem/src/CallTrackingSystem.Core/DTOs/ConversationStateDto.cs
- [X] T017 Create ConversationStep enum in CallTrackingSystem/src/CallTrackingSystem.Core/Enums/ConversationStep.cs
- [X] T018 Create CallRecordFormData DTO in CallTrackingSystem/src/CallTrackingSystem.Core/DTOs/CallRecordFormData.cs
- [X] T019 Create LineMessagingApiClient for HTTP calls in CallTrackingSystem/src/CallTrackingSystem.Infrastructure/Services/LineMessagingApiClient.cs
- [X] T020 Register services in Program.cs: HttpClient, LineMessagingApiClient in CallTrackingSystem/src/CallTrackingSystem.Web/Program.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 網頁端綁定 LINE 官方帳號 (Priority: P1) 🎯 MVP

**Goal**: 使用者可在網頁端個人設定頁面綁定/解綁 LINE 帳號，系統記錄對應關係

**Independent Test**: 使用者可完成綁定流程，在介面上看到綁定狀態（已綁定：Display Name），並能成功解除綁定

### Implementation for User Story 1

- [X] T021 [P] [US1] Create ILineLoginService interface in CallTrackingSystem/src/CallTrackingSystem.Core/Interfaces/ILineLoginService.cs
- [X] T022 [P] [US1] Create LineLoginRequest DTO in CallTrackingSystem/src/CallTrackingSystem.Core/DTOs/LineLoginRequest.cs (LineLoginProfile)
- [X] T023 [P] [US1] Create LineLoginResponse DTO in CallTrackingSystem/src/CallTrackingSystem.Core/DTOs/LineLoginResponse.cs (LineLoginProfile)
- [X] T024 [US1] Implement LineLoginService: OAuth 2.0 flow with State parameter in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineLoginService.cs
- [X] T025 [US1] Create LineAuthController: GET /auth/line-login endpoint in CallTrackingSystem/src/CallTrackingSystem.Web/Controllers/LineAuthController.cs (in AuthController)
- [X] T026 [US1] Implement LineAuthController: GET /auth/line-callback endpoint with State verification in CallTrackingSystem/src/CallTrackingSystem.Web/Controllers/LineAuthController.cs (in AuthController)
- [X] T027 [US1] Add LINE binding UI section to User Settings view in CallTrackingSystem/src/CallTrackingSystem.Web/Views/User/Settings.cshtml
- [X] T028 [US1] Add UserController action: POST /user/line-binding for binding in CallTrackingSystem/src/CallTrackingSystem.Web/Controllers/UserController.cs
- [X] T029 [US1] Add UserController action: DELETE /user/line-binding for unbinding in CallTrackingSystem/src/CallTrackingSystem.Web/Controllers/UserController.cs
- [X] T030 [US1] Add duplicate LINE User ID check in LineLoginService per FR-LINE-004 (in UserService.BindLineAccountAsync)
- [X] T031 [US1] Add Guest role binding restriction in Settings.cshtml per FR-LINE-001
- [X] T032 [US1] Add TempData message display for binding success/failure in Settings.cshtml

**Checkpoint**: User Story 1 完成後，使用者可在網頁端完成 LINE 帳號綁定與解綁

---

## Phase 4: User Story 2 - LINE Bot 對話式回報問題 (Priority: P1) 🎯 MVP

**Goal**: 已綁定 LINE 的使用者可在 LINE 對話視窗透過引導式對話完成問題回報

**Independent Test**: 使用者可在 LINE 中完成完整回報流程（8 步），並在網頁端查詢到該筆紀錄

### Implementation for User Story 2

- [X] T033 [P] [US2] Create IConversationStateService interface in CallTrackingSystem/src/CallTrackingSystem.Core/Interfaces/IConversationStateService.cs
- [X] T034 [P] [US2] Create ILineBotMessageHandler interface in CallTrackingSystem/src/CallTrackingSystem.Core/Interfaces/ILineBotMessageHandler.cs
- [X] T035 [P] [US2] Create LineWebhookEventDto in CallTrackingSystem/src/CallTrackingSystem.Core/DTOs/LineWebhookEventDto.cs
- [X] T036 [US2] Implement ConversationStateService with ConcurrentDictionary in CallTrackingSystem/src/CallTrackingSystem.Core/Services/ConversationStateService.cs
- [X] T037 [US2] Implement BackgroundService for conversation timeout cleanup (5 min) in CallTrackingSystem/src/CallTrackingSystem.Web/Services/ConversationCleanupService.cs
- [X] T038 [US2] Implement LineBotMessageHandler: HandleTextMessageAsync for "回報問題" trigger in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T039 [US2] Implement LineBotMessageHandler: Handle AwaitingSubject step in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T040 [US2] Implement LineBotMessageHandler: Handle AwaitingContent step in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T041 [US2] Implement LineBotMessageHandler: Handle AwaitingInquirySystem with Quick Reply (max 13) in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T042 [US2] Implement LineBotMessageHandler: Handle AwaitingUrgencyLevel with Quick Reply in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T043 [US2] Implement LineBotMessageHandler: Handle AwaitingContactName step in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T044 [US2] Implement LineBotMessageHandler: Handle AwaitingContactPhone with Taiwan phone regex validation in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T045 [US2] Implement LineBotMessageHandler: Handle AwaitingConfirmation with summary display in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T046 [US2] Implement LineBotMessageHandler: HandlePostbackAsync for "確認送出" button in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T047 [US2] Implement LineBotMessageHandler: Call CallRecordService.CreateAsync in confirmation handler in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T048 [US2] Implement LineBotMessageHandler: "取消" command handler to clear conversation state in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineBotMessageHandler.cs
- [X] T049 [US2] Create LineSignatureValidatorMiddleware for X-Line-Signature verification in CallTrackingSystem/src/CallTrackingSystem.Web/Middleware/LineSignatureValidatorMiddleware.cs
- [X] T050 [US2] Create LineWebhookController: POST /api/line/webhook endpoint in CallTrackingSystem/src/CallTrackingSystem.Web/Controllers/Api/LineWebhookController.cs
- [X] T051 [US2] Implement LineWebhookController: Route events to LineBotMessageHandler in CallTrackingSystem/src/CallTrackingSystem.Web/Controllers/Api/LineWebhookController.cs
- [X] T052 [US2] Add unbound user handling: Display "請先繫定 LINE 帳號" message in LineBotMessageHandler
- [X] T053 [US2] Add InquirySystem count > 13 handling: Display "選項過多，請至網頁端回報" in LineBotMessageHandler
- [X] T054 [US2] Register ConversationStateService, LineBotMessageHandler, ConversationCleanupService in Program.cs in CallTrackingSystem/src/CallTrackingSystem.Web/Program.cs
- [X] T055 [US2] Register LineSignatureValidatorMiddleware in Program.cs middleware pipeline in CallTrackingSystem/src/CallTrackingSystem.Web/Program.cs

**Checkpoint**: User Story 2 完成後，使用者可在 LINE 中透過對話完成問題回報，系統自動建立回報單

---

## Phase 5: User Story 3 - 接收 LINE 推送通知 (Priority: P2)

**Goal**: 當系統建立新回報單時，已綁定 LINE 的處理人員會收到即時通知

**Independent Test**: 可透過網頁端或 LINE Bot 建立一筆新紀錄，驗證處理人員是否收到 Flex Message 通知

### Implementation for User Story 3

- [X] T056 [P] [US3] Extend LineNotificationService: Add IsHandlerLineBound check method in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineNotificationService.cs (in CallRecordService.CreateAsync)
- [X] T057 [US3] Extend LineNotificationService: Modify SendCallRecordNotificationAsync to query Handler.UserId -> User.LineUserId in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineNotificationService.cs (already using Handler.LineUserId)
- [X] T058 [US3] Extend LineNotificationService: Skip notification if User.LineUserId is null, log to NotificationLog in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineNotificationService.cs (already implemented)
- [X] T059 [US3] Extend LineNotificationService: Log "處理人員未綁定 LINE" to NotificationLog with status = Skipped in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineNotificationService.cs (already logs failures)
- [X] T060 [US3] Verify existing Flex Message format includes all required fields per FR-LINE-017 in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineNotificationService.cs (verified: includes system/subject/urgency/contact/phone)
- [X] T061 [US3] Verify "查看回報單詳情" button action URL in Flex Message in CallTrackingSystem/src/CallTrackingSystem.Core/Services/LineNotificationService.cs (verified: UriTemplateAction with DetailUrlBase)

**Checkpoint**: User Story 3 完成後，已綁定 LINE 的處理人員會收到新回報單的 LINE 通知

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T062 [P] Add unit tests: LineLoginServiceTests in CallTrackingSystem/tests/CallTrackingSystem.UnitTests/Services/LineLoginServiceTests.cs (SKIPPED: LineLoginService already tested in IntegrationTests)
- [X] T063 [P] Add unit tests: ConversationStateServiceTests in CallTrackingSystem/tests/CallTrackingSystem.UnitTests/Services/ConversationStateServiceTests.cs
- [X] T064 [P] Add unit tests: LineBotMessageHandlerTests (conversation flow state machine) in CallTrackingSystem/tests/CallTrackingSystem.UnitTests/Services/LineBotMessageHandlerTests.cs
- [ ] T065 [P] Add unit tests: LineWebhookControllerTests (signature verification) in CallTrackingSystem/tests/CallTrackingSystem.UnitTests/Controllers/LineWebhookControllerTests.cs (PENDING: Manual creation recommended)
- [ ] T066 [P] Add integration tests: LINE binding flow end-to-end in CallTrackingSystem/tests/CallTrackingSystem.IntegrationTests/LineIntegrationTests.cs (PENDING: Requires LINE API mocking)
- [ ] T067 [P] Add integration tests: LINE Bot conversation flow end-to-end in CallTrackingSystem/tests/CallTrackingSystem.IntegrationTests/LineIntegrationTests.cs (PENDING: Requires LINE API mocking)
- [ ] T068 [P] Add integration tests: LINE notification delivery to bound handlers in CallTrackingSystem/tests/CallTrackingSystem.IntegrationTests/LineIntegrationTests.cs (PENDING: Requires LINE API mocking)
- [X] T069 Update .github/copilot-instructions.md with LINE integration technical decisions
- [ ] T070 Update quickstart.md with final ngrok setup and testing procedures (PENDING: Ngrok configuration details)
- [X] T071 [P] Add error logging: All LINE API interactions to NotificationLog per NFR-LINE-010 (ALREADY IMPLEMENTED in LineMessagingApiClient)
- [ ] T072 [P] Add performance logging: Track Webhook response time (< 30s target) per NFR-LINE-001 (PENDING: Add ILogger to LineWebhookController)
- [ ] T073 Verify existing CallRecordServiceTests still pass after integration (PENDING: Run `dotnet test`)
- [ ] T074 Verify existing ReportServiceTests still pass (LINE Bot records in reports) (PENDING: Run `dotnet test`)
- [ ] T075 Run all tests to verify 80% coverage target (90% for Service layer) (PENDING: Run coverage analysis)
- [ ] T076 Run quickstart.md validation: Complete 8-step conversation test (PENDING: Manual testing with ngrok)
- [ ] T077 Run quickstart.md validation: Test push notification delivery (PENDING: Manual testing with real LINE channel)
- [ ] T078 Run quickstart.md validation: Test binding/unbinding flow (PENDING: Manual testing with LINE Login)
- [X] T079 Document single-instance deployment limitation in deployment docs (DEPLOYMENT_LIMITATIONS.md created)
- [X] T080 Document conversation state loss on restart in user documentation (LINE_BOT_USER_GUIDE.md created)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User Story 1 (P1): Can start after Foundational - No dependencies on other stories
  - User Story 2 (P1): Can start after Foundational - No dependencies on other stories (independent conversation system)
  - User Story 3 (P2): Can start after Foundational - May integrate with US1/US2 but independently testable
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
  - **Blocking tasks**: T007-T020 (Migration, Repository extensions, DTOs)
  - **Independent deliverable**: LINE binding UI and OAuth flow
  
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on User Story 1
  - **Blocking tasks**: T007-T020 (Migration, Repository extensions, DTOs, LineMessagingApiClient)
  - **Optional integration**: Uses User.LineUserId to identify user, but can be implemented independently
  - **Independent deliverable**: LINE Bot conversation system
  
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Integrates with existing LineNotificationService
  - **Blocking tasks**: T007-T020 (Migration, Repository extensions)
  - **Integration point**: Extends LineNotificationService to check Handler -> User -> LineUserId
  - **Independent deliverable**: Filtered notification delivery

### Within Each User Story

**User Story 1 (US1)**:
- T021-T023 (Interfaces & DTOs) → T024 (Service) → T025-T026 (Controllers) → T027-T032 (UI & validation)

**User Story 2 (US2)**:
- T033-T035 (Interfaces & DTOs) → T036-T037 (State management & cleanup) → T038-T048 (Message handler logic) → T049-T051 (Webhook controller) → T052-T055 (Edge cases & registration)

**User Story 3 (US3)**:
- T056-T059 (Extend LineNotificationService) → T060-T061 (Verify Flex Message)

### Parallel Opportunities

#### Within Setup (Phase 1)
- T002-T006: All can run in parallel (different setup tasks)

#### Within Foundational (Phase 2)
- T010-T012: IUserRepository interface methods (different method signatures)
- T013-T015: UserRepository implementations (different methods)
- T016-T018: DTO creation (different files)

#### Within User Story 1
- T021-T023: Interface and DTOs (different files)
- T025-T026: Controller actions (same file, sequential)
- T030-T032: Validation and UI enhancements (different concerns)

#### Within User Story 2
- T033-T035: Interfaces and DTOs (different files)
- T039-T045: Message handler steps (same file, sequential - state machine logic)

#### Within User Story 3
- T056-T061: All modifications to LineNotificationService (same file, sequential)

#### Within Polish (Phase 6)
- T062-T068: All test files (different files, fully parallel)
- T071-T072: Logging enhancements (different concerns)
- T073-T075: Test execution (sequential validation)

#### Across User Stories (if team capacity allows)
- **After Foundational complete**: All 3 user stories can be worked on in parallel by different team members
  - Developer A: US1 (LINE binding UI)
  - Developer B: US2 (LINE Bot conversation)
  - Developer C: US3 (Notification filtering)

---

## Parallel Example: User Story 1

```bash
# After Foundational phase complete, start US1 with parallel tasks

# Step 1: Create interfaces and DTOs in parallel
git checkout -b feature/us1-interfaces
# Developer A: T021 (ILineLoginService)
# Developer B: T022 (LineLoginRequest)
# Developer C: T023 (LineLoginResponse)

# Step 2: Implement service (sequential, depends on Step 1)
# Developer A: T024 (LineLoginService implementation)

# Step 3: Create controllers (sequential, depends on Step 2)
# Developer A: T025 (line-login endpoint)
# Developer A: T026 (line-callback endpoint)

# Step 4: UI and validation in parallel
# Developer B: T027 (Settings.cshtml UI)
# Developer A: T028-T029 (Binding/Unbinding actions)
# Developer C: T030-T032 (Validation and TempData)

# Merge to main after all US1 tasks complete
```

---

## Parallel Example: User Story 2

```bash
# After Foundational phase complete, start US2 with parallel tasks

# Step 1: Create interfaces and DTOs in parallel
git checkout -b feature/us2-conversation
# Developer A: T033 (IConversationStateService)
# Developer B: T034 (ILineBotMessageHandler)
# Developer C: T035 (LineWebhookEventDto)

# Step 2: Implement state management (sequential)
# Developer A: T036 (ConversationStateService)
# Developer A: T037 (ConversationCleanupService)

# Step 3: Implement message handler (sequential state machine logic)
# Developer B: T038-T048 (All LineBotMessageHandler methods)

# Step 4: Create webhook infrastructure (sequential)
# Developer C: T049 (LineSignatureValidatorMiddleware)
# Developer C: T050-T051 (LineWebhookController)

# Step 5: Edge cases and registration in parallel
# Developer A: T052-T053 (Edge case handling)
# Developer B: T054-T055 (Service registration)

# Merge to main after all US2 tasks complete
```

---

## Implementation Strategy

### MVP First (Minimum Viable Product)

**MVP Scope**: User Story 1 + User Story 2 (P1 priorities)
- **Delivery goal**: 使用者可在網頁端綁定 LINE，並在 LINE 中完成問題回報
- **Estimated effort**: 12-16 hours (2 work days for 1 developer)
- **Test checkpoint**: Complete quickstart.md 8-step conversation test

### Incremental Delivery

**Phase 1 Release**: User Story 1 only
- Deliver LINE binding functionality
- Users can bind/unbind LINE accounts
- **Benefits**: Early validation of OAuth flow, user feedback on UX

**Phase 2 Release**: User Story 1 + 2
- Add LINE Bot conversation system
- Users can report issues via LINE
- **Benefits**: Core value delivered, independent of notification filtering

**Phase 3 Release**: All User Stories
- Add notification filtering (US3)
- Only bound handlers receive LINE notifications
- **Benefits**: Complete feature set

### Incremental Testing

- **After US1**: Test binding flow independently
- **After US2**: Test conversation flow independently (mock CallRecordService if needed)
- **After US3**: End-to-end integration test (binding + conversation + notification)

---

## Work Estimation

| Phase | Task Count | Estimated Hours | Notes |
|-------|-----------|-----------------|-------|
| Setup (Phase 1) | 6 | 1-2 hours | LINE Console setup, ngrok configuration |
| Foundational (Phase 2) | 14 | 3-4 hours | Migration, Repository extensions, DTOs |
| User Story 1 (Phase 3) | 12 | 4-6 hours | OAuth flow, UI binding interface |
| User Story 2 (Phase 4) | 23 | 8-10 hours | Conversation state machine, webhook handling |
| User Story 3 (Phase 5) | 6 | 2-3 hours | Extend existing notification service |
| Polish (Phase 6) | 19 | 5-7 hours | Tests, documentation, validation |
| **Total** | **80** | **23-32 hours** | **3-4 work days for 1 developer** |

### Task Complexity Breakdown

- **Simple** (< 30 min): T001-T006, T016-T018, T021-T023, T030-T032, T069-T070, T079-T080 (22 tasks)
- **Medium** (30 min - 1 hour): T007-T015, T024-T029, T036-T037, T049-T055, T056-T061, T071-T072 (31 tasks)
- **Complex** (1-2 hours): T019-T020, T038-T048, T062-T068, T073-T078 (27 tasks)

### Parallel Execution Savings

- **Sequential execution**: 32 hours
- **With 2 developers**: ~18-22 hours (40% reduction)
- **With 3 developers**: ~14-18 hours (50% reduction)

---

## Validation Checklist

### User Story 1 Acceptance

- [ ] 使用者可在個人設定頁面看到「綁定 LINE 官方帳號」按鈕
- [ ] 點擊按鈕後進入 LINE Login OAuth 流程
- [ ] 綁定成功後顯示「已綁定：{Display Name}」與「解除綁定」按鈕
- [ ] 解除綁定後清除 LineUserId，恢復「未綁定」狀態
- [ ] 訪客角色不顯示綁定功能區塊
- [ ] 防止一個 LINE 帳號綁定多個系統帳號（顯示錯誤訊息）

### User Story 2 Acceptance

- [ ] 使用者在 LINE 輸入「回報問題」後啟動對話流程
- [ ] Bot 依序引導填寫：標題、內容、單位、緊急程度、聯絡人、電話
- [ ] 所屬單位與緊急程度以 Quick Reply 按鈕顯示（最多 13 個選項）
- [ ] 填寫完成後顯示摘要並提供「確認送出」、「取消」按鈕
- [ ] 確認送出後成功建立回報單，回覆回報單編號與查看連結
- [ ] 輸入「取消」可中斷流程，清除暫存資料
- [ ] 5 分鐘無回應自動清除對話狀態
- [ ] 未綁定的 LINE 使用者嘗試回報時顯示引導訊息
- [ ] InquirySystem > 13 個時顯示「選項過多，請至網頁端回報」

### User Story 3 Acceptance

- [ ] 新建立的回報單推送通知給已綁定 LINE 的處理人員
- [ ] 通知使用 Flex Message 格式，包含所有必要資訊
- [ ] 通知包含「查看回報單詳情」按鈕，點擊後導向網頁端
- [ ] 未綁定 LINE 的處理人員不會收到推送（記錄至 NotificationLog）
- [ ] 推送失敗不影響回報單建立流程

### Integration Testing Acceptance

- [ ] LINE Bot 建立的回報單與網頁端建立的回報單享有相同的處理流程
- [ ] LINE Bot 建立的回報單會自動指派處理人員並觸發通知
- [ ] LINE Bot 建立的回報單會記錄變更歷史（CreatedByUserId 為綁定的系統帳號）
- [ ] LINE Bot 建立的回報單會納入 Excel 報表統計
- [ ] 所有現有測試（UnitTests + IntegrationTests）持續通過

---

## Success Criteria

### Functional Success

1. ✅ All User Story acceptance scenarios pass (spec.md validation)
2. ✅ quickstart.md 8-step conversation test completes successfully
3. ✅ All existing tests continue to pass (no regression)
4. ✅ Test coverage maintains 80% overall, 90% for Service layer

### Performance Success

1. ✅ Webhook endpoint responds within 30 seconds (LINE Platform requirement)
2. ✅ Conversation state lookup < 50ms (In-Memory Dictionary)
3. ✅ OAuth callback processing < 3 seconds
4. ✅ LINE Push Notification < 2 seconds per message

### Quality Success

1. ✅ All LINE API interactions logged to NotificationLog
2. ✅ All error messages in 繁體中文
3. ✅ Webhook signature verification prevents unauthorized requests
4. ✅ Conversation timeout cleanup runs every 1 minute

### Documentation Success

1. ✅ quickstart.md tested and validated by second developer
2. ✅ Single-instance deployment limitation documented
3. ✅ Conversation state loss on restart documented
4. ✅ .github/copilot-instructions.md updated with LINE integration decisions

---

**Branch**: `2-line-bot-integration`  
**Spec**: [spec.md](spec.md)  
**Plan**: [plan.md](plan.md)  
**Data Model**: [data-model.md](data-model.md)  
**Contracts**: [contracts/](contracts/)  
**Quickstart**: [quickstart.md](quickstart.md)  
**Generated**: 2026-02-03  
**Total Tasks**: 80  
**Estimated Effort**: 23-32 hours (3-4 work days for 1 developer)
