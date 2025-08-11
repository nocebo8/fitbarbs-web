using Microsoft.EntityFrameworkCore;

namespace FitBarbs.Web.Models;

[Owned]
public class UserPreferences
{
    public int TargetDailyStudyMinutes { get; set; } = 20; // reserved for future; UI uses Profile.TargetDailyMinutes for now
    public bool AutoCompleteLessonAfterWatch { get; set; } = true;
    public bool PlayNextAutomatically { get; set; } = false;
    public bool EmailProgressSummaries { get; set; } = false;
}


