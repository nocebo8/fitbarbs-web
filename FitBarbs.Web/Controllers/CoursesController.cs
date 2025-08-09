using FitBarbs.Web.Data;
using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

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

    [HttpGet]
    public async Task<IActionResult> Index(DifficultyLevel? level)
    {
        var query = _dbContext.Courses.AsQueryable();
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


