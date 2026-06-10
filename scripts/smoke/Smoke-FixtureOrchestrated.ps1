<#
.SYNOPSIS
    Wave4.9.1.4.b (2026-06-09): fixture-orchestrated mutator smokes
    for the 3 audit-found event-scoped routes that need a precondition.

.DESCRIPTION
    The base Smoke-Mutator.ps1 path-substitution lets you supply
    {eventId:guid} / {groupId:guid} via env var or -Id. But the 3
    routes flagged in docs/audit/route-inventory-2026-06-08.md need
    fixtures to exist FIRST:

      - photoAlbum-Create needs an event the caller organizes
      - emailGroup-Update needs an existing EmailGroup
      - registration-UpdateDetails needs an existing registration

    This script orchestrates the fixture lifecycle: create -> exercise
    the audit-mutator route -> verify -> cleanup. The result is one
    pass / fail per audit-route, with the smoke leaving no test data
    on staging.

.NOTES
    Wave4.9.1.4.b (deferred from Wave4.9.1.4 push).
    Depends on Invoke-Login.ps1 to populate $env:LC_BEARER + $env:LC_USER_ID.
#>
[CmdletBinding()]
param(
    [string]$StagingUrl = $(if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' }),
    # Override to point at a different owned event. Default = "Sample group tier
    # testing - Varuni" (Niroshana-owned + Published — PhotoAlbum create requires
    # the event to be in Published / Active / Completed / Archived status).
    # Earlier defaults that fail:
    #   - 5fbcea92-... ("Maname"): owned by SYSTEM admin, returns 400 organizer-only
    #   - 8e096789-... ("Varuni Group Pricing Test"): status=Draft (=4), 400 status-gate
    [string]$EventId = '61e61068-0f4b-4c6a-b8ae-b12ae5a502d3'
)

$ErrorActionPreference = 'Stop'

if (-not $env:LC_BEARER) {
    Write-Error 'Smoke-FixtureOrchestrated requires $env:LC_BEARER - call Invoke-Login.ps1 first.'
    exit 1
}

$headers = @{ Authorization = "Bearer $env:LC_BEARER" }
$results = @()
$anyFail = $false

function Invoke-Api {
    param([string]$Method, [string]$Path, [hashtable]$Body)
    $uri = "$StagingUrl$Path"
    $params = @{
        Uri = $uri; Method = $Method; Headers = $headers; TimeoutSec = 30
    }
    if ($Body) {
        $params['ContentType'] = 'application/json'
        $params['Body'] = ($Body | ConvertTo-Json -Compress -Depth 5)
    }
    return Invoke-RestMethod @params
}

# -----------------------------------------------------------------------------
# Fixture 1: EmailGroup. Create -> Update -> Delete.
# -----------------------------------------------------------------------------
Write-Host ''
Write-Host '=== emailGroup-Update (G1.d) ===' -ForegroundColor Cyan
$groupId = $null
# Name needs to be unique per run because DELETE on EmailGroup is a soft-delete
# (deactivate); the unique-name index still tracks deactivated rows. Use a
# tick-suffixed name so each run gets a fresh slot.
$nameSuffix = [DateTime]::UtcNow.Ticks
try {
    $createBody = @{
        name           = "Wave4.9.1.4.b Smoke Group $nameSuffix"
        emailAddresses = "smoke1@test.local, smoke2@test.local"
        description    = "Created by Smoke-FixtureOrchestrated; safe to delete"
    }
    $created = Invoke-Api -Method POST -Path '/api/EmailGroups' -Body $createBody
    $groupId = $created.id
    Write-Host "  CREATE OK groupId=$groupId" -ForegroundColor Gray

    $updateBody = @{
        name           = "Wave4.9.1.4.b Renamed $nameSuffix"
        emailAddresses = "smoke1@test.local, smoke3@test.local"
        description    = "Updated by Smoke-FixtureOrchestrated"
    }
    Invoke-Api -Method PUT -Path "/api/EmailGroups/$groupId" -Body $updateBody | Out-Null
    # PUT returns 204 No Content for EmailGroup.
    Write-Host "  UPDATE OK (204 No Content)" -ForegroundColor Gray

    # Re-fetch to assert UpdatedAt > CreatedAt
    $refetched = Invoke-Api -Method GET -Path "/api/EmailGroups/$groupId"
    if ($refetched.updatedAt -and $refetched.createdAt -and [datetime]$refetched.updatedAt -gt [datetime]$refetched.createdAt) {
        $results += "  [OK] emailGroup-Update: createdAt=$($refetched.createdAt) updatedAt=$($refetched.updatedAt) advanced"
    } else {
        $results += "  [FAIL] emailGroup-Update: UpdatedAt did not advance past CreatedAt (createdAt=$($refetched.createdAt) updatedAt=$($refetched.updatedAt))"
        $anyFail = $true
    }
}
catch {
    $results += "  [FAIL] emailGroup-Update: $($_.Exception.Message)"
    $anyFail = $true
}
finally {
    if ($groupId) {
        try {
            Invoke-Api -Method DELETE -Path "/api/EmailGroups/$groupId" | Out-Null
            Write-Host "  CLEANUP OK (deactivated $groupId)" -ForegroundColor DarkGray
        }
        catch {
            Write-Host "  CLEANUP FAILED: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

# -----------------------------------------------------------------------------
# Fixture 2: PhotoAlbum on a known event. Create -> verify -> Delete.
# Founder must own the EventId for create to succeed.
# -----------------------------------------------------------------------------
Write-Host ''
Write-Host '=== photoAlbum-Create (G2) ===' -ForegroundColor Cyan
$albumId = $null
try {
    $createBody = @{
        name        = "Wave4.9.1.4.b Smoke Album"
        description = "Created by Smoke-FixtureOrchestrated; safe to delete"
    }
    $created = Invoke-Api -Method POST -Path "/api/events/$EventId/albums" -Body $createBody
    $albumId = $created.id
    if ($created.createdAt) {
        $ageSec = ((Get-Date).ToUniversalTime() - ([datetime]$created.createdAt).ToUniversalTime()).TotalSeconds
        if ($ageSec -le 60 -and $ageSec -ge -2) {
            $results += "  [OK] photoAlbum-Create: id=$albumId createdAt=$($created.createdAt) (age $([Math]::Round($ageSec))s)"
        }
        else {
            $results += "  [FAIL] photoAlbum-Create: createdAt outside 60s window (age $ageSec s)"
            $anyFail = $true
        }
    }
    else {
        $results += "  [FAIL] photoAlbum-Create: response missing createdAt - $($created | ConvertTo-Json -Compress -Depth 3)"
        $anyFail = $true
    }
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $results += "  [FAIL] photoAlbum-Create: HTTP $statusCode $($_.Exception.Message)"
    $anyFail = $true
}
finally {
    if ($albumId) {
        try {
            Invoke-Api -Method DELETE -Path "/api/events/$EventId/albums/$albumId" | Out-Null
            Write-Host "  CLEANUP OK (deleted album $albumId)" -ForegroundColor DarkGray
        }
        catch {
            Write-Host "  CLEANUP FAILED: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

# -----------------------------------------------------------------------------
# registration-UpdateDetails is NOT exercised here because it requires the
# caller to have an existing registration on the target event. Creating a
# registration is a multi-step flow (potentially involving payment) that's
# out of scope for a fixture-orchestrated smoke. The route inventory G1.d
# entry is verified via unit tests + the Wave4.9.1.4 base route assertion;
# operator UAT covers the user-facing flow.
# -----------------------------------------------------------------------------
Write-Host ''
Write-Host '=== registration-UpdateDetails (G1.d) ===' -ForegroundColor Cyan
$results += "  [SKIP] registration-UpdateDetails: needs existing registration; deferred to operator UAT (no fixture-orchestration path)"

# -----------------------------------------------------------------------------
# Report
# -----------------------------------------------------------------------------
Write-Host ''
Write-Host 'Smoke-FixtureOrchestrated (Wave4.9.1.4.b) results:' -ForegroundColor Cyan
Write-Host '---------------------------------------------------'
$results | ForEach-Object { Write-Host $_ }
Write-Host ''

if ($anyFail) {
    Write-Error 'Smoke-FixtureOrchestrated FAILED - one or more fixture-orchestrated mutators did not round-trip cleanly. See lines above.'
    exit 1
}

Write-Host 'Smoke-FixtureOrchestrated OK (Wave4.9.1.4.b).' -ForegroundColor Green
exit 0
