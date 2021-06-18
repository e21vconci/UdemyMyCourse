using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MyCourse.Models.Authorization
{
    public class CourseAuthorRequirementHandler : AuthorizationHandler<CourseAuthorRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ICachedCourseService courseService;

        public CourseAuthorRequirementHandler(IHttpContextAccessor httpContextAccessor, ICachedCourseService courseService)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.courseService = courseService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, 
                                                                CourseAuthorRequirement requirement)
        {
            // 1.leggere l’id dell’utente dalla sua identità
            string userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2.capire a quale corso sta cercando di accedere dall'url della richiesta (/Courses/Edit/1) 
            int courseId = context.Resource is int ? (int)context.Resource : // Se non ho l'id nell'url
                           Convert.ToInt32(httpContextAccessor.HttpContext.Request.RouteValues["id"]);

            if (courseId == 0)
            {
                context.Fail();
                return;
            }

            // 3.estrarre dal database l’id dell’autore del corso
            // per evitare query al db potrei utilizzare un claim personalizzato per memorizzare nel cookie di autenticazione
            // gli id dei corsi appartenenti al docente
            string authorId = await courseService.GetCourseAuthorIdAsync(courseId);

            // 4.verificare che l’id dell’utente sia uguale all’id dell’autore del corso
            if (userId == authorId)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
