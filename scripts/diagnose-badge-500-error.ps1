# Badge 500 Error Diagnostic Script
# Purpose: Collect evidence before making assumptions

Write-Host "=== BADGE 500 ERROR DIAGNOSTIC REPORT ===" -ForegroundColor Cyan
Write-Host "Generated: $(Get-Date)" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# STEP 1: CHECK ACTUAL DATABASE STATE
# ============================================================================
Write-Host "STEP 1: DATABASE STATE VERIFICATION" -ForegroundColor Yellow
Write-Host "Connecting to database to check for NULL values..." -ForegroundColor Gray

$checkNullsQuery = @"
-- Check for NULL values in Badge location config columns
SELECT
    id,
    name,
    position_x_listing,
    position_y_listing,
    position_x_featured,
    position_y_featured,
    position_x_detail,
    position_y_detail,
    CASE
        WHEN position_x_listing IS NULL OR position_y_listing IS NULL THEN 'LISTING_HAS_NULLS'
        WHEN position_x_featured IS NULL OR position_y_featured IS NULL THEN 'FEATURED_HAS_NULLS'
        WHEN position_x_detail IS NULL OR position_y_detail IS NULL THEN 'DETAIL_HAS_NULLS'
        ELSE 'NO_NULLS'
    END as null_status
FROM badges.badges
WHERE is_active = true
ORDER BY null_status DESC, name;

-- Summary count
SELECT
    COUNT(*) as total_active_badges,
    COUNT(CASE WHEN position_x_listing IS NULL OR position_y_listing IS NULL THEN 1 END) as listing_nulls,
    COUNT(CASE WHEN position_x_featured IS NULL OR position_y_featured IS NULL THEN 1 END) as featured_nulls,
    COUNT(CASE WHEN position_x_detail IS NULL OR position_y_detail IS NULL THEN 1 END) as detail_nulls
FROM badges.badges
WHERE is_active = true;
"@

Write-Host "Query to run on database:" -ForegroundColor Cyan
Write-Host $checkNullsQuery -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 2: CHECK SCHEMA STATE
# ============================================================================
Write-Host "STEP 2: SCHEMA STATE VERIFICATION" -ForegroundColor Yellow
Write-Host "Checking column nullability..." -ForegroundColor Gray

$checkSchemaQuery = @"
-- Check actual column nullability in database
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'badges'
  AND table_name = 'badges'
  AND column_name IN (
    'position_x_listing', 'position_y_listing',
    'position_x_featured', 'position_y_featured',
    'position_x_detail', 'position_y_detail'
  )
ORDER BY column_name;
"@

Write-Host "Query to run on database:" -ForegroundColor Cyan
Write-Host $checkSchemaQuery -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 3: CHECK MIGRATION HISTORY
# ============================================================================
Write-Host "STEP 3: MIGRATION HISTORY" -ForegroundColor Yellow
Write-Host "Checking which migrations actually executed..." -ForegroundColor Gray

$checkMigrationsQuery = @"
-- Check migration history
SELECT migration_id, product_version
FROM public.__efmigrationshistory
WHERE migration_id LIKE '%Badge%'
ORDER BY migration_id DESC
LIMIT 10;
"@

Write-Host "Query to run on database:" -ForegroundColor Cyan
Write-Host $checkMigrationsQuery -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 4: AZURE CONTAINER LOGS CHECK
# ============================================================================
Write-Host "STEP 4: AZURE CONTAINER LOGS" -ForegroundColor Yellow
Write-Host "Commands to check actual exception:" -ForegroundColor Gray
Write-Host ""

Write-Host "Option A: If using Azure Container Apps:" -ForegroundColor Cyan
Write-Host "az containerapp logs show --name <your-app-name> --resource-group <your-rg> --follow --tail 100" -ForegroundColor White
Write-Host ""

Write-Host "Option B: If using Azure App Service:" -ForegroundColor Cyan
Write-Host "az webapp log tail --name <your-app-name> --resource-group <your-rg>" -ForegroundColor White
Write-Host ""

