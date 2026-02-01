using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 認證頁面（MVC）
/// </summary>
[Route("Auth")]
public class AuthViewController : Controller
{
    [HttpGet("Login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet("Bind")]
    public IActionResult Bind(string lineUserId, string displayName)
    {
        var model = new BindLineAccountViewModel
        {
            LineUserId = lineUserId,
            DisplayName = displayName
        };

        return View(model);
    }

    [HttpGet("Profile")]
    public IActionResult Profile()
    {
        return View();
    }
}
