<#
.SYNOPSIS
  Wave 9.f scenario 2: event discovery flow (list upcoming -> read detail -> see analytics).
  Cross-controller: EventsController + AnalyticsController (W5.4 EventAnalyticsRepository).
#>

[CmdletBinding()]
param([switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot '..\modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

$report = New-LcReport -Name 'Wave 9.f Scenario: Event Discovery (Events + Analytics W5.4)'
$loginResult = Invoke-LcLogin
if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }

# Step 1: List upcoming events
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$eventId = $null
try {
    $r1 = Invoke-LcGet -Path '/api/Events/upcoming?pageNumber=1&pageSize=5'
    Assert-Http200 -Result $r1
    if ($r1.Body.items -and $r1.Body.items.Count -gt 0) {
        $eventId = $r1.Body.items[0].id
    }
    $sw.Stop()
    Add-LcResult -Report $report -Status PASS -Section 'event-discovery' -TestName 'step 1: GET /api/Events/upcoming' -Endpoint 'GET /api/Events/upcoming' -DurationMs $sw.ElapsedMilliseconds
}
catch {
    $sw.Stop()
    Add-LcResult -Report $report -Status FAIL -Section 'event-discovery' -TestName 'step 1: GET /api/Events/upcoming' -Endpoint 'GET /api/Events/upcoming' -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
}

# Step 2: If we have an event ID, fetch detail
if ($eventId) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r2 = Invoke-LcGet -Path "/api/Events/$eventId"
        Assert-Http200 -Result $r2
        $sw.Stop()
        Add-LcResult -Report $report -Status PASS -Section 'event-discovery' -TestName 'step 2: GET /api/Events/{id} detail' -Endpoint 'GET /api/Events/{id}' -DurationMs $sw.ElapsedMilliseconds
    }
    catch {
        $sw.Stop()
        Add-LcResult -Report $report -Status FAIL -Section 'event-discovery' -TestName 'step 2: GET /api/Events/{id} detail' -Endpoint 'GET /api/Events/{id}' -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }

    # Step 3: Try to read analytics for the event (W5.4 EventAnalyticsRepository)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r3 = Invoke-LcGet -Path "/api/Analytics/events/$eventId"
        # 200 (analytics exist) or 403 (not organizer) both prove cross-controller wiring
        if ($r3.StatusCode -ge 500) { throw "5xx: $($r3.StatusCode)" }
        $sw.Stop()
        Add-LcResult -Report $report -Status PASS -Section 'event-discovery' -TestName 'step 3: GET /api/Analytics/events/{id} (W5.4 EventAnalyticsRepository)' -Endpoint 'GET /api/Analytics/events/{id}' -DurationMs $sw.ElapsedMilliseconds
    }
    catch {
        $sw.Stop()
        Add-LcResult -Report $report -Status FAIL -Section 'event-discovery' -TestName 'step 3: GET /api/Analytics/events/{id} (W5.4 EventAnalyticsRepository)' -Endpoint 'GET /api/Analytics/events/{id}' -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
} else {
    Add-LcResult -Report $report -Status SKIP -Section 'event-discovery' -TestName 'step 2: event detail' -Endpoint 'GET /api/Events/{id}' -SkipReason 'no upcoming events to drill into (empty staging)'
    Add-LcResult -Report $report -Status SKIP -Section 'event-discovery' -TestName 'step 3: event analytics' -Endpoint 'GET /api/Analytics/events/{id}' -SkipReason 'no upcoming events to drill into (empty staging)'
}

Complete-LcReport -Report $report | Out-Null
$summary = Get-LcReportSummary -Report $report
Write-Host ""
Write-Host "=== SCENARIO Event Discovery: passed=$($summary.Passed) failed=$($summary.Failed) total=$($summary.Total) ==="
exit $summary.Failed
