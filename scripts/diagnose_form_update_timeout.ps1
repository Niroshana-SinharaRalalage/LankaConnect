# Diagnose Form Update Timeout Issue
# Phase 6A.111 - Deep Investigation Script
# Usage: .\scripts\diagnose_form_update_timeout.ps1

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Form Update Timeout Diagnostics" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# Check if Azure CLI is installed
Write-Host "[1/6] Checking Azure CLI..." -ForegroundColor Yellow
$azInstalled = Get-Command az -ErrorAction SilentlyContinue
if (-not $azInstalled) {
    Write-Host "ERROR: Azure CLI not installed. Please install from https://aka.ms/installazurecliwindows" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Azure CLI found`n" -ForegroundColor Green

# Check Azure login
Write-Host "[2/6] Checking Azure login..." -ForegroundColor Yellow
$accountInfo = az account show 2>&1 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Not logged into Azure. Please run 'az login'" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Logged in as: $($accountInfo.user.name)`n" -ForegroundColor Green

# Check backend revision history
Write-Host "[3/6] Checking backend deployment history..." -ForegroundColor Yellow
Write-Host "Fetching revisions for lankaconnect-api-staging...`n" -ForegroundColor Gray

$revisions = az containerapp revision list `
    --name lankaconnect-api-staging `
    --resource-group lankaconnect-staging `
    --query "[].{name:name,active:properties.active,trafficWeight:properties.trafficWeight,createdTime:properties.createdTime,replicas:properties.replicas}" `
    -o json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to fetch revisions" -ForegroundColor Red
    exit 1
}

Write-Host "ACTIVE REVISIONS:" -ForegroundColor Cyan
$revisions | Where-Object { $_.active -eq $true } | ForEach-Object {
    $createdDate = [DateTime]::Parse($_.createdTime).ToString("yyyy-MM-dd HH:mm:ss")
    Write-Host "  - $($_.name)" -ForegroundColor White
    Write-Host "    Created: $createdDate UTC" -ForegroundColor Gray
    Write-Host "    Traffic Weight: $($_.trafficWeight)%" -ForegroundColor Gray
    Write-Host "    Replicas: $($_.replicas)`n" -ForegroundColor Gray
}

# Check if latest revision (from fix commit 71feed37) is active
$latestRevision = $revisions | Where-Object { $_.active -eq $true } | Select-Object -First 1
$latestCreated = [DateTime]::Parse($latestRevision.createdTime)
$fixCommitTime = [DateTime]::Parse("2026-02-13T00:29:18Z")

if ($latestCreated -ge $fixCommitTime) {
    Write-Host "✓ Latest revision is from AFTER the fix (71feed37)" -ForegroundColor Green
    Write-Host "  Backend deployment is up-to-date`n" -ForegroundColor Green
} else {
    Write-Host "⚠ WARNING: Latest revision is from BEFORE the fix!" -ForegroundColor Red
    Write-Host "  Expected: >= 2026-02-13 00:29:18 UTC" -ForegroundColor Red
    Write-Host "  Actual: $($latestCreated.ToString('yyyy-MM-dd HH:mm:ss')) UTC`n" -ForegroundColor Red
}

# Search logs for UpdateFormResponse operations
Write-Host "[4/6] Searching logs for UpdateFormResponse operations (last 500 lines)..." -ForegroundColor Yellow

$logs = az containerapp logs show `
    --name lankaconnect-api-staging `
    --resource-group lankaconnect-staging `
    --tail 500 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to fetch logs" -ForegroundColor Red
    Write-Host $logs -ForegroundColor Red
    exit 1
}

# Parse logs for UpdateFormResponse entries
$updateLogs = $logs | Select-String -Pattern "UpdateFormResponse" -AllMatches

if ($updateLogs.Count -eq 0) {
    Write-Host "⚠ No UpdateFormResponse logs found in last 500 lines" -ForegroundColor Yellow
    Write-Host "  This could mean:" -ForegroundColor Gray
    Write-Host "  1. No form update attempts recently" -ForegroundColor Gray
    Write-Host "  2. Requests not reaching backend (CORS, network issue)" -ForegroundColor Gray
    Write-Host "  3. Old logs already rotated out`n" -ForegroundColor Gray
} else {
    Write-Host "✓ Found $($updateLogs.Count) UpdateFormResponse log entries`n" -ForegroundColor Green

    # Show last 10 entries
    Write-Host "RECENT LOG ENTRIES:" -ForegroundColor Cyan
    $updateLogs | Select-Object -Last 10 | ForEach-Object {
        $line = $_.Line
        if ($line -match "START") {
            Write-Host "  [START] $line" -ForegroundColor Yellow
        } elseif ($line -match "COMPLETE") {
            Write-Host "  [COMPLETE] $line" -ForegroundColor Green
        } elseif ($line -match "FAILED") {
            Write-Host "  [FAILED] $line" -ForegroundColor Red
        } else {
            Write-Host "  $line" -ForegroundColor Gray
        }
    }
    Write-Host ""

    # Check for hanging operations (START without COMPLETE)
    $startLogs = $updateLogs | Where-Object { $_.Line -match "START" }
    $completeLogs = $updateLogs | Where-Object { $_.Line -match "COMPLETE" }

    Write-Host "OPERATION SUMMARY:" -ForegroundColor Cyan
    Write-Host "  Total START: $($startLogs.Count)" -ForegroundColor White
    Write-Host "  Total COMPLETE: $($completeLogs.Count)" -ForegroundColor White

    if ($startLogs.Count -gt $completeLogs.Count) {
        $hanging = $startLogs.Count - $completeLogs.Count
        Write-Host "  ⚠ HANGING OPERATIONS: $hanging (started but never completed)" -ForegroundColor Red
        Write-Host "  This suggests backend is timing out or hanging during processing!`n" -ForegroundColor Red
    } else {
        Write-Host "  ✓ All operations completed successfully`n" -ForegroundColor Green
    }
}

