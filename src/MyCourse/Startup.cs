using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Application;
using MyCourse.Models.Services.Infrastructure;

namespace MyCourse
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) 
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            //services.AddTransient<ICourseService, CourseService>();
            services.AddTransient<ICourseService, AdoNetCourseService>();
            //services.AddTransient<ICourseService, EfCoreCourseService>();
            services.AddTransient<IDatabaseAccessor, SqliteDatabaseAccessor>();
            services.AddTransient<ICachedCourseService, MemoryCacheCourseService>();

            //services.AddScoped<MyCourseDbContext>();
            //services.AddDbContext<MyCourseDbContext>();
            services.AddDbContextPool<MyCourseDbContext>(optionsBuilder => 
            {
                //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                //optionsBuilder.UseSqlite("Data Source=Data/MyCourse.db");
                string connectionString = Configuration.GetSection("ConnectionStrings").GetValue<string>("Default");
                optionsBuilder.UseSqlite(connectionString);
            });

            #region Configurazione del servizio di cache distribuita

            //Se vogliamo usare Redis, ecco le istruzioni per installarlo: https://docs.microsoft.com/it-it/aspnet/core/performance/caching/distributed?view=aspnetcore-2.2#distributed-redis-cache
            //Bisogna anche installare il pacchetto NuGet: Microsoft.Extensions.Caching.StackExchangeRedis
            services.AddStackExchangeRedisCache(options =>
            {
                Configuration.Bind("DistributedCache:Redis", options);
            });

            //Se vogliamo usare Sql Server, ecco le istruzioni per preparare la tabella usata per la cache: https://docs.microsoft.com/it-it/aspnet/core/performance/caching/distributed?view=aspnetcore-2.2#distributed-sql-server-cache
            /*services.AddDistributedSqlServerCache(options => 
            {
                Configuration.Bind("DistributedCache:SqlServer", options);
            });*/

            //Se vogliamo usare la memoria, mentre siamo in sviluppo (solo per lo sviluppo!)
            //services.AddDistributedMemoryCache();

            #endregion

            //Options
            services.Configure<ConnectionStringsOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection("MemoryCache"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // Qui vengono inseriti i middleware
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsProduction()) 
            {
                app.UseHttpsRedirection();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                //Aggiorniamo un file per notificare al BrowserSync che deve aggiornare la pagina
                lifetime.ApplicationStarted.Register(() => 
                {
                    string filePath = Path.Combine(env.ContentRootPath, "bin/reload.txt");
                    File.WriteAllText(filePath, DateTime.Now.ToString());
                });
            } 
            else 
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            // Middleware di routing
            //app.UseMvcWithDefaultRoute();
            app.UseMvc(routeBuilder =>
            {
                // es. /courses/detail/5
                // route di default
                routeBuilder.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
