using System.Net;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.Api;

public class TestApiCallErrorException : Exception
{
    public string Error { get; }
    public HttpStatusCode? HttpStatusCode { get; }

    public TestApiCallErrorException(string error, int? httpStatusCode)
    {
        Error = error;
        HttpStatusCode = (HttpStatusCode?)httpStatusCode;
    }
}