Write-Host "Option C: Azure Portal:" -ForegroundColor Cyan
Write-Host "1. Navigate to Azure Portal > Your Container App/App Service" -ForegroundColor White
Write-Host "2. Go to 'Log stream' or 'Logs'" -ForegroundColor White
Write-Host "3. Trigger the Badge API endpoint (GET /api/badges)" -ForegroundColor White
Write-Host "4. Copy the FULL exception stack trace" -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 5: LOCAL REPRODUCTION
# ============================================================================
Write-Host "STEP 5: LOCAL REPRODUCTION TEST" -ForegroundColor Yellow
Write-Host "Commands to test locally with staging database:" -ForegroundColor Gray
Write-Host ""

Write-Host "1. Update appsettings.Development.json to point to staging DB connection string" -ForegroundColor White
Write-Host "2. Run locally: dotnet run --project src/LankaConnect.API" -ForegroundColor White
Write-Host "3. Test endpoint: curl http://localhost:5000/api/badges" -ForegroundColor White
Write-Host "4. Check if error reproduces locally (gives better stack trace)" -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 6: EF CORE CONFIGURATION CHECK
# ============================================================================
Write-Host "STEP 6: EF CORE CONFIGURATION VERIFICATION" -ForegroundColor Yellow
Write-Host "Files to review:" -ForegroundColor Gray
Write-Host ""

Write-Host "1. BadgeConfiguration.cs - Check owned entity configuration" -ForegroundColor White
Write-Host "   Location: src/LankaConnect.Infrastructure/Data/Configurations/BadgeConfiguration.cs" -ForegroundColor White
Write-Host "   Look for: OwnsOne() configurations for ListingConfig, FeaturedConfig, DetailConfig" -ForegroundColor White
Write-Host "   Verify: All properties marked as .IsRequired() or have default values" -ForegroundColor White
Write-Host ""

Write-Host "2. BadgeLocationConfig.cs - Check value object" -ForegroundColor White
Write-Host "   Location: src/LankaConnect.Domain/Aggregates/BadgeAggregate/ValueObjects/BadgeLocationConfig.cs" -ForegroundColor White
Write-Host "   Verify: Constructor requires all values or has defaults" -ForegroundColor White
Write-Host ""

# ============================================================================
# DIAGNOSTIC CHECKLIST
# ============================================================================
Write-Host ""
Write-Host "=== DIAGNOSTIC CHECKLIST ===" -ForegroundColor Cyan
Write-Host ""

$checklist = @(
    @{Task="Run NULL check query on staging database"; Status="PENDING"},
    @{Task="Run schema check query on staging database"; Status="PENDING"},
    @{Task="Check Azure container logs for actual exception"; Status="PENDING"},
    @{Task="Verify migration history in database"; Status="PENDING"},
    @{Task="Review BadgeConfiguration.cs owned entity setup"; Status="PENDING"},
    @{Task="Review BadgeLocationConfig.cs constructor"; Status="PENDING"},
    @{Task="Attempt local reproduction with staging DB"; Status="PENDING"}
)

foreach ($item in $checklist) {
    Write-Host "[ ] $($item.Task)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== NEXT STEPS ===" -ForegroundColor Cyan
Write-Host "1. Execute database queries above against STAGING database" -ForegroundColor White
Write-Host "2. Capture Azure container log output with actual exception" -ForegroundColor White
Write-Host "3. Review EF Core configuration files" -ForegroundColor White
Write-Host "4. DO NOT make any more migrations until root cause is confirmed" -ForegroundColor White
Write-Host ""
Write-Host "After collecting evidence, we can determine:" -ForegroundColor Green
Write-Host "  - Is it NULL values? (database evidence)" -ForegroundColor White
Write-Host "  - Is it EF Core mapping? (configuration evidence)" -ForegroundColor White
Write-Host "  - Is it something else? (log evidence)" -ForegroundColor White
Write-Host ""
Write-Host "=== END OF DIAGNOSTIC REPORT ===" -ForegroundColor Cyan
