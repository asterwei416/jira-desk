namespace CallTrackingSystem.Core.Enums;

/// <summary>
/// 處理狀態枚舉
/// </summary>
public enum ProcessStatus
{
    /// <summary>
    /// 待處理
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// 處理中
    /// </summary>
    InProgress = 1,
    
    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// 已關閉
    /// </summary>
    Closed = 3
}
