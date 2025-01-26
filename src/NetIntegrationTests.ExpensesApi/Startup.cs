using MassTransit;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.Setup;
using RabbitMQ.Client;

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

public class Startup(AppSettings appSettings)
{
    private readonly AppSettings _appSettings = appSettings;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication("BasicAuthentication")
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

        services
            .AddControllers()
            .AddApplicationPart(typeof(ExpensesController).Assembly)
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration Tests API", Version = "v1" });
            
            c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                Description = "Input your username and password to access this API"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "basic"
                        }
                    },
                    new string[] { }
                }
            });
            
            c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
        });
        
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(new Uri(_appSettings.RabbitMqHost), h =>
                {
                    h.Username(_appSettings.RabbitMqUsername);
                    h.Password(_appSettings.RabbitMqPassword);
                });

                cfg.ReceiveEndpoint(_appSettings.ServiceQueueName, e =>
                {
                    
                });
            });
        });
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        builder.RegisterInstance(_appSettings);

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
    public string BasicApiUserPassword { get; set; }
    public string WebEndpoint { get; set; }
    public string ServiceQueueName { get; set; }
}
