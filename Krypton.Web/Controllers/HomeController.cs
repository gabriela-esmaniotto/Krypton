using System.Diagnostics;
using Krypton.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Krypton.Web.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
