using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

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

    public class SaveThumbnailRequest
    {
        public string? DataUrl { get; set; }
        public string? ContentType { get; set; }
    }

    // Dev-only helper to mark a lesson as completed for a given user (by email)
    [HttpPost]
    [AllowAnonymous]
    [Route("dev/mark-complete/{id}")]
    public async Task<IActionResult> DevMarkComplete(int id, string? email = null)
    {
        if (!_env.IsDevelopment()) return Forbid();
        var lesson = await _dbContext.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();

        IdentityUser? user;
        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _userManager.FindByEmailAsync(email);
        }
        else
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return BadRequest("Provide email when not authenticated");
            }
            user = await _userManager.GetUserAsync(User);
        }
        if (user == null) return NotFound("User not found");

        var userId = user.Id;
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

        var lessons = await _dbContext.Lessons.Where(l => l.CourseId == lesson.CourseId).OrderBy(l => l.OrderIndex).ToListAsync();
        var nextLesson = lessons.SkipWhile(l => l.Id != id).Skip(1).FirstOrDefault();
        progress.CurrentLessonId = nextLesson?.Id;
        var completedCount = lessons.TakeWhile(l => l.Id != (nextLesson?.Id ?? 0)).Count();
        progress.CompletionPercent = (int)Math.Round((double)completedCount / Math.Max(lessons.Count, 1) * 100);
        await _dbContext.SaveChangesAsync();
        return Ok(new { user = user.Email, courseId = lesson.CourseId, currentLessonId = progress.CurrentLessonId, completion = progress.CompletionPercent });
    }

    private async Task<string?> TryGenerateVideoThumbnailAsync(string videoPhysicalPath)
    {
        try
        {
            var thumbnailsDir = Path.Combine(_env.WebRootPath, "uploads", "thumbnails");
            Directory.CreateDirectory(thumbnailsDir);
            var fileName = Path.GetFileNameWithoutExtension(videoPhysicalPath);
            var outputName = $"{fileName}-{Guid.NewGuid():N}.jpg";
            var thumbPhysicalPath = Path.Combine(thumbnailsDir, outputName);

            using var ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-y -ss 00:00:01 -i \"{videoPhysicalPath}\" -frames:v 1 -q:v 2 \"{thumbPhysicalPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            ffmpeg.Start();
            // Read streams to avoid deadlocks
            var _ = ffmpeg.StandardOutput.ReadToEndAsync();
            var __ = ffmpeg.StandardError.ReadToEndAsync();
            var exited = await Task.Run(() => ffmpeg.WaitForExit(20000)); // 20s timeout
            if (exited && ffmpeg.ExitCode == 0 && System.IO.File.Exists(thumbPhysicalPath))
            {
                return $"/uploads/thumbnails/{outputName}";
            }
        }
        catch
        {
            // ignored – fallback handled by caller
        }
        return null;
    }

    // Dev-only endpoint to rebuild missing thumbnails for existing lessons
    [HttpPost]
    [AllowAnonymous]
    [Route("dev/rebuild-thumbnails")]
    public async Task<IActionResult> RebuildThumbnailsDev()
    {
        if (!_env.IsDevelopment()) return Forbid();
        var lessons = await _dbContext.Lessons.ToListAsync();
        var updated = 0;
        foreach (var lesson in lessons)
        {
            var needs = string.IsNullOrWhiteSpace(lesson.ThumbnailPath) || lesson.ThumbnailPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
            if (needs && !string.IsNullOrWhiteSpace(lesson.VideoPath))
            {
                var physical = lesson.VideoPath.StartsWith("/")
                    ? Path.Combine(_env.WebRootPath, lesson.VideoPath.TrimStart('/'))
                    : Path.Combine(_env.WebRootPath, lesson.VideoPath);
                if (System.IO.File.Exists(physical))
                {
                    var thumb = await TryGenerateVideoThumbnailAsync(physical);
                    if (!string.IsNullOrWhiteSpace(thumb))
                    {
                        lesson.ThumbnailPath = thumb;
                        updated++;
                    }
                }
            }
        }
        await _dbContext.SaveChangesAsync();
        return Ok(new { updated, total = lessons.Count });
    }

    private static List<string> GenerateTipsForLesson(Lesson lesson)
    {
        // Simple tip bank by title keywords for beginner pilates
        var tips = new List<string>();
        var title = (lesson.Title ?? string.Empty).ToLowerInvariant();
        if (title.Contains("oddech"))
        {
            tips.Add("Oddychaj boczno-żebrowo: wdech rozszerza żebra na boki, wydech aktywuje mięśnie głębokie.");
            tips.Add("Utrzymuj neutralne ustawienie miednicy, unikaj przesadnego dociśnięcia odcinka lędźwiowego.");
            tips.Add("Ruch wykonuj płynnie, bez pośpiechu — jakość ponad ilość.");
        }
        else if (title.Contains("kręgosłup") || title.Contains("mobilizacja"))
        {
            tips.Add("Rozpocznij od małych zakresów, stopniowo je zwiększając.");
            tips.Add("Utrzymuj długi kark i aktywne łopatki, nie unosząc barków.");
            tips.Add("Jeśli czujesz dyskomfort — zmniejsz zakres lub przerwij ćwiczenie.");
        }
        else if (title.Contains("bioder") || title.Contains("stabilizacja"))
        {
            tips.Add("Skup się na ustawieniu kolan nad stopami i stabilnej miednicy.");
            tips.Add("Aktywuj pośladki na wydechu, unikaj zapadania kolan do środka.");
            tips.Add("Trzymaj żebra miękko opuszczone — nie wypychaj klatki piersiowej.");
        }
        else
        {
            tips.Add("Ćwicz w komfortowym zakresie ruchu, utrzymując płynny oddech.");
            tips.Add("Kontrola i precyzja są ważniejsze niż tempo.");
        }
        return tips;
    }

    [HttpGet]
    public async Task<IActionResult> Watch(int id, bool completed = false)
    {
        var lesson = await _dbContext.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();

        // Ensure thumbnail exists; try to generate on-demand if missing
        if (string.IsNullOrWhiteSpace(lesson.ThumbnailPath) && !string.IsNullOrWhiteSpace(lesson.VideoPath))
        {
            try
            {
                var physicalVideoPath = lesson.VideoPath.StartsWith("/")
                    ? Path.Combine(_env.WebRootPath, lesson.VideoPath.TrimStart('/'))
                    : Path.Combine(_env.WebRootPath, lesson.VideoPath);
                if (System.IO.File.Exists(physicalVideoPath))
                {
                    var thumb = await TryGenerateVideoThumbnailAsync(physicalVideoPath);
                    if (!string.IsNullOrWhiteSpace(thumb))
                    {
                        lesson.ThumbnailPath = thumb;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch { /* ignore thumbnail errors */ }
        }

        // Compute course progress and create view model with tips and progress bar info
        var lessons = await _dbContext.Lessons.Where(l => l.CourseId == lesson.CourseId).OrderBy(l => l.OrderIndex).ToListAsync();
        var index = lessons.FindIndex(l => l.Id == id);

        var userId = _userManager.GetUserId(User)!;
        var progress = await _dbContext.UserCourseProgresses
            .FirstOrDefaultAsync(p => p.CourseId == lesson.CourseId && p.UserId == userId);
        var completionPercent = progress?.CompletionPercent ?? 0;

        var tips = GenerateTipsForLesson(lesson);

        // Determine if current lesson is already completed for this user
        var isCompleted = false;
        if (progress != null)
        {
            if (!progress.CurrentLessonId.HasValue)
            {
                isCompleted = true; // entire course done
            }
            else
            {
                // Completed if this lesson comes before the current lesson pointer
                var currentIdx = lessons.FindIndex(l => l.Id == (progress.CurrentLessonId ?? 0));
                isCompleted = currentIdx > index; // any earlier lessons are done
            }
        }

        // Respect explicit completion flag from redirect to avoid any race or cache issues
        isCompleted = isCompleted || completed;

        var vm = new LessonWatchViewModel
        {
            Lesson = lesson,
            Tips = tips,
            CompletionPercent = completionPercent,
            CurrentLessonIndex = index + 1,
            TotalLessons = lessons.Count,
            IsCompleted = isCompleted
        };
        return View(vm);
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
        // After completion, stay on the same lesson to allow repeating; also pass a hint flag
        return RedirectToAction("Watch", new { id, completed = true });
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

        // Try to generate a thumbnail from the uploaded video (first second)
        var generatedThumb = await TryGenerateVideoThumbnailAsync(filePath);
        if (!string.IsNullOrWhiteSpace(generatedThumb))
        {
            model.ThumbnailPath = generatedThumb;
        }

        // Ensure order index
        var maxOrder = await _dbContext.Lessons.Where(l => l.CourseId == model.CourseId).MaxAsync(l => (int?)l.OrderIndex) ?? 0;
        model.OrderIndex = maxOrder + 1;

        _dbContext.Lessons.Add(model);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction("Details", "Courses", new { id = model.CourseId });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveThumbnail(int id, [FromBody] SaveThumbnailRequest payload)
    {
        var lesson = await _dbContext.Lessons.FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();
        if (payload?.DataUrl == null) return BadRequest();

        try
        {
            var commaIdx = payload.DataUrl.IndexOf(',');
            var base64 = commaIdx >= 0 ? payload.DataUrl[(commaIdx + 1)..] : payload.DataUrl;
            var bytes = Convert.FromBase64String(base64);
            var thumbnailsDir = Path.Combine(_env.WebRootPath, "uploads", "thumbnails");
            Directory.CreateDirectory(thumbnailsDir);
            var fileName = $"client-{Guid.NewGuid():N}.jpg";
            var path = Path.Combine(thumbnailsDir, fileName);
            await System.IO.File.WriteAllBytesAsync(path, bytes);
            lesson.ThumbnailPath = $"/uploads/thumbnails/{fileName}";
            await _dbContext.SaveChangesAsync();
            return Ok(new { lessonId = id, thumbnail = lesson.ThumbnailPath });
        }
        catch
        {
            return StatusCode(500);
        }
    }
}


