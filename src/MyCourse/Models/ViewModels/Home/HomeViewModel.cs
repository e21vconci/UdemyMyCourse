using System.Collections.Generic;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Models.ViewModels.Home
{
    public class HomeViewModel : CourseViewModel
    {
        public List<CourseViewModel> MostRecentCourses { get; set; }
        public List<CourseViewModel> BestRatingCourses { get; set; }
    }
}