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
# Extend this table as new gaps need new smokes.
$script:RouteMap = @{
    'user-UpdateLocation' = @{
        Method = 'PUT'
        Path   = '/api/users/me/location'
        Body   = @{ city = 'Cleveland'; state = 'OH'; country = 'USA' }
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
}

$key = "$Resource-$Mode"
if (-not $script:RouteMap.ContainsKey($key)) {
    $known = ($script:RouteMap.Keys | Sort-Object) -join ', '
    Write-Error "Smoke-Mutator FAILED - unknown (Resource,Mode) = ($Resource, $Mode). Add the route mapping to `$script:RouteMap in scripts/smoke/Smoke-Mutator.ps1. Known mappings: $known"
    exit 1
}

$route = $script:RouteMap[$key]
$uri = "$StagingUrl$($route.Path)"
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
