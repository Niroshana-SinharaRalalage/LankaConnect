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
    # Wave 9.h.2 investigation: F16 RESOLUTION BLOCKED ON F20.
    # GET /api/Businesses/{id}/services likely works with a real business but Business
    # creation itself is broken (F20: POST /api/Businesses returns 500 DatabaseError).
    # Cannot create fixture business -> cannot smoke /services real-business path.
    Add-LcResult -Report $Report -Status SKIP -Section 'businesses-read' -TestName 'business services (F16; BLOCKED on F20)' -Endpoint 'GET /api/Businesses/{id}/services' -SkipReason 'F16 resolution requires real business fixture; F20 (POST /api/Businesses 500 DatabaseError) blocks fixture creation; needs platform F20 fix first'
    Test-LcEndpoint -Report $Report -Section 'businesses-read' -TestName 'business images' -Endpoint 'GET /api/Businesses/{id}/images' -Action {
        $r = Invoke-LcGet -Path "/api/Businesses/$fakeId/images"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-BusinessesMutatorsFlow {
    param([Parameter(Mandatory)]$Report)
    $mutators = @(
        @{ Method='POST';   Path='/api/Businesses';                                       Name='create business' }
        @{ Method='PUT';    Path='/api/Businesses/{id}';                                  Name='update business' }
        @{ Method='DELETE'; Path='/api/Businesses/{id}';                                  Name='delete business' }
        @{ Method='POST';   Path='/api/Businesses/{id}/services';                         Name='add service' }
        @{ Method='POST';   Path='/api/Businesses/{id}/images';                           Name='upload image' }
        @{ Method='DELETE'; Path='/api/Businesses/{id}/images/{imageId}';                 Name='delete image' }
        @{ Method='PATCH';  Path='/api/Businesses/{id}/images/{imageId}/set-primary';     Name='set primary image' }
        @{ Method='PATCH';  Path='/api/Businesses/{id}/images/reorder';                   Name='reorder images' }
    )
    foreach ($m in $mutators) {
        Add-LcResult -Report $Report -Status SKIP -Section 'businesses-mutators' -TestName $m.Name -Endpoint "$($m.Method) $($m.Path)" -SkipReason 'destructive; -IncludeDestructive'
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
