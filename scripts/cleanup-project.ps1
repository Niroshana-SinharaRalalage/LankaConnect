# LankaConnect Project Cleanup Script
# Created: 2025-12-31
# Purpose: Remove unwanted files, build artifacts, logs, and obsolete documentation

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LankaConnect Project Cleanup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$totalDeleted = 0
$totalSizeFreed = 0

# Function to calculate directory size
function Get-DirectorySize {
    param([string]$path)
    if (Test-Path $path) {
        $size = (Get-ChildItem -Path $path -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum
        return [math]::Round($size / 1MB, 2)
    }
    return 0
}

# Function to delete with confirmation
function Remove-ItemSafe {
    param([string]$path, [string]$description)

    if (Test-Path $path) {
        $sizeMB = Get-DirectorySize $path
        Write-Host "Deleting: $description" -ForegroundColor Yellow
        Write-Host "  Path: $path" -ForegroundColor Gray
        Write-Host "  Size: $sizeMB MB" -ForegroundColor Gray

        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue

        if (-not (Test-Path $path)) {
            Write-Host "  ✓ Successfully deleted" -ForegroundColor Green
            $script:totalDeleted++
            $script:totalSizeFreed += $sizeMB
        } else {
            Write-Host "  ✗ Failed to delete" -ForegroundColor Red
        }
        Write-Host ""
    } else {
        Write-Host "Skipping: $description (not found)" -ForegroundColor DarkGray
        Write-Host "  Path: $path" -ForegroundColor DarkGray
        Write-Host ""
    }
}

Write-Host "PHASE 1: Next.js Build Artifacts" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Remove-ItemSafe "web\.next" "Next.js build directory (380MB)"

Write-Host "PHASE 2: Test and Demo Pages" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Remove-ItemSafe "web\src\app\test-events" "Test events page"
Remove-ItemSafe "web\src\app\test-simple" "Simple test page"
Remove-ItemSafe "web\src\app\component-demo" "Component demo page"

Write-Host "PHASE 3: Root-Level Log Files" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
$rootLogs = @(
    "build-analysis.log",
    "build_errors.log",
    "build_errors_complete.log",
    "compilation_errors_analysis.log",
    "package-analysis.log"
)

foreach ($log in $rootLogs) {
    Remove-ItemSafe $log "Root log file: $log"
}

Write-Host "PHASE 4: Backend API Logs" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Remove-ItemSafe "src\LankaConnect.API\logs" "Backend API logs directory"

Write-Host "PHASE 5: Analysis and Temporary Files" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Remove-ItemSafe "docs\duplicate_analysis.json" "Duplicate analysis file"
Remove-ItemSafe "docs\error_progress.log" "Error progress log"

Write-Host "PHASE 6: Obsolete Documentation (Archived)" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

# Create archive directory for old docs
$archiveDir = "docs\archive\obsolete-analysis"
if (-not (Test-Path $archiveDir)) {
    New-Item -Path $archiveDir -ItemType Directory -Force | Out-Null
    Write-Host "Created archive directory: $archiveDir" -ForegroundColor Cyan
    Write-Host ""
}

# List of obsolete analysis/RCA documents to archive
$obsoleteDocs = @(
    "docs\AUTHENTICATION_COOKIE_ARCHITECTURE_ANALYSIS.md",
    "docs\AUTHENTICATION_ISSUE_ROOT_CAUSE_ANALYSIS.md",
    "docs\AUTHORIZATION_500_ERROR_ROOT_CAUSE_ANALYSIS.md",
    "docs\BADGE_500_IMMEDIATE_ACTIONS.md",
    "docs\Badge_Migration_Fix_Summary.md",
    "docs\CI_WORKFLOW_ISSUE_ANALYSIS.md",
    "docs\COMPLETE_41_ENUM_TYPES_RCA_AND_FIX_PLAN.md",
    "docs\COMPLETE_ENUM_AUDIT_FRONTEND_BACKEND.md",
    "docs\CORS_DEBUGGING_SUMMARY.md",
    "docs\CORS_RESOLUTION_NEXT_STEPS.md",
    "docs\CSV_EXPORT_RCA.md",
    "docs\CSV_EXPORT_FIX_PLAN.md",
    "docs\DEBUGGING_LOGIN_ISSUE.md",
    "docs\DIAGNOSIS_FREE_EVENT_EMAIL_FAILURE.md",
    "docs\DOMAIN_EVENT_DIAGNOSTIC_ANALYSIS.md",
    "docs\DOMAIN_EVENT_FIX_SUMMARY.md",
    "docs\EMAIL_SENDING_FAILURE_COMPREHENSIVE_ANALYSIS.md",
    "docs\EMAIL_SENDING_FAILURE_COMPREHENSIVE_RCA.md",
    "docs\EMAIL_SENDING_FAILURE_RCA.md",
    "docs\2025-12-10_NON_API_CAPABILITIES_ANALYSIS.md",
    "docs\architecture\BADGE_500_ERROR_ROOT_CAUSE_ANALYSIS.md",
    "docs\architecture\CRITICAL_ISSUES_ANALYSIS.md",
    "docs\architecture\DOMAIN_EVENT_COLLECTION_ROOT_CAUSE_ANALYSIS.md",
    "docs\architecture\IMAGE_UPLOAD_500_ERROR_ANALYSIS.md",
    "docs\architecture\PAYMENT_WEBHOOK_ROOT_CAUSE_ANALYSIS.md",
    "docs\architecture\PHASE_6A_24_WEBHOOK_SECRET_ROOT_CAUSE_ANALYSIS.md",
    "docs\architecture\ROOT_CAUSE_ANALYSIS_COMMITMENTS_MISSING.md",
    "docs\EVENT_CREATION_500_ERROR_ARCHITECTURE_ANALYSIS.md",
    "docs\EVENT_INTERESTS_ROOT_CAUSE_ANALYSIS.md",
    "docs\HMR_FAILURE_ROOT_CAUSE_ANALYSIS.md",
    "docs\INDEX_EVENT_API_FAILURE_ANALYSIS.md",
    "docs\LANDING_PAGE_REUSABLE_COMPONENTS_ANALYSIS.md",
    "docs\MY_REGISTRATION_500_ERROR_RCA.md",
    "docs\PAID_EVENT_REGISTRATION_ISSUES_PHASE2_RCA.md",
    "docs\PAID_EVENT_REGISTRATION_ISSUES_RCA.md",
    "docs\PHASE_6A28_ISSUE_4_DIAGNOSTIC_AND_FIX_PLAN.md",
    "docs\PHASE_6A32_BADGE_ZERO_VALUES_ROOT_CAUSE_ANALYSIS.md",
    "docs\PHASE_6A33_UPDATE_DIAGNOSTIC.md",
    "docs\PHASE_6A44_ANONYMOUS_SIGNUP_RCA.md"
)

Write-Host "Moving obsolete analysis docs to archive..." -ForegroundColor Yellow
$archivedCount = 0

foreach ($doc in $obsoleteDocs) {
    if (Test-Path $doc) {
        $fileName = Split-Path $doc -Leaf
        $destPath = Join-Path $archiveDir $fileName

        try {
            Move-Item -Path $doc -Destination $destPath -Force
            Write-Host "  ✓ Archived: $fileName" -ForegroundColor Green
            $archivedCount++
        } catch {
            Write-Host "  ✗ Failed to archive: $fileName" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "Archived $archivedCount obsolete documentation files" -ForegroundColor Cyan
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CLEANUP SUMMARY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Items deleted: $totalDeleted" -ForegroundColor Green
Write-Host "Space freed: $totalSizeFreed MB" -ForegroundColor Green
Write-Host "Documents archived: $archivedCount" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Run 'git status' to see pending changes" -ForegroundColor White
Write-Host "2. Review the archived documents in: $archiveDir" -ForegroundColor White
Write-Host "3. Commit the deletions: git add . && git commit -m 'chore: cleanup project files'" -ForegroundColor White
Write-Host ""
Write-Host "Cleanup completed successfully!" -ForegroundColor Green
