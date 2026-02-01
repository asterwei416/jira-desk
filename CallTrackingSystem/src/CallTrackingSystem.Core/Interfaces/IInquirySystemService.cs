using CallTrackingSystem.Core.Entities;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 詢問系統管理服務
/// </summary>
public interface IInquirySystemService
{
    Task<List<InquirySystem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InquirySystem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InquirySystem> CreateAsync(string name, CancellationToken cancellationToken = default);
    Task<InquirySystem> UpdateAsync(int id, string name, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
