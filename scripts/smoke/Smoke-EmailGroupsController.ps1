<#
.SYNOPSIS
  Smoke for EmailGroupsController (Wave 9.d). 5 endpoints (CRUD).
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
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

function Test-EmailGroupsReadFlow {
    param([Parameter(Mandatory)]$Report)
    $fakeId = [Guid]::NewGuid().ToString()
    Test-LcEndpoint -Report $Report -Section 'email-groups-read' -TestName 'list email groups' -Endpoint 'GET /api/EmailGroups' -Action {
        $r = Invoke-LcGet -Path '/api/EmailGroups'
        Assert-Http200 -Result $r
    }
    Test-LcEndpoint -Report $Report -Section 'email-groups-read' -TestName 'group by id (404 OK)' -Endpoint 'GET /api/EmailGroups/{id}' -Action {
        $r = Invoke-LcGet -Path "/api/EmailGroups/$fakeId"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }
}

function Test-EmailGroupsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.3: tag-based fixture + full create/update/delete lifecycle
    $tag = Get-LcCurrentRunTag
    $groupId = $null

    Test-LcEndpoint -Report $Report -Section 'email-groups-mutators' -TestName 'create email group' -Endpoint 'POST /api/EmailGroups' -Action {
        # Contract: emailAddresses is a comma-separated STRING (not array)
        $r = Invoke-LcPost -Path '/api/EmailGroups' -Body @{
            name = "$tag SmokeGroup"
            description = 'Wave 9.h.3 smoke fixture'
            emailAddresses = (Get-LcFixtureEmail -Slug 'email-group-recipient' -Suffix $tag)
        }
        if (-not $r.Success) { throw "create failed: HTTP $($r.StatusCode)" }
        $script:emailGroupId = if ($r.Body -is [string]) { $r.Body.Trim('"') } elseif ($r.Body.id) { $r.Body.id } else { $null }
    }

    if ($script:emailGroupId) {
        Test-LcEndpoint -Report $Report -Section 'email-groups-mutators' -TestName 'update email group' -Endpoint 'PUT /api/EmailGroups/{id}' -Action {
            $r = Invoke-LcPut -Path "/api/EmailGroups/$($script:emailGroupId)" -Body @{
                name = "$tag SmokeGroup Updated"
                description = 'Updated by 9.h.3'
                emailAddresses = "$(Get-LcFixtureEmail -Slug 'email-group-recipient' -Suffix $tag),$(Get-LcFixtureEmail -Slug 'email-group-recipient2' -Suffix $tag)"
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'email-groups-mutators' -TestName 'delete email group' -Endpoint 'DELETE /api/EmailGroups/{id}' -Action {
            $r = Invoke-LcDelete -Path "/api/EmailGroups/$($script:emailGroupId)"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'email-groups-mutators' -TestName 'update email group' -Endpoint 'PUT /api/EmailGroups/{id}' -SkipReason 'create did not yield ID'
        Add-LcResult -Report $Report -Status SKIP -Section 'email-groups-mutators' -TestName 'delete email group' -Endpoint 'DELETE /api/EmailGroups/{id}' -SkipReason 'create did not yield ID'
    }
}

function Invoke-EmailGroupsControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'email-groups-read'; Func = { Test-EmailGroupsReadFlow -Report $report } }
        @{ Name = 'email-groups-mutators'; Func = { Test-EmailGroupsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.d: Smoke-EmailGroupsController'
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
    $report = Invoke-EmailGroupsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
