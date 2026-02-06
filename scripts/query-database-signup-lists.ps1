# Query signup lists from staging database
$connectionString = "Host=lankaconnect-postgres-staging.postgres.database.azure.com;Database=lankaconnect;Username=lankaconnect_admin;Password=YOUR_PASSWORD_HERE;SSL Mode=Require"

# Install Npgsql if not already installed
if (-not (Get-Module -ListAvailable -Name Npgsql)) {
    Write-Host "Installing Npgsql module..."
    Install-Package Npgsql -Force -SkipDependencies
}

$query = @"
SELECT
    id,
    category,
    description,
    has_mandatory_items,
    has_preferred_items,
    has_suggested_items,
    has_open_items,
    created_at
FROM events.sign_up_lists
WHERE event_id = '89f8ef9f-af11-4b1a-8dec-b440faef9ad0'
ORDER BY created_at;
"@

Write-Host "Query: $query"
Write-Host ""
Write-Host "Note: You need to provide the actual database password in the connection string"
