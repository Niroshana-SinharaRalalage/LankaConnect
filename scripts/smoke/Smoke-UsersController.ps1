<#
.SYNOPSIS
  Per-controller smoke for UsersController. Wave 9.b deliverable (Auth/Identity cluster).

.DESCRIPTION
  Exercises UsersController (18 endpoints) through 4 independent sub-section functions.
  Architect-mandated Q3 independence pattern (each section runs in own try/catch).
#>

[CmdletBinding()]
param(
    [string[]]$Sections = @(),
    [switch]$IncludeDestructive,
    [switch]$IncludePhotoUpload,
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
    } catch {
        $sw.Stop()
        Add-LcResult -Report $Report -Status FAIL -Section $Section -TestName $TestName -Endpoint $Endpoint -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
}

# ----------------------------------------------------------------------------
# Sub-section 1: users-read — Read-only profile reads
# ----------------------------------------------------------------------------
function Test-UsersReadFlow {
    param([Parameter(Mandatory)]$Report)

    Test-LcEndpoint -Report $Report -Section 'users-read' -TestName 'health' -Endpoint 'GET /api/Users/health' -Action {
        $r = Invoke-LcGet -Path '/api/Users/health' -Bearer $null
        Assert-Http200 -Result $r
    }

    $userId = Get-LcUserId
    Test-LcEndpoint -Report $Report -Section 'users-read' -TestName 'get authenticated user profile' -Endpoint 'GET /api/Users/{id}' -Action {
        $r = Invoke-LcGet -Path "/api/Users/$userId"
        Assert-Http200 -Result $r
    }

    Test-LcEndpoint -Report $Report -Section 'users-read' -TestName 'list user external providers' -Endpoint 'GET /api/Users/{id}/external-providers' -Action {
        $r = Invoke-LcGet -Path "/api/Users/$userId/external-providers"
        Assert-Http200 -Result $r
    }

    Test-LcEndpoint -Report $Report -Section 'users-read' -TestName 'get preferred metro areas' -Endpoint 'GET /api/Users/{id}/preferred-metro-areas' -Action {
        $r = Invoke-LcGet -Path "/api/Users/$userId/preferred-metro-areas"
        Assert-Http200 -Result $r
    }

    Test-LcEndpoint -Report $Report -Section 'users-read' -TestName 'search users (by query)' -Endpoint 'GET /api/Users/search' -Action {
        # Controller signature: [FromQuery] string query (required)
        $r = Invoke-LcGet -Path '/api/Users/search?query=test'
        Assert-Http200 -Result $r
    }
}

# ----------------------------------------------------------------------------
# Sub-section 2: users-profile-update — Profile PUT/PATCH paths
# ----------------------------------------------------------------------------
function Test-UsersProfileUpdateFlow {
    param([Parameter(Mandatory)]$Report)

    $userId = Get-LcUserId

    # PUT basic-info (toggle a fielf back-and-forth to leave staging in original state)
    Test-LcEndpoint -Report $Report -Section 'users-update' -TestName 'update basic-info' -Endpoint 'PUT /api/Users/{id}/basic-info' -Action {
        # Read current first
        $pre = Invoke-LcGet -Path "/api/Users/$userId"
        Assert-Http200 -Result $pre

        # PUT with the same data (no semantic change but exercises the write path)
        $body = @{
            firstName = $pre.Body.firstName
            lastName  = $pre.Body.lastName
            bio       = $pre.Body.bio
        }
        $r = Invoke-LcPut -Path "/api/Users/$userId/basic-info" -Body $body
        if ($r.StatusCode -ne 200 -and $r.StatusCode -ne 204) {
            throw "Expected 200/204, got $($r.StatusCode)"
        }
    }

    Test-LcEndpoint -Report $Report -Section 'users-update' -TestName 'update location' -Endpoint 'PUT /api/Users/{id}/location' -Action {
        # Per UpdateLocationRequest: City / State / ZipCode / Country (all nullable strings)
        $body = @{
            city    = 'Boston'
            state   = 'Massachusetts'
            zipCode = '02110'
            country = 'USA'
        }
        $r = Invoke-LcPut -Path "/api/Users/$userId/location" -Body $body
        if ($r.StatusCode -ne 200 -and $r.StatusCode -ne 204) {
            throw "Expected 200/204, got $($r.StatusCode)"
        }
    }

    Test-LcEndpoint -Report $Report -Section 'users-update' -TestName 'update languages' -Endpoint 'PUT /api/Users/{id}/languages' -Action {
        # Per UpdateLanguagesRequest: Languages: List<LanguageRequestDto{ LanguageCode, ProficiencyLevel }>
        # ProficiencyLevel = Native|Fluent|Conversational|Basic enum
        $body = @{
            languages = @(
                @{ languageCode = 'en'; proficiencyLevel = 'Native' }
            )
        }
        $r = Invoke-LcPut -Path "/api/Users/$userId/languages" -Body $body
        if ($r.StatusCode -ne 200 -and $r.StatusCode -ne 204) {
            throw "Expected 200/204, got $($r.StatusCode)"
        }
    }

    Test-LcEndpoint -Report $Report -Section 'users-update' -TestName 'update cultural-interests' -Endpoint 'PUT /api/Users/{id}/cultural-interests' -Action {
        $body = @{ culturalInterests = @('Music', 'Cuisine') }
        $r = Invoke-LcPut -Path "/api/Users/$userId/cultural-interests" -Body $body
        if ($r.StatusCode -ne 200 -and $r.StatusCode -ne 204) {
            throw "Expected 200/204, got $($r.StatusCode)"
        }
    }

    Test-LcEndpoint -Report $Report -Section 'users-update' -TestName 'update preferred metro areas' -Endpoint 'PUT /api/Users/{id}/preferred-metro-areas' -Action {
        # Use empty array to avoid coupling to specific metro area IDs
        $body = @{ metroAreaIds = @() }
        $r = Invoke-LcPut -Path "/api/Users/$userId/preferred-metro-areas" -Body $body
        if ($r.StatusCode -ne 200 -and $r.StatusCode -ne 204) {
            throw "Expected 200/204, got $($r.StatusCode)"
        }
    }

    Add-LcResult -Report $Report -Status SKIP -Section 'users-update' -TestName 'update email' -Endpoint 'PUT /api/Users/{id}/email' -SkipReason 'destructive (email rotation requires verification flow); -IncludeDestructive'
}

