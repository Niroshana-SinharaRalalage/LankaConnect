# Phase 2: Remove all type aliases from files (batch operation)
# Expected: 40-50 → 40-50 errors (STABLE - aliases are now redundant)

param(
    [Parameter(Mandatory=$false)]
    [string]$BatchName = "all"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Phase 2: Removing Type Aliases (Batch: $BatchName) ===" -ForegroundColor Cyan
Write-Host ""

# Find all files with type aliases
$filesWithAliases = Get-ChildItem -Path "$PSScriptRoot\..\src" -Recurse -Filter "*.cs" |
    Where-Object { (Select-String -Path $_.FullName -Pattern "^using \w+ = LankaConnect\." -Quiet) }

Write-Host "Found $($filesWithAliases.Count) files with type aliases" -ForegroundColor Yellow
Write-Host ""

$modifiedCount = 0

foreach ($file in $filesWithAliases) {
    $lines = Get-Content -Path $file.FullName
    $newLines = @()
    $removedCount = 0

    foreach ($line in $lines) {
        # Skip type alias lines
        if ($line -match "^using \w+ = LankaConnect\.") {
            Write-Host "[REMOVE] $($file.Name): $line" -ForegroundColor DarkYellow
            $removedCount++
            continue
        }

        $newLines += $line
    }

    if ($removedCount -gt 0) {
        Set-Content -Path $file.FullName -Value $newLines -Encoding UTF8
        Write-Host "[OK] Removed $removedCount aliases from: $($file.Name)" -ForegroundColor Green
        $modifiedCount++
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Modified: $modifiedCount files" -ForegroundColor Green
Write-Host ""
Write-Host "Next step: Run validation build" -ForegroundColor Yellow
Write-Host "  dotnet build 2>&1 | tee docs/validation/phase2-$BatchName-after.txt" -ForegroundColor White
