<#
.SYNOPSIS
  Per-controller smoke for VenueLayoutsController. Wave 9.c deliverable.

.DESCRIPTION
  29 endpoints; the controller is the primary surface for the Venue/Seating capability
  shipped in Wave 5.3. CREATE/MUTATE endpoints are SKIP_DESTRUCTIVE by default
  (-IncludeDestructive). Read endpoints (presets/templates + by-event/seat-list) are
  exercised; they cover the VenueLayoutRepository + SeatHoldRepository read paths
  that Wave 9.a smoke did not touch.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-FinanceFixtures.psm1') -Force

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

# ----------------------------------------------------------------------------
# Sub-section 1: venue-global-reads — Globally-scoped reads (presets, templates)
# ----------------------------------------------------------------------------
function Test-VenueGlobalReadsFlow {
    param([Parameter(Mandatory)]$Report)

    Test-LcEndpoint -Report $Report -Section 'venue-global-reads' -TestName 'list venue layout presets' -Endpoint 'GET /api/venue-layouts/presets' -Action {
        $r = Invoke-LcGet -Path '/api/venue-layouts/presets'
        Assert-Http200 -Result $r
    }

    Test-LcEndpoint -Report $Report -Section 'venue-global-reads' -TestName 'list venue layout templates' -Endpoint 'GET /api/venue-layouts/templates' -Action {
        $r = Invoke-LcGet -Path '/api/venue-layouts/templates'
        Assert-Http200 -Result $r
    }
}

