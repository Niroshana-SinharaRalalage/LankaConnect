# Phase 6A.31c: Comprehensive Badge 500 Error Diagnostics
# Purpose: Determine root cause of persistent HTTP 500 error after data-only migration
# This script executes a decision tree to narrow down the exact failure point

param(
    [string]$Environment = "staging"
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Badge 500 Error Diagnostic Suite" -ForegroundColor Cyan
Write-Host "Phase 6A.31c: Post-Migration Analysis" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Generated: $(Get-Date)" -ForegroundColor Gray
Write-Host ""

# Configuration
$envFile = "c:\Work\LankaConnect\.env.$Environment"
if (-not (Test-Path $envFile)) {
    Write-Host "ERROR: Environment file not found: $envFile" -ForegroundColor Red
    Write-Host "Please create .env.staging with LANKACONNECT_CONNECTION_STRING and LANKACONNECT_API_BASE_URL" -ForegroundColor Yellow
    exit 1
}

# Load environment variables
Get-Content $envFile | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        $name = $matches[1].Trim()
        $value = $matches[2].Trim()
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }
}

$apiBaseUrl = [Environment]::GetEnvironmentVariable("LANKACONNECT_API_BASE_URL")
$connectionString = [Environment]::GetEnvironmentVariable("LANKACONNECT_CONNECTION_STRING")

Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "API URL: $apiBaseUrl" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# DIAGNOSTIC 1: Verify Migration Execution in Database
# ============================================================================
Write-Host "[1/6] DIAGNOSTIC: Migration History Verification" -ForegroundColor Cyan
Write-Host "Checking if migration 20251217205258 was executed..." -ForegroundColor Gray

$migrationCheckQuery = @"
SELECT
    migration_id,
    product_version
FROM public.__efmigrationshistory
WHERE migration_id = '20251217205258_FixBadgeNullsDataOnly'
ORDER BY migration_id DESC;
"@

