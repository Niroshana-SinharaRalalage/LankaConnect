<#
.SYNOPSIS
  Per-controller smoke for AdminRecoveryController. Wave 9.h.10.4 gap-close.

.DESCRIPTION
  1 endpoint (destructive, admin-only):
    POST /api/admin/recovery/trigger-payment-event

  Documented SKIP: this endpoint replays payment events against a real registration
  and can corrupt payment state if fired without a targeted fixture. Per architect
  guidance, executed only with -IncludeDestructive AND a caller-supplied
  RegistrationId + explicit consent. Default run flags SKIP with reason.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [string]$RecoveryRegistrationId, [switch]$SkipLogChecks)

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

function Test-AdminRecoveryFlow {
    param([Parameter(Mandatory)]$Report)
    if ($IncludeDestructive -and $RecoveryRegistrationId) {
        Test-LcEndpoint -Report $Report -Section 'admin-recovery' -TestName 'trigger payment event (destructive, caller-scoped)' -Endpoint 'POST /api/admin/recovery/trigger-payment-event' -Action {
            $r = Invoke-LcPost -Path '/api/admin/recovery/trigger-payment-event' -Body @{
                registrationId = $RecoveryRegistrationId
                eventType      = 'PaymentSucceeded'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'admin-recovery' -TestName 'trigger payment event (destructive)' -Endpoint 'POST /api/admin/recovery/trigger-payment-event' -SkipReason 'Destructive: replays payment events on real registrations. Run with -IncludeDestructive -RecoveryRegistrationId <guid>. Requires targeted fixture per architect ruling.'
    }
}

function Invoke-AdminRecoveryControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'admin-recovery'; Func = { Test-AdminRecoveryFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.h.10.4: Smoke-AdminRecoveryController'
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
    $report = Invoke-AdminRecoveryControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRatePct)% ==="
}
