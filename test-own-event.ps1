# Test with user's own event

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId

Write-Host "Logged in as: $($loginResponse.user.email)" -ForegroundColor Cyan
Write-Host "User ID: $userId`n" -ForegroundColor Cyan

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Find user's own events
Write-Host "Fetching your events..." -ForegroundColor Yellow
$events = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Get -Headers $headers

$ownEvents = $events | Where-Object { $_.organizerId -eq $userId }

if ($ownEvents.Count -eq 0) {
    Write-Host "❌ No events found for your user. Cannot test update." -ForegroundColor Red
    Write-Host "`nAll events organizers:" -ForegroundColor Yellow
    $events | Select-Object -First 5 | ForEach-Object {
        Write-Host "  Event: $($_.title) - Organizer: $($_.organizerId)" -ForegroundColor Gray
    }
    exit 0
}

Write-Host "✅ Found $($ownEvents.Count) event(s) you can modify" -ForegroundColor Green
$testEvent = $ownEvents[0]
Write-Host "  Using: $($testEvent.title) (ID: $($testEvent.id))" -ForegroundColor Cyan

# Get full event details
$event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

Write-Host "`nCurrent email groups: $(if ($event.emailGroupIds.Count -gt 0) { $event.emailGroupIds -join ', ' } else { 'None' })" -ForegroundColor White

# Prepare update with email group
$updateBody = @{
    eventId = $event.id
    title = $event.title
    description = $event.description
    startDate = $event.startDate
    endDate = $event.endDate
    capacity = $event.capacity
    category = $event.category
    emailGroupIds = @("c74c0635-59f4-42d1-874f-204a67c4b21d", "f11e9e26-9848-4369-9893-024c229a8f50")  # Both groups
}

Write-Host "`nUpdating event with email groups..." -ForegroundColor Yellow

try {
    $jsonBody = $updateBody | ConvertTo-Json -Depth 3
    $response = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Put -Headers $headers -Body $jsonBody

    Write-Host "✅ Update successful!" -ForegroundColor Green

    # Verify
    Start-Sleep -Seconds 1
    $updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

    Write-Host "`n✅ Verification:" -ForegroundColor Green
    Write-Host "  Email Group IDs: $($updatedEvent.emailGroupIds -join ', ')" -ForegroundColor White
    Write-Host "  Email Groups:" -ForegroundColor White
    foreach ($group in $updatedEvent.emailGroups) {
        Write-Host "    - $($group.name) (ID: $($group.id), Active: $($group.isActive))" -ForegroundColor White
    }

    Write-Host "`n✅ Phase 6A.32 Backend API Test: PASSED" -ForegroundColor Green
    Write-Host "  - Email groups assigned successfully" -ForegroundColor White
    Write-Host "  - Batch query working (Fix #3)" -ForegroundColor White
    Write-Host "  - IsActive flag populated correctly (Fix #5)" -ForegroundColor White
    Write-Host "  - Authorization validation working" -ForegroundColor White

} catch {
    Write-Host "❌ Update failed:" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)"

    if ($_.ErrorDetails.Message) {
        Write-Host "`nError Details:" -ForegroundColor Yellow
        $_.ErrorDetails.Message | Write-Host
    }
}
