# Verification Script for Issue #78: Festival Filter Error
# This script checks if EventCategory values exist in the database

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Issue #78: Festival Filter Verification" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# API endpoint
$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "Fetching EventCategory reference data from API..." -ForegroundColor Yellow
Write-Host "GET $baseUrl/api/reference-data?types=EventCategory&activeOnly=true`n" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/reference-data?types=EventCategory&activeOnly=true" -Method Get

    Write-Host "✅ API Response Received" -ForegroundColor Green
    Write-Host "Total Categories: $($response.Count)`n" -ForegroundColor Green

    # Display all categories
    Write-Host "EventCategory Reference Data:" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ("| {0,-3} | {1,-15} | {2,-20} |" -f "Int", "Code", "Name") -ForegroundColor White
    Write-Host ("| {0,-3} | {1,-15} | {2,-20} |" -f "---", "---------------", "--------------------") -ForegroundColor DarkGray

    $response | Sort-Object -Property intValue | ForEach-Object {
        $color = if ($_.intValue -ge 8) { "Green" } else { "White" }
        Write-Host ("| {0,-3} | {1,-15} | {2,-20} |" -f $_.intValue, $_.code, $_.name) -ForegroundColor $color
    }
    Write-Host "========================================`n" -ForegroundColor Cyan

    # Check for specific categories
    Write-Host "Verification Results:" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    $expectedCategories = @(
        @{ Code = "Religious"; IntValue = 0 },
        @{ Code = "Cultural"; IntValue = 1 },
        @{ Code = "Community"; IntValue = 2 },
        @{ Code = "Educational"; IntValue = 3 },
        @{ Code = "Social"; IntValue = 4 },
        @{ Code = "Business"; IntValue = 5 },
        @{ Code = "Charity"; IntValue = 6 },
        @{ Code = "Entertainment"; IntValue = 7 },
        @{ Code = "Workshop"; IntValue = 8 },
        @{ Code = "Festival"; IntValue = 9 },
        @{ Code = "Ceremony"; IntValue = 10 },
        @{ Code = "Celebration"; IntValue = 11 }
    )

    $allFound = $true
    foreach ($expected in $expectedCategories) {
        $found = $response | Where-Object { $_.code -eq $expected.Code -and $_.intValue -eq $expected.IntValue }

        if ($found) {
            Write-Host "✅ $($expected.Code) (int_value=$($expected.IntValue)) - FOUND" -ForegroundColor Green
        } else {
            Write-Host "❌ $($expected.Code) (int_value=$($expected.IntValue)) - MISSING" -ForegroundColor Red
            $allFound = $false
        }
    }

    Write-Host "`n========================================" -ForegroundColor Cyan

    if ($allFound) {
        Write-Host "✅ All 12 EventCategory values exist in database" -ForegroundColor Green
        Write-Host "`nConclusion: Database has all required categories." -ForegroundColor Green
        Write-Host "The issue is ONLY with backend EventCategory enum missing 4 values (8-11)." -ForegroundColor Yellow
    } else {
        Write-Host "❌ Some EventCategory values are missing" -ForegroundColor Red
        Write-Host "`nConclusion: Database is missing categories." -ForegroundColor Red
        Write-Host "Both database AND backend enum need to be fixed." -ForegroundColor Yellow
    }

    # Test Festival filter
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Testing Festival Filter (category=9)" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    Write-Host "GET $baseUrl/api/events?category=9`n" -ForegroundColor Gray

    try {
        $eventsResponse = Invoke-RestMethod -Uri "$baseUrl/api/events?category=9" -Method Get -ErrorAction Stop
        Write-Host "✅ Festival filter API call succeeded" -ForegroundColor Green
        Write-Host "Events returned: $($eventsResponse.Count)" -ForegroundColor Green

        if ($eventsResponse.Count -eq 0) {
            Write-Host "⚠️  No Festival events found (empty result set)" -ForegroundColor Yellow
            Write-Host "This is expected if no events have category=9 in database." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ Festival filter API call FAILED" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }

} catch {
    Write-Host "❌ Failed to fetch reference data" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Verification Complete" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan
