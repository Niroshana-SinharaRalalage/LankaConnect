# PowerShell script to test LankaConnect API endpoints
Write-Host "=== LankaConnect API Endpoint Testing ===" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"
$headers = @{ 'Content-Type' = 'application/json' }

# Test Health endpoint first
Write-Host "`n1. Testing Health Endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 10
    Write-Host "✅ Health check passed" -ForegroundColor Green
    Write-Host "Status: $($healthResponse.Status)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Is the API running on port 5000?" -ForegroundColor Yellow
    exit 1
}

# Test GET /api/businesses (empty list is OK)
Write-Host "`n2. Testing GET /api/businesses..." -ForegroundColor Yellow
try {
    $businessesResponse = Invoke-RestMethod -Uri "$baseUrl/api/businesses" -Method Get -Headers $headers -TimeoutSec 10
    Write-Host "✅ GET /api/businesses successful" -ForegroundColor Green
    Write-Host "Total businesses: $($businessesResponse.TotalCount)" -ForegroundColor Gray
} catch {
    Write-Host "❌ GET /api/businesses failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

# Test POST /api/businesses (create test business)
Write-Host "`n3. Testing POST /api/businesses..." -ForegroundColor Yellow
$testBusiness = @{
    name = "Test Sri Lankan Restaurant"
    description = "A test restaurant serving authentic Sri Lankan cuisine"
    contactPhone = "+1-555-0123"
    contactEmail = "test@srilankanrest.com"
    website = "https://srilankanrest.com"
    address = "123 Main Street"
    city = "New York"
    province = "NY"
    postalCode = "10001"
    latitude = 40.7589
    longitude = -73.9851
    category = "Restaurant"
    ownerId = "00000000-0000-0000-0000-000000000001"
    categories = @("Restaurant", "Sri Lankan")
    tags = @("spicy", "authentic", "curry")
} | ConvertTo-Json

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/businesses" -Method Post -Headers $headers -Body $testBusiness -TimeoutSec 10
    Write-Host "✅ POST /api/businesses successful" -ForegroundColor Green
    Write-Host "Created Business ID: $($createResponse.BusinessId)" -ForegroundColor Gray
    $createdBusinessId = $createResponse.BusinessId
} catch {
    Write-Host "❌ POST /api/businesses failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        # Try to read the error response
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseText = $reader.ReadToEnd()
            Write-Host "Response: $responseText" -ForegroundColor Red
        } catch { }
    }
}

# Test GET /api/businesses/{id} if we created one
if ($createdBusinessId) {
    Write-Host "`n4. Testing GET /api/businesses/{id}..." -ForegroundColor Yellow
    try {
        $businessResponse = Invoke-RestMethod -Uri "$baseUrl/api/businesses/$createdBusinessId" -Method Get -Headers $headers -TimeoutSec 10
        Write-Host "✅ GET /api/businesses/{id} successful" -ForegroundColor Green
        Write-Host "Business Name: $($businessResponse.name)" -ForegroundColor Gray
    } catch {
        Write-Host "❌ GET /api/businesses/{id} failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test GET /api/businesses/search
Write-Host "`n5. Testing GET /api/businesses/search..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-RestMethod -Uri "$baseUrl/api/businesses/search?searchTerm=restaurant" -Method Get -Headers $headers -TimeoutSec 10
    Write-Host "✅ GET /api/businesses/search successful" -ForegroundColor Green
    Write-Host "Search results: $($searchResponse.TotalCount)" -ForegroundColor Gray
} catch {
    Write-Host "❌ GET /api/businesses/search failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    }
}

# Test Swagger documentation
Write-Host "`n6. Testing Swagger UI availability..." -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -Method Get -TimeoutSec 10
    if ($swaggerResponse.StatusCode -eq 200) {
        Write-Host "✅ Swagger UI available" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ Swagger UI not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== API Testing Complete ===" -ForegroundColor Cyan
Write-Host "Note: Some failures are expected if database is not set up or API is not running." -ForegroundColor Yellow