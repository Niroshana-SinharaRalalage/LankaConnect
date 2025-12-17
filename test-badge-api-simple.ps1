# Simple Badge API test
$ErrorActionPreference = "Stop"

# Step 1: Login
Write-Host "Step 1: Logging in..." -ForegroundColor Yellow
$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
Write-Host "✅ Login successful" -ForegroundColor Green

# Step 2: Test Badges API
Write-Host "`nStep 2: Testing Badges API..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Badges' -Method Get -Headers $headers -ErrorAction Stop

    Write-Host "✅ SUCCESS! HTTP 200" -ForegroundColor Green
    Write-Host "Found $($response.Count) badges" -ForegroundColor Cyan

} catch {
    Write-Host "❌ FAILED! HTTP $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor White
}
