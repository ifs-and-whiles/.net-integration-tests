using Autofac;
using MassTransit;
using NetIntegrationTests.Database;
using NetIntegrationTests.Expenses;
using NetIntegrationTests.Services;

namespace NetIntegrationTests.Setup;

public class AutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ExpensesRepository>().AsSelf();
        builder.RegisterType<UsersService>().AsSelf();
        builder.RegisterType<ExpenseCreatedConsumer>()
            .As<IConsumer<ExpenseCreatedEvent>>();
    }
}