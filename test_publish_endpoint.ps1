$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "=== TESTING PUBLISH ENDPOINT (POST) ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

Write-Host "Logging in..." -ForegroundColor Yellow
$loginBody = '{"email":"admin@lankaconnect.com","password":"Admin@2025!","rememberMe":true,"ipAddress":"127.0.0.1"}'
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.accessToken
Write-Host "Logged in: $($loginResponse.user.email)" -ForegroundColor Green
Write-Host ""

# Use a draft event (we need an unpublished event to test publish)
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"  # This is published, so publish will fail, but we'll see if endpoint is hit
$uri = "$baseUrl/api/Events/$eventId/publish"

Write-Host "Testing POST $uri" -ForegroundColor Yellow
$headers = @{ Authorization = "Bearer $token" }

try {
    $response = Invoke-WebRequest -Uri $uri -Method POST -Headers $headers -ContentType "application/json" -UseBasicParsing

    Write-Host "STATUS: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "CORRELATION-ID: $($response.Headers['x-correlation-id'])" -ForegroundColor Cyan

    Write-Host ""
    Write-Host "Waiting 5 seconds for logs..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    Write-Host "Correlation ID: $($response.Headers['x-correlation-id'])" -ForegroundColor Cyan
} catch {
    Write-Host "STATUS: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Yellow
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Yellow
    if ($_.Exception.Response) {
        $corrId = $_.Exception.Response.Headers['x-correlation-id']
        Write-Host "CORRELATION-ID: $corrId" -ForegroundColor Cyan

        Write-Host ""
        Write-Host "Waiting 5 seconds for logs..." -ForegroundColor Yellow
        Start-Sleep -Seconds 5

        Write-Host "Correlation ID: $corrId" -ForegroundColor Cyan
    }
}
