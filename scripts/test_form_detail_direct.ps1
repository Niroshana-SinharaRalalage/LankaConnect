# Test Form Detail API endpoint (used by form fill page)

$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
$eventId = "62bf37a7-08c5-49e9-84ad-2be388e26caa"
$formId = "ade5a7ac-748a-4b0d-a602-c26226010d59"

Write-Host "`n=== Testing Form Detail API (AllowAnonymous) ===`n" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/Events/$eventId/forms/$formId" -Method Get -ContentType "application/json" -ErrorAction Stop

    Write-Host "✅ SUCCESS`n" -ForegroundColor Green

    Write-Host "Form ID: $($response.id)" -ForegroundColor Gray
    Write-Host "Title: $($response.title)" -ForegroundColor Gray
    Write-Host "Status: $($response.status)" -ForegroundColor Gray
    Write-Host "Question Count: $($response.questions.Count)`n" -ForegroundColor Yellow

    if ($response.questions) {
        Write-Host "Questions:" -ForegroundColor Yellow
        foreach ($q in $response.questions) {
            Write-Host "  [$($q.sortOrder)] $($q.questionText)" -ForegroundColor Gray
            Write-Host "      Type: $($q.questionType), Required: $($q.isRequired)" -ForegroundColor DarkGray
            if ($q.options -and $q.options.Count -gt 0) {
                Write-Host "      Options: $($q.options.Count) options" -ForegroundColor DarkGray
            }
        }
    }

    Write-Host "`n=== Full JSON ===`n" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10

} catch {
    Write-Host "`n❌ API ERROR`n" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
