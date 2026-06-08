<#
.SYNOPSIS
    Wave4.9.1.11 (2026-06-08): asserts the event-detail JSON response
    matches the dual-field Money DTO shape per ADR-005.

.DESCRIPTION
    The BuildingBlocks + SharedKernel Wave 2 work introduced a dual-field
    expand/contract pattern for monetary fields:

        ticketPriceAmount   (decimal)
        ticketPriceCurrency (string ISO 4217)

    NOT a nested {amount, currency} object. The frontend (W11) eventually
    consumes a typed Money object on TypeScript side; the API contract
    stays flat for backwards compatibility with Wave 1 clients.

    This smoke verifies the contract is intact on a representative
    paid event with cultural / category metadata so a price-shape
    regression is caught BEFORE any UI breaks.

.PARAMETER EventId
    Defaults to a known paid staging event. Override via -EventId to
    target a different fixture.

.OUTPUTS
    Exit 0 + summary on green.
    Exit 1 + diagnostic on first contract violation.

.NOTES
    G8 smoke from docs/audit/route-inventory-2026-06-08.md. Wave4.9.1.11.
#>
[CmdletBinding()]
param(
    [string]$EventId = '5fbcea92-bd5b-486f-9eab-1c4ee0146307',
    [string]$StagingUrl = $(if ($env:LC_STAGING_URL) { $env:LC_STAGING_URL } else { 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io' })
)

$ErrorActionPreference = 'Stop'

if (-not $env:LC_BEARER) {
    Write-Error 'Smoke-EventDetailPriceShape requires $env:LC_BEARER - call Invoke-Login.ps1 first.'
    exit 1
}

$uri = "$StagingUrl/api/events/$EventId"
try {
    $event = Invoke-RestMethod -Uri $uri -Headers @{ Authorization = "Bearer $env:LC_BEARER" } -TimeoutSec 30
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Error "Smoke-EventDetailPriceShape FAILED - HTTP $statusCode fetching $uri ($($_.Exception.Message))"
    exit 1
}

$violations = @()

# 1. Required ticket-price fields (dual-field shape)
$requiredFields = @(
    'ticketPriceAmount', 'ticketPriceCurrency',
    'isFree', 'paymentMode',
    'hasDualPricing', 'hasGroupPricing'
)
foreach ($f in $requiredFields) {
    if (-not $event.PSObject.Properties.Name -contains $f) {
        $violations += "missing required field: $f"
    }
}

# 2. Currency code shape: 3-letter ISO 4217 (USD, LKR, etc.)
if ($event.ticketPriceCurrency) {
    if ($event.ticketPriceCurrency -notmatch '^[A-Z]{3}$') {
        $violations += "ticketPriceCurrency='$($event.ticketPriceCurrency)' violates ISO 4217 (3 uppercase letters)"
    }
}

# 3. Amount shape: must be a number-y value (decimal). PowerShell's JSON parser
#    returns System.Decimal for non-integer JSON numbers; assert IsValueType.
if ($null -ne $event.ticketPriceAmount) {
    if (-not ($event.ticketPriceAmount -is [decimal] -or $event.ticketPriceAmount -is [double] -or $event.ticketPriceAmount -is [int] -or $event.ticketPriceAmount -is [long])) {
        $violations += "ticketPriceAmount type=$($event.ticketPriceAmount.GetType().FullName) - expected numeric"
    }
}

# 4. The dual-field invariant: there must NOT be a nested 'price' object
#    (i.e., no { amount, currency } sub-object that bypasses the flat contract).
if ($event.PSObject.Properties.Name -contains 'price') {
    $priceProp = $event.price
    if ($priceProp -is [PSCustomObject] -and ($priceProp.PSObject.Properties.Name -contains 'amount' -or $priceProp.PSObject.Properties.Name -contains 'currency')) {
        $violations += 'event contains a nested `price` object with amount/currency - violates the flat dual-field contract per ADR-005. The frontend (W11) needs the flat shape.'
    }
}

# 5. Dual-pricing fields (adult/child) shape parallel
foreach ($prefix in @('adult', 'child')) {
    $hasAmount   = $event.PSObject.Properties.Name -contains "${prefix}PriceAmount"
    $hasCurrency = $event.PSObject.Properties.Name -contains "${prefix}PriceCurrency"
    if (($hasAmount -and -not $hasCurrency) -or (-not $hasAmount -and $hasCurrency)) {
        $violations += "${prefix}Price* dual-field pair is split (amount present but currency missing, or vice versa)"
    }
}

# 6. paymentMode enum-string sanity (current accepted values)
$validPaymentModes = @('OnPlatformPaid', 'OffPlatformPaid', 'FreeAtVenue', 'FreeRegistration', 'ExternalRegistration')
if ($event.paymentMode -and $event.paymentMode -notin $validPaymentModes) {
    $violations += "paymentMode='$($event.paymentMode)' is not in the known set ($($validPaymentModes -join ', ')). New mode? Update the smoke."
}

# Report
$summary = "Smoke OK event-DetailPriceShape eventId=$EventId ticketPriceAmount=$($event.ticketPriceAmount) currency=$($event.ticketPriceCurrency) isFree=$($event.isFree) paymentMode=$($event.paymentMode)"

if ($violations.Count -gt 0) {
    Write-Host ''
    Write-Host "Smoke-EventDetailPriceShape FAILED - $($violations.Count) contract violation(s):" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    Write-Host ''
    Write-Error 'Event-detail price-shape contract regressed. The W11 frontend will break against this response.'
    exit 1
}

Write-Host $summary -ForegroundColor Green
exit 0
