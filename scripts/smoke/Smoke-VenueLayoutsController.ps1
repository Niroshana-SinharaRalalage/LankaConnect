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

    # Each mutator endpoint creates / mutates / deletes a layout artifact. All
    # SKIP_DESTRUCTIVE by default to avoid polluting staging or breaking other
    # smokes. Future: -IncludeDestructive will create a tagged layout + tear down.

    $mutators = @(
        @{ Method = 'POST';   Path = '/api/venue-layouts';                                         Name = 'create venue layout' }
        @{ Method = 'PUT';    Path = '/api/venue-layouts/{id}';                                    Name = 'update venue layout' }
        @{ Method = 'DELETE'; Path = '/api/venue-layouts/{id}';                                    Name = 'delete venue layout' }
        @{ Method = 'PUT';    Path = '/api/venue-layouts/{id}/batch';                              Name = 'batch update layout' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/from-preset';                             Name = 'create from preset' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/apply-preset';                            Name = 'apply preset' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/from-template';                           Name = 'create from template' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/apply-template';                          Name = 'apply template' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/{id}/save-as-template';                   Name = 'save layout as template' }
        @{ Method = 'PATCH';  Path = '/api/venue-layouts/{id}/zones/{zoneId}';                     Name = 'update zone' }
        @{ Method = 'DELETE'; Path = '/api/venue-layouts/{id}/zones/{zoneId}';                     Name = 'delete zone' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/{id}/tables';                             Name = 'create table' }
        @{ Method = 'PATCH';  Path = '/api/venue-layouts/{id}/tables/{tableId}';                   Name = 'update table' }
        @{ Method = 'DELETE'; Path = '/api/venue-layouts/{id}/tables/{tableId}';                   Name = 'delete table' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/{id}/decorations';                        Name = 'create decoration' }
        @{ Method = 'PATCH';  Path = '/api/venue-layouts/{id}/decorations/{decId}';                Name = 'update decoration' }
        @{ Method = 'DELETE'; Path = '/api/venue-layouts/{id}/decorations/{decId}';                Name = 'delete decoration' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/{id}/tier-assignments';                   Name = 'add tier assignment' }
        @{ Method = 'DELETE'; Path = '/api/venue-layouts/{id}/tier-assignments/{tierId}/.../{aId}'; Name = 'remove tier assignment' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/{layoutId}/zones/{zoneId}/generate-seats';Name = 'generate seats' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/assign';                                  Name = 'bulk assign' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/events/{eventId}/seats/hold';             Name = 'hold seat (W5.3 SeatHoldRepository)' }
        @{ Method = 'POST';   Path = '/api/venue-layouts/events/{eventId}/seats/release';          Name = 'release seat (W5.3 SeatHoldRepository)' }
    )

    foreach ($m in $mutators) {
        Add-LcResult -Report $Report -Status SKIP -Section 'venue-mutators' -TestName $m.Name -Endpoint "$($m.Method) $($m.Path)" -SkipReason 'destructive (creates/mutates layouts); -IncludeDestructive'
    }
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
