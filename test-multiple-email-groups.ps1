# Test PUT with multiple email groups

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken

Write-Host "✅ Logged in: $($loginResponse.user.email)" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get user's email groups
$emailGroups = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/EmailGroups' -Method Get -Headers $headers

Write-Host "`nAvailable Email Groups:" -ForegroundColor Cyan
foreach ($group in $emailGroups) {
    Write-Host "  - $($group.name) (ID: $($group.id))" -ForegroundColor White
}

# Use an event ID we know exists
$eventId = "d9fa9a8e-2b54-47b2-bb24-09ee6f8dd656"

# Get event details
$event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

Write-Host "`nEvent: $($event.title)" -ForegroundColor Cyan

# Prepare update with multiple email groups (use first 2-3 groups)
$groupIds = $emailGroups | Select-Object -First 3 | ForEach-Object { $_.id }

$updateBody = @{
    eventId = $event.id
    title = $event.title
    description = $event.description
    startDate = $event.startDate
    endDate = $event.endDate
    capacity = $event.capacity
    category = $event.category
    emailGroupIds = $groupIds
}

Write-Host "`nUpdating with $($groupIds.Count) email groups:" -ForegroundColor Yellow
foreach ($id in $groupIds) {
    $groupName = ($emailGroups | Where-Object { $_.id -eq $id }).name
    Write-Host "  - $groupName" -ForegroundColor White
}

$jsonBody = $updateBody | ConvertTo-Json -Depth 3

try {
    $response = Invoke-WebRequest -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $jsonBody -UseBasicParsing

    Write-Host "`n✅ Update successful! Status: $($response.StatusCode)" -ForegroundColor Green

    # Verify persistence
    Start-Sleep -Seconds 2
    $updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

    Write-Host "`nVerification:" -ForegroundColor Cyan
    Write-Host "Email Group IDs Count: $($updatedEvent.emailGroupIds.Count)" -ForegroundColor White
    if ($updatedEvent.emailGroupIds -and $updatedEvent.emailGroupIds.Count -gt 0) {
        Write-Host "✅ Email groups persisted successfully!" -ForegroundColor Green
        Write-Host "`nAssigned Email Groups:" -ForegroundColor Cyan
        foreach ($group in $updatedEvent.emailGroups) {
            Write-Host "  - $($group.name) (ID: $($group.id))" -ForegroundColor White
        }
    } else {
        Write-Host "❌ Email groups NOT persisted" -ForegroundColor Red
    }

} catch {
    Write-Host "`n❌ Update failed!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.ErrorDetails.Message)" -ForegroundColor White
}
