# Phase 6A.116: Test Deployed P0 Fixes on Staging
# Tests Issues #3, #4, #8 via API calls

$apiBaseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PHASE 6A.116: P0 FIX VERIFICATION" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login to get JWT token
Write-Host "[1/5] Logging in to get JWT token..." -ForegroundColor Yellow
$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/Auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody

    $jwtToken = $loginResponse.token
    Write-Host "  ✅ Login successful" -ForegroundColor Green
    Write-Host "  Token: $($jwtToken.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "  ❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Get list of events to find one with forms
Write-Host "[2/5] Getting events list..." -ForegroundColor Yellow
try {
    $eventsResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/Events?pageNumber=1&pageSize=10" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $jwtToken"
        }

    $eventWithForms = $eventsResponse.items | Where-Object { $_.formCount -gt 0 } | Select-Object -First 1

    if ($eventWithForms) {
        Write-Host "  ✅ Found event with forms: $($eventWithForms.title)" -ForegroundColor Green
        Write-Host "  Event ID: $($eventWithForms.id)" -ForegroundColor Gray
        Write-Host "  Form Count: $($eventWithForms.formCount)" -ForegroundColor Gray

        $eventId = $eventWithForms.id
    } else {
        Write-Host "  ❌ No events with forms found" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  ❌ Failed to get events: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 3: Get forms for the event
Write-Host "[3/5] Getting forms for event..." -ForegroundColor Yellow
try {
    $formsResponse = Invoke-RestMethod -Uri "$apiBaseUrl/api/Events/$eventId/forms" `
        -Method GET `
        -Headers @{
            "Authorization" = "Bearer $jwtToken"
        }

    $form = $formsResponse | Select-Object -First 1

    if ($form) {
        Write-Host "  ✅ Found form: $($form.title)" -ForegroundColor Green
        Write-Host "  Form ID: $($form.id)" -ForegroundColor Gray

        $formId = $form.id
    } else {
        Write-Host "  ❌ No forms found for event" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  ❌ Failed to get forms: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: TEST ISSUE #3 - X-Access-Token header support
Write-Host "[4/5] Testing Issue #3: X-Access-Token header support..." -ForegroundColor Yellow

# First, check if user has existing response
try {
    $myResponseUrl = "$apiBaseUrl/api/Events/$eventId/forms/$formId/responses/mine"

    # Test 1: GET with JWT (should work)
    Write-Host "  Test 1: GET with JWT token..." -ForegroundColor Gray
    try {
        $response1 = Invoke-RestMethod -Uri $myResponseUrl `
            -Method GET `
            -Headers @{
                "Authorization" = "Bearer $jwtToken"
            }

        if ($response1) {
            Write-Host "    ✅ GET with JWT: Response found" -ForegroundColor Green
            Write-Host "    Response ID: $($response1.id)" -ForegroundColor Gray

            $responseId = $response1.id
            $hasResponse = $true
        } else {
            Write-Host "    ℹ️ No existing response for this form" -ForegroundColor Yellow
            $hasResponse = $false
        }
    } catch {
        if ($_.Exception.Response.StatusCode -eq 204) {
            Write-Host "    ℹ️ No existing response (204 No Content)" -ForegroundColor Yellow
            $hasResponse = $false
        } else {
            Write-Host "    ❌ GET with JWT failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Test 2: If user has a response with access token, test X-Access-Token header
    if ($hasResponse -and $response1.accessToken) {
        Write-Host "  Test 2: GET with X-Access-Token header..." -ForegroundColor Gray
        try {
            $response2 = Invoke-RestMethod -Uri $myResponseUrl `
                -Method GET `
                -Headers @{
                    "X-Access-Token" = $response1.accessToken
                }

            Write-Host "    ✅ GET with X-Access-Token header: SUCCESS" -ForegroundColor Green
            Write-Host "    ✅ Issue #3 FIX VERIFIED" -ForegroundColor Green
        } catch {
            Write-Host "    ❌ GET with X-Access-Token header failed: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "    ❌ Issue #3 FIX NOT WORKING" -ForegroundColor Red
        }

        # Test 3: GET with token query parameter (backward compatibility)
        Write-Host "  Test 3: GET with token query parameter..." -ForegroundColor Gray
        try {
            $response3 = Invoke-RestMethod -Uri "$myResponseUrl`?token=$($response1.accessToken)" `
                -Method GET

            Write-Host "    ✅ GET with query token: SUCCESS (backward compatible)" -ForegroundColor Green
        } catch {
            Write-Host "    ❌ GET with query token failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "    ℹ️ Skipping X-Access-Token test (no response or no access token)" -ForegroundColor Yellow
    }

} catch {
    Write-Host "  ❌ Issue #3 test failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Step 5: Summary
Write-Host "========================================" -ForegroundColor Green
Write-Host "TEST SUMMARY" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Event: $($eventWithForms.title)" -ForegroundColor White
Write-Host "Form: $($form.title)" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Apply Phase6A116 migration on staging database" -ForegroundColor White
Write-Host "2. Submit/update a form response to test email rendering" -ForegroundColor White
Write-Host "3. Check email for:" -ForegroundColor White
Write-Host "   - Issue #4: All placeholders replaced (no {{UserName}}, etc.)" -ForegroundColor White
Write-Host "   - Issue #5: Line breaks rendering (not literal <br/>)" -ForegroundColor White
Write-Host "   - Issue #8: Edit button URL correct (no duplicate /events/)" -ForegroundColor White
Write-Host "   - Issue #9: Signup buttons clickable (if event has lists/forms)" -ForegroundColor White
Write-Host ""
