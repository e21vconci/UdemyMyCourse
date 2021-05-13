using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ViewModels.Courses;
using MyCourse.Models.ViewModels.Home;

namespace MyCourse.Controllers
{
    //L'attributo avrebbe effetto su tutte le action del controller
    //[ResponseCache(CacheProfileName= "Home")]
    public class HomeController : Controller
    {
        //[ResponseCache(CacheProfileName= "Home")]
        [AllowAnonymous]
        public async Task<IActionResult> Index([FromServices] ICachedCourseService courseService)
        {
            ViewData["Title"] = "Benvenuto su MyCourse";
            List<CourseViewModel> bestRatingCourses = await courseService.GetBestRatingCoursesAsync();
            List<CourseViewModel> mostRecentCourses = await courseService.GetMostRecentCoursesAsync();

            HomeViewModel viewModel = new HomeViewModel
            {
                BestRatingCourses = bestRatingCourses,
                MostRecentCourses = mostRecentCourses
            };

            return View(viewModel);
        }
    }
}