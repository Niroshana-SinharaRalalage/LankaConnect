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

    # Wave 9.h.9 Batch 2: subscribe is testable (real subscriber added to staging list).
    # Founder OK with real subscribe emails. Confirm/unsubscribe with token are
    # genuinely state-dependent (need actual email token).
    Test-LcEndpoint -Report $Report -Section 'newsletter-public' -TestName 'subscribe' -Endpoint 'POST /api/Newsletter/subscribe' -Action {
        $r = Invoke-LcPost -Path '/api/Newsletter/subscribe' -Bearer $null -Body @{
            email = "smoke-newsletter-$(Get-Random -Maximum 9999)@lankaconnect.test"
            name  = 'Smoke Subscriber'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'newsletter-public' -TestName 'unsubscribe (POST by email)' -Endpoint 'POST /api/Newsletter/unsubscribe' -Action {
        $r = Invoke-LcPost -Path '/api/Newsletter/unsubscribe' -Bearer $null -Body @{
            email = 'smoke-test-unsub@lankaconnect.test'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    # Token-based GET endpoints require reading an actual email; genuinely untestable
    # without inbox access (architect Q1 ruling 2026-06-30 -- inbox-token flow is
    # the irreducible SKIP category).
    Add-LcResult -Report $Report -Status SKIP -Section 'newsletter-public' -TestName 'confirm subscription (GET token)' -Endpoint 'GET /api/Newsletter/confirm' -SkipReason 'requires email token from inbox; truly untestable without inbox polling (architect Q1 irreducible-SKIP category)'
    Add-LcResult -Report $Report -Status SKIP -Section 'newsletter-public' -TestName 'unsubscribe (GET token)' -Endpoint 'GET /api/Newsletter/unsubscribe' -SkipReason 'requires email token from inbox; truly untestable without inbox polling (architect Q1 irreducible-SKIP category)'
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
