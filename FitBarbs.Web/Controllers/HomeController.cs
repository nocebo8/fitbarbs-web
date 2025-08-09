using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FitBarbs.Web.Models;
using FitBarbs.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public IActionResult Index()
    {
        var featured = _db.Courses.Include(c => c.Lessons)
            .OrderBy(c => c.Difficulty).ThenBy(c => c.Title).Take(6).ToList();
        var vm = new HomeViewModel { FeaturedCourses = featured };
        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet("/Pricing")]
    public IActionResult Pricing()
    {
        return View();
    }

    [HttpGet("/Oferta")]
    public IActionResult Oferta()
    {
        return View();
    }

    [HttpGet("/Blog")]
    public IActionResult Blog()
    {
        return View();
    }

    public IActionResult ONas()
    {
        return View();
    }

    public IActionResult Kontakt()
    {
        return View();
    }

    [HttpGet("/Regulamin")]
    public IActionResult Regulamin()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
