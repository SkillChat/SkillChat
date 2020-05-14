using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents.Session;
using Raven.Embedded;
using Serilog;

namespace SkillChat.Server
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Log.Information("Building web host");
                var host = CreateWebHostBuilder(args).Build();

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

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration));
    }
}
