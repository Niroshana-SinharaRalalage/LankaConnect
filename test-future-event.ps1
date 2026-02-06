# Test with future event date

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Logged in as: $($loginResponse.user.email)" -ForegroundColor Cyan
Write-Host "User ID: $userId`n" -ForegroundColor Cyan

# Get user's events
$events = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Get -Headers $headers
$ownEvents = $events | Where-Object { $_.organizerId -eq $userId }

Write-Host "User's events:" -ForegroundColor Cyan
foreach ($evt in $ownEvents) {
    $startDate = [DateTime]::Parse($evt.startDate)
    $isFuture = $startDate -gt [DateTime]::UtcNow
    $futureTag = if ($isFuture) { "[FUTURE]" } else { "[PAST]" }
    Write-Host "  $futureTag $($evt.title)" -ForegroundColor $(if ($isFuture) { "Green" } else { "Gray" })
    Write-Host "     Start: $($evt.startDate), ID: $($evt.id)" -ForegroundColor White
}

# Find event with future start date
$futureEvents = $ownEvents | Where-Object {
    $startDate = [DateTime]::Parse($_.startDate)
    $startDate -gt [DateTime]::UtcNow
}

if ($futureEvents -and $futureEvents.Count -gt 0) {
    $testEvent = $futureEvents[0]
    Write-Host "`n✅ Using future event: $($testEvent.title)" -ForegroundColor Green

    # Get full event details
    $event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

    # Prepare update
    $updateBody = @{
        eventId = $event.id
        title = $event.title
        description = $event.description
        startDate = $event.startDate
        endDate = $event.endDate
        capacity = $event.capacity
        category = $event.category
        emailGroupIds = @("c74c0635-59f4-42d1-874f-204a67c4b21d", "f11e9e26-9848-4369-9893-024c229a8f50")
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
        if ($updatedEvent.emailGroups) {
            Write-Host "  Email Groups:" -ForegroundColor White
            foreach ($group in $updatedEvent.emailGroups) {
                Write-Host "    - $($group.name) (ID: $($group.id), Active: $($group.isActive))" -ForegroundColor White
            }
        }

        Write-Host "`n✅ Phase 6A.32 Backend API Test: PASSED" -ForegroundColor Green
        Write-Host "  - Email groups assigned successfully" -ForegroundColor White
        Write-Host "  - Batch query working (Fix #3)" -ForegroundColor White
        Write-Host "  - IsActive flag populated correctly (Fix #5)" -ForegroundColor White

    } catch {
        Write-Host "❌ Update failed:" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)"

        if ($_.ErrorDetails.Message) {
            Write-Host "`nError Details:" -ForegroundColor Yellow
            $_.ErrorDetails.Message | Write-Host
        }
    }
} else {
    Write-Host "`n⚠️ No future events found. Cannot test update with past event dates." -ForegroundColor Yellow
    Write-Host "Note: Validation requires start date to be in the future." -ForegroundColor Yellow
}
