using System.ComponentModel.DataAnnotations;

namespace FitBarbs.Web.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Beginner;

    public string? ThumbnailPath { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}


