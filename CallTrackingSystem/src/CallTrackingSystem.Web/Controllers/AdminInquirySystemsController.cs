using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 詢問系統管理 API
/// </summary>
[ApiController]
[Route("api/admin/inquiry-systems")]
[Authorize(Roles = "Admin")]
public class AdminInquirySystemsController : ControllerBase
{
    private const int NameMaxLength = 100;
    private readonly IInquirySystemService _inquirySystemService;

    public AdminInquirySystemsController(IInquirySystemService inquirySystemService)
    {
        _inquirySystemService = inquirySystemService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InquirySystemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, CancellationToken cancellationToken = default)
    {
        var systems = await _inquirySystemService.GetAllAsync(cancellationToken);
        if (isActive.HasValue)
        {
            systems = systems.Where(x => x.IsActive == isActive.Value).ToList();
        }

        return Ok(systems.Select(MapToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InquirySystemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
    {
        var system = await _inquirySystemService.GetByIdAsync(id, cancellationToken);
        if (system == null)
        {
            return NotFound(new { error = "找不到指定系統" });
        }

        return Ok(MapToResponse(system));
    }

    [HttpPost]
    [ProducesResponseType(typeof(InquirySystemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInquirySystemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidName(request.Name, out var error))
        {
            return BadRequest(new { error });
        }

        try
        {
            var system = await _inquirySystemService.CreateAsync(request.Name, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = system.Id }, MapToResponse(system));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InquirySystemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateInquirySystemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidName(request.Name, out var error))
        {
            return BadRequest(new { error });
        }

        try
        {
            var system = await _inquirySystemService.UpdateAsync(id, request.Name, request.IsActive, cancellationToken);
            return Ok(MapToResponse(system));
        }
        catch (InvalidOperationException ex) when (ex.Message == "詢問系統不存在")
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _inquirySystemService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "詢問系統不存在")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static InquirySystemResponse MapToResponse(InquirySystem system)
    {
        return new InquirySystemResponse
        {
            Id = system.Id,
            Name = system.Name,
            IsActive = system.IsActive,
            CreatedAt = system.CreatedAt
        };
    }

    private static bool IsValidName(string name, out string error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "系統名稱不可為空";
            return false;
        }

        if (name.Length > NameMaxLength)
        {
            error = "系統名稱不可超過 100 字";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
