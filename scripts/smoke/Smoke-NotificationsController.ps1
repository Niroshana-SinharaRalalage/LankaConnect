<#
.SYNOPSIS
  Per-controller smoke for NotificationsController (Modules/Notifications). Wave 9.h.10.4.

.DESCRIPTION
  3 endpoints:
    GET  /api/Notifications/unread
    POST /api/Notifications/{notificationId}/read
    POST /api/Notifications/read-all
  No email dispatch on these paths. Wiring-mode.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

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

function Test-NotificationsFlow {
    param([Parameter(Mandatory)]$Report)
    Test-LcEndpoint -Report $Report -Section 'notifications' -TestName 'list unread notifications' -Endpoint 'GET /api/Notifications/unread' -Action {
        $r = Invoke-LcGet -Path '/api/Notifications/unread'
        Assert-Http200 -Result $r
    }
    $fakeId = [Guid]::NewGuid().ToString()
    Test-LcEndpoint -Report $Report -Section 'notifications' -TestName 'mark notification read (404 wiring)' -Endpoint 'POST /api/Notifications/{notificationId}/read' -Action {
        $r = Invoke-LcPost -Path "/api/Notifications/$fakeId/read" -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'notifications' -TestName 'mark all read' -Endpoint 'POST /api/Notifications/read-all' -Action {
        $r = Invoke-LcPost -Path '/api/Notifications/read-all' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Invoke-NotificationsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'notifications'; Func = { Test-NotificationsFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.h.10.4: Smoke-NotificationsController'
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
    $report = Invoke-NotificationsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRatePct)% ==="
}
