<#
.SYNOPSIS
  Smoke for SponsorsController (Wave 9.e). 14 endpoints, event-scoped.
  CRITICAL Wave 5 verification: exercises SponsorRepository read paths shipped in
  Wave 5.3 (Wave 9.a smoke did NOT touch this controller).
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

function Test-SponsorsReadFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.2: real event fixture + sponsor-config enabled
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'sponsors-read' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    foreach ($e in @(
        @{ Path="/api/events/$eventId/sponsors/public"; Name='public sponsors (W5.3 SponsorRepository)' }
        @{ Path="/api/events/$eventId/sponsors/mine";   Name='my sponsorships' }
    )) {
        Test-LcEndpoint -Report $Report -Section 'sponsors-read' -TestName $e.Name -Endpoint "GET $($e.Path)" -Action {
            $r = Invoke-LcGet -Path $e.Path
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    }

    # Wave 9.h.2: F4-F6 resolved -- previously SKIPped with 500-on-fake-event finding;
    # now exercised against a real event the test user organizes. PASS = environmental
    # finding (not a real bug); FAIL with 5xx = real bug confirmed.
    Test-LcEndpoint -Report $Report -Section 'sponsors-read' -TestName 'organizer sponsors list (F4 - real event)' -Endpoint "GET /api/events/{eventId}/sponsors" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/sponsors"
        if ($r.StatusCode -ge 500) { throw "F4 confirmed real bug: 5xx $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'sponsors-read' -TestName 'organizer sponsors summary (F5 - real event)' -Endpoint "GET /api/events/{eventId}/sponsors/summary" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/sponsors/summary"
        if ($r.StatusCode -ge 500) { throw "F5 confirmed real bug: 5xx $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'sponsors-read' -TestName 'organizer sponsors CSV export (F6 - real event)' -Endpoint "GET /api/events/{eventId}/sponsors/export" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/sponsors/export"
        if ($r.StatusCode -ge 500) { throw "F6 confirmed real bug: 5xx $($r.StatusCode)" }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Test-SponsorsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.3: real fixtures + actual mutator coverage
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'sponsors-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'create money sponsor (W5.3 write)' -Endpoint 'POST /api/events/{eventId}/sponsors/money' -Action {
        $s = New-LcTaggedMoneySponsor -EventId $eventId
        if (-not $s.Success) { throw "create failed: HTTP $($s.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'create item sponsor (W5.3 write)' -Endpoint 'POST /api/events/{eventId}/sponsors/item' -Action {
        $s = New-LcTaggedItemSponsor -EventId $eventId
        if (-not $s.Success) { throw "create failed: HTTP $($s.StatusCode)" }
    }

    # Image upload / delete / brochure / patch / staging-image / off-platform
    # Each requires either a sponsor ID (from a prior create) or specific multipart shape.
    # These remain SKIPped with VALID technical reasons (not "destructive" -- specific):
    Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName 'upload sponsor image (multipart)' -Endpoint 'POST /api/events/{eventId}/sponsors/{sponsorId}/image' -SkipReason 'CompletedPayment-status sponsor required before image; sponsor create returns sessionUrl not sponsorId in money flow (Stripe-mediated); 9.h.5 territory'
    Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName 'delete sponsor image' -Endpoint 'DELETE /api/events/{eventId}/sponsors/{sponsorId}/image' -SkipReason 'requires sponsor with prior image upload; same blocker as upload'
    Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName 'upload sponsor brochure (multipart)' -Endpoint 'POST /api/events/{eventId}/sponsors/{sponsorId}/brochure' -SkipReason 'requires sponsorId from Stripe-completed flow; 9.h.5'
    Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName 'delete sponsor brochure' -Endpoint 'DELETE /api/events/{eventId}/sponsors/{sponsorId}/brochure' -SkipReason 'requires sponsor with prior brochure'
    Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName 'patch sponsor' -Endpoint 'PATCH /api/events/{eventId}/sponsors/{sponsorId}' -SkipReason 'requires sponsor from Stripe-completed money flow; 9.h.5'

    Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'upload staging image (multipart)' -Endpoint 'POST /api/events/{eventId}/sponsors/staging-image' -Action {
        $r = Invoke-LcMultipart -Path "/api/events/$eventId/sponsors/staging-image" -FileFieldName 'image' -FileName 'staging-logo.png'
        if (-not $r.Success -and $r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'record off-platform sponsor' -Endpoint 'POST /api/events/{eventId}/sponsors/off-platform' -Action {
        $r = Invoke-LcPost -Path "/api/events/$eventId/sponsors/off-platform" -Body @{
            sponsorName = "Off-platform Smoke"
            amount = 50.00
            currency = 'USD'
            sponsorType = 'Money'
            notes = 'Wave 9.h.3 smoke'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Invoke-SponsorsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'sponsors-read'; Func = { Test-SponsorsReadFlow -Report $report } }
        @{ Name = 'sponsors-mutators'; Func = { Test-SponsorsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-SponsorsController'
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
    $report = Invoke-SponsorsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
