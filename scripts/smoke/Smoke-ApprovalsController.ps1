<#
.SYNOPSIS
  Smoke for ApprovalsController (Wave 9.e). 3 endpoints (admin user-approval workflow).
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

function Test-ApprovalsFlow {
    param([Parameter(Mandatory)]$Report)
    $fakeUserId = [Guid]::NewGuid().ToString()

    Test-LcEndpoint -Report $Report -Section 'approvals-flow' -TestName 'list pending approvals' -Endpoint 'GET /api/Approvals/pending' -Action {
        $r = Invoke-LcGet -Path '/api/Approvals/pending'
        # Non-admin -> 403 expected; admin -> 200 also OK
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    # Test user is AdminManager (highest role). Approvals require a user with PENDING
    # upgrade state. The fakeUserId here will return 404 (no such pending request),
    # which proves wiring without doing real state mutation.
    Test-LcEndpoint -Report $Report -Section 'approvals-flow' -TestName 'approve user upgrade (wiring)' -Endpoint 'POST /api/Approvals/{userId}/approve' -Action {
        $r = Invoke-LcPost -Path "/api/Approvals/$fakeUserId/approve" -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'approvals-flow' -TestName 'reject user upgrade (wiring)' -Endpoint 'POST /api/Approvals/{userId}/reject' -Action {
        $r = Invoke-LcPost -Path "/api/Approvals/$fakeUserId/reject" -Body @{ reason = 'Smoke test rejection' }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Invoke-ApprovalsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'approvals-flow'; Func = { Test-ApprovalsFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-ApprovalsController'
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
    $report = Invoke-ApprovalsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
