using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Controllers;

[Authorize]
public class EnrollmentsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;

    public EnrollmentsController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var userId = _userManager.GetUserId(User)!;
        var exists = await _dbContext.Enrollments.AnyAsync(e => e.CourseId == courseId && e.UserId == userId);
        if (!exists)
        {
            _dbContext.Enrollments.Add(new Enrollment { CourseId = courseId, UserId = userId });
            // initialize progress if not exists
            if (!await _dbContext.UserCourseProgresses.AnyAsync(p => p.CourseId == courseId && p.UserId == userId))
            {
                var firstLessonId = await _dbContext.Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.OrderIndex).Select(l => l.Id).FirstOrDefaultAsync();
                _dbContext.UserCourseProgresses.Add(new UserCourseProgress
                {
                    CourseId = courseId,
                    UserId = userId,
                    CurrentLessonId = firstLessonId == 0 ? null : firstLessonId,
                    CompletionPercent = 0
                });
            }
            await _dbContext.SaveChangesAsync();
        }
        return RedirectToAction("Details", "Courses", new { id = courseId });
    }
}


