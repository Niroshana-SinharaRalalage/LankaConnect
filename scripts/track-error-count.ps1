# Error count tracker for refactoring progress
# Logs error count after each phase/step

param(
    [Parameter(Mandatory=$true)]
    [string]$Phase,

    [Parameter(Mandatory=$true)]
    [string]$Step
)

$ErrorActionPreference = "Continue"

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$logFile = "$PSScriptRoot\..\docs\validation\error-tracking.log"

# Ensure log directory exists
$logDir = Split-Path -Path $logFile -Parent
if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
}

Write-Host "=== Error Count Tracker ===" -ForegroundColor Cyan
Write-Host "Phase: $Phase" -ForegroundColor White
Write-Host "Step:  $Step" -ForegroundColor White
Write-Host ""

# Run build and capture output
$buildOutput = dotnet build 2>&1 | Out-String

# Count errors by type
$cs0535Count = ($buildOutput | Select-String "error CS0535").Count
$cs0246Count = ($buildOutput | Select-String "error CS0246").Count
$cs0104Count = ($buildOutput | Select-String "error CS0104").Count
$cs0738Count = ($buildOutput | Select-String "error CS0738").Count
$totalErrors = ($buildOutput | Select-String "error CS").Count

# Create log entry
$logEntry = "$timestamp | $Phase | $Step | Total: $totalErrors | CS0535: $cs0535Count | CS0246: $cs0246Count | CS0104: $cs0104Count | CS0738: $cs0738Count"

# Append to log file
Add-Content -Path $logFile -Value $logEntry

# Display results
Write-Host "Results:" -ForegroundColor Cyan
Write-Host "  Total Errors:       $totalErrors" -ForegroundColor $(if ($totalErrors -eq 0) { "Green" } elseif ($totalErrors -lt 50) { "Yellow" } else { "Red" })
Write-Host "  CS0535 (Interface): $cs0535Count" -ForegroundColor DarkGray
Write-Host "  CS0246 (Not found): $cs0246Count" -ForegroundColor DarkGray
Write-Host "  CS0104 (Ambiguous): $cs0104Count" -ForegroundColor DarkGray
Write-Host "  CS0738 (Return):    $cs0738Count" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Log entry written to: $logFile" -ForegroundColor DarkGray

# Return error count for scripting
exit $totalErrors
