# Continuous build monitor for refactoring progress
# Shows real-time error count and build status

param(
    [Parameter(Mandatory=$false)]
    [int]$IntervalSeconds = 10
)

$ErrorActionPreference = "Continue"

Write-Host "=== Refactoring Progress Monitor ===" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor DarkGray
Write-Host ""

$iteration = 1

while ($true) {
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] Build #$iteration..." -ForegroundColor White

    # Run build and capture output
    $buildOutput = dotnet build 2>&1 | Out-String

    # Count errors by type
    $cs0535Count = ($buildOutput | Select-String "error CS0535").Count
    $cs0246Count = ($buildOutput | Select-String "error CS0246").Count
    $cs0104Count = ($buildOutput | Select-String "error CS0104").Count
    $cs0738Count = ($buildOutput | Select-String "error CS0738").Count
    $totalErrors = ($buildOutput | Select-String "error CS").Count

    # Display results
    if ($buildOutput -match "Build succeeded") {
        Write-Host "✅ BUILD SUCCESSFUL - ZERO ERRORS!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Refactoring complete! All type aliases removed." -ForegroundColor Cyan
        break
    } else {
        $color = if ($totalErrors -gt 50) { "Red" } elseif ($totalErrors -gt 20) { "Yellow" } else { "Cyan" }
        Write-Host "  Total Errors: $totalErrors" -ForegroundColor $color
        Write-Host "    CS0535 (Interface): $cs0535Count" -ForegroundColor DarkGray
        Write-Host "    CS0246 (Not found): $cs0246Count" -ForegroundColor DarkGray
        Write-Host "    CS0104 (Ambiguous): $cs0104Count" -ForegroundColor DarkGray
        Write-Host "    CS0738 (Return type): $cs0738Count" -ForegroundColor DarkGray
        Write-Host ""
    }

    # Wait before next check
    Start-Sleep -Seconds $IntervalSeconds
    $iteration++
}

Write-Host ""
Write-Host "Monitor stopped." -ForegroundColor DarkGray
