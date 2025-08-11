using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitBarbs.Web.Controllers;

[Authorize]
public partial class ProfileController : Controller
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
                      ?? new UserProfile { UserId = userId, TargetDailyMinutes = 20, Preferences = new UserPreferences() };

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
    public async Task<IActionResult> Index(ProfileViewModel vm)
    {
        var userId = _userManager.GetUserId(User)!;
        var existing = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (existing == null)
        {
            var model = vm.Profile ?? new UserProfile();
            model.UserId = userId;
            if (model.Preferences == null) model.Preferences = new UserPreferences();
            _dbContext.UserProfiles.Add(model);
        }
        else
        {
            var model = vm.Profile ?? new UserProfile();
            existing.TargetDailyMinutes = model.TargetDailyMinutes;
            existing.PreferredDifficulty = model.PreferredDifficulty;
            // Update nested preferences (idempotent)
            if (existing.Preferences == null)
            {
                existing.Preferences = new UserPreferences();
            }
            existing.Preferences.AutoCompleteLessonAfterWatch = model.Preferences?.AutoCompleteLessonAfterWatch ?? existing.Preferences.AutoCompleteLessonAfterWatch;
            existing.Preferences.PlayNextAutomatically = model.Preferences?.PlayNextAutomatically ?? existing.Preferences.PlayNextAutomatically;
            existing.Preferences.EmailProgressSummaries = model.Preferences?.EmailProgressSummaries ?? existing.Preferences.EmailProgressSummaries;
        }
        await _dbContext.SaveChangesAsync();
        TempData["Saved"] = true;
        return RedirectToAction(nameof(Index));
    }
}

// Dev-only endpoints for clearing user progress to simplify manual testing
[Authorize]
public partial class ProfileController
{
    [HttpPost]
    [Route("dev/clear-progress")]
    [AllowAnonymous]
    public async Task<IActionResult> DevClearProgress(string? email = null)
    {
        if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()) return Forbid();
        var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser? user = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await userManager.FindByEmailAsync(email);
        }
        else
        {
            if (!User.Identity?.IsAuthenticated ?? true) return BadRequest("Provide email when not authenticated");
            user = await userManager.GetUserAsync(User);
        }
        if (user == null) return NotFound("User not found");
        var db = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var progresses = await db.UserCourseProgresses.Where(p => p.UserId == user.Id).ToListAsync();
        db.UserCourseProgresses.RemoveRange(progresses);
        await db.SaveChangesAsync();
        return Ok(new { cleared = progresses.Count, user = user.Email });
    }
}


