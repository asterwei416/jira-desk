using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 使用者管理 API
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private const int NameMaxLength = 50;
    private const int UsernameMaxLength = 50;
    private readonly IUserManagementService _userManagementService;

    public AdminUsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken = default)
    {
        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!Enum.TryParse<UserRole>(role, true, out var parsedRole))
            {
                return BadRequest(new { error = "角色參數無效" });
            }
            roleFilter = parsedRole;
        }

        var users = await _userManagementService.GetAllAsync(roleFilter, isActive, cancellationToken);
        return Ok(users.Select(MapToResponse));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateCreateRequest(request, out var error))
        {
            return BadRequest(new { error });
        }

        try
        {
            var user = await _userManagementService.CreateAsync(
                request.Username,
                request.Password,
                request.Name,
                request.Role,
                cancellationToken);

            return CreatedAtAction(nameof(GetAll), new { id = user.Id }, MapToResponse(user));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "名稱不可為空" });
        }

        if (request.Name.Length > NameMaxLength)
        {
            return BadRequest(new { error = "名稱不可超過 50 字" });
        }

        try
        {
            var user = await _userManagementService.UpdateAsync(
                id,
                request.Name,
                request.Role,
                request.IsActive,
                cancellationToken);

            return Ok(MapToResponse(user));
        }
        catch (InvalidOperationException ex) when (ex.Message == "使用者不存在")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _userManagementService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "使用者不存在")
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        string id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { error = "新密碼不可為空" });
        }

        try
        {
            await _userManagementService.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
            return Ok(new { success = true, message = "密碼重設成功" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "使用者不存在")
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private static bool ValidateCreateRequest(CreateUserRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            error = "使用者名稱不可為空";
            return false;
        }

        if (request.Username.Length > UsernameMaxLength)
        {
            error = "使用者名稱不可超過 50 字";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "名稱不可為空";
            return false;
        }

        if (request.Name.Length > NameMaxLength)
        {
            error = "名稱不可超過 50 字";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            error = "密碼不可為空";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role,
            LineUserId = user.LineUserId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
