# PowerShell script to query badge location config values from staging database
# This will help diagnose why the migration didn't fix NULL values

$apiUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "=== Badge Location Config Database Query Test ===" -ForegroundColor Cyan
Write-Host ""

# First, get the database connection string from Azure
Write-Host "Step 1: Getting database connection string from Azure Container App..." -ForegroundColor Yellow
$envVars = az containerapp show `
    --name lankaconnect-api-staging `
    --resource-group politebay-79d6e8a2 `
    --query "properties.template.containers[0].env" `
    --output json | ConvertFrom-Json

$connString = ($envVars | Where-Object { $_.name -eq "ConnectionStrings__DefaultConnection" }).value

if (-not $connString) {
    Write-Host "ERROR: Could not retrieve connection string" -ForegroundColor Red
    exit 1
}

Write-Host "Connection string retrieved successfully" -ForegroundColor Green
Write-Host ""

# Parse connection string to get credentials
Write-Host "Step 2: Parsing connection string..." -ForegroundColor Yellow
$connParams = @{}
$connString -split ';' | ForEach-Object {
    $parts = $_ -split '=', 2
    if ($parts.Count -eq 2) {
        $connParams[$parts[0].Trim()] = $parts[1].Trim()
    }
}

$dbHost = $connParams['Host']
$dbName = $connParams['Database']
$dbUser = $connParams['Username']
$dbPassword = $connParams['Password']
$dbPort = if ($connParams['Port']) { $connParams['Port'] } else { '5432' }

Write-Host "Database: $dbName on $dbHost" -ForegroundColor Green
Write-Host ""

# Query to check badge location config NULL values
$query = @"
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    size_width_listing,
    size_height_listing,
    rotation_listing,
    position_x_featured,
    position_y_featured,
    size_width_featured,
    size_height_featured,
    rotation_featured,
    position_x_detail,
    position_y_detail,
    size_width_detail,
    size_height_detail,
    rotation_detail,
    CASE
        WHEN position_x_listing IS NULL OR position_y_listing IS NULL OR
             size_width_listing IS NULL OR size_height_listing IS NULL OR rotation_listing IS NULL OR
             position_x_featured IS NULL OR position_y_featured IS NULL OR
             size_width_featured IS NULL OR size_height_featured IS NULL OR rotation_featured IS NULL OR
             position_x_detail IS NULL OR position_y_detail IS NULL OR
             size_width_detail IS NULL OR size_height_detail IS NULL OR rotation_detail IS NULL
        THEN 'HAS_NULLS'
        ELSE 'ALL_NON_NULL'
    END as null_status
FROM badges.badges
ORDER BY created_at DESC;
"@

Write-Host "Step 3: Querying badge location config values..." -ForegroundColor Yellow
Write-Host ""

# Set PostgreSQL password environment variable
$env:PGPASSWORD = $dbPassword

# Execute query using psql
$result = psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $query

Write-Host "Query Results:" -ForegroundColor Cyan
Write-Host $result
Write-Host ""

# Also check migration history
Write-Host "Step 4: Checking migration history..." -ForegroundColor Yellow
$migrationQuery = @"
SELECT migration_id, product_version
FROM public.__efmigrationshistory
WHERE migration_id LIKE '%BadgeLocationConfig%' OR migration_id LIKE '%20251217%'
ORDER BY migration_id DESC;
"@

$migrationResult = psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $migrationQuery

Write-Host "Recent Badge-related Migrations:" -ForegroundColor Cyan
Write-Host $migrationResult
Write-Host ""

# Count NULL vs non-NULL badges
Write-Host "Step 5: Summary statistics..." -ForegroundColor Yellow
$statsQuery = @"
SELECT
    COUNT(*) as total_badges,
    SUM(CASE WHEN position_x_listing IS NULL THEN 1 ELSE 0 END) as null_position_x_listing,
    SUM(CASE WHEN position_y_listing IS NULL THEN 1 ELSE 0 END) as null_position_y_listing,
    SUM(CASE WHEN size_width_listing IS NULL THEN 1 ELSE 0 END) as null_size_width_listing,
    SUM(CASE WHEN size_height_listing IS NULL THEN 1 ELSE 0 END) as null_size_height_listing,
    SUM(CASE WHEN rotation_listing IS NULL THEN 1 ELSE 0 END) as null_rotation_listing
FROM badges.badges;
"@

$statsResult = psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -c $statsQuery

Write-Host "NULL Value Statistics:" -ForegroundColor Cyan
Write-Host $statsResult
Write-Host ""

# Clear password from environment
$env:PGPASSWORD = $null

Write-Host "=== Test Complete ===" -ForegroundColor Green
