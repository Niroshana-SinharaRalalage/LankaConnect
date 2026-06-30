<#
.SYNOPSIS
  Smoke for NewsletterController (singular - public subscribe surface). Wave 9.d.
  4 endpoints: subscribe, confirm, unsubscribe (GET+POST).
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

function Test-NewsletterPublicFlow {
    param([Parameter(Mandatory)]$Report)

    # All these are state-changing (subscribe / unsubscribe lists). SKIP by default.
    Add-LcResult -Report $Report -Status SKIP -Section 'newsletter-public' -TestName 'subscribe' -Endpoint 'POST /api/Newsletter/subscribe' -SkipReason 'destructive (would add subscriber to list); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'newsletter-public' -TestName 'confirm subscription' -Endpoint 'GET /api/Newsletter/confirm' -SkipReason 'state-dependent (requires valid confirmation token); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'newsletter-public' -TestName 'unsubscribe (GET token)' -Endpoint 'GET /api/Newsletter/unsubscribe' -SkipReason 'state-dependent (requires valid unsubscribe token); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'newsletter-public' -TestName 'unsubscribe (POST)' -Endpoint 'POST /api/Newsletter/unsubscribe' -SkipReason 'destructive (would unsubscribe an email); -IncludeDestructive'
}

function Invoke-NewsletterControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'newsletter-public'; Func = { Test-NewsletterPublicFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-NewsletterController'
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
    $report = Invoke-NewsletterControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
