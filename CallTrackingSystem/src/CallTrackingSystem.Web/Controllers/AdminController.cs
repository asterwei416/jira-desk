using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 管理介面（MVC）
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    [HttpGet]
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
