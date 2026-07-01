<#
.SYNOPSIS
  Per-controller smoke for AddOnsController. Wave 9.c deliverable.

.DESCRIPTION
  12 endpoints, all scoped under /api/events/{eventId}/add-ons/...
  Tests cover the AddOnDefinitionRepository + AddOnPurchaseRepository read paths
  shipped in Wave 5.3 (RegistrationAddition is also touched transitively).

  Reads use fake event GUID -> 404 OK; writes are SKIP_DESTRUCTIVE.
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

# ----------------------------------------------------------------------------
# Sub-section 1: addons-read — Event-scoped GET endpoints
# ----------------------------------------------------------------------------
function Test-AddOnsReadFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.2: create a real event fixture and enable add-on subresources.
    # All reads now exercise actual W5.3 AddOnDefinition + AddOnPurchase repos
    # against a real event the test user organizes. The 3 organizer-only endpoints
    # (purchases/summary/export) that previously SKIPped with "500 on fake event"
    # findings F1-F3 now have real coverage.
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'addons-read' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture event create failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    Test-LcEndpoint -Report $Report -Section 'addons-read' -TestName 'list add-on definitions (W5.3 AddOnDefinitionRepository)' -Endpoint 'GET /api/events/{eventId}/add-ons' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/add-ons"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'addons-read' -TestName 'my add-on purchases (W5.3 AddOnPurchaseRepository)' -Endpoint 'GET /api/events/{eventId}/add-ons/my-purchases' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/add-ons/my-purchases"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    Test-LcEndpoint -Report $Report -Section 'addons-read' -TestName 'mine (alternate listing)' -Endpoint 'GET /api/events/{eventId}/add-ons/mine' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/add-ons/mine"
        if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
    }

    # Wave 9.h.2: previously-SKIPped F1-F3 now exercised against real event fixture.
    # If these PASS, the findings were environmental (fake-event 500 = hardening but
    # not real bug); if they 5xx, real platform bug confirmed.
    Test-LcEndpoint -Report $Report -Section 'addons-read' -TestName 'organizer purchases listing (F1 - real event)' -Endpoint 'GET /api/events/{eventId}/add-ons/purchases' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/add-ons/purchases"
        if ($r.StatusCode -ge 500) { throw "F1 confirmed real bug: 5xx $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'addons-read' -TestName 'organizer purchases summary (F2 - real event)' -Endpoint 'GET /api/events/{eventId}/add-ons/purchases/summary' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/add-ons/purchases/summary"
        if ($r.StatusCode -ge 500) { throw "F2 confirmed real bug: 5xx $($r.StatusCode)" }
    }
    Test-LcEndpoint -Report $Report -Section 'addons-read' -TestName 'organizer purchases CSV export (F3 - real event)' -Endpoint 'GET /api/events/{eventId}/add-ons/purchases/export' -Action {
        $r = Invoke-LcGet -Path "/api/events/$eventId/add-ons/purchases/export"
        if ($r.StatusCode -ge 500) { throw "F3 confirmed real bug: 5xx $($r.StatusCode)" }
    }

    # Cleanup: cascade-delete event removes all addon definitions + purchases
    Remove-LcFixturesByTag | Out-Null
}

