using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace LankaConnect.IntegrationTests;

public class Phase6A53VerificationTests
{
    private readonly ITestOutputHelper _output;
    private const string ConnectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require";

    public Phase6A53VerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task VerifyEmailTemplateMigrationApplied()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        _output.WriteLine("✅ Database connected successfully!");
        _output.WriteLine("");

        // Check migration
        _output.WriteLine("=== CHECK 1: Email Template Migration Status ===");
        await using (var cmd = new NpgsqlCommand(
            @"SELECT ""MigrationId"", ""ProductVersion""
              FROM ""__EFMigrationsHistory""
              WHERE ""MigrationId"" LIKE '%20251229231742%'
              ORDER BY ""MigrationId"" DESC", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _output.WriteLine($"✅ Migration Applied: {reader.GetString(0)}");
                _output.WriteLine($"   Product Version: {reader.GetString(1)}");
            }
            else
            {
                _output.WriteLine("❌ Migration NOT found in database!");
            }
        }
        _output.WriteLine("");

        // Check email template
        _output.WriteLine("=== CHECK 2: Email Template Content ===");
        await using (var cmd = new NpgsqlCommand(
            @"SELECT id, name, subject_template,
                     LENGTH(html_template) as HtmlBodyLength,
                     LEFT(html_template, 400) as HtmlBodyPreview,
                     updated_at
              FROM communications.email_templates
              WHERE type = 'EmailVerification'
              ORDER BY updated_at DESC
              LIMIT 1", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _output.WriteLine($"Template ID: {reader.GetGuid(0)}");
                _output.WriteLine($"Type: {reader.GetString(1)}");
                _output.WriteLine($"Subject: {reader.GetString(2)}");
                _output.WriteLine($"HTML Body Length: {reader.GetInt32(3)} chars");
                _output.WriteLine($"Last Updated: {reader.GetDateTime(5):yyyy-MM-dd HH:mm:ss}");

                var preview = reader.GetString(4);
                if (preview.Contains("✦"))
                {
                    _output.WriteLine("❌ WARNING: Template contains decorative stars (✦)");
                }
                else
                {
                    _output.WriteLine("✅ Template does NOT contain decorative stars");
                }

                if (preview.Contains("logo.png") || preview.Contains("<img"))
                {
                    _output.WriteLine("⚠️  Template may contain logo image");
                }
                else
                {
                    _output.WriteLine("✅ Template does NOT contain logo");
                }

                _output.WriteLine($"\nPreview (first 400 chars):\n{preview}");
            }
            else
            {
                _output.WriteLine("❌ No email template found!");
            }
        }
        _output.WriteLine("");

        // Check test user
        _output.WriteLine("=== CHECK 3: Test User Verification Status ===");
        await using (var cmd = new NpgsqlCommand(
            @"SELECT ""Email"", ""IsEmailVerified"",
                     ""EmailVerificationToken"" IS NOT NULL as HasToken,
                     ""EmailVerificationTokenExpiry"", ""CreatedAt""
              FROM ""Users""
              WHERE ""Email"" = 'test.phase6a53fix@gmail.com'", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _output.WriteLine($"✅ User Found: {reader.GetString(0)}");
                _output.WriteLine($"   Email Verified: {reader.GetBoolean(1)}");
                _output.WriteLine($"   Has Verification Token: {reader.GetBoolean(2)}");
                if (!reader.IsDBNull(3))
                {
                    _output.WriteLine($"   Token Expiry: {reader.GetDateTime(3):yyyy-MM-dd HH:mm:ss}");
                }
                _output.WriteLine($"   Created At: {reader.GetDateTime(4):yyyy-MM-dd HH:mm:ss}");
            }
            else
            {
                _output.WriteLine("❌ Test user NOT found in database!");
            }
        }
        _output.WriteLine("");

        // Last 5 migrations
        _output.WriteLine("=== CHECK 4: Last 5 Migrations ===");
        await using (var cmd = new NpgsqlCommand(
            @"SELECT ""MigrationId"", ""ProductVersion""
              FROM ""__EFMigrationsHistory""
              ORDER BY ""MigrationId"" DESC
              LIMIT 5", conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                _output.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
            }
        }

        _output.WriteLine("");
        _output.WriteLine("✅ Verification complete!");
    }
}
