# Test Forms List Endpoint
# Check if questionCount is correctly calculated

$apiBase = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
$eventId = "62bf37a7-08c5-49e9-84ad-2be388e26caa"

# Login to get token
$loginPayload = @{
    email = "niroshhh@gmail.com"
    password = "1qaz!QAZ"
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

Write-Host "Logging in..." -ForegroundColor Cyan
$loginResponse = Invoke-RestMethod -Uri "$apiBase/Auth/login" -Method Post -Body $loginPayload -ContentType "application/json"
$token = $loginResponse.accessToken

# Get forms list
Write-Host "`nFetching forms list..." -ForegroundColor Cyan
$headers = @{
    "Authorization" = "Bearer $token"
}

$formsList = Invoke-RestMethod -Uri "$apiBase/events/$eventId/forms" -Method Get -Headers $headers

Write-Host "`n=== FORMS LIST ===" -ForegroundColor Green
foreach ($form in $formsList) {
    Write-Host "`nForm: $($form.title)"
    Write-Host "  ID: $($form.id)"
    Write-Host "  Status: $($form.status)"
    Write-Host "  Question Count: $($form.questionCount)" -ForegroundColor $(if ($form.questionCount -eq 0) { "Red" } else { "Green" })
    Write-Host "  Response Count: $($form.responseCount)"
}

Write-Host "`n=== FULL JSON ===" -ForegroundColor Cyan
$formsList | ConvertTo-Json -Depth 5
