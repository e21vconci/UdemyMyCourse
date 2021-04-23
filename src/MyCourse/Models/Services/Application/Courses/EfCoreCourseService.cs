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
using System.Linq.Dynamic.Core;

namespace MyCourse.Models.Services.Application.Courses
{
    public class EfCoreCourseService : ICourseService
    {

        private readonly MyCourseDbContext dbContext;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;
        private readonly ILogger<EfCoreCourseService> logger;
        private readonly IImagePersister imagePersister;
        private readonly IHttpContextAccessor httpContextAccessor;

        public EfCoreCourseService(IHttpContextAccessor httpContextAccessor, ILogger<EfCoreCourseService> logger, IImagePersister imagePersister, MyCourseDbContext dbContext, IOptionsMonitor<CoursesOptions> coursesOptions)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.imagePersister = imagePersister;
            this.logger = logger;
            this.coursesOptions = coursesOptions;
            this.dbContext = dbContext;
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

            CourseDetailViewModel viewModel = await queryLinq.SingleAsync();
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

            // UTILIZZO DYNAMIC LINQ
            string orderby = model.OrderBy;
            if (orderby == "CurrentPrice")
            {
                orderby = "CurrentPrice.Amount";
            }
            string direction = model.Ascending ? "asc" : "desc";

            IQueryable<MyCourse.Models.Entities.Course> baseQuery = dbContext.Courses.OrderBy($"{orderby} {direction}");

            // CON SWITCH EXPRESSION
            //baseQuery = (model.OrderBy, model.Ascending) switch
            //{
            //    ("Title", true) => baseQuery.OrderBy(course => course.Title),
            //    ("Title", false) => baseQuery.OrderByDescending(course => course.Title),
            //    ("Rating", true) => baseQuery.OrderBy(course => course.Rating),
            //    ("Rating", false) => baseQuery.OrderByDescending(course => course.Rating),
            //    ("CurrentPrice", true) => baseQuery.OrderBy(course => course.CurrentPrice.Amount),
            //    ("CurrentPrice", false) => baseQuery.OrderByDescending(course => course.CurrentPrice.Amount),
            //    ("Id", true) => baseQuery.OrderBy(course => course.Id),
            //    ("Id", false) => baseQuery.OrderByDescending(course => course.Id),
            //    _ => baseQuery
            //};
            
            //switch (model.OrderBy)
            //{
            //    case "Title":
            //        if (model.Ascending)
            //        {
            //            baseQuery = baseQuery.OrderBy(course => course.Title);
            //        }
            //        else
            //        {
            //            baseQuery = baseQuery.OrderByDescending(course => course.Title);
            //        }
            //        break;
            //    case "Rating":
            //        if (model.Ascending)
            //        {
            //            baseQuery = baseQuery.OrderBy(course => course.Rating);
            //        }
            //        else
            //        {
            //            baseQuery = baseQuery.OrderByDescending(course => course.Rating);
            //        }
            //        break;
            //    case "CurrentPrice":
            //        if (model.Ascending)
            //        {
            //            baseQuery = baseQuery.OrderBy(course => course.CurrentPrice.Amount);
            //        }
            //        else
            //        {
            //            baseQuery = baseQuery.OrderByDescending(course => course.CurrentPrice.Amount);
            //        }
            //        break;
            //    case "Id":
            //        if (model.Ascending)
            //        {
            //            baseQuery = baseQuery.OrderBy(course => course.Id);
            //        }
            //        else
            //        {
            //            baseQuery = baseQuery.OrderByDescending(course => course.Id);
            //        }
            //        break;
            //}

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
                throw new UserUnkownException();
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
    }
}