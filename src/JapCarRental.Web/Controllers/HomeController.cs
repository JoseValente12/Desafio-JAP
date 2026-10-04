using JapCarRental.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace JapCarRental.Web.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;

    public HomeController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    // All the figures are calculated in DashboardService; the controller only shows them.
    public async Task<IActionResult> Index()
    {
        return View(await _dashboard.GetSummaryAsync());
    }
}