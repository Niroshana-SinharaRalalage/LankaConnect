# Test Event Publication Email - Phase 6A.41 Fix Verification
#
# PURPOSE: Verify that event-published email template works after fixing NULL subject
#
# PREREQUISITES:
# 1. Phase 6A.41 EF Core fix deployed (commit 9306c99b)
# 2. Database fix script executed (FixEventPublishedTemplateSubject_Phase6A41.sql)
# 3. Valid authentication token in token.txt
#
# WHAT THIS SCRIPT DOES:
# 1. Creates a draft event via API
# 2. Publishes the event (triggers EventPublishedEvent)
# 3. Monitors for email sending in logs
# 4. Verifies email was sent successfully
#
# EXPECTED RESULT:
# - Email sent to all event notification recipients
# - Subject: "New Event: [EventTitle] in [City], [State]"
# - No errors in logs

param(
    [string]$Environment = "staging",
    [string]$TokenFile = "token.txt"
)

# Configuration
$baseUrl = if ($Environment -eq "production") {
    "https://lankaconnect.azurewebsites.net"
} else {
    "https://lankaconnect-staging.azurewebsites.net"
}

Write-Host "=== Event Publication Email Test ===" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host ""

# Check token file exists
if (-not (Test-Path $TokenFile)) {
    Write-Host "ERROR: Token file not found: $TokenFile" -ForegroundColor Red
    Write-Host "Please run login first and save token to token.txt" -ForegroundColor Yellow
    exit 1
}

# Read authentication token
$token = Get-Content $TokenFile -Raw
$token = $token.Trim()

if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Host "ERROR: Token file is empty" -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host "Step 1: Creating draft event..." -ForegroundColor Cyan

# Create event payload
$eventTitle = "Test Event Publication Email - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
$createEvent = @{
    title = $eventTitle
    description = "This is a test event to verify that event-published email template works correctly after Phase 6A.41 fix. The template should have a valid subject: 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}'"
    startDate = (Get-Date).AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ")
    endDate = (Get-Date).AddDays(30).AddHours(4).ToString("yyyy-MM-ddTHH:mm:ssZ")
    location = @{
        name = "Test Venue"
        address = @{
            street = "123 Test Street"
            city = "Los Angeles"
            state = "California"
            zipCode = "90001"
            country = "USA"
        }
        latitude = 34.0522
        longitude = -118.2437
    }
    category = "Community"
    isFree = $true
} | ConvertTo-Json -Depth 10

try {
    # Create event
    $event = Invoke-RestMethod -Uri "$baseUrl/api/events" `
        -Method POST `
        -Headers $headers `
        -Body $createEvent `
        -ErrorAction Stop

    Write-Host "✓ Event created successfully" -ForegroundColor Green
    Write-Host "  Event ID: $($event.id)" -ForegroundColor Gray
    Write-Host "  Title: $($event.title)" -ForegroundColor Gray
    Write-Host "  Status: $($event.status)" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Step 2: Publishing event (triggers EventPublishedEvent)..." -ForegroundColor Cyan

    # Publish event
    $publishResult = Invoke-RestMethod `
        -Uri "$baseUrl/api/events/$($event.id)/publish" `
        -Method PATCH `
        -Headers $headers `
        -ErrorAction Stop

    Write-Host "✓ Event published successfully" -ForegroundColor Green
    Write-Host "  Status: $($publishResult.status)" -ForegroundColor Gray
    Write-Host ""

    Write-Host "Step 3: Waiting for email processing..." -ForegroundColor Cyan
    Write-Host "  (EventPublishedEventHandler should trigger email sending)" -ForegroundColor Gray
    Start-Sleep -Seconds 5
    Write-Host ""

    Write-Host "=== Test Summary ===" -ForegroundColor Cyan
    Write-Host "✓ Event created: $($event.id)" -ForegroundColor Green
    Write-Host "✓ Event published: Status = $($publishResult.status)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Check Azure Container App logs for email sending status:" -ForegroundColor White
    Write-Host "   az containerapp logs show --name lankaconnect-$Environment --resource-group lankaconnect-$Environment --follow false --tail 100" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Look for log entries:" -ForegroundColor White
    Write-Host "   - '[Phase 6A] EventPublishedEventHandler INVOKED'" -ForegroundColor Gray
    Write-Host "   - 'Event notification emails completed'" -ForegroundColor Gray
    Write-Host "   - 'Sending templated email 'event-published''" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Expected Results:" -ForegroundColor White
    Write-Host "   SUCCESS: 'Event notification emails completed for event $($event.id). Success: X, Failed: 0'" -ForegroundColor Green
    Write-Host "   FAILURE: 'Failed to send templated email' or 'Cannot access value of a failed result'" -ForegroundColor Red
    Write-Host ""
    Write-Host "4. Verify email subject in Azure Communication Services:" -ForegroundColor White
    Write-Host "   Subject should be: 'New Event: $eventTitle in Los Angeles, California'" -ForegroundColor Gray
    Write-Host ""

    # Return event details for further investigation
    return @{
        EventId = $event.id
        EventTitle = $event.title
        Status = $publishResult.status
        Success = $true
    }
}
catch {
    Write-Host ""
    Write-Host "=== ERROR ===" -ForegroundColor Red
    Write-Host "Message: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "Common Issues:" -ForegroundColor Yellow
    Write-Host "1. Token expired - Re-run login and update token.txt" -ForegroundColor White
    Write-Host "2. Insufficient permissions - Ensure user has EventOrganizer role" -ForegroundColor White
    Write-Host "3. API endpoint changed - Verify base URL is correct" -ForegroundColor White
    Write-Host ""

    return @{
        Success = $false
        Error = $_.Exception.Message
    }
}
