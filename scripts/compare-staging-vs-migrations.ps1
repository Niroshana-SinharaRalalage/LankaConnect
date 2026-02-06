# PowerShell script to identify manual database changes
# Compares staging database with migration files to find discrepancies

$stagingConnectionString = az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv

Write-Host "=== FINDING MANUAL DATABASE CHANGES ===" -ForegroundColor Cyan
Write-Host ""

# Function to run SQL query and get results
function Query-Database {
    param($sql)

    $env:PGPASSWORD = "1qaz!QAZ"
    $result = psql "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;SslMode=Require" -t -c $sql 2>&1
    return $result
}

Write-Host "1. Checking email templates..." -ForegroundColor Yellow

# Query 1: Find templates that were updated after creation (manual changes)
$sql1 = @"
SELECT name, created_at, updated_at
FROM communications.email_templates
WHERE updated_at IS NOT NULL AND updated_at > created_at
ORDER BY updated_at DESC;
"@

Write-Host "Templates with manual updates:" -ForegroundColor Green
Query-Database $sql1

Write-Host ""
Write-Host "2. Checking reference data changes..." -ForegroundColor Yellow

# Query 2: Check if metro areas were modified
$sql2 = @"
SELECT COUNT(*) as metro_count FROM events.metro_areas;
"@

Write-Host "Metro areas count:" -ForegroundColor Green
Query-Database $sql2

Write-Host ""
Write-Host "3. Checking for custom data..." -ForegroundColor Yellow

# Query 3: Look for data that shouldn't be in staging
$sql3 = @"
SELECT
    (SELECT COUNT(*) FROM communications.email_templates) as templates_count,
    (SELECT COUNT(*) FROM events.metro_areas) as metros_count,
    (SELECT COUNT(*) FROM reference_data.reference_values) as reference_values_count,
    (SELECT COUNT(*) FROM reference_data.state_tax_rates) as tax_rates_count;
"@

Write-Host "Reference data counts:" -ForegroundColor Green
Query-Database $sql3

Write-Host ""
Write-Host "=== SOLUTION ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Option 1: Export all templates and create a migration (SAFE)" -ForegroundColor Green
Write-Host "  Run: psql `$stagingConnectionString -f scripts/export-staging-email-templates.sql > staging-templates.txt"
Write-Host ""
Write-Host "Option 2: Assume staging is correct and let it run (RISKY)" -ForegroundColor Yellow
Write-Host "  - Production will get original migration templates"
Write-Host "  - You'll need to manually update production later"
Write-Host ""
Write-Host "Option 3: Copy staging database to production (NOT RECOMMENDED)" -ForegroundColor Red
Write-Host "  - This includes test data which you don't want"
Write-Host ""
