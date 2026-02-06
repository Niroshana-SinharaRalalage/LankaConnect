# Test registration API script
$ErrorActionPreference = "Stop"

# Step 1: Get fresh token
$loginBody = Get-Content "c:\Work\LankaConnect\login_request.json" -Raw
Write-Host "Step 1: Logging in..."
$loginResponse = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body $loginBody

$token = $loginResponse.accessToken
Write-Host "Token obtained: $($token.Substring(0, 50))..."
$token | Out-File "c:\Work\LankaConnect\token.txt" -NoNewline

# Step 2: Test registration
$eventId = "68f675f1-327f-42a9-be9e-f66148d826c3"
$registrationBody = @{
    attendees = @(
        @{ name = "Test Attendee 1"; age = 30 }
    )
    email = "niroshhh2@gmail.com"
    phoneNumber = "+94771234567"
    successUrl = "https://lankaconnect-staging.azurewebsites.net/events/payment/success"
    cancelUrl = "https://lankaconnect-staging.azurewebsites.net/events/payment/cancel"
} | ConvertTo-Json -Depth 3

Write-Host "`nStep 2: Testing RSVP registration for event: $eventId"

try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
        "accept" = "application/json"
    }

    $response = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$eventId/rsvp" `
        -Method POST `
        -Headers $headers `
        -Body $registrationBody `
        -TimeoutSec 60

    Write-Host "`nSUCCESS! Registration response:"
    $response | ConvertTo-Json -Depth 5
} catch {
    Write-Host "`nERROR: $($_.Exception.Message)"
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)"
    }
    if ($_.Exception.Response) {
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response Body: $responseBody"
        } catch {}
    }
}
