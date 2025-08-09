using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;

    public ProfileController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
                      ?? new UserProfile { UserId = userId, TargetDailyMinutes = 20 };

        var myEnrollments = await _dbContext.Enrollments.Include(e => e.Course).Where(e => e.UserId == userId).ToListAsync();
        var progresses = await _dbContext.UserCourseProgresses.Where(p => p.UserId == userId).ToListAsync();

        var recommended = await _dbContext.Courses.Where(c => c.Difficulty == profile.PreferredDifficulty).OrderBy(c => c.Title).Take(6).ToListAsync();

        var vm = new ProfileViewModel
        {
            Profile = profile,
            MyEnrollments = myEnrollments,
            ProgressByCourseId = progresses.ToDictionary(p => p.CourseId, p => p.CompletionPercent),
            RecommendedCourses = recommended
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UserProfile model)
    {
        var userId = _userManager.GetUserId(User)!;
        var existing = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing == null)
        {
            model.UserId = userId;
            _dbContext.UserProfiles.Add(model);
        }
        else
        {
            existing.TargetDailyMinutes = model.TargetDailyMinutes;
            existing.PreferredDifficulty = model.PreferredDifficulty;
        }
        await _dbContext.SaveChangesAsync();
        TempData["Saved"] = true;
        return RedirectToAction(nameof(Index));
    }
}


