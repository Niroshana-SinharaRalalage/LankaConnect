<#
.SYNOPSIS
  Wave 8.5.k: Smoke-BusinessesController is a SKIP-only stub.

.DESCRIPTION
  The Businesses aggregate + controller were removed 2026-07-16 per founder direction
  ("get rid off those LankaBusiness controls, we can add them freshly later"). See
  Wave 8.5.k in docs/coordination/EXECUTION_PLAN.md + Consult #12 Option D (which
  had already retired the Business domain aggregate at Wave 6.5).

  This stub replaces the prior 14-endpoint smoke so:
    (a) Run-Wave9.ps1 keeps the manifest slot (visibility in SKIP audit),
    (b) the whole controller is SKIP'd with a single audit-friendly result instead
        of 14 forced-fail entries against the deleted endpoint,
    (c) LankaBusiness product re-add in Phase B can restore the full smoke.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

$SkipReason = 'Businesses removed 2026-07-16 per founder direction; comes back cleanly with LankaBusiness product launch in Phase B'

function Invoke-BusinessesControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    $report = New-LcReport -Name 'Wave 9.e: Smoke-BusinessesController (Wave 8.5.k SKIP stub)'
    Add-LcResult -Report $report -Status SKIP -Section 'businesses-removed' `
        -TestName 'Businesses controller removed' `
        -Endpoint '/api/Businesses (all verbs)' `
        -SkipReason $SkipReason
    Complete-LcReport -Report $report | Out-Null
    return $report
}

if ($MyInvocation.InvocationName -ne '.') {
    $report = Invoke-BusinessesControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
