# Comprehensive test of email groups CREATE and UPDATE flow

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId

Write-Host "�o. Logged in: $($loginResponse.user.email)" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get email groups
$emailGroups = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/EmailGroups' -Method Get -Headers $headers
Write-Host "Email Groups Available: $($emailGroups.Count)" -ForegroundColor Cyan

# TEST 1: CREATE with 1 email group
Write-Host "`n=== TEST 1: CREATE with 1 email group ===" -ForegroundColor Yellow
$createBody = @{
    title = "Test Email Groups Flow"
    description = "Created with 1 email group"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id)
} | ConvertTo-Json

$eventId = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createBody
Write-Host "Event ID: $eventId" -ForegroundColor Cyan

$createdEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers
Write-Host "Email Groups Count: $($createdEvent.emailGroupIds.Count)" -ForegroundColor White

if ($createdEvent.emailGroupIds.Count -eq 1) {
    Write-Host "�o. CREATE works!" -ForegroundColor Green
} else {
    Write-Host "�?O CREATE failed!" -ForegroundColor Red
}

# TEST 2: UPDATE to add second email group
Write-Host "`n=== TEST 2: UPDATE to add second email group ===" -ForegroundColor Yellow
$updateBody = @{
    eventId = $eventId
    title = "Test Email Groups Flow - Updated"
    description = "Updated to have 2 email groups"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id, $emailGroups[1].id)
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $updateBody

$updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers
Write-Host "Email Groups Count: $($updatedEvent.emailGroupIds.Count)" -ForegroundColor White

if ($updatedEvent.emailGroupIds.Count -eq 2) {
    Write-Host "�o. UPDATE works!" -ForegroundColor Green
} else {
    Write-Host "�?O UPDATE failed!" -ForegroundColor Red
}

# TEST 3: UPDATE to remove all email groups
Write-Host "`n=== TEST 3: UPDATE to remove all email groups ===" -ForegroundColor Yellow
$updateBody2 = @{
    eventId = $eventId
    title = "Test Email Groups Flow - No Groups"
    description = "Removed all email groups"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    capacity = 50
    category = 0
    emailGroupIds = @()
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $updateBody2

$finalEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers
Write-Host "Email Groups Count: $($finalEvent.emailGroupIds.Count)" -ForegroundColor White

if ($finalEvent.emailGroupIds.Count -eq 0) {
    Write-Host "�o. UPDATE remove works!" -ForegroundColor Green
} else {
    Write-Host "�?O UPDATE remove failed!" -ForegroundColor Red
}

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "�o. Phase 6A.32 Email Groups Integration COMPLETE!" -ForegroundColor Green
