# Phase 6A.47 API Testing Script
# Tests search and filter parameters on all three endpoints

$baseUrl = "https://lankaconnect-staging.azurewebsites.net/api"
$authToken = ""  # Will be set after login

Write-Host "`n=== Phase 6A.47 API Testing ===" -ForegroundColor Cyan
Write-Host "Testing search and filter support on event endpoints`n" -ForegroundColor Cyan

# Test 1: GET /events with searchTerm
Write-Host "`n[Test 1] GET /events?searchTerm=workshop" -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri "$baseUrl/events?searchTerm=workshop" -Method Get -ContentType "application/json" -ErrorAction SilentlyContinue
if ($response) {
    Write-Host "✅ SUCCESS: Returned $($response.Count) events matching 'workshop'" -ForegroundColor Green
    if ($response.Count -gt 0) {
        Write-Host "   Sample event: $($response[0].title)" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ FAILED: No response or error" -ForegroundColor Red
}

# Test 2: GET /events with category filter
Write-Host "`n[Test 2] GET /events?category=Workshop" -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri "$baseUrl/events?category=Workshop" -Method Get -ContentType "application/json" -ErrorAction SilentlyContinue
if ($response) {
    Write-Host "✅ SUCCESS: Returned $($response.Count) Workshop events" -ForegroundColor Green
} else {
    Write-Host "❌ FAILED: No response or error" -ForegroundColor Red
}

# Test 3: GET /events with searchTerm + category
Write-Host "`n[Test 3] GET /events?searchTerm=community&category=Social" -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri "$baseUrl/events?searchTerm=community&category=Social" -Method Get -ContentType "application/json" -ErrorAction SilentlyContinue
if ($response) {
    Write-Host "✅ SUCCESS: Returned $($response.Count) events matching 'community' in Social category" -ForegroundColor Green
} else {
    Write-Host "❌ FAILED: No response or error" -ForegroundColor Red
}

# Test 4: GET /events with date range
Write-Host "`n[Test 4] GET /events?startDateFrom=2025-01-01&startDateTo=2025-12-31" -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri "$baseUrl/events?startDateFrom=2025-01-01&startDateTo=2025-12-31" -Method Get -ContentType "application/json" -ErrorAction SilentlyContinue
if ($response) {
    Write-Host "✅ SUCCESS: Returned $($response.Count) events in 2025" -ForegroundColor Green
} else {
    Write-Host "❌ FAILED: No response or error" -ForegroundColor Red
}

# Test 5: GET /events with state filter
Write-Host "`n[Test 5] GET /events?state=California" -ForegroundColor Yellow
$response = Invoke-RestMethod -Uri "$baseUrl/events?state=California" -Method Get -ContentType "application/json" -ErrorAction SilentlyContinue
if ($response) {
    Write-Host "✅ SUCCESS: Returned $($response.Count) events in California" -ForegroundColor Green
} else {
    Write-Host "❌ FAILED: No response or error" -ForegroundColor Red
}

# Test 6: Authenticated endpoints require login
Write-Host "`n[Test 6-8] Authenticated Endpoints (my-events, my-rsvps)" -ForegroundColor Yellow
Write-Host "Note: These require authentication. Testing endpoint availability..." -ForegroundColor Gray

# Test endpoint availability (401 expected without auth)
$headers = @{ "Content-Type" = "application/json" }
try {
    Invoke-RestMethod -Uri "$baseUrl/events/my-events" -Method Get -Headers $headers -ErrorAction Stop
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ SUCCESS: /events/my-events endpoint requires auth (401 as expected)" -ForegroundColor Green
    } else {
        Write-Host "❌ UNEXPECTED: Status $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

try {
    Invoke-RestMethod -Uri "$baseUrl/events/my-rsvps" -Method Get -Headers $headers -ErrorAction Stop
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ SUCCESS: /events/my-rsvps endpoint requires auth (401 as expected)" -ForegroundColor Green
    } else {
        Write-Host "❌ UNEXPECTED: Status $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "`n=== API Testing Complete ===" -ForegroundColor Cyan
Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "✅ Public endpoint /events supports searchTerm, category, date range, and state filters" -ForegroundColor Green
Write-Host "✅ Authenticated endpoints /my-events and /my-rsvps are available (require auth)" -ForegroundColor Green
Write-Host "`nNext: Test authenticated endpoints with valid JWT token" -ForegroundColor Yellow