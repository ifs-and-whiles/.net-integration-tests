using System.Net;
using NetIntegrationTests.ExpensesInTests.Infrastructure.Api;
using NetIntegrationTests.Services;
using HttpMethod = NetIntegrationTests.ExpensesInTests.Infrastructure.Api.HttpMethod;

namespace NetIntegrationTests.ExpensesInTests.Fixtures.FakeServices;

public class UsersFakeService 
{
    private readonly ExpensesTestFixture _fixture;
    private readonly string _url;
    private ApiMock _apiMock;
    private List<Endpoint> _endpoints = new List<Endpoint>();
    private string _username;

    public UsersFakeService(
        ExpensesTestFixture fixture,
        string url)
    {
        _fixture = fixture;
        _url = url;
    }
    
    public UsersFakeService with_user_not_found(
        Contracts.Users.V1.Get.Response user)
    {
        var endpoint = new Endpoint
        {
            HttpCode = HttpStatusCode.NotFound,
            HttpMethod = HttpMethod.Post,
            Result = user,
            Url = $"/users/get-user"
        };
			
        _endpoints.Add(endpoint);

        return this;
    }
    
    public UsersFakeService with_get_user(
        Contracts.Users.V1.Get.Response user)
    {
        var endpoint = new Endpoint
        {
            HttpCode = HttpStatusCode.OK,
            HttpMethod = HttpMethod.Post,
            Result = user,
            Url = $"/users/get-user"
        };
			
        _endpoints.Add(endpoint);

        return this;
    }

    public void start()
    {
        _apiMock = new ApiMock(
            _url,
            _endpoints.Cast<IEndpoint>().ToArray());

        _apiMock.Start();
    }

    public List<PostEndpointRequest> GetPostRequests()
    {
        return _apiMock.GetRequestedMethods();
    }
    
    public async Task DisposeAsync()
    {
        if (_apiMock != null)
        {
            await _apiMock.DisposeAsync();
        }
    }
}