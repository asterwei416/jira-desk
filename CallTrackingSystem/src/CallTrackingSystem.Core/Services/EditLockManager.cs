using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;

namespace CallTrackingSystem.Core.Services;

/// <summary>
/// 編輯鎖定管理服務
/// </summary>
public class EditLockManager : IEditLockManager
{
    private const int DefaultLockTimeoutMinutes = 30;
    private readonly ICallRecordRepository _callRecordRepository;

    public EditLockManager(ICallRecordRepository callRecordRepository)
    {
        _callRecordRepository = callRecordRepository;
    }

    /// <summary>
    /// 取得編輯鎖定
    /// </summary>
    public async Task<CallRecordLockResponse> AcquireLockAsync(
        int recordId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(recordId, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        var acquired = callRecord.TryAcquireLock(userId, DefaultLockTimeoutMinutes);
        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        var isLocked = IsLocked(callRecord, DefaultLockTimeoutMinutes);

        return new CallRecordLockResponse
        {
            Acquired = acquired,
            IsLocked = isLocked,
            LockedByUserId = isLocked ? callRecord.LockedByUserId : null,
            LockedAt = isLocked ? callRecord.LockedAt : null
        };
    }

    /// <summary>
    /// 釋放編輯鎖定
    /// </summary>
    public async Task<CallRecordLockStatusResponse> ReleaseLockAsync(
        int recordId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(recordId, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        callRecord.ReleaseLock(userId);
        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        var isLocked = IsLocked(callRecord, DefaultLockTimeoutMinutes);

        return new CallRecordLockStatusResponse
        {
            IsLocked = isLocked,
            LockedByUserId = isLocked ? callRecord.LockedByUserId : null,
            LockedAt = isLocked ? callRecord.LockedAt : null
        };
    }

    /// <summary>
    /// 強制解鎖（僅管理者）
    /// </summary>
    public async Task<CallRecordLockStatusResponse> ForceUnlockAsync(
        int recordId,
        CancellationToken cancellationToken = default)
    {
        var callRecord = await _callRecordRepository.GetByIdAsync(recordId, cancellationToken);
        if (callRecord == null)
        {
            throw new InvalidOperationException("來電紀錄不存在");
        }

        callRecord.LockedByUserId = null;
        callRecord.LockedAt = null;
        await _callRecordRepository.UpdateAsync(callRecord, cancellationToken);

        return new CallRecordLockStatusResponse
        {
            IsLocked = false,
            LockedByUserId = null,
            LockedAt = null
        };
    }

    private static bool IsLocked(CallRecord callRecord, int lockTimeoutMinutes)
    {
        if (callRecord.LockedByUserId == null || !callRecord.LockedAt.HasValue)
        {
            return false;
        }

        return DateTime.UtcNow <= callRecord.LockedAt.Value.AddMinutes(lockTimeoutMinutes);
    }
}
