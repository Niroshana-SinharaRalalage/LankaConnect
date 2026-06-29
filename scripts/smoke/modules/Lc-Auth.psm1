<#
.SYNOPSIS
  LankaConnect smoke authentication module. Foundation for Wave 9 API Smoke Suite.

.DESCRIPTION
  Login + token caching + automatic refresh on expiry. Wraps the existing
  Invoke-Login.ps1 flow but exposes a cleaner module API for use by every
  per-controller smoke script.

  Exposes:
   - Get-LcBearer  — returns cached bearer token; refreshes if missing or near expiry
   - Get-LcUserId  — returns the logged-in user's userId (from login response)
   - Invoke-LcLogin — explicit login (refreshes cache; call to rotate test users)
   - Clear-LcAuthCache — clears the cache (useful for tests)

.NOTES
  Wave 9.a Foundation module (architect-ruled 2026-06-29 Q1).
  Depends on Lc-Http.psm1.
#>

# Token cache is module-private; survives across calls within a single PowerShell session.
$script:LcAuthCache = @{
    Bearer      = $null
    UserId      = $null
    ExpiresAt   = [datetime]::MinValue
    Email       = $null
}

# Refresh tokens this many seconds before their actual expiry (avoid edge-case expiry mid-call)
$script:LcAuthRefreshGraceSeconds = 60

function Clear-LcAuthCache {
    [CmdletBinding()] param()
    $script:LcAuthCache = @{
        Bearer    = $null
        UserId    = $null
        ExpiresAt = [datetime]::MinValue
        Email     = $null
    }
}

function Invoke-LcLogin {
    <#
    .SYNOPSIS
      Performs a login against staging. Populates the module cache and returns the auth result.

    .PARAMETER Email
      Login email. Default: $env:LC_LOGIN_EMAIL or 'niroshhh@gmail.com' (the canonical staging test user).

    .PARAMETER Password
      Login password. Default: $env:LC_LOGIN_PASSWORD or '1qaz!QAZ'.

    .PARAMETER RememberMe
      Whether to request long-lived refresh token. Default $true.

    .OUTPUTS
      pscustomobject { Success, UserId, Bearer, ExpiresAt, Error }
    #>
    [CmdletBinding()]
    param(
        [string]$Email = $(if ($env:LC_LOGIN_EMAIL) { $env:LC_LOGIN_EMAIL } else { 'niroshhh@gmail.com' }),
        [string]$Password = $(if ($env:LC_LOGIN_PASSWORD) { $env:LC_LOGIN_PASSWORD } else { '1qaz!QAZ' }),
        [bool]$RememberMe = $true
    )

    $loginBody = @{
        email      = $Email
        password   = $Password
        rememberMe = $RememberMe
        ipAddress  = 'string'
    }

    # Direct call — don't use Invoke-LcPost because that injects the bearer we're trying to get
    $result = Invoke-LcRequest -Method POST -Path '/api/Auth/login' -Body $loginBody -Bearer $null

    if (-not $result.Success) {
        return [pscustomobject]@{
            Success = $false
            UserId  = $null
            Bearer  = $null
            Error   = "Login failed: HTTP $($result.StatusCode). $($result.Error)"
        }
    }

    $body = $result.Body
    if (-not $body.accessToken) {
        return [pscustomobject]@{
            Success = $false
            UserId  = $null
            Bearer  = $null
            Error   = 'Login response missing accessToken field'
        }
    }

    # Parse expiresAt - login response shape: { user, accessToken, refreshToken, tokenExpiresAt }
    $expiresAt = if ($body.tokenExpiresAt) {
        [datetime]::Parse($body.tokenExpiresAt).ToUniversalTime()
    } else {
        # Fallback: 30 minutes (typical staging JWT lifetime)
        [datetime]::UtcNow.AddMinutes(30)
    }

    $userId = if ($body.user -and $body.user.userId) { $body.user.userId } else { $null }

    $script:LcAuthCache = @{
        Bearer    = $body.accessToken
        UserId    = $userId
        ExpiresAt = $expiresAt
        Email     = $Email
    }

    # Also export as env var so legacy scripts continue to work
    $env:LC_BEARER = $body.accessToken
    $env:LC_USER_ID = $userId

    return [pscustomobject]@{
        Success   = $true
        UserId    = $userId
        Bearer    = $body.accessToken
        ExpiresAt = $expiresAt
        Error     = $null
    }
}

function Test-LcBearerExpired {
    [CmdletBinding()] param()
    if (-not $script:LcAuthCache.Bearer) { return $true }
    $graceCutoff = [datetime]::UtcNow.AddSeconds($script:LcAuthRefreshGraceSeconds)
    return $script:LcAuthCache.ExpiresAt -le $graceCutoff
}

function Get-LcBearer {
    <#
    .SYNOPSIS
      Returns a valid bearer token. Logs in / refreshes if cache is empty or near expiry.
    #>
    [CmdletBinding()] param()

    if (Test-LcBearerExpired) {
        $loginResult = Invoke-LcLogin
        if (-not $loginResult.Success) {
            throw "Cannot obtain bearer token: $($loginResult.Error)"
        }
    }

    return $script:LcAuthCache.Bearer
}

function Get-LcUserId {
    [CmdletBinding()] param()
    if (-not $script:LcAuthCache.UserId) {
        # Force a login to populate
        Get-LcBearer | Out-Null
    }
    return $script:LcAuthCache.UserId
}

function Get-LcAuthCache {
    # Test-only helper. Returns a copy of the cache for inspection.
    [CmdletBinding()] param()
    return [pscustomobject]@{
        Bearer    = $script:LcAuthCache.Bearer
        UserId    = $script:LcAuthCache.UserId
        ExpiresAt = $script:LcAuthCache.ExpiresAt
        Email     = $script:LcAuthCache.Email
    }
}

Export-ModuleMember -Function `
    Invoke-LcLogin, `
    Get-LcBearer, Get-LcUserId, `
    Test-LcBearerExpired, Clear-LcAuthCache, Get-LcAuthCache
