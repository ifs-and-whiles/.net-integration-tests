using MassTransit;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.Setup;

namespace NetIntegrationTests;

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication("BasicAuthentication")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

        services.AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration Tests API", Version = "v1" });
            c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
        });
        
        var settings = Configuration.GetSection("Settings").Get<AppSettings>();
        services.AddMassTransit(x =>
        {
            x.AddConsumer<ExpenseCreatedConsumer>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(settings.RabbitMqHost), h =>
                {
                    h.Username(settings.RabbitMqUsername);
                    h.Password(settings.RabbitMqPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        var mySettings = Configuration.GetSection("Settings").Get<AppSettings>();
        builder.RegisterInstance(mySettings);

        builder.RegisterModule(new AutofacModule());
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UsePathBase("/api");
        
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Integration Tests API v1"));

        app.UseRouting();
        app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
 
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

public class AppSettings
{
    public string ConnectionString { get; set; }
    public string RabbitMqHost { get; set; }
    public string RabbitMqUsername { get; set; }
    public string RabbitMqPassword { get; set; }
    public string UsersServicePath { get; set; }
    public string BasicApiUser { get; set; }
    public string BasicApiPassword { get; set; }
}
