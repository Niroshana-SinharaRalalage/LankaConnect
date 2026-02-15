# Test if Custom Forms API endpoint returns data for the specific event

$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
$eventId = "62bf37a7-08c5-49e9-84ad-2be388e26caa"

Write-Host "`n=== Testing Custom Forms API (No Auth Required) ===" -ForegroundColor Cyan
Write-Host "Event ID: $eventId" -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/Events/$eventId/forms" -Method Get -ContentType "application/json" -ErrorAction Stop

    if ($response -and $response.Count -gt 0) {
        Write-Host "`n✅ SUCCESS: API returned $($response.Count) form(s)" -ForegroundColor Green

        foreach ($form in $response) {
            Write-Host "`nForm Details:" -ForegroundColor Yellow
            Write-Host "  ID: $($form.id)" -ForegroundColor Gray
            Write-Host "  Title: $($form.title)" -ForegroundColor Gray
            Write-Host "  Status: $($form.status)" -ForegroundColor $(if ($form.status -eq "Active") { "Green" } else { "Yellow" })
            Write-Host "  Question Count: $($form.questionCount)" -ForegroundColor $(if ($form.questionCount -gt 0) { "Green" } else { "Red" })
            Write-Host "  Response Count: $($form.responseCount)" -ForegroundColor Gray
        }

        Write-Host "`n=== Full JSON ===" -ForegroundColor Cyan
        $response | ConvertTo-Json -Depth 5

    } else {
        Write-Host "`n⚠️  API returned empty array - No forms found" -ForegroundColor Yellow
    }

} catch {
    Write-Host "`n❌ API ERROR" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
