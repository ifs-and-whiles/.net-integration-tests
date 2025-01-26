using DapperExtensions;

namespace NetIntegrationTests.ExpensesInTests.Fixtures;

public class ExpensesDatabase(ExpensesTestFixture fixture)
{
    public async Task wait_for_entities<TEntity>(
        Action<List<TEntity>> assertions) where TEntity : class
    {
        await fixture.wait_for_passed_condition_or_throw_after_timeout(
            async ()  => await do_all_entities_meet_assertions(assertions),
            checkingIntervalInMilliseconds: 500,
            retryCount: 15);
    }
    
    private async Task<bool> do_all_entities_meet_assertions<TEntity>(
        Action<List<TEntity>> assertions) where TEntity : class
    {
        await using var connection = new Npgsql.NpgsqlConnection(fixture.AppSettings.ConnectionString);
    
        var result = await connection.GetListAsync<TEntity>();
        var entities = result.ToList();

        try
        {
            assertions(entities);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}