namespace NetIntegrationTests.ExpensesInTests.Fixtures;

public class ExpensesApi(BasicAuthApiUser basicAuthApiUser, ExpensesTestFixture fixture)
{
    private readonly BasicAuthApiUser _basicAuthApiUser = basicAuthApiUser;
    private readonly ExpensesTestFixture _fixture = fixture;
    
    
}