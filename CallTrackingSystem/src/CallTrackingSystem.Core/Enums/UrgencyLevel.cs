namespace CallTrackingSystem.Core.Enums;

/// <summary>
/// 緊急程度枚舉
/// </summary>
public enum UrgencyLevel
{
    /// <summary>
    /// 低（一般諮詢）
    /// </summary>
    Low = 0,
    
    /// <summary>
    /// 中（影響使用）
    /// </summary>
    Medium = 1,
    
    /// <summary>
    /// 高（系統無法使用）
    /// </summary>
    High = 2
}
