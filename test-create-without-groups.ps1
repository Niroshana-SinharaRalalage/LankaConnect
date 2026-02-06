# Test CREATE event WITHOUT email groups (regression test)

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

# Create event WITHOUT email groups
$createEventBody = @{
    title = "Test No Groups $(Get-Date -Format 'HH:mm:ss')"
    description = "Test event without email groups"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0
    # NO emailGroupIds field
} | ConvertTo-Json

Write-Host "`nCreating event WITHOUT email groups..." -ForegroundColor Yellow
$eventId = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createEventBody

Write-Host "Event ID: $eventId" -ForegroundColor Cyan

Write-Host "`nGetting event..." -ForegroundColor Yellow
$createdEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

Write-Host "Title: $($createdEvent.title)" -ForegroundColor White
Write-Host "Email Group IDs Count: $($createdEvent.emailGroupIds.Count)" -ForegroundColor White

if ($createdEvent.emailGroupIds.Count -eq 0) {
    Write-Host "✅ SUCCESS - Event created without email groups!" -ForegroundColor Green
} else {
    Write-Host "❌ FAILURE - Unexpected email groups found!" -ForegroundColor Red
}
