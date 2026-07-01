<#
.SYNOPSIS
  Per-controller smoke for TestController. Wave 9.h.10.4 gap-close.

.DESCRIPTION
  2 endpoints:
    POST /api/Test/send-test-email  -- sends inline HTML email (no template)
    GET  /api/Test/email-health     -- health probe for email service

  send-test-email fires via AzureEmailService directly (bypasses template system).
  Recipient defaults to niroshanaks@gmail.com; we override to founder inbox alias.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force

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

function Test-TestControllerFlow {
    param([Parameter(Mandatory)]$Report)
    $toEmail = Get-LcFixtureEmail -Slug 'test-controller-send-test-email'
    Test-LcEndpoint -Report $Report -Section 'test-ctl' -TestName 'send test email (bypass template)' -Endpoint 'POST /api/Test/send-test-email' -Action {
        $r = Invoke-LcPost -Path "/api/Test/send-test-email?toEmail=$([uri]::EscapeDataString($toEmail))" -Bearer $null -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'test-ctl' -TestName 'email service health' -Endpoint 'GET /api/Test/email-health' -Action {
        $r = Invoke-LcGet -Path '/api/Test/email-health' -Bearer $null
        Assert-Http200 -Result $r
    }
}

function Invoke-TestControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'test-ctl'; Func = { Test-TestControllerFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.h.10.4: Smoke-TestController'
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
    $report = Invoke-TestControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRatePct)% ==="
}
