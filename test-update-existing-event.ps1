# Test UPDATE on an event that was created WITH email groups

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
    title = "Test Event for Update"
    description = "Created with 1 email group"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id)
} | ConvertTo-Json

Write-Host "`nCreating event WITH email group..." -ForegroundColor Yellow
$eventId = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createBody
Write-Host "Event ID: $eventId" -ForegroundColor Cyan

# Verify it was created with email groups
$createdEventData = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers
Write-Host "Initial email groups count: $($createdEventData.emailGroupIds.Count)" -ForegroundColor Cyan

# Now UPDATE to change email groups
$updateBody = @{
    title = "Updated Event Title"
    description = "Updated to have 2 email groups"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id, $emailGroups[1].id)
} | ConvertTo-Json

try {
    Write-Host "`nUpdating event to have 2 email groups..." -ForegroundColor Yellow
    Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $updateBody

    Write-Host "`nGetting updated event..." -ForegroundColor Yellow
    $updatedEventData = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

    Write-Host "Updated email groups count: $($updatedEventData.emailGroupIds.Count)" -ForegroundColor Cyan

    if ($updatedEventData.emailGroupIds.Count -eq 2) {
        Write-Host "✅ SUCCESS - UPDATE works!" -ForegroundColor Green
        foreach ($id in $updatedEventData.emailGroupIds) {
            Write-Host "  - $id" -ForegroundColor White
        }
    } else {
        Write-Host "❌ FAILURE - Expected 2 groups, got $($updatedEventData.emailGroupIds.Count)!" -ForegroundColor Red
    }
} catch {
    Write-Host "`n❌ UPDATE failed!" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Error: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}
