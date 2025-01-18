namespace NetIntegrationTests.UsersApi.Database;

public class UserEntity
{
    public string Name { get; set; }
    public Guid Id { get; set; }
    public int ExpensesCount { get; set; }
    public int MaxExpenseCount { get; set; }
}