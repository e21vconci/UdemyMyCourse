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
using Microsoft.Extensions.Hosting;
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
            services.AddResponseCaching();

            services.AddMvc(options =>
            {
                var homeProfile = new CacheProfile();
                //homeProfile.Duration = Configuration.GetValue<int>("ResponseCache:Home:Duration");
                //homeProfile.Location = Configuration.GetValue<ResponseCacheLocation>("ResponseCache:Location");
                //homeProfile.VaryByQueryKeys = new string[] { "page" };
                Configuration.Bind("ResponseCache:Home", homeProfile);
                
                options.CacheProfiles.Add("Home", homeProfile);
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            #if DEBUG
            .AddRazorRuntimeCompilation()
            #endif
            ;

            //services.AddTransient<ICourseService, CourseService>();
            //services.AddTransient<ICourseService, AdoNetCourseService>();
            services.AddTransient<ICourseService, EfCoreCourseService>();
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

            //Options
            services.Configure<ConnectionStringsOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection("MemoryCache"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // Qui vengono inseriti i middleware
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            /*if (env.IsProduction()) 
            {
                app.UseHttpsRedirection();
            }*/

            //if (env.IsDevelopment())
            if (env.IsEnvironment("Development"))
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

            //EndpointRoutingMiddleware
            app.UseRouting();

            app.UseResponseCaching();

            //EndpointMiddleware
            app.UseEndpoints(routeBuilder => {
                routeBuilder.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            // Middleware di routing .NET 2.2
            //app.UseMvcWithDefaultRoute();
            /*app.UseMvc(routeBuilder =>
            {
                // es. /courses/detail/5
                // route di default
                routeBuilder.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });*/
        }
    }
}
