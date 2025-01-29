# .NET Integration Tests

## 📌 Project Overview
This repository demonstrates how to write comprehensive integration tests in C# for a .AspNet Core application. The tests are designed to validate the entire solution by:

- **Utilizing a database**: Ensuring data persistence and retrieval operations function correctly.
- **Starting the service API**: Allowing all requests to interact with the running application without any mocks.
- **Incorporating a functioning message queue**: Testing the application's messaging components.
- **Interacting with dependent HTTP services**: The running application makes real requests to dependent services as if they actually existed.

These tests are efficient, as the API application is initialized only once, and they provide reliable results by verifying the application's behavior along with all its dependencies. They are also **fast**, as they do not require re-initializing the database, API, or other components for each test, which is a key advantage. Additionally, they do not use **any mocks** in the code, making maintenance easy—there is no need to adjust the tests with every code change, only when the application's external contract changes.

## 🚀 Example of the tests

```csharp
[Fact]
public async Task should_create_expense_and_emit_event()
{
    var user = new UserBuilder(this);

    // Start http users service and return user model from get-user api method
    UsersService
        .with_get_user(user.ToGetUserResponse())
        .start();
    
    var expense = new ExpenseBuilder(this)
        .WithUserId(user.Id);

    // Http call to main API to save expense
    var response = await Api.save_expense(expense.ToCreateExpense(), DefaultBasicAuthApiUser);

    response.Should().NotBeNull();

    expense.WithId(response.Id);

    // Http call to main API to get previously created expense from DB
    var createdExpense = await Api.get_expense(new Contracts.Expenses.V1.Get.Request()
    {
        Id = response.Id
    }, DefaultBasicAuthApiUser);

    createdExpense.Should().BeEquivalentTo(expense.ToGetExpenseResponse(response.Id));

    // Wait for message in the queue that Expense has been created. Main API application emits the event after user creation in DB
    await wait_for_message_in_queue<Contracts.Expenses.V1.Events.ExpenseCreatedEvent>
    (
        TestQueue, 
        message =>
        {
            message.Should().BeEquivalentTo(
                expense.ToCreatedExpenseEntity()
            );
        }
    );
}
```

## 📚 How it works

The biggest challenge in writing good tests is verifying the entire execution path as accurately as possible (ideally, along with all dependencies) while minimizing the need for frequent test modifications. Therefore, the main principle of these tests is to utilize a **running API, message queue, and database**, eliminating the need for mocks that simulate the behavior of original components.  

### **Step 1: Running the API with Dependencies**  
The first step is to start the application's API along with all its dependencies, but using a **configuration file specifically created in the test project**, where I modify values such as the **ConnectionString**.  
```csharp
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
```

### **Step 2: Setting Up the Database**  
The second step involves configuring the database and its access within the tests.  
To achieve this:  
- I create the database using **Docker Compose**.  
- The database schema must then be set up manually or with a chosen tool (this is not part of this project).  
- Finally, I enable access to the database, allowing test code to interact with it and verify data.  
```csharp
public async Task wait_for_entities<TEntity>(
    Action<List<TEntity>> assertions) where TEntity : class
{
    await fixture.wait_for_passed_condition_or_throw_after_timeout(
        async ()  => await do_all_entities_meet_assertions(assertions),
        checkingIntervalInMilliseconds: 500,
        retryCount: 15
    );
}
```

### **Step 3: Configuring RabbitMQ**  
The third step is setting up the **RabbitMQ message queue**.  
- I can create any queue and listen for or capture any message sent from the tested application.
```csharp
private async Task ConfigureRabbitMq(HostFixture hostFixture)
{
    await QueueEngine.CreateQueueIfDoesNotExist(AppSettings.ServiceQueueName);
    await QueueEngine.BindQueueToExchange(
        hostFixture.TestQueue,
        Contracts.Expenses.V1.Events.ExpenseCreatedEvent.ExchangeName
    );
}
```
This enables verification of outgoing messages from the application.  

- Message interception in tests is an **asynchronous process** so tests can wait for the message.

```csharp
await wait_for_message_in_queue<Contracts.Expenses.V1.Events.ExpenseCreatedEvent>
(
    TestQueue, 
    message =>
    {
        message.Should().BeEquivalentTo(
            expense.ToCreatedExpenseEntity()
        );
    }
);
```

### **Step 4: Running dependent HTTP services**  
The fourth step is to start the dependent HTTP services that the application communicates with.

Any use of mocks instead of real implementations within the application introduces a potential risk that the code will not be fully tested. To mitigate this, I start dependent HTTP services as running applications that the tested application can connect to, just as it would during normal operation.

```csharp
UsersService
    .with_get_user(user.ToGetUserResponse())
    .start();
```

Negative scenario, service returns 404
```csharp
UsersService
    .with_user_not_found(user.ToGetUserResponse())
    .start();
```

## 🚀 Running the Tests
### 1. Start the Test Environment
Before running the tests, ensure that all required services (PostgreSQL database, RabbitMQ) are available. You can do this using Docker:
Go to .net-integration-tests/tree/main/src/NetIntegrationTests.ExpensesInTests and run:
```sh
docker-compose up -d
```

### 2. Run the Tests
```sh
dotnet test
```

