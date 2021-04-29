using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

using MyCourse.Models.Enums;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.Entities;
using MyCourse.Models.Validators;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.Services.Application.Lessons;
using MyCourse.Customizations.ModelBinders;
using MyCourse.Customizations.Identity;

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
            // Abilito Razor Pages
            services.AddRazorPages();

            services.AddMvc(options =>
            {
                var homeProfile = new CacheProfile();
                //homeProfile.Duration = Configuration.GetValue<int>("ResponseCache:Home:Duration");
                //homeProfile.Location = Configuration.GetValue<ResponseCacheLocation>("ResponseCache:Location");
                //homeProfile.VaryByQueryKeys = new string[] { "page" };
                Configuration.Bind("ResponseCache:Home", homeProfile);
                options.CacheProfiles.Add("Home", homeProfile);

                options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
            .AddFluentValidation(options => {
                options.RegisterValidatorsFromAssemblyContaining<CourseCreateValidator>();
                //Per il validator personalizzato
                options.ConfigureClientsideValidation(clientSide => {
                    clientSide.Add(typeof(RemotePropertyValidator), (context, description, validator) => new RemoteClientValidator(description, validator));
                });
            })
            #if DEBUG
            .AddRazorRuntimeCompilation()
            #endif
            ;

            var identityBuilder = services.AddDefaultIdentity<ApplicationUser>(options => {
                    // Criteri di validazione della password
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredUniqueChars = 4;

                    // Conferma dell'account
                    options.SignIn.RequireConfirmedAccount = true;

                    // Blocco dell'account
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>()
                // Aggiungo il servizio per la validazione password con logica personalizzata
                .AddPasswordValidator<CommonPasswordValidator<ApplicationUser>>();

            //Usiamo ADO.NET o Entity Framework Core per l'accesso ai dati?
            var persistence = Persistence.EfCore;
            switch (persistence)
            {
                case Persistence.AdoNet:
                    services.AddTransient<ICourseService, AdoNetCourseService>();
                    services.AddTransient<ILessonService, AdoNetLessonService>();
                    services.AddTransient<IDatabaseAccessor, SqliteDatabaseAccessor>();

                    //Imposta l'AdoNetUserStore come servizio di persistenza per Identity
                    identityBuilder.AddUserStore<AdoNetUserStore>();
                break;

                case Persistence.EfCore:
                    // Core Identity utilizza EntityFramework 
                    // Imposta il MyCourseDbContext come servizio di persistenza per Identity
                    identityBuilder.AddEntityFrameworkStores<MyCourseDbContext>();

                    services.AddTransient<ICourseService, EfCoreCourseService>();
                    services.AddTransient<ILessonService, EfCoreLessonService>();
                    //services.AddScoped<MyCourseDbContext>();
                    //services.AddDbContext<MyCourseDbContext>();
                    services.AddDbContextPool<MyCourseDbContext>(optionsBuilder => 
                    {
                        //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                        //optionsBuilder.UseSqlite("Data Source=Data/MyCourse.db");
                        string connectionString = Configuration.GetSection("ConnectionStrings").GetValue<string>("Default");
                        optionsBuilder.UseSqlite(connectionString);
                    });
                break;
            }
            //services.AddTransient<ICourseService, CourseService>();
            
            services.AddTransient<ICachedCourseService, MemoryCacheCourseService>();
            services.AddTransient<ICachedLessonService, MemoryCacheLessonService>();
            services.AddSingleton<IImagePersister, MagickNetImagePersister>();
            services.AddSingleton<IEmailSender, MailKitEmailSender>();

            //Options prelevate dal file appsettings.json
            services.Configure<ConnectionStringsOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection("MemoryCache"));
            services.Configure<KestrelServerOptions>(Configuration.GetSection("Kestrel"));
            services.Configure<SmtpOptions>(Configuration.GetSection("Smtp"));

            // Servizio per il mapping tra datarow e viewmodel
            services.AddAutoMapper(typeof(Startup));

            //Validators di FluentValidation
            //Si possono registrare così nel caso ci sia bisogno di selezionare un ciclo di vita diverso da Transient
            //services.AddScoped<IValidator<CourseCreateInputModel>, CourseCreateValidator>();
            //services.AddSingleton<IValidator<CourseEditInputModel>, CourseEditValidator>();
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

            //Nel caso volessi impostare una Culture specifica...
            /*var appCulture = CultureInfo.InvariantCulture;
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(appCulture),
                SupportedCultures = new[] { appCulture }
            });*/

            //EndpointRoutingMiddleware
            app.UseRouting();

            // Middleware autenticazione e autorizzazione
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseResponseCaching();

            //EndpointRoutingMiddleware
            app.UseEndpoints(routeBuilder => {
                routeBuilder.MapControllerRoute(
                    name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
                routeBuilder.MapRazorPages();
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
