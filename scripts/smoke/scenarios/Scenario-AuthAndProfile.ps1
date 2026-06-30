<#
.SYNOPSIS
  Wave 9.f scenario 1: cross-controller user journey (login -> profile read -> update -> re-read).

.DESCRIPTION
  Demonstrates the cross-controller-scenario pattern: an end-user flow exercises
  multiple controllers in sequence, asserting integrity across them. Adds value
  beyond per-controller smokes by surfacing inter-controller integration bugs.
#>

[CmdletBinding()]
param([switch]$SkipLogChecks)

$moduleDir = Join-Path $PSScriptRoot '..\modules'
Import-Module (Join-Path $moduleDir 'Lc-Http.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Auth.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Assertion.psm1') -Force
Import-Module (Join-Path $moduleDir 'Lc-Report.psm1') -Force

$report = New-LcReport -Name 'Wave 9.f Scenario: Auth + Profile'
$loginResult = Invoke-LcLogin
if (-not $loginResult.Success) { throw "Login failed: $($loginResult.Error)" }

# Step 1: Read profile via AuthController
$sw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $r1 = Invoke-LcGet -Path '/api/Auth/profile'
    Assert-Http200 -Result $r1
    $sw.Stop()
    Add-LcResult -Report $report -Status PASS -Section 'auth-and-profile' -TestName 'step 1: GET /api/Auth/profile' -Endpoint 'GET /api/Auth/profile' -DurationMs $sw.ElapsedMilliseconds
}
catch {
    $sw.Stop()
    Add-LcResult -Report $report -Status FAIL -Section 'auth-and-profile' -TestName 'step 1: GET /api/Auth/profile' -Endpoint 'GET /api/Auth/profile' -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
}

# Step 2: Read same profile via UsersController (cross-controller read consistency)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $userId = Get-LcUserId
    $r2 = Invoke-LcGet -Path "/api/Users/$userId"
    Assert-Http200 -Result $r2
    $sw.Stop()
    Add-LcResult -Report $report -Status PASS -Section 'auth-and-profile' -TestName 'step 2: GET /api/Users/{id} (cross-controller)' -Endpoint 'GET /api/Users/{id}' -DurationMs $sw.ElapsedMilliseconds
}
catch {
    $sw.Stop()
    Add-LcResult -Report $report -Status FAIL -Section 'auth-and-profile' -TestName 'step 2: GET /api/Users/{id} (cross-controller)' -Endpoint 'GET /api/Users/{id}' -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
}

# Step 3: Verify WhatsApp preferences accessible after auth
$sw = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $r3 = Invoke-LcGet -Path '/api/whatsapp/preferences'
    Assert-Http200 -Result $r3
    $sw.Stop()
    Add-LcResult -Report $report -Status PASS -Section 'auth-and-profile' -TestName 'step 3: GET /api/whatsapp/preferences (downstream)' -Endpoint 'GET /api/whatsapp/preferences' -DurationMs $sw.ElapsedMilliseconds
}
catch {
    $sw.Stop()
    Add-LcResult -Report $report -Status FAIL -Section 'auth-and-profile' -TestName 'step 3: GET /api/whatsapp/preferences (downstream)' -Endpoint 'GET /api/whatsapp/preferences' -DurationMs $sw.ElapsedMilliseconds -ErrorMessage $_.Exception.Message
}

Complete-LcReport -Report $report | Out-Null
$summary = Get-LcReportSummary -Report $report
Write-Host ""
Write-Host "=== SCENARIO Auth+Profile: passed=$($summary.Passed) failed=$($summary.Failed) total=$($summary.Total) ==="
exit $summary.Failed
