using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents.Session;
using Serilog;
using WritableJsonConfiguration;

namespace SkillChat.Server
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Log.Information("Building host");
                var host = CreateHostBuilder(args).Build();

                Log.Information("Start RavenDB");
                host.Services.GetRequiredService<IAsyncDocumentSession>();
                
                Log.Information("Starting up web host");
                host.Run();
                Log.Information("Shutting down web host");
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(builder =>
                {
                    builder.Add(new WritableJsonConfigurationSource { Path = "appsettings.json" });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration));
    }
}
