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
    $mutators = @(
        @{ Method='POST';   Path='/api/events/{eventId}/sponsors/money';                       Name='create money sponsor (W5.3 SponsorRepository write)' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsors/item';                        Name='create item sponsor (W5.3 SponsorRepository write)' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsors/{sponsorId}/image';           Name='upload sponsor image' }
        @{ Method='DELETE'; Path='/api/events/{eventId}/sponsors/{sponsorId}/image';           Name='delete sponsor image' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsors/{sponsorId}/brochure';        Name='upload sponsor brochure' }
        @{ Method='DELETE'; Path='/api/events/{eventId}/sponsors/{sponsorId}/brochure';        Name='delete sponsor brochure' }
        @{ Method='PATCH';  Path='/api/events/{eventId}/sponsors/{sponsorId}';                 Name='update sponsor' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsors/staging-image';               Name='upload staging image' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsors/off-platform';                Name='record off-platform sponsor' }
    )
    foreach ($m in $mutators) {
        Add-LcResult -Report $Report -Status SKIP -Section 'sponsors-mutators' -TestName $m.Name -Endpoint "$($m.Method) $($m.Path)" -SkipReason 'destructive (creates/mutates sponsors); -IncludeDestructive'
    }
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
