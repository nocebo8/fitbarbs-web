using System.ComponentModel.DataAnnotations;

namespace FitBarbs.Web.Models;

public class UserProfile
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Range(5, 180)]
    public int TargetDailyMinutes { get; set; } = 20;

    public DifficultyLevel PreferredDifficulty { get; set; } = DifficultyLevel.Beginner;

    public UserPreferences Preferences { get; set; } = new();
}


