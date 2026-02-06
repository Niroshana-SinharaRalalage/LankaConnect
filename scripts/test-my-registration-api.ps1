# Test my-registration API endpoint - Phase 6A.47 verification

$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Step 1: Login
Write-Host "Step 1: Authenticating..." -ForegroundColor Cyan
$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.accessToken
    Write-Host "Authentication successful" -ForegroundColor Green
    Write-Host "Token (first 50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Gray
    Write-Host "User ID: $($loginResponse.userId)" -ForegroundColor Gray
    Write-Host "User Email: $($loginResponse.email)" -ForegroundColor Gray
} catch {
    Write-Host "Authentication failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Test my-registration endpoint
Write-Host "`nStep 2: Testing my-registration endpoint..." -ForegroundColor Cyan
$eventId = "9e3722f5-c255-4dcc-b167-afef56bc5592"

try {
    $result = Invoke-WebRequest -Uri "$baseUrl/api/Events/$eventId/my-registration" -Method Get -Headers @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    } -UseBasicParsing

    Write-Host "SUCCESS - HTTP $($result.StatusCode)" -ForegroundColor Green
    $data = $result.Content | ConvertFrom-Json
    Write-Host "Registration ID: $($data.id)" -ForegroundColor Yellow
    Write-Host "Status: $($data.status)" -ForegroundColor Yellow
    Write-Host "Payment Status: $($data.paymentStatus)" -ForegroundColor Yellow
    Write-Host "Attendees Count: $($data.attendees.Count)" -ForegroundColor Yellow
    Write-Host "`nPhase 6A.47 Fix VERIFIED - AsNoTracking works correctly!" -ForegroundColor Green
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "FAILED - HTTP $statusCode" -ForegroundColor Red

    if ($statusCode -eq 401) {
        Write-Host "Token authentication issue - checking if user has registration..." -ForegroundColor Yellow
    } elseif ($statusCode -eq 404) {
        Write-Host "No registration found for this event (expected if user not registered)" -ForegroundColor Yellow
    } elseif ($statusCode -eq 500) {
        Write-Host "CRITICAL: Still getting 500 error - AsNoTracking fix did NOT work!" -ForegroundColor Red
    }

    Write-Host "Error details: $_" -ForegroundColor Red
}
