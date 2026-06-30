<#
.SYNOPSIS
  Wave 9.f scenario 3: platform bootstrap data integrity (reference data + metro areas
  + config + features available without auth). Cross-controller: 5 controllers.
#>

[CmdletBinding()]
param([switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot '..\modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

$report = New-LcReport -Name 'Wave 9.f Scenario: Platform Bootstrap (anonymous)'
# No login -- this scenario verifies the anonymous/bootstrap surface

$endpoints = @(
    @{ Path='/api/Health';                              Name='/api/Health (bootstrap healthcheck)' }
    @{ Path='/api/Health/feature-flags';                Name='/api/Health/feature-flags (FE bootstrap)' }
    @{ Path='/api/Configuration/features';              Name='/api/Configuration/features (FE config)' }
    @{ Path='/api/metro-areas';                         Name='/api/metro-areas (W5.3 MetroAreaRepository; FE picker)' }
    @{ Path='/api/reference-data?types=EventCategory,EventStatus,UserRole'; Name='/api/reference-data (lookup tables; required ?types=...)' }
    @{ Path='/api/Public/stats';                        Name='/api/Public/stats (landing page)' }
)

foreach ($e in $endpoints) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-LcGet -Path $e.Path -Bearer $null
        Assert-Http200 -Result $r
        $sw.Stop()
        Add-LcResult -Report $report -Status PASS -Section 'platform-bootstrap' -TestName $e.Name -Endpoint "GET $($e.Path)" -DurationMs $sw.ElapsedMilliseconds
    }
    catch {
        $sw.Stop()
        Add-LcResult -Report $report -Status FAIL -Section 'platform-bootstrap' -TestName $e.Name -Endpoint "GET $($e.Path)" -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
    }
}

Complete-LcReport -Report $report | Out-Null
$summary = Get-LcReportSummary -Report $report
Write-Host ""
Write-Host "=== SCENARIO Platform Bootstrap: passed=$($summary.Passed) failed=$($summary.Failed) total=$($summary.Total) ==="
# Show failure detail when failures exist
foreach ($r in $report.Results) {
    if ($r.Status -eq 'FAIL') {
        Write-Host "FAIL: $($r.TestName) [$($r.Endpoint)] -- $($r.ErrorMessage)"
    }
}
exit $summary.Failed
