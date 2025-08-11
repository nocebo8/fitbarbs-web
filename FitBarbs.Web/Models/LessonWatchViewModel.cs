namespace FitBarbs.Web.Models;

public class LessonWatchViewModel
{
    public Lesson Lesson { get; set; } = new();

    public List<string> Tips { get; set; } = new();

    public int CompletionPercent { get; set; }

    public int CurrentLessonIndex { get; set; }

    public int TotalLessons { get; set; }

    public bool IsCompleted { get; set; }
}


