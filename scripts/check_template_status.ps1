# Check Email Template Status in Staging Database
# Requires Npgsql NuGet package

$connectionString = "Host=lankaconnect-db-staging.postgres.database.azure.com;Database=lankaconnect_staging;Username=lankaconnect_admin;Password=LankaConnect2024!Secure;SSL Mode=Require;Trust Server Certificate=true"

# Load Npgsql assembly
Add-Type -Path "C:\Users\$env:USERNAME\.nuget\packages\npgsql\8.0.0\lib\net8.0\Npgsql.dll" -ErrorAction SilentlyContinue

try {
    # Create connection
    $conn = New-Object Npgsql.NpgsqlConnection($connectionString)
    $conn.Open()

    Write-Host "=== Email Template Status in Staging Database ===" -ForegroundColor Cyan
    Write-Host ""

    # Query 1: All templates
    Write-Host "All Email Templates:" -ForegroundColor Yellow
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = @"
        SELECT
            name,
            is_active,
            category,
            created_at,
            LENGTH(html_template) as html_length
        FROM communications.email_templates
        ORDER BY is_active DESC, name
"@

    $reader = $cmd.ExecuteReader()
    $templates = @()
    while ($reader.Read()) {
        $templates += [PSCustomObject]@{
            Name = $reader["name"]
            IsActive = $reader["is_active"]
            Category = $reader["category"]
            CreatedAt = $reader["created_at"]
            HtmlLength = $reader["html_length"]
        }
    }
    $reader.Close()

    $templates | Format-Table -AutoSize

    Write-Host ""
    Write-Host "=== Signup List Templates ===" -ForegroundColor Yellow

    # Query 2: Signup list templates specifically
    $cmd.CommandText = @"
        SELECT
            name,
            is_active,
            created_at,
            updated_at
        FROM communications.email_templates
        WHERE name LIKE '%signup-list-commitment%'
        ORDER BY name
"@

    $reader = $cmd.ExecuteReader()
    $signupTemplates = @()
    while ($reader.Read()) {
        $signupTemplates += [PSCustomObject]@{
            Name = $reader["name"]
            IsActive = $reader["is_active"]
            CreatedAt = $reader["created_at"]
            UpdatedAt = $reader["updated_at"]
        }
    }
    $reader.Close()

    if ($signupTemplates.Count -eq 0) {
        Write-Host "❌ NO SIGNUP LIST TEMPLATES FOUND!" -ForegroundColor Red
    } else {
        $signupTemplates | Format-Table -AutoSize
    }

    Write-Host ""
    Write-Host "=== Summary ===" -ForegroundColor Yellow

    # Query 3: Summary count
    $cmd.CommandText = @"
        SELECT
            is_active,
            COUNT(*) as count
        FROM communications.email_templates
        GROUP BY is_active
        ORDER BY is_active DESC
"@

    $reader = $cmd.ExecuteReader()
    while ($reader.Read()) {
        $status = if ($reader["is_active"]) { "✅ Active" } else { "❌ Inactive" }
        $count = $reader["count"]
        Write-Host "$status : $count templates"
    }
    $reader.Close()

    Write-Host ""
    Write-Host "=== Inactive Templates ===" -ForegroundColor Yellow

    # Query 4: List inactive templates
    $cmd.CommandText = @"
        SELECT name, category
        FROM communications.email_templates
        WHERE is_active = false
        ORDER BY category, name
"@

    $reader = $cmd.ExecuteReader()
    $inactiveTemplates = @()
    while ($reader.Read()) {
        $inactiveTemplates += [PSCustomObject]@{
            Name = $reader["name"]
            Category = $reader["category"]
        }
    }
    $reader.Close()

    if ($inactiveTemplates.Count -eq 0) {
        Write-Host "✅ All templates are active!" -ForegroundColor Green
    } else {
        $inactiveTemplates | Format-Table -AutoSize
    }

    $conn.Close()

} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Note: This script requires Npgsql package. Install with:" -ForegroundColor Yellow
    Write-Host "dotnet add package Npgsql"
}
