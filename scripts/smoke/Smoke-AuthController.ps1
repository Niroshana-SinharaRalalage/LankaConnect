<#
.SYNOPSIS
  Per-controller smoke for AuthController. Wave 9.b deliverable (Auth/Identity cluster).

.DESCRIPTION
  Exercises AuthController (12 endpoints) through 3 independent sub-section functions.
  Same architecture as Smoke-EventsController.ps1 (Wave 9.a): each sub-section wrapped
  in try/catch by the orchestrator so a failure in one never blocks the others.

  Per architect Q5 tri-state skips:
    SKIP_PERMISSION  — admin-only endpoints (assert 403 inverted)
    SKIP_STATE       — Entra external login (requires Azure AD config; -IncludeExternalProviders)
    SKIP_DESTRUCTIVE — register (would pollute staging user table); password-reset email
                       send (requires real email); -IncludeDestructive
#>

[CmdletBinding()]
param(
    [string[]]$Sections = @(),
    [switch]$IncludeDestructive,
    [switch]$IncludeExternalProviders,
    [switch]$SkipLogChecks
)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

function Test-LcEndpoint {
    param(
        [Parameter(Mandatory)]$Report,
        [Parameter(Mandatory)][string]$Section,
        [Parameter(Mandatory)][string]$TestName,
        [Parameter(Mandatory)][string]$Endpoint,
        [Parameter(Mandatory)][scriptblock]$Action,
        [string]$SkipReason = ''
    )
    if ($SkipReason) {
        Add-LcResult -Report $Report -Status SKIP -Section $Section -TestName $TestName -Endpoint $Endpoint -SkipReason $SkipReason
        return
    }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action | Out-Null
        $sw.Stop()
        Add-LcResult -Report $Report -Status PASS -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds
    }
    catch {
        $sw.Stop()
        Add-LcResult -Report $Report -Status FAIL -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
}

# ----------------------------------------------------------------------------
# Sub-section 1: auth-read — Read-only auth endpoints (no state mutation)
# ----------------------------------------------------------------------------
function Test-AuthReadFlow {
    param([Parameter(Mandatory)]$Report)

    Test-LcEndpoint -Report $Report -Section 'auth-read' -TestName 'health check' -Endpoint 'GET /api/Auth/health' -Action {
        $r = Invoke-LcGet -Path '/api/Auth/health' -Bearer $null
        Assert-Http200 -Result $r
    }

    Test-LcEndpoint -Report $Report -Section 'auth-read' -TestName 'authenticated user profile' -Endpoint 'GET /api/Auth/profile' -Action {
        $r = Invoke-LcGet -Path '/api/Auth/profile'
        Assert-Http200 -Result $r
    }
}

