using Npgsql;

var connectionString = "Host=lankaconnect-db-staging.postgres.database.azure.com;Database=lankaconnect_staging;Username=lankaconnect_admin;Password=LankaConnect2024!Secure;SSL Mode=Require;Trust Server Certificate=true";

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

Console.WriteLine("=== Email Template Status in Staging Database ===\n");

// Query 1: All templates
Console.WriteLine("All Email Templates:");
Console.WriteLine(new string('-', 120));

await using (var cmd = new NpgsqlCommand(@"
    SELECT
        name,
        is_active,
        category,
        TO_CHAR(created_at, 'YYYY-MM-DD HH24:MI') as created_at,
        LENGTH(html_template) as html_length
    FROM communications.email_templates
    ORDER BY is_active DESC, name", conn))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine($"{"Name",-60} {"Active",-10} {"Category",-20} {"Created",-20} {"HTML Length",-15}");
    Console.WriteLine(new string('-', 120));

    while (await reader.ReadAsync())
    {
        var name = reader.GetString(0);
        var isActive = reader.GetBoolean(1);
        var category = reader.IsDBNull(2) ? "N/A" : reader.GetString(2);
        var createdAt = reader.GetString(3);
        var htmlLength = reader.GetInt32(4);
        var activeSymbol = isActive ? "✅" : "❌";

        Console.WriteLine($"{name,-60} {activeSymbol,-10} {category,-20} {createdAt,-20} {htmlLength,-15}");
    }
}

Console.WriteLine("\n=== Signup List Templates ===\n");

// Query 2: Signup list templates specifically
await using (var cmd = new NpgsqlCommand(@"
    SELECT
        name,
        is_active,
        TO_CHAR(created_at, 'YYYY-MM-DD HH24:MI') as created_at,
        TO_CHAR(updated_at, 'YYYY-MM-DD HH24:MI') as updated_at
    FROM communications.email_templates
    WHERE name LIKE '%signup-list-commitment%'
    ORDER BY name", conn))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    var found = false;

    while (await reader.ReadAsync())
    {
        found = true;
        var name = reader.GetString(0);
        var isActive = reader.GetBoolean(1);
        var createdAt = reader.GetString(2);
        var updatedAt = reader.GetString(3);
        var activeSymbol = isActive ? "✅ ACTIVE" : "❌ INACTIVE";

        Console.WriteLine($"{name}");
        Console.WriteLine($"  Status: {activeSymbol}");
        Console.WriteLine($"  Created: {createdAt}");
        Console.WriteLine($"  Updated: {updatedAt}\n");
    }

    if (!found)
    {
        Console.WriteLine("❌ NO SIGNUP LIST TEMPLATES FOUND!");
    }
}

Console.WriteLine("\n=== Summary ===\n");

// Query 3: Summary count
await using (var cmd = new NpgsqlCommand(@"
    SELECT
        is_active,
        COUNT(*) as count
    FROM communications.email_templates
    GROUP BY is_active
    ORDER BY is_active DESC", conn))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        var isActive = reader.GetBoolean(0);
        var count = reader.GetInt64(1);
        var status = isActive ? "✅ Active" : "❌ Inactive";
        Console.WriteLine($"{status}: {count} templates");
    }
}

Console.WriteLine("\n=== Inactive Templates ===\n");

// Query 4: List inactive templates
await using (var cmd = new NpgsqlCommand(@"
    SELECT name, category
    FROM communications.email_templates
    WHERE is_active = false
    ORDER BY category, name", conn))
{
    await using var reader = await cmd.ExecuteReaderAsync();
    var found = false;

    while (await reader.ReadAsync())
    {
        found = true;
        var name = reader.GetString(0);
        var category = reader.IsDBNull(1) ? "N/A" : reader.GetString(1);
        Console.WriteLine($"❌ {name} ({category})");
    }

    if (!found)
    {
        Console.WriteLine("✅ All templates are active!");
    }
}

await conn.CloseAsync();