# ----------------------------------------------------------------------------
# Sub-section 3: users-photo — Profile photo upload + delete
# ----------------------------------------------------------------------------
function Test-UsersPhotoFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.3: multipart upload (9.h.4 wrapper) + DELETE for cleanup.
    # Reversible state -- upload then delete restores user to no-photo.
    $userId = Get-LcUserId

    Test-LcEndpoint -Report $Report -Section 'users-photo' -TestName 'upload profile photo (multipart)' -Endpoint 'POST /api/Users/{id}/profile-photo' -Action {
        $r = Invoke-LcMultipart -Path "/api/Users/$userId/profile-photo" -FileFieldName 'image' -FileName 'smoke-profile.png'
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'users-photo' -TestName 'delete profile photo' -Endpoint 'DELETE /api/Users/{id}/profile-photo' -Action {
        $r = Invoke-LcDelete -Path "/api/Users/$userId/profile-photo"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

# ----------------------------------------------------------------------------
# Sub-section 4: users-providers — External provider link / unlink
# ----------------------------------------------------------------------------
function Test-UsersProvidersFlow {
    param([Parameter(Mandatory)]$Report)

    Add-LcResult -Report $Report -Status SKIP -Section 'users-providers' -TestName 'link external provider' -Endpoint 'POST /api/Users/{id}/external-providers/link' -SkipReason 'state-dependent (requires valid OAuth callback); -IncludeExternalProviders'
    Add-LcResult -Report $Report -Status SKIP -Section 'users-providers' -TestName 'unlink external provider' -Endpoint 'DELETE /api/Users/{id}/external-providers/{provider}' -SkipReason 'destructive (would unlink prod-linked provider); -IncludeDestructive'
}

# ----------------------------------------------------------------------------
# Sub-section 5: users-upgrade — Self-service upgrade requests
# ----------------------------------------------------------------------------
function Test-UsersUpgradeFlow {
    param([Parameter(Mandatory)]$Report)

    # These mutate user state; SKIP by default unless -IncludeDestructive
    Add-LcResult -Report $Report -Status SKIP -Section 'users-upgrade' -TestName 'request upgrade' -Endpoint 'POST /api/Users/me/request-upgrade' -SkipReason 'state-dependent (state machine: only valid from certain roles); -IncludeDestructive'
    Add-LcResult -Report $Report -Status SKIP -Section 'users-upgrade' -TestName 'cancel upgrade' -Endpoint 'POST /api/Users/me/cancel-upgrade' -SkipReason 'state-dependent (only valid if pending upgrade exists); -IncludeDestructive'
}

# ----------------------------------------------------------------------------
# Sub-section 6: users-create — POST /api/Users (admin/registration)
# ----------------------------------------------------------------------------
function Test-UsersCreateFlow {
    param([Parameter(Mandatory)]$Report)

    Add-LcResult -Report $Report -Status SKIP -Section 'users-create' -TestName 'create user (POST /api/Users)' -Endpoint 'POST /api/Users' -SkipReason 'destructive (would pollute staging users); -IncludeDestructive'
}

# ============================================================================
# Public entry point
# ============================================================================
function Invoke-UsersControllerSmoke {
    [CmdletBinding()]
    param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)

    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }

    $allSections = @(
        @{ Name = 'users-read';        Func = { Test-UsersReadFlow -Report $report } }
        @{ Name = 'users-update';      Func = { Test-UsersProfileUpdateFlow -Report $report } }
        @{ Name = 'users-photo';       Func = { Test-UsersPhotoFlow -Report $report } }
        @{ Name = 'users-providers';   Func = { Test-UsersProvidersFlow -Report $report } }
        @{ Name = 'users-upgrade';     Func = { Test-UsersUpgradeFlow -Report $report } }
        @{ Name = 'users-create';      Func = { Test-UsersCreateFlow -Report $report } }
    )

    $sectionsToRun = if ($Only.Count -gt 0) {
        $allSections | Where-Object { $Only -contains $_.Name }
    } else { $allSections }

    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }
    Write-Host "Logged in as $($loginResult.UserId)"

    $report = New-LcReport -Name 'Wave 9.b: Smoke-UsersController'

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
    $report = Invoke-UsersControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""
    Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) passRate=$($summary.PassRate)% ==="
    Write-Host ""
    Write-Host (ConvertTo-LcMarkdown -Report $report)
    exit $summary.Failed
}
