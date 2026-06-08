<#
.SYNOPSIS
    Pre-push enforcement of CLAUDE.md §13 testing discipline.

.DESCRIPTION
    Blocks `git push` when any commit being pushed touches `src/` or `tests/`
    but lacks `T-triggers:` and `S-class:` lines in its message body. Also
    enforces the test-debt budget per §D.1 (max 2 untested commits in any
    rolling 24h window, counted from the install baseline forward).

    Exempt commits (no annotations required):
      - Subject starts with: docs:  chore:  test:  revert:  [hotfix]
      - Subject starts with: Merge  Revert
      - Files touched are entirely outside src/ and tests/

    Bypass (intentional, logged):
      git push --no-verify
    Bypasses append a timestamped entry to docs/audit/test-debt-overrides.log
    on next push (the bypass itself can't be intercepted, but the next
    successful push notices the gap and logs it).

.NOTES
    Installed via `git config core.hooksPath scripts/hooks` - see install.ps1.
    Built as part of the forcing function step after Gap G0 (2026-06-08).
#>

$ErrorActionPreference = 'Stop'

function Test-CommitExempt {
    param([string]$Subject)
    return ($Subject -match '^(docs|chore|test|revert):\s' -or
            $Subject -match '^(Merge|Revert)\s' -or
            $Subject -match '^\[hotfix\]')
}

function Test-CommitTouchesCode {
    param([string]$Sha)
    $files = & git show --pretty="" --name-only $Sha 2>$null
    foreach ($f in $files) {
        if ($f -like 'src/*' -or $f -like 'tests/*') {
            return $true
        }
    }
    return $false
}

function Test-CommitAnnotated {
    param([string]$Body)
    $hasT = $Body -match '(?m)^T-triggers:'
    $hasS = $Body -match '(?m)^S-class:'
    return @{
        HasT       = $hasT
        HasS       = $hasS
        Annotated  = $hasT -and $hasS
    }
}

# Load the install baseline (commits older than this are grandfathered into
# the test-debt budget so the discipline can be activated mid-session without
# nuking the recent un-annotated commits).
$baselineFile = Join-Path $PSScriptRoot '.test-debt-baseline'
$baselineUtc = $null
if (Test-Path $baselineFile) {
    $baselineRaw = (Get-Content $baselineFile -ErrorAction SilentlyContinue | Where-Object { $_ -and -not $_.StartsWith('#') } | Select-Object -First 1)
    if ($baselineRaw) {
        try { $baselineUtc = [datetime]::Parse($baselineRaw).ToUniversalTime() } catch { $baselineUtc = $null }
    }
}

# Determine which commits are being pushed (ahead of upstream).
$upstream = & git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null
$upstreamCommits = if ($upstream) {
    & git log --format='%H' "${upstream}..HEAD" 2>$null
} else {
    Write-Host 'pre-push: no upstream configured for current branch; checking last 5 commits.' -ForegroundColor Yellow
    & git log -5 --format='%H' 2>$null
}

if (-not $upstreamCommits) {
    Write-Host 'pre-push: nothing to push.' -ForegroundColor Cyan
    exit 0
}

# Hard check: every commit being pushed that touches src/ or tests/ must
# carry both annotations unless exempt.
$failed = @()
foreach ($sha in $upstreamCommits) {
    $subject = & git log -1 --format='%s' $sha
    if (Test-CommitExempt -Subject $subject) { continue }
    if (-not (Test-CommitTouchesCode -Sha $sha)) { continue }
    $body = & git log -1 --format='%B' $sha
    $ann = Test-CommitAnnotated -Body $body
    if (-not $ann.Annotated) {
        $short = $sha.Substring(0, 8)
        $miss = @()
        if (-not $ann.HasT) { $miss += 'T-triggers:' }
        if (-not $ann.HasS) { $miss += 'S-class:' }
        $failed += "  $short '$subject' - missing $($miss -join ' + ')"
    }
}

if ($failed.Count -gt 0) {
    Write-Host ''
    Write-Host 'PRE-PUSH BLOCKED (CLAUDE.md §13.5)' -ForegroundColor Red
    Write-Host '---------------------------------'
    Write-Host "$($failed.Count) commit(s) being pushed touch src/ or tests/ but lack required annotations:"
    Write-Host ''
    $failed | ForEach-Object { Write-Host $_ }
    Write-Host ''
    Write-Host 'Each commit message must include both lines (no leading spaces):' -ForegroundColor Yellow
    Write-Host '  T-triggers: <list of T-triggers fired + matching test paths>'
    Write-Host '  S-class:    <smoke class S1-S6 + which scripts/smoke/*.ps1 will run>'
    Write-Host ''
    Write-Host 'Exempt subject prefixes: docs: chore: test: revert: [hotfix] Merge Revert' -ForegroundColor DarkGray
    Write-Host ''
    Write-Host 'Emergency bypass (logged to docs/audit/test-debt-overrides.log):' -ForegroundColor DarkGray
    Write-Host '  git push --no-verify'
    Write-Host ''
    exit 1
}

# Soft check: test-debt budget. Count untested non-exempt code-touching commits
# in the last 24h (only those AFTER the install baseline, if set). Block if > 2.
$since = (Get-Date).AddHours(-24)
$recentShas = & git log --since="$($since.ToString('yyyy-MM-ddTHH:mm:ss'))" --format='%H'
$untestedRecent = 0
$recentDetail = @()
foreach ($sha in $recentShas) {
    # Skip commits older than the baseline (grandfathered)
    if ($baselineUtc) {
        $commitTs = [datetime]::Parse((& git log -1 --format='%cI' $sha)).ToUniversalTime()
        if ($commitTs -lt $baselineUtc) { continue }
    }
    $subject = & git log -1 --format='%s' $sha
    if (Test-CommitExempt -Subject $subject) { continue }
    if (-not (Test-CommitTouchesCode -Sha $sha)) { continue }
    $body = & git log -1 --format='%B' $sha
    $ann = Test-CommitAnnotated -Body $body
    if (-not $ann.Annotated) {
        $untestedRecent++
        $recentDetail += "  $($sha.Substring(0,8)) '$subject'"
    }
}

if ($untestedRecent -gt 2) {
    Write-Host ''
    Write-Host 'PRE-PUSH BLOCKED - test-debt budget exceeded (§D.1)' -ForegroundColor Red
    Write-Host '--------------------------------------------------'
    Write-Host "$untestedRecent untested commits in last 24h (max 2 allowed). Recent offenders:"
    Write-Host ''
    $recentDetail | ForEach-Object { Write-Host $_ }
    Write-Host ''
    Write-Host 'Pay down test-debt before pushing more. Either:' -ForegroundColor Yellow
    Write-Host '  (a) Add unit tests / smoke evidence as a NEW commit and re-push, OR'
    Write-Host '  (b) Bypass via:  git push --no-verify  (logged to docs/audit/test-debt-overrides.log)'
    Write-Host ''
    exit 1
}

# Optional verbose summary for green pushes
Write-Host "pre-push: OK ($($upstreamCommits.Count) commits being pushed, $untestedRecent / 2 untested in last 24h)" -ForegroundColor Green
exit 0
