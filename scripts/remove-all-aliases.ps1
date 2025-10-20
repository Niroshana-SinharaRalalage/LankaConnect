# Remove ALL namespace aliases from codebase
# Per HIVE_MIND_CODE_REVIEW_REPORT.md - Zero tolerance for using aliases

$files = @(
    "src\LankaConnect.Application\Common\Interfaces\IMultiLanguageAffinityRoutingEngine.cs",
    "src\LankaConnect.Domain\Shared\LanguageRoutingTypes.cs",
    "src\LankaConnect.Application\Common\Interfaces\IDatabasePerformanceMonitoringEngine.cs",
    "src\LankaConnect.Infrastructure\Security\MockImplementations.cs",
    "src\LankaConnect.Infrastructure\Monitoring\CulturalIntelligenceMetricsService.cs",
    "src\LankaConnect.Infrastructure\Database\LoadBalancing\MultiLanguageAffinityRoutingEngine.cs",
    "src\LankaConnect.Infrastructure\Database\LoadBalancing\DatabasePerformanceMonitoringEngine.cs",
    "src\LankaConnect.Infrastructure\Database\LoadBalancing\BackupDisasterRecoveryEngine.cs",
    "src\LankaConnect.Infrastructure\Database\LoadBalancing\CulturalAffinityGeographicLoadBalancer.cs",
    "src\LankaConnect.Infrastructure\Security\ICulturalSecurityService.cs",
    "src\LankaConnect.Application\Common\Interfaces\IDatabaseSecurityOptimizationEngine.cs",
    "src\LankaConnect.Application\Common\Interfaces\IAutoScalingConnectionPoolEngine.cs",
    "src\LankaConnect.Application\Common\Security\AuditAccessTypes.cs",
    "src\LankaConnect.Infrastructure\Database\Scaling\CulturalIntelligencePredictiveScalingService.cs",
    "src\LankaConnect.Application\Common\Interfaces\IBackupDisasterRecoveryEngine.cs",
    "src\LankaConnect.Application\Common\Interfaces\ICulturalConflictResolutionEngine.cs",
    "src\LankaConnect.Infrastructure\Database\Optimization\CulturalIntelligenceQueryOptimizer.cs",
    "src\LankaConnect.Application\Common\Interfaces\ICulturalIntelligenceCacheService.cs",
    "src\LankaConnect.Infrastructure\Database\Sharding\CulturalIntelligenceShardingService.cs",
    "src\LankaConnect.Application\Common\Interfaces\ICulturalIntelligenceConsistencyService.cs",
    "src\LankaConnect.Infrastructure\Database\Consistency\CulturalIntelligenceConsistencyService.cs"
)

$totalAliases = 0
$filesProcessed = 0

Write-Host "🚨 ALIAS REMOVAL - ZERO TOLERANCE POLICY" -ForegroundColor Red
Write-Host "Per HIVE_MIND_CODE_REVIEW_REPORT.md: Remove ALL namespace aliases" -ForegroundColor Yellow
Write-Host ""

foreach ($file in $files) {
    $fullPath = Join-Path "C:\Work\LankaConnect" $file

    if (Test-Path $fullPath) {
        $content = Get-Content $fullPath -Raw
        $lines = Get-Content $fullPath

        # Count aliases in this file
        $aliasCount = ($lines | Select-String "^using .+ = .+;" | Measure-Object).Count

        if ($aliasCount -gt 0) {
            Write-Host "Processing: $file" -ForegroundColor Cyan
            Write-Host "  Found $aliasCount aliases" -ForegroundColor Yellow

            # Remove alias lines
            $cleaned = $lines | Where-Object { $_ -notmatch "^using .+ = .+;" }

            # Write back
            $cleaned | Set-Content $fullPath

            $totalAliases += $aliasCount
            $filesProcessed++

            Write-Host "  ✅ Removed $aliasCount aliases" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "═══════════════════════════════════════" -ForegroundColor Green
Write-Host "✅ ALIAS REMOVAL COMPLETE" -ForegroundColor Green
Write-Host "Files processed: $filesProcessed" -ForegroundColor Cyan
Write-Host "Total aliases removed: $totalAliases" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════" -ForegroundColor Green
