using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System.Linq;
using Microsoft.Data.Sqlite;
using ImageMagick;

using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.Exceptions;
using MyCourse.Models.Options;
using MyCourse.Models.ViewModels;
using MyCourse.Models.ValueTypes;
using MyCourse.Models.Exceptions.Infrastructure;
using MyCourse.Models.Exceptions.Application;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.ViewModels.Courses;
using MyCourse.Models.ViewModels.Lessons;
using MyCourse.Models.Enums;
using MyCourse.Models.Extensions;

namespace MyCourse.Models.Services.Application.Courses
{
    public class AdoNetCourseService : ICourseService
    {
        private readonly IDatabaseAccessor db;
        private readonly IImagePersister imagePersister;
        private readonly IOptionsMonitor<CoursesOptions> coursesOptions;
        private readonly ILogger<AdoNetCourseService> logger;
        private readonly IMapper mapper;

        public AdoNetCourseService(ILogger<AdoNetCourseService> logger, IImagePersister imagePersister,
            IDatabaseAccessor db, IOptionsMonitor<CoursesOptions> coursesOptions, IMapper mapper)
        {
            this.mapper = mapper;
            this.imagePersister = imagePersister;
            this.logger = logger;
            this.coursesOptions = coursesOptions;
            this.db = db;
        }
        public async Task<CourseDetailViewModel> GetCourseAsync(int id)
        {

            logger.LogInformation("Course {id} requested", id);

            FormattableString query = $@"SELECT Id, Title, Description, ImagePath, Author, Rating, FullPrice_Amount, FullPrice_Currency, CurrentPrice_Amount, CurrentPrice_Currency FROM Courses WHERE Id={id} AND Status<>{nameof(CourseStatus.Deleted)}
            ; SELECT Id, Title, Description, Duration FROM Lessons WHERE CourseId={id} ORDER BY [Order], Id";

            DataSet dataSet = await db.QueryAsync(query);

            //Course
            var courseTable = dataSet.Tables[0];
            if (courseTable.Rows.Count != 1)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }
            var courseRow = courseTable.Rows[0];

            // Utilizziamo AutoMapper per i Corsi
            // var courseDetailViewModel = CourseDetailViewModel.FromDataRow(courseRow);
            var courseDetailViewModel = mapper.Map<CourseDetailViewModel>(courseRow);

            //Course Lessons
            var lessonDataTable = dataSet.Tables[1];

            // Utilizziamo AutoMapper per le lezioni
            courseDetailViewModel.Lessons = mapper.Map<List<LessonViewModel>>(lessonDataTable.Rows);
            // foreach (DataRow lessonRow in lessonDataTable.Rows)
            // {
            //     // LessonViewModel lessonViewModel = LessonViewModel.FromDataRow(lessonRow);
            //     courseDetailViewModel.Lessons.Add(lessonViewModel);
            // }
            return courseDetailViewModel;
        }

        public async Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            //Sanitizzazione parametri
            /*page = Math.Max(1, page);
            int limit = coursesOptions.CurrentValue.PerPage;
            int offset = (page - 1) * limit;
            var orderOptions = coursesOptions.CurrentValue.Order;
            if(!orderOptions.Allow.Contains(orderby))
            {
                orderby = orderOptions.By;
                ascending = orderOptions.Ascending;
            }

            //Decidere cosa estrarre dal db (componendo una query SQL)
            if (orderby == "CurrentPrice")
            {
                orderby = "CurrentPrice_Amount";
            }*/
            string orderby = model.OrderBy == "CurrentPrice" ? "CurrentPrice_Amount" : model.OrderBy;
            string direction = model.Ascending ? "ASC" : "DESC";

            FormattableString query = $@"SELECT id, Title, ImagePath, Author, Rating, FullPrice_Amount, FullPrice_Currency, CurrentPrice_Amount, CurrentPrice_Currency FROM Courses WHERE Title LIKE {"%" + model.Search + "%"} AND Status<>{nameof(CourseStatus.Deleted)} ORDER BY {(Sql)orderby} {(Sql)direction} LIMIT {model.Limit} OFFSET {model.Offset}; 
            SELECT COUNT(*) FROM Courses WHERE Title LIKE {"%" + model.Search + "%"} AND Status<>{nameof(CourseStatus.Deleted)}";
            DataSet dataSet = await db.QueryAsync(query);
            var dataTable = dataSet.Tables[0];
            // var courseList = new List<CourseViewModel>();
            // foreach (DataRow courseRow in dataTable.Rows)
            // {
            //     CourseViewModel courseViewModel = CourseViewModel.FromDataRow(courseRow);
            //     Se volessimo usare il FromDataRow dell'extension method 
            //     CourseViewModel courseViewModel = courseRow.ToCourseViewModel();
            //     courseList.Add(courseViewModel);
            // }
            // Utilizziamo AutoMapper per il mapping dataRow - viewModel
            var courseList = mapper.Map<List<CourseViewModel>>(dataTable.Rows);

            ListViewModel<CourseViewModel> result = new ListViewModel<CourseViewModel>
            {
                Results = courseList,
                TotalCount = Convert.ToInt32(dataSet.Tables[1].Rows[0][0])
            };

            return result;
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

        public async Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            string title = inputModel.Title;
            string author = "Mario Rossi";

