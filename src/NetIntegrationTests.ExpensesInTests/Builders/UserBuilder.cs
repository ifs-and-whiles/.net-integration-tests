using NetIntegrationTests.ExpensesInTests.Fixtures;
using NetIntegrationTests.Services;

namespace NetIntegrationTests.ExpensesInTests.Builders;

public class UserBuilder(ExpensesTestFixture fixture)
{
    public string Name { get; set; } = "Test User";
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ExpensesCount { get; set; } = 0;
    public int MaxExpenseCount { get; set; } = 10;

    public UserBuilder WithName(string name)
    {
        Name = name;
        return this;
    }

    public UserBuilder WithId(Guid id)
    {
        Id = id;
        return this;
    }

    public UserBuilder WithExpensesCount(int expensesCount)
    {
        ExpensesCount = expensesCount;
        return this;
    }

    public UserBuilder WithMaxExpenseCount(int maxExpenseCount)
    {
        MaxExpenseCount = maxExpenseCount;
        return this;
    }

    public Contracts.Users.V1.Get.Response ToGetUserResponse()
    {
        return new Contracts.Users.V1.Get.Response()
        {
            Id = Id,
            Name = Name,
            ExpensesCount = ExpensesCount,
            MaxExpenseCount = MaxExpenseCount
        };
    }

}