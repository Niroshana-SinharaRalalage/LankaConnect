# Test both cancellation scenarios for Phase 6A.28
# Scenario 1: Cancel registration but KEEP signup commitments (default)
# Scenario 2: Cancel registration AND DELETE signup commitments

$eventId = "89f8ef9f-af11-4b1a-8dec-b440faef9ad0"
$apiUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Read token from file
$token = Get-Content -Path "token_staging.txt" -Raw
$token = $token.Trim()

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Phase 6A.28 - Cancel RSVP Test Scenarios" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Before cancellation - check current signup commitments
Write-Host "[BEFORE CANCELLATION] Checking current signup commitments..." -ForegroundColor Yellow
$signupListsBefore = Invoke-RestMethod -Uri "$apiUrl/api/events/$eventId/signup-lists" -Headers @{
    "Authorization" = "Bearer $token"
}

Write-Host "Current commitments:" -ForegroundColor Green
foreach ($list in $signupListsBefore) {
    Write-Host "  Category: $($list.category)" -ForegroundColor Cyan
    foreach ($item in $list.items) {
        $userCommitments = $item.commitments | Where-Object { $_.userId -eq "5e782b4d-29ed-4e1d-9039-6c8f698aeea9" }
        if ($userCommitments) {
            Write-Host "    - $($item.itemDescription): Quantity=$($userCommitments.quantity), Remaining=$($item.remainingQuantity)" -ForegroundColor White
        }
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Scenario 1: Cancel WITHOUT deleting commitments (default)" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

try {
    $response1 = Invoke-RestMethod -Uri "$apiUrl/api/events/$eventId/rsvp" -Method Delete -Headers @{
        "Authorization" = "Bearer $token"
    }
    Write-Host "✅ Cancellation successful (kept commitments)" -ForegroundColor Green

    # Check signup lists after cancellation
    Write-Host ""
    Write-Host "[AFTER CANCELLATION] Checking signup commitments..." -ForegroundColor Yellow
    $signupListsAfter1 = Invoke-RestMethod -Uri "$apiUrl/api/events/$eventId/signup-lists" -Headers @{
        "Authorization" = "Bearer $token"
    }

    Write-Host "Commitments after Scenario 1:" -ForegroundColor Green
    foreach ($list in $signupListsAfter1) {
        Write-Host "  Category: $($list.category)" -ForegroundColor Cyan
        foreach ($item in $list.items) {
            $userCommitments = $item.commitments | Where-Object { $_.userId -eq "5e782b4d-29ed-4e1d-9039-6c8f698aeea9" }
            if ($userCommitments) {
                Write-Host "    - $($item.itemDescription): Quantity=$($userCommitments.quantity), Remaining=$($item.remainingQuantity)" -ForegroundColor White
            }
        }
    }

    Write-Host ""
    Write-Host "✅ EXPECTED: Commitments should STILL EXIST" -ForegroundColor Green
    Write-Host "✅ EXPECTED: remaining_quantity should NOT change" -ForegroundColor Green
} catch {
    Write-Host "❌ Error in Scenario 1: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Re-register for event before Scenario 2..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# You'll need to re-register manually or via API before running Scenario 2
Write-Host "⚠️ Please re-register for the event before testing Scenario 2" -ForegroundColor Yellow
Write-Host "Press any key after re-registering..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Scenario 2: Cancel WITH deleting commitments" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

try {
    $response2 = Invoke-RestMethod -Uri "$apiUrl/api/events/$eventId/rsvp?deleteSignUpCommitments=true" -Method Delete -Headers @{
        "Authorization" = "Bearer $token"
    }
    Write-Host "✅ Cancellation successful (deleted commitments)" -ForegroundColor Green

    # Check signup lists after cancellation with delete
    Write-Host ""
    Write-Host "[AFTER CANCELLATION WITH DELETE] Checking signup commitments..." -ForegroundColor Yellow
    $signupListsAfter2 = Invoke-RestMethod -Uri "$apiUrl/api/events/$eventId/signup-lists" -Headers @{
        "Authorization" = "Bearer $token"
    }

    Write-Host "Commitments after Scenario 2:" -ForegroundColor Green
    foreach ($list in $signupListsAfter2) {
        Write-Host "  Category: $($list.category)" -ForegroundColor Cyan
        foreach ($item in $list.items) {
            $userCommitments = $item.commitments | Where-Object { $_.userId -eq "5e782b4d-29ed-4e1d-9039-6c8f698aeea9" }
            if ($userCommitments) {
                Write-Host "    - $($item.itemDescription): Quantity=$($userCommitments.quantity), Remaining=$($item.remainingQuantity)" -ForegroundColor White
            } else {
                Write-Host "    - $($item.itemDescription): NO USER COMMITMENTS, Remaining=$($item.remainingQuantity)" -ForegroundColor Gray
            }
        }
    }

    Write-Host ""
    Write-Host "✅ EXPECTED: Commitments should be DELETED" -ForegroundColor Green
    Write-Host "✅ EXPECTED: remaining_quantity should be RESTORED" -ForegroundColor Green
} catch {
    Write-Host "❌ Error in Scenario 2: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
