using Autofac;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NetIntegrationTests.UsersApi.Setup;
using Newtonsoft.Json;

namespace NetIntegrationTests.UsersApi;

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
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Users API", Version = "v1" });
            c.CustomSchemaIds(type => type.FullName.Replace("+", "."));
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
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Users API v1"));

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
    public string BasicApiUser { get; set; }
    public string BasicApiPassword { get; set; }
}
