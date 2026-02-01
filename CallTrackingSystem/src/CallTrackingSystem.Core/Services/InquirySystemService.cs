using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 詢問系統管理服務
/// </summary>
public class InquirySystemService : IInquirySystemService
{
    private readonly IInquirySystemRepository _inquirySystemRepository;

    public InquirySystemService(IInquirySystemRepository inquirySystemRepository)
    {
        _inquirySystemRepository = inquirySystemRepository;
    }

    public async Task<List<InquirySystem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _inquirySystemRepository.GetAllAsync(cancellationToken);
    }

    public async Task<InquirySystem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _inquirySystemRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<InquirySystem> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("系統名稱不可為空");
        }

        var existing = await _inquirySystemRepository.GetByNameAsync(name, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("系統名稱已存在");
        }

        var inquirySystem = InquirySystem.Create(name);
        return await _inquirySystemRepository.AddAsync(inquirySystem, cancellationToken);
    }

    public async Task<InquirySystem> UpdateAsync(
        int id,
        string name,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var inquirySystem = await _inquirySystemRepository.GetByIdAsync(id, cancellationToken);
        if (inquirySystem == null)
        {
            throw new InvalidOperationException("詢問系統不存在");
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var existing = await _inquirySystemRepository.GetByNameAsync(name, cancellationToken);
            if (existing != null && existing.Id != id)
            {
                throw new InvalidOperationException("系統名稱已存在");
            }

            inquirySystem.UpdateName(name);
        }

        if (isActive)
        {
            inquirySystem.Activate();
        }
        else
        {
            inquirySystem.Deactivate();
        }

        await _inquirySystemRepository.UpdateAsync(inquirySystem, cancellationToken);
        return inquirySystem;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var inquirySystem = await _inquirySystemRepository.GetByIdAsync(id, cancellationToken);
        if (inquirySystem == null)
        {
            throw new InvalidOperationException("詢問系統不存在");
        }

        var hasRecords = await _inquirySystemRepository.HasCallRecordsAsync(id, cancellationToken);
        if (hasRecords)
        {
            throw new InvalidOperationException("此詢問系統有相關來電紀錄，無法刪除");
        }

        await _inquirySystemRepository.DeleteAsync(inquirySystem, cancellationToken);
    }
}
