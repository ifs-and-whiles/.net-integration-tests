# .NET Integration Tests

## 📌 Project Overview
This repository demonstrates how to write comprehensive integration tests in C# for a .AspNet Core application. The tests are designed to validate the entire solution by:

- **Utilizing a database**: Ensuring data persistence and retrieval operations function correctly.
- **Starting the service API**: Allowing all requests to interact with the running application without any mocks.
- **Incorporating a functioning message queue**: Testing the application's messaging components.
- **Interacting with dependent HTTP services**: Tested application makes real requests to dependent services as if they actually existed.

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

    // Wait for message in the queue that Expense has been created. Main API application
