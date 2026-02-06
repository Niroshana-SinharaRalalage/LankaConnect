$baseUrl = 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api'
$eventId = '89f8ef9f-af11-4b1a-8dec-b440faef9ad0'

# Step 1: Login
Write-Host "=== Step 1: Login ==="
$loginBody = @{
    Email = 'niroshanaks@gmail.com'
    Password = 'Nirosh@12345'
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType 'application/json' -ErrorAction Stop
    $token = $loginResponse.data.token
    Write-Host "Login successful, token obtained"

    $headers = @{
        'Authorization' = "Bearer $token"
        'Content-Type' = 'application/json'
    }

    # Step 2: Get event details
    Write-Host "`n=== Step 2: Get Event Details ==="
    $eventData = Invoke-RestMethod -Uri "$baseUrl/events/$eventId" -Method Get -Headers $headers -ErrorAction Stop
    Write-Host "Event Title: $($eventData.data.title)"
    Write-Host "Event Status: $($eventData.data.status)"
    Write-Host "Event Location: $($eventData.data.location)"

    # Step 3: Cancel event
    Write-Host "`n=== Step 3: Cancel Event ==="
    $cancelBody = @{
        cancellationReason = 'Testing Phase 6A.63 - Event Cancellation Email System with comprehensive logging'
    } | ConvertTo-Json

    $cancelResponse = Invoke-RestMethod -Uri "$baseUrl/events/$eventId/cancel" -Method Post -Body $cancelBody -Headers $headers -ErrorAction Stop
    Write-Host "Cancel Success: $($cancelResponse.isSuccess)"

    # Step 4: Verify cancellation
    Write-Host "`n=== Step 4: Verify Cancellation ==="
    $verifyData = Invoke-RestMethod -Uri "$baseUrl/events/$eventId" -Method Get -Headers $headers -ErrorAction Stop
    Write-Host "Status after cancel: $($verifyData.data.status)"
    Write-Host "Cancellation reason: $($verifyData.data.cancellationReason)"

    if ($verifyData.data.status -eq 'Cancelled') {
        Write-Host "`nSUCCESS: Event cancelled successfully"
        Write-Host "`nExpected Email Recipients (6):"
        Write-Host "  1. test1@example.com"
        Write-Host "  2. test2@example.com"
        Write-Host "  3. test3@example.com"
        Write-Host "  4. niroshanaks@gmail.com"
        Write-Host "  5. niroshhh@gmail.com"
        Write-Host "  6. varunipw@gmail.com"
    }
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Response Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
