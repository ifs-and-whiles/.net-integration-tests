using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace NetIntegrationTests;

public class Runner
{
    public static void Main(string[] args)
    {
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables();
            
            var appSettings = builder.Configuration.GetSection("Settings").Get<AppSettings>();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .Enrich.WithProperty("ServiceName", "NetIntegrationTests.ExpensesApi")
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Error)
                .MinimumLevel.Override("System", LogEventLevel.Error)
                .MinimumLevel.Debug()
                .CreateLogger();

            var startup = new Startup(appSettings);

            builder
                .Host
                .ConfigureServices(services =>
                {
                    startup.ConfigureServices(services);
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(containerBuilder =>
                {
                    startup.ConfigureContainer(containerBuilder);
                })
                .UseSerilog(logger: Log.Logger);
            
            builder
                .WebHost
                .UseUrls(appSettings.WebEndpoint)
                .UseShutdownTimeout(TimeSpan.FromMinutes(1))
                .CaptureStartupErrors(true);
            
            var app = builder.Build();
            
            startup.Configure(app, app.Environment);

            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}