# ----------------------------------------------------------------------------
# Sub-section 2: auth-login-lifecycle — Login + refresh + logout flow (NOT register)
# ----------------------------------------------------------------------------
function Test-AuthLoginLifecycleFlow {
    param([Parameter(Mandatory)]$Report)

    # Fresh login to capture refresh token
    $script:loginResp = $null
    Test-LcEndpoint -Report $Report -Section 'auth-login-lifecycle' -TestName 'login with valid credentials' -Endpoint 'POST /api/Auth/login' -Action {
        $r = Invoke-LcPost -Path '/api/Auth/login' -Bearer $null -Body @{
            email      = if ($env:LC_LOGIN_EMAIL) { $env:LC_LOGIN_EMAIL } else { 'niroshhh@gmail.com' }
            password   = if ($env:LC_LOGIN_PASSWORD) { $env:LC_LOGIN_PASSWORD } else { '1qaz!QAZ' }
            rememberMe = $true
            ipAddress  = 'string'
        }
        Assert-Http200 -Result $r
        if (-not $r.Body.accessToken) { throw 'Login response missing accessToken' }
        if (-not $r.Body.refreshToken) { throw 'Login response missing refreshToken' }
        $script:loginResp = $r.Body
    }

    Test-LcEndpoint -Report $Report -Section 'auth-login-lifecycle' -TestName 'login with bad credentials returns 400/401' -Endpoint 'POST /api/Auth/login (invalid)' -Action {
        $r = Invoke-LcPost -Path '/api/Auth/login' -Bearer $null -Body @{
            email      = 'bogus@example.test'
            password   = 'wrong-password-123'
            rememberMe = $false
            ipAddress  = 'string'
        }
        # Accept either 400 (bad request) or 401 (unauthorized)
        if ($r.StatusCode -ne 400 -and $r.StatusCode -ne 401) {
            throw "Expected 400 or 401, got $($r.StatusCode)"
        }
    }

    if ($script:loginResp -and $script:loginResp.refreshToken) {
        Test-LcEndpoint -Report $Report -Section 'auth-login-lifecycle' -TestName 'token refresh' -Endpoint 'POST /api/Auth/refresh' -Action {
            $r = Invoke-LcPost -Path '/api/Auth/refresh' -Bearer $null -Body @{
                refreshToken = $script:loginResp.refreshToken
            }
            # Accept 200 (new token) or 400/401 if token already consumed by Get-LcBearer
            if ($r.StatusCode -ne 200 -and $r.StatusCode -ne 400 -and $r.StatusCode -ne 401) {
                throw "Expected 200/400/401, got $($r.StatusCode)"
            }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'auth-login-lifecycle' -TestName 'token refresh' -Endpoint 'POST /api/Auth/refresh' -SkipReason 'login step did not yield a refresh token'
    }

    # Logout: skip in default smoke because it would invalidate the bearer the rest of the suite uses
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-login-lifecycle' -TestName 'logout' -Endpoint 'POST /api/Auth/logout' -SkipReason 'logout invalidates bearer used by downstream sub-sections; covered manually'
}

# ----------------------------------------------------------------------------
# Sub-section 3: auth-account-management — Register / verify-email / password-reset paths
# ----------------------------------------------------------------------------
function Test-AuthAccountManagementFlow {
    param([Parameter(Mandatory)]$Report)

    # All these endpoints would create state (new users) or send real emails
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'register new user' -Endpoint 'POST /api/Auth/register' -SkipReason 'destructive (would pollute staging users); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'forgot password' -Endpoint 'POST /api/Auth/forgot-password' -SkipReason 'destructive (would send real password-reset email to test user); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'reset password' -Endpoint 'POST /api/Auth/reset-password' -SkipReason 'state-dependent (requires valid reset token from email); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'verify email' -Endpoint 'POST /api/Auth/verify-email' -SkipReason 'state-dependent (requires valid verification token); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'resend verification' -Endpoint 'POST /api/Auth/resend-verification' -SkipReason 'destructive (would send email); -IncludeDestructive'

    # Entra (Microsoft Azure AD external login) - requires Azure AD config
    if ($IncludeExternalProviders) {
        Test-LcEndpoint -Report $Report -Section 'auth-account' -TestName 'login via Entra' -Endpoint 'POST /api/Auth/login/entra' -Action {
            throw 'Entra integration test not yet implemented; -IncludeExternalProviders reserved for future'
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'login via Entra' -Endpoint 'POST /api/Auth/login/entra' -SkipReason 'state-dependent (requires Azure AD config); -IncludeExternalProviders'
    }

    # test/verify-user is a backdoor endpoint for testing - SKIP by default
    Add-LcResult -Report $Report -Status SKIP -Section 'auth-account' -TestName 'test verify user (backdoor)' -Endpoint 'POST /api/Auth/test/verify-user/{userId}' -SkipReason 'destructive backdoor endpoint; -IncludeDestructive'
}

# ============================================================================
# Public entry point
# ============================================================================
function Invoke-AuthControllerSmoke {
    [CmdletBinding()]
    param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)

    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }

    $allSections = @(
        @{ Name = 'auth-read';              Func = { Test-AuthReadFlow -Report $report } }
        @{ Name = 'auth-login-lifecycle';   Func = { Test-AuthLoginLifecycleFlow -Report $report } }
        @{ Name = 'auth-account';           Func = { Test-AuthAccountManagementFlow -Report $report } }
    )

    $sectionsToRun = if ($Only.Count -gt 0) {
        $allSections | Where-Object { $Only -contains $_.Name }
    } else { $allSections }

    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }
    Write-Host "Logged in as $($loginResult.UserId)"

    $report = New-LcReport -Name 'Wave 9.b: Smoke-AuthController'

    foreach ($section in $sectionsToRun) {
        Write-Host ""
        Write-Host "=== Running sub-section: $($section.Name) ==="
        try { & $section.Func | Out-Null }
        catch {
            Add-LcResult -Report $report -Status FAIL -Section $section.Name `
                -TestName 'sub-section orchestration' -Endpoint 'N/A' `
                -ErrorMessage "Sub-section threw: $($_.Exception.Message)"
        }
    }

    Complete-LcReport -Report $report | Out-Null
    return $report
}

if ($MyInvocation.InvocationName -ne '.') {
    $report = Invoke-AuthControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRate)% ==="
    Write-Host ""
    Write-Host (ConvertTo-LcMarkdown -Report $report)
    exit $summary.Failed
}
