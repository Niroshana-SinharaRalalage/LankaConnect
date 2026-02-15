# Simple test to check if Custom Forms endpoints exist on staging

$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"

Write-Host "`n=== Testing Custom Forms Endpoint Availability ===" -ForegroundColor Cyan

# Step 1: Login
Write-Host "`n1. Authenticating..." -ForegroundColor Yellow
$authResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method Post -ContentType "application/json" -Body '{
  "email": "niroshhh@gmail.com",
  "password": "1qaz!QAZ",
  "rememberMe": true,
  "ipAddress": "127.0.0.1"
}'
$token = $authResponse.accessToken
Write-Host "✅ Authenticated as: $($authResponse.user.fullName)" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Step 2: Get a test event ID
Write-Host "`n2. Getting test event..." -ForegroundColor Yellow
$eventsResponse = Invoke-RestMethod -Uri "$baseUrl/Events/my-events?page=1&pageSize=10" -Method Get -Headers $headers
if ($eventsResponse.items.Count -eq 0) {
    Write-Host "❌ No events found. Cannot test forms API." -ForegroundColor Red
    exit 1
}
$event = $eventsResponse.items[0]
$eventId = $event.id
Write-Host "✅ Test Event: $($event.title)" -ForegroundColor Green
Write-Host "   Event ID: $eventId" -ForegroundColor Gray

# Step 3: Test if Forms endpoint exists (GET)
Write-Host "`n3. Testing GET /api/events/{id}/forms endpoint..." -ForegroundColor Yellow
try {
    $formsResponse = Invoke-RestMethod -Uri "$baseUrl/Events/$eventId/forms" -Method Get -Headers $headers
    Write-Host "✅ Forms endpoint EXISTS and is accessible" -ForegroundColor Green
    Write-Host "   Found $($formsResponse.Count) form(s) for this event" -ForegroundColor Gray

    if ($formsResponse.Count -gt 0) {
        Write-Host "`n   Existing forms:" -ForegroundColor Gray
        foreach ($form in $formsResponse) {
            Write-Host "   - $($form.title) (Status: $($form.status), Responses: $($form.responseCount))" -ForegroundColor Gray
        }
    }
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "❌ Forms endpoint NOT ACCESSIBLE" -ForegroundColor Red
    Write-Host "   Status Code: $statusCode" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red

    if ($statusCode -eq 404) {
        Write-Host "`n⚠️  This indicates the Custom Forms backend (Phases 1-4) has NOT been deployed to staging." -ForegroundColor Yellow
        Write-Host "   Action Required: Deploy backend changes from commit 45f3e674" -ForegroundColor Yellow
    } elseif ($statusCode -eq 405) {
        Write-Host "`n⚠️  Method Not Allowed - Endpoint may exist but routing is incorrect" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host "`n=== Endpoint Test Complete ===" -ForegroundColor Cyan
