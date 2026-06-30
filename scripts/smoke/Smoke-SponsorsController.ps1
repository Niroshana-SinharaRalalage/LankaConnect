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
    # Wave 9.h.9: use off-platform sponsor (returns sponsor ID immediately, no Stripe).
    # Off-platform sponsors exist as fully-created records suitable for image/brochure/patch.
    $offPlatform = Invoke-LcPost -Path "/api/events/$eventId/sponsors/off-platform" -Body @{
        sponsorName = 'Off-platform Smoke (for image/brochure/patch tests)'
        amount = 50.00
        currency = 'USD'
        sponsorType = 'Money'
        notes = 'Wave 9.h.9 smoke fixture'
    }
    $sponsorId = if ($offPlatform.Body.id) { $offPlatform.Body.id } elseif ($offPlatform.Body -is [string]) { $offPlatform.Body.Trim('"') } else { $null }

    if ($sponsorId) {
        Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'upload sponsor image (multipart)' -Endpoint 'POST /api/events/{eventId}/sponsors/{sponsorId}/image' -Action {
            $r = Invoke-LcMultipart -Path "/api/events/$eventId/sponsors/$sponsorId/image" -FileFieldName 'image' -FileName 'sponsor.png'
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'delete sponsor image' -Endpoint 'DELETE /api/events/{eventId}/sponsors/{sponsorId}/image' -Action {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/sponsors/$sponsorId/image"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'upload sponsor brochure (multipart)' -Endpoint 'POST /api/events/{eventId}/sponsors/{sponsorId}/brochure' -Action {
            $r = Invoke-LcMultipart -Path "/api/events/$eventId/sponsors/$sponsorId/brochure" -FileFieldName 'brochure' -FileName 'brochure.pdf' -ContentType 'application/pdf'
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'delete sponsor brochure' -Endpoint 'DELETE /api/events/{eventId}/sponsors/{sponsorId}/brochure' -Action {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/sponsors/$sponsorId/brochure"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sponsors-mutators' -TestName 'patch sponsor' -Endpoint 'PATCH /api/events/{eventId}/sponsors/{sponsorId}' -Action {
            $r = Invoke-LcPatch -Path "/api/events/$eventId/sponsors/$sponsorId" -Body @{
                notes = 'Updated by Wave 9.h.9 smoke'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        foreach ($n in 'upload sponsor image','delete sponsor image','upload sponsor brochure','delete sponsor brochure','patch sponsor') {
            Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName $n -Endpoint '...' -SkipReason 'off-platform sponsor create did not yield id'
        }
    }

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
