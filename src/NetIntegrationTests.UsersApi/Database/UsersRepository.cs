using Dapper;
using Npgsql;

namespace NetIntegrationTests.UsersApi.Database;

public class UsersRepository(AppSettings appSettings)
{
    private readonly string _connectionString = appSettings.ConnectionString;

    public async Task CreateUser(string name, Guid id, int expensesCount, int maxExpenseCount)
    {
        const string query = "INSERT INTO users (id, name, expenses_count, max_expenses_count) VALUES (@Id, @Name, @ExpensesCount, @MaxExpenseCount)";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(query, new { Id = id, Name = name, ExpensesCount = expensesCount, MaxExpenseCount = maxExpenseCount});
    }

    public bool DoesUserExist(string requestName)
    {
        const string query = "SELECT COUNT(1) FROM users WHERE name = @Name";
        
        using var connection = new NpgsqlConnection(_connectionString);
        return connection.ExecuteScalar<int>(query, new { Name = requestName }) > 0;
    }

    public async Task<UserEntity?> GetUser(Guid userId)
    {
        const string query = "SELECT id, name, expenses_count AS ExpensesCount, max_expenses_count AS MaxExpenseCount FROM users WHERE id = @UserId";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<UserEntity>(query, new { UserId = userId });
    }

    public async Task IncrementExpensesCount(Guid userId)
    {
        const string query = "UPDATE users SET expenses_count = expenses_count + 1 WHERE id = @UserId";
        
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(query, new { UserId = userId });
}

}