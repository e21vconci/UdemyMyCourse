using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using MyCourse.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.ViewModels;
using MyCourse.Models.Options;
using MyCourse.Models.Entities;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.ViewModels.Courses;
using MyCourse.Models.ViewModels.Lessons;
using Ganss.XSS;

namespace MyCourse.Models.Services.Application.Courses
{
    public class EfCoreCourseService : ICourseService
    {

        private readonly MyCourseDbContext dbContext;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;
        private readonly ILogger<EfCoreCourseService> logger;
        private readonly IImagePersister imagePersister;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IEmailClient emailClient;

        public EfCoreCourseService(IHttpContextAccessor httpContextAccessor, ILogger<EfCoreCourseService> logger, IEmailClient emailClient, IImagePersister imagePersister, MyCourseDbContext dbContext, IOptionsMonitor<CoursesOptions> coursesOptions)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.imagePersister = imagePersister;
            this.logger = logger;
            this.coursesOptions = coursesOptions;
            this.dbContext = dbContext;
            this.emailClient = emailClient;
        }

        public MyCourseDbContext DbContext { get; }

        public async Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            // Con esempio di query dichiarativa
            IQueryable<CourseDetailViewModel> queryLinq =
                from course in dbContext.Courses.AsNoTracking().Include(course => course.Lessons)
                where course.Id == id
                select CourseDetailViewModel.FromEntity(course);
            // .Where(course => course.Id == id)
            // .Select(course => CourseDetailViewModel.FromEntity(course)); //Usando metodi statici come FromEntity, la query potrebbe essere inefficiente. Mantenere il mapping nella lambda oppure usare un extension method personalizzato

            CourseDetailViewModel viewModel = await queryLinq.FirstOrDefaultAsync();
            //.FirstOrDefaultAsync(); //Restituisce null se l'elenco è vuoto e non solleva mai un'eccezione
            //.SingleOrDefaultAsync(); //Tollera il fatto che l'elenco sia vuoto e in quel caso restituisce null, oppure se l'elenco contiene più di un elemento solleva un'eccezione
            //.FirstAsync(); //Restituisce il primo elemento, ma se l'elenco è vuoto solleva un'eccezione
            //.SingleAsync(); //Restituisce il primo elemento dell'elenco, ma se l'elenco ne contiene 0 o più di 1, allora solleva un'eccezione

