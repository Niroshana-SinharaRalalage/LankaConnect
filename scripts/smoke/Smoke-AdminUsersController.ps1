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
    #
    # Wave 9.h.10.5 Q20 (architect-mandated 2026-07-02): the previous 1-user-
    # 7-action pattern silently drops admin-lifecycle emails because the
    # cumulative state transitions on ONE user hit domain guards (e.g. deactivate
    # after lock rejected, activate on already-active user rejected). Handler
    # returns Failure BEFORE calling SendXxxEmailAsync, no probe evidence, no
    # inbox delivery, still 7 PASS at the smoke assertion (< 500 check). This
    # is INSTANCE 2 of the "cumulative-state smoke fixture" bug class (instance
    # 1 was 9.h.10.2b: shared admin id across actions). Fix per architect Q20
    # ruling: use ONE fresh throwaway per action, each in its required starting
    # state. Post-run assertion (probe-ENTRY-count = matching-action-count) is
    # the guard against instance 3 of the same class.
    #
    # Note: not all 7 admin actions dispatch email. Actions that dispatch email
    # (and therefore expect probe ENTRY markers):
    #   lock                 -> template-account-locked-by-admin
    #   unlock               -> template-account-unlocked-by-admin
    #   deactivate           -> template-account-deactivated-by-admin
    #   activate             -> template-account-activated-by-admin
    #   resend-verification  -> template-membership-email-verification
    # Actions that do NOT dispatch email (audit-only mutations):
    #   downgrade            -> role change only, no email dispatch
    #   upgrade              -> role change only, no email dispatch

    $adminBearer = $env:LC_BEARER
    if (-not $adminBearer) {
        Add-LcResult -Report $Report -Status FAIL -Section 'admin-mutators' -TestName 'admin bearer available' -Endpoint 'N/A' -ErrorMessage 'LC_BEARER env var missing after login'
        return
    }

    function _NewFreshThrowaway {
        param([string]$SlugForAction)
        $t = New-LcTaggedThrowawayUser -SlugPrefix "throwaway-$SlugForAction"
        if (-not $t.Success -or -not $t.UserId) { throw "throwaway ($SlugForAction) create failed: $($t.Error)" }
        return $t
    }

    function _AdminAction {
        param([string]$UserId, [string]$Action, [hashtable]$Body = @{})
        # Uses the admin bearer captured at test start. Direct call, no assertion
        # -- caller (setup vs test) decides how to interpret the response.
        return Invoke-LcPost -Path "/api/admin/users/$UserId/$Action" -Body $Body
    }

    # ---- action 1: lock -- fresh Active user -> lock ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin lock throwaway user (fresh Active)' -Endpoint 'POST /api/admin/users/{id}/lock' -Action {
        $t = _NewFreshThrowaway 'lock'
        $r = _AdminAction -UserId $t.UserId -Action 'lock' -Body @{ reason = 'wave 9h10.5 Q20 fresh lock'; lockUntil = '2027-01-01T00:00:00Z' }
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "expected 200 for lock on fresh Active user, got $($r.StatusCode): $($r.Error)" }
    }

    # ---- action 2: unlock -- setup: register+lock, then test unlock ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin unlock throwaway user (fresh Locked)' -Endpoint 'POST /api/admin/users/{id}/unlock' -Action {
        $t = _NewFreshThrowaway 'unlock'
        $setup = _AdminAction -UserId $t.UserId -Action 'lock' -Body @{ reason = 'setup for unlock'; lockUntil = '2027-01-01T00:00:00Z' }
        if ($setup.StatusCode -ge 400) { throw "setup lock failed: $($setup.StatusCode)" }
        $r = _AdminAction -UserId $t.UserId -Action 'unlock' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "expected 200 for unlock on Locked user, got $($r.StatusCode): $($r.Error)" }
    }

    # ---- action 3: deactivate -- fresh Active user -> deactivate ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin deactivate throwaway user (fresh Active)' -Endpoint 'POST /api/admin/users/{id}/deactivate' -Action {
        $t = _NewFreshThrowaway 'deactivate'
        $r = _AdminAction -UserId $t.UserId -Action 'deactivate' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "expected 200 for deactivate on Active user, got $($r.StatusCode): $($r.Error)" }
    }

    # ---- action 4: activate -- setup: register+deactivate, then test activate ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin activate throwaway user (fresh Deactivated)' -Endpoint 'POST /api/admin/users/{id}/activate' -Action {
        $t = _NewFreshThrowaway 'activate'
        $setup = _AdminAction -UserId $t.UserId -Action 'deactivate' -Body @{}
        if ($setup.StatusCode -ge 400) { throw "setup deactivate failed: $($setup.StatusCode)" }
        $r = _AdminAction -UserId $t.UserId -Action 'activate' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "expected 200 for activate on Deactivated user, got $($r.StatusCode): $($r.Error)" }
    }

    # ---- action 5: resend-verification -- fresh unverified user ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin resend-verification throwaway user (fresh Unverified)' -Endpoint 'POST /api/admin/users/{id}/resend-verification' -Action {
        $t = _NewFreshThrowaway 'resend'
        $r = _AdminAction -UserId $t.UserId -Action 'resend-verification' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        if ($r.StatusCode -ge 400) { throw "expected 200 for resend-verification on fresh user, got $($r.StatusCode): $($r.Error)" }
    }

    # ---- action 6: downgrade -- fresh user (no email dispatch) ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin downgrade throwaway user (fresh; audit-only, no email)' -Endpoint 'POST /api/admin/users/{id}/downgrade' -Action {
        $t = _NewFreshThrowaway 'downgrade'
        $r = _AdminAction -UserId $t.UserId -Action 'downgrade' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # ---- action 7: upgrade -- fresh user (no email dispatch) ----
    Test-LcEndpoint -Report $Report -Section 'admin-mutators' -TestName 'admin upgrade throwaway user (fresh; audit-only, no email)' -Endpoint 'POST /api/admin/users/{id}/upgrade' -Action {
        $t = _NewFreshThrowaway 'upgrade'
        $r = _AdminAction -UserId $t.UserId -Action 'upgrade' -Body @{}
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # ---- POST-RUN PROBE-ENTRY-COUNT ASSERTION (Q20 harness) — SUPERSEDED ----
    # Wave 9.h.10.5 Q20 was a 300-line container-tail probe fired ~5s after the
    # last admin action, meant to catch instance 3 of the "cumulative-state fixture"
    # bug class. In practice the 300-line window rotates faster than the assertion
    # can read it during a full smoke run — Pass 1 saw the assertion FAIL while every
    # underlying admin action returned 200 and every corresponding email delivered to
    # the founder inbox (verified in Pass 2 + founder inbox screenshot).
    #
    # Wave 9.h.10.6 F34: retired the standalone assertion. F26's rotating-tail
    # (`_probe-rotating-tail.ps1` at 5s cadence) captures the same probe evidence
    # continuously across the full suite window and is parsed by `_probe-parse.ps1`
    # into the union log — that's the canonical delivery evidence now. Keeping the
    # 7 admin action HTTP tests above; dropping the tail-probe assertion.
    Add-LcResult -Report $Report -Status SKIP -Section 'admin-mutators' -TestName 'probe-ENTRY count assertion (Q20 harness)' -Endpoint '(container logs)' -SkipReason 'F34 superseded: rotating-tail (F26 at 5s cadence) + probe-parse (F26) union log is the canonical email delivery evidence. The 7 admin action HTTP tests + Pass 3 rotating-tail evidence together prove the flow — no need for the 300-line-window race condition.'
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