# ----------------------------------------------------------------------------
# Sub-section 2: addons-mutators — POST/PUT/DELETE (all SKIP)
# ----------------------------------------------------------------------------
function Test-AddOnsMutatorsFlow {
    param([Parameter(Mandatory)]$Report)

    # Wave 9.h.3: real fixtures + actual mutator coverage
    $fix = New-LcFreeEvent
    if (-not $fix.Success) {
        Add-LcResult -Report $Report -Status FAIL -Section 'addons-mutators' -TestName 'fixture setup' -Endpoint 'POST /api/Events' -ErrorMessage "fixture failed: $($fix.Error)"
        return
    }
    Publish-LcEvent -EventId $fix.EventId | Out-Null
    Enable-LcEventFinanceConfigs -EventId $fix.EventId | Out-Null
    $eventId = $fix.EventId

    # Create definition + capture ID for subsequent mutators
    $defId = $null
    Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'create add-on definition (W5.3 write)' -Endpoint 'POST /api/events/{eventId}/add-ons' -Action {
        $a = New-LcTaggedAddOnDefinition -EventId $eventId
        if (-not $a.Success) { throw "create failed: HTTP $($a.StatusCode)" }
        $script:addOnDefId = if ($a.Body -is [string]) { $a.Body.Trim('"') }
                             elseif ($a.Body.id) { $a.Body.id }
                             else { $null }
    }

    if ($script:addOnDefId) {
        Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'update add-on definition' -Endpoint 'PUT /api/events/{eventId}/add-ons/{defId}' -Action {
            $r = Invoke-LcPut -Path "/api/events/$eventId/add-ons/$($script:addOnDefId)" -Body @{
                name = 'Smoke Updated AddOn'; description = 'Updated by 9.h.3'; price = 12.00; currency = 'USD'; quantityLimit = 50; sortOrder = 0; isActive = $true
            }
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'upload definition image (multipart)' -Endpoint 'POST /api/events/{eventId}/add-ons/{defId}/image' -Action {
            $r = Invoke-LcMultipart -Path "/api/events/$eventId/add-ons/$($script:addOnDefId)/image" -FileFieldName 'image' -FileName 'addon.png'
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
        Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'delete definition image' -Endpoint 'DELETE /api/events/{eventId}/add-ons/{defId}/image' -Action {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/add-ons/$($script:addOnDefId)/image"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    } else {
        foreach ($n in 'update add-on definition','upload definition image','delete definition image') {
            Add-LcResult -Report $Report -Status SKIP -Section 'addons-mutators' -TestName $n -Endpoint '...' -SkipReason 'definition create did not yield ID for downstream'
        }
    }

    # Wave 9.h.10.4 gap-close: DELETE definition image was missing from coverage
    if ($script:addOnDefId) {
        Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'delete add-on definition image' -Endpoint 'DELETE /api/events/{eventId}/add-ons/{definitionId}/image' -Action {
            $r = Invoke-LcDelete -Path "/api/events/$eventId/add-ons/$($script:addOnDefId)/image"
            if ($r.StatusCode -ge 500) { throw "5xx: $($r.StatusCode)" }
        }
    }

    # Wave 9.h.5: Stripe purchase endpoints DO work - they return 200 with a Stripe
    # CHECKOUT SESSION URL (cs_test_...) and write the pending AddOnPurchase record.
    # We don't need to complete the Stripe flow to verify the W5.3 repo write path.
    if ($script:addOnDefId) {
        Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'purchase add-on (W5.3 AddOnPurchaseRepository write + Stripe session)' -Endpoint 'POST /api/events/{eventId}/add-ons/{defId}/purchase' -Action {
            $r = Invoke-LcPost -Path "/api/events/$eventId/add-ons/$($script:addOnDefId)/purchase" -Body @{
                buyerName  = 'Smoke Buyer 9h5'
                buyerEmail = (Get-LcFixtureEmail -Slug 'addon-purchase' -Suffix (Get-LcCurrentRunTag))
                buyerPhone = '+15555550999'
                quantity   = 1
                successUrl = 'https://example.test/success'
                cancelUrl  = 'https://example.test/cancel'
            }
            if (-not $r.Success) { throw "purchase failed: HTTP $($r.StatusCode)" }
            if ($r.Body -notmatch 'checkout.stripe.com') { throw 'expected Stripe checkout URL in response body' }
        }

        Test-LcEndpoint -Report $Report -Section 'addons-mutators' -TestName 'purchase add-on cart (W5.3 AddOnPurchaseRepository + RegistrationAdditionRepository writes)' -Endpoint 'POST /api/events/{eventId}/add-ons/purchase-cart' -Action {
            $r = Invoke-LcPost -Path "/api/events/$eventId/add-ons/purchase-cart" -Body @{
                buyerName  = 'Smoke Cart Buyer'
                buyerEmail = (Get-LcFixtureEmail -Slug 'addon-cart-purchase' -Suffix (Get-LcCurrentRunTag))
                successUrl = 'https://example.test/success'
                cancelUrl  = 'https://example.test/cancel'
                items      = @( @{ addOnDefinitionId = $script:addOnDefId; quantity = 2 } )
            }
            if (-not $r.Success) { throw "cart purchase failed: HTTP $($r.StatusCode)" }
            if ($r.Body -notmatch 'checkout.stripe.com') { throw 'expected Stripe checkout URL in response body' }
        }
    } else {
        Add-LcResult -Report $Report -Status SKIP -Section 'addons-mutators' -TestName 'purchase add-on' -Endpoint 'POST /api/events/{eventId}/add-ons/{defId}/purchase' -SkipReason 'no add-on definition id from create'
        Add-LcResult -Report $Report -Status SKIP -Section 'addons-mutators' -TestName 'purchase add-on cart' -Endpoint 'POST /api/events/{eventId}/add-ons/purchase-cart' -SkipReason 'no add-on definition id from create'
    }

    Remove-LcFixturesByTag | Out-Null
}

function Invoke-AddOnsControllerSmoke {
    [CmdletBinding()]
    param([string[]]$Only = @(), [switch]$SkipLogChecksLocal)
    if ($SkipLogChecksLocal) { $script:SkipLogChecks = $true }

    $allSections = @(
        @{ Name = 'addons-read';     Func = { Test-AddOnsReadFlow -Report $report } }
        @{ Name = 'addons-mutators'; Func = { Test-AddOnsMutatorsFlow -Report $report } }
    )
    $sectionsToRun = if ($Only.Count -gt 0) { $allSections | Where-Object { $Only -contains $_.Name } } else { $allSections }

    $loginResult = Invoke-LcLogin
    if (-not $loginResult.Success) { throw "Login to staging failed: $($loginResult.Error)" }

    $report = New-LcReport -Name 'Wave 9.c: Smoke-AddOnsController'
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
    $report = Invoke-AddOnsControllerSmoke -Only $Sections -SkipLogChecksLocal:$SkipLogChecks
    $summary = Get-LcReportSummary -Report $report
    Write-Host ""; Write-Host "=== SMOKE COMPLETE: passed=$($summary.Passed) failed=$($summary.Failed) skipped=$($summary.Skipped) total=$($summary.Total) ==="
    exit $summary.Failed
}
