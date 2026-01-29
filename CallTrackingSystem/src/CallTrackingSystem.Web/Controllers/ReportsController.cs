using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 報表匯出 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly CallRecordService _callRecordService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        CallRecordService callRecordService,
        ILogger<ReportsController> logger)
    {
        _callRecordService = callRecordService;
        _logger = logger;
    }

    /// <summary>
    /// 匯出 Excel 報表
    /// </summary>
    /// <param name="request">匯出條件</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Excel 檔案</returns>
    /// <response code="200">匯出成功</response>
    /// <response code="400">參數錯誤或超出匯出限制</response>
    [HttpPost("excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportExcel(
        [FromBody] ExcelReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var content = await _callRecordService.ExportExcelAsync(request, cancellationToken);
            var fileName = $"來電紀錄_{DateTime.Now:yyyy-MM}.xlsx";

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "報表匯出失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message, code = "EXPORT_LIMIT_EXCEEDED" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "報表匯出時發生未預期錯誤");
            return StatusCode(500, new { error = "報表匯出時發生錯誤" });
        }
    }
}