            if (viewModel == null)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }

            return viewModel;
        }

        public async Task<List<CourseViewModel>> GetMostRecentCoursesAsync()
        {
            CourseListInputModel inputModel = new CourseListInputModel(
                search: "",
                page: 1,
                orderby: "Id",
                ascending: false,
                limit: coursesOptions.CurrentValue.InHome,
                orderOptions: coursesOptions.CurrentValue.Order);

            ListViewModel<CourseViewModel> result = await GetCoursesAsync(inputModel);
            return result.Results;
        }

        public async Task<List<CourseViewModel>> GetBestRatingCoursesAsync()
        {
            CourseListInputModel inputModel = new CourseListInputModel(
                search: "",
                page: 1,
                orderby: "Rating",
                ascending: false,
                limit: coursesOptions.CurrentValue.InHome,
                orderOptions: coursesOptions.CurrentValue.Order);

            ListViewModel<CourseViewModel> result = await GetCoursesAsync(inputModel);
            return result.Results;
        }

        public async Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            /*search = search ?? ""; //null coalescing operator
            page = Math.Max(1, page);
            int limit = coursesOptions.CurrentValue.PerPage;
            int offset = (page - 1) * limit;
            var orderOptions = coursesOptions.CurrentValue.Order;
            if(!orderOptions.Allow.Contains(orderby))
            {
                orderby = orderOptions.By;
                ascending = orderOptions.Ascending;
            }*/

            IQueryable<MyCourse.Models.Entities.Course> baseQuery = dbContext.Courses;

            switch (model.OrderBy)
            {
                case "Title":
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.Title);
                    }
                    else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.Title);
                    }
                    break;
                case "Rating":
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.Rating);
                    }
                    else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.Rating);
                    }
                    break;
                case "CurrentPrice":
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.CurrentPrice.Amount);
                    }
                    else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.CurrentPrice.Amount);
                    }
                    break;
                case "Id":
                    if (model.Ascending)
                    {
                        baseQuery = baseQuery.OrderBy(course => course.Id);
                    }
                    else
                    {
                        baseQuery = baseQuery.OrderByDescending(course => course.Id);
                    }
                    break;
            }

            IQueryable<Course> queryLinq = baseQuery
                .Where(course => course.Title.Contains(model.Search))
                .AsNoTracking();
            //per problemi legati a EFCore 3.0 bisogna spostare la Select dopo Skip e Take

            List<CourseViewModel> courses = await queryLinq
                .Skip(model.Offset)
                .Take(model.Limit)
                .Select(course => CourseViewModel.FromEntity(course)) //Usando metodi statici come FromEntity, la query potrebbe essere inefficiente. Mantenere il mapping nella lambda oppure usare un extension method personalizzato
            /*    new CourseViewModel {
                    Id = course.Id,
                    Title = course.Title,
                    ImagePath = course.ImagePath,
                    Author = course.Author,
                    Rating = course.Rating,
                    CurrentPrice = course.CurrentPrice,
                    FullPrice = course.FullPrice
            });*/
                .ToListAsync(); //La query al database viene inviata qui, quando manifestiamo l'intenzione di voler leggere i risultati

            int totalCount = await queryLinq.CountAsync();

            ListViewModel<CourseViewModel> result = new ListViewModel<CourseViewModel>
            {
                Results = courses,
                TotalCount = totalCount
            };

            return result;
        }

        public async Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            string title = inputModel.Title;
            //string author = "Mario Rossi";

            // Tramite Identity possiamo ricavare l'utente autenticato che effettua la creazione del corso
            string author;
            string authorId;

            try
            {
                author = httpContextAccessor.HttpContext.User.FindFirst("FullName").Value;
                // Ricavo l'id dell'utente registrato che crea il nuovo corso
                authorId = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            }
            catch (NullReferenceException)
            {
                throw new UserUnknownException();
            }

            var course = new Course(title, author, authorId);
            dbContext.Add(course);
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exc) when ((exc.InnerException as SqliteException)?.SqliteErrorCode == 19)
            {
                throw new CourseTitleUnavailableException(title, exc);
            }
            return CourseDetailViewModel.FromEntity(course);
        }

        public async Task<bool> IsTitleAvailableAsync(string title, int id)
        {
            //await dbContext.Courses.AnyAsync(course => course.Title == title);
            bool titleExists = await dbContext.Courses.AnyAsync(course => EF.Functions.Like(course.Title, title) && course.Id != id);
            return !titleExists;
        }

        public async Task<CourseEditInputModel> GetCourseForEditingAsync(int id)
        {
            IQueryable<CourseEditInputModel> queryLinq = dbContext.Courses
                .AsNoTracking()
                .Where(course => course.Id == id)
                .Select(course => CourseEditInputModel.FromEntity(course)); //Usando metodi statici come FromEntity, la query potrebbe essere inefficiente. Mantenere il mapping nella lambda oppure usare un extension method personalizzato

            CourseEditInputModel viewModel = await queryLinq.FirstOrDefaultAsync();

            if (viewModel == null)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }

            return viewModel;
        }

        public async Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel)
        {
            Course course = await dbContext.Courses.FindAsync(inputModel.Id);

            course.ChangeTitle(inputModel.Title);
            course.ChangePrices(inputModel.FullPrice, inputModel.CurrentPrice);
            course.ChangeDescription(inputModel.Description);
            course.ChangeEmail(inputModel.Email);

            // aggiornamento proprietà RowVersion. con Entry accediamo al change tracker
            dbContext.Entry(course).Property(course => course.RowVersion).OriginalValue = inputModel.RowVersion;

            if (inputModel.Image != null)
            {
                try
                {
                    string imagePath = await imagePersister.SaveCourseImageAsync(inputModel.Id, inputModel.Image);
                    course.ChangeImagePath(imagePath);
                }
                catch (Exception exc)
                {
                    throw new CourseImageInvalidException(inputModel.Id, exc);
                }
            }

            //dbContext.Update(course); 

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new OptimisticConcurrencyException();
            }
            catch (DbUpdateException exc) when ((exc.InnerException as SqliteException)?.SqliteErrorCode == 19)
            {
                throw new CourseTitleUnavailableException(inputModel.Title, exc);
            }

            return CourseDetailViewModel.FromEntity(course);
        }

        public async Task DeleteCourseAsync(CourseDeleteInputModel inputModel)
        {
            Course course = await dbContext.Courses.FindAsync(inputModel.Id);

            if (course == null)
            {
                throw new CourseNotFoundException(inputModel.Id);
            }

            course.ChangeStatus(CourseStatus.Deleted);
            await dbContext.SaveChangesAsync();
        }

        public async Task SendQuestionToCourseAuthorAsync(int id, string question)
        {
            // Sanitizzo l'input dell'utente
            question = new HtmlSanitizer(allowedTags: new string[0]).Sanitize(question);

            // Recupero le informazioni del corso
            Course course = await dbContext.Courses.FindAsync(id);

            if (course == null)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }

            string courseTitle = course.Title;
            string courseEmail = course.Email;

            // Recupero le informazioni dell'utente che vuole inviare la domanda
            string userFullName;
            string userEmail;

            try
            {
                userFullName = httpContextAccessor.HttpContext.User.FindFirst("FullName").Value;
                // Aggiunto il claim Email nella classe CustomClaimsPrincipalFactory oppure utilizzare il claim Name
                // Nei claims ricavati dall'User tramite httpContext, non è presente il ClaimTypes.Email
                // Se nel CustomClaimsPrincipalFactory aggiungo il claim in questo modo:
                // identity.AddClaim(new Claim(ClaimTypes.Email, user.Email)); posso ricavarmi il valore dell'email dell'utente con ClaimTypes.Email
                userEmail = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email).Value;
            }
            catch (NullReferenceException)
            {
                throw new UserUnknownException();
            }

            // Sanitizzo la domanda dell'utente
            question = new HtmlSanitizer(allowedTags: new string[0]).Sanitize(question);

            // Compongo il testo della domanda
            string subject = $@"Domanda per il tuo corso ""{courseTitle}""";
            string message = $@"<p>L'utente {userFullName} (<a href=""{userEmail}"">{userEmail}</a>)
                                ti ha inviato la seguente domanda per il tuo corso ""{courseTitle}"".</p>
                                <p>{question}</p>";

            // Invio la domanda
            try
            {
                await emailClient.SendEmailAsync(courseEmail, userEmail, subject, message);
            }
            catch
            {
                throw new SendException();
            }
        }

        // Metodo per prelevare l'id dell'autore di un corso per la policy di autorizzazione 
        public Task<string> GetCourseAuthorIdAsync(int courseId)
        {
            return dbContext.Courses
                .Where(course => course.Id == courseId)
                .Select(course => course.AuthorId)
                .FirstOrDefaultAsync();
        }

        public Task<int> GetCourseCountByAuthorIdAsync(string authorId)
        {
            return dbContext.Courses
                .Where(course => course.AuthorId == authorId)
                .CountAsync();
        }
    }
}