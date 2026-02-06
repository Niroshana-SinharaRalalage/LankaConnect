# Test Rice Tay Fix - Verify commitments array is populated
# Run this after deployment completes

$API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
$EVENT_ID = "0458806b-8672-4ad5-a7cb-f5346f1b282a"
$TOKEN = Get-Content "token.txt" -Raw -ErrorAction SilentlyContinue

if (-not $TOKEN) {
    Write-Host "ERROR: token.txt not found. Please run login first." -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $TOKEN"
}

Write-Host "=== Testing Rice Tay Fix ===" -ForegroundColor Cyan
Write-Host "Fetching signup lists for Dana January 2026 event..." -ForegroundColor Yellow

$response = Invoke-RestMethod -Uri "$API_BASE/events/$EVENT_ID/signups" -Headers $headers 2>$null

$riceTayItem = $response |
    ForEach-Object { $_.items } |
    Where-Object { $_.itemDescription -eq "Rice Tay" } |
    Select-Object -First 1

if (-not $riceTayItem) {
    Write-Host "ERROR: Rice Tay item not found" -ForegroundColor Red
    exit 1
}

Write-Host "`nRice Tay Item:" -ForegroundColor Green
Write-Host "  Description: $($riceTayItem.itemDescription)"
Write-Host "  Quantity: $($riceTayItem.quantity)"
Write-Host "  Remaining: $($riceTayItem.remainingQuantity)"
Write-Host "  Committed Quantity: $($riceTayItem.committedQuantity)"
Write-Host "  Commitments Array Length: $($riceTayItem.commitments.Count)"

if ($riceTayItem.committedQuantity -eq 0) {
    Write-Host "`nℹ️  No commitments exist for Rice Tay (committedQuantity = 0)" -ForegroundColor Yellow
    Write-Host "   This is expected if commitments were cancelled" -ForegroundColor Yellow
    exit 0
}

if ($riceTayItem.commitments.Count -eq 0 -and $riceTayItem.committedQuantity -gt 0) {
    Write-Host "`n❌ TEST FAILED" -ForegroundColor Red
    Write-Host "   committedQuantity = $($riceTayItem.committedQuantity)" -ForegroundColor Red
    Write-Host "   commitments.length = 0" -ForegroundColor Red
    Write-Host "   FIX DID NOT WORK - commitments array is still empty!" -ForegroundColor Red
    exit 1
}

if ($riceTayItem.commitments.Count -gt 0) {
    Write-Host "`n✅ TEST PASSED" -ForegroundColor Green
    Write-Host "   Commitments array is populated!" -ForegroundColor Green
    Write-Host "`nCommitment Details:" -ForegroundColor Cyan
    $riceTayItem.commitments | ForEach-Object {
        $name = if ($_.contactName) { $_.contactName } else { "NULL" }
        Write-Host "  - $name (Qty: $($_.quantity), User: $($_.userId.Substring(0,8))...)" -ForegroundColor White
    }
    exit 0
}
