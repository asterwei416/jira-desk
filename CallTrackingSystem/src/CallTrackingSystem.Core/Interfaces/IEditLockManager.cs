using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 編輯鎖定管理服務介面
/// </summary>
public interface IEditLockManager
{
    /// <summary>
    /// 取得編輯鎖定
    /// </summary>
    Task<CallRecordLockResponse> AcquireLockAsync(
        int recordId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 釋放編輯鎖定
    /// </summary>
    Task<CallRecordLockStatusResponse> ReleaseLockAsync(
        int recordId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 強制解鎖（僅管理者）
    /// </summary>
    Task<CallRecordLockStatusResponse> ForceUnlockAsync(
        int recordId,
        CancellationToken cancellationToken = default);
}
