using System.Net;
using FluentAssertions;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.ExpensesInTests.Builders;
using NetIntegrationTests.ExpensesInTests.Fixtures;
using NetIntegrationTests.ExpensesInTests.Infrastructure;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Api;
using NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;
using Xunit.Abstractions;

namespace NetIntegrationTests.ExpensesInTests.TestCases;

[Collection(IntegrationTestsCollection.Name)]
public class create_expenses_tests(HostFixture hostFixture, ITestOutputHelper iTestOutputHelper)
    : ExpensesTestFixture(hostFixture, iTestOutputHelper)
{
    [Fact]
    public async Task should_return_400_when_expense_name_is_empty()
    {
        var user = new UserBuilder(this);
        
        UsersService
            .with_get_user(user.ToGetUserResponse())
            .start();
        
        var expense = new ExpenseBuilder(this)
            .WithName("")
            .WithUserId(user.Id);
        
        var response = async ()=>   await Api.save_expense(expense.ToCreateExpense(), DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.And.Error.Should().Be("Name is required");
    }

    [Fact]
    public async Task should_return_400_when_expense_amount_is_negative()
    {
        var user = new UserBuilder(this);
        
        UsersService
            .with_get_user(user.ToGetUserResponse())
            .start();
        
        var expense = new ExpenseBuilder(this)
            .WithAmount(-3)
            .WithUserId(user.Id);
        
        var response = async ()=>   await Api.save_expense(expense.ToCreateExpense(), DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.And.Error.Should().Be("Amount must be greater than 0");
    }

    [Fact]
    public async Task should_return_400_when_user_does_not_exist()
    {
        var user = new UserBuilder(this);
        
        UsersService
            .with_user_not_found(user.ToGetUserResponse())
            .start();
        
        var expense = new ExpenseBuilder(this)
            .WithUserId(user.Id);
        
        var response = async ()=>   await Api.save_expense(expense.ToCreateExpense(), DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.And.Error.Should().Be("User does not exist");
    }
    
    [Fact]
    public async Task should_return_400_when_user_reached_max_expense_count()
    {
        var user = new UserBuilder(this)
            .WithExpensesCount(10)
            .WithMaxExpenseCount(10);
        
        UsersService
            .with_get_user(user.ToGetUserResponse())
            .start();
        
        var expense = new ExpenseBuilder(this)
            .WithUserId(user.Id);
        
        var response = async ()=>   await Api.save_expense(expense.ToCreateExpense(), DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.BadRequest);
        exception.And.Error.Should().Be("User has reached max expense count");
    }

    [Fact]
    public async Task should_create_expense_and_emit_event()
    {
        var user = new UserBuilder(this);
        
        UsersService
            .with_get_user(user.ToGetUserResponse())
            .start();
        
        var expense = new ExpenseBuilder(this)
            .WithUserId(user.Id);
        
        var response = await Api.save_expense(expense.ToCreateExpense(), DefaultBasicAuthApiUser);

        response.Should().NotBeNull();

        expense.WithId(response.Id);
        
        var createdExpense = await Api.get_expense(new Contracts.Expenses.V1.Get.Request()
        {
            Id = response.Id
        }, DefaultBasicAuthApiUser);

        createdExpense.Should().BeEquivalentTo(expense.ToGetExpenseResponse(response.Id));
        
        await wait_for_message_in_queue<Contracts.Expenses.V1.Events.ExpenseCreatedEvent>
        (TestQueue, message =>
        {
            message.Should().BeEquivalentTo(
                expense.ToCreatedExpenseEntity());
        });
    }
    
    



}