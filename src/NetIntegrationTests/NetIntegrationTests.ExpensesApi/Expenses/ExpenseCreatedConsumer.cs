using NetIntegrationTests.Database;
using NetIntegrationTests.Services;

namespace NetIntegrationTests.Expenses;

using MassTransit;

public class ExpenseCreatedEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
}

public class ExpenseCreatedConsumer(UsersService usersService) : IConsumer<ExpenseCreatedEvent>
{
    public async Task Consume(ConsumeContext<ExpenseCreatedEvent> context)
    {
        await usersService.IncrementExpensesCount(context.Message.UserId);
    }
}