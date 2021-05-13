using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Pages
{
    [ValidateReCaptcha]
    public class ContactModel : PageModel
    {
        public CourseDetailViewModel Course { get; private set; }

        [Required(ErrorMessage = "Il testo della domanda è obbligatorio")]
        [BindProperty]
        public string Question { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, [FromServices] ICourseService courseService)
        {
            try
            {
                Course = await courseService.GetCourseAsync(id);
                return Page();
            }
            catch
            {
                return RedirectToAction("Index", "Courses");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id, [FromServices] ICourseService courseService)
        {
            if (ModelState.IsValid)
            {
                // Invio domanda al docente
                await courseService.SendQuestionToCourseAuthorAsync(id, Question);
                TempData["ConfirmationMessage"] = "La tua domanda è stata inviata";
                return RedirectToAction("Detail", "Courses", new { id = id });
            }
            else
            {
                return await OnGetAsync(id, courseService);
            }
        }
    }
}
