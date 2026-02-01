using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 處理人員對應管理服務
/// </summary>
public class HandlerMappingService : IHandlerMappingService
{
    private readonly IHandlerMappingRepository _handlerMappingRepository;
    private readonly IHandlerRepository _handlerRepository;
    private readonly IInquirySystemRepository _inquirySystemRepository;

    public HandlerMappingService(
        IHandlerMappingRepository handlerMappingRepository,
        IHandlerRepository handlerRepository,
        IInquirySystemRepository inquirySystemRepository)
    {
        _handlerMappingRepository = handlerMappingRepository;
        _handlerRepository = handlerRepository;
        _inquirySystemRepository = inquirySystemRepository;
    }

    public async Task<List<HandlerMapping>> GetMappingsAsync(
        int? inquirySystemId,
        int? handlerId,
        CancellationToken cancellationToken = default)
    {
        return await _handlerMappingRepository.GetMappingsAsync(inquirySystemId, handlerId, cancellationToken);
    }

    public async Task<HandlerMapping> CreateMappingAsync(
        int handlerId,
        int inquirySystemId,
        CancellationToken cancellationToken = default)
    {
        var handler = await _handlerRepository.GetByIdAsync(handlerId, cancellationToken);
        if (handler == null)
        {
            throw new InvalidOperationException("找不到指定處理人員");
        }

        var system = await _inquirySystemRepository.GetByIdAsync(inquirySystemId, cancellationToken);
        if (system == null)
        {
            throw new InvalidOperationException("找不到指定詢問系統");
        }

        var exists = await _handlerMappingRepository.ExistsAsync(handlerId, inquirySystemId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("此處理人員已對應到該詢問系統");
        }

        var mapping = HandlerMapping.Create(handlerId, inquirySystemId);
        return await _handlerMappingRepository.AddAsync(mapping, cancellationToken);
    }

    public async Task DeleteMappingAsync(int id, CancellationToken cancellationToken = default)
    {
        var mapping = await _handlerMappingRepository.GetByIdAsync(id, cancellationToken);
        if (mapping == null)
        {
            throw new InvalidOperationException("找不到指定對應關係");
        }

        await _handlerMappingRepository.DeleteAsync(mapping, cancellationToken);
    }
}
