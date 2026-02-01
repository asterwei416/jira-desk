using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 來電紀錄 Repository 介面
/// </summary>
public interface ICallRecordRepository
{
    /// <summary>
    /// 根據 ID 取得來電紀錄（含關聯資料）
    /// </summary>
    Task<CallRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 取得分頁來電紀錄列表
    /// </summary>
    Task<(List<CallRecord> Items, int TotalCount)> GetPagedAsync(
        CallTrackingSystem.Core.DTOs.CallRecordSearchCriteria criteria,
        int pageNumber,
        int pageSize,
        string sortBy,
        string sortOrder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依條件搜尋來電紀錄（供報表匯出）
    /// </summary>
    Task<List<CallRecord>> SearchAsync(
        CallTrackingSystem.Core.DTOs.CallRecordSearchCriteria criteria,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 新增來電紀錄
    /// </summary>
    Task<CallRecord> AddAsync(CallRecord callRecord, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新來電紀錄
    /// </summary>
    Task UpdateAsync(CallRecord callRecord, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 刪除來電紀錄
    /// </summary>
    Task DeleteAsync(CallRecord callRecord, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 檢查來電紀錄是否存在
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 詢問系統 Repository 介面
/// </summary>
public interface IInquirySystemRepository
{
    /// <summary>
    /// 根據 ID 取得詢問系統
    /// </summary>
    Task<InquirySystem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 取得所有啟用的詢問系統
    /// </summary>
    Task<List<InquirySystem>> GetActiveSystemsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 處理人員 Repository 介面
/// </summary>
public interface IHandlerRepository
{
    /// <summary>
    /// 根據詢問系統 ID 取得對應的處理人員
    /// </summary>
    Task<List<Handler>> GetHandlersByInquirySystemIdAsync(int inquirySystemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據處理人員 ID 清單取得處理人員
    /// </summary>
    Task<List<Handler>> GetByIdsAsync(IReadOnlyCollection<int> handlerIds, CancellationToken cancellationToken = default);
}

/// <summary>
/// 變更歷史 Repository 介面
/// </summary>
public interface IChangeHistoryRepository
{
    /// <summary>
    /// 取得指定來電紀錄的變更歷史
    /// </summary>
    Task<List<ChangeHistory>> GetByCallRecordIdAsync(int callRecordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增變更歷史
    /// </summary>
    Task AddRangeAsync(IEnumerable<ChangeHistory> histories, CancellationToken cancellationToken = default);
}

/// <summary>
/// 使用者 Repository 介面
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 根據 ID 取得使用者
    /// </summary>
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據使用者名稱取得使用者
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根據 LINE User ID 取得使用者
    /// </summary>
    Task<User?> GetByLineUserIdAsync(string lineUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增使用者
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新使用者
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}

/// <summary>
/// 通知記錄 Repository 介面
/// </summary>
public interface INotificationLogRepository
{
    /// <summary>
    /// 取得指定來電紀錄的通知記錄
    /// </summary>
    Task<List<NotificationLog>> GetByCallRecordIdAsync(int callRecordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得失敗的通知記錄
    /// </summary>
    Task<List<NotificationLog>> GetFailedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增通知記錄
    /// </summary>
    Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default);
}
