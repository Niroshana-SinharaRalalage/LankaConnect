# Test UPDATE on an old event (created before today)

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken

Write-Host "Logged in: $($loginResponse.user.email)" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get all events
$events = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Get -Headers $headers

# Find an event created before today
$oldEvent = $events | Where-Object { $_.title -like "*Monthly Dana*" } | Select-Object -First 1

if ($oldEvent) {
    Write-Host "`nFound event: $($oldEvent.title)" -ForegroundColor Cyan
    Write-Host "Event ID: $($oldEvent.id)" -ForegroundColor Cyan

    # Try to update it
    $updateBody = @{
        title = $oldEvent.title
        description = $oldEvent.description
        startDate = $oldEvent.startDate
        endDate = $oldEvent.endDate
        capacity = $oldEvent.capacity
        category = $oldEvent.category
    } | ConvertTo-Json

    try {
        Write-Host "`nUpdating event (no actual changes)..." -ForegroundColor Yellow
        Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($oldEvent.id)" -Method Put -Headers $headers -Body $updateBody
        Write-Host "✅ SUCCESS - UPDATE works on old events!" -ForegroundColor Green
    } catch {
        Write-Host "❌ UPDATE failed on old event too!" -ForegroundColor Red
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
} else {
    Write-Host "No old events found" -ForegroundColor Yellow
}
