using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Controllers;

[Authorize]
public class LessonsController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public LessonsController(ApplicationDbContext dbContext, UserManager<IdentityUser> userManager, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Watch(int id)
    {
        var lesson = await _dbContext.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();
        return View(lesson);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var lesson = await _dbContext.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var progress = await _dbContext.UserCourseProgresses
            .FirstOrDefaultAsync(p => p.CourseId == lesson.CourseId && p.UserId == userId);
        if (progress == null)
        {
            progress = new UserCourseProgress
            {
                CourseId = lesson.CourseId,
                UserId = userId,
                CurrentLessonId = id,
                CompletionPercent = 0
            };
            _dbContext.UserCourseProgresses.Add(progress);
        }

        // Compute completion based on lessons count
        var lessons = await _dbContext.Lessons.Where(l => l.CourseId == lesson.CourseId).OrderBy(l => l.OrderIndex).ToListAsync();
        var nextLesson = lessons.SkipWhile(l => l.Id != id).Skip(1).FirstOrDefault();
        progress.CurrentLessonId = nextLesson?.Id;
        var completedCount = lessons.TakeWhile(l => l.Id != (nextLesson?.Id ?? 0)).Count();
        progress.CompletionPercent = (int)Math.Round((double)completedCount / Math.Max(lessons.Count, 1) * 100);

        await _dbContext.SaveChangesAsync();
        return RedirectToAction("Details", "Courses", new { id = lesson.CourseId });
    }

    [Authorize(Roles = ApplicationRoles.Instructor)]
    [HttpGet]
    public IActionResult Create(int courseId)
    {
        return View(new Lesson { CourseId = courseId });
    }

    [Authorize(Roles = ApplicationRoles.Instructor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lesson model, IFormFile? video)
    {
        // VideoPath is set server-side, so remove it from ModelState to avoid [Required] blocking validation
        ModelState.Remove(nameof(Lesson.VideoPath));

        if (video == null || video.Length == 0)
        {
            ModelState.AddModelError("VideoPath", "Wymagany jest plik wideo.");
        }

        if (!ModelState.IsValid) return View(model);

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "videos");
        Directory.CreateDirectory(uploadsDir);
        var ext = Path.GetExtension(video!.FileName).ToLowerInvariant();
        var allowed = new[] { ".mp4", ".mov", ".webm", ".mkv" };
        if (!allowed.Contains(ext))
        {
            ModelState.AddModelError("VideoPath", "Nieobsługiwany typ pliku wideo.");
            return View(model);
        }
        if (video.Length > 2L * 1024 * 1024 * 1024) // 2GB
        {
            ModelState.AddModelError("VideoPath", "Plik wideo jest zbyt duży (max 2GB).");
            return View(model);
        }
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        await using (var stream = System.IO.File.Create(filePath))
        {
            await video.CopyToAsync(stream);
        }
        model.VideoPath = $"/uploads/videos/{fileName}";

        // Ensure order index
        var maxOrder = await _dbContext.Lessons.Where(l => l.CourseId == model.CourseId).MaxAsync(l => (int?)l.OrderIndex) ?? 0;
        model.OrderIndex = maxOrder + 1;

        _dbContext.Lessons.Add(model);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction("Details", "Courses", new { id = model.CourseId });
    }
}


