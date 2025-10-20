# Phase 3.1: Fix CS0104 ambiguous reference errors
# Target: MockSecurityMetricsCollector.cs
# Expected: 40-50 → 38-48 errors (-2)

$ErrorActionPreference = "Stop"

Write-Host "=== Phase 3.1: Fixing CS0104 Ambiguous Reference Errors ===" -ForegroundColor Cyan
Write-Host ""

$targetFile = "$PSScriptRoot\..\src\LankaConnect.Infrastructure\Security\MockImplementations.cs"

if (-not (Test-Path $targetFile)) {
    Write-Host "[ERROR] File not found: $targetFile" -ForegroundColor Red
    exit 1
}

$content = Get-Content -Path $targetFile -Raw

# Fix PerformanceMetrics ambiguity (line ~264)
$pattern1 = 'return Task\.FromResult\(new PerformanceMetrics\('
$replacement1 = 'return Task.FromResult(new LankaConnect.Infrastructure.Monitoring.PerformanceMetrics('

if ($content -match $pattern1) {
    $content = $content -replace $pattern1, $replacement1
    Write-Host "[OK] Fixed PerformanceMetrics ambiguity" -ForegroundColor Green
}

# Fix ComplianceMetrics ambiguity (line ~270)
$pattern2 = 'return Task\.FromResult\(new ComplianceMetrics\('
$replacement2 = 'return Task.FromResult(new LankaConnect.Infrastructure.Monitoring.ComplianceMetrics('

if ($content -match $pattern2) {
    $content = $content -replace $pattern2, $replacement2
    Write-Host "[OK] Fixed ComplianceMetrics ambiguity" -ForegroundColor Green
}

# Save the file
Set-Content -Path $targetFile -Value $content -Encoding UTF8 -NoNewline

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Fixed CS0104 errors in: MockImplementations.cs" -ForegroundColor Green
Write-Host ""
Write-Host "Next step: Run validation build" -ForegroundColor Yellow
Write-Host "  dotnet build 2>&1 | tee docs/validation/phase3-step1-after.txt" -ForegroundColor White
