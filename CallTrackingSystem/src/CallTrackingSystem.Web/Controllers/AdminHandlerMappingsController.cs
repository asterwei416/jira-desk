using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 處理人員對應管理 API
/// </summary>
[ApiController]
[Route("api/admin/handler-mappings")]
[Authorize(Roles = "Admin")]
public class AdminHandlerMappingsController : ControllerBase
{
    private readonly IHandlerMappingService _handlerMappingService;

    public AdminHandlerMappingsController(IHandlerMappingService handlerMappingService)
    {
        _handlerMappingService = handlerMappingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HandlerMappingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? inquirySystemId,
        [FromQuery] int? handlerId,
        CancellationToken cancellationToken = default)
    {
        var mappings = await _handlerMappingService.GetMappingsAsync(inquirySystemId, handlerId, cancellationToken);
        return Ok(mappings.Select(MapToResponse));
    }

    [HttpPost]
    [ProducesResponseType(typeof(HandlerMappingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHandlerMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.HandlerId <= 0 || request.InquirySystemId <= 0)
        {
            return BadRequest(new { error = "處理人員與詢問系統為必填" });
        }

        try
        {
            var mapping = await _handlerMappingService.CreateMappingAsync(request.HandlerId, request.InquirySystemId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, MapToResponse(mapping));
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
            await _handlerMappingService.DeleteMappingAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "找不到指定對應關係")
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private static HandlerMappingResponse MapToResponse(HandlerMapping mapping)
    {
        return new HandlerMappingResponse
        {
            Id = mapping.Id,
            CreatedAt = mapping.CreatedAt,
            Handler = new HandlerResponse
            {
                Id = mapping.Handler?.Id ?? mapping.HandlerId,
                Name = mapping.Handler?.Name ?? string.Empty,
                LineUserId = mapping.Handler?.LineUserId,
                IsActive = mapping.Handler?.IsActive ?? false,
                CreatedAt = mapping.Handler?.CreatedAt ?? default
            },
            InquirySystem = new InquirySystemResponse
            {
                Id = mapping.InquirySystem?.Id ?? mapping.InquirySystemId,
                Name = mapping.InquirySystem?.Name ?? string.Empty,
                IsActive = mapping.InquirySystem?.IsActive ?? false,
                CreatedAt = mapping.InquirySystem?.CreatedAt ?? default
            }
        };
    }
}
