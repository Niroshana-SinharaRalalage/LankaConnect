<#
.SYNOPSIS
  Identity fixture builders for Wave 9.h: throwaway test users + Newsletter test mailbox.

.DESCRIPTION
  Per Wave 9.h architect ruling:
  - Throwaway test users for admin destructive smokes (lock/deactivate/downgrade)
    use create-mutate-delete pattern (NOT a rotation pool)
  - All tagged with current run tag for grep-able cleanup

.NOTES
  Throwaway user creation hits /api/Auth/register (a SKIP_DESTRUCTIVE in Wave 9.a-g).
  The teardown deletes via /api/admin/users/{id} which requires admin privilege --
  if the smoke test user is NOT admin (per Wave 9.b finding), throwaway-user
  destructive smokes are inherently SKIPped until founder grants admin role to
  the staging smoke user OR a dedicated admin-bearer flow is wired up.
#>

$script:LcCachedMetroAreaId = $null

function Get-LcAnyMetroAreaId {
    <#
    .SYNOPSIS
      Returns a valid metro-area id from staging. Cached per-session because
      register/user-preferences smoke calls it repeatedly.

    .DESCRIPTION
      Wave 9.h.10.2b (2026-07-01): Auth register now REQUIRES preferredMetroAreaIds
      (min 1). Wave 9.h.9 smoke silently sent register requests without it and got
      400 --- but the smoke assertion only tripped on >= 500 so registration was
      silently failing and every downstream throwaway-user flow was targeting the
      admin user's own id (then rejected by "cannot lock own account"), so no
      admin-lifecycle emails ever fired.
    #>
    if ($script:LcCachedMetroAreaId) { return $script:LcCachedMetroAreaId }
    $r = Invoke-LcGet -Path '/api/metro-areas'
    if (-not $r.Success) { throw "Get-LcAnyMetroAreaId: /api/metro-areas returned HTTP $($r.StatusCode)" }
    $first = @($r.Body)[0]
    if (-not $first -or -not $first.id) { throw "Get-LcAnyMetroAreaId: no metro areas returned from staging" }
    $script:LcCachedMetroAreaId = $first.id
    return $script:LcCachedMetroAreaId
}

function New-LcTaggedThrowawayUser {
    [CmdletBinding()]
    param(
        # Wave 9.h.10.2b: Get-LcCurrentRunTag lives in Lc-EventFixtures which
        # AdminUsers/Auth smokes don't import (deliberately, per architect Q1
        # ruling that AdminUsers shouldn't pull the fat Events module). Fall
        # back to a locally-generated tag when the helper isn't loaded.
        [string]$Tag = $(if (Get-Command Get-LcCurrentRunTag -ErrorAction SilentlyContinue) { Get-LcCurrentRunTag } else { "[SMOKE-9h10-$(Get-Date -Format yyyyMMddHHmmss)]" }),
        [string]$Role = 'EventAttendee'
    )
    $shortTag = $Tag.Trim('[]').Replace('SMOKE-', '').Replace('-', '').Substring(0, [Math]::Min(8, $Tag.Length))
    # Wave 9.h.10.2: throwaway users route through founder Gmail alias so
    # admin-triggered lifecycle emails (Locked/Unlocked/Activated/Deactivated,
    # plus registration confirmation) actually deliver during smoke runs.
    $email = Get-LcFixtureEmail -Slug 'throwaway-user' -Suffix "$shortTag-$(Get-Random -Maximum 9999)"
    $metroId = Get-LcAnyMetroAreaId
    $body = @{
        firstName             = "$Tag Throwaway"
        lastName              = 'User'
        email                 = $email
        password              = 'Throwaway1!Qz'
        confirmPassword       = 'Throwaway1!Qz'
        acceptTerms           = $true
        preferredMetroAreaIds = @($metroId)
    }
    $r = Invoke-LcPost -Path '/api/Auth/register' -Bearer $null -Body $body
    # Wave 9.h.10.2b: hard-assert register succeeded. The old <500 check let 400s
    # pass silently and hid the missing preferredMetroAreaIds requirement.
    if (-not $r.Success -and $r.StatusCode -ne 201) {
        return [pscustomobject]@{
            Success = $false; StatusCode = $r.StatusCode; Body = $r.Body; Email = $email; UserId = $null; Tag = $Tag
            Error   = "register FAILED HTTP $($r.StatusCode): $($r.Error)"
        }
    }
    $userId = if ($r.Body.userId) { $r.Body.userId } elseif ($r.Body.id) { $r.Body.id } else { $null }
    return [pscustomobject]@{
        Success    = $true
        StatusCode = $r.StatusCode
        Body       = $r.Body
        Email      = $email
        UserId     = $userId
        Tag        = $Tag
        Error      = $null
    }
}

function Remove-LcThrowawayUserByEmail {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Email)
    # Requires admin role; staging smoke user is NOT admin per Wave 9.b finding.
    # This will return 403 if smoke user is non-admin; that's expected. Caller
    # decides whether to fail the cleanup or accept it.
    # First find user by email via admin endpoint
    $search = Invoke-LcGet -Path "/api/admin/users?search=$([uri]::EscapeDataString($Email))"
    if (-not $search.Success) {
        return [pscustomobject]@{ Found = $false; Deleted = $false; Email = $Email; Error = "search HTTP $($search.StatusCode)" }
    }
    $items = if ($search.Body.items) { $search.Body.items } else { $search.Body }
    $user = @($items | Where-Object { $_.email -eq $Email }) | Select-Object -First 1
    if (-not $user) {
        return [pscustomobject]@{ Found = $false; Deleted = $false; Email = $Email; Error = 'not found' }
    }
    # Soft-deactivate (no hard-delete endpoint exists for users)
    $d = Invoke-LcPost -Path "/api/admin/users/$($user.id)/deactivate" -Body @{}
    return [pscustomobject]@{
        Found   = $true
        Deleted = ($d.Success -or $d.StatusCode -eq 204)
        Email   = $Email
        Error   = if ($d.Success) { $null } else { "deactivate HTTP $($d.StatusCode)" }
    }
}

Export-ModuleMember -Function `
    New-LcTaggedThrowawayUser, Remove-LcThrowawayUserByEmail, Get-LcAnyMetroAreaId
