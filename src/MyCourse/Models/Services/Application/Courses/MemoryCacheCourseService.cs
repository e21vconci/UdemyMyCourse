using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MyCourse.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.ViewModels.Courses;

namespace MyCourse.Models.Services.Application.Courses
{
    public class MemoryCacheCourseService : ICachedCourseService
    {
        private readonly ICourseService courseService;
        private readonly IMemoryCache memoryCache;
        private readonly IConfiguration configuration;

        public MemoryCacheCourseService(ICourseService courseService, IMemoryCache memoryCache, IConfiguration configuration)
        {
            this.configuration = configuration;
            this.memoryCache = memoryCache;
            this.courseService = courseService;
        }

        public Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            int timeSpan = configuration.GetValue<int>("MemoryCache:TimeSpan");
            return memoryCache.GetOrCreateAsync($"Course{id}", cacheEntry =>
            {
                //cacheEntry.SetSize(1);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeSpan)); //Esercizio: provate a recuperare il valore 60 usando il servizio di configurazione(nel file appsettings.json)
                return courseService.GetCourseAsync(id);
            });
        }

        public Task<List<CourseViewModel>> GetMostRecentCoursesAsync()
        {
            int timeSpan = configuration.GetValue<int>("MemoryCache:TimeSpan");

            return memoryCache.GetOrCreateAsync($"MostRecentCourses", cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeSpan));
                return courseService.GetMostRecentCoursesAsync();
            });
        }

        public Task<List<CourseViewModel>> GetBestRatingCoursesAsync()
        {
            int timeSpan = configuration.GetValue<int>("MemoryCache:TimeSpan");

            return memoryCache.GetOrCreateAsync($"BestRatingCourses", cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeSpan));
                return courseService.GetBestRatingCoursesAsync();
            });
        }

        public Task<ListViewModel<CourseViewModel>> GetCoursesAsync(CourseListInputModel model)
        {
            //Metto in cache i risultati solo per le prime 5 pagine del catalogo, che reputo essere
            //le più visitate dagli utenti, e che perciò mi permettono di avere il maggior beneficio dalla cache.
            //E inoltre, metto in cache i risultati solo se l'utente non ha cercato nulla.
            //In questo modo riduco drasticamente il consumo di memoria RAM
            bool canCache = model.Page <= 5 && string.IsNullOrEmpty(model.Search);

            //Se canCache è true, sfrutto il meccanismo di caching
            if (canCache)
            {
                int timeSpan = configuration.GetValue<int>("MemoryCache:TimeSpan");
                return memoryCache.GetOrCreateAsync($"Courses{model.Page}-{model.OrderBy}-{model.Ascending}", cacheEntry =>
                {
                    //cacheEntry.SetSize(1);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeSpan)); //Esercizio: provate a recuperare il valore 60 usando il servizio di configurazione
                    return courseService.GetCoursesAsync(model);
                });
            }

            //Altrimenti uso il servizio applicativo sottostante, che recupererà sempre i valori dal database
            return courseService.GetCoursesAsync(model);
        }

        public Task<CourseDetailViewModel> CreateCourseAsync(CourseCreateInputModel inputModel)
        {
            return courseService.CreateCourseAsync(inputModel);
        }

        public Task<bool> IsTitleAvailableAsync(string title, int id)
        {
            return courseService.IsTitleAvailableAsync(title, id);
        }

        public Task<CourseEditInputModel> GetCourseForEditingAsync(int id)
        {
            return courseService.GetCourseForEditingAsync(id);
        }

        //TODO: ricordati di usare memoryCache.Remove($"Course{id}") quando aggiorni il corso
        public async Task<CourseDetailViewModel> EditCourseAsync(CourseEditInputModel inputModel)
        {
            CourseDetailViewModel viewModel = await courseService.EditCourseAsync(inputModel);
            memoryCache.Remove($"Course{inputModel.Id}");
            return viewModel;
        }

        public async Task DeleteCourseAsync(CourseDeleteInputModel inputModel)
        {
            await courseService.DeleteCourseAsync(inputModel);
            memoryCache.Remove($"Course{inputModel.Id}");
        }

        public Task SendQuestionToCourseAuthorAsync(int id, string question)
        {
            return courseService.SendQuestionToCourseAuthorAsync(id, question);
        }

        public Task<string> GetCourseAuthorIdAsync(int courseId)
        {
            return memoryCache.GetOrCreateAsync($"CourseAuthorId{courseId}", cacheEntry =>
            {
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(60)); //Esercizio: provate a recuperare il valore 60 usando il servizio di configurazione
                return courseService.GetCourseAuthorIdAsync(courseId);
            }); ;
        }
    }
}