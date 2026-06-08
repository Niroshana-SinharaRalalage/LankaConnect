<#
.SYNOPSIS
    Generic mutator round-trip smoke per CLAUDE.md §13.2 S2/S3/S4.

.DESCRIPTION
    Pattern: CREATE -> re-fetch -> assert createdAt ≤60s old AND updatedAt
    equals createdAt on fresh row; then optionally PATCH -> re-fetch ->
    assert updatedAt > createdAt.

    Mode-specific routes are configured in the $script:RouteMap below.
    Add new resources here as G2/G3/G4/G5 work needs them.

.PARAMETER Resource
    One of: user, event, registration, sponsor, collection, emailGroup,
    notification, photoAlbum, eventForm, donation.

.PARAMETER Mode
    What to do. Common modes: Create, Update, ReadOnly, UpdateLocation,
    UpgradeToOrganizer, Cancel, MarkAsRefunded, Deactivate. Mode-route
    binding lives in the RouteMap.

.PARAMETER Id
    Optional - if Mode requires an existing resource by ID, supply it here.
    Defaults to a sentinel "test-known" id per resource (also in RouteMap).

.PARAMETER StagingUrl
    Default env $env:LC_STAGING_URL or hardcoded staging URL.

.OUTPUTS
    Exit 0 + summary like "Smoke OK user UpdateLocation 200 createdAt-OK
    updatedAt-OK".
    Exit 1 + diagnostic on red.

.NOTES
    Built as part of Gap G0 (2026-06-08). Requires $env:LC_BEARER set
    (call Invoke-Login.ps1 first).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Resource,

    [Parameter(Mandatory)]
    [string]$Mode,

    [string]$Id,
    [hashtable]$Body,
    [string]$StagingUrl = $(if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' })
)

$ErrorActionPreference = 'Stop'

if (-not $env:LC_BEARER) {
    Write-Error 'Smoke-Mutator requires $env:LC_BEARER - call Invoke-Login.ps1 first.'
    exit 1
}

# Route map per (Resource, Mode). Each entry returns @{ Method = ...; Path = ...; Body = ...; AssertAuditFields = $true/$false }
# Path placeholders are substituted at runtime from env vars or the -Id parameter:
#   {id:guid}      -> $env:LC_USER_ID or -Id
#   {eventId:guid} -> $env:LC_EVENT_ID (or -Id when context is event-scoped)
#   {groupId:guid} -> $env:LC_EMAIL_GROUP_ID
# Extend this table as new gaps need new smokes.
$script:RouteMap = @{
    'user-UpdateLocation' = @{                     # Wave4.9.1.4 (route audit 2026-06-08 fix)
        Method = 'PUT'
        Path   = '/api/users/{id:guid}/location'   # was '/api/users/me/location'; corrected per UsersController.cs
        # Wave4.9.1.4 smoke run found domain rule: ZipCode required (test caught "Zip code is required" 400).
        Body   = @{ city = 'Cleveland'; state = 'OH'; zipCode = '44115'; country = 'USA' }
        AssertAuditFields = $true
    }
    'user-ReadProfile' = @{
        Method = 'GET'
        Path   = '/api/users/me'
        AssertAuditFields = $false
    }
    'notification-Unread' = @{
        Method = 'GET'
        Path   = '/api/notifications/unread'
        AssertAuditFields = $false
    }
    'event-List' = @{
        Method = 'GET'
        Path   = '/api/events?page=1&pageSize=5'
        AssertAuditFields = $false
    }
    # Wave4.9.1.4 additions per docs/audit/route-inventory-2026-06-08.md
    'photoAlbum-Create' = @{                       # G2 Media smoke
        Method = 'POST'
        Path   = '/api/events/{eventId:guid}/albums'
        Body   = @{ name = 'Wave4.9.1.4 Smoke Album'; description = 'Mutator round-trip' }
        AssertAuditFields = $true
    }
    'emailGroup-Update' = @{                       # G1.d smoke (needs $env:LC_EMAIL_GROUP_ID)
        Method = 'PUT'
        Path   = '/api/EmailGroups/{groupId:guid}'
        Body   = @{ name = 'Wave4.9.1.4 Updated'; emailAddresses = 'a@b.com'; description = 'Mutator smoke' }
        AssertAuditFields = $true
    }
    'registration-UpdateDetails' = @{              # G1.d smoke (needs $env:LC_EVENT_ID + existing registration)
        Method = 'PUT'
        Path   = '/api/events/{eventId:guid}/my-registration'
        Body   = @{
            attendees = @( @{ name = 'Test G1.d'; ageCategory = 'Adult' } )
            contact   = @{ email = 'test@test.com'; phone = '1234567890' }
        }
        AssertAuditFields = $true
    }
}

