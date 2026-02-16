# Test GET Events API with authentication
$response = Get-Content "c:/Work/LankaConnect/.login_response.json" | ConvertFrom-Json
$token = $response.accessToken
$userId = $response.user.userId

Write-Host "UserId: $userId"
Write-Host "Testing GET /api/Events with authentication..."

$headers = @{
    'Authorization' = "Bearer $token"
    'Accept' = 'application/json'
}

try {
    $events = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events?statusFilter=1' -Method GET -Headers $headers

    Write-Host "`nTotal events returned: $($events.Count)"
    Write-Host "`nFirst 3 events:"

    $events[0..2] | ForEach-Object {
        Write-Host "`n---"
        Write-Host "Event ID: $($_.id)"
        Write-Host "Title: $($_.title)"
        Write-Host "UserRegistrationStatus: $($_.userRegistrationStatus)"
        Write-Host "CurrentRegistrations: $($_.currentRegistrations)"
        Write-Host "Capacity: $($_.capacity)"
    }

    # Look for Christmas Dinner Dance specifically
    $christmasEvent = $events | Where-Object { $_.title -like "*Christmas*Dinner*" }
    if ($christmasEvent) {
        Write-Host "`n=== CHRISTMAS DINNER DANCE EVENT ==="
        Write-Host "Event ID: $($christmasEvent.id)"
        Write-Host "Title: $($christmasEvent.title)"
        Write-Host "UserRegistrationStatus: $($christmasEvent.userRegistrationStatus)"
        Write-Host "CurrentRegistrations: $($christmasEvent.currentRegistrations)"
        Write-Host "Capacity: $($christmasEvent.capacity)"
    }

} catch {
    Write-Host "Error: $_"
    Write-Host $_.Exception.Message
}
