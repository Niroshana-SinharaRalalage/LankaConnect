# Badge Migration Verification Script (PowerShell)
# Purpose: Verify that migration 20251216150703_UpdateBadgeLocationConfigsWithDefaults is deployed
# Usage: .\scripts\verify-badge-migration.ps1

$ErrorActionPreference = "Stop"

$API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
$MIGRATION_NAME = "20251216150703_UpdateBadgeLocationConfigsWithDefaults"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Badge Migration Deployment Verification" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if token file exists
if (-not (Test-Path "token.txt")) {
    Write-Host "❌ ERROR: token.txt not found" -ForegroundColor Red
    Write-Host "Please create a token.txt file with a valid Bearer token"
    exit 1
}

$TOKEN = Get-Content "token.txt" -Raw
$TOKEN = $TOKEN.Trim()

Write-Host "1️⃣  Testing Badge Management API Endpoint..." -ForegroundColor Yellow
Write-Host "-----------------------------------------------------------------"

try {
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
        "Accept" = "application/json"
    }

    $response = Invoke-WebRequest -Uri "$API_BASE/api/badges" -Headers $headers -Method GET
    $statusCode = $response.StatusCode
    $content = $response.Content

    Write-Host "HTTP Status Code: $statusCode" -ForegroundColor Green

    if ($statusCode -eq 200) {
        Write-Host "✅ SUCCESS: Badge API returned 200 OK" -ForegroundColor Green
        Write-Host ""
        Write-Host "Response preview:"
        $content | ConvertFrom-Json | ConvertTo-Json -Depth 3
        Write-Host ""
        Write-Host "✅ MIGRATION DEPLOYED SUCCESSFULLY" -ForegroundColor Green
        $MIGRATION_STATUS = "DEPLOYED ✅"
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "HTTP Status Code: $statusCode" -ForegroundColor Red

    if ($statusCode -eq 500) {
        Write-Host "❌ FAILURE: Badge API returned 500 Internal Server Error" -ForegroundColor Red
        Write-Host ""
        Write-Host "Error response:"
        $_.Exception.Response | Out-String
        Write-Host ""
        Write-Host "❌ MIGRATION NOT YET DEPLOYED" -ForegroundColor Red
        $MIGRATION_STATUS = "NOT DEPLOYED ❌"
    } else {
        Write-Host "⚠️  UNEXPECTED: HTTP $statusCode" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Error:"
        $_.Exception.Message
        Write-Host ""
        $MIGRATION_STATUS = "UNKNOWN ⚠️"
    }
}

Write-Host ""
Write-Host "2️⃣  Checking GitHub Actions Deployment Status..." -ForegroundColor Yellow
Write-Host "-----------------------------------------------------------------"

Write-Host "Latest GitHub Actions workflows:"
Write-Host "https://github.com/[org]/LankaConnect/actions?query=branch:develop"
Write-Host ""
Write-Host "Look for workflow triggered by commit: a359fea"
Write-Host "Check 'Run EF Migrations' step for: '✅ Migrations completed successfully'"

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Migration: $MIGRATION_NAME"
Write-Host "Status:    $MIGRATION_STATUS"
Write-Host ""

if ($MIGRATION_STATUS -eq "DEPLOYED ✅") {
    Write-Host "✅ ALL SYSTEMS OPERATIONAL" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:"
    Write-Host "1. Test Badge Management page in UI: http://localhost:3000/dashboard"
    Write-Host "2. Verify existing badges display correctly"
    Write-Host "3. Try creating a new badge to test defaults"
    exit 0
} elseif ($MIGRATION_STATUS -eq "NOT DEPLOYED ❌") {
    Write-Host "❌ MIGRATION DEPLOYMENT REQUIRED" -ForegroundColor Red
    Write-Host ""
    Write-Host "Resolution Steps:"
    Write-Host "1. Check GitHub Actions: https://github.com/[org]/LankaConnect/actions"
    Write-Host "2. If workflow not triggered, run:"
    Write-Host "   git commit --allow-empty -m 'chore: trigger staging deployment'"
    Write-Host "   git push origin develop"
    Write-Host "3. Wait 2-3 minutes for deployment to complete"
    Write-Host "4. Re-run this script to verify"
    exit 1
} else {
    Write-Host "⚠️  UNEXPECTED STATUS - MANUAL INVESTIGATION REQUIRED" -ForegroundColor Yellow
    exit 2
}
