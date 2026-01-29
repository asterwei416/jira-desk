using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 來電紀錄服務
/// </summary>
public class CallRecordService
{
    private readonly ICallRecordRepository _callRecordRepository;
    private readonly IInquirySystemRepository _inquirySystemRepository;
    private readonly IHandlerRepository _handlerRepository;

    public CallRecordService(
        ICallRecordRepository callRecordRepository,
        IInquirySystemRepository inquirySystemRepository,
        IHandlerRepository handlerRepository)
    {
        _callRecordRepository = callRecordRepository;
        _inquirySystemRepository = inquirySystemRepository;
        _handlerRepository = handlerRepository;
    }

    /// <summary>
    /// 建立新來電紀錄
    /// </summary>
    public async Task<CallRecordResponse> CreateAsync(
        CreateCallRecordRequest request, 
        string userId,
        CancellationToken cancellationToken = default)
    {
        // 驗證詢問系統是否存在
        var inquirySystem = await _inquirySystemRepository.GetByIdAsync(
            request.InquirySystemId, 
            cancellationToken);
        
        if (inquirySystem == null)
        {
            throw new InvalidOperationException("詢問系統不存在");
        }

        // 建立實體
        var callRecord = CallRecord.Create(
            request.Subject,
            request.Content,
            request.InquirySystemId,
            request.UrgencyLevel,
            request.ContactName,
            request.ContactPhone,
            userId);

        if (!string.IsNullOrWhiteSpace(request.FaqReference))
        {
            callRecord.FaqReference = request.FaqReference;
        }

        // 儲存
        var created = await _callRecordRepository.AddAsync(callRecord, cancellationToken);

        // 取得完整資料（含關聯）
        var result = await _callRecordRepository.GetByIdAsync(created.Id, cancellationToken);
        
        return MapToResponse(result!);
    }

    /// <summary>
    /// 取得來電紀錄詳情
    /// </summary>
    public async Task<CallRecordResponse?> GetByIdAsync(
        int id, 
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        
        return callRecord == null ? null : MapToResponse(callRecord);
    }

    /// <summary>
    /// 取得分頁來電紀錄列表
    /// </summary>
    public async Task<PagedResult<CallRecordListItemResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchKeyword = null,
        int? inquirySystemId = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _callRecordRepository.GetPagedAsync(
            pageNumber,
            pageSize,
            searchKeyword,
            inquirySystemId,
            cancellationToken);

        var responseItems = items.Select(MapToListItemResponse).ToList();

        return new PagedResult<CallRecordListItemResponse>
        {
            Items = responseItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 更新來電紀錄
    /// </summary>
    public async Task<CallRecordResponse> UpdateAsync(
        int id,
        UpdateCallRecordRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        // 檢查編輯鎖定
        if (callRecord.IsLockedByOther(userId))
        {
            throw new InvalidOperationException($"此紀錄正被其他使用者編輯中（鎖定者: {callRecord.LockedByUserId}）");
        }

        // 更新資料
        callRecord.Update(
            request.Subject,
            request.Content,
            request.UrgencyLevel,
            request.ContactName,
            request.ContactPhone,
            request.FaqReference);

        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        var updated = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        return MapToResponse(updated!);
    }

    /// <summary>
    /// 刪除來電紀錄
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(id, cancellationToken);
        
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        await _callRecordRepository.DeleteAsync(callRecord, cancellationToken);
    }

    /// <summary>
    /// 取得所有啟用的詢問系統
    /// </summary>
    public async Task<List<InquirySystemDto>> GetActiveInquirySystemsAsync(
        CancellationToken cancellationToken = default)
    {
        var systems = await _inquirySystemRepository.GetActiveSystemsAsync(cancellationToken);
        
        return systems.Select(s => new InquirySystemDto
        {
            Id = s.Id,
            Name = s.Name,
            IsActive = s.IsActive
        }).ToList();
    }

    // 私有輔助方法
    private static CallRecordResponse MapToResponse(CallRecord callRecord)
    {
        return new CallRecordResponse
        {
            Id = callRecord.Id,
            Subject = callRecord.Subject,
            Content = callRecord.Content,
            Status = callRecord.Status,
            UrgencyLevel = callRecord.UrgencyLevel,
            ContactName = callRecord.ContactName,
            ContactPhone = callRecord.ContactPhone,
            FaqReference = callRecord.FaqReference,
            CreatedAt = callRecord.CreatedAt,
            UpdatedAt = callRecord.UpdatedAt,
            CreatedByUserId = callRecord.CreatedByUserId,
            LockedByUserId = callRecord.LockedByUserId,
            LockedAt = callRecord.LockedAt,
            InquirySystem = new InquirySystemDto
            {
                Id = callRecord.InquirySystem.Id,
                Name = callRecord.InquirySystem.Name,
                IsActive = callRecord.InquirySystem.IsActive
            },
            Handlers = callRecord.Handlers.Select(h => new HandlerDto
            {
                Id = h.Id,
                Name = h.Name,
                LineUserId = h.LineUserId
            }).ToList()
        };
    }

    private static CallRecordListItemResponse MapToListItemResponse(CallRecord callRecord)
    {
        return new CallRecordListItemResponse
        {
            Id = callRecord.Id,
            Subject = callRecord.Subject,
            Status = callRecord.Status,
            UrgencyLevel = callRecord.UrgencyLevel,
            ContactName = callRecord.ContactName,
            CreatedAt = callRecord.CreatedAt,
            InquirySystemName = callRecord.InquirySystem.Name,
            HandlerCount = callRecord.Handlers.Count
        };
    }
}
