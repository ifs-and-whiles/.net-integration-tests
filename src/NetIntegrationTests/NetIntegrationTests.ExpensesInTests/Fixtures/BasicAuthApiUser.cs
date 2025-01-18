namespace NetIntegrationTests.ExpensesInTests.Fixtures;

public class BasicAuthApiUser(string username, string password)
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;
}