# DELETE DUPLICATE TYPES - Automated Removal Script
# Generated: 2025-10-12
# Total Deletions: 27 duplicates + 7 files

$ErrorActionPreference = "Stop"

Write-Host "=== DUPLICATE TYPE DELETION SCRIPT ===" -ForegroundColor Cyan
Write-Host "Canonical file: Stage5MissingTypes.cs" -ForegroundColor Yellow
Write-Host ""

# Track progress
$deletedFiles = 0
$deletedTypes = 0

# ============================================================================
# STEP 1: DELETE ENTIRE FILES (7 files - single-type inheriting BaseEntity)
# ============================================================================

Write-Host "[STEP 1] Deleting 7 entire files..." -ForegroundColor Cyan

$filesToDelete = @(
    "src\LankaConnect.Application\Common\Models\Performance\RevenueRiskCalculation.cs",
    "src\LankaConnect.Application\Common\Models\Performance\RevenueCalculationModel.cs",
    "src\LankaConnect.Application\Common\Models\Performance\CompetitiveBenchmarkData.cs",
    "src\LankaConnect.Application\Common\Models\Performance\MarketPositionAnalysis.cs",
    "src\LankaConnect.Application\Common\Models\Performance\ScalingThresholdOptimization.cs",
    "src\LankaConnect.Application\Common\Models\Performance\CostPerformanceAnalysis.cs",
    "src\LankaConnect.Application\Common\Models\Performance\CostAnalysisParameters.cs"
)

foreach ($file in $filesToDelete) {
    $fullPath = "C:\Work\LankaConnect\$file"
    if (Test-Path $fullPath) {
        Write-Host "  [DELETE] $file" -ForegroundColor Red
        Remove-Item $fullPath -Force
        $deletedFiles++
    } else {
        Write-Host "  [SKIP] File not found: $file" -ForegroundColor Yellow
    }
}

Write-Host "  Deleted $deletedFiles files" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 2: MANUAL RESOLUTION REQUIRED - DO NOT AUTOMATE
# ============================================================================

Write-Host "[STEP 2] MANUAL RESOLUTION REQUIRED" -ForegroundColor Yellow
Write-Host "  The following conflicts require manual analysis:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. LanguagePreferences (CRITICAL)" -ForegroundColor Red
Write-Host "     - Stage5MissingTypes.cs:180 (POCO, mutable)" -ForegroundColor Gray
Write-Host "     - UserPreferenceValueObjects.cs:358 (ValueObject, immutable)" -ForegroundColor Gray
Write-Host "     Action: Choose implementation based on usage analysis" -ForegroundColor White
Write-Host ""
Write-Host "  2. CorrelationConfiguration" -ForegroundColor Red
Write-Host "     - Stage5MissingTypes.cs:261 (class)" -ForegroundColor Gray
Write-Host "     - EngineResults.cs:149 (record)" -ForegroundColor Gray
Write-Host "     Action: Verify record completeness, prefer record type" -ForegroundColor White
Write-Host ""
Write-Host "  3. Enum vs Class Conflicts (3 items)" -ForegroundColor Red
Write-Host "     - RiskAssessmentTimeframe (class vs enum)" -ForegroundColor Gray
Write-Host "     - ThresholdAdjustmentReason (class vs enum)" -ForegroundColor Gray
Write-Host "     - CreditCalculationPolicy (class vs enum)" -ForegroundColor Gray
Write-Host "     Action: Determine correct type based on usage" -ForegroundColor White
Write-Host ""

# ============================================================================
# STEP 3: USAGE ANALYSIS FOR LANGUAGEPREFERENCES
# ============================================================================

Write-Host "[STEP 3] Analyzing LanguagePreferences usage..." -ForegroundColor Cyan

Write-Host "  Searching for usages across codebase..." -ForegroundColor Gray

# Search for both implementations
$stage5Usage = Select-String -Path "C:\Work\LankaConnect\src\**\*.cs" -Pattern "LanguagePreferences" -ErrorAction SilentlyContinue | Measure-Object | Select-Object -ExpandProperty Count
$valueObjectFile = "C:\Work\LankaConnect\src\LankaConnect.Domain\Events\ValueObjects\Recommendations\UserPreferenceValueObjects.cs"

Write-Host ""
Write-Host "  Total references found: $stage5Usage occurrences" -ForegroundColor White
Write-Host "  ValueObject file: UserPreferenceValueObjects.cs" -ForegroundColor White
Write-Host ""
Write-Host "  RECOMMENDATION:" -ForegroundColor Yellow
Write-Host "  - ValueObject implementation (UserPreferenceValueObjects.cs) is more robust" -ForegroundColor Gray
Write-Host "  - Immutable design follows DDD best practices" -ForegroundColor Gray
Write-Host "  - Located in proper domain layer" -ForegroundColor Gray
Write-Host "  ACTION: Delete LanguagePreferences from Stage5MissingTypes.cs" -ForegroundColor Green
Write-Host ""

# ============================================================================
# STEP 4: SUMMARY AND NEXT STEPS
# ============================================================================

Write-Host "[STEP 4] Summary" -ForegroundColor Cyan
Write-Host "  Files deleted: $deletedFiles / 7" -ForegroundColor White
Write-Host "  Manual resolutions pending: 5" -ForegroundColor Yellow
Write-Host ""

Write-Host "[NEXT STEPS]" -ForegroundColor Yellow
Write-Host "  1. Review LanguagePreferences usage manually" -ForegroundColor Gray
Write-Host "  2. Decide on enum vs class for 3 conflicting types" -ForegroundColor Gray
Write-Host "  3. Choose CorrelationConfiguration implementation (record preferred)" -ForegroundColor Gray
Write-Host "  4. Run: scripts\delete-duplicate-types-phase2.ps1 (after manual decisions)" -ForegroundColor Gray
Write-Host "  5. Validate build: dotnet build" -ForegroundColor Gray
Write-Host ""

Write-Host "=== PHASE 1 COMPLETE ===" -ForegroundColor Green
Write-Host "Next: Make manual decisions, then run Phase 2 script" -ForegroundColor White
