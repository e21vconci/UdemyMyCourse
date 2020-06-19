using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyCourse.Models.Exceptions;
using MyCourse.Models.InputModels;
using MyCourse.Models.Services.Application;
using MyCourse.Models.ViewModels;

namespace MyCourse.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ICourseService courseService;
        
        public CoursesController(ICachedCourseService courseService)
        {
            this.courseService = courseService;
        }

        public async Task<IActionResult> Index(CourseListInputModel input/*string search, int page, string orderby, bool ascending*/)
        {
            ViewData["Title"] = "Catalogo dei corsi";
            //var courseService = new CourseService();
            ListViewModel<CourseViewModel> courses = await courseService.GetCoursesAsync(input);

            CourseListViewModel viewModel = new CourseListViewModel
            {
                Courses = courses,
                Input = input
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Detail(int id)
        {
            //var courseService = new CourseService();
            CourseDetailViewModel viewModel = await courseService.GetCourseAsync(id);
            ViewData["Title"] = viewModel.Title;
            return View(viewModel);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Nuovo Corso";
            var inputModel = new CourseCreateInputModel();
            return View(inputModel);
        }

        [HttpPost] //Per differenziare la chiamata all'action del controller in base al method del form nella view
        public async Task<IActionResult> Create(CourseCreateInputModel inputModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Coinvolgere un servizio applicativo in modo che il corso venga creato
                    CourseDetailViewModel course = await courseService.CreateCourseAsync(inputModel);
                    return RedirectToAction(nameof(Index));
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseDetailViewModel.Title), "Questo titolo già esiste");
                }
            }

            ViewData["Title"] = "Nuovo Corso";
            return View(inputModel);
        }

        public async Task<IActionResult> IsTitleAvailable(string title)
        {
            bool result = await courseService.IsTitleAvailableAsync(title);
            return Json(result);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Modifica corso";
            CourseEditInputModel inputModel = await courseService.GetCourseForEditingAsync(id);
            return View(inputModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CourseEditInputModel inputModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Persisto i dati
                    CourseDetailViewModel course = await courseService.EditCourseAsync(inputModel);
                    return RedirectToAction(nameof(Index));
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseDetailViewModel.Title), "Questo titolo già esiste");
                }
            }

            ViewData["Title"] = "Modifica corso";
            return View(inputModel);
        }
    }
}