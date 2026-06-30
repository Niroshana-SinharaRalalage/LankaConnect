<#
.SYNOPSIS
  Smoke for SponsorshipPackagesController (Wave 9.e). 8 endpoints.
  CRITICAL Wave 5 verification: exercises SponsorshipPackageRepository read paths.
#>

[CmdletBinding()]
param([string[]]$Sections = @(), [switch]$IncludeDestructive, [switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot 'modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-EventFixtures.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-FinanceFixtures.psm1') -Force

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

function Test-SponsorshipPackagesReadFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.2: real event fixture + sponsor-config enabled
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'sp-packages-read' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    Test-LcEndpoint -Report $Report -Section 'sp-packages-read' -TestName 'list active packages' -Endpoint "GET /api/events/{eventId}/sponsorship-packages/active" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/sponsorship-packages/active"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Wave 9.h.2: F7 resolved via real event fixture
    Test-LcEndpoint -Report $Report -Section 'sp-packages-read' -TestName 'list packages (F7 - real event; W5.3 SponsorshipPackageRepository)' -Endpoint "GET /api/events/{eventId}/sponsorship-packages" -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/sponsorship-packages"
        if ($r.StatusCode -ge 500) { throw "F7 confirmed real bug: 5xx $($r.StatusCode)" }
    }

    Remove-LcFixturesByTag | Out-Null
}

function Test-SponsorshipPackagesMutatorsFlow {
    param([Parameter(Mandatory)]$Report)
    $mutators = @(
        @{ Method='POST';   Path='/api/events/{eventId}/sponsorship-packages';                  Name='create package' }
        @{ Method='PUT';    Path='/api/events/{eventId}/sponsorship-packages/{pkgId}';          Name='update package' }
        @{ Method='DELETE'; Path='/api/events/{eventId}/sponsorship-packages/{pkgId}';          Name='delete package' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsorship-packages/{pkgId}/purchase'; Name='purchase package' }
        @{ Method='POST';   Path='/api/events/{eventId}/sponsorship-packages/{pkgId}/image';    Name='upload image' }
        @{ Method='DELETE'; Path='/api/events/{eventId}/sponsorship-packages/{pkgId}/image';    Name='delete image' }
    )
    foreach ($m in $mutators) {
        Add-LcResult -Report $Report -Status SKIP -Section 'sp-packages-mutators' -TestName $m.Name -Endpoint "$($m.Method) $($m.Path)" -SkipReason 'destructive; -IncludeDestructive'
    }
}

function Invoke-SponsorshipPackagesControllerSmoke {
    [CmdletBinding()] param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }
    $allSections = @(
        @{ Name = 'sp-packages-read';     Func = { Test-SponsorshipPackagesReadFlow -Report $report } }
        @{ Name = 'sp-packages-mutators'; Func = { Test-SponsorshipPackagesMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }
    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }
    $report = New-LcReport -Name 'Wave 9.e: Smoke-SponsorshipPackagesController'
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
    $report = Invoke-SponsorshipPackagesControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
