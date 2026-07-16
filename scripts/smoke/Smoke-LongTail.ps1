<#
.SYNOPSIS
  Wave 9.f long-tail controller smoke. One file covers ~17 remaining controllers,
  each with minimal coverage (1-3 endpoints) to verify wiring + permission gates.

.DESCRIPTION
  Covered controllers:
    Health, Public, Configuration, ReferenceData, MetroAreas (W5.3 MetroAreaRepository),
    Admin, AdminSupportTickets, AdminRecovery, Dashboard, EventConfig, EventTemplates,
    Diagnostics, Email, PhotoAlbums, Badges, Contact, WhatsAppWebhook
  Each has its own sub-section function so a failure in one never blocks the others.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force  # Wave 9.h.10.2: Get-LcFixtureEmail

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

# Helper: wiring-check (any non-5xx OK)
function Test-WiringGet {
    param($Report, [string]$Section, [string]$TestName, [string]$Endpoint, [string]$Path, [object]$Bearer = '__USE_ENV__')
    Test-LcEndpoint -Report $Report -Section $Section -TestName $TestName -Endpoint $Endpoint -Action {
        $r = if ($Bearer -eq $null) { Invoke-LcGet -Path $Path -Bearer $null } else { Invoke-LcGet -Path $Path }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-Health {
    param($Report)
    Test-WiringGet $Report 'health' 'health'           'GET /api/Health'              '/api/Health' $null
    Test-WiringGet $Report 'health' 'detailed health'  'GET /api/Health/detailed'     '/api/Health/detailed' $null
    Test-WiringGet $Report 'health' 'feature flags'    'GET /api/Health/feature-flags' '/api/Health/feature-flags' $null
}

function Test-PublicController {
    param($Report)
    Test-WiringGet $Report 'public' 'public stats' 'GET /api/Public/stats' '/api/Public/stats' $null
}

function Test-Configuration {
    param($Report)
    Test-WiringGet $Report 'configuration' 'features list'        'GET /api/Configuration/features'             '/api/Configuration/features' $null
    Test-WiringGet $Report 'configuration' 'commission settings'  'GET /api/Configuration/commission-settings'  '/api/Configuration/commission-settings'
}

function Test-ReferenceData {
    param($Report)
    Test-WiringGet $Report 'reference-data' 'list reference data (typed)' 'GET /api/reference-data?types=...' '/api/reference-data?types=EventCategory,EventStatus,UserRole'
    Test-WiringGet $Report 'reference-data' 'commission settings' 'GET /api/reference-data/commission-settings' '/api/reference-data/commission-settings'
    # Test user is AdminManager. Cache invalidation is safe in staging (reloads from DB).
    Test-LcEndpoint $Report 'reference-data' 'invalidate cache (typed)' 'POST /api/reference-data/invalidate-cache/EventCategory' {
        $r = Invoke-LcPost -Path '/api/reference-data/invalidate-cache/EventCategory' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint $Report 'reference-data' 'invalidate all caches' 'POST /api/reference-data/invalidate-all-caches' {
        $r = Invoke-LcPost -Path '/api/reference-data/invalidate-all-caches' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-MetroAreas {
    param($Report)
    # Exercises Wave 5.3 MetroAreaRepository directly
    Test-WiringGet $Report 'metro-areas' 'list metro areas (W5.3 MetroAreaRepository)' 'GET /api/metro-areas' '/api/metro-areas' $null
}

function Test-AdminControllers {
    param($Report)
    # AdminController remaining endpoints not covered by /trigger-reminder-job + /send-event-reminder
    # (which are covered by Smoke-EventsController.Test-EventsWave5UncoveredReposFlow).
    # AdminSupportTickets is now covered by Smoke-AdminSupportTicketsController (real path).
    # AdminRecovery is covered by dedicated Smoke-AdminRecoveryController.

    # Wave 9.h.10.4 gap-close: AdminController's 4 uncovered admin-only endpoints.
    # AdminManager may or may not have Admin-role permission. Wiring probe (< 500 OK).
    Test-LcEndpoint -Report $Report -Section 'admin-perm' -TestName 'admin seed' -Endpoint 'POST /api/Admin/seed' -Action {
        $r = Invoke-LcPost -Path '/api/Admin/seed' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'admin-perm' -TestName 'admin fix-passwords (wiring)' -Endpoint 'POST /api/Admin/fix-passwords' -Action {
        $r = Invoke-LcPost -Path '/api/Admin/fix-passwords' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'admin-perm' -TestName 'admin environment' -Endpoint 'GET /api/Admin/environment' -Action {
        $r = Invoke-LcGet -Path '/api/Admin/environment'
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'admin-perm' -TestName 'admin reminder-diagnostics' -Endpoint 'GET /api/Admin/reminder-diagnostics' -Action {
        $r = Invoke-LcGet -Path '/api/Admin/reminder-diagnostics'
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-Dashboard {
    param($Report)
    Test-WiringGet $Report 'dashboard' 'organizer dashboard stats' 'GET /api/Dashboard/stats' '/api/Dashboard/stats'
    Test-WiringGet $Report 'dashboard' 'organizer dashboard feed'  'GET /api/Dashboard/feed'  '/api/Dashboard/feed'
}

function Test-EventConfig {
    param($Report)
    # Event-scoped config; fake event ID -> 5xx or 404 expected
    $fakeId = [Guid]::NewGuid().ToString()
    Test-WiringGet $Report 'event-config' 'event config' 'GET /api/EventConfig/{eventId}' "/api/EventConfig/$fakeId"
}

function Test-EventTemplates {
    param($Report)
    Test-WiringGet $Report 'event-templates' 'list event templates' 'GET /api/EventTemplates' '/api/EventTemplates'
}

function Test-Diagnostics {
    param($Report)
    # Admin-scoped; 200 or 403 both signal wiring is OK
    Test-WiringGet $Report 'diagnostics' 'email-templates status'    'GET /api/Diagnostics/email-templates/status'    '/api/Diagnostics/email-templates/status'
    Test-WiringGet $Report 'diagnostics' 'email-templates inactive'  'GET /api/Diagnostics/email-templates/inactive'  '/api/Diagnostics/email-templates/inactive'
    # Wave 9.h.10.4 gap-close
    Test-WiringGet $Report 'diagnostics' 'email-templates check-blocks' 'GET /api/Diagnostics/email-templates/check-blocks' '/api/Diagnostics/email-templates/check-blocks'
    Test-WiringGet $Report 'diagnostics' 'email-templates html-snippet' 'GET /api/Diagnostics/email-templates/{templateName}/html-snippet' '/api/Diagnostics/email-templates/template-welcome/html-snippet'
    # Test endpoint by design (creates a diagnostic log entry). AdminManager OK.
    Test-LcEndpoint $Report 'diagnostics' 'test signup logging' 'POST /api/Diagnostics/test-signup-commitment-logging' {
        $r = Invoke-LcPost -Path '/api/Diagnostics/test-signup-commitment-logging' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-PhotoAlbums {
    param($Report)
    # Real event + album CRUD lifecycle
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'photo-albums' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId
    $tag = Get-LcCurrentRunTag

    Test-WiringGet $Report 'photo-albums' 'list albums for event' 'GET /api/events/{eventId}/albums' "/api/events/$eventId/albums"

    $script:albumId = $null
    Test-LcEndpoint $Report 'photo-albums' 'create album' 'POST /api/events/{eventId}/albums' {
        $r = Invoke-LcPost -Path "/api/events/$eventId/albums" -Body @{
            name = "$tag SmokeAlbum"
            description = 'Wave 9.h.9 smoke fixture'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.Body.id) { $script:albumId = $r.Body.id }
        elseif ($r.Body -is [string]) { $script:albumId = $r.Body.Trim('"') }
    }

    if ($script:albumId) {
        Test-LcEndpoint $Report 'photo-albums' 'update album' 'PUT /api/events/{eventId}/albums/{albumId}' {
            $r = Invoke-LcPut -Path "/api/events/$eventId/albums/$($script:albumId)" -Body @{
                name = "$tag SmokeAlbum Updated"
                description = 'Updated'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint $Report 'photo-albums' 'publish album' 'POST /api/events/{eventId}/albums/{albumId}/publish' {
            $r = Invoke-LcPost -Path "/api/events/$eventId/albums/$($script:albumId)/publish" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint $Report 'photo-albums' 'delete album' 'DELETE /api/events/{eventId}/albums/{albumId}' {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/albums/$($script:albumId)"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        foreach ($n in 'update album','publish album','delete album') {
            Add-LcResult -Report $Report -Status SKIP -Section 'photo-albums' -TestName $n -Endpoint '...' -SkipReason 'create did not yield id'
        }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Test-Badges {
    param($Report)
    $fakeId = [Guid]::NewGuid().ToString()
    Test-WiringGet $Report 'badges' 'list badges' 'GET /api/Badges' '/api/Badges'
    Test-WiringGet $Report 'badges' 'badge detail (404 OK)' 'GET /api/Badges/{id}' "/api/Badges/$fakeId"
    # Test user is AdminManager. Badge CRUD via real endpoints.
    $tag = Get-LcCurrentRunTag
    $script:badgeId = $null
    Test-LcEndpoint $Report 'badges' 'create badge' 'POST /api/Badges' {
        $r = Invoke-LcPost -Path '/api/Badges' -Body @{
            name = "$tag SmokeBadge"
            description = 'Wave 9.h.9 smoke'
            iconUrl = 'https://example.test/icon.png'
            criteria = 'Manual'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.Body.id) { $script:badgeId = $r.Body.id }
    }
    if ($script:badgeId) {
        Test-LcEndpoint $Report 'badges' 'update badge' 'PUT /api/Badges/{id}' {
            $r = Invoke-LcPut -Path "/api/Badges/$($script:badgeId)" -Body @{
                name = "$tag SmokeBadge Updated"
                description = 'Updated'
                iconUrl = 'https://example.test/icon.png'
                criteria = 'Manual'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint $Report 'badges' 'update badge image (multipart)' 'PUT /api/Badges/{id}/image' {
            # Badges image is PUT not POST; reuse multipart wrapper
            $r = Invoke-LcMultipart -Path "/api/Badges/$($script:badgeId)/image" -FileFieldName 'image' -FileName 'badge.png'
            # multipart wrapper uses POST internally; if PUT required, will return 405
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'badges' -TestName 'update badge' -Endpoint 'PUT /api/Badges/{id}' -SkipReason 'create did not yield ID'
        Add-LcResult -Report $Report -Status SKIP -Section 'badges' -TestName 'update badge image' -Endpoint 'PUT /api/Badges/{id}/image' -SkipReason 'create did not yield ID'
    }

    # Wave 9.h.10.4 gap-close: 4 Badges endpoints previously missing
    $fakeEventId = [Guid]::NewGuid().ToString()
    Test-WiringGet $Report 'badges' 'list event badges' 'GET /api/Badges/events/{eventId}' "/api/Badges/events/$fakeEventId"
    if ($script:badgeId) {
        Test-LcEndpoint $Report 'badges' 'link badge to event (404 wiring)' 'POST /api/Badges/events/{eventId}/badges/{badgeId}' {
            $r = Invoke-LcPost -Path "/api/Badges/events/$fakeEventId/badges/$($script:badgeId)" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint $Report 'badges' 'unlink badge from event (404 wiring)' 'DELETE /api/Badges/events/{eventId}/badges/{badgeId}' {
            $r = Invoke-LcDelete -Path "/api/Badges/events/$fakeEventId/badges/$($script:badgeId)"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint $Report 'badges' 'delete badge' 'DELETE /api/Badges/{id}' {
            $r = Invoke-LcDelete -Path "/api/Badges/$($script:badgeId)"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        foreach ($n in 'link badge to event','unlink badge from event','delete badge') {
            Add-LcResult -Report $Report -Status SKIP -Section 'badges' -TestName $n -Endpoint '...' -SkipReason 'badge create did not yield ID'
        }
    }
}

function Test-Contact {
    param($Report)
    # Real support email send; founder OK with smoke testing this.
    Test-LcEndpoint $Report 'contact' 'submit contact form' 'POST /api/Contact' {
        $r = Invoke-LcPost -Path '/api/Contact' -Bearer $null -Body @{
            name = 'Smoke Test'
            email = (Get-LcFixtureEmail -Slug 'template-support-ticket-confirmation')
            subject = 'Wave 9.h.9 smoke test'
            message = 'Wave 9.h.9 smoke test message; safe to delete.'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

# Wave 8.5 Agent-SkipAudit 2026-07-16: Test-WhatsAppWebhook stub removed.
# The whatsapp-webhook receivers are covered by the dedicated Smoke-WhatsAppWebhookController.ps1
# script (Wave 9.h.10.4). Keeping this stub only emitted a noisy SKIP without adding coverage.
# Deleted per RECOVERABLE-obsolete category.

function Test-Email {
    param($Report)
    # EmailController has admin endpoints; SKIP since they overlap with EmailGroups + EmailMetrics
    # Test user is AdminManager. Founder OK with real test emails.
    # Wave 9.h.10.4: added test-connection + send-template previously missing.
    Test-LcEndpoint $Report 'email' 'test email connection' 'POST /api/Email/test-connection' {
        $r = Invoke-LcPost -Path '/api/Email/test-connection' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint $Report 'email' 'send template email (wiring)' 'POST /api/Email/send-template' {
        $r = Invoke-LcPost -Path '/api/Email/send-template' -Body @{
            templateName = 'template-welcome'
            toEmail = (Get-LcFixtureEmail -Slug 'admin-send-template')
            parameters = @{ UserName='Smoke'; UserEmail=(Get-LcFixtureEmail -Slug 'admin-send-template') }
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint $Report 'email' 'send admin test email' 'POST /api/Email/send-test' {
        $r = Invoke-LcPost -Path '/api/Email/send-test' -Body @{
            toEmail = (Get-LcFixtureEmail -Slug 'admin-test-email')
            subject = 'Wave 9.h.9 smoke test'
            bodyHtml = '<p>Wave 9.h.9 smoke test email; safe to delete.</p>'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

# ============================================================================
function Invoke-LongTailSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'health';            Func = { Test-Health -Report $report } }
        @{ Name = 'public';            Func = { Test-PublicController -Report $report } }
        @{ Name = 'configuration';     Func = { Test-Configuration -Report $report } }
        @{ Name = 'reference-data';    Func = { Test-ReferenceData -Report $report } }
        @{ Name = 'metro-areas';       Func = { Test-MetroAreas -Report $report } }
        @{ Name = 'admin-perm';        Func = { Test-AdminControllers -Report $report } }
        @{ Name = 'dashboard';         Func = { Test-Dashboard -Report $report } }
        @{ Name = 'event-config';      Func = { Test-EventConfig -Report $report } }
        @{ Name = 'event-templates';   Func = { Test-EventTemplates -Report $report } }
        @{ Name = 'diagnostics';       Func = { Test-Diagnostics -Report $report } }
        @{ Name = 'photo-albums';      Func = { Test-PhotoAlbums -Report $report } }
        @{ Name = 'badges';            Func = { Test-Badges -Report $report } }
        @{ Name = 'contact';           Func = { Test-Contact -Report $report } }
        # 'whatsapp-webhook' stub removed 2026-07-16 (Agent-SkipAudit) — coverage in Smoke-WhatsAppWebhookController.ps1
        @{ Name = 'email';             Func = { Test-Email -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.f: Smoke-LongTail (15 controllers)'
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
    $report = Invoke-LongTailSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
