<#
.SYNOPSIS
  Wave 9 master orchestrator. Runs every per-controller smoke that has shipped + the
  Wave 9 EventsController smoke from Wave 9.a. Emits combined Markdown + HTML reports.

.DESCRIPTION
  Successor to Run-Wave9a.ps1 (which is now a backward-compat alias running only the
  Events smoke). Per architect Q4 + founder corrective ruling 2026-06-29 evening, this
  orchestrator builds up as Wave 9.b, 9.c, 9.d, 9.e, 9.f sub-slices ship.

  Currently invokes:
    Wave 9.a (Events)            -- Smoke-EventsController.ps1
    Wave 9.b (Auth + Identity)   -- Smoke-AuthController.ps1
                                    Smoke-UsersController.ps1
                                    Smoke-AdminUsersController.ps1
    (more invoked as Wave 9.c+ land)

.PARAMETER OutputDir
  Output directory for Markdown + HTML reports. Default: ./reports/wave-9-<UTC>/

.PARAMETER OnlyControllers
  Run only the listed controllers (e.g. 'Events', 'Auth'). Default: all.
#>

[CmdletBinding()]
param(
    [string]$OutputDir = '',
    [string[]]$OnlyControllers = @(),
    [switch]$IncludeDestructive,
    [switch]$IncludePaymentFlows,
    [switch]$IncludeExternalProviders,
    [switch]$IncludePhotoUpload,
    [switch]$SkipLogChecks
)

$ErrorActionPreference = 'Stop'

if (-not $OutputDir) {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDir = Join-Path $PSScriptRoot "../../reports/wave-9-$timestamp"
}
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)

Write-Host "============================================"
Write-Host "Wave 9 Master Smoke Suite Orchestrator"
Write-Host "Output: $OutputDir"
Write-Host "============================================"
Write-Host ""

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

# Controller manifest: as Wave 9.c-f sub-slices ship, append entries here.
$controllerManifest = @(
    @{ Name = 'Events';          Script = 'Smoke-EventsController.ps1';          Invoker = 'Invoke-EventsControllerSmoke' }
    @{ Name = 'Auth';            Script = 'Smoke-AuthController.ps1';            Invoker = 'Invoke-AuthControllerSmoke' }
    @{ Name = 'Users';           Script = 'Smoke-UsersController.ps1';           Invoker = 'Invoke-UsersControllerSmoke' }
    @{ Name = 'AdminUsers';      Script = 'Smoke-AdminUsersController.ps1';      Invoker = 'Invoke-AdminUsersControllerSmoke' }
    @{ Name = 'VenueLayouts';    Script = 'Smoke-VenueLayoutsController.ps1';    Invoker = 'Invoke-VenueLayoutsControllerSmoke' }
    @{ Name = 'AddOns';          Script = 'Smoke-AddOnsController.ps1';          Invoker = 'Invoke-AddOnsControllerSmoke' }
    @{ Name = 'SeatingMetrics';        Script = 'Smoke-SeatingMetricsController.ps1';        Invoker = 'Invoke-SeatingMetricsControllerSmoke' }
    @{ Name = 'Newsletters';           Script = 'Smoke-NewslettersController.ps1';           Invoker = 'Invoke-NewslettersControllerSmoke' }
    @{ Name = 'Newsletter';            Script = 'Smoke-NewsletterController.ps1';            Invoker = 'Invoke-NewsletterControllerSmoke' }
    @{ Name = 'EmailGroups';           Script = 'Smoke-EmailGroupsController.ps1';           Invoker = 'Invoke-EmailGroupsControllerSmoke' }
    @{ Name = 'EmailMetrics';          Script = 'Smoke-EmailMetricsController.ps1';          Invoker = 'Invoke-EmailMetricsControllerSmoke' }
    @{ Name = 'WhatsApp';              Script = 'Smoke-WhatsAppController.ps1';              Invoker = 'Invoke-WhatsAppControllerSmoke' }
    @{ Name = 'WhatsAppAdmin';         Script = 'Smoke-WhatsAppAdminController.ps1';         Invoker = 'Invoke-WhatsAppAdminControllerSmoke' }
    @{ Name = 'AdminEmailTemplates';     Script = 'Smoke-AdminEmailTemplatesController.ps1';     Invoker = 'Invoke-AdminEmailTemplatesControllerSmoke' }
    @{ Name = 'Sponsors';                Script = 'Smoke-SponsorsController.ps1';                Invoker = 'Invoke-SponsorsControllerSmoke' }
    @{ Name = 'SponsorshipPackages';     Script = 'Smoke-SponsorshipPackagesController.ps1';     Invoker = 'Invoke-SponsorshipPackagesControllerSmoke' }
    @{ Name = 'Donations';               Script = 'Smoke-DonationsController.ps1';               Invoker = 'Invoke-DonationsControllerSmoke' }
    @{ Name = 'Collections';             Script = 'Smoke-CollectionsController.ps1';             Invoker = 'Invoke-CollectionsControllerSmoke' }
    @{ Name = 'Businesses';              Script = 'Smoke-BusinessesController.ps1';              Invoker = 'Invoke-BusinessesControllerSmoke' }
    @{ Name = 'Payments';                Script = 'Smoke-PaymentsController.ps1';                Invoker = 'Invoke-PaymentsControllerSmoke' }
    @{ Name = 'RefundReconciliation';    Script = 'Smoke-RefundReconciliationController.ps1';    Invoker = 'Invoke-RefundReconciliationControllerSmoke' }
    @{ Name = 'Approvals';               Script = 'Smoke-ApprovalsController.ps1';               Invoker = 'Invoke-ApprovalsControllerSmoke' }
    @{ Name = 'Analytics';               Script = 'Smoke-AnalyticsController.ps1';               Invoker = 'Invoke-AnalyticsControllerSmoke' }
    @{ Name = 'LongTail';                Script = 'Smoke-LongTail.ps1';                          Invoker = 'Invoke-LongTailSmoke' }
    # Wave 9.h.10.4 gap-close (2026-07-01): 7 new controller smokes added to close
    # 144 previously-uncovered endpoints across the platform.
    @{ Name = 'AdminSupportTickets';     Script = 'Smoke-AdminSupportTicketsController.ps1';     Invoker = 'Invoke-AdminSupportTicketsControllerSmoke' }
    @{ Name = 'PhotoAlbums';             Script = 'Smoke-PhotoAlbumsController.ps1';             Invoker = 'Invoke-PhotoAlbumsControllerSmoke' }
    @{ Name = 'Content';                 Script = 'Smoke-ContentController.ps1';                 Invoker = 'Invoke-ContentControllerSmoke' }
    @{ Name = 'Notifications';           Script = 'Smoke-NotificationsController.ps1';           Invoker = 'Invoke-NotificationsControllerSmoke' }
    @{ Name = 'TestCtl';                 Script = 'Smoke-TestController.ps1';                    Invoker = 'Invoke-TestControllerSmoke' }
    @{ Name = 'AdminRecovery';           Script = 'Smoke-AdminRecoveryController.ps1';           Invoker = 'Invoke-AdminRecoveryControllerSmoke' }
    @{ Name = 'WhatsAppWebhook';         Script = 'Smoke-WhatsAppWebhookController.ps1';         Invoker = 'Invoke-WhatsAppWebhookControllerSmoke' }
)

