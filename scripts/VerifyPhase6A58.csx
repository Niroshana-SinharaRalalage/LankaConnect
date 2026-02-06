#!/usr/bin/env dotnet-script

// Phase 6A.58 - Verify Database Schema
// This script checks the actual column names in PostgreSQL for the events.events table

using System;
using Npgsql;

Console.WriteLine("Fetching database connection string from Azure Key Vault...");

var process = new System.Diagnostics.Process
{
    StartInfo = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "az",
        Arguments = "keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    }
};

process.Start();
var connStr = process.StandardOutput.ReadToEnd().Trim();
process.WaitForExit();

if (string.IsNullOrEmpty(connStr))
{
    Console.WriteLine("ERROR: Could not retrieve connection string");
    return 1;
}

Console.WriteLine("Connection string retrieved successfully");
Console.WriteLine();

var query = @"
SELECT column_name, data_type, udt_name
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name IN ('Status', 'status', 'Category', 'category', 'StartDate', 'start_date', 'search_vector', 'Id')
ORDER BY column_name;
";

try
{
    using var conn = new NpgsqlConnection(connStr);
    await conn.OpenAsync();

    Console.WriteLine("Connected to database successfully");
    Console.WriteLine("Querying for column names in events.events table...");
    Console.WriteLine();

    using var cmd = new NpgsqlCommand(query, conn);
    using var reader = await cmd.ExecuteReaderAsync();

    Console.WriteLine("COLUMN NAME           DATA TYPE        UDT NAME");
    Console.WriteLine("================================================================================");

    var found = false;
    while (await reader.ReadAsync())
    {
        found = true;
        var columnName = reader.GetString(0);
        var dataType = reader.GetString(1);
        var udtName = reader.GetString(2);
        Console.WriteLine($"{columnName,-20} {dataType,-15} {udtName}");
    }

    if (!found)
    {
        Console.WriteLine("No columns found matching the criteria.");
    }

    Console.WriteLine();
    Console.WriteLine("Query completed successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    return 1;
}

return 0;
