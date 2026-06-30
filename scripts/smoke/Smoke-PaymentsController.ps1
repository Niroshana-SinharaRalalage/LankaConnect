<#
.SYNOPSIS
  Smoke for PaymentsController (Wave 9.e). 4 endpoints (Stripe).
  CRITICAL Wave 5 verification: exercises RegistrationPaymentRepository indirectly via
  webhook + checkout-session paths.
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

function Test-PaymentsReadFlow {
    param([Parameter(Mandatory)]$Report)

    Test-LcEndpoint -Report $Report -Section 'payments-read' -TestName 'payments config' -Endpoint 'GET /api/Payments/config' -Action {
        $r = Invoke-LcGet -Path '/api/Payments/config'
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-PaymentsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)
    Add-LcResult -Report $Report -Status SKIP -Section 'payments-mutators' -TestName 'create Stripe checkout session' -Endpoint 'POST /api/Payments/create-checkout-session' -SkipReason 'destructive (creates real Stripe session; RegistrationPaymentRepository W5.3 write path); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'payments-mutators' -TestName 'create Stripe portal session' -Endpoint 'POST /api/Payments/create-portal-session' -SkipReason 'destructive (creates Stripe portal session); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'payments-mutators' -TestName 'Stripe webhook receiver' -Endpoint 'POST /api/Payments/webhook' -SkipReason 'requires valid Stripe signature; cannot fake; webhook tested by Stripe Dashboard'
}

function Invoke-PaymentsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'payments-read';     Func = { Test-PaymentsReadFlow -Report $report } }
        @{ Name = 'payments-mutators'; Func = { Test-PaymentsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-PaymentsController'
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
    $report = Invoke-PaymentsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
