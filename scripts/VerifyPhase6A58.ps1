# Phase 6A.58 - Verify Database Schema for Events Table
# This script checks the actual column names in PostgreSQL

Write-Host "Fetching database connection string from Azure Key Vault..." -ForegroundColor Cyan

$connStr = az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv

if (-not $connStr) {
    Write-Host "ERROR: Could not retrieve connection string" -ForegroundColor Red
    exit 1
}

Write-Host "Connection string retrieved successfully" -ForegroundColor Green

# Parse connection string to extract components
if ($connStr -match "Host=([^;]+);.*Database=([^;]+);.*Username=([^;]+);.*Password=([^;]+)") {
    $host = $matches[1]
    $database = $matches[2]
    $username = $matches[3]
    $password = $matches[4]

    Write-Host "`nDatabase: $database" -ForegroundColor Yellow
    Write-Host "Host: $host" -ForegroundColor Yellow
    Write-Host "Username: $username" -ForegroundColor Yellow

    # Query to check column names
    $query = @"
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name IN ('Status', 'status', 'Category', 'category', 'StartDate', 'start_date', 'search_vector')
ORDER BY column_name;
"@

    Write-Host "`nQuerying database for column names..." -ForegroundColor Cyan
    Write-Host "Query: $query" -ForegroundColor Gray

    # Set PGPASSWORD environment variable for psql
    $env:PGPASSWORD = $password

    # Execute query using psql
    $result = psql -h $host -U $username -d $database -c $query 2>&1

    Write-Host "`nQuery Results:" -ForegroundColor Green
    Write-Host $result

    # Clean up password
    Remove-Item Env:\PGPASSWORD
} else {
    Write-Host "ERROR: Could not parse connection string" -ForegroundColor Red
    exit 1
}
