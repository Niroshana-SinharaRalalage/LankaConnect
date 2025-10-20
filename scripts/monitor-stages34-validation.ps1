# Stages 3-4 TDD Validation Monitor
# Continuous build monitoring with automated checkpoints

$ErrorActionPreference = "Stop"
$logFile = "C:\Work\LankaConnect\docs\validation-monitoring-log.md"
$checkpointInterval = 120 # 2 minutes

# Initialize log
@"
# Stages 3-4 TDD Validation Log
**Started:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Baseline:** 676 errors
**Target:** ~194 errors (CS0535/CS0738 = 0)

## Error Checkpoints

| Timestamp | Total Errors | CS0535 | CS0738 | CS0246 | CS0234 | Delta | Stage/Fix |
|-----------|--------------|--------|--------|--------|--------|-------|-----------|
"@ | Out-File -FilePath $logFile -Encoding UTF8

# Baseline measurement
function Get-BuildMetrics {
    $buildOutput = dotnet build 2>&1 | Out-String

    $totalErrors = ($buildOutput | Select-String -Pattern "error CS" -AllMatches).Matches.Count

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

    return @{
        Total = $totalErrors
        Types = $errorTypes
        Timestamp = Get-Date
    }
}

# Record checkpoint
function Write-Checkpoint {
    param(
        [object]$Metrics,
        [int]$PreviousTotal,
        [string]$Stage
    )

    $delta = $Metrics.Total - $PreviousTotal
    $deltaStr = if ($delta -gt 0) { "+$delta" } elseif ($delta -lt 0) { "$delta" } else { "0" }

    $cs0535 = if ($Metrics.Types.ContainsKey("CS0535")) { $Metrics.Types["CS0535"] } else { 0 }
    $cs0738 = if ($Metrics.Types.ContainsKey("CS0738")) { $Metrics.Types["CS0738"] } else { 0 }
    $cs0246 = if ($Metrics.Types.ContainsKey("CS0246")) { $Metrics.Types["CS0246"] } else { 0 }
    $cs0234 = if ($Metrics.Types.ContainsKey("CS0234")) { $Metrics.Types["CS0234"] } else { 0 }

    $timestamp = $Metrics.Timestamp.ToString("HH:mm:ss")
    $row = "| $timestamp | $($Metrics.Total) | $cs0535 | $cs0738 | $cs0246 | $cs0234 | $deltaStr | $Stage |"

    Add-Content -Path $logFile -Value $row

    # Alert on increase
    if ($delta -gt 0) {
        Write-Host "ALERT: Error count INCREASED by $delta!" -ForegroundColor Red
    } elseif ($delta -lt 0) {
        Write-Host "Progress: Error count DECREASED by $([Math]::Abs($delta))" -ForegroundColor Green
    }

    # Alert on unexpected error types
    if ($cs0535 -gt 0 -or $cs0738 -gt 0) {
        Write-Host "Note: CS0535 ($cs0535) and CS0738 ($cs0738) should decrease in Stage 4" -ForegroundColor Yellow
    }
}

# Main monitoring loop
Write-Host "Starting Stages 3-4 validation monitoring..."
Write-Host "Checkpoint interval: $checkpointInterval seconds"
Write-Host "Log file: $logFile"

$iteration = 0
$previousMetrics = Get-BuildMetrics

Write-Host "`nBaseline: $($previousMetrics.Total) errors" -ForegroundColor Cyan
Write-Checkpoint -Metrics $previousMetrics -PreviousTotal $previousMetrics.Total -Stage "Baseline"

try {
    while ($true) {
        Start-Sleep -Seconds $checkpointInterval

        $iteration++
        Write-Host "`n[Iteration $iteration] Running checkpoint at $(Get-Date -Format 'HH:mm:ss')..."

        $currentMetrics = Get-BuildMetrics
        Write-Checkpoint -Metrics $currentMetrics -PreviousTotal $previousMetrics.Total -Stage "Auto-checkpoint"

        # Store coordination memory
        $memoryKey = "swarm/stages34/checkpoint-$iteration"
        npx claude-flow@alpha hooks post-edit --file "validation-checkpoint" --memory-key $memoryKey 2>&1 | Out-Null

        $previousMetrics = $currentMetrics

        # Success condition
        if ($currentMetrics.Total -le 194 -and
            -not $currentMetrics.Types.ContainsKey("CS0535") -and
            -not $currentMetrics.Types.ContainsKey("CS0738")) {
            Write-Host "`nSUCCESS: Target reached! ~194 errors, CS0535/CS0738 = 0" -ForegroundColor Green
            break
        }
    }
}
catch {
    Write-Host "`nMonitoring stopped: $_" -ForegroundColor Red
}
finally {
    # Final summary
    @"

## Final Summary
**Completed:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Final Error Count:** $($currentMetrics.Total)
**Total Reduction:** $($previousMetrics.Total - $currentMetrics.Total) errors

### Error Type Distribution (Final)
"@ | Add-Content -Path $logFile

    foreach ($errorType in $currentMetrics.Types.Keys | Sort-Object) {
        "- **$errorType**: $($currentMetrics.Types[$errorType])" | Add-Content -Path $logFile
    }

    Write-Host "`nFinal log saved to: $logFile"
}
