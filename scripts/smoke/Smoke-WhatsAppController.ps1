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

    # Wave 9.h.3: WhatsApp prefs are user-scoped and REVERSIBLE.
    # Pattern: capture original -> mutate -> assert -> restore original.

    # First read current
    $orig = Invoke-LcGet -Path '/api/whatsapp/preferences'
    if (-not $orig.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'whatsapp-mutators' -TestName 'capture original state' -Endpoint 'GET /api/whatsapp/preferences' -ErrorMessage "HTTP $($orig.StatusCode)"
        return
    }

    Test-LcEndpoint -Report $Report -Section 'whatsapp-mutators' -TestName 'update preferences' -Endpoint 'PUT /api/whatsapp/preferences' -Action {
        $r = Invoke-LcPut -Path '/api/whatsapp/preferences' -Body @{
            receiveEventNotifications = $true
            receiveOrganizerMessages  = $true
            receivePromotionalMessages = $false
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'whatsapp-mutators' -TestName 'disable whatsapp' -Endpoint 'POST /api/whatsapp/disable' -Action {
        $r = Invoke-LcPost -Path '/api/whatsapp/disable' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'whatsapp-mutators' -TestName 'enable whatsapp (re-enable)' -Endpoint 'POST /api/whatsapp/enable' -Action {
        $r = Invoke-LcPost -Path '/api/whatsapp/enable' -Body @{ phoneNumber = '+15555550100' }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Restore original prefs (best-effort)
    if ($orig.Body) {
        Invoke-LcPut -Path '/api/whatsapp/preferences' -Body @{
            receiveEventNotifications = $orig.Body.receiveEventNotifications
            receiveOrganizerMessages  = $orig.Body.receiveOrganizerMessages
            receivePromotionalMessages = $orig.Body.receivePromotionalMessages
        } | Out-Null
    }

    # Verify request/confirm SKIP -- truly send real SMS via Twilio (9.h.6)
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'request verification' -Endpoint 'POST /api/whatsapp/verify/request' -SkipReason 'would send real SMS via Twilio; 9.h.6 (LC_DISABLE_WEBHOOK_SIG_VALIDATION + test-mode flag)'
    Add-LcResult -Report $Report -Status SKIP -Section 'whatsapp-mutators' -TestName 'confirm verification' -Endpoint 'POST /api/whatsapp/verify/confirm' -SkipReason 'inbox-token flow (SMS code); 9.h.6'
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