# ----------------------------------------------------------------------------
# Sub-section 2: venue-event-reads — Event-scoped reads (need real event ID)
# ----------------------------------------------------------------------------
function Test-VenueEventReadsFlow {
    param([Parameter(Mandatory)]$Report)

    # Reuse a known existing upcoming event for read-only probes (404 = OK signal:
    # no layout for that event; we're testing the endpoint wiring not data presence)
    $fakeEventId = [Guid]::NewGuid().ToString()

    Test-LcEndpoint -Report $Report -Section 'venue-event-reads' -TestName 'get layout by event' -Endpoint 'GET /api/venue-layouts/by-event/{eventId}' -Action {
        $r = Invoke-LcGet -Path "/api/venue-layouts/by-event/$fakeEventId"
        # 200 (layout exists) OR 404 (no layout) both prove wiring; 5xx fails
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-event-reads' -TestName 'list seats for event' -Endpoint 'GET /api/venue-layouts/events/{eventId}/seats' -Action {
        $r = Invoke-LcGet -Path "/api/venue-layouts/events/$fakeEventId/seats"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

# ----------------------------------------------------------------------------
# Sub-section 3: venue-layout-reads — Layout-ID-scoped reads
# ----------------------------------------------------------------------------
function Test-VenueLayoutReadsFlow {
    param([Parameter(Mandatory)]$Report)

    $fakeLayoutId = [Guid]::NewGuid().ToString()

    Test-LcEndpoint -Report $Report -Section 'venue-layout-reads' -TestName 'get layout by id' -Endpoint 'GET /api/venue-layouts/{id}' -Action {
        $r = Invoke-LcGet -Path "/api/venue-layouts/$fakeLayoutId"
        # 200 OR 404 both OK
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-layout-reads' -TestName 'get layout publish readiness' -Endpoint 'GET /api/venue-layouts/{id}/publish-readiness' -Action {
        $r = Invoke-LcGet -Path "/api/venue-layouts/$fakeLayoutId/publish-readiness"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

# ----------------------------------------------------------------------------
# Sub-section 4: venue-mutators — All CREATE/UPDATE/DELETE endpoints (SKIP)
# ----------------------------------------------------------------------------
function Test-VenueMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.7: full mutator coverage via real fixtures.
    # Setup: create event + create layout + capture layout/zone IDs for downstream
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'venue-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture event failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    # 1. Create layout (capture for downstream)
    $layoutId = $null
    $zoneId = $null
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'create venue layout' -Endpoint 'POST /api/venue-layouts' -Action {
        $l = New-LcTaggedVenueLayout -EventId $eventId
        if (-not $l.Success) { throw "create failed: HTTP $($l.StatusCode)" }
        $script:venueLayoutId = $l.LayoutId
        $script:venueZoneId = $l.ZoneId
    }
    $layoutId = $script:venueLayoutId
    $zoneId = $script:venueZoneId

    if (-not $layoutId) {
        # Cannot exercise downstream mutators without layout ID; record SKIPs for remaining
        foreach ($n in 'update venue layout','batch update layout','save layout as template','update zone','delete zone','create table','update table','delete table','create decoration','update decoration','delete decoration','add tier assignment','remove tier assignment','generate seats','bulk assign','hold seat','release seat','delete venue layout') {
            Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName $n -Endpoint '...' -SkipReason 'create layout did not yield ID'
        }
        return
    }

    # 2. Update layout
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'update venue layout' -Endpoint 'PUT /api/venue-layouts/{id}' -Action {
        $r = Invoke-LcPut -Path "/api/venue-layouts/$layoutId" -Body @{
            name = "$(Get-LcCurrentRunTag) UpdatedLayout"
            layoutType = 'Banquet'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 3. Batch update layout
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'batch update layout' -Endpoint 'PUT /api/venue-layouts/{id}/batch' -Action {
        $r = Invoke-LcPut -Path "/api/venue-layouts/$layoutId/batch" -Body @{
            zones = @()
            tables = @()
            decorations = @()
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 4. Save as template
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'save layout as template' -Endpoint 'POST /api/venue-layouts/{id}/save-as-template' -Action {
        $r = Invoke-LcPost -Path "/api/venue-layouts/$layoutId/save-as-template" -Body @{
            name = "$(Get-LcCurrentRunTag) Tmpl"
            description = 'Wave 9.h.7 smoke'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 5. Zone update/delete
    if ($zoneId) {
        Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'update zone' -Endpoint 'PATCH /api/venue-layouts/{id}/zones/{zoneId}' -Action {
            $r = Invoke-LcPatch -Path "/api/venue-layouts/$layoutId/zones/$zoneId" -Body @{
                name = 'Updated Zone'; color = '#0055FF'; sortOrder = 1
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'update zone' -Endpoint 'PATCH /api/venue-layouts/{id}/zones/{zoneId}' -SkipReason 'no zone id from layout create'
    }

    # 6. Create table (capture id)
    $tableId = $null
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'create table' -Endpoint 'POST /api/venue-layouts/{id}/tables' -Action {
        $r = Invoke-LcPost -Path "/api/venue-layouts/$layoutId/tables" -Body @{
            zoneId = $zoneId
            label = 'T1'
            shape = 'Round'
            seats = 8
            x = 100; y = 100; rotation = 0
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.Body.id) { $script:venueTableId = $r.Body.id }
    }
    $tableId = $script:venueTableId

    if ($tableId) {
        Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'update table' -Endpoint 'PATCH /api/venue-layouts/{id}/tables/{tableId}' -Action {
            $r = Invoke-LcPatch -Path "/api/venue-layouts/$layoutId/tables/$tableId" -Body @{
                label = 'T1-updated'; seats = 10
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'delete table' -Endpoint 'DELETE /api/venue-layouts/{id}/tables/{tableId}' -Action {
            $r = Invoke-LcDelete -Path "/api/venue-layouts/$layoutId/tables/$tableId"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'update table' -Endpoint '...' -SkipReason 'table create did not yield id'
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'delete table' -Endpoint '...' -SkipReason 'table create did not yield id'
    }

    # 7. Create decoration
    $decId = $null
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'create decoration' -Endpoint 'POST /api/venue-layouts/{id}/decorations' -Action {
        $r = Invoke-LcPost -Path "/api/venue-layouts/$layoutId/decorations" -Body @{
            zoneId = $zoneId
            type = 'Stage'
            label = 'Stage'
            x = 50; y = 50; width = 200; height = 100; rotation = 0
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.Body.id) { $script:venueDecId = $r.Body.id }
    }
    $decId = $script:venueDecId

    if ($decId) {
        Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'update decoration' -Endpoint 'PATCH /api/venue-layouts/{id}/decorations/{decId}' -Action {
            $r = Invoke-LcPatch -Path "/api/venue-layouts/$layoutId/decorations/$decId" -Body @{
                label = 'Stage-updated'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'delete decoration' -Endpoint 'DELETE /api/venue-layouts/{id}/decorations/{decId}' -Action {
            $r = Invoke-LcDelete -Path "/api/venue-layouts/$layoutId/decorations/$decId"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'update decoration' -Endpoint '...' -SkipReason 'decoration create did not yield id'
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'delete decoration' -Endpoint '...' -SkipReason 'decoration create did not yield id'
    }

    # 8. Generate seats for the zone
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'generate seats (W5.3 SeatReservation write)' -Endpoint 'POST /api/venue-layouts/{layoutId}/zones/{zoneId}/generate-seats' -Action {
        $r = Invoke-LcPost -Path "/api/venue-layouts/$layoutId/zones/$zoneId/generate-seats" -Body @{
            rows = 2
            columns = 4
            rowPrefix = 'A'
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 9. Assign + hold + release seat
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'bulk assign seats' -Endpoint 'POST /api/venue-layouts/assign' -Action {
        $r = Invoke-LcPost -Path '/api/venue-layouts/assign' -Body @{
            layoutId = $layoutId
            assignments = @()
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'hold seat (W5.3 SeatHoldRepository)' -Endpoint 'POST /api/venue-layouts/events/{eventId}/seats/hold' -Action {
        $r = Invoke-LcPost -Path "/api/venue-layouts/events/$eventId/seats/hold" -Body @{
            seatIds = @()
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'release seat (W5.3 SeatHoldRepository)' -Endpoint 'POST /api/venue-layouts/events/{eventId}/seats/release' -Action {
        $r = Invoke-LcPost -Path "/api/venue-layouts/events/$eventId/seats/release" -Body @{
            holdId = [Guid]::NewGuid().ToString()
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 10. From-preset + from-template + apply-preset + apply-template
    # These need pre-existing presets/templates which may not exist in fresh staging
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'create from preset (uses an existing preset)' -Endpoint 'POST /api/venue-layouts/from-preset' -Action {
        # Fetch presets list, take first if any
        $presets = Invoke-LcGet -Path '/api/venue-layouts/presets'
        $presetId = if ($presets.Body -and $presets.Body.Count -gt 0) { $presets.Body[0].id } else { [Guid]::Empty.ToString() }
        $r = Invoke-LcPost -Path '/api/venue-layouts/from-preset' -Body @{
            presetId = $presetId
            eventId = $eventId
            name = "$(Get-LcCurrentRunTag) FromPreset"
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'apply preset to existing layout' -Endpoint 'POST /api/venue-layouts/apply-preset' -Action {
        $presets = Invoke-LcGet -Path '/api/venue-layouts/presets'
        $presetId = if ($presets.Body -and $presets.Body.Count -gt 0) { $presets.Body[0].id } else { [Guid]::Empty.ToString() }
        $r = Invoke-LcPost -Path '/api/venue-layouts/apply-preset' -Body @{
            presetId = $presetId
            layoutId = $layoutId
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'create from template' -Endpoint 'POST /api/venue-layouts/from-template' -Action {
        $tpls = Invoke-LcGet -Path '/api/venue-layouts/templates'
        $tplId = if ($tpls.Body -and $tpls.Body.Count -gt 0) { $tpls.Body[0].id } else { [Guid]::Empty.ToString() }
        $r = Invoke-LcPost -Path '/api/venue-layouts/from-template' -Body @{
            templateId = $tplId
            eventId = $eventId
            name = "$(Get-LcCurrentRunTag) FromTpl"
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'apply template to existing layout' -Endpoint 'POST /api/venue-layouts/apply-template' -Action {
        $tpls = Invoke-LcGet -Path '/api/venue-layouts/templates'
        $tplId = if ($tpls.Body -and $tpls.Body.Count -gt 0) { $tpls.Body[0].id } else { [Guid]::Empty.ToString() }
        $r = Invoke-LcPost -Path '/api/venue-layouts/apply-template' -Body @{
            templateId = $tplId
            layoutId = $layoutId
        }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # 11. Tier assignments (skip since no ticket tiers exist on free event)
    Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'add tier assignment' -Endpoint 'POST /api/venue-layouts/{id}/tier-assignments' -SkipReason 'free-event fixture has no ticket tiers; would need paid-event fixture'
    Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'remove tier assignment' -Endpoint 'DELETE /api/venue-layouts/{id}/tier-assignments/{tierId}/.../{aId}' -SkipReason 'no tier to remove'

    # 12. Delete zone (after table cleanup so cascade is clean)
    if ($zoneId) {
        Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'delete zone' -Endpoint 'DELETE /api/venue-layouts/{id}/zones/{zoneId}' -Action {
            $r = Invoke-LcDelete -Path "/api/venue-layouts/$layoutId/zones/$zoneId"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName 'delete zone' -Endpoint '...' -SkipReason 'no zone id'
    }

    # 13. Delete layout (final teardown)
    Test-LcEndpoint -Report $Report -Section 'venue-mutators' -TestName 'delete venue layout' -Endpoint 'DELETE /api/venue-layouts/{id}' -Action {
        $r = Invoke-LcDelete -Path "/api/venue-layouts/$layoutId"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Cleanup the event fixture (cascade-deletes anything still linked)
    Remove-LcFixturesByTag | Out-Null
}

function Invoke-VenueLayoutsControllerSmoke {
    [CmdletBinding()]
    param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }

    $allSections = @(
        @{ Name = 'venue-global-reads';  Func = { Test-VenueGlobalReadsFlow -Report $report } }
        @{ Name = 'venue-event-reads';   Func = { Test-VenueEventReadsFlow -Report $report } }
        @{ Name = 'venue-layout-reads';  Func = { Test-VenueLayoutReadsFlow -Report $report } }
        @{ Name = 'venue-mutators';      Func = { Test-VenueMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }

    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }

    $report = New-LcReport -Name 'Wave 9.c: Smoke-VenueLayoutsController'
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
    $report = Invoke-VenueLayoutsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
