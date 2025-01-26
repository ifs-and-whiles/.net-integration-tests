using AutoFixture;
using Dapper;
using DapperExtensions;
using Flurl;
using Flurl.Http;
using MassTransit;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NetIntegrationTests.Database;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Api;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Database;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Queue;
using Newtonsoft.Json;
using Npgsql;
using Polly;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;
using JsonDocument = System.Text.Json.JsonDocument;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;

public abstract class TestFixture : IAsyncLifetime
    {
        public ITestOutputHelper TestOutput { get; }
        public AppSettings AppSettings { get; private set; }
        protected IFlurlClient FlurlClient { get; set; }
        protected Fixture Fixture { get; private set; }

        protected static IQueueEngine QueueEngine;
        
        private IBus _bus;
		
        protected TestFixture(HostFixture hostFixture, ITestOutputHelper testOutput)
        {
	        DapperExtensions.DapperAsyncExtensions.DefaultMapper = typeof(ExpenseEntity);
	        DapperAsyncExtensions
		        .SetMappingAssemblies([typeof(TestFixture).Assembly, typeof(ExpenseEntity).Assembly]);
	        DapperExtensions.DapperExtensions.DefaultMapper = typeof(ExpenseEntity);
	        DapperExtensions.DapperExtensions
		        .SetMappingAssemblies([typeof(TestFixture).Assembly, typeof(ExpenseEntity).Assembly]);

	        Fixture = new Fixture();
            TestQueue = hostFixture.TestQueue;
            TestOutput = testOutput;

            AppSettings = hostFixture.AppSettings;
            FlurlClient = hostFixture.FlurlClient;


			DbCleaner.CleanDatabase(AppSettings.ConnectionString);

            QueueEngine = RabbitMq.CreateAsync(
	            hostFixture.App.Configuration[RabbitMqConfig.RabbitMqHostName],
	            hostFixture.App.Configuration[RabbitMqConfig.RabbitMqUsername],
	            hostFixture.App.Configuration[RabbitMqConfig.RabbitMqPassword],
	            hostFixture.App.Configuration[RabbitMqConfig.RabbitMqPort]).Result;

            _bus = (IBus)hostFixture.App.Services.GetService(typeof(IBus));

            CreateLogger();
		}

        public string TestQueue { get; set; }

        private void CreateLogger()
        {
	        var configuration = new LoggerConfiguration()
		        .WriteTo.TestOutput(TestOutput, LogEventLevel.Debug)
		        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Debug);

	        Log.Logger = configuration.CreateLogger();
        }

		public Task InitializeAsync()
        {
          return  Task.CompletedTask;
        }
		
        public virtual Task DisposeAsync()
		{
			QueueEngine?.Dispose();
            return Task.CompletedTask;
        }

        public void run_db_command(string command)
        {
            using (var connection = new NpgsqlConnection(AppSettings.ConnectionString))
            {
	            connection.Open();
	            connection.Execute(command);
            }
        }

        protected async Task<string> send_local_message<TMessage>(TMessage message)
        {
            var correlationId = Guid.NewGuid().ToString();
			
            await _bus.Send(message);

            return correlationId;
        }


        public async Task<TResult> get_from_sut_api<TResult>(
            string path,
            string userName,
            string password,
            params (string key, object? value)[] queryParams)
        {
            var request = AppSettings.WebEndpoint.AppendPathSegment(path);

            foreach (var queryParam in queryParams)
            {
                request.SetQueryParam(queryParam.key, queryParam.value);
            }

            try
            {
                return await request
                 .WithBasicAuth(userName, password)
                    .GetJsonAsync<TResult>();
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.Call.Response.GetStringAsync();
                throw new TestApiCallErrorException(responseString, ex.StatusCode);
            }
        }

        public async Task delete_from_sut_api(
         string path,
         string userName,
         string password,
         params (string key, object value)[] queryParams)
        {
            var request = AppSettings.WebEndpoint.AppendPathSegment(path);

            foreach (var queryParam in queryParams)
            {
                request.SetQueryParam(queryParam.key, queryParam.value);
            }

            try
            {
                await request
                    .WithBasicAuth(userName, password)
                    .DeleteAsync();
            }
            catch (FlurlHttpException ex)
            {
                var responseString = await ex.Call.Response.GetStringAsync();
                throw new TestApiCallErrorException(responseString, ex.StatusCode);
            }
        }

        public async Task post_to_sut_api<TRequest>(
               string path,
               TRequest request,
               string userName,
               string password,
               Action<TRequest> requestModifier = null)
        {
            requestModifier?.Invoke(request);

            var response = await FlurlClient
             .Request(
	             AppSettings.WebEndpoint,
              path)
                .AllowAnyHttpStatus()
                .WithBasicAuth(userName, password)
                .PostJsonAsync(request);

            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                var error = await response.GetStringAsync();
                throw new TestApiCallErrorException(error, response.StatusCode);
            }
        }
        
        public async Task<TResponse> put_to_sut_api_with_response<TRequest, TResponse>
        (string path,
            TRequest request,
            string userName,
            string password,
            Action<TRequest> requestModifier = null)
        {
            requestModifier?.Invoke(request);

            var response = await FlurlClient
                .WithBasicAuth(userName, password)
                .AllowAnyHttpStatus()
                .Request(
	                AppSettings.WebEndpoint,
                    path)
                .PutJsonAsync(request);

            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                var error = await response.GetStringAsync();
                throw new TestApiCallErrorException(error, response.StatusCode);
            }

            var result = await response.GetStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(result);
        }

        public async Task<TResponse> post_to_sut_api_with_response<TRequest, TResponse>
               (string path,
               TRequest request,
               string userName,
               string password,
               Action<TRequest> requestModifier = null)
        {
            requestModifier?.Invoke(request);

            var response = await FlurlClient
               .WithBasicAuth(userName, password)
               .AllowAnyHttpStatus()
               .Request(AppSettings.WebEndpoint, path)
               .PostJsonAsync(request);

            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                var error = await response.GetStringAsync();
                throw new TestApiCallErrorException(error, response.StatusCode);
            }

            var result = await response.GetStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(result);
        }
        
        protected async Task<List<TMessage>> wait_for_many_messages_in_queue<TMessage>(
            string queueName,
            Func<TMessage, bool> condition,
            int expectedNumberOfMessages)
            where TMessage : class
        {
            List<TMessage> expectedMessages = new List<TMessage>();

            await wait_for_passed_condition_or_throw_after_timeout(async () =>
            {
                var rawMessage = await QueueEngine.ReadRawMessageFromQueue(
	                queueName);

                // The queue does not contain messages
                if (string.IsNullOrEmpty(rawMessage))
					throw new IntegrationTestException("Message was null");
                
                var deserializedMessage = JsonConvert.DeserializeObject<TMessage>(rawMessage);

                if (deserializedMessage != null && condition(deserializedMessage))
                    expectedMessages.Add(deserializedMessage);

                if (expectedMessages.Count != expectedNumberOfMessages)
                {
	                throw new IntegrationTestException("Number of messages did not match");
				}
            },
                checkingIntervalInMilliseconds: 1000,
                retryCount: 20);

            return expectedMessages;
        }

		public async Task  wait_for_message_in_queue<TMessage>(
			string queueName, 
			Action<TMessage> condition)
		where TMessage : class
		{
			Exception relevantError = null;

			try
			{
				await wait_for_passed_condition_or_throw_after_timeout(async () =>
					{
						var rawMessage = await QueueEngine.ReadRawMessageFromQueue(
							queueName);

						if (string.IsNullOrEmpty(rawMessage))
							throw new IntegrationTestException(" The queue does not contain expected messages");
						
						var expectedMessage = JsonConvert.DeserializeObject<TMessage>(rawMessage);

						if (expectedMessage == null)
						{
							throw new IntegrationTestException("Message was null");
						}

						try
						{
							condition(expectedMessage);
						}
						catch (Exception e)
						{
							relevantError = e;
							throw;
						}

					},
					checkingIntervalInMilliseconds: 100,
					retryCount: 100);
			}
			catch (Exception)
			{
				if (relevantError != null)
				{
					throw relevantError;
				}
				else
				{
					throw;
				}
			}
		}

		public void wait_for_passed_condition_or_throw_after_timeout(
            Action checkingAction,
            int checkingIntervalInMilliseconds,
            int retryCount)
		{
			Exception relevantError = null;

			var conditionFulfilled = Policy.HandleResult<bool>(result => result == false)
				.WaitAndRetry(
					retryCount: retryCount,
					sleepDurationProvider: retryAttempt =>
						TimeSpan.FromMilliseconds(checkingIntervalInMilliseconds))
				.Execute(() =>
				{
					try
					{
						checkingAction();
						return true;
					}
					catch (Exception)
					{
						return false;
					}
				});

			if (!conditionFulfilled)
			{
				checkingAction();
			}

		}

		public async Task wait_for_passed_condition_or_throw_after_timeout(
			Func<Task> checkingAction,
			int checkingIntervalInMilliseconds,
			int retryCount)
		{
			Exception relevantError = null;
			var conditionFulfilled = Policy.HandleResult<bool>(result => result == false)
					.WaitAndRetryAsync(
						retryCount: retryCount,
						sleepDurationProvider: retryAttempt =>
							TimeSpan.FromMilliseconds(checkingIntervalInMilliseconds))
					.ExecuteAsync(async () =>
					{
						try
						{
							await checkingAction();
							return true;
						}
						catch (Exception e)
						{
							relevantError = e;
							return false;
						}
					});

				if (! (await conditionFulfilled))
				{
					 await checkingAction();
				}


		}
		
    }