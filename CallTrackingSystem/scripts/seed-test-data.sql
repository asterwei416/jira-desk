-- ========================================
-- 測試資料種子檔案
-- 用於 LINE 通知整合測試
-- ========================================

-- 1. 建立詢問系統範例資料
INSERT INTO InquirySystems (Id, SystemName, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (1, '訂單查詢系統', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (2, '會員服務系統', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
    (3, '物流追蹤系統', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- 2. 建立處理人員（含 LINE User ID）
INSERT INTO Handlers (Id, HandlerName, LineUserId, IsActive, CreatedAt, UpdatedAt)
VALUES 
    (1, '測試人員', 'U7d6dea4b033c775bc811eabe558b3607', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- 3. 建立處理人員對應關係
-- 將「訂單查詢系統」對應到測試人員
INSERT INTO HandlerMappings (Id, InquirySystemId, HandlerId, CreatedAt, UpdatedAt)
VALUES 
    (1, 1, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- ========================================
-- 驗證資料
-- ========================================
SELECT '===== 詢問系統 =====' AS Info;
SELECT * FROM InquirySystems;

SELECT '===== 處理人員 =====' AS Info;
SELECT * FROM Handlers;

SELECT '===== 對應關係 =====' AS Info;
SELECT * FROM HandlerMappings;
