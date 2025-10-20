# Delete Phase 2 Test Files - Cleanup Script
# Removes test files that reference deleted Phase 2 production code

Write-Host "`n=== Phase 2 Test Cleanup ===" -ForegroundColor Cyan
Write-Host "Deleting test files for Cultural Intelligence, Disaster Recovery, Load Balancing`n" -ForegroundColor Yellow

# Track deletions
$deletedDirs = 0
$deletedFiles = 0

# Delete Phase 2 test directories
$phase2Dirs = @(
    "tests\LankaConnect.Application.Tests\Common\DisasterRecovery",
    "tests\LankaConnect.Application.Tests\Common\Enterprise",
    "tests\LankaConnect.Application.Tests\Common\CulturalIntelligence",
    "tests\LankaConnect.Domain.Tests\Common\DisasterRecovery",
    "tests\LankaConnect.IntegrationTests\CulturalIntelligence"
)

foreach ($dir in $phase2Dirs) {
    if (Test-Path $dir) {
        $fileCount = (Get-ChildItem $dir -Filter "*.cs" -Recurse).Count
        Remove-Item $dir -Recurse -Force
        Write-Host "[DELETED DIR] $dir ($fileCount files)" -ForegroundColor Green
        $deletedDirs++
        $deletedFiles += $fileCount
    } else {
        Write-Host "[SKIP] $dir (not found)" -ForegroundColor DarkGray
    }
}

# Delete individual Phase 2 test files
$individualTests = @(
    "tests\LankaConnect.Application.Tests\Common\Monitoring\MonitoringTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Routing\RoutingTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Security\CrossRegionSecurityTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Security\SecurityFoundationTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Security\SecurityResultTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Performance\PerformanceMonitoringResultTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Performance\AutoScalingPerformanceTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\PerformanceObjectiveTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Revenue\RevenueOptimizationTypesTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Models\BackupScheduleResultTests.cs",
    "tests\LankaConnect.Application.Tests\Common\Models\Performance\PerformanceAlertTests.cs",
    "tests\LankaConnect.Application.Tests\Common\CulturalIntelligence\SecurityComplianceTypesTests.cs",
    "tests\LankaConnect.Domain.Tests\Common\Database\LoadBalancingConfigurationTests.cs",
    "tests\LankaConnect.Domain.Tests\Business\RevenueProtectionStrategyTests.cs",
    "tests\LankaConnect.IntegrationTests\Cache\CacheAsidePatternIntegrationTests.cs"
)

foreach ($file in $individualTests) {
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "[DELETED FILE] $file" -ForegroundColor Green
        $deletedFiles++
    } else {
        Write-Host "[SKIP] $file (not found)" -ForegroundColor DarkGray
    }
}

Write-Host "`n=== Deletion Summary ===" -ForegroundColor Cyan
Write-Host "Directories deleted: $deletedDirs" -ForegroundColor Green
Write-Host "Files deleted: $deletedFiles" -ForegroundColor Green

# Build to verify error count
Write-Host "`n=== Building to verify error reduction ===" -ForegroundColor Cyan
$buildOutput = dotnet build 2>&1
$errorCount = ($buildOutput | Select-String "error CS").Count

Write-Host "`nBuild Errors: $errorCount" -ForegroundColor $(if ($errorCount -lt 150) { "Green" } else { "Red" })

if ($errorCount -lt 150) {
    Write-Host "`n✅ SUCCESS: Error count reduced to MVP baseline" -ForegroundColor Green
    Write-Host "Expected: ~100-120 errors (MVP fixes needed)" -ForegroundColor Yellow
} else {
    Write-Host "`n⚠️ WARNING: Error count higher than expected" -ForegroundColor Red
    Write-Host "Expected: ~100-120 errors, Got: $errorCount" -ForegroundColor Yellow
}

# Show error breakdown
Write-Host "`n=== Error Breakdown ===" -ForegroundColor Cyan
$buildOutput | Select-String "error CS" | ForEach-Object { $_.ToString() -replace '^.*?(error CS\d+).*$','$1' } |
    Group-Object | Sort-Object Count -Descending | Select-Object -First 10 |
    ForEach-Object { Write-Host "$($_.Name): $($_.Count)" -ForegroundColor Yellow }

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Review error count (should be ~100-120)" -ForegroundColor White
Write-Host "2. Commit deletion: git add tests/ && git commit -m 'Delete Phase 2 test files'" -ForegroundColor White
Write-Host "3. Fix remaining MVP errors systematically" -ForegroundColor White
