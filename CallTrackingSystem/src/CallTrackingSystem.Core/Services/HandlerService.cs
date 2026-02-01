using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 處理人員管理服務
/// </summary>
public class HandlerService : IHandlerService
{
    private readonly IHandlerRepository _handlerRepository;

    public HandlerService(IHandlerRepository handlerRepository)
    {
        _handlerRepository = handlerRepository;
    }

    public async Task<List<Handler>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _handlerRepository.GetAllAsync(cancellationToken);
    }

    public async Task<Handler?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _handlerRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Handler> CreateAsync(string name, string? lineUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("姓名不可為空");
        }

        if (!string.IsNullOrWhiteSpace(lineUserId))
        {
            var existing = await _handlerRepository.GetByLineUserIdAsync(lineUserId, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException("此 LINE User ID 已被使用");
            }
        }

        var handler = Handler.Create(name, lineUserId);
        return await _handlerRepository.AddAsync(handler, cancellationToken);
    }

    public async Task<Handler> UpdateAsync(
        int id,
        string name,
        string? lineUserId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var handler = await _handlerRepository.GetByIdAsync(id, cancellationToken);
        if (handler == null)
        {
            throw new InvalidOperationException("處理人員不存在");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("姓名不可為空");
        }

        if (!string.IsNullOrWhiteSpace(lineUserId))
        {
            var existing = await _handlerRepository.GetByLineUserIdAsync(lineUserId, cancellationToken);
            if (existing != null && existing.Id != id)
            {
                throw new InvalidOperationException("此 LINE User ID 已被使用");
            }
        }

        handler.UpdateInfo(name, lineUserId);
        if (isActive)
        {
            handler.Activate();
        }
        else
        {
            handler.Deactivate();
        }

        await _handlerRepository.UpdateAsync(handler, cancellationToken);
        return handler;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var handler = await _handlerRepository.GetByIdAsync(id, cancellationToken);
        if (handler == null)
        {
            throw new InvalidOperationException("處理人員不存在");
        }

        await _handlerRepository.DeleteAsync(handler, cancellationToken);
    }
}
