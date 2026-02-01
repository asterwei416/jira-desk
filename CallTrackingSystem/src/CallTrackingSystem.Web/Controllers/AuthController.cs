using System.Security.Claims;
using CallTrackingSystem.Core.DTOs;
using CallTrackingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 認證 API
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 系統內建帳號登入
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message is "帳號或密碼錯誤" or "帳號已停用")
            {
                return Unauthorized(new { error = ex.Message });
            }

            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登入時發生未預期的錯誤");
            return StatusCode(500, new { error = "登入時發生錯誤" });
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        return NoContent();
    }

    /// <summary>
    /// 取得個人資料
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "尚未登入" });
        }
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return NotFound(new { error = "使用者不存在" });
        }

        return Ok(new UserProfile
        {
            Id = user.Id,
            Username = user.Username,
            Name = user.Name,
            Role = user.Role,
            LineUserId = user.LineUserId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// 更新個人資料
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { error = "尚未登入" });
            }
            var user = await _userService.UpdateAsync(userId, request.Name, cancellationToken);

            return Ok(new UserProfile
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Role = user.Role,
                LineUserId = user.LineUserId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 變更密碼
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { error = "尚未登入" });
            }
            await _authService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
