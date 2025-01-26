using Dapper;

namespace NetIntegrationTests.Database;

public class ExpensesRepository(AppSettings appSettings)
{
    private readonly string _connectionString = appSettings.ConnectionString;

    public async Task SaveExpense(
        Guid id,
        string name, 
        decimal amount,
        Guid userId)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        var query = "INSERT INTO expenses (id, name, amount, user_id) VALUES (@Id, @Name, @Amount, @UserId)";
        await connection.ExecuteScalarAsync(query, new { Id = id, Name = name, Amount = amount, UserId = userId });
    }

    public async Task<ExpenseEntity?> GetExpenseById(Guid id)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        var query = "SELECT id, name, amount, user_id as UserId FROM expenses WHERE id = @Id;";
        return await connection.QuerySingleOrDefaultAsync<ExpenseEntity>(query, new { Id = id });
    }

    public async Task DeleteExpense(Guid id)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        var query = "DELETE FROM expenses WHERE id = @Id;";
        await connection.ExecuteAsync(query, new { Id = id });
    }
}