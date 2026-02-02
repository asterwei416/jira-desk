using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 管理介面（MVC）
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    // 支援 Cookie 認證 (Web) 或 JWT (API)
    // 實際上因為 Program.cs 的 PolicyScheme 設定，這裡只需 Authorize 即可，
    // 但為了明確與相容性，我們先保持預設，讓 PolicyScheme 自動判斷。
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("InquirySystems")]
    public IActionResult InquirySystems()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Handlers()
    {
        return View();
    }

    [HttpGet]
    public IActionResult HandlerMappings()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Users()
    {
        return View();
    }
}
