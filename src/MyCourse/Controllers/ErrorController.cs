using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using MyCourse.Models.Exceptions.Application;

namespace MyCourse.Controllers
{
    public class ErrorController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index() 
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            
            switch(feature.Error)
            {
                case CourseNotFoundException exc:
                    ViewData["Title"] = "Corso non trovato";
                    Response.StatusCode = 404;
                    return View("CourseNotFound");

                case UserUnknownException exc:
                    ViewData["Title"] = "Utente sconosciuto";
                    Response.StatusCode = 400;
                    return View();

                case SendException exc:
                    ViewData["Title"] = "Non � stato possibile inviare il messaggio, riprova pi� tardi";
                    Response.StatusCode = 500;
                    return View();

                default:
                    ViewData["Title"] = "Errore";
                    return View();
            }
        }
    }
}