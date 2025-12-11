using System;
using System.Threading.Tasks;
using Npgsql;

namespace LankaConnect.Scripts
{
    class VerifyNewsletterSchema
    {
        static async Task Main(string[] args)
        {
            var connectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require;Pooling=true;MinPoolSize=2;MaxPoolSize=20";

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                Console.WriteLine("✓ Connected to Azure staging database");
                Console.WriteLine();

                // 1. Check if table exists
                Console.WriteLine("=== 1. CHECKING TABLE EXISTENCE ===");
                await using (var cmd = new NpgsqlCommand(@"
                    SELECT table_name
                    FROM information_schema.tables
                    WHERE table_schema = 'communications'
                      AND table_name = 'newsletter_subscribers'", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        Console.WriteLine($"✓ Table 'communications.newsletter_subscribers' exists");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Table 'communications.newsletter_subscribers' NOT FOUND");
                        return;
                    }
                }
                Console.WriteLine();

                // 2. Get column information
                Console.WriteLine("=== 2. TABLE COLUMNS ===");
                await using (var cmd = new NpgsqlCommand(@"
                    SELECT
                        column_name,
                        data_type,
                        is_nullable,
                        character_maximum_length
                    FROM information_schema.columns
                    WHERE table_schema = 'communications'
                      AND table_name = 'newsletter_subscribers'
                    ORDER BY ordinal_position", connection))
                {
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var isNullable = reader.GetString(2);
                        var maxLength = reader.IsDBNull(3) ? "" : $"({reader.GetInt32(3)})";

                        Console.WriteLine($"  • {columnName,-30} {dataType}{maxLength,-20} NULL: {isNullable}");
                    }
                }
                Console.WriteLine();

                // 3. Check indexes
                Console.WriteLine("=== 3. INDEXES ===");
                var expectedIndexes = new[]
                {
                    "pk_newsletter_subscribers",
                    "idx_newsletter_subscribers_email",
                    "idx_newsletter_subscribers_confirmation_token",
                    "idx_newsletter_subscribers_unsubscribe_token",
                    "idx_newsletter_subscribers_metro_area_id",
                    "idx_newsletter_subscribers_active_confirmed"
                };

                await using (var cmd = new NpgsqlCommand(@"
                    SELECT indexname
                    FROM pg_indexes
                    WHERE schemaname = 'communications'
                      AND tablename = 'newsletter_subscribers'
                    ORDER BY indexname", connection))
                {
                    await using var reader = await cmd.ExecuteReaderAsync();
                    var foundIndexes = new System.Collections.Generic.List<string>();

                    while (await reader.ReadAsync())
                    {
                        var indexName = reader.GetString(0);
                        foundIndexes.Add(indexName);
                        Console.WriteLine($"  ✓ {indexName}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("=== INDEX VERIFICATION ===");
                    foreach (var expectedIndex in expectedIndexes)
                    {
                        if (foundIndexes.Contains(expectedIndex))
                        {
                            Console.WriteLine($"  ✓ {expectedIndex}");
                        }
                        else
                        {
                            Console.WriteLine($"  ✗ {expectedIndex} MISSING");
                        }
                    }
                }
                Console.WriteLine();

                // 4. Get row count
                Console.WriteLine("=== 4. ROW COUNT ===");
                await using (var cmd = new NpgsqlCommand(@"
                    SELECT COUNT(*) FROM communications.newsletter_subscribers", connection))
                {
                    var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
                    Console.WriteLine($"  Total rows: {count}");
                }
                Console.WriteLine();

                Console.WriteLine("✓ All verification checks completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }
    }
}
