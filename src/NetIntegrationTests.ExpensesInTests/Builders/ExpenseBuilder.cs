using FluentAssertions;
using NetIntegrationTests.Database;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.ExpensesInTests.Fixtures;

namespace NetIntegrationTests.ExpensesInTests.Builders;

public class ExpenseBuilder(ExpensesTestFixture fixture)
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; } = 10.00M;
    public string Name { get; set; } = "Test Expense";
    public Guid UserId { get; set; } = Guid.NewGuid();
    
    public ExpenseBuilder WithName(string name)
    {
        Name = name;
        return this;
    }

    public ExpenseBuilder WithAmount(decimal amount)
    {
        Amount = amount;
        return this;
    }

    public ExpenseBuilder WithUserId(Guid userId)
    {
        UserId = userId;
        return this;
    }

    public Contracts.Expenses.V1.Get.Response ToGetExpenseResponse(Guid expenseId)
    {
        return new Contracts.Expenses.V1.Get.Response()
        {
            Id = expenseId,
            Name = Name,
            Amount = Amount,
            UserId = UserId
        };
    }

    public ExpenseEntity ToExpenseEntity(Guid expenseId)
    {
        return new ExpenseEntity()
        {
            Id = expenseId,
            Name = Name,
            Amount = Amount,
            UserId = UserId
        };
    }

    public ExpenseBuilder WithId(Guid id)
    {
        Id = id;
        return this;
    }

    public async Task<Guid> CreateExpense()
    {
        var response = await fixture.Api.save_expense(
            new Contracts.Expenses.V1.Create.Request()
            {
                Name = Name,
                Amount = Amount,
                UserId = UserId
            }, fixture.DefaultBasicAuthApiUser);
        
        Id = response.Id;
        
        return response.Id;
    }

    public Contracts.Expenses.V1.Create.Request ToCreateExpense()
    {
        return new Contracts.Expenses.V1.Create.Request()
        {
            Name = Name,
            Amount = Amount,
            UserId = UserId
        };
    }
    
    public Contracts.Expenses.V1.Events.ExpenseCreatedEvent ToCreatedExpenseEntity()
    {
        return new Contracts.Expenses.V1.Events.ExpenseCreatedEvent()
        {
            Id = Id,
            UserId = UserId
        };
    }

    public async Task<ExpenseBuilder> SaveExpenseAndWait()
    {
        var response = await fixture.Api.save_expense(ToCreateExpense(), fixture.DefaultBasicAuthApiUser);

        Id = response.Id;
        
        await fixture.wait_for_message_in_queue<Contracts.Expenses.V1.Events.ExpenseCreatedEvent>
        (fixture.TestQueue, message =>
        {
            message.Should().BeEquivalentTo(
                ToCreatedExpenseEntity());
        }); 
        
        return this;
    }


}