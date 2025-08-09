namespace FitBarbs.Web.Models;

public class ProfileViewModel
{
    public UserProfile Profile { get; set; } = new UserProfile();
    public List<Enrollment> MyEnrollments { get; set; } = new();
    public Dictionary<int, int> ProgressByCourseId { get; set; } = new();
    public List<Course> RecommendedCourses { get; set; } = new();
}


