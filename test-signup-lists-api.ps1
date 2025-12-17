# Test signup lists API endpoint
$eventId = "89f8ef9f-af11-4b1a-8dec-b440faef9ad0"
$apiUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$eventId/signup-lists"

Write-Host "Fetching signup lists from: $apiUrl"
Write-Host ""

$response = Invoke-RestMethod -Uri $apiUrl -Method Get
$response | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== SUMMARY ==="
foreach ($list in $response) {
    Write-Host ""
    Write-Host "List: $($list.category)"
    Write-Host "  - hasMandatoryItems: $($list.hasMandatoryItems)"
    Write-Host "  - hasPreferredItems: $($list.hasPreferredItems)"
    Write-Host "  - hasSuggestedItems: $($list.hasSuggestedItems)"
    Write-Host "  - hasOpenItems: $($list.hasOpenItems)"
}
