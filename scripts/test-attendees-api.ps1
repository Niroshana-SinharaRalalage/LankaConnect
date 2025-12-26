# Test attendees API endpoint - Diagnose HTTP 500 error

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
    Write-Host "User ID: $($loginResponse.userId)" -ForegroundColor Gray
} catch {
    Write-Host "Authentication failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Test attendees endpoint
Write-Host "`nStep 2: Testing attendees endpoint..." -ForegroundColor Cyan
$eventId = "0458806b-8672-4ad5-a7cb-f5346f1b282a"

try {
    $result = Invoke-WebRequest -Uri "$baseUrl/api/Events/$eventId/attendees" -Method Get -Headers @{
        "Authorization" = "Bearer $token"
        "Accept" = "application/json"
    } -UseBasicParsing

    Write-Host "SUCCESS - HTTP $($result.StatusCode)" -ForegroundColor Green
    $data = $result.Content | ConvertFrom-Json
    Write-Host "Total Registrations: $($data.totalRegistrations)" -ForegroundColor Yellow
    Write-Host "Total Attendees: $($data.totalAttendees)" -ForegroundColor Yellow
    Write-Host "Attendees Count: $($data.attendees.Count)" -ForegroundColor Yellow
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "FAILED - HTTP $statusCode" -ForegroundColor Red

    if ($statusCode -eq 500) {
        Write-Host "CRITICAL: HTTP 500 Internal Server Error!" -ForegroundColor Red
        # Try to get error details
        try {
            $errorBody = $_.ErrorDetails.Message
            Write-Host "`nError Details:" -ForegroundColor Yellow
            Write-Host $errorBody -ForegroundColor Red
        } catch {
            Write-Host "Could not retrieve error details" -ForegroundColor Yellow
        }
    } elseif ($statusCode -eq 403) {
        Write-Host "User is not authorized to view attendees (not event organizer)" -ForegroundColor Yellow
    } elseif ($statusCode -eq 404) {
        Write-Host "Event not found" -ForegroundColor Yellow
    }

    Write-Host "`nFull error: $_" -ForegroundColor Red
}
