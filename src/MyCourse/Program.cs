using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MyCourse
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Log.Information("Starting web host");
                //string firstArgument = args.FirstOrDefault();
                CreateWebHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                // .ConfigureWebHostDefaults(webHostBuilder =>
                // {
                //     webHostBuilder.UseStartup<Startup>();
                // })
                .UseStartup<Startup>()
                .UseSerilog((webHostBuilderContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(webHostBuilderContext.Configuration);
                })
                //.UseStartup<Startup>()

                //Se volessi configurare la DI in un'applicazione console userei:
                //.ConfigureServices

                //Posso ridefinire l'elenco dei provider di default
                /*.ConfigureLogging((context, builder) => {
                    builder.ClearProviders();
                    builder.AddConsole();
                    builder.Add...;
                })*/

                //Posso ridefinire l'elenco delle fonti di configurazione con ConfigureAppConfiguration
                /*.ConfigureAppConfiguration((context, builder) => {
                    builder.Sources.Clear();
                    builder.AddJsonFile("appsettings.json", optional:true, reloadOnChange: true);
                    builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    //Qui altre fonti...
                })*/
                ;
    }
}
