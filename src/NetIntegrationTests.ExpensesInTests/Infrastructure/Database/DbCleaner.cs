using Dapper;
using Npgsql;

namespace NetIntegrationTests.ExpensesInTests.Infrastructure.Database;

public class DbCleaner
{
    public static void CleanDatabase(string connectionString)
    {
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            var cmdText = @"DO $$ 
DECLARE
   table_name text;
BEGIN
   FOR table_name IN (SELECT tablename FROM pg_tables WHERE schemaname = 'public')
   LOOP
      EXECUTE 'TRUNCATE TABLE public.' || table_name || ' CASCADE';
   END LOOP;
END $$;";

            connection.Execute(cmdText);
        }
    }
}