using CallTrackingSystem.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CallTrackingSystem.Web.Controllers;

/// <summary>
/// 來電紀錄 MVC 頁面
/// </summary>
public class CallRecordController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new SearchFilterModel());
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        ViewData["CallRecordId"] = id;
        return View();
    }
}
