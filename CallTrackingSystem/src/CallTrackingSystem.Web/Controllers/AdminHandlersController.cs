using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 處理人員管理 API
/// </summary>
[ApiController]
[Route("api/admin/handlers")]
[Authorize(Roles = "Admin")]
public class AdminHandlersController : ControllerBase
{
    private const int NameMaxLength = 50;
    private const int LineUserIdMaxLength = 100;
    private readonly IHandlerService _handlerService;

    public AdminHandlersController(IHandlerService handlerService)
    {
        _handlerService = handlerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HandlerResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken = default)
    {
        var handlers = await _handlerService.GetAllAsync(cancellationToken);
        if (isActive.HasValue)
        {
            handlers = handlers.Where(x => x.IsActive == isActive.Value).ToList();
        }

        return Ok(handlers.Select(MapToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HandlerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var handler = await _handlerService.GetByIdAsync(id, cancellationToken);
        if (handler == null)
        {
            return NotFound(new { error = "找不到指定處理人員" });
        }

        return Ok(MapToResponse(handler));
    }

    [HttpPost]
    [ProducesResponseType(typeof(HandlerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHandlerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateRequest(request.Name, request.LineUserId, out var error))
        {
            return BadRequest(new { error });
        }

        try
        {
            var handler = await _handlerService.CreateAsync(request.Name, request.LineUserId, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = handler.Id }, MapToResponse(handler));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HandlerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateHandlerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateRequest(request.Name, request.LineUserId, out var error))
        {
            return BadRequest(new { error });
        }

        try
        {
            var handler = await _handlerService.UpdateAsync(id, request.Name, request.LineUserId, request.IsActive, cancellationToken);
            return Ok(MapToResponse(handler));
        }
        catch (InvalidOperationException ex) when (ex.Message == "處理人員不存在")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _handlerService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "處理人員不存在")
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private static HandlerResponse MapToResponse(Handler handler)
    {
        return new HandlerResponse
        {
            Id = handler.Id,
            Name = handler.Name,
            LineUserId = handler.LineUserId,
            IsActive = handler.IsActive,
            CreatedAt = handler.CreatedAt
        };
    }

    private static bool ValidateRequest(string name, string? lineUserId, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "姓名不可為空";
            return false;
        }

        if (name.Length > NameMaxLength)
        {
            error = "姓名不可超過 50 字";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(lineUserId) && lineUserId.Length > LineUserIdMaxLength)
        {
            error = "LINE User ID 不可超過 100 字";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