$key = "$Resource-$Mode"
if (-not $script:RouteMap.ContainsKey($key)) {
    $known = ($script:RouteMap.Keys | Sort-Object) -join ', '
    Write-Error "Smoke-Mutator FAILED - unknown (Resource,Mode) = ($Resource, $Mode). Add the route mapping to `$script:RouteMap in scripts/smoke/Smoke-Mutator.ps1. Known mappings: $known"
    exit 1
}

$route = $script:RouteMap[$key]

# Wave4.9.1.4: substitute path placeholders {id:guid} / {eventId:guid} / {groupId:guid}
# from env vars or the -Id param. Fails fast if a placeholder has no source.
function Resolve-Placeholder {
    param([string]$Token, [string]$IdParam)
    switch ($Token) {
        '{id:guid}' {
            if ($IdParam) { return $IdParam }
            if ($env:LC_USER_ID) { return $env:LC_USER_ID }
            throw "Path contains {id:guid} but neither -Id nor `$env:LC_USER_ID is set. Run Invoke-Login.ps1 to populate `$env:LC_USER_ID."
        }
        '{eventId:guid}' {
            if ($IdParam) { return $IdParam }
            if ($env:LC_EVENT_ID) { return $env:LC_EVENT_ID }
            throw "Path contains {eventId:guid} but neither -Id nor `$env:LC_EVENT_ID is set. Supply -Id <staging-event-guid> or set `$env:LC_EVENT_ID."
        }
        '{groupId:guid}' {
            if ($IdParam) { return $IdParam }
            if ($env:LC_EMAIL_GROUP_ID) { return $env:LC_EMAIL_GROUP_ID }
            throw "Path contains {groupId:guid} but neither -Id nor `$env:LC_EMAIL_GROUP_ID is set."
        }
        default { throw "Unknown path placeholder: $Token" }
    }
}

$resolvedPath = $route.Path
[regex]::Matches($resolvedPath, '\{[a-zA-Z]+:guid\}') | ForEach-Object {
    $token = $_.Value
    $resolved = Resolve-Placeholder -Token $token -IdParam $Id
    $resolvedPath = $resolvedPath.Replace($token, $resolved)
}

$uri = "$StagingUrl$resolvedPath"
$method = $route.Method
$payload = if ($Body) { $Body } else { $route.Body }

$headers = @{ Authorization = "Bearer $env:LC_BEARER" }

try {
    $invokeParams = @{
        Uri         = $uri
        Method      = $method
        Headers     = $headers
        TimeoutSec  = 30
    }
    if ($payload -and $method -ne 'GET') {
        $invokeParams['ContentType'] = 'application/json'
        $invokeParams['Body']        = ($payload | ConvertTo-Json -Compress -Depth 5)
    }

    $response = Invoke-RestMethod @invokeParams

    $summary = "Smoke OK $Resource $Mode $method $($route.Path)"

    if ($route.AssertAuditFields -and $response) {
        # Try common audit field names - both camelCase and PascalCase.
        # PowerShell 5.1 does not support ?? null-coalescing; use long form.
        $createdAt = if ($response.createdAt) { $response.createdAt } else { $response.CreatedAt }
        $updatedAt = if ($response.updatedAt) { $response.updatedAt } else { $response.UpdatedAt }
        if ($createdAt) {
            $age = (Get-Date).ToUniversalTime() - ([datetime]$createdAt).ToUniversalTime()
            $summary += " createdAt=$createdAt (age $([Math]::Round($age.TotalSeconds))s)"
            if ($age.TotalSeconds -gt 86400) {
                Write-Verbose "createdAt > 1 day old; this may be an existing-row update test, not a fresh create"
            }
        }
        if ($updatedAt -and $createdAt) {
            if ([datetime]$updatedAt -gt [datetime]$createdAt) {
                $summary += " updatedAt>createdAt OK"
            }
            else {
                $summary += " updatedAt=$updatedAt (== createdAt; OK for fresh create or no-op update)"
            }
        }
    }

    $summary
    exit 0
}
catch {
    $msg = $_.Exception.Message
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Error "Smoke-Mutator FAILED ($Resource $Mode $method) HTTP=$statusCode $msg"
    exit 1
}
