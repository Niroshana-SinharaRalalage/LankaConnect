using System;
using System.Threading.Tasks;
using Npgsql;

namespace LankaConnect.Scripts
{
    class GetMetroAreaId
    {
        static async Task Main(string[] args)
        {
            var connectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require;Pooling=true;MinPoolSize=2;MaxPoolSize=20";

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                Console.WriteLine("=== METRO AREAS ===");
                await using (var cmd = new NpgsqlCommand(@"
                    SELECT id, name
                    FROM events.metro_areas
                    ORDER BY name
                    LIMIT 5", connection))
                {
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var id = reader.GetGuid(0);
                        var name = reader.GetString(1);
                        Console.WriteLine($"  {id} - {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
