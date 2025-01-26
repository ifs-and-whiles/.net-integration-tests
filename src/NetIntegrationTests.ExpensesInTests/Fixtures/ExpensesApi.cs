using NetIntegrationTests.Expenses;

namespace NetIntegrationTests.ExpensesInTests.Fixtures;

public class ExpensesApi(ExpensesTestFixture fixture, BasicAuthApiUser basicAuthApiUser)
{
    private readonly BasicAuthApiUser _basicAuthApiUser = basicAuthApiUser;
    
    public Task<Contracts.Expenses.V1.Create.Response> save_expense(
        Contracts.Expenses.V1.Create.Request request,
        BasicAuthApiUser apiUser)
    {
        return fixture.post_to_sut_api_with_response<Contracts.Expenses.V1.Create.Request, Contracts.Expenses.V1.Create.Response>(
            path: $"api/expenses/create-expense",
            request: request,
            userName: apiUser.Username,
            password: apiUser.Password);
    }

    public Task<Contracts.Expenses.V1.Get.Response> get_expense(
        Contracts.Expenses.V1.Get.Request request,
        BasicAuthApiUser apiUser)
    {
        return fixture.post_to_sut_api_with_response<Contracts.Expenses.V1.Get.Request, Contracts.Expenses.V1.Get.Response>(
            path: $"api/expenses/get-expense",
            request: request,
            userName: apiUser.Username,
            password: apiUser.Password);
    }
    
    public Task delete_expense(
        Contracts.Expenses.V1.Delete.Request request,
        BasicAuthApiUser apiUser)
    {
        return fixture.post_to_sut_api<Contracts.Expenses.V1.Delete.Request>(
            path: $"api/expenses/delete-expense",
            request: request,
            userName: apiUser.Username,
            password: apiUser.Password);
    }
}