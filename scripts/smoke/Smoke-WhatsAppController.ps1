<#
.SYNOPSIS
  Smoke for WhatsAppController (Wave 9.d). 6 endpoints (user preferences).
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

function Test-WhatsAppPreferencesFlow {
    param([Parameter(Mandatory)]$Report)

    Test-LcEndpoint -Report $Report -Section 'whatsapp-prefs' -TestName 'get preferences' -Endpoint 'GET /api/whatsapp/preferences' -Action {
        $r = Invoke-LcGet -Path '/api/whatsapp/preferences'
        Assert-Http200 -Result $r
    }
}

function Test-WhatsAppMutatorsFlow {
    param([Parameter(Mandatory)]$Report)
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'enable whatsapp' -Endpoint 'POST /api/whatsapp/enable' -SkipReason 'destructive (mutates user preferences); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'disable whatsapp' -Endpoint 'POST /api/whatsapp/disable' -SkipReason 'destructive (mutates user preferences); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'request verification' -Endpoint 'POST /api/whatsapp/verify/request' -SkipReason 'destructive (would send real SMS via Twilio); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'confirm verification' -Endpoint 'POST /api/whatsapp/verify/confirm' -SkipReason 'state-dependent (requires valid code from SMS); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'update preferences' -Endpoint 'PUT /api/whatsapp/preferences' -SkipReason 'destructive (mutates preferences); -IncludeDestructive'
}

function Invoke-WhatsAppControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'whatsapp-prefs'; Func = { Test-WhatsAppPreferencesFlow -Report $report } }
        @{ Name = 'whatsapp-mutators'; Func = { Test-WhatsAppMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-WhatsAppController'
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
    $report = Invoke-WhatsAppControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
