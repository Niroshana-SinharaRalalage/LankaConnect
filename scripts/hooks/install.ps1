<#
.SYNOPSIS
    Activate the CLAUDE.md §13 pre-push enforcement on this clone.

.DESCRIPTION
    Sets git config `core.hooksPath` to `scripts/hooks` so the in-repo hook
    is the canonical source of truth (versus copying into .git/hooks/ which
    drifts). Also creates the test-debt baseline file so commits older than
    the install moment are grandfathered.

    Run once per clone. Idempotent.

.NOTES
    Per docs/architecture/TESTING_DISCIPLINE_RULING.md §A.5.
#>

$ErrorActionPreference = 'Stop'

# 1. Point git at the in-repo hooks directory
& git config core.hooksPath scripts/hooks
if ($LASTEXITCODE -ne 0) {
    Write-Error 'git config core.hooksPath failed; ensure you are inside a git repo.'
    exit 1
}
Write-Host "core.hooksPath = scripts/hooks" -ForegroundColor Green

# 2. Create the install baseline if missing (grandfather older commits into
#    the 24h budget)
$baselineFile = Join-Path $PSScriptRoot '.test-debt-baseline'
if (-not (Test-Path $baselineFile)) {
    $now = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    @"
# CLAUDE.md §13.5 test-debt baseline.
# The pre-push hook ignores commits OLDER than the timestamp below in its
# 24h test-debt budget calculation. This grandfathers pre-discipline commits
# from breaking the first push after activation. New (post-baseline) commits
# touching src/ or tests/ must still carry T-triggers: and S-class: lines.
$now
"@ | Out-File -FilePath $baselineFile -Encoding utf8
    Write-Host "Wrote baseline -> $baselineFile (UTC $now)" -ForegroundColor Green
} else {
    Write-Host "Baseline already exists at $baselineFile" -ForegroundColor DarkGray
}

# 3. Smoke-check
Write-Host ''
Write-Host 'Pre-push hook installed. Verify with:' -ForegroundColor Cyan
Write-Host '  git config --get core.hooksPath'
Write-Host '  pwsh scripts/hooks/pre-push.ps1   # dry-run against current ahead-of-upstream commits'
Write-Host ''
Write-Host 'Emergency bypass (logged):  git push --no-verify' -ForegroundColor DarkGray
