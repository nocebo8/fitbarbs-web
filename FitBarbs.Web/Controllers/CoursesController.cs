using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace FitBarbs.Web.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;

    public CoursesController(ApplicationDbContext dbContext, IWebHostEnvironment env)
    {
        _dbContext = dbContext;
        _env = env;
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
            var _ = ffmpeg.StandardOutput.ReadToEndAsync();
            var __ = ffmpeg.StandardError.ReadToEndAsync();
            var exited = await Task.Run(() => ffmpeg.WaitForExit(20000));
            if (exited && ffmpeg.ExitCode == 0 && System.IO.File.Exists(thumbPhysicalPath))
            {
                return $"/uploads/thumbnails/{outputName}";
            }
        }
        catch { }
        return null;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DifficultyLevel? level)
    {
        var query = _dbContext.Courses.Include(c => c.Lessons).AsQueryable();
        if (level.HasValue)
        {
            query = query.Where(c => c.Difficulty == level.Value);
        }
        var courses = await query.OrderBy(c => c.Difficulty).ThenBy(c => c.Title).ToListAsync();
        return View(courses);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _dbContext.Courses
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();

        // Ensure thumbnails exist for lessons so list can show real screenshots
        foreach (var lesson in course.Lessons)
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
                    }
                }
            }
        }
        await _dbContext.SaveChangesAsync();
        return View(course);
    }

    [Authorize(Roles = ApplicationRoles.Instructor)]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Course());
    }

    [Authorize(Roles = ApplicationRoles.Instructor)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course model, IFormFile? thumbnail)
    {
        if (!ModelState.IsValid) return View(model);
        if (thumbnail != null && thumbnail.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(thumbnail.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("ThumbnailPath", "Nieobsługiwany typ pliku miniatury.");
                return View(model);
            }
            if (thumbnail.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("ThumbnailPath", "Plik miniatury jest zbyt duży (max 5MB).");
                return View(model);
            }
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "thumbs");
            Directory.CreateDirectory(uploadsDir);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            await using (var stream = System.IO.File.Create(filePath))
            {
                await thumbnail.CopyToAsync(stream);
            }
            model.ThumbnailPath = $"/uploads/thumbs/{fileName}";
        }
        _dbContext.Courses.Add(model);
        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }
}


