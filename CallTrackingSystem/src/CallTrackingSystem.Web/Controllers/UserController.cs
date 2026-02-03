using CallTrackingSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 使用者個人設定 Controller
/// </summary>
[Authorize]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// 個人設定頁面
    /// </summary>
    [HttpGet("/user/settings")]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value ?? "0";
        var user = await _userService.GetByIdAsync(userId, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    /// <summary>
    /// 綁定 LINE 帳號（從 OAuth Callback 呼叫）
    /// </summary>
    [HttpPost("/user/line-binding")]
    public async Task<IActionResult> BindLineAccount(
        [FromForm] string lineUserId,
        [FromForm] string displayName,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value ?? "0";

        try
        {
            await _userService.BindLineAccountAsync(userId, lineUserId, cancellationToken);
            TempData["SuccessMessage"] = "LINE 帳號綁定成功";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "LINE 綁定失敗: UserId={UserId}, LineUserId={LineUserId}", userId, lineUserId);
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Settings));
    }

    /// <summary>
    /// 解除 LINE 帳號綁定
    /// </summary>
    [HttpPost("/user/line-binding/remove")]
    public async Task<IActionResult> UnbindLineAccount(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirst("sub")?.Value ?? "0";

        try
        {
            await _userService.UnbindLineAccountAsync(userId, cancellationToken);
            TempData["SuccessMessage"] = "LINE 帳號已解除綁定";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解除 LINE 綁定失敗: UserId={UserId}", userId);
            TempData["ErrorMessage"] = "解除綁定失敗，請稍後再試";
        }

        return RedirectToAction(nameof(Settings));
    }
}
