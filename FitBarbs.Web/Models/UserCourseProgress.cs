using System.ComponentModel.DataAnnotations;

namespace FitBarbs.Web.Models;

public class UserCourseProgress
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public int? CurrentLessonId { get; set; }
    public Lesson? CurrentLesson { get; set; }

    // 0..100
    public int CompletionPercent { get; set; }

    // Allows user to choose comfortable pace (minutes per day)
    public int TargetDailyMinutes { get; set; } = 20;
}


