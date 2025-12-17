# Test UPDATE event with email groups - show error details

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId

Write-Host "Logged in: $($loginResponse.user.email)" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get email groups
$emailGroups = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/EmailGroups' -Method Get -Headers $headers

# Create event first
$createBody = @{
    title = "Test Update $(Get-Date -Format 'HH:mm:ss')"
    description = "Will be updated"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0
} | ConvertTo-Json

Write-Host "`nCreating event..." -ForegroundColor Yellow
$eventId = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createBody
Write-Host "Event ID: $eventId" -ForegroundColor Cyan

# Update with email groups
$updateBody = @{
    title = "Updated Title"
    description = "Updated description"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id)
} | ConvertTo-Json

Write-Host "`nUpdate Payload:" -ForegroundColor Yellow
Write-Host $updateBody

try {
    Write-Host "`nUpdating event with email groups..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $updateBody

    Write-Host "`nGetting updated event..." -ForegroundColor Yellow
    $updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

    Write-Host "Title: $($updatedEvent.title)" -ForegroundColor White
    Write-Host "Email Group IDs Count: $($updatedEvent.emailGroupIds.Count)" -ForegroundColor White

    if ($updatedEvent.emailGroupIds.Count -gt 0) {
        Write-Host "✅ SUCCESS - UPDATE works!" -ForegroundColor Green
        foreach ($id in $updatedEvent.emailGroupIds) {
            Write-Host "  - $id" -ForegroundColor White
        }
    } else {
        Write-Host "❌ FAILURE - Email groups not persisted!" -ForegroundColor Red
    }
} catch {
    Write-Host "`n❌ UPDATE failed!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
}
