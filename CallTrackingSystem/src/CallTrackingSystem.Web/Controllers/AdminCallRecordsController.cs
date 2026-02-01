using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 管理者專用來電紀錄 API
/// </summary>
[ApiController]
[Route("api/admin/call-records")]
[Produces("application/json")]
public class AdminCallRecordsController : ControllerBase
{
    private readonly IEditLockManager _editLockManager;
    private readonly ILogger<AdminCallRecordsController> _logger;

    public AdminCallRecordsController(
        IEditLockManager editLockManager,
        ILogger<AdminCallRecordsController> logger)
    {
        _editLockManager = editLockManager;
        _logger = logger;
    }

    /// <summary>
    /// 強制解鎖（僅 Admin）
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>鎖定狀態</returns>
    /// <response code="200">成功強制解鎖</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpPost("{id}/force-unlock")]
    [ProducesResponseType(typeof(CallRecordLockStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForceUnlock(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 驗證管理者權限（JWT / Role）
            var adminUserId = "admin-user-001";

            var result = await _editLockManager.ForceUnlockAsync(id, cancellationToken);

            _logger.LogInformation("管理者強制解鎖成功: {CallRecordId} by {AdminUserId}", id, adminUserId);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "強制解鎖時發生未預期錯誤: {CallRecordId}", id);
            return StatusCode(500, new { error = "強制解鎖時發生錯誤" });
        }
    }
}
