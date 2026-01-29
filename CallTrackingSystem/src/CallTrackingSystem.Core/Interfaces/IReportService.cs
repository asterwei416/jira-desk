using CallTrackingSystem.Core.DTOs;

namespace CallTrackingSystem.Core.Interfaces;

/// <summary>
/// 報表生成服務介面
/// </summary>
public interface IReportService
{
    /// <summary>
    /// 生成 Excel 報表
    /// </summary>
    /// <param name="filter">篩選條件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>Excel 檔案位元組</returns>
    /// <exception cref="InvalidOperationException">資料超過 5000 筆限制時拋出</exception>
    Task<byte[]> GenerateExcelReportAsync(
        ExcelReportRequest filter,
        CancellationToken cancellationToken = default);
}
