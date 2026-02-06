# Test UPDATE event with email groups - detailed error logging

# Login
$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get event
$eventId = "68f675f1-327f-42a9-be9e-f66148d826c3"
$event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

Write-Host "Event Details:" -ForegroundColor Cyan
$event | ConvertTo-Json -Depth 3 | Write-Host

# Prepare update payload with ALL required fields
$updateBody = @{
    eventId = $event.id
    title = $event.title
    description = $event.description
    startDate = $event.startDate
    endDate = $event.endDate
    capacity = $event.capacity
    category = $event.category
    emailGroupIds = @("c74c0635-59f4-42d1-874f-204a67c4b21d")  # Cleveland SL Community
}

# Add optional fields if they exist
if ($event.locationAddress) { $updateBody.locationAddress = $event.locationAddress }
if ($event.locationCity) { $updateBody.locationCity = $event.locationCity }
if ($event.locationState) { $updateBody.locationState = $event.locationState }
if ($event.locationZipCode) { $updateBody.locationZipCode = $event.locationZipCode }
if ($event.locationCountry) { $updateBody.locationCountry = $event.locationCountry }
if ($event.locationLatitude) { $updateBody.locationLatitude = $event.locationLatitude }
if ($event.locationLongitude) { $updateBody.locationLongitude = $event.locationLongitude }

Write-Host "`nUpdate Payload:" -ForegroundColor Cyan
$updateBody | ConvertTo-Json -Depth 2 | Write-Host

try {
    $jsonBody = $updateBody | ConvertTo-Json -Depth 3
    $response = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Put -Headers $headers -Body $jsonBody

    Write-Host "`n✅ Update successful!" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 2 | Write-Host
} catch {
    Write-Host "`n❌ Update failed:" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Error: $($_.Exception.Message)"

    if ($_.ErrorDetails.Message) {
        Write-Host "`nError Details:" -ForegroundColor Yellow
        try {
            $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
            $errorObj | ConvertTo-Json -Depth 3 | Write-Host
        } catch {
            Write-Host $_.ErrorDetails.Message
        }
    }
}
