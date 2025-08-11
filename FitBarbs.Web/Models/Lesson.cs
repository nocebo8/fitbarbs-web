using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitBarbs.Web.Models;

public class Lesson
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public string VideoPath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ThumbnailPath { get; set; }

    public int OrderIndex { get; set; }

    [ForeignKey(nameof(Course))]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    // Optional set of concise tips specific to the lesson
    [NotMapped]
    public List<string> Tips { get; set; } = new();
}


