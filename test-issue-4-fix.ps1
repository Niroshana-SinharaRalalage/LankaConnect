# Test Phase 6A.28 Issue 4 Fix - Open Items Deletion
# This script tests that Open Items are deleted when canceling registration with deleteSignUpCommitments=true

$API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
$TOKEN = Get-Content "token.txt" -Raw
$USER_ID = "5e782b4d-29ed-4e1d-9039-6c8f698aeea9"

$headers = @{
    "Authorization" = "Bearer $TOKEN"
    "Content-Type" = "application/json"
}

Write-Host "`n=== Phase 6A.28 Issue 4 - API Test ===" -ForegroundColor Cyan
Write-Host "Testing: Open Items deleted when canceling registration`n" -ForegroundColor Yellow

# Step 1: Find a free event to register for
Write-Host "[1] Finding a free event..." -ForegroundColor Green
$events = Invoke-RestMethod -Uri "$API_BASE/events" -Headers $headers -Method GET
$freeEvent = $events | Where-Object { $_.isFree -eq $true } | Select-Object -First 1

if (-not $freeEvent) {
    Write-Host "ERROR: No free events found" -ForegroundColor Red
    exit 1
}

Write-Host "    Using event: $($freeEvent.title)" -ForegroundColor Gray
Write-Host "    Event ID: $($freeEvent.id)" -ForegroundColor Gray

# Step 2: Register for the event
Write-Host "`n[2] Registering for event..." -ForegroundColor Green
$registerPayload = @{
    userId = $USER_ID
    quantity = 1
    attendees = @(@{ name = "Test User"; age = 30 })
    email = "niroshhh@gmail.com"
    phoneNumber = "+1234567890"
    address = "123 Test Street"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/rsvp" `
        -Headers $headers `
        -Method POST `
        -Body $registerPayload `
        -ErrorAction Stop
    Write-Host "    Registration successful" -ForegroundColor Gray
} catch {
    if ($_.Exception.Response.StatusCode -eq 204) {
        Write-Host "    Registration successful (204 No Content)" -ForegroundColor Gray
    } else {
        Write-Host "    ERROR: Registration failed - $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Step 3: Get signup lists
Write-Host "`n[3] Getting signup lists..." -ForegroundColor Green
$signupLists = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists" -Headers $headers -Method GET

if ($signupLists.Count -eq 0) {
    Write-Host "    No signup lists found for this event - cannot test Open Items" -ForegroundColor Yellow
    Write-Host "    Canceling registration and exiting..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/rsvp" -Headers $headers -Method DELETE | Out-Null
    exit 0
}

$openList = $signupLists | Where-Object { $_.category -eq "Open" } | Select-Object -First 1

if (-not $openList) {
    Write-Host "    No Open signup list found - cannot test" -ForegroundColor Yellow
    Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/rsvp" -Headers $headers -Method DELETE | Out-Null
    exit 0
}

Write-Host "    Found signup lists: $($signupLists.Count)" -ForegroundColor Gray
Write-Host "    Open list ID: $($openList.id)" -ForegroundColor Gray

# Step 4: Create 2 Open Items
Write-Host "`n[4] Creating Open Items..." -ForegroundColor Green
$item1Payload = @{
    itemDescription = "Test Open Item 1 - Issue 4"
    quantity = 5
    notes = "Testing deletion on cancel"
} | ConvertTo-Json

$item2Payload = @{
    itemDescription = "Test Open Item 2 - Issue 4"
    quantity = 3
    notes = "Testing deletion on cancel"
} | ConvertTo-Json

$item1 = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists/$($openList.id)/items" `
    -Headers $headers `
    -Method POST `
    -Body $item1Payload

$item2 = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists/$($openList.id)/items" `
    -Headers $headers `
    -Method POST `
    -Body $item2Payload

Write-Host "    Created item 1: $($item1.itemDescription)" -ForegroundColor Gray
Write-Host "    Created item 2: $($item2.itemDescription)" -ForegroundColor Gray

# Step 5: Commit to both items
Write-Host "`n[5] Committing to Open Items..." -ForegroundColor Green
$commitPayload = @{
    quantity = 1
    notes = "My test commitment"
} | ConvertTo-Json

Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists/$($openList.id)/items/$($item1.id)/commit" `
    -Headers $headers `
    -Method POST `
    -Body $commitPayload | Out-Null

Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists/$($openList.id)/items/$($item2.id)/commit" `
    -Headers $headers `
    -Method POST `
    -Body $commitPayload | Out-Null

Write-Host "    Committed to both items" -ForegroundColor Gray

# Step 6: Verify items exist BEFORE cancellation
Write-Host "`n[6] Verifying items exist (BEFORE cancellation)..." -ForegroundColor Green
$listsBeforeCancel = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists" -Headers $headers -Method GET
$openListBefore = $listsBeforeCancel | Where-Object { $_.category -eq "Open" } | Select-Object -First 1
$itemsBeforeCount = ($openListBefore.items | Where-Object { $_.createdByUserId -eq $USER_ID }).Count

Write-Host "    Open Items created by user: $itemsBeforeCount" -ForegroundColor Gray
if ($itemsBeforeCount -ne 2) {
    Write-Host "    ERROR: Expected 2 items, found $itemsBeforeCount" -ForegroundColor Red
    exit 1
}

# Step 7: Cancel registration WITH deleteSignUpCommitments=true
Write-Host "`n[7] Canceling registration WITH deleteSignUpCommitments=true..." -ForegroundColor Green
Write-Host "    *** THIS IS THE FIX BEING TESTED ***" -ForegroundColor Yellow

try {
    $cancelResponse = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/rsvp?deleteSignUpCommitments=true" `
        -Headers $headers `
        -Method DELETE `
        -ErrorAction Stop
    Write-Host "    Cancellation successful" -ForegroundColor Gray
} catch {
    if ($_.Exception.Response.StatusCode -eq 204) {
        Write-Host "    Cancellation successful (204 No Content)" -ForegroundColor Gray
    } else {
        Write-Host "    ERROR: Cancellation failed - $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Step 8: Verify Open Items DELETED (AFTER cancellation)
Write-Host "`n[8] Verifying Open Items DELETED (AFTER cancellation)..." -ForegroundColor Green
$listsAfterCancel = Invoke-RestMethod -Uri "$API_BASE/events/$($freeEvent.id)/signup-lists" -Headers $headers -Method GET
$openListAfter = $listsAfterCancel | Where-Object { $_.category -eq "Open" } | Select-Object -First 1
$itemsAfterCount = ($openListAfter.items | Where-Object { $_.createdByUserId -eq $USER_ID }).Count

Write-Host "    Open Items created by user: $itemsAfterCount" -ForegroundColor Gray

# THE TEST
if ($itemsAfterCount -eq 0) {
    Write-Host "`n=== TEST PASSED ===" -ForegroundColor Green
    Write-Host "✅ Open Items were DELETED as expected" -ForegroundColor Green
    Write-Host "✅ Fix is working correctly!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n=== TEST FAILED ===" -ForegroundColor Red
    Write-Host "❌ Expected 0 items, found $itemsAfterCount" -ForegroundColor Red
    Write-Host "❌ Open Items were NOT deleted" -ForegroundColor Red
    exit 1
}
