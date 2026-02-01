using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 處理人員對應管理服務
/// </summary>
public interface IHandlerMappingService
{
    Task<List<HandlerMapping>> GetMappingsAsync(int? inquirySystemId, int? handlerId, CancellationToken cancellationToken = default);
    Task<HandlerMapping> CreateMappingAsync(int handlerId, int inquirySystemId, CancellationToken cancellationToken = default);
    Task DeleteMappingAsync(int id, CancellationToken cancellationToken = default);
}
