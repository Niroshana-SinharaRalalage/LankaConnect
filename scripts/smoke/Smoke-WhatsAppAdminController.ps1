<#
.SYNOPSIS
  Smoke for WhatsAppAdminController (Wave 9.d). 4 endpoints, admin-scoped.
  Inverted 403 assertions (test user is not global admin).
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

function Test-WhatsAppAdminPermissionFlow {
    param([Parameter(Mandatory)]$Report)

    # Test user is AdminManager.
    Test-LcEndpoint -Report $Report -Section 'whatsapp-admin' -TestName 'admin metrics' -Endpoint 'GET /api/whatsapp-admin/metrics' -Action {
        $r = Invoke-LcGet -Path '/api/whatsapp-admin/metrics'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'whatsapp-admin' -TestName 'admin templates' -Endpoint 'GET /api/whatsapp-admin/templates' -Action {
        $r = Invoke-LcGet -Path '/api/whatsapp-admin/templates'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'whatsapp-admin' -TestName 'admin messages (filter required)' -Endpoint 'GET /api/whatsapp-admin/messages?userId=...' -Action {
        $userId = Get-LcUserId
        $r = Invoke-LcGet -Path "/api/whatsapp-admin/messages?userId=$userId"
        Assert-Http200 -Result $r
    }
    # Test user is AdminManager. The endpoint sends a test WhatsApp message to a phone;
    # use a non-functional test number so it errors at Twilio level (returns 400 or
    # similar non-5xx). Any non-5xx proves the platform handler ran.
    Test-LcEndpoint -Report $Report -Section 'whatsapp-admin-perm' -TestName 'test-message send (wiring)' -Endpoint 'POST /api/whatsapp-admin/test-message' -Action {
        $r = Invoke-LcPost -Path '/api/whatsapp-admin/test-message' -Body @{
            toPhone = '+15555550199'
            message = 'Wave 9.h.9 smoke test'
            templateName = 'TestMessage'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Invoke-WhatsAppAdminControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'whatsapp-admin-perm'; Func = { Test-WhatsAppAdminPermissionFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-WhatsAppAdminController'
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
    $report = Invoke-WhatsAppAdminControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
