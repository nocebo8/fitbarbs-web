using FitBarbs.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

namespace FitBarbs.Web.Services;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        // Ensure roles
        if (!await roleManager.RoleExistsAsync(ApplicationRoles.Instructor))
            await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.Instructor));
        if (!await roleManager.RoleExistsAsync(ApplicationRoles.User))
            await roleManager.CreateAsync(new IdentityRole(ApplicationRoles.User));

        // Ensure Barbie account (instructor)
        var barbieEmail = "barbie@fitbarbs.app";
        var barbie = await userManager.Users.FirstOrDefaultAsync(u => u.Email == barbieEmail);
        if (barbie == null)
        {
            barbie = new IdentityUser
            {
                UserName = barbieEmail,
                Email = barbieEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(barbie, "Barbie!123");
            if (!result.Succeeded)
            {
                throw new Exception($"Failed to create seed user {barbieEmail}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // Ensure the existing user is confirmed and not locked out
            if (!barbie.EmailConfirmed)
            {
                barbie.EmailConfirmed = true;
                await userManager.UpdateAsync(barbie);
            }
#pragma warning disable CS0618
            // Clear potential lockout issues (IdentityUser has these props)
            barbie.LockoutEnd = null;
            barbie.AccessFailedCount = 0;
            await userManager.UpdateAsync(barbie);
#pragma warning restore CS0618

            // Reset password to known dev password to avoid invalid login loops
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(barbie);
            var resetResult = await userManager.ResetPasswordAsync(barbie, resetToken, "Barbie!1234!");
            if (!resetResult.Succeeded)
            {
                throw new Exception($"Failed to reset seed user password: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            }
            if (!await userManager.CheckPasswordAsync(barbie, "Barbie!1234!"))
            {
                throw new Exception("Password check failed after reset for barbie@fitbarbs.app");
            }
        }
        // Ensure the Barbie account is in the Instructor role even if it already existed
        if (!await userManager.IsInRoleAsync(barbie, ApplicationRoles.Instructor))
        {
            await userManager.AddToRoleAsync(barbie, ApplicationRoles.Instructor);
        }

        // Ensure beginner course exists and is populated with the exact 6 lessons and local video files specified
        var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

        var beginnerCourseTitle = "Pilates Start: Początkujący";
        var desiredCourseDescription = "Łagodny wstęp do pilates dla osób zaczynających przygodę z ruchem. Krótkie, lekkie wizualnie lekcje z naciskiem na oddech, mobilizację i stabilizację.";

        var courseEntity = await db.Courses.Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Title == beginnerCourseTitle);

        if (courseEntity == null)
        {
            courseEntity = new Models.Course
            {
                Title = beginnerCourseTitle,
                Description = desiredCourseDescription,
                Difficulty = Models.DifficultyLevel.Beginner,
                ThumbnailPath = "/img/hero_pilates.svg"
            };
            db.Courses.Add(courseEntity);
            await db.SaveChangesAsync();
        }
        else
        {
            // Keep course metadata in sync
            courseEntity.Description = desiredCourseDescription;
            courseEntity.Difficulty = Models.DifficultyLevel.Beginner;
            courseEntity.ThumbnailPath = courseEntity.ThumbnailPath ?? "/img/hero_pilates.svg";
        }

        // Desired lessons spec
        var desiredLessons = new[]
        {
            new { OrderIndex = 1, Title = "Lekcja 1: Oddech i aktywacja core", Description = "Nauka oddechu boczno‑żebrowego, neutralnej miednicy i delikatnej aktywacji mięśnia poprzecznego brzucha; spokojne tempo, nacisk na jakość ruchu.", FileName = "pilates-start-beginner-01-oddech-i-aktywacja-core.mp4" },
            new { OrderIndex = 2, Title = "Lekcja 2: Mobilizacja kręgosłupa", Description = "Segmentowa mobilizacja kręgosłupa (posterior/anterior tilt, cat-cow, roll-down); poprawa elastyczności odcinka piersiowego i lędźwiowego.", FileName = "pilates-start-beginner-02-mobilizacja-kregoslupa.mp4" },
            new { OrderIndex = 3, Title = "Lekcja 3: Stabilizacja bioder", Description = "Ćwiczenia stabilizujące miednicę i biodra (mosty, clamshell, odwodzenie nogi); spokojne przejścia, kontrola ustawienia kolan i stóp.", FileName = "pilates-start-beginner-03-stabilizacja-bioder.mp4" },
            new { OrderIndex = 4, Title = "Lekcja 4: Ustawienie łopatek i górnej części pleców", Description = "Aktywacja łopatki i mięśnia zębatego przedniego, wydłużenie kręgosłupa piersiowego; praca nad otwieraniem klatki bez unoszenia barków.", FileName = "pilates-start-beginner-04-ustawienie-lopatek.mp4" },
            new { OrderIndex = 5, Title = "Lekcja 5: Balans i kontrola", Description = "Proste sekwencje równoważne (stanie na jednej nodze, gentle hinge), stabilizacja środka i praca z oddechem dla utrzymania równowagi.", FileName = "pilates-start-beginner-05-balans-i-kontrola.mp4" },
            new { OrderIndex = 6, Title = "Lekcja 6: Delikatne rozciąganie całego ciała", Description = "Pełne, łagodne rozciąganie (tyły ud, biodra, klatka piersiowa) z oddechem; przyjemne wyciszenie i regeneracja.", FileName = "pilates-start-beginner-06-rozciaganie-calego-ciala.mp4" }
        };

        // Load current lessons for this course
        var existingLessons = await db.Lessons.Where(l => l.CourseId == courseEntity.Id).ToListAsync();

        // Helper: generate thumbnail via ffmpeg if possible
        static async Task<string?> GenerateThumbAsync(IWebHostEnvironment env, string videoPhysicalPath)
        {
            try
            {
                var thumbnailsDir = Path.Combine(env.WebRootPath, "uploads", "thumbnails");
                Directory.CreateDirectory(thumbnailsDir);
                var name = Path.GetFileNameWithoutExtension(videoPhysicalPath);
                var output = Path.Combine(thumbnailsDir, $"{name}-{Guid.NewGuid():N}.jpg");
                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-y -ss 00:00:01 -i \"{videoPhysicalPath}\" -frames:v 1 -q:v 2 \"{output}\"",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                var _ = p.StandardOutput.ReadToEndAsync();
                var __ = p.StandardError.ReadToEndAsync();
                var ok = await Task.Run(() => p.WaitForExit(20000));
                if (ok && p.ExitCode == 0 && System.IO.File.Exists(output))
                {
                    return $"/uploads/thumbnails/{Path.GetFileName(output)}";
                }
            }
            catch { }
            return null;
        }

        // Upsert lessons by OrderIndex
        foreach (var spec in desiredLessons)
        {
            var lesson = existingLessons.FirstOrDefault(l => l.OrderIndex == spec.OrderIndex);
            var videoPath = $"/uploads/videos/{spec.FileName}";
            var physicalVideoPath = Path.Combine(env.WebRootPath, "uploads", "videos", spec.FileName);
            if (lesson == null)
            {
                lesson = new Models.Lesson
                {
                    CourseId = courseEntity.Id,
                    OrderIndex = spec.OrderIndex,
                    Title = spec.Title,
                    Description = spec.Description,
                    VideoPath = videoPath,
                    ThumbnailPath = null
                };
                if (System.IO.File.Exists(physicalVideoPath))
                {
                    lesson.ThumbnailPath = await GenerateThumbAsync(env, physicalVideoPath) ?? lesson.ThumbnailPath;
                }
                db.Lessons.Add(lesson);
            }
            else
            {
                lesson.Title = spec.Title;
                lesson.Description = spec.Description;
                lesson.VideoPath = videoPath;
                // If current thumbnail is empty or an icon (svg), try replace with real screenshot
                var isIcon = string.IsNullOrWhiteSpace(lesson.ThumbnailPath) || lesson.ThumbnailPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
                if (isIcon && System.IO.File.Exists(physicalVideoPath))
                {
                    var thumb = await GenerateThumbAsync(env, physicalVideoPath);
                    if (!string.IsNullOrWhiteSpace(thumb)) lesson.ThumbnailPath = thumb;
                }
            }
        }

        // Remove any extra lessons not in desired set
        var desiredOrders = desiredLessons.Select(d => d.OrderIndex).ToHashSet();
        var toRemove = existingLessons.Where(l => !desiredOrders.Contains(l.OrderIndex)).ToList();
        if (toRemove.Count > 0)
        {
            db.Lessons.RemoveRange(toRemove);
        }

        await db.SaveChangesAsync();
    }
}


