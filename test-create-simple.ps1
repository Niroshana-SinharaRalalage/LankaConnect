# Simple test to see CREATE response

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
Write-Host "Email Group ID: $($emailGroups[0].id)" -ForegroundColor Cyan

# Create event
$createEventBody = @{
    title = "Test $(Get-Date -Format 'HH:mm:ss')"
    description = "Test description"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0
    emailGroupIds = @($emailGroups[0].id)
} | ConvertTo-Json

Write-Host "`nCreating event..." -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createEventBody

Write-Host "`nCREATE Response:" -ForegroundColor Cyan
$response | ConvertTo-Json -Depth 5 | Write-Host

# Response is a GUID string directly, not an object
$eventId = $response

if ($eventId) {
    Write-Host "`nGetting event by ID: $eventId" -ForegroundColor Yellow
    $event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

    Write-Host "`nGET Response:" -ForegroundColor Cyan
    Write-Host "Title: $($event.title)" -ForegroundColor White
    Write-Host "Email Group IDs: $($event.emailGroupIds)" -ForegroundColor White
    Write-Host "Email Group IDs Count: $($event.emailGroupIds.Count)" -ForegroundColor White

    if ($event.emailGroupIds -and $event.emailGroupIds.Count -gt 0) {
        Write-Host "✅ SUCCESS - Email groups persisted!" -ForegroundColor Green
        foreach ($id in $event.emailGroupIds) {
            Write-Host "  - $id" -ForegroundColor White
        }
    } else {
        Write-Host "❌ FAILURE - Email groups NOT persisted!" -ForegroundColor Red
    }
} else {
    Write-Host "❌ No event ID in response!" -ForegroundColor Red
}
