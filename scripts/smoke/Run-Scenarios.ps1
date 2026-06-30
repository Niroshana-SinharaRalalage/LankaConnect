<#
.SYNOPSIS
  Wave 9.f scenarios orchestrator. Runs all .ps1 files in ./scenarios/ and aggregates.
#>

[CmdletBinding()]
param([string]$OutputDir = '')

$ErrorActionPreference = 'Stop'

if (-not $OutputDir) {
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $OutputDir = Join-Path $PSScriptRoot "../../reports/scenarios-$timestamp"
}
$OutputDir = [System.IO.Path]::GetFullPath($OutputDir)
New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null

Write-Host "============================================"
Write-Host "Wave 9.f Cross-Controller Scenarios"
Write-Host "Output: $OutputDir"
Write-Host "============================================"

$scenarios = Get-ChildItem -Path (Join-Path $PSScriptRoot 'scenarios') -Filter 'Scenario-*.ps1' -ErrorAction SilentlyContinue
if (-not $scenarios) {
    Write-Host "No scenarios found in scenarios/ directory"
    exit 0
}

$totalFail = 0
foreach ($s in $scenarios) {
    Write-Host ""
    Write-Host "### Running $($s.Name) ###"
    & $s.FullName
    if ($LASTEXITCODE -ne 0) {
        $totalFail++
        Write-Host "::warning::Scenario $($s.Name) had $LASTEXITCODE failures"
    }
}

Write-Host ""
Write-Host "============================================"
Write-Host "$($scenarios.Count) scenarios completed; $totalFail had failures"
Write-Host "============================================"
exit $totalFail
