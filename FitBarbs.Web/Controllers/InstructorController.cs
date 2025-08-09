using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Controllers;

[Authorize(Roles = ApplicationRoles.Instructor)]
public class InstructorController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public InstructorController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var courses = await _dbContext.Courses.Include(c => c.Lessons).ToListAsync();
        return View(courses);
    }
}


