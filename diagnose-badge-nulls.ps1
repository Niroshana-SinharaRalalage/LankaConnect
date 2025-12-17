# Diagnose Badge NULL values by attempting to query the API and checking detailed error
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

# Step 2: Test Badges API with detailed error
Write-Host "`nStep 2: Testing Badges API..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $response = Invoke-WebRequest -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Badges' -Method Get -Headers $headers -ErrorAction Stop

    Write-Host "✅ SUCCESS! HTTP $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    $response.Content

} catch {
    Write-Host "❌ FAILED! HTTP $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red

    # Get response body if available
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        $reader.Close()

        Write-Host "`nResponse Body:" -ForegroundColor Red
        Write-Host $responseBody -ForegroundColor White
    }

    Write-Host "`nFull Error Details:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor White

    if ($_.ErrorDetails.Message) {
        Write-Host "`nError Details Message:" -ForegroundColor Red
        Write-Host $_.ErrorDetails.Message -ForegroundColor White
    }
}

# Step 3: Try to get container logs if we have Azure CLI
Write-Host "`nStep 3: Attempting to get container logs..." -ForegroundColor Yellow
try {
    $logs = az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 50 2>&1
    Write-Host "Recent Container Logs:" -ForegroundColor Cyan
    Write-Host $logs -ForegroundColor White
} catch {
    Write-Host "⚠️ Could not retrieve container logs (Azure CLI might not be configured)" -ForegroundColor Yellow
}
