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
    private readonly ILineLoginService _lineLoginService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        ILineLoginService lineLoginService,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _lineLoginService = lineLoginService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
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
    /// 啟動 LINE Login 流程
    /// </summary>
    [HttpGet("line/login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult LineLogin()
    {
        var state = Guid.NewGuid().ToString("N");
        Response.Cookies.Append("line_login_state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddMinutes(5)
        });

        var redirectUri = _configuration["Line:Login:CallbackUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new { error = "LINE Login CallbackUrl 未設定" });
        }

        var loginUrl = _lineLoginService.BuildLoginUrl(state, redirectUri);
        return Redirect(loginUrl);
    }

    /// <summary>
    /// LINE Login 回調
    /// </summary>
    [HttpGet("line/callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LineCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken = default)
    {
        var expectedState = Request.Cookies["line_login_state"];
        if (string.IsNullOrWhiteSpace(expectedState) || !string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            return BadRequest(new { error = "LINE Login 驗證失敗" });
        }

        var redirectUri = _configuration["Line:Login:CallbackUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            return BadRequest(new { error = "LINE Login CallbackUrl 未設定" });
        }

        try
        {
            var profile = await _lineLoginService.GetUserProfileAsync(code, redirectUri, cancellationToken);
            var user = await _userService.GetByLineUserIdAsync(profile.LineUserId, cancellationToken);
            var frontendBaseUrl = _configuration["Line:Login:FrontendBaseUrl"];
            var baseUrl = string.IsNullOrWhiteSpace(frontendBaseUrl)
                ? $"{Request.Scheme}://{Request.Host}"
                : frontendBaseUrl.TrimEnd('/');

            if (user == null)
            {
                var bindUrl = $"{baseUrl}/auth/bind?lineUserId={Uri.EscapeDataString(profile.LineUserId)}&displayName={Uri.EscapeDataString(profile.DisplayName)}";
                return Redirect(bindUrl);
            }

            var token = _jwtTokenService.GenerateToken(user);
            var redirectUrl = $"{baseUrl}/?token={Uri.EscapeDataString(token)}";
            return Redirect(redirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "LINE Login 回調處理失敗");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 綁定 LINE 帳號（管理者）
    /// </summary>
    [HttpPost("line/bind")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BindLineAccount(
        [FromBody] BindLineAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userService.BindLineAccountAsync(request.UserId, request.LineUserId, cancellationToken);
            return Ok(new
            {
                success = true,
                message = "LINE 帳號綁定成功",
                user = new UserProfile
                {
                    Id = user.Id,
                    Username = user.Username,
                    Name = user.Name,
                    Role = user.Role,
                    LineUserId = user.LineUserId,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 解除 LINE 帳號綁定（管理者）
    /// </summary>
    [HttpPost("line/unbind")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnbindLineAccount(
        [FromBody] UnbindLineAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userService.UnbindLineAccountAsync(request.UserId, cancellationToken);
            return Ok(new { success = true, message = "LINE 帳號綁定已解除" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "使用者不存在")
        {
            return NotFound(new { error = ex.Message });
        }
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
