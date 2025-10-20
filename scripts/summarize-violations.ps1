$json = Get-Content "C:\Work\LankaConnect\docs\violations-raw.json" -Raw | ConvertFrom-Json

Write-Host "========================================" -ForegroundColor Green
Write-Host "FILE ORGANIZATION AUDIT RESULTS" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "SUMMARY STATISTICS:" -ForegroundColor Cyan
Write-Host "  Total Files with Multiple Types: $($json.multiTypeFiles.Count)" -ForegroundColor Yellow
Write-Host "  Interface File Violations: $($json.interfaceFileViolations.Count)" -ForegroundColor Red
Write-Host "  Total Violations: $($json.totalViolations)" -ForegroundColor Red
Write-Host "  Refactoring Actions Required: $($json.refactoringPlan.Count)" -ForegroundColor Magenta
Write-Host ""
Write-Host "TOP 30 FILES WITH MULTIPLE TYPES:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green

$json.multiTypeFiles |
    Select-Object -First 30 |
    ForEach-Object {
        Write-Host "$($_.file)" -ForegroundColor White
        Write-Host "  Type Count: $($_.typeCount)" -ForegroundColor Yellow
        foreach ($type in $_.types) {
            Write-Host "    - Line $($type.line): $($type.kind) $($type.name)" -ForegroundColor Gray
        }
        Write-Host ""
    }

Write-Host ""
Write-Host "INTERFACE FILE VIOLATIONS (First 10):" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green

$json.interfaceFileViolations |
    Select-Object -First 10 |
    ForEach-Object {
        Write-Host "$($_.file)" -ForegroundColor White
        if ($_.primaryInterface) {
            Write-Host "  Primary Interface: $($_.primaryInterface)" -ForegroundColor Cyan
        }
        Write-Host "  Embedded Types:" -ForegroundColor Red
        foreach ($type in $_.embeddedTypes) {
            Write-Host "    - Line $($type.line): $($type.kind) $($type.name)" -ForegroundColor Yellow
        }
        Write-Host ""
    }
