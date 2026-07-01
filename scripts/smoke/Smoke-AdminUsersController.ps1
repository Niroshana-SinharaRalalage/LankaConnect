<#
.SYNOPSIS
  Per-controller smoke for AdminUsersController. Wave 9.b deliverable (Auth/Identity cluster).

.DESCRIPTION
  AdminUsersController has 10 endpoints, all requiring global Admin role.
  The test user (niroshhh@gmail.com) is EventOrganizer NOT global admin.

  Per architect Q5 SKIP_PERMISSION pattern: smoke STILL hits these endpoints +
  asserts 403 (NOT skipped) — verifies authorization wiring catches non-admin access.
  This is the "inverted assertion" pattern.
#>

[CmdletBinding()]
param(
    [string[]]$Sections = @(),
    [switch]$IncludeDestructive,
    [switch]$SkipLogChecks
)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force  # Wave 9.h.10.2: Get-LcFixtureEmail
Import-Module (Join-Path $moduleDir 'Lc-IdentityFixtures.psm1') -Force  # Wave 9.h.10.2b: New-LcTaggedThrowawayUser + Get-LcAnyMetroAreaId

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
    } catch {
        $sw.Stop()
        Add-LcResult -Report $Report -Status FAIL -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
}

# ----------------------------------------------------------------------------
# Sub-section: admin-reads — niroshhh@gmail.com is AdminManager (highest role).
# Assert 200 OK on all admin reads.
# ----------------------------------------------------------------------------
function Test-AdminUsersReadFlow {
    param([Parameter(Mandatory)]$Report)

    $userId = Get-LcUserId

    Test-LcEndpoint -Report $Report -Section 'admin-reads' -TestName 'list admin users (paginated)' -Endpoint 'GET /api/admin/users' -Action {
        $r = Invoke-LcGet -Path '/api/admin/users?pageNumber=1&pageSize=5'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'admin-reads' -TestName 'admin user detail (self lookup)' -Endpoint 'GET /api/admin/users/{id}' -Action {
        $r = Invoke-LcGet -Path "/api/admin/users/$userId"
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'admin-reads' -TestName 'admin statistics' -Endpoint 'GET /api/admin/users/statistics' -Action {
        $r = Invoke-LcGet -Path '/api/admin/users/statistics'
        Assert-Http200 -Result $r
    }
}

# ----------------------------------------------------------------------------
# Sub-section: admin-mutators — niroshhh is AdminManager. Use throwaway-user
# pattern: create test user via /api/Auth/register, exercise mutators on THAT
# user (not on self -- avoids breaking the suite bearer), then leave the
# throwaway user deactivated.
# ----------------------------------------------------------------------------
function Test-AdminUsersMutatorFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.10.2b: use the shared New-LcTaggedThrowawayUser fixture which
    # (a) routes to founder Gmail alias for admin-lifecycle email delivery, and
    # (b) includes preferredMetroAreaIds --- register now requires min 1, and
    # the old inline body omitted it so register was silently 400ing and every
    # admin lifecycle action targeted the admin's own id (rejected as "cannot
    # lock own account"), causing 0 lifecycle emails to fire despite 7 PASS.
    $throwaway = New-LcTaggedThrowawayUser
    if (-not $throwaway.Success -or -not $throwaway.UserId) {
        Add-LcResult -Report $Report -Status FAIL -Section 'admin-mutators' -TestName 'throwaway user setup' -Endpoint 'POST /api/Auth/register' -ErrorMessage "throwaway create failed: $($throwaway.Error)"
        return
    }
    $script:throwawayEmail = $throwaway.Email
    $throwawayUserId = $throwaway.UserId

    foreach ($action in 'lock', 'unlock', 'deactivate', 'activate', 'resend-verification', 'downgrade', 'upgrade') {
        Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName "admin $action throwaway user" -Endpoint "POST /api/admin/users/{id}/$action" -Action {
            $r = Invoke-LcPost -Path "/api/admin/users/$throwawayUserId/$action" -Body @{}
            if ($r.StatusCode -ge 500) {
                throw "5xx response: $($r.StatusCode)"
            }
        }
    }
}

# ============================================================================
# Public entry point
# ============================================================================
function Invoke-AdminUsersControllerSmoke {
    [CmdletBinding()]
    param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)

    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }

    $allSections = @(
        @{ Name = 'admin-reads';     Func = { Test-AdminUsersReadFlow -Report $report } }
        @{ Name = 'admin-mutators';  Func = { Test-AdminUsersMutatorFlow -Report $report } }
    )

    $sectionsToRun = if ($Only.Count -gt 0) {
        $allSections | Where-Object { $Only -contains $_.Name }
    } else { $allSections }

    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }
    Write-Host "Logged in as $($loginResult.UserId)"

    $report = New-LcReport -Name 'Wave 9.b: Smoke-AdminUsersController'

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
    $report = Invoke-AdminUsersControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRate)% ==="
    Write-Host ""
    Write-Host (ConvertTo-LcMarkdown -Report $report)
    exit $summary.Failed
}
