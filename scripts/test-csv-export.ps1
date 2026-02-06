# Test Attendees CSV Export API on Azure Staging
# Phase 6A.48: Verify UTF-8 BOM and phone number formatting

$eventId = "0458806b-8672-4ad5-a7cb-f5346f1b282a"
$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
$outputFile = "event-$eventId-attendees-test.csv"

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "Testing Attendees CSV Export API (Phase 6A.48B)" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Test endpoint
$endpoint = "$baseUrl/api/events/$eventId/export?format=csv"
Write-Host "Endpoint: $endpoint" -ForegroundColor Yellow
Write-Host ""

try {
    Write-Host "Downloading CSV file..." -ForegroundColor Yellow

    # Download the CSV file
    Invoke-WebRequest -Uri $endpoint -OutFile $outputFile -ErrorAction Stop

    Write-Host "✅ CSV file downloaded successfully" -ForegroundColor Green
    Write-Host "File: $outputFile" -ForegroundColor Green
    Write-Host ""

    # Check file size
    $fileInfo = Get-Item $outputFile
    Write-Host "File Size: $($fileInfo.Length) bytes" -ForegroundColor Yellow

    # Read raw bytes to check UTF-8 BOM
    $bytes = [System.IO.File]::ReadAllBytes($outputFile)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) {
        Write-Host "✅ UTF-8 BOM detected (EF BB BF)" -ForegroundColor Green
    } else {
        Write-Host "❌ UTF-8 BOM NOT found" -ForegroundColor Red
        Write-Host "First 3 bytes: $($bytes[0..2] -join ' ')" -ForegroundColor Yellow
    }

    # Read first 5 lines
    Write-Host ""
    Write-Host "First 5 lines of CSV:" -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Gray
    $content = Get-Content $outputFile -TotalCount 5
    $content | ForEach-Object { Write-Host $_ -ForegroundColor White }
    Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor Gray

    # Check for phone numbers
    Write-Host ""
    Write-Host "Checking phone number formatting..." -ForegroundColor Yellow
    $allContent = Get-Content $outputFile -Raw
    if ($allContent -match "'[0-9]{10}") {
        Write-Host "✅ Phone numbers formatted with apostrophe prefix" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Could not verify phone number formatting" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "✅ Test Complete - Please verify in Excel:" -ForegroundColor Green
    Write-Host "1. Open $outputFile in Excel" -ForegroundColor White
    Write-Host "2. Check special characters (—, é, ñ) display correctly" -ForegroundColor White
    Write-Host "3. Check phone numbers are text (left-aligned, no scientific notation)" -ForegroundColor White
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

} catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    }
}
