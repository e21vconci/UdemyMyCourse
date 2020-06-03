using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MyCourse.Models.ViewModels;
using Microsoft.Extensions.Configuration;

namespace MyCourse.Models.Services.Application
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

        //TODO: ricordati di usare memoryCache.Remove($"Course{id}") quando aggiorni il corso
        public Task<CourseDetailViewModel> GetCourseAsync(int id)
        {
            int timeSpan = configuration.GetValue<int>("MemoryCache:TimeSpan");
            return memoryCache.GetOrCreateAsync($"Course{id}", cacheEntry =>
            {
                cacheEntry.SetSize(1);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeSpan)); //Esercizio: provate a recuperare il valore 60 usando il servizio di configurazione(nel file appsettings.json)
                return courseService.GetCourseAsync(id);
            });
        }

        public Task<List<CourseViewModel>> GetCoursesAsync()
        {
            int timeSpan = configuration.GetValue<int>("MemoryCache:TimeSpan");
            return memoryCache.GetOrCreateAsync($"Courses", cacheEntry =>
            {
                cacheEntry.SetSize(1);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(timeSpan)); //Esercizio: provate a recuperare il valore 60 usando il servizio di configurazione
                return courseService.GetCoursesAsync();
            });
        }
    }
}