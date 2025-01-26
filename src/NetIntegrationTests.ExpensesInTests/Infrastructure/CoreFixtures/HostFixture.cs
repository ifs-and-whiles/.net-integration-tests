using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flurl.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Queue;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;

 public class HostFixture : IAsyncDisposable
    {
        public AppSettings AppSettings { get; set; }

        public IFlurlClient FlurlClient { get; } = new FlurlClient();
        public WebApplication App;

		public string TestQueue = "netintegrationtests-expenses-api-test-queue";
        public HostFixture()
        {
	        StartService();
        }

        public RabbitMq QueueEngine { get; set; }

        private void StartService()
        {
	        var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
	        {
		        EnvironmentName = "integrationtests"
	        });
	        
	        AppSettings = builder.Configuration.GetSection("Settings").Get<AppSettings>();
	        
	        var startup = new Startup(AppSettings);

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
		        });

	        builder.WebHost.UseUrls(AppSettings.WebEndpoint);

	        App = builder.Build();

	        startup.Configure(App, App.Environment);

	        App.Start();
        }

        public async ValueTask DisposeAsync()
        {
            await App.DisposeAsync();
            FlurlClient.Dispose();
        }
    }