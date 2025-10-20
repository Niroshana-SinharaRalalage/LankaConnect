# Continuous TDD Validation Monitor for Stages 4-5
# Monitors every 10 minutes until 0 errors achieved

param(
    [int]$IntervalMinutes = 10,
    [int]$MaxDuration = 120,
    [string]$LogFile = "C:\Work\LankaConnect\docs\validation-monitoring-log.md"
)

$StartTime = Get-Date
$Baseline = 672
$CheckpointNumber = 0

# Initialize log file
@"
# Stages 4-5 Validation Monitoring Log

**Started**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Baseline**: $Baseline errors (Stage 3 completion state)
**Mission**: Zero errors through systematic agent coordination

---

"@ | Out-File $LogFile -Encoding UTF8

function Get-ErrorCounts {
    $output = "C:\Work\LankaConnect\docs\validation-checkpoint-$CheckpointNumber.txt"

    # Run build and capture
    dotnet build 2>&1 | Out-File $output -Encoding UTF8

    # Count errors
    $total = (Select-String -Path $output -Pattern "error CS" | Measure-Object).Count
    $agent4A = (Select-String -Path $output -Pattern "BackupDisasterRecoveryEngine" | Select-String -Pattern "error CS" | Measure-Object).Count
    $agent4B = (Select-String -Path $output -Pattern "DatabaseSecurityOptimizationEngine|CulturalConflictResolutionEngine" | Select-String -Pattern "error CS" | Measure-Object).Count
    $agent5 = (Select-String -Path $output -Pattern "error CS0246" | Measure-Object).Count

    return @{
        Total = $total
        Agent4A = $agent4A
        Agent4B = $agent4B
        Agent5 = $agent5
        OutputFile = $output
    }
}

function Write-Checkpoint {
    param($counts, $elapsedMinutes)

    $delta = $Baseline - $counts.Total
    $deltaSign = if ($delta -gt 0) { "-" } else { "+" }
    $deltaAbs = [Math]::Abs($delta)
    $status = if ($counts.Total -le $Baseline) { "✅ ON TRACK" } else { "🚨 REGRESSION" }

    $checkpointText = @"

## Checkpoint T+${elapsedMinutes}min

**Timestamp**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Total Errors**: $($counts.Total) ($deltaSign$deltaAbs from baseline)
**Agent 4A (BackupDisasterRecoveryEngine)**: $($counts.Agent4A) errors
**Agent 4B (Security/Conflict)**: $($counts.Agent4B) errors
**Agent 5 (Missing Types)**: $($counts.Agent5) errors
**Status**: $status

"@

    Add-Content -Path $LogFile -Value $checkpointText -Encoding UTF8

    # Console output
    Write-Host "`n=== CHECKPOINT T+${elapsedMinutes}min ===" -ForegroundColor Cyan
    Write-Host "Total: $($counts.Total) | 4A: $($counts.Agent4A) | 4B: $($counts.Agent4B) | 5: $($counts.Agent5)"
    Write-Host "Status: $status" -ForegroundColor $(if ($status -like "*TRACK*") { "Green" } else { "Red" })

    # Regression alert
    if ($counts.Total -gt $Baseline) {
        Write-Host "`n🚨 REGRESSION DETECTED!" -ForegroundColor Red
        Write-Host "Errors increased by $deltaAbs" -ForegroundColor Red

        Add-Content -Path $LogFile -Value "### ⚠️ REGRESSION ALERT: Investigate immediately`n" -Encoding UTF8
    }

    # Success celebration
    if ($counts.Total -eq 0) {
        Write-Host "`n🎉 SUCCESS! Zero errors achieved!" -ForegroundColor Green

        Add-Content -Path $LogFile -Value @"

---

## 🎉 MISSION COMPLETE

**Final Status**: 0 errors
**Total Time**: ${elapsedMinutes} minutes
**Reduction**: $Baseline errors eliminated (100%)

"@ -Encoding UTF8

        return $true
    }

    return $false
}

function Update-Dashboard {
    param($allCheckpoints)

    $dashboard = @"
# Stages 4-5 Validation Dashboard

**Last Updated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

## Progress Summary

| Checkpoint | Total Errors | 4A Errors | 4B Errors | 5 Errors | Status |
|------------|--------------|-----------|-----------|----------|--------|
"@

    foreach ($cp in $allCheckpoints) {
        $status = if ($cp.Total -le $Baseline) { "✅" } else { "🚨" }
        $dashboard += "`n| T+$($cp.Minutes)min | $($cp.Total) | $($cp.Agent4A) | $($cp.Agent4B) | $($cp.Agent5) | $status |"
    }

    $dashboard += @"


## Expected Trajectory

| Time | Target | Actual | Delta |
|------|--------|--------|-------|
| T+0min | 672 | $($allCheckpoints[0].Total) | $(672 - $allCheckpoints[0].Total) |
| T+30min | ~550 | - | - |
| T+60min | ~400 | - | - |
| T+90min | ~200 | - | - |
| T+120min | 0 | - | - |

## Real-time Status

**Current**: $($allCheckpoints[-1].Total) errors
**Baseline**: $Baseline errors
**Reduction**: $(((($Baseline - $allCheckpoints[-1].Total) / $Baseline) * 100).ToString("F1"))%

"@

    $dashboard | Out-File "C:\Work\LankaConnect\docs\validation-dashboard.md" -Encoding UTF8
}

# Main monitoring loop
Write-Host "Starting continuous validation monitoring..." -ForegroundColor Green
Write-Host "Interval: $IntervalMinutes minutes" -ForegroundColor Cyan
Write-Host "Max duration: $MaxDuration minutes" -ForegroundColor Cyan

$allCheckpoints = @()

while ($true) {
    $elapsed = ((Get-Date) - $StartTime).TotalMinutes

    if ($elapsed -ge $MaxDuration) {
        Write-Host "`nReached max duration ($MaxDuration minutes). Stopping." -ForegroundColor Yellow
        break
    }

    Write-Host "`nRunning checkpoint $CheckpointNumber..." -ForegroundColor Yellow

    $counts = Get-ErrorCounts
    $elapsedRounded = [Math]::Floor($elapsed)

    $allCheckpoints += @{
        Minutes = $elapsedRounded
        Total = $counts.Total
        Agent4A = $counts.Agent4A
        Agent4B = $counts.Agent4B
        Agent5 = $counts.Agent5
    }

    $success = Write-Checkpoint -counts $counts -elapsedMinutes $elapsedRounded
    Update-Dashboard -allCheckpoints $allCheckpoints

    # Notify via hooks
    $hookMessage = "Checkpoint T+${elapsedRounded}min: $($counts.Total) errors (4A:$($counts.Agent4A) 4B:$($counts.Agent4B) 5:$($counts.Agent5))"
    npx claude-flow@alpha hooks notify --message $hookMessage 2>&1 | Out-Null

    if ($success) {
        Write-Host "Zero errors achieved! Exiting." -ForegroundColor Green
        break
    }

    $CheckpointNumber++

    # Wait for next interval
    $waitSeconds = $IntervalMinutes * 60
    Write-Host "Waiting $IntervalMinutes minutes until next checkpoint..." -ForegroundColor Gray
    Start-Sleep -Seconds $waitSeconds
}

Write-Host "`nMonitoring complete. See logs at:" -ForegroundColor Green
Write-Host "  - $LogFile" -ForegroundColor Cyan
Write-Host "  - C:\Work\LankaConnect\docs\validation-dashboard.md" -ForegroundColor Cyan
