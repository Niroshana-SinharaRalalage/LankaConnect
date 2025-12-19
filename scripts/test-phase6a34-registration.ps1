# Phase 6A.34 Registration Test Script
# Tests domain event dispatching after entity state reset fix

$ErrorActionPreference = "Stop"

Write-Host "Phase 6A.34 Domain Event Dispatch Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
$EVENT_ID = "c1f182a9-c957-4a78-a0b2-085917a88900"
$EMAIL = "niroshhh@gmail.com"
$PASSWORD = "12!@qwASzx"

Write-Host "Step 1: Login and get auth token..." -ForegroundColor Yellow

$loginBody = @{
    email = $EMAIL
    password = $PASSWORD
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$API_BASE/api/Auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody

    $TOKEN = $loginResponse.accessToken
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Cancel existing registration (if any)..." -ForegroundColor Yellow

try {
    $headers = @{
        "Authorization" = "Bearer $TOKEN"
        "Accept" = "application/json"
    }

    Invoke-RestMethod -Uri "$API_BASE/api/Events/$EVENT_ID/rsvp" `
        -Method DELETE `
        -Headers $headers `
        -ErrorAction SilentlyContinue

    Write-Host "✓ Registration cancelled" -ForegroundColor Green
    Start-Sleep -Seconds 2
} catch {
    Write-Host "  (No existing registration or already cancelled)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Step 3: Create NEW registration..." -ForegroundColor Yellow
Write-Host "  This will test Phase 6A.34 fix (DetectChanges + no entity state reset)" -ForegroundColor Gray

$rsvpBody = @{
    quantity = 1
} | ConvertTo-Json

try {
    $rsvpResponse = Invoke-RestMethod -Uri "$API_BASE/api/Events/$EVENT_ID/rsvp" `
        -Method POST `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $rsvpBody

    Write-Host "✓ Registration created successfully!" -ForegroundColor Green
} catch {
    Write-Host "✗ Registration failed: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response | ConvertTo-Json)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 4: Check Azure logs for domain event dispatch..." -ForegroundColor Yellow
Write-Host "  Waiting 3 seconds for logs to appear..." -ForegroundColor Gray
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "Run this command to check logs:" -ForegroundColor Cyan
Write-Host "az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --type console --tail 100 --follow false | grep -E '(RsvpToEvent|RegistrationConfirmed|Phase 6A.24.*Found.*domain)'" -ForegroundColor White

Write-Host ""
Write-Host "Expected log sequence:" -ForegroundColor Cyan
Write-Host "  1. Processing RsvpToEventCommand" -ForegroundColor White
Write-Host "  2. [Phase 6A.24] Found 1 domain events to dispatch: RegistrationConfirmedEvent" -ForegroundColor White
Write-Host "  3. RegistrationConfirmedEventHandler invoked" -ForegroundColor White
Write-Host "  4. Email queued for delivery" -ForegroundColor White

Write-Host ""
Write-Host "Test complete! Check logs above for verification." -ForegroundColor Green
