<#
.SYNOPSIS
    Wave4.9.1.5 (2026-06-08): probe per-module DbContext + operational
    table reachability on staging WITHOUT requiring psql or direct DB
    access.

.DESCRIPTION
    Each module-scoped DbContext (NotificationsDbContext, MediaDbContext,
    FormsDbContext) has a public API endpoint that exercises the
    primary operational table of that context. Hitting the endpoint
    with HTTP 200 + non-error response proves three things:

      1. The schema exists on staging
      2. The primary table exists in that schema
      3. The DbContext can SELECT from it (no Wave3 IAuditable-leak 42703s)
      4. The dependency-injection chain wires the context to that schema

    This is the S6 smoke class from CLAUDE.md §13.2 - module-DbContext
    touch verification - without the psql installation requirement.

.PARAMETER StagingUrl
    Default env $env:LC_STAGING_URL or hardcoded.

.OUTPUTS
    Exit 0 + per-module summary on green.
    Exit 1 + per-module diagnostic on first red.

.NOTES
    Wave4.9.1.5 (2026-06-08). Per architect P5 ruling: when direct DB
    probe infrastructure (psql / Key Vault on PS 5.1 / pwsh 7) is not
    available, fall back to API-endpoint exercise. This catches the same
    class of bug (schema missing, IAuditable column leak) because the
    DbContext SELECTs against the physical schema.
#>
[CmdletBinding()]
param(
    [string]$StagingUrl = $(if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' })
)

$ErrorActionPreference = 'Stop'

if (-not $env:LC_BEARER) {
    Write-Error 'Probe-OperationalTables requires $env:LC_BEARER - call Invoke-Login.ps1 first.'
    exit 1
}

$probes = @(
    @{
        Module = 'notifications'
        Context = 'NotificationsDbContext'
        Endpoint = '/api/notifications/unread'
        ProofProperty = $null   # 200 alone is sufficient; could be empty array
    }
    @{
        Module = 'media'
        Context = 'MediaDbContext'
        Endpoint = '/api/events/{eventId}/albums'   # Wave4.9.1.4.b - needs eventId fixture
        ProofProperty = $null
        Skip = $true
        SkipReason = 'Endpoint requires eventId path parameter; deferred to Wave4.9.1.4.b fixture orchestration'
    }
    @{
        Module = 'forms'
        Context = 'FormsDbContext'
        Endpoint = '/api/events/{eventId}/forms'   # also event-scoped; same Skip
        ProofProperty = $null
        Skip = $true
        SkipReason = 'Endpoint requires eventId path parameter; deferred to Wave4.9.1.4.b fixture orchestration'
    }
)

$headers = @{ Authorization = "Bearer $env:LC_BEARER" }
$results = @()
$anyFail = $false

foreach ($probe in $probes) {
    if ($probe.Skip) {
        $results += "  [SKIP] $($probe.Module) ($($probe.Context)) - $($probe.SkipReason)"
        continue
    }

    $uri = "$StagingUrl$($probe.Endpoint)"
    try {
        $r = Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing -TimeoutSec 30
        if ($r.StatusCode -eq 200) {
            $contentLen = $r.Content.Length
            $results += "  [OK]   $($probe.Module) ($($probe.Context)) GET $($probe.Endpoint) -> 200 ($contentLen bytes)"
        }
        else {
            $results += "  [FAIL] $($probe.Module) ($($probe.Context)) GET $($probe.Endpoint) -> HTTP $($r.StatusCode)"
            $anyFail = $true
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $msg = $_.Exception.Message
        $results += "  [FAIL] $($probe.Module) ($($probe.Context)) GET $($probe.Endpoint) -> HTTP $statusCode ($msg)"
        $anyFail = $true
    }
}

Write-Host ''
Write-Host 'Probe-OperationalTables (Wave4.9.1.5) - per-module DbContext probe via API exercise:'
Write-Host '----------------------------------------------------------------------------------------'
$results | ForEach-Object { Write-Host $_ }
Write-Host ''

if ($anyFail) {
    Write-Error 'Probe-OperationalTables FAILED - one or more module-context probes did not return 200. See lines above. A FAIL typically means a missing schema/table OR an EF Core configuration leak (e.g., the IgnoreAuditByActorPropertiesUntilPhase1 hotfix was lost), producing PostgreSQL 42703 errors when the module DbContext SELECTs against the schema.'
    exit 1
}

Write-Host 'Probe-OperationalTables OK (Wave4.9.1.5).' -ForegroundColor Green
exit 0
