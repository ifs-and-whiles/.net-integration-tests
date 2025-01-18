namespace NetIntegrationTests.Services;

public class ApiCallException(string message) : Exception(message)
{
}