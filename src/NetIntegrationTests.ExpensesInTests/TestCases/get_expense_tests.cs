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
public class get_expense_tests(HostFixture hostFixture, ITestOutputHelper iTestOutputHelper)
    : ExpensesTestFixture(hostFixture, iTestOutputHelper)
{
    [Fact]
    public async Task should_return_404_when_expense_not_found()
    {
        var response = async () => await Api.get_expense(new Contracts.Expenses.V1.Get.Request()
        {
            Id = Guid.NewGuid()
        }, DefaultBasicAuthApiUser);

        var exception = await response.Should().ThrowAsync<TestApiCallErrorException>();
        exception.And.HttpStatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task should_return_expense()
    {
        var user = new UserBuilder(this);
        
        UsersService
            .with_get_user(user.ToGetUserResponse())
            .start();
        
        var expense = await new ExpenseBuilder(this)
            .WithUserId(user.Id)
            .SaveExpenseAndWait();
        
        var response = await Api.get_expense(new Contracts.Expenses.V1.Get.Request()
        {
            Id = expense.Id
        }, DefaultBasicAuthApiUser);

        response.Should().BeEquivalentTo(expense.ToGetExpenseResponse(expense.Id));
    }
}