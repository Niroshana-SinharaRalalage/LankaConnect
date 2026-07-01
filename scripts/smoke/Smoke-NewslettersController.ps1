<#
.SYNOPSIS
  Smoke for NewslettersController (Wave 9.d). 12 endpoints.
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

function Test-NewslettersReadFlow {
    param([Parameter(Mandatory)]$Report)
    $fakeId = [Guid]::NewGuid().ToString()

    # Wave 9.h.fix: F17 + F18 FIXED (98cc4e37) -- Newsletter handlers now use
    # CLR-typed NewsletterMetroAreaLink junction (was Dictionary<string,object>).
    Test-LcEndpoint -Report $Report -Section 'newsletters-read' -TestName 'my newsletters list (F17 fixed)' -Endpoint 'GET /api/Newsletters/my-newsletters' -Action {
        $r = Invoke-LcGet -Path '/api/Newsletters/my-newsletters'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'newsletters-read' -TestName 'published newsletters (F18 fixed)' -Endpoint 'GET /api/Newsletters/published' -Action {
        $r = Invoke-LcGet -Path '/api/Newsletters/published'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'newsletters-read' -TestName 'newsletter by id (404 OK)' -Endpoint 'GET /api/Newsletters/{id}' -Action {
        $r = Invoke-LcGet -Path "/api/Newsletters/$fakeId"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'newsletters-read' -TestName 'newsletters by event' -Endpoint 'GET /api/Newsletters/event/{eventId}' -Action {
        $r = Invoke-LcGet -Path "/api/Newsletters/event/$fakeId"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'newsletters-read' -TestName 'recipient preview' -Endpoint 'GET /api/Newsletters/{id}/recipient-preview' -Action {
        $r = Invoke-LcGet -Path "/api/Newsletters/$fakeId/recipient-preview"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-NewslettersMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.9 Batch 2: real newsletter lifecycle (create -> update -> publish ->
    # unpublish -> reactivate -> send -> delete). Test user is AdminManager so all
    # mutators permitted. Founder OK with real test emails.
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'newsletters-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId
    $tag = Get-LcCurrentRunTag

    # Newsletter create requires recipient source (email group or subscriber list).
    # Create an EmailGroup first as the recipient source. Cleanup at end.
    $script:newsletterEmailGroupId = $null
    $egCreate = Invoke-LcPost -Path '/api/EmailGroups' -Body @{
        name = "$tag NewsletterRecipients"
        description = 'Wave 9.h.9 smoke fixture'
        emailAddresses = 'smoke-newsletter-recipient@lankaconnect.test'
    }
    if ($egCreate.Success) {
        $script:newsletterEmailGroupId = if ($egCreate.Body.id) { $egCreate.Body.id } elseif ($egCreate.Body -is [string]) { $egCreate.Body.Trim('"') } else { $null }
    }

    $script:newsletterId = $null
    Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'create newsletter' -Endpoint 'POST /api/Newsletters' -Action {
        $body = @{
            eventId       = $eventId
            title         = "$tag SmokeNewsletter"
            description   = 'Wave 9.h.9 smoke; safe to delete.'
            subject       = "$tag SmokeNewsletter"
            bodyHtml      = "<p>$tag Wave 9.h.9 smoke; safe to delete.</p>"
            targetSegment = 'EventRegistrants'
            includeNewsletterSubscribers = $true
        }
        if ($script:newsletterEmailGroupId) {
            $body['emailGroupIds'] = @($script:newsletterEmailGroupId)
        }
        $r = Invoke-LcPost -Path '/api/Newsletters' -Body $body
        if (-not $r.Success) { throw "create failed: HTTP $($r.StatusCode) body: $($r.Body | ConvertTo-Json -Compress -Depth 3)" }
        if ($r.Body.id) { $script:newsletterId = $r.Body.id }
        elseif ($r.Body -is [string]) { $script:newsletterId = $r.Body.Trim('"') }
    }

    if ($script:newsletterId) {
        Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'update newsletter' -Endpoint 'PUT /api/Newsletters/{id}' -Action {
            $r = Invoke-LcPut -Path "/api/Newsletters/$($script:newsletterId)" -Body @{
                subject  = "$tag SmokeNewsletter Updated"
                bodyHtml = "<p>$tag Updated by 9.h.9.</p>"
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'publish newsletter' -Endpoint 'POST /api/Newsletters/{id}/publish' -Action {
            $r = Invoke-LcPost -Path "/api/Newsletters/$($script:newsletterId)/publish" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'unpublish newsletter' -Endpoint 'POST /api/Newsletters/{id}/unpublish' -Action {
            $r = Invoke-LcPost -Path "/api/Newsletters/$($script:newsletterId)/unpublish" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'reactivate newsletter' -Endpoint 'POST /api/Newsletters/{id}/reactivate' -Action {
            $r = Invoke-LcPost -Path "/api/Newsletters/$($script:newsletterId)/reactivate" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'send newsletter (real ACS email send)' -Endpoint 'POST /api/Newsletters/{id}/send' -Action {
            $r = Invoke-LcPost -Path "/api/Newsletters/$($script:newsletterId)/send" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'newsletters-mutators' -TestName 'delete newsletter' -Endpoint 'DELETE /api/Newsletters/{id}' -Action {
            $r = Invoke-LcDelete -Path "/api/Newsletters/$($script:newsletterId)"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        foreach ($n in 'update newsletter','publish newsletter','unpublish newsletter','reactivate newsletter','send newsletter','delete newsletter') {
            Add-LcResult -Report $Report -Status SKIP -Section 'newsletters-mutators' -TestName $n -Endpoint '...' -SkipReason 'create did not yield id'
        }
    }

    # Cleanup the helper email group
    if ($script:newsletterEmailGroupId) {
        Invoke-LcDelete -Path "/api/EmailGroups/$($script:newsletterEmailGroupId)" | Out-Null
    }
    Remove-LcFixturesByTag | Out-Null
}

function Invoke-NewslettersControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'newsletters-read'; Func = { Test-NewslettersReadFlow -Report $report } }
        @{ Name = 'newsletters-mutators'; Func = { Test-NewslettersMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-NewslettersController'
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
    $report = Invoke-NewslettersControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