try {
    $migrationResult = psql $connectionString -c $migrationCheckQuery -t -A

    if ($migrationResult) {
        Write-Host "RESULT: Migration WAS applied to database" -ForegroundColor Green
        Write-Host "Details: $migrationResult" -ForegroundColor Gray
    } else {
        Write-Host "CRITICAL: Migration NOT FOUND in __EFMigrationsHistory" -ForegroundColor Red
        Write-Host "This means EF Core never recorded the migration execution." -ForegroundColor Red
        Write-Host "Migration may have failed silently during deployment." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "RECOMMENDATION: Run migration manually with verbose logging" -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR querying migration history: $_" -ForegroundColor Red
    Write-Host "If psql is not available, run this query manually in Azure Query Editor:" -ForegroundColor Yellow
    Write-Host $migrationCheckQuery -ForegroundColor White
}

Write-Host ""

# ============================================================================
# DIAGNOSTIC 2: Check for NULL Values in Badge Location Config Columns
# ============================================================================
Write-Host "[2/6] DIAGNOSTIC: NULL Value Detection" -ForegroundColor Cyan
Write-Host "Scanning badges table for NULL location config columns..." -ForegroundColor Gray

$nullCheckQuery = @"
SELECT
    id,
    name,
    CASE
        WHEN position_x_listing IS NULL THEN 'position_x_listing'
        WHEN position_y_listing IS NULL THEN 'position_y_listing'
        WHEN size_width_listing IS NULL THEN 'size_width_listing'
        WHEN size_height_listing IS NULL THEN 'size_height_listing'
        WHEN rotation_listing IS NULL THEN 'rotation_listing'
        WHEN position_x_featured IS NULL THEN 'position_x_featured'
        WHEN position_y_featured IS NULL THEN 'position_y_featured'
        WHEN size_width_featured IS NULL THEN 'size_width_featured'
        WHEN size_height_featured IS NULL THEN 'size_height_featured'
        WHEN rotation_featured IS NULL THEN 'rotation_featured'
        WHEN position_x_detail IS NULL THEN 'position_x_detail'
        WHEN position_y_detail IS NULL THEN 'position_y_detail'
        WHEN size_width_detail IS NULL THEN 'size_width_detail'
        WHEN size_height_detail IS NULL THEN 'size_height_detail'
        WHEN rotation_detail IS NULL THEN 'rotation_detail'
        ELSE 'NO_NULLS'
    END as first_null_column
FROM badges.badges
WHERE
    position_x_listing IS NULL OR
    position_y_listing IS NULL OR
    size_width_listing IS NULL OR
    size_height_listing IS NULL OR
    rotation_listing IS NULL OR
    position_x_featured IS NULL OR
    position_y_featured IS NULL OR
    size_width_featured IS NULL OR
    size_height_featured IS NULL OR
    rotation_featured IS NULL OR
    position_x_detail IS NULL OR
    position_y_detail IS NULL OR
    size_width_detail IS NULL OR
    size_height_detail IS NULL OR
    rotation_detail IS NULL;
"@

try {
    $nullCheckResult = psql $connectionString -c $nullCheckQuery -t -A

    if ($nullCheckResult) {
        Write-Host "CRITICAL: NULL values STILL EXIST in database" -ForegroundColor Red
        Write-Host "Badges with NULL columns:" -ForegroundColor Yellow
        Write-Host $nullCheckResult
        Write-Host ""
        Write-Host "This means:" -ForegroundColor Yellow
        Write-Host "- Migration SQL UPDATE statement did NOT execute" -ForegroundColor Yellow
        Write-Host "- OR migration succeeded but new badges were created after it" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "NULL Hypothesis: CONFIRMED - NULL values are the root cause" -ForegroundColor Red
    } else {
        Write-Host "RESULT: NO NULL values found in any badge location config columns" -ForegroundColor Green
        Write-Host ""
        Write-Host "NULL Hypothesis: REJECTED - All columns have valid values" -ForegroundColor Yellow
        Write-Host "The 500 error must be caused by something else." -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR querying for NULL values: $_" -ForegroundColor Red
    Write-Host "If psql is not available, run this query manually in Azure Query Editor:" -ForegroundColor Yellow
    Write-Host $nullCheckQuery -ForegroundColor White
}

Write-Host ""

# ============================================================================
# DIAGNOSTIC 3: Sample Badge Data Inspection
# ============================================================================
Write-Host "[3/6] DIAGNOSTIC: Sample Badge Data Inspection" -ForegroundColor Cyan
Write-Host "Retrieving raw data for first 3 badges..." -ForegroundColor Gray

$sampleDataQuery = @"
SELECT
    id,
    name,
    is_active,
    is_system,
    position_x_listing, position_y_listing, size_width_listing, size_height_listing, rotation_listing,
    position_x_featured, position_y_featured, size_width_featured, size_height_featured, rotation_featured,
    position_x_detail, position_y_detail, size_width_detail, size_height_detail, rotation_detail
FROM badges.badges
ORDER BY created_at
LIMIT 3;
"@

try {
    $sampleData = psql $connectionString -c $sampleDataQuery -x

    Write-Host "Sample badge data:" -ForegroundColor Gray
    Write-Host $sampleData
} catch {
    Write-Host "ERROR retrieving sample data: $_" -ForegroundColor Red
    Write-Host "Run this query manually in Azure Query Editor:" -ForegroundColor Yellow
    Write-Host $sampleDataQuery -ForegroundColor White
}

Write-Host ""

# ============================================================================
# DIAGNOSTIC 4: API Health Check
# ============================================================================
Write-Host "[4/6] DIAGNOSTIC: API Health Check" -ForegroundColor Cyan
Write-Host "Testing basic API connectivity..." -ForegroundColor Gray

try {
    $healthResponse = Invoke-WebRequest -Uri "$apiBaseUrl/health" -Method GET -ErrorAction Stop
    Write-Host "API Health Status: $($healthResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "ERROR: API health check failed" -ForegroundColor Red
    Write-Host "Details: $_" -ForegroundColor Red
}

Write-Host ""

# ============================================================================
# DIAGNOSTIC 5: Badge API Endpoint Test with Detailed Error Capture
# ============================================================================
Write-Host "[5/6] DIAGNOSTIC: Badge API Endpoint Test" -ForegroundColor Cyan
Write-Host "Testing GET /api/badges with error details..." -ForegroundColor Gray

try {
    $headers = @{
        "Accept" = "application/json"
    }

    $badgeResponse = Invoke-WebRequest -Uri "$apiBaseUrl/api/badges?activeOnly=true" -Method GET -Headers $headers -ErrorAction Stop

    Write-Host "SUCCESS: Badge API returned HTTP $($badgeResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Response preview:" -ForegroundColor Gray
    $content = $badgeResponse.Content | ConvertFrom-Json
    Write-Host "Badge count: $($content.Count)" -ForegroundColor Gray

    if ($content.Count -gt 0) {
        Write-Host "First badge: $($content[0] | ConvertTo-Json -Depth 2)" -ForegroundColor Gray
    }

} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "FAILED: HTTP $statusCode" -ForegroundColor Red

    if ($statusCode -eq 500) {
        Write-Host "500 Internal Server Error confirmed" -ForegroundColor Red
        Write-Host ""

        # Try to extract error details from response
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorBody = $reader.ReadToEnd()
            $reader.Close()

            Write-Host "Error response body:" -ForegroundColor Yellow
            Write-Host $errorBody
        } catch {
            Write-Host "Could not read error response body" -ForegroundColor Gray
        }
    }
}

