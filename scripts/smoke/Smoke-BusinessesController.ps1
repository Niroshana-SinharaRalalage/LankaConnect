<#
.SYNOPSIS
  Smoke for BusinessesController (Wave 9.e). 14 endpoints.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-CommonFixtures.psm1') -Force  # Wave 9.h.10.2: Get-LcFixtureEmail

function Test-LcEndpoint {
    param([Parameter(Mandatory)]$Report, [Parameter(Mandatory)][string]$Section,
          [Parameter(Mandatory)][string]$TestName, [Parameter(Mandatory)][string]$Endpoint,
          [Parameter(Mandatory)][scriptblock]$Action, [string]$SkipReason = '')
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

function Test-BusinessesReadFlow {
    param([Parameter(Mandatory)]$Report)
    $fakeId = [Guid]::NewGuid().ToString()

    Test-LcEndpoint -Report $Report -Section 'businesses-read' -TestName 'list businesses' -Endpoint 'GET /api/Businesses' -Action {
        $r = Invoke-LcGet -Path '/api/Businesses'
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'businesses-read' -TestName 'search businesses' -Endpoint 'GET /api/Businesses/search' -Action {
        $r = Invoke-LcGet -Path '/api/Businesses/search?query=test'
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'businesses-read' -TestName 'business detail (404 OK)' -Endpoint 'GET /api/Businesses/{id}' -Action {
        $r = Invoke-LcGet -Path "/api/Businesses/$fakeId"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
    # Wave 9.h.fix: F20 fixed (BusinessConfiguration UpdatedAt no longer IsRequired);
    # F16 now exercisable via real business fixture.
    $fbiz = New-LcTaggedBusiness
    if ($fbiz.Success -and $fbiz.BusinessId) {
        Test-LcEndpoint -Report $Report -Section 'businesses-read' -TestName 'business services (F16 - real business fixture)' -Endpoint 'GET /api/Businesses/{id}/services' -Action {
            $r = Invoke-LcGet -Path "/api/Businesses/$($fbiz.BusinessId)/services"
            if ($r.StatusCode -ge 500) { throw "F16 confirmed real bug: 5xx $($r.StatusCode)" }
        }
        # Cleanup
        Invoke-LcDelete -Path "/api/Businesses/$($fbiz.BusinessId)" | Out-Null
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'businesses-read' -TestName 'business services (F16)' -Endpoint 'GET /api/Businesses/{id}/services' -SkipReason "F20 fixed but fixture business create failed: HTTP $($fbiz.StatusCode)"
    }
    Test-LcEndpoint -Report $Report -Section 'businesses-read' -TestName 'business images' -Endpoint 'GET /api/Businesses/{id}/images' -Action {
        $r = Invoke-LcGet -Path "/api/Businesses/$fakeId/images"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-BusinessesMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.fix: F20 fixed. Business CRUD lifecycle now testable.
    $script:mutBizId = $null
    Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'create business' -Endpoint 'POST /api/Businesses' -Action {
        $b = New-LcTaggedBusiness
        if (-not $b.Success) { throw "create failed: HTTP $($b.StatusCode)" }
        $script:mutBizId = $b.BusinessId
    }

    if ($script:mutBizId) {
        Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'update business' -Endpoint 'PUT /api/Businesses/{id}' -Action {
            $r = Invoke-LcPut -Path "/api/Businesses/$($script:mutBizId)" -Body @{
                name = 'Updated Smoke Business'
                description = 'Updated'
                contactPhone = '+15555550201'
                contactEmail = (Get-LcFixtureEmail -Slug 'business-updated' -Suffix (Get-LcCurrentRunTag))
                website = 'https://example.test/updated'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'add service' -Endpoint 'POST /api/Businesses/{id}/services' -Action {
            $r = Invoke-LcPost -Path "/api/Businesses/$($script:mutBizId)/services" -Body @{
                name = 'Smoke Service'
                description = 'Wave 9.h smoke'
                price = 25.00
                currency = 'USD'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'upload image (multipart)' -Endpoint 'POST /api/Businesses/{id}/images' -Action {
            $r = Invoke-LcMultipart -Path "/api/Businesses/$($script:mutBizId)/images" -FileFieldName 'image' -FileName 'biz.png'
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'reorder images' -Endpoint 'PATCH /api/Businesses/{id}/images/reorder' -Action {
            $r = Invoke-LcPatch -Path "/api/Businesses/$($script:mutBizId)/images/reorder" -Body @{ imageIds = @() }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        # Image delete + set-primary require specific image IDs from an upload; probe wire only
        $fakeImgId = [Guid]::NewGuid().ToString()
        Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'delete image (fake id wiring)' -Endpoint 'DELETE /api/Businesses/{id}/images/{imageId}' -Action {
            $r = Invoke-LcDelete -Path "/api/Businesses/$($script:mutBizId)/images/$fakeImgId"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'set primary image (fake id wiring)' -Endpoint 'PATCH /api/Businesses/{id}/images/{imageId}/set-primary' -Action {
            $r = Invoke-LcPatch -Path "/api/Businesses/$($script:mutBizId)/images/$fakeImgId/set-primary" -Body @{}
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        # Create a fresh business for the delete test (the mutBizId one has attached
        # services + images that may block deletion; direct-probe confirms delete
        # works cleanly on a bare business).
        $delFix = New-LcTaggedBusiness
        if ($delFix.Success -and $delFix.BusinessId) {
            Test-LcEndpoint -Report $Report -Section 'businesses-mutators' -TestName 'delete business (fresh fixture)' -Endpoint 'DELETE /api/Businesses/{id}' -Action {
                $r = Invoke-LcDelete -Path "/api/Businesses/$($delFix.BusinessId)"
                if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
            }
        } else {
            Add-LcResult -Report $Report -Status SKIP -Section 'businesses-mutators' -TestName 'delete business' -Endpoint 'DELETE /api/Businesses/{id}' -SkipReason 'fresh delete-fixture create failed'
        }
        # Cleanup: delete the mutator lifecycle business (best-effort)
        Invoke-LcDelete -Path "/api/Businesses/$($script:mutBizId)" | Out-Null
    } else {
        foreach ($n in 'update business','delete business','add service','upload image','delete image','set primary image','reorder images') {
            Add-LcResult -Report $Report -Status SKIP -Section 'businesses-mutators' -TestName $n -Endpoint '...' -SkipReason 'business create did not yield id'
        }
    }
}

function Test-BusinessesAdminFlow {
    param([Parameter(Mandatory)]$Report)
    Add-LcResult -Report $Report -Status SKIP -Section 'businesses-admin' -TestName 'list pending approvals' -Endpoint 'GET /api/Businesses/pending' -SkipReason 'admin-only; not in this controller route'
}

function Invoke-BusinessesControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'businesses-read'; Func = { Test-BusinessesReadFlow -Report $report } }
        @{ Name = 'businesses-mutators'; Func = { Test-BusinessesMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-BusinessesController'
    foreach ($section in $sectionsToRun) {
        Write-Host ""; Write-Host "=== Running sub-section: $($section.Name) ==="
        try { & $section.Func | Out-Null } catch {
            Add-LcResult -Report $report -Status FAIL -Section $section.Name -TestName 'sub-section orchestration' -Endpoint 'N/A' -ErrorMessage $_.Exception.Message
        }
    }
    Complete-LcReport -Report $report | Out-Null
    return $report
}

if ($MyInvocation.InvocationName -ne '.') {
    $report = Invoke-BusinessesControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
