using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyCourse.Models.Enums;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ICourseService courseService;
        
        public CoursesController(ICachedCourseService courseService)
        {
            this.courseService = courseService;
        }

        [AllowAnonymous]
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

        [Authorize]
        public async Task<IActionResult> Subscribe(int id)
        {
            //TODO: reindirizzo l'utente verso la pagina di pagamento
            // Se il pagamento si conclude correttamento persisto l'utente nella tabella del db
            CourseSubscribeInputModel inputModel = new CourseSubscribeInputModel()
            {
                CourseId = id,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                TransactionId = string.Empty,
                PaymentType = string.Empty,
                Paid = new Models.ValueTypes.Money(Currency.EUR, 0m),
                PaymentDate = DateTime.UtcNow
            };
            await courseService.SubscribeCourseAsync(inputModel);
            TempData["ConfirmationMessage"] = "Grazie per esserti iscritto, guarda subito la prima lezione!";
            return RedirectToAction(nameof(Detail), new { id = id });
        }

        [AllowAnonymous]
        public async Task<IActionResult> Detail(int id)
        {
            //var courseService = new CourseService();
            CourseDetailViewModel viewModel = await courseService.GetCourseAsync(id);
            ViewData["Title"] = viewModel.Title;
            return View(viewModel);
        }

        [Authorize(Roles = nameof(Role.Teacher))]
        public IActionResult Create()
        {
            ViewData["Title"] = "Nuovo Corso";
            var inputModel = new CourseCreateInputModel();
            return View(inputModel);
        }

        [HttpPost] //Per differenziare la chiamata all'action del controller in base al method del form nella view
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Create(CourseCreateInputModel inputModel, [FromServices] IAuthorizationService authorizationService, [FromServices] IEmailClient emailClient, [FromServices] IOptionsMonitor<UsersOptions> usersOptions)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Coinvolgere un servizio applicativo in modo che il corso venga creato
                    CourseDetailViewModel course = await courseService.CreateCourseAsync(inputModel);

                    // Utilizzo l'IAuthorizationService per verificare se il docente ha creato più di 5 corsi
                    // Per non inserire logica nel controller, potremmo spostare questo blocco all'interno del metodo CreateCourseAsync del servizio applicativo
                    // ...ma attenzione a non creare riferimenti circolari! Se il course service dipende da IAuthorizationService
                    // ...e viceversa l'authorization handler dipende dal course service, allora la dependency injection non riuscirà a risolvere nessuno dei due servizi, dandoci un errore
                    AuthorizationResult result = await authorizationService.AuthorizeAsync(User, nameof(Policy.CourseLimit));
                    if (!result.Succeeded)
                    {
                        await emailClient.SendEmailAsync(usersOptions.CurrentValue.NotificationEmailRecipient, "Avviso superamento soglia", $"Il docente {User.Identity.Name} ha creato molti corsi: verifica che riesca a gestirli tutti.");
                    }

                    // Avremmo anche potuto operare diversamente e non utilizzare il meccanismo delle policy per effettuare questa verifica
                    // Il meccanismo della policy è utile se tale logica deve essere riutilizzata in più punti dell'applicazione
                    //int courseCount = await courseService.GetCourseCountByAuthorIdAsync(userId);
                    //if (courseCount > 5)
                    //{
                    //    // Invio l'email
                    //}

                    TempData["ConfirmationMessage"] = "Ok! Il tuo corso è stato creato, ora perché non inserisci anche gli altri dati?";
                    return RedirectToAction(nameof(Edit), new { id = course.Id });
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseDetailViewModel.Title), "Questo titolo già esiste");
                }
            }

            ViewData["Title"] = "Nuovo Corso";
            return View(inputModel);
        }

        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> IsTitleAvailable(string title, int id = 0)
        {
            bool result = await courseService.IsTitleAvailableAsync(title, id);
            return Json(result);
        }

        [Authorize(Policy = nameof(Policy.CourseAuthor))]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Modifica corso";
            CourseEditInputModel inputModel = await courseService.GetCourseForEditingAsync(id);
            return View(inputModel);
        }

        [HttpPost]
        [Authorize(Policy = nameof(Policy.CourseAuthor))]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Edit(CourseEditInputModel inputModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Persisto i dati
                    CourseDetailViewModel course = await courseService.EditCourseAsync(inputModel);
                    TempData["ConfirmationMessage"] = "I dati sono stati salvati con successo";
                    return RedirectToAction(nameof(Detail), new { id = inputModel.Id });
                }
                catch (CourseTitleUnavailableException)
                {
                    ModelState.AddModelError(nameof(CourseDetailViewModel.Title), "Questo titolo già esiste");
                }
                catch (CourseImageInvalidException)
                {
                    ModelState.AddModelError(nameof(CourseEditInputModel.Image), "L'immagine selezionata non è valida");
                }
                catch (OptimisticConcurrencyException)
                {
                     ModelState.AddModelError("", "Spiacenti, il salvataggio non è andato a buon fine perchè nel frattempo un altro utente ha aggiornato il corso. Ti preghiamo di aggiornare la pagina e ripetere le modifiche.");
                }
            }

            ViewData["Title"] = "Modifica corso";
            return View(inputModel);
        }

        [HttpPost]
        [Authorize(Policy = nameof(Policy.CourseAuthor))]
        [Authorize(Roles = nameof(Role.Teacher))]
        public async Task<IActionResult> Delete(CourseDeleteInputModel inputModel)
        {
            await courseService.DeleteCourseAsync(inputModel);
            TempData["ConfirmationMessage"] = "Il corso è stato eliminato ma potrebbe continuare a comparire negli elenchi per un breve periodo, finché la cache non viene aggiornata.";
            return RedirectToAction(nameof(Index));
        }
    }
}