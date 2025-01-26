using NetIntegrationTests.ExpensesInTests.Infrastructure.CoreFixtures;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure;

[CollectionDefinition(Name)]
public class IntegrationTestsCollection : ICollectionFixture<HostFixture>
{
    public const string Name = "IntegrationTests";

    
}