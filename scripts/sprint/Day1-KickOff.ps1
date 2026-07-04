<#
.SYNOPSIS
  Sprint Day 1 kickoff orchestrator. Runs Monday 2026-07-06 morning.

.DESCRIPTION
  Executes the Day 1 AM sequence per docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md:

    1. Confirm working tree is on wave-6-5-f-5-hotfix branch and clean
    2. Fetch develop + verify no upstream drift
    3. Merge-tree probe (verify no conflicts)
    4. Checkout develop, merge hotfix stack with --no-ff (preserve stack topology)
    5. Push develop to origin -- auto-triggers deploy-staging.yml
    6. Print next-steps: monitor staging deploy, run Wave 9 smoke, EOD sign-off

  Stops on the first failure. Does NOT auto-recover -- founder is engaged for decisions.

.PARAMETER Force
  Skip clean-working-tree check. Only use if founder has explicitly approved.

.EXAMPLE
  .\Day1-KickOff.ps1

.NOTES
  Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md
  Fail-state:   If any step below fails, STOP and escalate to founder. Do NOT retry.
#>

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

function Assert-CleanTree {
    param([switch]$Force)
    $porcelain = git status --porcelain
    $tracked   = $porcelain | Where-Object { $_ -notmatch '^\?\?' }
    if ($tracked -and -not $Force) {
        Write-Host "FAIL - working tree has uncommitted tracked changes:" -ForegroundColor Red
        $tracked | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        throw "Refusing to proceed. Commit or stash first, or re-run with -Force."
    }
    Write-Host "  Working tree clean." -ForegroundColor Green
}

function Assert-OnBranch {
    param([string]$Expected)
    $actual = (git branch --show-current).Trim()
    if ($actual -ne $Expected) {
        throw "Expected branch $Expected, got $actual"
    }
    Write-Host "  On branch $Expected." -ForegroundColor Green
}

# --- Step 1: Preconditions ---
Write-Host ""
Write-Host "=== Day 1 Kickoff -- 2-Week Bulk-Move Sprint ===" -ForegroundColor Cyan
Write-Host "Sprint bible: docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md"
Write-Host ""

Write-Host "[1/6] Preconditions..."
Assert-OnBranch -Expected 'wave-6-5-f-5-hotfix'
Assert-CleanTree -Force:$Force
Write-Host ""

# --- Step 2: Fetch develop, verify no upstream drift ---
Write-Host "[2/6] Fetching develop..."
git fetch origin develop
$behind = (git rev-list --count develop..origin/develop).Trim()
$ahead  = (git rev-list --count origin/develop..develop).Trim()
if ($behind -ne '0') {
    Write-Host "  FAIL - local develop is $behind commit(s) behind origin/develop." -ForegroundColor Red
    throw "Sync local develop first: git checkout develop && git pull"
}
if ($ahead -ne '0') {
    Write-Host "  WARN - local develop is $ahead commit(s) ahead of origin/develop." -ForegroundColor Yellow
    Write-Host "         (Expected during develop maintenance. Proceeding.)"
}
Write-Host "  develop in sync with origin/develop." -ForegroundColor Green
Write-Host ""

# --- Step 3: Merge-tree probe ---
Write-Host "[3/6] Probing merge for conflicts..."
$mergeTree = git merge-tree develop wave-6-5-f-5-hotfix 2>&1
if ($mergeTree -match 'conflict|<<<<<<< ') {
    Write-Host "  FAIL - conflicts detected in probe:" -ForegroundColor Red
    Write-Host $mergeTree
    throw "Resolve conflicts before merge. Do NOT force."
}
Write-Host "  Probe clean." -ForegroundColor Green
Write-Host ""

# --- Step 4: Merge with --no-ff to preserve stack topology ---
Write-Host "[4/6] Merging hotfix stack to develop (--no-ff)..."
git checkout develop
git merge --no-ff wave-6-5-f-5-hotfix -m "Merge wave-6-5-f-5-hotfix: 6-commit stack (48168fbc..cd864bfa) - Wave 6.5.f.5 hotfix consolidation`n`nSprint Day 1 (Mon 2026-07-06 AM) kickoff per docs/MASTER_TODO_SPRINT_TWO_WEEK_BULK_MOVE.md.`n`nFixes:`n- hotfix (48168fbc):   un-Ignore junctions + move junction configs to Products.LankaEvents.Infrastructure`n- hotfix2 (f25003a1):  embed physical (schema, table) in Products.LankaEvents configs + extend parity tests`n- hotfix2b (b010e3a9): remove HasDefaultSchema + refined parity + LankaEvents snapshot rebaseline`n- hotfix2c (a173fe89): restore EventBadge->Badge Restrict FK + AppDbContext snapshot rebaseline`n- hotfix2d (54422e85): repair GetEventBadgesQueryHandler cross-module hydration`n- hotfix2e (cd864bfa): apply LankaEventsDbContext migrations at deploy time`n`nSprint start baseline: Wave 9 smoke = 182/0/79."
Write-Host "  Merged." -ForegroundColor Green
Write-Host ""

# --- Step 5: Push develop ---
Write-Host "[5/6] Push develop to origin (auto-triggers deploy-staging.yml)..."
git push origin develop
Write-Host "  Pushed." -ForegroundColor Green
Write-Host ""

# --- Step 6: Next steps ---
Write-Host "[6/6] Post-merge instructions:"
Write-Host ""
Write-Host "  A. Monitor staging deploy:"
Write-Host "     gh run watch (or GitHub Actions UI)"
Write-Host ""
Write-Host "  B. Once deployed, run Wave 9 smoke suite:"
Write-Host "     scripts/smoke/Run-Wave9.ps1 (or scripts/smoke/Run-Wave9a.ps1 for subset)"
Write-Host ""
Write-Host "  C. Confirm baseline 182/0/79 restored before proceeding to Day 1 PM."
Write-Host ""
Write-Host "  D. Day 1 PM tasks (docs already prepped in Day 0.5):"
Write-Host "     - docs/PHASE_A_5_PLAN.md  (already authored)"
Write-Host "     - MASTER_TODO surgery     (already applied via SPRINT NOTICE header)"
Write-Host ""
Write-Host "  E. EOD 18:00 - founder sign-off on Day 1 completion (hotfix green + manifest ratified)."
Write-Host ""
Write-Host "=== Day 1 AM kickoff complete. ===" -ForegroundColor Cyan
