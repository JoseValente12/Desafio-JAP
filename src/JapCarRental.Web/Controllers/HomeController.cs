using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JapCarRental.Web.Models;

namespace JapCarRental.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

 
}
