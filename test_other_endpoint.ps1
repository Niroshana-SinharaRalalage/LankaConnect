$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "=== TESTING OTHER EVENTSCONTROLLER ENDPOINT ===" -ForegroundColor Cyan
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host ""

Write-Host "[1/2] Logging in..." -ForegroundColor Yellow
$loginBody = '{"email":"admin@lankaconnect.com","password":"Admin@2025!","rememberMe":true,"ipAddress":"127.0.0.1"}'
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.accessToken
Write-Host "      Logged in as: $($loginResponse.user.email)" -ForegroundColor Green
Write-Host ""

Write-Host "[2/2] Getting event details..." -ForegroundColor Yellow
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$uri = "$baseUrl/api/Events/$eventId"

Write-Host "      GET $uri" -ForegroundColor Gray

$headers = @{ Authorization = "Bearer $token" }

try {
    $response = Invoke-WebRequest -Uri $uri -Method GET -Headers $headers -UseBasicParsing

    Write-Host "      STATUS: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "      CORRELATION-ID: $($response.Headers['x-correlation-id'])" -ForegroundColor Cyan
    Write-Host "      CONTENT-LENGTH: $($response.Headers['Content-Length'])" -ForegroundColor Gray

    Write-Host ""
    Write-Host "=== Waiting 5 seconds for logs ===" -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    Write-Host "      Correlation ID to search: $($response.Headers['x-correlation-id'])" -ForegroundColor Cyan
    Write-Host "      Check if this appears in Azure logs!" -ForegroundColor Cyan
} catch {
    Write-Host "      ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "      Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}
