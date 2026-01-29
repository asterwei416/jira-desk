using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Core.DTOs;

/// <summary>
/// 建立來電紀錄請求 DTO
/// </summary>
public record CreateCallRecordRequest
{
    /// <summary>
    /// 問題主旨（最多 50 字）
    /// </summary>
    public required string Subject { get; init; }
    
    /// <summary>
    /// 問題內容（最多 150 字）
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// 詢問系統 ID
    /// </summary>
    public required int InquirySystemId { get; init; }
    
    /// <summary>
    /// 緊急程度
    /// </summary>
    public required UrgencyLevel UrgencyLevel { get; init; }
    
    /// <summary>
    /// 聯絡人姓名
    /// </summary>
    public required string ContactName { get; init; }
    
    /// <summary>
    /// 聯絡電話
    /// </summary>
    public required string ContactPhone { get; init; }
    
    /// <summary>
    /// FAQ 連結或說明（選填）
    /// </summary>
    public string? FaqReference { get; init; }
}

/// <summary>
/// 更新來電紀錄請求 DTO
/// </summary>
public record UpdateCallRecordRequest
{
    /// <summary>
    /// 問題主旨（最多 50 字）
    /// </summary>
    public required string Subject { get; init; }
    
    /// <summary>
    /// 問題內容（最多 150 字）
    /// </summary>
    public required string Content { get; init; }
    
    /// <summary>
    /// 緊急程度
    /// </summary>
    public required UrgencyLevel UrgencyLevel { get; init; }
    
    /// <summary>
    /// 聯絡人姓名
    /// </summary>
    public required string ContactName { get; init; }
    
    /// <summary>
    /// 聯絡電話
    /// </summary>
    public required string ContactPhone { get; init; }
    
    /// <summary>
    /// FAQ 連結或說明（選填）
    /// </summary>
    public string? FaqReference { get; init; }
}

/// <summary>
/// 來電紀錄回應 DTO
/// </summary>
public record CallRecordResponse
{
    public required int Id { get; init; }
    public required string Subject { get; init; }
    public required string Content { get; init; }
    public required ProcessStatus Status { get; init; }
    public required UrgencyLevel UrgencyLevel { get; init; }
    public required string ContactName { get; init; }
    public required string ContactPhone { get; init; }
    public string? FaqReference { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required string CreatedByUserId { get; init; }
    public string? LockedByUserId { get; init; }
    public DateTime? LockedAt { get; init; }
    
    // 關聯資料
    public required InquirySystemDto InquirySystem { get; init; }
    public required List<HandlerDto> Handlers { get; init; }
}

/// <summary>
/// 來電紀錄列表項目 DTO（簡化版本）
/// </summary>
public record CallRecordListItemResponse
{
    public required int Id { get; init; }
    public required string Subject { get; init; }
    public required ProcessStatus Status { get; init; }
    public required UrgencyLevel UrgencyLevel { get; init; }
    public required string ContactName { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required string InquirySystemName { get; init; }
    public required int HandlerCount { get; init; }
}

/// <summary>
/// 編輯鎖定狀態 DTO
/// </summary>
public record CallRecordLockStatusResponse
{
    public required bool IsLocked { get; init; }
    public string? LockedByUserId { get; init; }
    public DateTime? LockedAt { get; init; }
}

/// <summary>
/// 編輯鎖定操作結果 DTO
/// </summary>
public record CallRecordLockResponse
{
    public required bool Acquired { get; init; }
    public required bool IsLocked { get; init; }
    public string? LockedByUserId { get; init; }
    public DateTime? LockedAt { get; init; }
}

/// <summary>
/// 變更歷史項目 DTO
/// </summary>
public record ChangeHistoryItemDto
{
    public required string FieldName { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public required DateTime ChangedAt { get; init; }
    public required string ChangedByUserId { get; init; }
}

/// <summary>
/// 變更歷史回應 DTO
/// </summary>
public record ChangeHistoryResponse
{
    public required int CallRecordId { get; init; }
    public required List<ChangeHistoryItemDto> Changes { get; init; }
}

/// <summary>
/// 詢問系統 DTO
/// </summary>
public record InquirySystemDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
}

/// <summary>
/// 處理人員 DTO
/// </summary>
public record HandlerDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? LineUserId { get; init; }
}

/// <summary>
/// 分頁結果 DTO
/// </summary>
public record PagedResult<T>
{
    public required List<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Excel 報表匯出請求 DTO
/// </summary>
public record ExcelReportRequest
{
    public string? Keyword { get; init; }
    public int? InquirySystemId { get; init; }
    public string? Status { get; init; }
    public string? UrgencyLevel { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    /// <summary>
    /// 報表月份 (yyyy/MM) - 與 StartDate/EndDate 互斥
    /// </summary>
    public string? ReportMonth { get; init; }
}

/// <summary>
/// 搜尋條件（供資料查詢使用）
/// </summary>
public record CallRecordSearchCriteria
{
    public string? Keyword { get; init; }
    public int? InquirySystemId { get; init; }
    public ProcessStatus? Status { get; init; }
    public UrgencyLevel? UrgencyLevel { get; init; }
    public DateTime? StartDateUtc { get; init; }
    public DateTime? EndDateUtc { get; init; }
}
