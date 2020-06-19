using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.ViewModels;
using MyCourse.Models.Options;
using Microsoft.Extensions.Logging;
using MyCourse.Models.Entities;
using MyCourse.Models.InputModels;
using Microsoft.Data.Sqlite;
using MyCourse.Models.Exceptions;

namespace MyCourse.Models.Services.Application
{
    public class EfCoreCourseService : ICourseService
    {

        private readonly MyCourseDbContext dbContext;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;
        private readonly ILogger<EfCoreCourseService> logger;

        public EfCoreCourseService(ILogger<EfCoreCourseService> logger, MyCourseDbContext dbContext, IOptionsMonitor<CoursesOptions> coursesOptions)
        {
            this.logger = logger;
            this.coursesOptions = coursesOptions;
            this.dbContext = dbContext;
        }

        public MyCourseDbContext DbContext { get; }

        public async Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            IQueryable<CourseDetailViewModel> queryLinq = dbContext.Courses
                .AsNoTracking()
                .Where(course => course.Id == id)
                .Select(course => new CourseDetailViewModel
                {
                    Id = course.Id,
                    Title = course.Title,
                    Description = course.Description,
                    Author = course.Author,
                    ImagePath = course.ImagePath,
                    Rating = course.Rating,
                    CurrentPrice = course.CurrentPrice,
                    FullPrice = course.FullPrice,
                    Lessons = course.Lessons.Select(lesson => new LessonViewModel
                    {
                        Id = lesson.Id,
                        Title = lesson.Title,
                        Description = lesson.Description,
                        Duration = lesson.Duration
                    }).ToList()
                });

            CourseDetailViewModel viewModel = await queryLinq.SingleAsync();
            //.FirstOrDefaultAsync(); //Restituisce null se l'elenco è vuoto e non solleva mai un'eccezione
            //.SingleOrDefaultAsync(); //Tollera il fatto che l'elenco sia vuoto e in quel caso restituisce null, oppure se l'elenco contiene più di un elemento solleva un'eccezione
            //.FirstAsync(); //Restituisce il primo elemento, ma se l'elenco è vuoto solleva un'eccezione
            //.SingleAsync(); //Restituisce il primo elemento dell'elenco, ma se l'elenco ne contiene 0 o più di 1, allora solleva un'eccezione

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
            
            IQueryable<Course> baseQuery = dbContext.Courses;

            switch(model.OrderBy)
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
            string author = "Mario Rossi";

            var course = new Course(title, author);
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

        public async Task<bool> IsTitleAvailableAsync(string title)
        {
            await dbContext.Courses.AnyAsync(course => course.Title == title);
            bool titleExists = await dbContext.Courses.AnyAsync(course => EF.Functions.Like(course.Title, title));
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

            //dbContext.Update(course); 

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exc) when ((exc.InnerException as SqliteException)?.SqliteErrorCode == 19)
            {
                throw new CourseTitleUnavailableException(inputModel.Title, exc);
            }

            return CourseDetailViewModel.FromEntity(course);
        }
    }
}