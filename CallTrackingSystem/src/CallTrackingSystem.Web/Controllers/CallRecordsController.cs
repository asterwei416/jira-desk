using Microsoft.AspNetCore.Mvc;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Web.Models;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 來電紀錄管理 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CallRecordsController : ControllerBase
{
    private readonly CallRecordService _callRecordService;
    private readonly ILogger<CallRecordsController> _logger;

    public CallRecordsController(
        CallRecordService callRecordService,
        ILogger<CallRecordsController> logger)
    {
        _callRecordService = callRecordService;
        _logger = logger;
    }

    /// <summary>
    /// 建立新的來電紀錄
    /// </summary>
    /// <param name="request">來電紀錄資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>建立的來電紀錄</returns>
    /// <response code="201">成功建立來電紀錄</response>
    /// <response code="400">請求資料無效</response>
    /// <response code="500">伺服器內部錯誤</response>
    [HttpPost]
    [ProducesResponseType(typeof(CallRecordResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateCallRecord(
        [FromBody] CreateCallRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 從 JWT Token 取得使用者 ID，目前使用測試值
            var userId = "test-user-001";

            var result = await _callRecordService.CreateAsync(request, userId, cancellationToken);

            _logger.LogInformation("來電紀錄已建立: {CallRecordId} by User: {UserId}", result.Id, userId);

            return CreatedAtAction(
                nameof(GetCallRecord),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "建立來電紀錄失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立來電紀錄時發生未預期的錯誤");
            return StatusCode(500, new { error = "建立來電紀錄時發生錯誤" });
        }
    }

    /// <summary>
    /// 取得指定的來電紀錄詳細資訊
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>來電紀錄詳細資訊</returns>
    /// <response code="200">成功取得來電紀錄</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CallRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCallRecord(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await _callRecordService.GetByIdAsync(id, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = $"找不到 ID 為 {id} 的來電紀錄" });
        }

        return Ok(result);
    }

    /// <summary>
    /// 取得來電紀錄清單（分頁）
    /// </summary>
    /// <param name="filter">搜尋條件</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分頁的來電紀錄清單</returns>
    /// <response code="200">成功取得來電紀錄清單</response>
    /// <response code="400">請求參數無效</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CallRecordListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCallRecords(
        [FromQuery] SearchFilterModel filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new CallRecordSearchRequest
            {
                Keyword = filter.Keyword,
                InquirySystemId = filter.InquirySystemId,
                Status = filter.Status,
                UrgencyLevel = filter.UrgencyLevel,
                StartDate = filter.StartDate,
                EndDate = filter.EndDate,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                SortBy = filter.SortBy,
                SortOrder = filter.SortOrder
            };

            var result = await _callRecordService.GetPagedAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新來電紀錄
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="request">更新的資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>更新後的來電紀錄</returns>
    /// <response code="200">成功更新來電紀錄</response>
    /// <response code="400">請求資料無效</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    /// <response code="409">紀錄正被其他使用者編輯中</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CallRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCallRecord(
        [FromRoute] int id,
        [FromBody] UpdateCallRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 從 JWT Token 取得使用者 ID，目前使用測試值
            var userId = "test-user-001";

            var result = await _callRecordService.UpdateAsync(id, request, userId, cancellationToken);

            _logger.LogInformation("來電紀錄已更新: {CallRecordId} by User: {UserId}", id, userId);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("不存在"))
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("編輯中"))
        {
            _logger.LogWarning(ex, "來電紀錄被鎖定: {CallRecordId}", id);
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新來電紀錄時發生未預期的錯誤: {CallRecordId}", id);
            return StatusCode(500, new { error = "更新來電紀錄時發生錯誤" });
        }
    }

    /// <summary>
    /// 取得編輯鎖定狀態
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>鎖定狀態</returns>
    /// <response code="200">成功取得鎖定狀態</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpGet("{id}/lock")]
    [ProducesResponseType(typeof(CallRecordLockStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLockStatus(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _callRecordService.GetLockStatusAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取得編輯鎖定
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>鎖定結果</returns>
    /// <response code="200">成功取得鎖定或已由自己鎖定</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    /// <response code="409">紀錄正被其他使用者編輯中</response>
    [HttpPost("{id}/lock")]
    [ProducesResponseType(typeof(CallRecordLockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcquireLock(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 從 JWT Token 取得使用者 ID，目前使用測試值
            var userId = "test-user-001";

            var result = await _callRecordService.AcquireLockAsync(id, userId, cancellationToken);

            if (!result.Acquired && result.IsLocked)
            {
                return Conflict(new { error = $"此紀錄正被其他使用者編輯中（鎖定者: {result.LockedByUserId}）" });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 釋放編輯鎖定
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>鎖定狀態</returns>
    /// <response code="200">成功釋放鎖定</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpDelete("{id}/lock")]
    [ProducesResponseType(typeof(CallRecordLockStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReleaseLock(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 從 JWT Token 取得使用者 ID，目前使用測試值
            var userId = "test-user-001";

            var result = await _callRecordService.ReleaseLockAsync(id, userId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 刪除來電紀錄
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>無內容</returns>
    /// <response code="204">成功刪除來電紀錄</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCallRecord(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _callRecordService.DeleteAsync(id, cancellationToken);

            _logger.LogInformation("來電紀錄已刪除: {CallRecordId}", id);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刪除來電紀錄時發生未預期的錯誤: {CallRecordId}", id);
            return StatusCode(500, new { error = "刪除來電紀錄時發生錯誤" });
        }
    }

    /// <summary>
    /// 更新處理狀態
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="request">狀態更新資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>更新後的來電紀錄</returns>
    /// <response code="200">成功更新狀態</response>
    /// <response code="400">狀態值無效</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(CallRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 從 JWT Token 取得使用者 ID，目前使用測試值
            var userId = "test-user-001";

            if (!Enum.TryParse<ProcessStatus>(request.Status, true, out var status))
            {
                return BadRequest(new { error = "狀態值無效" });
            }

            var result = await _callRecordService.UpdateStatusAsync(id, status, userId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 查詢變更歷史
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>變更歷史清單</returns>
    /// <response code="200">查詢成功</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpGet("{id}/change-history")]
    [ProducesResponseType(typeof(ChangeHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChangeHistory(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _callRecordService.GetChangeHistoryAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 查詢通知記錄
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>通知記錄清單</returns>
    /// <response code="200">查詢成功</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpGet("{id}/notifications")]
    [ProducesResponseType(typeof(List<NotificationLogItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotifications(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _callRecordService.GetNotificationLogsAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 更新處理人員
    /// </summary>
    /// <param name="id">來電紀錄 ID</param>
    /// <param name="request">處理人員更新資料</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>更新後的來電紀錄</returns>
    /// <response code="200">更新成功</response>
    /// <response code="400">請求資料無效</response>
    /// <response code="404">找不到指定的來電紀錄</response>
    [HttpPut("{id}/handlers")]
    [ProducesResponseType(typeof(CallRecordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHandlers(
        [FromRoute] int id,
        [FromBody] UpdateHandlersRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: 從 JWT Token 取得使用者 ID，目前使用測試值
            var userId = "test-user-001";

            var result = await _callRecordService.UpdateHandlersAsync(
                id,
                request.HandlerIds,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("來電紀錄"))
        {
            _logger.LogWarning(ex, "找不到來電紀錄: {CallRecordId}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "更新處理人員失敗: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// 更新狀態請求 DTO
/// </summary>
public record UpdateStatusRequest
{
    public required string Status { get; init; }
}
