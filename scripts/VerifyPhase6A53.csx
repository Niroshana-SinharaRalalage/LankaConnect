#!/usr/bin/env dotnet-script
#r "nuget: Npgsql, 8.0.1"

using Npgsql;
using System;
using System.Threading.Tasks;

var connectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require;Pooling=true;MinPoolSize=2;MaxPoolSize=20";

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

Console.WriteLine("✅ Database connection successful!\n");

// Check 1: Migration applied?
Console.WriteLine("=== CHECK 1: Email Template Migration Status ===");
await using (var cmd = new NpgsqlCommand(
    @"SELECT ""MigrationId"", ""ProductVersion""
      FROM ""__EFMigrationsHistory""
      WHERE ""MigrationId"" LIKE '%20251229231742%'
      ORDER BY ""MigrationId"" DESC", connection))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"✅ Migration Applied: {reader.GetString(0)}");
        Console.WriteLine($"   Product Version: {reader.GetString(1)}\n");
    }
    else
    {
        Console.WriteLine("❌ Migration NOT found in database!\n");
    }
}

// Check 2: Email template content
Console.WriteLine("=== CHECK 2: Email Template Content ===");
await using (var cmd = new NpgsqlCommand(
    @"SELECT ""Id"", ""TemplateType"", ""Subject"",
             LENGTH(""HtmlBody"") as HtmlBodyLength,
             LEFT(""HtmlBody"", 400) as HtmlBodyPreview,
             ""UpdatedAt""
      FROM ""EmailTemplates""
      WHERE ""TemplateType"" = 'EmailVerification'
      ORDER BY ""UpdatedAt"" DESC
      LIMIT 1", connection))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"Template ID: {reader.GetGuid(0)}");
        Console.WriteLine($"Type: {reader.GetString(1)}");
        Console.WriteLine($"Subject: {reader.GetString(2)}");
        Console.WriteLine($"HTML Body Length: {reader.GetInt32(3)} chars");
        Console.WriteLine($"Last Updated: {reader.GetDateTime(5):yyyy-MM-dd HH:mm:ss}\n");

        var preview = reader.GetString(4);
        // Check for decorative stars
        if (preview.Contains("✦"))
        {
            Console.WriteLine("❌ WARNING: Template contains decorative stars (✦)");
        }
        else
        {
            Console.WriteLine("✅ Template does NOT contain decorative stars");
        }

        // Check for logo
        if (preview.Contains("logo.png") || preview.Contains("<img"))
        {
            Console.WriteLine("⚠️  Template may contain logo image");
        }
        else
        {
            Console.WriteLine("✅ Template does NOT contain logo");
        }

        Console.WriteLine($"\nPreview (first 400 chars):\n{preview}\n");
    }
    else
    {
        Console.WriteLine("❌ No email template found!\n");
    }
}

// Check 3: User created with verification token
Console.WriteLine("=== CHECK 3: Test User Verification Status ===");
await using (var cmd = new NpgsqlCommand(
    @"SELECT ""Email"", ""IsEmailVerified"",
             ""EmailVerificationToken"" IS NOT NULL as HasToken,
             ""EmailVerificationTokenExpiry"", ""CreatedAt""
      FROM ""Users""
      WHERE ""Email"" = 'test.phase6a53fix@gmail.com'", connection))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        Console.WriteLine($"✅ User Found: {reader.GetString(0)}");
        Console.WriteLine($"   Email Verified: {reader.GetBoolean(1)}");
        Console.WriteLine($"   Has Verification Token: {reader.GetBoolean(2)}");
        if (!reader.IsDBNull(3))
        {
            Console.WriteLine($"   Token Expiry: {reader.GetDateTime(3):yyyy-MM-dd HH:mm:ss}");
        }
        Console.WriteLine($"   Created At: {reader.GetDateTime(4):yyyy-MM-dd HH:mm:ss}\n");
    }
    else
    {
        Console.WriteLine("❌ Test user NOT found in database!\n");
    }
}

// Check 4: Recent migrations
Console.WriteLine("=== CHECK 4: Last 5 Migrations ===");
await using (var cmd = new NpgsqlCommand(
    @"SELECT ""MigrationId"", ""ProductVersion""
      FROM ""__EFMigrationsHistory""
      ORDER BY ""MigrationId"" DESC
      LIMIT 5", connection))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"  - {reader.GetString(0)} ({reader.GetString(1)})");
    }
}

Console.WriteLine("\n✅ Verification complete!");
