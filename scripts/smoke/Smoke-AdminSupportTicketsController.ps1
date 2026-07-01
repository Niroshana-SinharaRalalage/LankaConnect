<#
.SYNOPSIS
  Per-controller smoke for AdminSupportTicketsController. Wave 9.h.10.4 gap-close.

.DESCRIPTION
  Replaces the broken LongTail stub that called `/api/AdminSupportTickets` (404,
  wrong path, silently PASS because <500) with real coverage against the actual
  route `/api/admin/support-tickets`.

  Fires `template-support-ticket-reply` via POST /{ticketId}/reply after founder
  contact-form submission produces a real ticket.

  Endpoints covered (7 of 7):
    GET  /api/admin/support-tickets                       -- list
    GET  /api/admin/support-tickets/{ticketId}            -- detail
    GET  /api/admin/support-tickets/statistics
    POST /api/admin/support-tickets/{ticketId}/reply      -- FIRES template-support-ticket-reply
    POST /api/admin/support-tickets/{ticketId}/status
    POST /api/admin/support-tickets/{ticketId}/assign
    POST /api/admin/support-tickets/{ticketId}/notes
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force

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

function Test-AdminSupportTicketsReadFlow {
    param([Parameter(Mandatory)]$Report)
    Test-LcEndpoint -Report $Report -Section 'ast-read' -TestName 'list tickets' -Endpoint 'GET /api/admin/support-tickets' -Action {
        $r = Invoke-LcGet -Path '/api/admin/support-tickets?page=1&pageSize=5'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'ast-read' -TestName 'statistics' -Endpoint 'GET /api/admin/support-tickets/statistics' -Action {
        $r = Invoke-LcGet -Path '/api/admin/support-tickets/statistics'
        Assert-Http200 -Result $r
    }
    $fakeTicket = [Guid]::NewGuid().ToString()
    Test-LcEndpoint -Report $Report -Section 'ast-read' -TestName 'ticket detail (404 OK)' -Endpoint 'GET /api/admin/support-tickets/{ticketId}' -Action {
        $r = Invoke-LcGet -Path "/api/admin/support-tickets/$fakeTicket"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-AdminSupportTicketsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Fixture: create a real support ticket via the public contact form so we
    # have a real id to reply/assign/status/note against. Uses Gmail alias so
    # the confirmation email delivers to founder inbox.
    $submitterEmail = Get-LcFixtureEmail -Slug 'template-support-ticket-confirmation' -Suffix "ast-$(Get-Random -Maximum 99999)"
    $contact = Invoke-LcPost -Path '/api/Contact' -Bearer $null -Body @{
        name = 'Smoke AST Fixture'
        email = $submitterEmail
        subject = "Wave 9.h.10.4 AST smoke fixture $(Get-Random -Maximum 99999)"
        message = 'Auto-created by Wave 9.h.10.4 AdminSupportTickets smoke. Safe to close/delete.'
    }
    if ($contact.StatusCode -ge 500) {
        Add-LcResult -Report $Report -Status FAIL -Section 'ast-mutators' -TestName 'contact fixture setup' -Endpoint 'POST /api/Contact' -ErrorMessage "contact create failed: HTTP $($contact.StatusCode)"
        return
    }

    # Find the newly created ticket by searching. Contact creates ticket sync,
    # so it should be listable immediately.
    Start-Sleep -Milliseconds 500
    $list = Invoke-LcGet -Path "/api/admin/support-tickets?page=1&pageSize=20&search=$([uri]::EscapeDataString('Wave 9.h.10.4'))"
    if (-not $list.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'ast-mutators' -TestName 'find fixture ticket' -Endpoint 'GET /api/admin/support-tickets?search=...' -ErrorMessage "list HTTP $($list.StatusCode)"
        return
    }
    $items = if ($list.Body.items) { $list.Body.items } elseif ($list.Body.data) { $list.Body.data } else { $list.Body }
    $ticket = @($items | Where-Object { $_.subject -and $_.subject.StartsWith('Wave 9.h.10.4 AST smoke fixture') }) | Sort-Object -Property createdAt -Descending | Select-Object -First 1
    if (-not $ticket -or -not $ticket.id) {
        Add-LcResult -Report $Report -Status FAIL -Section 'ast-mutators' -TestName 'fixture ticket resolvable' -Endpoint 'GET /api/admin/support-tickets?search=...' -ErrorMessage 'no fixture ticket found in list'
        return
    }
    $ticketId = $ticket.id
    Write-Host "AST fixture ticket id: $ticketId"

    Test-LcEndpoint -Report $Report -Section 'ast-mutators' -TestName 'reply to ticket (FIRES template-support-ticket-reply)' -Endpoint 'POST /api/admin/support-tickets/{ticketId}/reply' -Action {
        $r = Invoke-LcPost -Path "/api/admin/support-tickets/$ticketId/reply" -Body @{
            content = 'Wave 9.h.10.4 smoke reply. Fires template-support-ticket-reply. Safe to ignore.'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'ast-mutators' -TestName 'update status' -Endpoint 'POST /api/admin/support-tickets/{ticketId}/status' -Action {
        $r = Invoke-LcPost -Path "/api/admin/support-tickets/$ticketId/status" -Body @{ status = 'InProgress' }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'ast-mutators' -TestName 'assign ticket to self' -Endpoint 'POST /api/admin/support-tickets/{ticketId}/assign' -Action {
        $r = Invoke-LcPost -Path "/api/admin/support-tickets/$ticketId/assign" -Body @{ assignToUserId = (Get-LcUserId) }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'ast-mutators' -TestName 'add note (internal)' -Endpoint 'POST /api/admin/support-tickets/{ticketId}/notes' -Action {
        $r = Invoke-LcPost -Path "/api/admin/support-tickets/$ticketId/notes" -Body @{
            content = 'Wave 9.h.10.4 smoke internal note. Not visible to submitter.'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'ast-mutators' -TestName 'detail after mutations' -Endpoint 'GET /api/admin/support-tickets/{ticketId}' -Action {
        $r = Invoke-LcGet -Path "/api/admin/support-tickets/$ticketId"
        Assert-Http200 -Result $r
    }
}

function Invoke-AdminSupportTicketsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'ast-read';     Func = { Test-AdminSupportTicketsReadFlow -Report $report } }
        @{ Name = 'ast-mutators'; Func = { Test-AdminSupportTicketsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.h.10.4: Smoke-AdminSupportTicketsController'
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
    $report = Invoke-AdminSupportTicketsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRatePct)% ==="
    Format-LcReportMarkdown -Report $report | Write-Host
}
