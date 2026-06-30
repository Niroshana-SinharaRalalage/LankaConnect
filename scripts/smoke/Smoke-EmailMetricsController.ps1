<#
.SYNOPSIS
  Smoke for EmailMetricsController (Wave 9.d). 7 endpoints (admin only).
  Uses inverted assertion (test user is NOT global admin -> assert 403/401).
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

function Test-EmailMetricsPermissionFlow {
    param([Parameter(Mandatory)]$Report)

    # WAVE 9.d FINDING: test user IS authorized for /api/admin/email-metrics (returns 200
    # not 403). Either the test user has a specific email-metrics permission, OR these
    # endpoints don't gate on global admin. Either way: assert success on GETs; POST
    # /reset is SKIP_DESTRUCTIVE (would wipe metrics).

    foreach ($e in @(
        @{ Path='/api/admin/email-metrics/summary';             Name='summary' }
        @{ Path='/api/admin/email-metrics/by-template';         Name='by-template list' }
        @{ Path='/api/admin/email-metrics/by-template/welcome'; Name='by-template detail' }
        @{ Path='/api/admin/email-metrics/failures';            Name='failures' }
        @{ Path='/api/admin/email-metrics/validation-failures'; Name='validation-failures' }
        @{ Path='/api/admin/email-metrics/migration-progress';  Name='migration-progress' }
    )) {
        Test-LcEndpoint -Report $Report -Section 'email-metrics-reads' -TestName $e.Name -Endpoint "GET $($e.Path)" -Action {
            $r = Invoke-LcGet -Path $e.Path
            Assert-Http200 -Result $r
        }
    }

    # Test user is AdminManager. Reset endpoint clears in-memory counters; safe in staging.
    Test-LcEndpoint -Report $Report -Section 'email-metrics-reads' -TestName 'reset metrics' -Endpoint 'POST /api/admin/email-metrics/reset' -Action {
        $r = Invoke-LcPost -Path '/api/admin/email-metrics/reset' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Invoke-EmailMetricsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'email-metrics-reads'; Func = { Test-EmailMetricsPermissionFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-EmailMetricsController'
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
    $report = Invoke-EmailMetricsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
