namespace NetIntegrationTests.Database;

public class ExpenseEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Amount { get; set; }
    public Guid UserId { get; set; }
}