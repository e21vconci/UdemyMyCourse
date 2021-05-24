using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Security.Claims;
using System.Net.Http;
using System.Text.Json;
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
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication;

using MyCourse.Models.Enums;
using MyCourse.Models.Options;
using MyCourse.Models.Services.Infrastructure;
using MyCourse.Models.Entities;
using MyCourse.Models.Validators;
using MyCourse.Models.Services.Application.Courses;
using MyCourse.Models.Services.Application.Lessons;
using MyCourse.Customizations.ModelBinders;
using MyCourse.Customizations.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using AspNetCore.ReCaptcha;
using MyCourse.Models.Authorization;

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
            services.AddReCaptcha(Configuration.GetSection("ReCaptcha"));
            services.AddResponseCaching();
            // Abilito Razor Pages
            services.AddRazorPages(options => {
                options.Conventions.AllowAnonymousToPage("/Privacy");
            });

            // Login tramite Facebook
            services.AddAuthentication().AddFacebook(facebookOptions =>
            {
                facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];

                facebookOptions.Scope.Add("email");
                facebookOptions.Scope.Add("user_location");
                
                // Per ottenere altre informazioni da Facebook
                facebookOptions.Events = new OAuthEvents
                {
                    OnCreatingTicket = async context =>
                    {
                        var location = await GetUserLocationFromFacebook(context.AccessToken);
                        var identity = context.Principal.Identity as ClaimsIdentity;
                        identity.AddClaim(new Claim(ClaimTypes.Locality, location));
                    }
                };
            });

            services.AddMvc(options =>
            {
                var homeProfile = new CacheProfile();
                //homeProfile.Duration = Configuration.GetValue<int>("ResponseCache:Home:Duration");
                //homeProfile.Location = Configuration.GetValue<ResponseCacheLocation>("ResponseCache:Location");
                //homeProfile.VaryByQueryKeys = new string[] { "page" };
                Configuration.Bind("ResponseCache:Home", homeProfile);
                options.CacheProfiles.Add("Home", homeProfile);

                options.ModelBinderProviders.Insert(0, new DecimalModelBinderProvider());

                // Tutte le action di tutti i controller richiedono autorizzazione
                AuthorizationPolicyBuilder policyBuilder = new AuthorizationPolicyBuilder();
                AuthorizationPolicy policy = policyBuilder.RequireAuthenticatedUser().Build();
                AuthorizeFilter filter = new AuthorizeFilter(policy);
                options.Filters.Add(filter);

            })// .SetCompatibilityVersion(CompatibilityVersion.Version_3_0) con .NET 5 è diventato superfluo
            .AddFluentValidation(options => {
                options.RegisterValidatorsFromAssemblyContaining<CourseCreateValidator>();
                //Per il validator personalizzato
                options.ConfigureClientsideValidation(clientSide => {
                    clientSide.Add(typeof(IRemotePropertyValidator), (context, description, validator) => new RemoteClientValidator(description, validator));
                });
            })
            // Con .NET 5 può essere spostato nel file launchSettings.json
            // "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES": "Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation"
            //#if DEBUG
            //.AddRazorRuntimeCompilation()
            //#endif
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
            services.AddSingleton<IEmailClient, MailKitEmailSender>();
            services.AddTransient<IImageValidator, MicrosoftAzureImageValidator>();
            services.AddSingleton<IAuthorizationHandler, CourseAuthorRequirementHandler>();

            // Policies
            services.AddAuthorization(options => 
            {
                // La mia policy che permette solo all'autore del corso la sua modifica
                options.AddPolicy("CourseAuthor", builder => 
                {
                    builder.Requirements.Add(new CourseAuthorRequirement());
                });
            });

            //Options prelevate dal file appsettings.json
            services.Configure<ConnectionStringsOptions>(Configuration.GetSection("ConnectionStrings"));
            services.Configure<CoursesOptions>(Configuration.GetSection("Courses"));
            services.Configure<MemoryCacheOptions>(Configuration.GetSection("MemoryCache"));
            services.Configure<KestrelServerOptions>(Configuration.GetSection("Kestrel"));
            services.Configure<SmtpOptions>(Configuration.GetSection("Smtp"));
            services.Configure<ImageValidationOptions>(Configuration.GetSection("ImageValidation"));
            services.Configure<UsersOptions>(Configuration.GetSection("Users"));

            // Servizio per il mapping tra datarow e viewmodel
            services.AddAutoMapper(typeof(Startup));

            //Validators di FluentValidation
            //Si possono registrare così nel caso ci sia bisogno di selezionare un ciclo di vita diverso da Transient
            //services.AddScoped<IValidator<CourseCreateInputModel>, CourseCreateValidator>();
            //services.AddSingleton<IValidator<CourseEditInputModel>, CourseEditValidator>();
        }

        private async Task<string> GetUserLocationFromFacebook(string accessToken)
        {
            using var client = new HttpClient();
            var response = await client.GetAsync($"https://graph.facebook.com/v9.0/me?access_token={accessToken}&fields=location");
            var body = await response.Content.ReadAsStreamAsync();
            var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("location").GetString("name");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        // Qui vengono inseriti i middleware. L'ordine dei middleware è importante. 
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
                //app.UseExceptionHandler("/Error");
                // Breaking change .NET 5: https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/5.0/middleware-exception-handler-throws-original-exception
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandlingPath = "/Error",
                    AllowStatusCode404Response = true
                });
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