$toRun = if ($OnlyControllers.Count -gt 0) {
    $controllerManifest | Where-Object { $OnlyControllers -contains $_.Name }
} else { $controllerManifest }

$allReports = @()
$grandTotals = @{
    Total = 0; Passed = 0; Failed = 0; Skipped = 0
}

foreach ($controller in $toRun) {
    $scriptPath = Join-Path $PSScriptRoot $controller.Script
    if (-not (Test-Path $scriptPath)) {
        Write-Host "Controller smoke script not found, skipping: $scriptPath"
        continue
    }

    Write-Host ""
    Write-Host "###############################################"
    Write-Host "# Running $($controller.Name)Controller smoke"
    Write-Host "###############################################"

    . $scriptPath

    # Each per-controller script exports its invoker function; gather params dynamically
    $invokerParams = @{ }
    if ($controller.Name -eq 'Events') {
        if ($IncludeDestructive)      { $invokerParams['IncludeDestructiveLocal'] = $true }
        if ($IncludePaymentFlows)     { $invokerParams['IncludePaymentFlowsLocal'] = $true }
        if ($SkipLogChecks)           { $invokerParams['SkipLogChecksLocal'] = $true }
    } elseif ($controller.Name -eq 'Auth' -or $controller.Name -eq 'Users' -or $controller.Name -eq 'AdminUsers') {
        if ($SkipLogChecks)           { $invokerParams['SkipLogChecksLocal'] = $true }
    }

    $report = & $controller.Invoker @invokerParams
    $allReports += @{ Controller = $controller.Name; Report = $report }

    $summary = Get-LcReportSummary -Report $report
    $grandTotals.Total += $summary.Total
    $grandTotals.Passed += $summary.Passed
    $grandTotals.Failed += $summary.Failed
    $grandTotals.Skipped += $summary.Skipped

    # Save per-controller artifacts
    $controllerDir = Join-Path $OutputDir $controller.Name
    Save-LcReportArtifacts -Report $report -OutputDir $controllerDir | Out-Null
}

# Combined run summary
Write-Host ""
Write-Host "============================================"
Write-Host "WAVE 9 FULL SUITE SUMMARY"
Write-Host "============================================"
Write-Host "Total tests:   $($grandTotals.Total)"
Write-Host "Passed:        $($grandTotals.Passed)"
Write-Host "Failed:        $($grandTotals.Failed)"
Write-Host "Skipped:       $($grandTotals.Skipped)"
if ($grandTotals.Total -gt 0) {
    $passRate = [math]::Round(($grandTotals.Passed / $grandTotals.Total) * 100, 2)
    Write-Host "Pass rate:     $passRate%"
}
Write-Host ""
Write-Host "Per-controller breakdown:"
foreach ($entry in $allReports) {
    $s = Get-LcReportSummary -Report $entry.Report
    Write-Host ("  {0,-15} PASS={1,3}  FAIL={2,3}  SKIP={3,3}  TOTAL={4,3}" -f $entry.Controller, $s.Passed, $s.Failed, $s.Skipped, $s.Total)
}
Write-Host ""
Write-Host "Reports saved to: $OutputDir"
Write-Host ""

# Aggregate index file
$indexLines = @(
    "# Wave 9 Full Smoke Suite -- $(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ') UTC"
    ""
    "## Grand Totals"
    ""
    "| Metric | Count |"
    "|---|---|"
    "| Total | $($grandTotals.Total) |"
    "| Passed | $($grandTotals.Passed) |"
    "| Failed | $($grandTotals.Failed) |"
    "| Skipped | $($grandTotals.Skipped) |"
    ""
    "## Per-Controller Results"
    ""
    "| Controller | Pass | Fail | Skip | Total |"
    "|---|---|---|---|---|"
)
foreach ($entry in $allReports) {
    $s = Get-LcReportSummary -Report $entry.Report
    $indexLines += "| $($entry.Controller) | $($s.Passed) | $($s.Failed) | $($s.Skipped) | $($s.Total) |"
}
Set-Content -Path (Join-Path $OutputDir 'INDEX.md') -Value ($indexLines -join "`n")

exit $grandTotals.Failed
