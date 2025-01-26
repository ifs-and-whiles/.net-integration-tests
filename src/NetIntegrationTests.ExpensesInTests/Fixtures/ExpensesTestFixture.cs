using System.Net;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.ExpensesInTests.Fixtures.FakeServices;
using NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;
using Xunit.Abstractions;

namespace NetIntegrationTests.ExpensesInTests.Fixtures;

public class ExpensesTestFixture: TestFixture
{

	public BasicAuthApiUser DefaultBasicAuthApiUser { get; }
	protected UsersFakeService UsersService { get; }

	public ExpensesApi Api { get; }
	protected ExpensesDatabase Database { get; }
	public ExpensesTestFixture(HostFixture hostFixture, ITestOutputHelper iTestOutputHelper) 
		: base(hostFixture, iTestOutputHelper)
	{

		DefaultBasicAuthApiUser = new BasicAuthApiUser(AppSettings.BasicApiUser, AppSettings.BasicApiUserPassword);
		Api = new ExpensesApi(this, DefaultBasicAuthApiUser);
		UsersService = new UsersFakeService(this, AppSettings.UsersServicePath);
		Database = new ExpensesDatabase(this);
		
		ConfigureRabbitMq(hostFixture).Wait();
	}

	private async Task ConfigureRabbitMq(HostFixture hostFixture)
	{
		await QueueEngine.CreateQueueIfDoesNotExist(AppSettings.ServiceQueueName);
		await QueueEngine.CreateQueueIfDoesNotExist(hostFixture.TestQueue);

		await QueueEngine.PurgeQueues(
			AppSettings.ServiceQueueName,
			$"{AppSettings.ServiceQueueName}_error",
			hostFixture.TestQueue,
			$"{hostFixture.TestQueue}_error");
		
		await QueueEngine.BindExchangeToExchange(
			$"{AppSettings.ServiceQueueName}_error", 
			$"{hostFixture.TestQueue}_error");

		await QueueEngine.BindQueueToExchange(hostFixture.TestQueue,
			Contracts.Expenses.V1.Events.ExpenseCreatedEvent.ExchangeName);
	}

	public async Task wait_for_api_response<TResponse>(
		Func<Task<TResponse>> apiCallFunc,
		Action<TResponse> assertions)
	{
		await wait_for_passed_condition_or_throw_after_timeout(
			async () =>
			{

				var apiResult = await apiCallFunc();
				assertions(apiResult);

			},
			checkingIntervalInMilliseconds: 50,
			retryCount: 100);
	}



	public override async Task DisposeAsync()
	{
		await UsersService.DisposeAsync();
		await base.DisposeAsync();
	}


}