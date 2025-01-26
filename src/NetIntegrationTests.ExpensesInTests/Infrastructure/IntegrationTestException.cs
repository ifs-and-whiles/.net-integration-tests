namespace NetIntegrationTests.ExpensesInTests.Infrastructure;

public class IntegrationTestException : Exception
{
    public IntegrationTestException(string message) : base(message) { }

    public IntegrationTestException(Exception exception)
        : base("Test failed because of the following exception:", exception) { }
}