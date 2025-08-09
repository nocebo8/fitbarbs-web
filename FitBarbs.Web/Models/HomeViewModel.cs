using System.Collections.Generic;

namespace FitBarbs.Web.Models;

public class HomeViewModel
{
    public IEnumerable<Course> FeaturedCourses { get; set; } = new List<Course>();
}


