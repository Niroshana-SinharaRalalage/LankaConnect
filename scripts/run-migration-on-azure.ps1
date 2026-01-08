# Phase 6A.64 - Manual Migration Script for Azure Staging
# Run this if auto-deployment hasn't triggered

Write-Host "Phase 6A.64 Migration - Manual Execution" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verify you have the connection string
Write-Host "Step 1: Get Azure PostgreSQL Connection String" -ForegroundColor Yellow
Write-Host "Go to: Azure Portal > PostgreSQL Server > Connection strings" -ForegroundColor Gray
Write-Host ""
Write-Host "Connection string format:" -ForegroundColor Gray
Write-Host 'Host=<server>.postgres.database.azure.com;Database=lankaconnect;Username=<user>;Password=<password>;SSL Mode=Require;' -ForegroundColor DarkGray
Write-Host ""

$connectionString = Read-Host "Paste your Azure PostgreSQL connection string"

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    Write-Host "ERROR: Connection string is required!" -ForegroundColor Red
    exit 1
}

# Step 2: Navigate to Infrastructure project
Write-Host ""
Write-Host "Step 2: Navigating to Infrastructure project..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\..\src\LankaConnect.Infrastructure"

# Step 3: Check EF Core tools
Write-Host ""
Write-Host "Step 3: Checking EF Core tools..." -ForegroundColor Yellow
$efVersion = dotnet ef --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: EF Core tools not installed!" -ForegroundColor Red
    Write-Host "Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    Pop-Location
    exit 1
}
Write-Host "EF Core version: $efVersion" -ForegroundColor Green

# Step 4: List pending migrations
Write-Host ""
Write-Host "Step 4: Checking pending migrations..." -ForegroundColor Yellow
$pendingMigrations = dotnet ef migrations list --context AppDbContext --connection $connectionString --no-build 2>&1
Write-Host $pendingMigrations

# Step 5: Run migration
Write-Host ""
Write-Host "Step 5: Running Phase 6A.64 migration..." -ForegroundColor Yellow
Write-Host "This will:" -ForegroundColor Gray
Write-Host "  1. Create junction table: newsletter_subscriber_metro_areas" -ForegroundColor Gray
Write-Host "  2. Migrate existing data" -ForegroundColor Gray
Write-Host "  3. Drop old metro_area_id column" -ForegroundColor Gray
Write-Host ""

$confirm = Read-Host "Continue with migration? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Migration cancelled." -ForegroundColor Yellow
    Pop-Location
    exit 0
}

Write-Host ""
Write-Host "Executing migration..." -ForegroundColor Cyan
dotnet ef database update --context AppDbContext --connection $connectionString --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ Migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Test newsletter subscription in UI" -ForegroundColor Gray
    Write-Host "2. Verify 5 metro areas are stored for Ohio" -ForegroundColor Gray
    Write-Host "3. Test event cancellation email delivery" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "❌ Migration failed!" -ForegroundColor Red
    Write-Host "Check error messages above and troubleshoot." -ForegroundColor Yellow
}

Pop-Location
Write-Host ""
Write-Host "Script completed." -ForegroundColor Cyan
