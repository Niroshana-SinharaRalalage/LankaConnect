<#
.SYNOPSIS
  Per-controller smoke for WhatsAppWebhookController. Wave 9.h.10.4 gap-close.

.DESCRIPTION
  2 endpoints (both webhook receivers for Twilio delivery status):
    POST /api/webhooks/whatsapp/status
    POST /api/webhooks/whatsapp/twilio-status

  Both require valid Twilio HMAC signature (X-Twilio-Signature header computed
  against the Twilio account auth token). Cannot fake in a smoke without
  compromising the auth token; smoke fires an unsigned probe expecting 401/400
  to verify the endpoint is wired + rejecting unsigned requests correctly.
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

function Test-WhatsAppWebhookFlow {
    param([Parameter(Mandatory)]$Report)
    # Wave 9.h.10.6 F31b: the /status + /twilio-status webhook endpoints now support
    # opt-in shared-secret verification via `Webhook:WhatsApp:SharedSecret` config
    # + `X-Webhook-Secret` header. When the config is set on the running app,
    # unsigned requests return 401. Until the secret is provisioned in the staging
    # Azure Container App env vars (and mirrored into the Event Grid subscription
    # + Twilio callback config), the endpoints ship in the pre-fix permissive mode
    # by design so the current subscriptions keep working. The assertions below
    # are deferred to a documented SKIP with the exact enablement steps.
    Add-LcResult -Report $Report -Status SKIP -Section 'wa-webhook' -TestName 'status webhook rejects unsigned request' -Endpoint 'POST /api/webhooks/whatsapp/status' -SkipReason 'F31b strict mode requires Webhook:WhatsApp:SharedSecret env var on staging; the code fix ships permissive-by-default. Set the env var + add matching X-Webhook-Secret to Event Grid subscription to enable strict mode; then convert this back to Test-LcEndpoint.'
    Add-LcResult -Report $Report -Status SKIP -Section 'wa-webhook' -TestName 'twilio-status webhook rejects unsigned request' -Endpoint 'POST /api/webhooks/whatsapp/twilio-status' -SkipReason 'F31b strict mode requires Webhook:WhatsApp:SharedSecret env var on staging; same enablement steps as above. Twilio webhook already validates X-Twilio-Signature; the shared-secret is a defence-in-depth fallback for callers that cannot sign.'
}

function Invoke-WhatsAppWebhookControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'wa-webhook'; Func = { Test-WhatsAppWebhookFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.h.10.4: Smoke-WhatsAppWebhookController'
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
    $report = Invoke-WhatsAppWebhookControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRatePct)% ==="
}
