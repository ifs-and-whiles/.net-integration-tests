using System.Net;
using FluentAssertions;
using NetIntegrationTests.ExpensesInTests.Builders;
using NetIntegrationTests.ExpensesInTests.Fixtures;
using NetIntegrationTests.ExpensesInTests.Infrastructure;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Api;
using NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;
using NetIntegrationTests.Services;
using Xunit.Abstractions;
using Contracts = NetIntegrationTests.Expenses.Contracts;

namespace NetIntegrationTests.ExpensesInTests.TestCases;

[Collection(IntegrationTestsCollection.Name)]
public class delete_expense_tests(HostFixture hostFixture, ITestOutputHelper iTestOutputHelper)
    : ExpensesTestFixture(hostFixture, iTestOutputHelper)
{
    [Fact]
    public async Task should_return_404_when_expense_not_found()
    {
        var response = async () => await Api.delete_expense(new Contracts.Expenses.V1.Delete.Request()
        {
            Id = Guid.NewGuid()
        }, DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task should_delete_expense()
    {
        var user = new UserBuilder(this);
        
        UsersService
            .with_get_user(user.ToGetUserResponse())
            .start();
        
        var expense = await new ExpenseBuilder(this)
            .WithUserId(user.Id)
            .SaveExpenseAndWait();
        
        await Api.delete_expense(new Contracts.Expenses.V1.Delete.Request()
        {
            Id = expense.Id
        }, DefaultBasicAuthApiUser);
        
        var response = async () => await Api.get_expense(new Contracts.Expenses.V1.Get.Request()
        {
            Id = Guid.NewGuid()
        }, DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}