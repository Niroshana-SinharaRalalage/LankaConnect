# Test signup lists API after cancelling registration
$eventId = "89f8ef9f-af11-4b1a-8dec-b440faef9ad0"
$apiUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Read token from file
$token = Get-Content -Path "token_staging.txt" -Raw
$token = $token.Trim()

Write-Host "Testing signup lists for event: $eventId"
Write-Host ""

# Fetch signup lists
$response = Invoke-RestMethod -Uri "$apiUrl/api/events/$eventId/signup-lists" -Headers @{
    "Authorization" = "Bearer $token"
}

Write-Host "=== SIGNUP LISTS RESPONSE ===" -ForegroundColor Green
$response | ConvertTo-Json -Depth 10

Write-Host ""
Write-Host "=== CHECKING FOR YOUR COMMITMENTS ===" -ForegroundColor Yellow
foreach ($list in $response) {
    Write-Host ""
    Write-Host "List: $($list.category)" -ForegroundColor Cyan
    foreach ($item in $list.items) {
        Write-Host "  Item: $($item.itemDescription)"
        Write-Host "    Commitments count: $($item.commitments.Count)"
        foreach ($commitment in $item.commitments) {
            Write-Host "      - UserId: $($commitment.userId)"
            Write-Host "        ContactName: $($commitment.contactName)"
            Write-Host "        Quantity: $($commitment.quantity)"
        }
    }
}
