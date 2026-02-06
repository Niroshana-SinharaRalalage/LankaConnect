# Test Badges API on staging with proper error details
$loginUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login"
$badgesUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Badges"

# Login
Write-Host "Logging in..." -ForegroundColor Yellow
$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri $loginUrl -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.accessToken
    Write-Host "✅ Login successful" -ForegroundColor Green
    Write-Host "User: $($loginResponse.user.email) ($($loginResponse.user.role))" -ForegroundColor Cyan
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    Write-Host "Error Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    exit 1
}

# Test Badges API
Write-Host "`nTesting Badges API..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $badgesResponse = Invoke-RestMethod -Uri $badgesUrl -Method Get -Headers $headers -ErrorAction Stop

    Write-Host "✅ Badges API successful - HTTP 200 OK" -ForegroundColor Green

    if ($badgesResponse -is [array]) {
        Write-Host "`nBadges count: $($badgesResponse.Count)" -ForegroundColor Cyan

        if ($badgesResponse.Count -gt 0) {
            $firstBadge = $badgesResponse[0]
            Write-Host "`nFirst badge:" -ForegroundColor Cyan
            Write-Host "  ID: $($firstBadge.id)" -ForegroundColor White
            Write-Host "  Name: $($firstBadge.name)" -ForegroundColor White
            Write-Host "  ListingConfig: X=$($firstBadge.listingConfig.positionX), Y=$($firstBadge.listingConfig.positionY)" -ForegroundColor White
            Write-Host "  FeaturedConfig: X=$($firstBadge.featuredConfig.positionX), Y=$($firstBadge.featuredConfig.positionY)" -ForegroundColor White
            Write-Host "  DetailConfig: X=$($firstBadge.detailConfig.positionX), Y=$($firstBadge.detailConfig.positionY)" -ForegroundColor White
        } else {
            Write-Host "`nNo badges found in database (empty array returned)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "`nResponse type: $($badgesResponse.GetType().Name)" -ForegroundColor Cyan
        Write-Host "Response: $($badgesResponse | ConvertTo-Json -Depth 3)" -ForegroundColor White
    }

    Write-Host "`n✅✅✅ Badge API 500 error is FIXED! ✅✅✅" -ForegroundColor Green

} catch {
    Write-Host "❌ Badges API failed!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Red

    if ($_.ErrorDetails.Message) {
        Write-Host "`nError Response Body:" -ForegroundColor Red
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }

    Write-Host "`n❌ The 500 error is STILL present. Need to investigate further." -ForegroundColor Red
    exit 1
}