# Check for errors in recent logs
Write-Host "[5/6] Checking for errors/exceptions in recent logs..." -ForegroundColor Yellow

$errorLogs = $logs | Select-String -Pattern "error|exception|timeout|failed" -AllMatches -CaseSensitive:$false

if ($errorLogs.Count -eq 0) {
    Write-Host "✓ No errors found in recent logs`n" -ForegroundColor Green
} else {
    Write-Host "⚠ Found $($errorLogs.Count) error-related log entries" -ForegroundColor Yellow
    Write-Host "RECENT ERRORS (last 5):" -ForegroundColor Cyan
    $errorLogs | Select-Object -Last 5 | ForEach-Object {
        Write-Host "  $($_.Line)" -ForegroundColor Red
    }
    Write-Host ""
}

# Summary and recommendations
Write-Host "[6/6] Diagnostic Summary & Next Steps`n" -ForegroundColor Yellow

Write-Host "================================" -ForegroundColor Cyan
Write-Host "DIAGNOSTIC RESULTS" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# Determine most likely issue
if ($latestCreated -lt $fixCommitTime) {
    Write-Host "PRIMARY ISSUE: Backend deployment is outdated" -ForegroundColor Red
    Write-Host "SOLUTION: Redeploy backend to staging`n" -ForegroundColor Yellow
}
elseif ($updateLogs.Count -eq 0) {
    Write-Host "PRIMARY ISSUE: No requests reaching backend" -ForegroundColor Red
    Write-Host "POSSIBLE CAUSES:" -ForegroundColor Yellow
    Write-Host "  1. Frontend cache still serving old code (check browser DevTools)" -ForegroundColor Gray
    Write-Host "  2. CORS issue (check browser console for CORS errors)" -ForegroundColor Gray
    Write-Host "  3. Network/firewall blocking requests`n" -ForegroundColor Gray
    Write-Host "NEXT STEPS:" -ForegroundColor Yellow
    Write-Host "  1. User: Hard refresh browser (Ctrl+Shift+R)" -ForegroundColor Cyan
    Write-Host "  2. User: Open DevTools -> Network tab" -ForegroundColor Cyan
    Write-Host "  3. User: Try updating form" -ForegroundColor Cyan
    Write-Host "  4. User: Screenshot request payload and timing`n" -ForegroundColor Cyan
}
elseif ($startLogs.Count -gt $completeLogs.Count) {
    Write-Host "PRIMARY ISSUE: Backend operations hanging/timing out" -ForegroundColor Red
    Write-Host "PROBABLE CAUSE: Database query slow or deadlock`n" -ForegroundColor Yellow
    Write-Host "SOLUTIONS (in priority order):" -ForegroundColor Yellow
    Write-Host "  1. QUICK FIX: Increase frontend timeout from 30s to 60s" -ForegroundColor Cyan
    Write-Host "     File: web/src/infrastructure/api/repositories/events.repository.ts" -ForegroundColor Gray
    Write-Host "     Line 1355: Add timeout parameter" -ForegroundColor Gray
    Write-Host "     await apiClient.put(url, request, { timeout: 60000 });`n" -ForegroundColor Gray

    Write-Host "  2. DURABLE FIX: Add database indexes" -ForegroundColor Cyan
    Write-Host "     Run: dotnet ef migrations add Phase6A111_AddFormResponseIndexes" -ForegroundColor Gray
    Write-Host "     Add indexes on FormAnswers.FormResponseId and FormQuestions.EventFormId`n" -ForegroundColor Gray

    Write-Host "  3. INVESTIGATION: Check database for locks" -ForegroundColor Cyan
    Write-Host "     Run SQL query to check active locks (see RCA document)`n" -ForegroundColor Gray
}
else {
    Write-Host "STATUS: All checks passed" -ForegroundColor Green
    Write-Host "Backend is deployed and processing requests successfully.`n" -ForegroundColor Green
    Write-Host "If user is still seeing timeout:" -ForegroundColor Yellow
    Write-Host "  1. Frontend cache issue - hard refresh (Ctrl+Shift+R)" -ForegroundColor Cyan
    Write-Host "  2. Browser timeout issue - clear browser cache" -ForegroundColor Cyan
    Write-Host "  3. Frontend deployment issue - check Vercel deployment status`n" -ForegroundColor Cyan
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Full investigation details: docs/RCA_PHASE_6A111_FORM_UPDATE_TIMEOUT_DEEP_INVESTIGATION.md" -ForegroundColor Gray
Write-Host "================================`n" -ForegroundColor Cyan

# Offer to tail logs in real-time
$tailLogs = Read-Host "Would you like to tail logs in real-time? (y/n)"
if ($tailLogs -eq 'y') {
    Write-Host "`nTailing logs (press Ctrl+C to stop)...`n" -ForegroundColor Yellow
    az containerapp logs show `
        --name lankaconnect-api-staging `
        --resource-group lankaconnect-staging `
        --tail 100 `
        --follow
}
