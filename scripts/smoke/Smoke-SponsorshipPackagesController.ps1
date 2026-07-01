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

    # Wave 9.h.3: real fixtures + actual mutator coverage
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'sp-packages-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    $pkgId = $null
    Test-LcEndpoint -Report $Report -Section 'sp-packages-mutators' -TestName 'create sponsorship package' -Endpoint 'POST /api/events/{eventId}/sponsorship-packages' -Action {
        $p = New-LcTaggedSponsorshipPackage -EventId $eventId
        if (-not $p.Success) { throw "create failed: HTTP $($p.StatusCode)" }
        $script:spPkgId = if ($p.Body -is [string]) { $p.Body.Trim('"') } elseif ($p.Body.id) { $p.Body.id } else { $null }
    }

    if ($script:spPkgId) {
        Test-LcEndpoint -Report $Report -Section 'sp-packages-mutators' -TestName 'update package' -Endpoint 'PUT /api/events/{eventId}/sponsorship-packages/{pkgId}' -Action {
            $r = Invoke-LcPut -Path "/api/events/$eventId/sponsorship-packages/$($script:spPkgId)" -Body @{
                name = 'Smoke Updated Package'; description = 'Updated by 9.h.3'; price = 750.00; currency = 'USD'; perks = @('Updated perk'); quantity = 10
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sp-packages-mutators' -TestName 'upload image (multipart)' -Endpoint 'POST /api/events/{eventId}/sponsorship-packages/{pkgId}/image' -Action {
            $r = Invoke-LcMultipart -Path "/api/events/$eventId/sponsorship-packages/$($script:spPkgId)/image" -FileFieldName 'image' -FileName 'pkg.png'
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sp-packages-mutators' -TestName 'delete image' -Endpoint 'DELETE /api/events/{eventId}/sponsorship-packages/{pkgId}/image' -Action {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/sponsorship-packages/$($script:spPkgId)/image"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'sp-packages-mutators' -TestName 'delete package' -Endpoint 'DELETE /api/events/{eventId}/sponsorship-packages/{pkgId}' -Action {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/sponsorship-packages/$($script:spPkgId)"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        foreach ($n in 'update package','upload image','delete image','delete package') {
            Add-LcResult -Report $Report -Status SKIP -Section 'sp-packages-mutators' -TestName $n -Endpoint '...' -SkipReason 'package create did not yield ID for downstream'
        }
    }

    # Create a fresh package + purchase via Stripe (returns Stripe URL; no actual payment)
    $tag = Get-LcCurrentRunTag
    $pp = New-LcTaggedSponsorshipPackage -EventId $eventId
    $purchasePkgId = if ($pp.Body.id) { $pp.Body.id } elseif ($pp.Body -is [string]) { $pp.Body.Trim('"') } else { $null }
    if ($purchasePkgId) {
        Test-LcEndpoint -Report $Report -Section 'sp-packages-mutators' -TestName 'purchase package (Stripe session URL)' -Endpoint 'POST /api/events/{eventId}/sponsorship-packages/{pkgId}/purchase' -Action {
            $r = Invoke-LcPost -Path "/api/events/$eventId/sponsorship-packages/$purchasePkgId/purchase" -Body @{
                sponsorName = 'Smoke Purchaser'
                sponsorEmail = (Get-LcFixtureEmail -Slug 'sponsorship-package-purchase' -Suffix $tag)
                sponsorOrganization = 'Smoke Co'
                successUrl = 'https://example.test/success'
                cancelUrl = 'https://example.test/cancel'
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'sp-packages-mutators' -TestName 'purchase package' -Endpoint 'POST /api/events/{eventId}/sponsorship-packages/{pkgId}/purchase' -SkipReason 'purchase fixture package create did not yield id'
    }

    Remove-LcFixturesByTag | Out-Null
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
