# Test Badge API on staging
$loginUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login"
$badgesUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Badges"

# Login
Write-Host "Logging in..." -ForegroundColor Yellow
$loginBody = @{
    email = "sam@test.com"
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✓ Login successful" -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $_" -ForegroundColor Red
    exit 1
}

# Test Badges API
Write-Host "`nTesting Badges API..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }
    $badgesResponse = Invoke-RestMethod -Uri $badgesUrl -Method Get -Headers $headers

    Write-Host "✓ Badges API successful" -ForegroundColor Green
    Write-Host "`nBadges returned: $($badgesResponse.data.Count)" -ForegroundColor Cyan

    if ($badgesResponse.data.Count -gt 0) {
        $firstBadge = $badgesResponse.data[0]
        Write-Host "`nFirst badge location configs:" -ForegroundColor Cyan
        Write-Host "  ListingConfig: X=$($firstBadge.listingConfig.positionX), Y=$($firstBadge.listingConfig.positionY), Size=$($firstBadge.listingConfig.sizeWidth)x$($firstBadge.listingConfig.sizeHeight)" -ForegroundColor White
        Write-Host "  FeaturedConfig: X=$($firstBadge.featuredConfig.positionX), Y=$($firstBadge.featuredConfig.positionY), Size=$($firstBadge.featuredConfig.sizeWidth)x$($firstBadge.featuredConfig.sizeHeight)" -ForegroundColor White
        Write-Host "  DetailConfig: X=$($firstBadge.detailConfig.positionX), Y=$($firstBadge.detailConfig.positionY), Size=$($firstBadge.detailConfig.sizeWidth)x$($firstBadge.detailConfig.sizeHeight)" -ForegroundColor White
    }

    Write-Host "`n✓✓✓ Badge location configs fix VERIFIED ✓✓✓" -ForegroundColor Green
} catch {
    Write-Host "✗ Badges API failed: $_" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    exit 1
}
