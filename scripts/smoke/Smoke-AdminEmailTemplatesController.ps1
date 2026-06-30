<#
.SYNOPSIS
  Smoke for AdminEmailTemplatesController (Wave 9.d). 6 endpoints, admin-scoped.
  Inverted 403 assertions.
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

function Test-AdminEmailTemplatesPermissionFlow {
    param([Parameter(Mandatory)]$Report)
    $fakeId = [Guid]::NewGuid().ToString()

    $endpoints = @(
        @{ Method='GET';   Path='/api/admin/email-templates';                Name='list templates' }
        @{ Method='GET';   Path="/api/admin/email-templates/$fakeId";        Name='template detail' }
        @{ Method='GET';   Path='/api/admin/email-templates/by-name/welcome';Name='template by name' }
        @{ Method='PUT';   Path="/api/admin/email-templates/$fakeId";        Name='update template' }
        @{ Method='PATCH'; Path="/api/admin/email-templates/$fakeId/toggle-active"; Name='toggle active' }
        @{ Method='POST';  Path="/api/admin/email-templates/$fakeId/preview";Name='preview template' }
    )

    # Test user is AdminManager. Assert wiring (non-5xx).
    foreach ($e in $endpoints) {
        Test-LcEndpoint -Report $Report -Section 'admin-templates' -TestName $e.Name -Endpoint "$($e.Method) $($e.Path)" -Action {
            $r = switch ($e.Method) {
                'GET'   { Invoke-LcGet  -Path $e.Path }
                'PUT'   { Invoke-LcPut  -Path $e.Path -Body @{ subject='Smoke'; body='Smoke body'; bodyHtml='<p>Smoke</p>' } }
                'PATCH' { Invoke-LcPatch -Path $e.Path -Body @{} }
                'POST'  { Invoke-LcPost -Path $e.Path -Body @{ recipientEmail='smoke@test'; testData=@{} } }
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    }
}

function Invoke-AdminEmailTemplatesControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @( @{ Name = 'admin-templates-perm'; Func = { Test-AdminEmailTemplatesPermissionFlow -Report $report } } )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-AdminEmailTemplatesController'
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
    $report = Invoke-AdminEmailTemplatesControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
