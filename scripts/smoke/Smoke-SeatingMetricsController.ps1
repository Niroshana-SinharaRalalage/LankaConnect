<#
.SYNOPSIS
  Per-controller smoke for SeatingMetricsController. Wave 9.c deliverable (Venue + ticketing).

.DESCRIPTION
  3 fire-and-forget analytics endpoints. Bodies are minimal.
  Per controller signature:
    POST /api/seating-metrics/selection-completed -> { EventId, AttendeeCount, TimeToCompleteMs }
    POST /api/seating-metrics/canvas-editor-opened -> { LayoutId }
    POST /api/seating-metrics/canvas-editor-saved -> { LayoutId, ChangesCount }
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

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

function Test-SeatingMetricsFireFlow {
    param([Parameter(Mandatory)]$Report)

    $fakeEventId = [Guid]::NewGuid().ToString()
    $fakeLayoutId = [Guid]::NewGuid().ToString()

    Test-LcEndpoint -Report $Report -Section 'metrics-fire' -TestName 'record seat-picker selection completed' -Endpoint 'POST /api/seating-metrics/selection-completed' -Action {
        $r = Invoke-LcPost -Path '/api/seating-metrics/selection-completed' -Bearer $null -Body @{
            eventId = $fakeEventId; attendeeCount = 2; timeToCompleteMs = 1234
        }
        # 204 NoContent on success; 400 if EventId.Empty (we send valid GUID)
        if ($r.StatusCode -ne 204 -and $r.StatusCode -ne 200) {
            throw "Expected 204/200, got $($r.StatusCode)"
        }
    }

    Test-LcEndpoint -Report $Report -Section 'metrics-fire' -TestName 'record canvas editor opened' -Endpoint 'POST /api/seating-metrics/canvas-editor-opened' -Action {
        $r = Invoke-LcPost -Path '/api/seating-metrics/canvas-editor-opened' -Body @{ layoutId = $fakeLayoutId }
        if ($r.StatusCode -ne 204 -and $r.StatusCode -ne 200) {
            throw "Expected 204/200, got $($r.StatusCode)"
        }
    }

    Test-LcEndpoint -Report $Report -Section 'metrics-fire' -TestName 'record canvas editor saved' -Endpoint 'POST /api/seating-metrics/canvas-editor-saved' -Action {
        $r = Invoke-LcPost -Path '/api/seating-metrics/canvas-editor-saved' -Body @{
            layoutId = $fakeLayoutId; changesCount = 5
        }
        if ($r.StatusCode -ne 204 -and $r.StatusCode -ne 200) {
            throw "Expected 204/200, got $($r.StatusCode)"
        }
    }
}

function Invoke-SeatingMetricsControllerSmoke {
    [CmdletBinding()]
    param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }

    $allSections = @( @{ Name = 'metrics-fire'; Func = { Test-SeatingMetricsFireFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }

    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }

    $report = New-LcReport -Name 'Wave 9.c: Smoke-SeatingMetricsController'
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
    $report = Invoke-SeatingMetricsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
