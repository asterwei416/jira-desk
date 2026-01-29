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
        int pageNumber,
        int pageSize,
        string? searchKeyword = null,
        int? inquirySystemId = null,
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
}
