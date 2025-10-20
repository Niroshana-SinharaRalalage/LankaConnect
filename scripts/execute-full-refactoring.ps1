# Master script: Execute full refactoring with checkpoints
# This script runs all phases in sequence with validation and rollback capabilities

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ZERO-ERROR INCREMENTAL REFACTORING" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will:" -ForegroundColor White
Write-Host "  1. Add canonical using statements (67 → 40-50 errors)" -ForegroundColor White
Write-Host "  2. Remove all type aliases (40-50 → 40-50 errors)" -ForegroundColor White
Write-Host "  3. Fix remaining errors (40-50 → 0 errors)" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to continue or Ctrl+C to cancel..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# =====================================================
# PHASE 1: ADD CANONICAL USING STATEMENTS
# =====================================================
Write-Host ""
Write-Host "=== PHASE 1: Adding Canonical Using Statements ===" -ForegroundColor Cyan

# Create checkpoint
git add -A
git commit -m "[Phase 1] Checkpoint: Before adding canonical using statements"

# Execute Phase 1
& "$PSScriptRoot\phase1-add-canonical-usings.ps1"

# Track errors
$phase1Errors = & "$PSScriptRoot\track-error-count.ps1" -Phase "Phase 1" -Step "After canonical usings"

Write-Host ""
Write-Host "Phase 1 Complete: $phase1Errors errors remaining" -ForegroundColor $(if ($phase1Errors -lt 60) { "Green" } else { "Yellow" })

if ($phase1Errors -gt 67) {
    Write-Host "[ERROR] Error count INCREASED! Rolling back..." -ForegroundColor Red
    git reset --hard HEAD~1
    exit 1
}

Write-Host "Press any key to continue to Phase 2..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# =====================================================
# PHASE 2: REMOVE TYPE ALIASES
# =====================================================
Write-Host ""
Write-Host "=== PHASE 2: Removing Type Aliases ===" -ForegroundColor Cyan

# Create checkpoint
git add -A
git commit -m "[Phase 2] Checkpoint: Before removing type aliases"

# Execute Phase 2
& "$PSScriptRoot\phase2-remove-all-aliases-batch.ps1" -BatchName "all"

# Track errors
$phase2Errors = & "$PSScriptRoot\track-error-count.ps1" -Phase "Phase 2" -Step "After alias removal"

Write-Host ""
Write-Host "Phase 2 Complete: $phase2Errors errors remaining" -ForegroundColor $(if ($phase2Errors -le 50) { "Green" } else { "Yellow" })

if ($phase2Errors -gt $phase1Errors + 5) {
    Write-Host "[ERROR] Error count INCREASED significantly! Rolling back..." -ForegroundColor Red
    git reset --hard HEAD~1
    exit 1
}

Write-Host "Press any key to continue to Phase 3..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# =====================================================
# PHASE 3: FIX REMAINING ERRORS
# =====================================================
Write-Host ""
Write-Host "=== PHASE 3: Fixing Remaining Errors ===" -ForegroundColor Cyan

# Create checkpoint
git add -A
git commit -m "[Phase 3] Checkpoint: Before fixing remaining errors"

# Step 3.1: Fix ambiguities
Write-Host ""
Write-Host "Step 3.1: Fixing CS0104 ambiguities..." -ForegroundColor White
& "$PSScriptRoot\phase3-fix-ambiguities.ps1"

$phase3_1Errors = & "$PSScriptRoot\track-error-count.ps1" -Phase "Phase 3.1" -Step "After ambiguity fixes"
Write-Host "Step 3.1 Complete: $phase3_1Errors errors remaining" -ForegroundColor Cyan

if ($phase3_1Errors -gt $phase2Errors) {
    Write-Host "[ERROR] Error count INCREASED! Rolling back..." -ForegroundColor Red
    git reset --hard HEAD~1
    exit 1
}

# Commit step 3.1
git add -A
git commit -m "[Phase 3.1] Fixed CS0104 ambiguous reference errors"

Write-Host ""
Write-Host "⚠️  MANUAL STEPS REQUIRED FOR PHASE 3.2, 3.3, 3.4" -ForegroundColor Yellow
Write-Host ""
Write-Host "The following steps require manual code changes:" -ForegroundColor White
Write-Host "  3.2: Implement missing interface methods (CS0535)" -ForegroundColor White
Write-Host "  3.3: Fix return type mismatches (CS0738)" -ForegroundColor White
Write-Host "  3.4: Add remaining missing types (CS0246)" -ForegroundColor White
Write-Host ""
Write-Host "Refer to:" -ForegroundColor Yellow
Write-Host "  docs/TDD_ZERO_ERROR_INCREMENTAL_REFACTORING_PLAN.md" -ForegroundColor White
Write-Host ""
Write-Host "Current error count: $phase3_1Errors" -ForegroundColor Cyan
Write-Host ""

# =====================================================
# SUMMARY
# =====================================================
Write-Host ""
Write-Host "=== REFACTORING SUMMARY ===" -ForegroundColor Cyan
Write-Host "Initial errors:         67" -ForegroundColor White
Write-Host "After Phase 1:          $phase1Errors" -ForegroundColor $(if ($phase1Errors -lt 60) { "Green" } else { "Yellow" })
Write-Host "After Phase 2:          $phase2Errors" -ForegroundColor $(if ($phase2Errors -le 50) { "Green" } else { "Yellow" })
Write-Host "After Phase 3.1:        $phase3_1Errors" -ForegroundColor $(if ($phase3_1Errors -lt 50) { "Green" } else { "Yellow" })
Write-Host ""
Write-Host "Next steps: Complete Phase 3.2-3.4 manually" -ForegroundColor Yellow
Write-Host ""

# View error log
if (Test-Path "$PSScriptRoot\..\docs\validation\error-tracking.log") {
    Write-Host "Full error tracking log:" -ForegroundColor Cyan
    Get-Content "$PSScriptRoot\..\docs\validation\error-tracking.log" | Select-Object -Last 5
}
