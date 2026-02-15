$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "=== FRESH SIGNUP COMMITMENT TEST ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

Write-Host "[1/3] Logging in..." -ForegroundColor Yellow
$loginBody = '{"email":"admin@lankaconnect.com","password":"Admin@2025!","rememberMe":true,"ipAddress":"127.0.0.1"}'
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId
Write-Host "      Logged in as: $($loginResponse.user.email)" -ForegroundColor Green
Write-Host "      User ID: $userId" -ForegroundColor Green
Write-Host ""

Write-Host "[2/3] Making signup commitment..." -ForegroundColor Yellow
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$signupId = "8567645d-7c71-4965-bec3-f696d266b597"
$itemId = "72639fc5-005f-415f-aa94-6326965e1590"

$now = Get-Date -Format "HH:mm:ss"
$commitBody = "{`"userId`":`"$userId`",`"quantity`":7,`"notes`":`"DEBUG TEST $now`",`"contactName`":`"Admin Manager`",`"contactEmail`":`"admin@lankaconnect.com`"}"

Write-Host "      Event: $eventId" -ForegroundColor Gray
Write-Host "      Signup: $signupId" -ForegroundColor Gray
Write-Host "      Item: $itemId" -ForegroundColor Gray
Write-Host "      Quantity: 7" -ForegroundColor Gray

$headers = @{ Authorization = "Bearer $token" }
$uri = "$baseUrl/api/Events/$eventId/signups/$signupId/items/$itemId/commit"

try {
    $commitResponse = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $commitBody -ContentType "application/json"
    Write-Host "      API Response: SUCCESS (HTTP 200)" -ForegroundColor Green
    Write-Host ""

    Write-Host "[3/3] Waiting 10 seconds for Azure logs..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    Write-Host "      Ready - check logs for DEBUG-CONTROLLER-ENTRY" -ForegroundColor Cyan
} catch {
    Write-Host "      API Response: FAILED" -ForegroundColor Red
    Write-Host "      Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
}
