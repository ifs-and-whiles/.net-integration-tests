using AutoFixture;
using Flurl.Http;
using MassTransit;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Queue;
using Xunit.Abstractions;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;

public abstract class TestFixture : IAsyncLifetime
{
    public ITestOutputHelper TestOutput { get; }
    public AppSettings AppSettings { get; }
    protected IFlurlClient FlurlClient { get;  }
    protected Fixture Fixture { get; }
    protected IQueueEngine QueueEngine { get; }
    private IBus _bus;

    protected TestFixture(
        HostFixture hostFixture,
        ITestOutputHelper testOutput)
    {
        Fixture = new Fixture();
        TestOutput = testOutput;
        AppSettings = hostFixture.AppSettings;
        FlurlClient = hostFixture.FlurlClient;
        
    }
    
    public Task InitializeAsync()
    {
        throw new NotImplementedException();
    }

    public Task DisposeAsync()
    {
        throw new NotImplementedException();
    }
}