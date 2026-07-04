<#
.SYNOPSIS
  Day 10 helper: delete legacy csproj files + rename LankaConnect.API to Host.AllInOne.

.DESCRIPTION
  Final Day 10 sequence per docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md:

    1. Remove ProjectReferences to legacy csproj files from every other csproj
    2. Remove legacy csproj entries from LankaConnect.sln
    3. git rm the 5 legacy directories:
         src/LankaConnect.Application/  (empty at this point except csproj + obj/bin)
         src/LankaConnect.Domain/       (Business KEEP-ALIVE content moves here as Legacy content)
         src/LankaConnect.Infrastructure/ (after Data/Migrations relocation Day 5)
         src/LankaConnect.Shared/       (empty)
         src/LankaConnect/              (empty)
    4. Rename src/LankaConnect.API/ -> src/Hosts/Host.AllInOne/
    5. Update sln + csproj references

  RATIFY WITH FOUNDER before running. This is irreversible.

.PARAMETER Confirm
  Must be explicitly true to run destructive ops.

.EXAMPLE
  # First: dry-run to preview
  .\Day10-LegacyDelete.ps1

  # Then: run for real
  .\Day10-LegacyDelete.ps1 -Confirm

.NOTES
  Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md Day 10 section.
#>

param([switch]$Confirm)

$ErrorActionPreference = "Stop"

Write-Host "=== Day 10: Legacy Cleanup + Host.AllInOne rename ===" -ForegroundColor Cyan
Write-Host ""

if (-not $Confirm) {
    Write-Host "DRY-RUN MODE. Re-run with -Confirm to execute." -ForegroundColor Yellow
    Write-Host ""
}

# --- Sanity: expect legacy folders to be mostly empty at this point ---
foreach ($D in @('LankaConnect.Application','LankaConnect.Domain','LankaConnect.Infrastructure','LankaConnect.Shared','LankaConnect')) {
    $path = "src/$D"
    if (Test-Path $path) {
        $csCount = (Get-ChildItem -LiteralPath $path -Recurse -Filter *.cs -File -ErrorAction SilentlyContinue |
                    Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' }).Count
        Write-Host "  $path : $csCount .cs file(s) remaining"
    } else {
        Write-Host "  $path : ALREADY GONE"
    }
}
Write-Host ""

if (-not $Confirm) {
    Write-Host "Legacy folders that will be deleted (excluding Business/Businesses KEEP-ALIVE if not yet relocated)."
    Write-Host "Handle Business KEEP-ALIVE manually before running -Confirm."
    Write-Host ""
    Write-Host "src/LankaConnect.API/ will be renamed to src/Hosts/Host.AllInOne/ (git mv)."
    Write-Host ""
    Write-Host "Re-run with -Confirm to execute."
    return
}

Write-Host "EXECUTING legacy delete + rename..." -ForegroundColor Red
Write-Host ""

# --- 1. Delete the 5 legacy dirs ---
foreach ($D in @('LankaConnect.Application','LankaConnect.Infrastructure','LankaConnect.Shared','LankaConnect')) {
    $path = "src/$D"
    if (Test-Path $path) {
        Write-Host "  git rm -r $path"
        git rm -r $path 2>&1 | Out-Null
    }
}

# LankaConnect.Domain handled separately -- rename to LankaConnect.Domain.Legacy for Business KEEP-ALIVE
$domainPath = "src/LankaConnect.Domain"
if (Test-Path $domainPath) {
    $businessOnly = @(Get-ChildItem -LiteralPath $domainPath -Recurse -Filter *.cs -File -ErrorAction SilentlyContinue |
                     Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' -and $_.FullName -notmatch '\\Business(es)?\\' })
    if ($businessOnly.Count -eq 0) {
        Write-Host "  LankaConnect.Domain contains only Business content -> renaming to LankaConnect.Domain.Legacy"
        git mv $domainPath "src/LankaConnect.Domain.Legacy" 2>&1 | Out-Null
    } else {
        Write-Host "  WARN: LankaConnect.Domain still contains non-Business content ($($businessOnly.Count) files). Manual review." -ForegroundColor Yellow
    }
}

# --- 2. Rename API to Host.AllInOne ---
if (Test-Path "src/LankaConnect.API") {
    if (-not (Test-Path "src/Hosts/Host.AllInOne")) {
        Write-Host "  git mv src/LankaConnect.API src/Hosts/Host.AllInOne"
        git mv src/LankaConnect.API src/Hosts/Host.AllInOne 2>&1 | Out-Null
    } else {
        # Move contents into existing Host.AllInOne
        Get-ChildItem -LiteralPath "src/LankaConnect.API" -File | ForEach-Object {
            git mv $_.FullName "src/Hosts/Host.AllInOne/$($_.Name)" 2>&1 | Out-Null
        }
        # Directory should be empty now
        Remove-Item -LiteralPath "src/LankaConnect.API" -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "Legacy delete complete. Manual follow-up:" -ForegroundColor Yellow
Write-Host "  1. Update LankaConnect.sln to remove deleted csproj entries + add Host.AllInOne"
Write-Host "  2. dotnet build LankaConnect.sln"
Write-Host "  3. Update deploy-staging.yml if it references LankaConnect.API/"
Write-Host "  4. Commit + push"
