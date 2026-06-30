<#
.SYNOPSIS
  Smoke for CollectionsController (Wave 9.e). 6 endpoints, event-scoped.
  CRITICAL Wave 5 verification: exercises CollectionRepository read paths.
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

function Test-CollectionsReadFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.2: real event fixture with collection-config enabled
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'collections-read' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    Test-LcEndpoint -Report $Report -Section 'collections-read' -TestName 'my collections (W5.3 CollectionRepository)' -Endpoint "GET /api/events/{eventId}/collections/mine" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/collections/mine"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Wave 9.h.2: F12-F15 resolved via real event fixture
    foreach ($e in @(
        @{ Path="/api/events/$eventId/collections";                Name='organizer collections list (F12 - real event)' }
        @{ Path="/api/events/$eventId/collections/summary";        Name='organizer collections summary (F13 - real event)' }
        @{ Path="/api/events/$eventId/collections/export";         Name='organizer collections export (F14 - real event)' }
        @{ Path="/api/events/$eventId/collections/public-summary"; Name='public collections summary (F15 - real event)' }
    )) {
        Test-LcEndpoint -Report $Report -Section 'collections-read' -TestName $e.Name -Endpoint "GET $($e.Path)" -Action {
            $r = Invoke-LcGet -Path $e.Path
            if ($r.StatusCode -ge 500) { throw "confirmed real bug: 5xx $($r.StatusCode)" }
        }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Test-CollectionsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.3: create real event + record a collection
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'collections-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null

    Test-LcEndpoint -Report $Report -Section 'collections-mutators' -TestName 'record collection (real event)' -Endpoint 'POST /api/events/{eventId}/collections' -Action {
        $c = New-LcTaggedCollection -EventId $fix.EventId
        if (-not $c.Success) { throw "collection create failed: HTTP $($c.StatusCode) $($c.Error)" }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Invoke-CollectionsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'collections-read';     Func = { Test-CollectionsReadFlow -Report $report } }
        @{ Name = 'collections-mutators'; Func = { Test-CollectionsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-CollectionsController'
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
    $report = Invoke-CollectionsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
