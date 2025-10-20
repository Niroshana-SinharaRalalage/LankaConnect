# Continuous Build Monitoring Script for Agent 4
# Monitors build errors every 3 minutes and detects regressions

param(
    [int]$IntervalSeconds = 180,  # 3 minutes
    [int]$MaxIterations = 100,
    [string]$LogFile = "docs\validation\continuous-monitoring.log"
)

Write-Host "=== Agent 4: Continuous TDD Validation & Monitoring ===" -ForegroundColor Cyan
Write-Host "Baseline: 924 errors" -ForegroundColor Yellow
Write-Host "Target: 420-460 errors (Stage 1 complete)" -ForegroundColor Yellow
Write-Host "Monitoring interval: $IntervalSeconds seconds" -ForegroundColor Yellow
Write-Host ""

$baseline = 924
$lastErrorCount = $baseline
$iteration = 0

while ($iteration -lt $MaxIterations) {
    $iteration++
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

    Write-Host "[$timestamp] Checkpoint $iteration - Building..." -ForegroundColor Cyan

    # Build and capture errors
    $buildOutput = dotnet build 2>&1 | Out-String
    $errorCount = ($buildOutput | Select-String "error CS" | Measure-Object).Count

    # Calculate change
    $change = $errorCount - $lastErrorCount
    $totalChange = $errorCount - $baseline
    $percentChange = [math]::Round((($baseline - $errorCount) / $baseline) * 100, 1)

    # Determine status
    if ($change -gt 5) {
        $status = "REGRESSION"
        $color = "Red"
    } elseif ($change -lt 0) {
        $status = "IMPROVEMENT"
        $color = "Green"
    } else {
        $status = "STABLE"
        $color = "White"
    }

    # Log results
    $logEntry = "[$timestamp] Checkpoint $iteration | Errors: $errorCount | Change: $change | Total: $totalChange ($percentChange%) | Status: $status"
    Add-Content -Path $LogFile -Value $logEntry

    Write-Host "  Errors: $errorCount" -ForegroundColor $color
    Write-Host "  Change from last: $change" -ForegroundColor $color
    Write-Host "  Total change: $totalChange ($percentChange%)" -ForegroundColor $color
    Write-Host "  Status: $status" -ForegroundColor $color

    # CRITICAL: Regression detected
    if ($change -gt 5) {
        Write-Host ""
        Write-Host "!!! CRITICAL REGRESSION DETECTED !!!" -ForegroundColor Red
        Write-Host "Error count increased by $change errors!" -ForegroundColor Red
        Write-Host "Alerting all agents..." -ForegroundColor Red

        # Alert via memory
        npx claude-flow@alpha memory store "swarm/agent4/regression_alert" "$errorCount" --namespace "validation"
        npx claude-flow@alpha hooks notify --message "REGRESSION: Build errors increased from $lastErrorCount to $errorCount (+$change)"

        Write-Host ""
    }

    # Store checkpoint in memory
    npx claude-flow@alpha memory store "swarm/agent4/checkpoint_$iteration" "$errorCount" --namespace "validation" 2>&1 | Out-Null

    # Check if target reached
    if ($errorCount -le 460 -and $errorCount -ge 420) {
        Write-Host ""
        Write-Host "=== TARGET REACHED ===" -ForegroundColor Green
        Write-Host "Stage 1 Complete: $errorCount errors (target: 420-460)" -ForegroundColor Green
        Write-Host "Total reduction: $totalChange errors ($percentChange%)" -ForegroundColor Green
        npx claude-flow@alpha hooks notify --message "Stage 1 COMPLETE: $errorCount errors reached (target: 420-460)"
        break
    }

    $lastErrorCount = $errorCount

    # Wait for next iteration
    if ($iteration -lt $MaxIterations) {
        Write-Host "  Waiting $IntervalSeconds seconds..." -ForegroundColor Gray
        Start-Sleep -Seconds $IntervalSeconds
    }
}

Write-Host ""
Write-Host "=== Monitoring Complete ===" -ForegroundColor Cyan
Write-Host "Total iterations: $iteration" -ForegroundColor Yellow
Write-Host "Final error count: $lastErrorCount" -ForegroundColor Yellow
Write-Host "Total reduction: $($baseline - $lastErrorCount) errors" -ForegroundColor Yellow
