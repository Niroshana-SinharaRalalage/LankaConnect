<#
.SYNOPSIS
  Wave 9.a orchestrator. Runs Smoke-EventsController.ps1 against current staging
  and emits the Markdown + HTML reports.

.DESCRIPTION
  Single entry-point for the W5.3-STAGING-VERIFIED gate. Used as:

    pwsh ./scripts/smoke/Run-Wave9a.ps1
    pwsh ./scripts/smoke/Run-Wave9a.ps1 -OutputDir ./reports/wave-9a-$(Get-Date -Format yyyyMMdd-HHmmss)
    pwsh ./scripts/smoke/Run-Wave9a.ps1 -Sections 'crud-read','rsvp'

  After Wave 9.b through 9.f land, this orchestrator extends to invoke their
  per-controller smokes too. For Wave 9.a it just runs Smoke-EventsController.
#>

[CmdletBinding()]
param(
    [string]$OutputDir = '',
    [string[]]$Sections = @(),
    [switch]$IncludeDestructive,
    [switch]$IncludePaymentFlows,
    [switch]$SkipLogChecks
)

$ErrorActionPreference = 'Stop'

# Resolve output dir
if (-not $OutputDir) {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDir = Join-Path $PSScriptRoot "../../reports/wave-9a-$timestamp"
}
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

Write-Host "============================================"
Write-Host "Wave 9.a Smoke Suite Orchestrator"
Write-Host "Output: $OutputDir"
Write-Host "============================================"
Write-Host ""

# Load + run EventsController smoke (the W5.3 gate)
$eventsScript = Join-Path $PSScriptRoot 'Smoke-EventsController.ps1'
. $eventsScript

$report = Invoke-EventsControllerSmoke `
    -Only $Sections `
    -IncludeDestructiveLocal:$IncludeDestructive `
    -IncludePaymentFlowsLocal:$IncludePaymentFlows `
    -SkipLogChecksLocal:$SkipLogChecks

$summary = Get-LcReportSummary -Report $report

# Save artifacts
$paths = Save-LcReportArtifacts -Report $report -OutputDir $OutputDir
Write-Host ""
Write-Host "Markdown report: $($paths.Markdown)"
Write-Host "HTML report:     $($paths.Html)"
Write-Host ""

# Print Markdown for inline review
$md = ConvertTo-LcMarkdown -Report $report
Write-Host $md

Write-Host ""
Write-Host "============================================"
Write-Host "Wave 9.a Final: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total)"
Write-Host "============================================"

# Exit non-zero on any failure (gates CI integration in Wave 9.f)
exit $summary.Failed
