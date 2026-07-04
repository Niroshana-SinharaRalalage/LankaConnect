<#
.SYNOPSIS
  Day 4 helper: merge 6 bulk-move worktree branches into bulk-move/integration.

.DESCRIPTION
  Runs from main working tree (C:\Work\LankaConnect\). Assumes:
    - All 6 bulk-move/agent-{A..F} branches have Day 2 + Day 3 commits pushed
    - Local main working tree can accept branch switching
    - We start from develop (post-Day-1 hotfix merge)

  Steps:
    1. Create bulk-move/integration branch off develop
    2. Merge branches in dependency order:
       Domain (A) -> Contracts/Shared (F) -> Application (B) -> Infrastructure non-EF (C) -> Infrastructure EF (D) -> Api (E)
    3. Attempt each merge; STOP on conflict (do NOT auto-resolve)
    4. Print each merge outcome

  NO push to origin -- founder review before Day 6 push to develop.

.PARAMETER Force
  Skip clean-tree assertion.

.EXAMPLE
  .\Day4-IntegrationMerge.ps1

.NOTES
  Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md Day 4 section.
#>

param([switch]$Force)

$ErrorActionPreference = "Stop"

$mergeOrder = @('agent-A','agent-F','agent-B','agent-C','agent-D','agent-E')

# --- Preconditions ---
Write-Host "=== Day 4 Integration Merge ===" -ForegroundColor Cyan
Write-Host ""

$curBranch = (git branch --show-current).Trim()
if ($curBranch -ne 'develop') {
    Write-Host "Switching to develop..."
    git checkout develop
}

$porcelain = git status --porcelain
$tracked = $porcelain | Where-Object { $_ -notmatch '^\?\?' }
if ($tracked -and -not $Force) {
    Write-Host "FAIL - working tree has uncommitted tracked changes." -ForegroundColor Red
    throw "Commit or stash first, or re-run with -Force."
}

Write-Host "Fetching all bulk-move branches..."
git fetch origin 'refs/heads/bulk-move/*:refs/remotes/origin/bulk-move/*' 2>&1 | tail -3
Write-Host ""

# Confirm all 6 branches exist
foreach ($b in $mergeOrder) {
    $rev = git rev-parse --verify "origin/bulk-move/$b" 2>$null
    if (-not $rev) { throw "Missing branch: origin/bulk-move/$b" }
    Write-Host "  origin/bulk-move/$b @ $($rev.Substring(0,8))"
}
Write-Host ""

# --- Create integration branch ---
Write-Host "Creating bulk-move/integration off develop..."
git branch -D bulk-move/integration 2>$null
git checkout -b bulk-move/integration develop
Write-Host ""

# --- Merge each in order ---
$results = @()
foreach ($b in $mergeOrder) {
    Write-Host "Merging bulk-move/$b..." -ForegroundColor Yellow
    $mergeExit = 0
    try {
        git merge --no-ff "origin/bulk-move/$b" -m "Merge bulk-move/$b into bulk-move/integration (Sprint Day 4)" 2>&1 | Tee-Object -Variable mergeOutput | Out-Null
        $mergeExit = $LASTEXITCODE
    } catch {
        $mergeExit = 1
    }

    if ($mergeExit -eq 0) {
        Write-Host "  MERGED $b OK" -ForegroundColor Green
        $results += [pscustomobject]@{ Branch = $b; Result = 'OK' }
    } else {
        Write-Host "  CONFLICT on $b -- STOPPING" -ForegroundColor Red
        git merge --abort 2>$null
        $results += [pscustomobject]@{ Branch = $b; Result = 'CONFLICT' }
        break
    }
}

Write-Host ""
Write-Host "=== Merge summary ==="
$results | Format-Table

$conflictCount = ($results | Where-Object { $_.Result -ne 'OK' }).Count
if ($conflictCount -eq 0) {
    Write-Host "All 6 branches merged into bulk-move/integration." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next: dotnet build LankaConnect.sln (expect compile errors)"
    Write-Host "      Day 4 agents A/B/C fix errors; commit + stay on bulk-move/integration"
    Write-Host "      DO NOT push to develop -- Day 6 does that."
} else {
    Write-Host "Merge stopped due to conflicts. Founder decision required." -ForegroundColor Red
    exit 1
}
