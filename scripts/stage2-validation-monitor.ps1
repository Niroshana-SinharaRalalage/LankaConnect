# Stage 2 TDD Validation Monitor
# Watches for file changes and validates build state

param(
    [int]$IntervalSeconds = 30,
    [string]$LogFile = "C:\Work\LankaConnect\docs\stage2-validation-checkpoints.md"
)

Write-Host "=== Stage 2 TDD Validation Monitor ===" -ForegroundColor Cyan
Write-Host "Monitoring files for Stage 2 duplicate member fixes" -ForegroundColor Yellow
Write-Host "Baseline: 710 errors, 28 duplicate members" -ForegroundColor White
Write-Host ""

# Target files
$targetFiles = @(
    "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Database\LoadBalancing\CulturalConflictResolutionEngine.cs",
    "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Database\LoadBalancing\DatabasePerformanceMonitoringEngineExtensions.cs"
)

# Track last modified times
$lastModified = @{}
foreach ($file in $targetFiles) {
    if (Test-Path $file) {
        $lastModified[$file] = (Get-Item $file).LastWriteTime
    }
}

function Run-ValidationBuild {
    param([string]$TriggerFile)

    Write-Host "`n[$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')] Running validation build..." -ForegroundColor Green
    Write-Host "Trigger: $TriggerFile" -ForegroundColor Gray

    # Run build and capture output
    $buildOutput = dotnet build 2>&1 | Out-String

    # Count total errors
    $totalErrors = ($buildOutput | Select-String "error CS" | Measure-Object).Count

    # Count duplicate member errors
    $duplicateErrors = ($buildOutput | Select-String "already defines a member called" | Measure-Object).Count

    # Determine checkpoint
    $checkpoint = ""
    $expected = 0
    $fileShortName = Split-Path $TriggerFile -Leaf

    if ($fileShortName -eq "CulturalConflictResolutionEngine.cs") {
        $checkpoint = "Checkpoint 1"
        $expected = 702
    } elseif ($fileShortName -eq "DatabasePerformanceMonitoringEngineExtensions.cs") {
        $checkpoint = "Checkpoint 2"
        $expected = 696
    }

    # Display results
    Write-Host "`n--- Build Validation Results ---" -ForegroundColor Cyan
    Write-Host "Checkpoint: $checkpoint" -ForegroundColor White
    Write-Host "Total Errors: $totalErrors (Expected: ~$expected)" -ForegroundColor $(if ($totalErrors -le $expected) { "Green" } else { "Red" })
    Write-Host "Duplicate Member Errors: $duplicateErrors" -ForegroundColor $(if ($duplicateErrors -lt 28) { "Green" } else { "Red" })

    # Alert if deviation
    $deviation = [Math]::Abs($totalErrors - $expected)
    if ($deviation -gt 10) {
        Write-Host "`n🚨 ALERT: Error count deviation > 10 from expected!" -ForegroundColor Red
        Write-Host "   Expected: ~$expected, Actual: $totalErrors, Deviation: $deviation" -ForegroundColor Red
    }

    # Update log
    Update-CheckpointLog -Checkpoint $checkpoint -TotalErrors $totalErrors -DuplicateErrors $duplicateErrors -Expected $expected

    # Store in memory
    & npx claude-flow@alpha hooks post-edit --file "$fileShortName" --memory-key "swarm/stage2/$checkpoint"

    return @{
        TotalErrors = $totalErrors
        DuplicateErrors = $duplicateErrors
        Checkpoint = $checkpoint
        Expected = $expected
        Deviation = $deviation
    }
}

function Update-CheckpointLog {
    param(
        [string]$Checkpoint,
        [int]$TotalErrors,
        [int]$DuplicateErrors,
        [int]$Expected
    )

    $timestamp = Get-Date -Format "yyyy-MM-ddTHH:mm:ssZ"
    $deviation = $TotalErrors - $Expected
    $status = if ([Math]::Abs($deviation) -le 10) { "✅ PASS" } else { "🚨 ALERT" }

    $logEntry = @"

### [$status] $Checkpoint Results
- Timestamp: $timestamp
- Total errors: $TotalErrors (Expected: ~$Expected)
- Duplicate member errors: $DuplicateErrors
- Deviation from expected: $deviation
- Status: $(if ([Math]::Abs($deviation) -le 10) { "Within tolerance" } else { "DEVIATION DETECTED" })

"@

    # Append to log file
    Add-Content -Path $LogFile -Value $logEntry
}

# Main monitoring loop
Write-Host "Starting file watch loop (Ctrl+C to stop)..." -ForegroundColor Yellow
Write-Host ""

$iteration = 0
while ($true) {
    $iteration++
    Write-Host "[Iteration $iteration] Checking for file modifications..." -ForegroundColor Gray

    foreach ($file in $targetFiles) {
        if (Test-Path $file) {
            $currentModified = (Get-Item $file).LastWriteTime

            if ($lastModified[$file] -ne $currentModified) {
                Write-Host "`n🔔 File modified: $(Split-Path $file -Leaf)" -ForegroundColor Yellow
                Write-Host "   Previous: $($lastModified[$file])" -ForegroundColor Gray
                Write-Host "   Current:  $currentModified" -ForegroundColor Gray

                # Update tracking
                $lastModified[$file] = $currentModified

                # Run validation build
                $results = Run-ValidationBuild -TriggerFile $file

                # Notify swarm
                $message = "Stage 2 Validation: $($results.Checkpoint) - $($results.TotalErrors) errors ($($results.DuplicateErrors) duplicates)"
                & npx claude-flow@alpha hooks notify --message $message
            }
        }
    }

    Start-Sleep -Seconds $IntervalSeconds
}
