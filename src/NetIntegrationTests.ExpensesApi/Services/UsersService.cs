using System.Net;
using Flurl;

namespace NetIntegrationTests.Services;

using Flurl.Http;

public class UsersService(AppSettings appSettings)
{
    private readonly string _usersServicePath = appSettings.UsersServicePath;
    
    public async Task<Contracts.Users.V1.Get.Response?> GetUser(Guid userId)
    {
        var response = await _usersServicePath
            .AppendPathSegment("users/get-user")
            .AllowAnyHttpStatus()
            .PostJsonAsync(new Contracts.Users.V1.Get.Request() { Id = userId });

        if (response.ResponseMessage.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.ResponseMessage.StatusCode == HttpStatusCode.OK)
        {
            return await response.GetJsonAsync<Contracts.Users.V1.Get.Response>();
        }
        
        throw new ApiCallException($"Request to users service failed. Status code: {response.StatusCode}. " +
                                   $"Response content: {await response.ResponseMessage.Content.ReadAsStringAsync()}");
    }

    public async Task IncrementExpensesCount(Guid userId)
    {
        var response = await _usersServicePath
            .AppendPathSegment("users/increment-expenses-count")
            .PostJsonAsync(new Contracts.Users.V1.IncrementExpensesCount.Request() { UserId = userId });
        
        if (response.ResponseMessage.StatusCode != HttpStatusCode.OK)
        {
            throw new ApiCallException($"Request to users service failed. Status code: {response.StatusCode}. " +
                                       $"Response content: {await response.ResponseMessage.Content.ReadAsStringAsync()}");
        }
    }
}

public static class Contracts
{
    public static class Users
    {
        public static class V1
        {
            public static class IncrementExpensesCount
            {
                public class Request
                {
                    public Guid UserId { get; set; }
                }
            }
            public static class Get
            {
                public class Request
                {
                    public Guid Id { get; set; }
                }
                
                public class Response
                {
                    public string Name { get; set; }
                    public Guid Id { get; set; }
                    public int ExpensesCount { get; set; }
                    public int MaxExpenseCount { get; set; }
                }
            }
        }
    }
}