            try
            {
                //DataSet dataSet = await db.QueryAsync($@"INSERT INTO Courses (Title, Author, ImagePath, CurrentPrice_Currency, CurrentPrice_Amount, FullPrice_Currency, FullPrice_Amount) VALUES ({title}, {author}, '/Courses/default.png', 'EUR', 0, 'EUR', 0);
                //                                 SELECT last_insert_rowid();");

                int courseId = await db.QueryScalarAsync<int>($@"INSERT INTO Courses (Title, Author, ImagePath, CurrentPrice_Currency, CurrentPrice_Amount, FullPrice_Currency, FullPrice_Amount) VALUES ({title}, {author}, '/Courses/default.png', 'EUR', 0, 'EUR', 0, {nameof(CourseStatus.Draft)});
                                                 SELECT last_insert_rowid();");

                //int courseId = Convert.ToInt32(dataSet.Tables[0].Rows[0][0]);
                CourseDetailViewModel course = await GetCourseAsync(courseId);
                return course;
            }
            catch (SqliteException exc) when (exc.SqliteErrorCode == 19)
            {
                throw new CourseTitleUnavailableException(title, exc);
            }
        }

        public async Task<bool> IsTitleAvailableAsync(string title, int id)
        {
            //DataSet result = await db.QueryAsync($"SELECT COUNT(*) FROM Courses WHERE Title LIKE {title} AND id<>{id}");
            bool titleExists = await db.QueryScalarAsync<bool>($"SELECT COUNT(*) FROM Courses WHERE Title LIKE {title} AND id<>{id}");
            //bool titleAvailable = Convert.ToInt32(result.Tables[0].Rows[0][0]) == 0;
            return !titleExists;
        }

        public async Task<CourseEditInputModel> GetCourseForEditingAsync(int id)
        {
            FormattableString query = $@"SELECT Id, Title, Description, ImagePath, Email, FullPrice_Amount, FullPrice_Currency, CurrentPrice_Amount, CurrentPrice_Currency, RowVersion FROM Courses WHERE Id={id} AND Status<>{nameof(CourseStatus.Deleted)}";

            DataSet dataSet = await db.QueryAsync(query);

            var courseTable = dataSet.Tables[0];
            if (courseTable.Rows.Count != 1)
            {
                logger.LogWarning("Course {id} not found", id);
                throw new CourseNotFoundException(id);
            }
            var courseRow = courseTable.Rows[0];
            var courseEditInputModel = CourseEditInputModel.FromDataRow(courseRow);
            return courseEditInputModel;
        }

        public async Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel)
        {
            //REFACTORING
            /*DataSet dataSet = await db.QueryAsync($"SELECT COUNT(*) FROM Courses WHERE Id={inputModel.Id}");
            bool courseExists = await db.QueryScalarAsync<bool>($"SELECT COUNT(*) FROM Courses WHERE Id={inputModel.Id}");
            //if (Convert.ToInt32(dataSet.Tables[0].Rows[0][0]) == 0)
            if (!courseExists)
            {
                throw new CourseNotFoundException(inputModel.Id);
            }*/
            try
            {
                string imagePath = null;
                if (inputModel.Image != null)
                {
                    imagePath = await imagePersister.SaveCourseImageAsync(inputModel.Id, inputModel.Image);
                }
                //dataSet = await db.QueryAsync($"UPDATE Courses SET Title={inputModel.Title}, Description={inputModel.Description}, Email={inputModel.Email}, CurrentPrice_Currency={inputModel.CurrentPrice.Currency}, CurrentPrice_Amount={inputModel.CurrentPrice.Amount}, FullPrice_Currency={inputModel.FullPrice.Currency}, FullPrice_Amount={inputModel.FullPrice.Amount} WHERE Id={inputModel.Id}");
                int affectedRows = await db.CommandAsync($"UPDATE Courses SET ImagePath=COALESCE({imagePath}, imagePath), Title={inputModel.Title}, Description={inputModel.Description}, Email={inputModel.Email}, CurrentPrice_Currency={inputModel.CurrentPrice.Currency}, CurrentPrice_Amount={inputModel.CurrentPrice.Amount}, FullPrice_Currency={inputModel.FullPrice.Currency}, FullPrice_Amount={inputModel.FullPrice.Amount} WHERE Id={inputModel.Id} AND Status<>{nameof(CourseStatus.Deleted)} AND RowVersion={inputModel.RowVersion}");
                if (affectedRows == 0)
                {
                    // inviamo la select in modo da sapere se il corso esisteva o meno attraverso il suo id
                    bool courseExists = await db.QueryScalarAsync<bool>($"SELECT COUNT(*) FROM Courses WHERE id={inputModel.Id}");
                    if (courseExists)
                    {
                        throw new OptimisticConcurrencyException();
                    }
                    else
                    {
                        throw new CourseNotFoundException(inputModel.Id);
                    }
                }
            }
            catch (ConstraintViolationException exc)
            {
                throw new CourseTitleUnavailableException(inputModel.Title, exc);
            }
            catch (ImagePersistenceException exc)
            {
                throw new CourseImageInvalidException(inputModel.Id, exc);
            }

            /*if (inputModel.Image != null)
            {
                try {
                    string imagePath = await imagePersister.SaveCourseImageAsync(inputModel.Id, inputModel.Image);
                    dataSet = await db.QueryAsync($"UPDATE Courses SET ImagePath={imagePath} WHERE Id={inputModel.Id}");
                }
                catch(Exception exc)
                {
                    throw new CourseImageInvalidException(inputModel.Id, exc);
                }
            }*/

            CourseDetailViewModel course = await GetCourseAsync(inputModel.Id);
            return course;
        }

        public async Task DeleteCourseAsync(CourseDeleteInputModel inputModel)
        {
            int affectedRows = await this.db.CommandAsync($"UPDATE Courses SET Status={nameof(CourseStatus.Deleted)} WHERE Id={inputModel.Id} AND Status<>{nameof(CourseStatus.Deleted)}");

            if (affectedRows == 0)
            {
                throw new CourseNotFoundException(inputModel.Id);
            }
        }
    }
}