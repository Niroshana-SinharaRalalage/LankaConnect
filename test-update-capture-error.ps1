# Test UPDATE with detailed error capture

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
    title = "Test Event"
    description = "Test description"
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

# Update with minimal payload (MUST include eventId!)
$updateBody = @{
    eventId = $eventId  # REQUIRED - matches route parameter
    title = "Updated Title"
    description = "Updated description"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    capacity = 50
    category = 0
} | ConvertTo-Json

try {
    Write-Host "`nUpdating event..." -ForegroundColor Yellow
    $response = Invoke-WebRequest -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $updateBody

    Write-Host "✅ SUCCESS - Status: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ UPDATE failed!" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red

    # Capture response body
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $reader.BaseStream.Position = 0
    $reader.DiscardBufferedData()
    $responseBody = $reader.ReadToEnd()

    Write-Host "`nResponse Body:" -ForegroundColor Yellow
    Write-Host $responseBody -ForegroundColor White

    try {
        $errorJson = $responseBody | ConvertFrom-Json
        Write-Host "`nParsed Error:" -ForegroundColor Cyan
        Write-Host "Title: $($errorJson.title)" -ForegroundColor White
        Write-Host "Detail: $($errorJson.detail)" -ForegroundColor White
        Write-Host "Status: $($errorJson.status)" -ForegroundColor White
    } catch {
        Write-Host "Could not parse as JSON" -ForegroundColor Gray
    }
}
