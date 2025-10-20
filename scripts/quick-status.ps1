# Quick Status Check - Stages 3-4 Validation
# Fast error count and distribution check

Write-Host "`n=== STAGES 3-4 TDD VALIDATION STATUS ===" -ForegroundColor Cyan
Write-Host "Timestamp: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

Write-Host "`n[1/3] Counting total errors..." -ForegroundColor Yellow
$totalErrors = (dotnet build 2>&1 | Select-String -Pattern "error CS" -AllMatches).Matches.Count
Write-Host "Total Errors: $totalErrors" -ForegroundColor $(if ($totalErrors -le 194) { "Green" } elseif ($totalErrors -le 674) { "Yellow" } else { "Red" })

Write-Host "`n[2/3] Analyzing error distribution..." -ForegroundColor Yellow
$buildOutput = dotnet build 2>&1 | Out-String
$errorTypes = @{}
$matches = $buildOutput | Select-String -Pattern "error (CS\d+)" -AllMatches
foreach ($match in $matches.Matches) {
    $errorCode = $match.Groups[1].Value
    if ($errorTypes.ContainsKey($errorCode)) {
        $errorTypes[$errorCode]++
    } else {
        $errorTypes[$errorCode] = 1
    }
}

Write-Host "`nError Type Distribution:" -ForegroundColor Cyan
foreach ($errorType in $errorTypes.Keys | Sort-Object { $errorTypes[$_] } -Descending) {
    $count = $errorTypes[$errorType]
    $percentage = [math]::Round(($count / $totalErrors) * 100, 1)

    $color = switch ($errorType) {
        "CS0535" { if ($count -eq 0) { "Green" } else { "Yellow" } }
        "CS0738" { if ($count -eq 0) { "Green" } else { "Yellow" } }
        "CS0101" { if ($count -le 2) { "Green" } else { "Yellow" } }
        default { "White" }
    }

    Write-Host "  $errorType : $count ($percentage%)" -ForegroundColor $color
}

Write-Host "`n[3/3] Stage progress assessment..." -ForegroundColor Yellow

# Stage 3 check
$cs0101Count = if ($errorTypes.ContainsKey("CS0101")) { $errorTypes["CS0101"] } else { 0 }
$stage3Status = if ($cs0101Count -le 2) { "COMPLETE" } elseif ($cs0101Count -eq 4) { "NOT STARTED" } else { "IN PROGRESS" }
$stage3Color = if ($stage3Status -eq "COMPLETE") { "Green" } elseif ($stage3Status -eq "IN PROGRESS") { "Yellow" } else { "White" }
Write-Host "`nStage 3 (Duplicates): $stage3Status" -ForegroundColor $stage3Color
Write-Host "  CS0101 count: $cs0101Count (target: ≤2)" -ForegroundColor $stage3Color

# Stage 4 check
$cs0535Count = if ($errorTypes.ContainsKey("CS0535")) { $errorTypes["CS0535"] } else { 0 }
$cs0738Count = if ($errorTypes.ContainsKey("CS0738")) { $errorTypes["CS0738"] } else { 0 }
$interfaceErrors = $cs0535Count + $cs0738Count
$stage4Status = if ($interfaceErrors -eq 0) { "COMPLETE" } elseif ($interfaceErrors -lt 478) { "IN PROGRESS" } else { "NOT STARTED" }
$stage4Color = if ($stage4Status -eq "COMPLETE") { "Green" } elseif ($stage4Status -eq "IN PROGRESS") { "Yellow" } else { "White" }
Write-Host "`nStage 4 (Interfaces): $stage4Status" -ForegroundColor $stage4Color
Write-Host "  CS0535 count: $cs0535Count (target: 0)" -ForegroundColor $stage4Color
Write-Host "  CS0738 count: $cs0738Count (target: 0)" -ForegroundColor $stage4Color
Write-Host "  Total interface errors: $interfaceErrors (baseline: 478)" -ForegroundColor $stage4Color

# Overall progress
$baseline = 676
$reduction = $baseline - $totalErrors
$reductionPercent = [math]::Round(($reduction / $baseline) * 100, 1)
Write-Host "`n=== OVERALL PROGRESS ===" -ForegroundColor Cyan
Write-Host "Baseline: $baseline errors" -ForegroundColor Gray
Write-Host "Current: $totalErrors errors" -ForegroundColor $(if ($totalErrors -le 194) { "Green" } else { "Yellow" })
Write-Host "Reduction: $reduction errors ($reductionPercent%)" -ForegroundColor $(if ($reduction -ge 482) { "Green" } elseif ($reduction -gt 0) { "Yellow" } else { "Red" })
Write-Host "Target: ~194 errors" -ForegroundColor Gray

if ($totalErrors -le 194 -and $interfaceErrors -eq 0) {
    Write-Host "`n SUCCESS: Stages 3-4 COMPLETE!" -ForegroundColor Green
    Write-Host "Ready to proceed to Stage 5 (Type definitions)" -ForegroundColor Green
} elseif ($interfaceErrors -eq 0) {
    Write-Host "`n Stage 4 COMPLETE (interfaces fixed)" -ForegroundColor Green
    Write-Host "Note: Total errors higher than expected. Review error types." -ForegroundColor Yellow
} elseif ($stage3Status -eq "COMPLETE") {
    Write-Host "`n Stage 3 COMPLETE, Stage 4 in progress..." -ForegroundColor Yellow
} else {
    Write-Host "`n Stages in progress..." -ForegroundColor Yellow
}

Write-Host "`n========================================`n" -ForegroundColor Cyan
