using Autofac;
using MassTransit;
using NetIntegrationTests.UsersApi.Database;

namespace NetIntegrationTests.UsersApi.Setup;

public class AutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UsersRepository>().AsSelf();
    }
}