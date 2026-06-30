<#
.SYNOPSIS
  Smoke for DonationsController (Wave 9.e). 6 endpoints, event-scoped.
  CRITICAL Wave 5 verification: exercises DonationRepository read paths.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-FinanceFixtures.psm1') -Force

function Test-LcEndpoint {
    param([Parameter(Mandatory)]$Report, [Parameter(Mandatory)][string]$Section,
          [Parameter(Mandatory)][string]$TestName, [Parameter(Mandatory)][string]$Endpoint,
          [Parameter(Mandatory)][scriptblock]$Action, [string]$SkipReason = '')
    if ($SkipReason) {
        Add-LcResult -Report $Report -Status SKIP -Section $Section -TestName $TestName -Endpoint $Endpoint -SkipReason $SkipReason
        return
    }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action | Out-Null
        $sw.Stop()
        Add-LcResult -Report $Report -Status PASS -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds
    } catch {
        $sw.Stop()
        Add-LcResult -Report $Report -Status FAIL -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
}

function Test-DonationsReadFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.2: real event fixture with donations enabled
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'donations-read' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    Test-LcEndpoint -Report $Report -Section 'donations-read' -TestName 'my donations (W5.3 DonationRepository)' -Endpoint "GET /api/events/{eventId}/donations/mine" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/donations/mine"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Wave 9.h.2: F8-F11 resolved via real event fixture (donations enabled).
    foreach ($e in @(
        @{ Path="/api/events/$eventId/donations";                Name='organizer donations list (F8 - real event)' }
        @{ Path="/api/events/$eventId/donations/summary";        Name='organizer donations summary (F9 - real event)' }
        @{ Path="/api/events/$eventId/donations/export";         Name='organizer donations export (F10 - real event)' }
        @{ Path="/api/events/$eventId/donations/public-summary"; Name='public donations summary (F11 - real event)' }
    )) {
        Test-LcEndpoint -Report $Report -Section 'donations-read' -TestName $e.Name -Endpoint "GET $($e.Path)" -Action {
            $r = Invoke-LcGet -Path $e.Path
            if ($r.StatusCode -ge 500) { throw "confirmed real bug: 5xx $($r.StatusCode)" }
        }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Test-DonationsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.3: create real event + actually record a donation
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'donations-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null

    Test-LcEndpoint -Report $Report -Section 'donations-mutators' -TestName 'record donation (real event)' -Endpoint 'POST /api/events/{eventId}/donations' -Action {
        $d = New-LcTaggedDonation -EventId $fix.EventId
        if (-not $d.Success) { throw "donation create failed: HTTP $($d.StatusCode) $($d.Error)" }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Invoke-DonationsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'donations-read';     Func = { Test-DonationsReadFlow -Report $report } }
        @{ Name = 'donations-mutators'; Func = { Test-DonationsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-DonationsController'
    foreach ($section in $sectionsToRun) {
        Write-Host ""; Write-Host "=== Running sub-section: $($section.Name) ==="
        try { & $section.Func | Out-Null } catch {
            Add-LcResult -Report $report -Status FAIL -Section $section.Name -TestName 'sub-section orchestration' -Endpoint 'N/A' -ErrorMessage $_.Exception.Message
        }
    }
    Complete-LcReport -Report $report | Out-Null
    return $report
}

if ($MyInvocation.InvocationName -ne '.') {
    $report = Invoke-DonationsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
