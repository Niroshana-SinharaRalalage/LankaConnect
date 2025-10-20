# Phase 1.1: Add canonical using statement to all files with type aliases
# Expected: 67 → 40-50 errors

$ErrorActionPreference = "Stop"
$canonicalUsing = "using LankaConnect.Domain.Shared;"

Write-Host "=== Phase 1.1: Adding Canonical Using Statements ===" -ForegroundColor Cyan
Write-Host "Target: All 61 files with type aliases" -ForegroundColor White
Write-Host ""

# Find all files with type aliases
$filesWithAliases = Get-ChildItem -Path "$PSScriptRoot\..\src" -Recurse -Filter "*.cs" |
    Where-Object { (Select-String -Path $_.FullName -Pattern "^using \w+ = LankaConnect\." -Quiet) }

Write-Host "Found $($filesWithAliases.Count) files with type aliases" -ForegroundColor Yellow
Write-Host ""

$modifiedCount = 0
$skippedCount = 0

foreach ($file in $filesWithAliases) {
    $content = Get-Content -Path $file.FullName -Raw

    # Check if canonical using already exists
    if ($content -match "using LankaConnect\.Domain\.Shared;") {
        Write-Host "[SKIP] Already has canonical using: $($file.Name)" -ForegroundColor DarkGray
        $skippedCount++
        continue
    }

    # Find the position after the last regular using statement (before type aliases)
    $lines = Get-Content -Path $file.FullName
    $insertIndex = -1

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Regular using statement (not alias)
        if ($line -match "^using [A-Za-z]" -and $line -notmatch "^using \w+ =") {
            $insertIndex = $i + 1
        }

        # Stop at namespace or first type alias
        if ($line -match "^namespace " -or ($line -match "^using \w+ =" -and $insertIndex -ne -1)) {
            break
        }
    }

    if ($insertIndex -eq -1) {
        Write-Host "[WARN] Could not find insertion point: $($file.Name)" -ForegroundColor Yellow
        continue
    }

    # Insert the canonical using statement
    $newLines = @()
    $newLines += $lines[0..($insertIndex - 1)]
    $newLines += $canonicalUsing
    $newLines += $lines[$insertIndex..($lines.Count - 1)]

    # Write back to file
    Set-Content -Path $file.FullName -Value $newLines -Encoding UTF8

    Write-Host "[OK] Added canonical using: $($file.Name)" -ForegroundColor Green
    $modifiedCount++
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Modified: $modifiedCount files" -ForegroundColor Green
Write-Host "Skipped:  $skippedCount files" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Next step: Run validation build" -ForegroundColor Yellow
Write-Host "  dotnet build 2>&1 | tee docs/validation/phase1-step1-after.txt" -ForegroundColor White
