<#
.SYNOPSIS
    Authenticate against the LankaConnect staging API. Exports $env:LC_BEARER
    + $env:LC_USER_ID for subsequent smoke scripts.

.DESCRIPTION
    Foundation script for every smoke flow per CLAUDE.md §13.4 step 2.
    Reads credentials from env vars (with sensible staging defaults) so the
    script never carries secrets in source.

.PARAMETER Email
    Override env var $env:LC_LOGIN_EMAIL. Default: $env:LC_LOGIN_EMAIL or
    'niroshhh@gmail.com' (per [[reference-staging-credentials]] memory).

.PARAMETER Password
    Override env var $env:LC_LOGIN_PASSWORD. Default: $env:LC_LOGIN_PASSWORD
    or '1qaz!QAZ'.

.PARAMETER StagingUrl
    Override env var $env:LC_STAGING_URL. Default points at the staging
    Container App.

.OUTPUTS
    Exit 0 + one-line summary on success: "Login OK <email> userId=<guid>".
    Exit 1 + diagnostic on failure.

.NOTES
    Built as part of Gap G0 (2026-06-08).
#>
[CmdletBinding()]
param(
    [string]$Email      = $(if ($env:LC_LOGIN_EMAIL) { $env:LC_LOGIN_EMAIL } else { 'niroshhh@gmail.com' }),
    [string]$Password   = $(if ($env:LC_LOGIN_PASSWORD) { $env:LC_LOGIN_PASSWORD } else { '1qaz!QAZ' }),
    [string]$StagingUrl = $(if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' })
)

$ErrorActionPreference = 'Stop'

try {
    $body = @{
        email      = $Email
        password   = $Password
        rememberMe = $true
        ipAddress  = '127.0.0.1'
    } | ConvertTo-Json -Compress

    $resp = Invoke-RestMethod `
        -Uri "$StagingUrl/api/Auth/login" `
        -Method POST `
        -ContentType 'application/json' `
        -Body $body `
        -TimeoutSec 30

    if (-not $resp.accessToken) {
        throw 'Login response missing accessToken'
    }
    if (-not $resp.user.userId) {
        throw 'Login response missing user.userId'
    }

    $env:LC_BEARER       = $resp.accessToken
    $env:LC_USER_ID      = $resp.user.userId
    $env:LC_USER_EMAIL   = $resp.user.email
    $env:LC_USER_ROLE    = $resp.user.role

    "Login OK $($resp.user.email) userId=$($resp.user.userId) role=$($resp.user.role)"
    exit 0
}
catch {
    Write-Error "Invoke-Login FAILED: $($_.Exception.Message)"
    exit 1
}
