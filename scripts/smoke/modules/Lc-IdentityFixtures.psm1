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

function New-LcTaggedThrowawayUser {
    [CmdletBinding()]
    param(
        [string]$Tag = $(Get-LcCurrentRunTag),
        [string]$Role = 'EventAttendee'
    )
    $shortTag = $Tag.Trim('[]').Replace('SMOKE-', '').Replace('-', '').Substring(0, [Math]::Min(8, $Tag.Length))
    $email = "smoke-throwaway-$shortTag-$(Get-Random -Maximum 9999)@lankaconnect.test"
    $body = @{
        firstName        = "$Tag Throwaway"
        lastName         = 'User'
        email            = $email
        password         = 'Throwaway1!Qz'
        confirmPassword  = 'Throwaway1!Qz'
        acceptTerms      = $true
    }
    $r = Invoke-LcPost -Path '/api/Auth/register' -Bearer $null -Body $body
    return [pscustomobject]@{
        Success    = $r.Success
        StatusCode = $r.StatusCode
        Body       = $r.Body
        Email      = $email
        Tag        = $Tag
        Error      = if ($r.Success) { $null } else { "HTTP $($r.StatusCode): $($r.Error)" }
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
    New-LcTaggedThrowawayUser, Remove-LcThrowawayUserByEmail