Write-Host ""

# ============================================================================
# DIAGNOSTIC 6: Azure Container Logs - Real-time Tail
# ============================================================================
Write-Host "[6/6] DIAGNOSTIC: Azure Container Logs (Last 50 lines)" -ForegroundColor Cyan
Write-Host "Fetching recent container logs to capture exceptions..." -ForegroundColor Gray
Write-Host "Run this command WHILE testing the API to capture real-time errors:" -ForegroundColor Yellow
Write-Host ""
Write-Host "az containerapp logs show --name lankaconnect-staging --resource-group LankaConnect-rg --follow" -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Fetching last 50 log entries..." -ForegroundColor Gray
    $logs = az containerapp logs show --name lankaconnect-staging --resource-group LankaConnect-rg --tail 50 2>&1

    # Filter for Badge-related or exception entries
    $relevantLogs = $logs | Select-String -Pattern "Badge|Exception|Error|500" -Context 2,2

    if ($relevantLogs) {
        Write-Host "Relevant log entries found:" -ForegroundColor Yellow
        Write-Host $relevantLogs
    } else {
        Write-Host "No Badge-related exceptions found in recent logs" -ForegroundColor Gray
        Write-Host "This suggests either:" -ForegroundColor Yellow
        Write-Host "- Exception is not being logged" -ForegroundColor Yellow
        Write-Host "- Request is not reaching the Badge endpoint" -ForegroundColor Yellow
        Write-Host "- Error is occurring before application logging starts" -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR fetching container logs: $_" -ForegroundColor Red
    Write-Host "You may need to run: az login" -ForegroundColor Yellow
}

Write-Host ""

# ============================================================================
# DECISION TREE OUTPUT
# ============================================================================
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "DIAGNOSTIC DECISION TREE" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Based on the diagnostics above, follow this decision tree:" -ForegroundColor Yellow
Write-Host ""
Write-Host "IF migration NOT in __EFMigrationsHistory:" -ForegroundColor White
Write-Host "   ACTION: Migration failed silently during deployment" -ForegroundColor Yellow
Write-Host "   SOLUTION: Run migration manually via dotnet ef database update" -ForegroundColor Green
Write-Host ""
Write-Host "ELSE IF NULL values found in database:" -ForegroundColor White
Write-Host "   ACTION: Migration recorded but UPDATE didn't execute" -ForegroundColor Yellow
Write-Host "   SOLUTION: Run direct SQL UPDATE on staging database" -ForegroundColor Green
Write-Host ""
Write-Host "ELSE IF NO NULL values found:" -ForegroundColor White
Write-Host "   ACTION: NULL hypothesis is WRONG" -ForegroundColor Yellow
Write-Host "   SOLUTION: Investigate actual exception from logs/API response" -ForegroundColor Green
Write-Host "   POSSIBILITIES:" -ForegroundColor Yellow
Write-Host "   - BadgeLocationConfig constructor throwing on invalid values" -ForegroundColor Gray
Write-Host "   - EF Core mapping issue with value object" -ForegroundColor Gray
Write-Host "   - Authentication/Authorization failure" -ForegroundColor Gray
Write-Host "   - Unrelated database constraint violation" -ForegroundColor Gray
Write-Host ""
Write-Host "ELSE IF API returns 200:" -ForegroundColor White
Write-Host "   ACTION: Issue was resolved by migration" -ForegroundColor Green
Write-Host "   SOLUTION: Verify frontend can consume API successfully" -ForegroundColor Green
Write-Host ""

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Diagnostic suite completed" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
