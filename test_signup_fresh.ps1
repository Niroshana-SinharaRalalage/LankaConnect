# Fresh signup commitment test with debug logging check
$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Login
Write-Host "=== Logging in ===" -ForegroundColor Cyan
$loginBody = @{
    email = "admin@lankaconnect.com"
    password = "Admin@2025!"
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
$userId = $loginResponse.user.id

Write-Host "✓ Logged in as: $($loginResponse.user.email)" -ForegroundColor Green
Write-Host "✓ User ID: $userId" -ForegroundColor Green
Write-Host ""

# Make commitment
Write-Host "=== Making Fresh Signup Commitment ===" -ForegroundColor Cyan
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$signupId = "8567645d-7c71-4965-bec3-f696d266b597"
$itemId = "72639fc5-005f-415f-aa94-6326965e1590"

$commitBody = @{
    userId = $userId
    quantity = 5
    notes = "FRESH TEST - $(Get-Date -Format 'HH:mm:ss')"
    contactName = "Admin Manager"
    contactEmail = "admin@lankaconnect.com"
} | ConvertTo-Json

Write-Host "Event: $eventId"
Write-Host "Signup: $signupId"
Write-Host "Item: $itemId (Eggs)"
Write-Host "Quantity: 5"
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host ""

try {
    $headers = @{ Authorization = "Bearer $token" }
    $uri = "$baseUrl/api/Events/$eventId/signups/$signupId/items/$itemId/commit"

    $commitResponse = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $commitBody -ContentType "application/json"

    Write-Host "✓ API Response: SUCCESS" -ForegroundColor Green
    Write-Host $commitResponse
} catch {
    Write-Host "✗ API Response: FAILED" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Error: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "=== Waiting 5 seconds for logs ===" -ForegroundColor Yellow
Start-Sleep -Seconds 5
Write-Host "✓ Now check Azure logs for DEBUG-CONTROLLER-ENTRY" -ForegroundColor Cyan
