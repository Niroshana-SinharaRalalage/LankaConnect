# Test UPDATE without changing email groups (just update title)

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

# Create event WITH email groups
$createBody = @{
    title = "Original Title"
    description = "Original description"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id)
} | ConvertTo-Json

Write-Host "`nCreating event..." -ForegroundColor Yellow
$eventId = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createBody
Write-Host "Event ID: $eventId" -ForegroundColor Cyan

# Verify creation
$initialEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers
Write-Host "Initial title: $($initialEvent.title)" -ForegroundColor White
Write-Host "Initial email groups: $($initialEvent.emailGroupIds.Count)" -ForegroundColor White

# Update WITHOUT touching email groups
$updateBody = @{
    title = "UPDATED Title Only"
    description = "Original description"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    capacity = 50
    category = 0
    # NO emailGroupIds field
} | ConvertTo-Json

try {
    Write-Host "`nUpdating title only (no email groups in payload)..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $updateBody

    $updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers
    Write-Host "`nUpdated title: $($updatedEvent.title)" -ForegroundColor White
    Write-Host "Email groups after update: $($updatedEvent.emailGroupIds.Count)" -ForegroundColor White

    if ($updatedEvent.title -eq "UPDATED Title Only" -and $updatedEvent.emailGroupIds.Count -eq 1) {
        Write-Host "✅ SUCCESS - UPDATE works when not touching email groups!" -ForegroundColor Green
    } else {
        Write-Host "❌ FAILURE!" -ForegroundColor Red
    }
} catch {
    Write-Host "`n❌ UPDATE failed!" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}
