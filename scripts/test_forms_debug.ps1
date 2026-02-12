# Debug test for Custom Forms endpoint
$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"

# Login
$authResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method Post -ContentType "application/json" -Body @"
{
  "email": "niroshhh@gmail.com",
  "password": "1qaz!QAZ",
  "rememberMe": true,
  "ipAddress": "127.0.0.1"
}
"@

$token = $authResponse.accessToken
$headers = @{
    "Authorization" = "Bearer $token"
}

# Get events
$events = Invoke-RestMethod -Uri "$baseUrl/Events/my-events?page=1&pageSize=1" -Method Get -Headers $headers

Write-Host "Events type: $($events.GetType().Name)"
Write-Host "Events count: $($events.Count)"

if ($events.Count -gt 0) {
    $firstEvent = $events[0]
    Write-Host "`nFirst event type: $($firstEvent.GetType().Name)"
    Write-Host "First event properties:"
    $firstEvent.PSObject.Properties | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Value)"
    }

    # Try to access the id
    $eventId = $firstEvent.id
    Write-Host "`nEvent ID extracted: '$eventId'"

    if ($eventId) {
        Write-Host "`nTesting forms endpoint..."
        try {
            $forms = Invoke-RestMethod -Uri "$baseUrl/Events/$eventId/forms" -Method Get -Headers $headers
            Write-Host "SUCCESS! Found $($forms.Count) forms"
        } catch {
            Write-Host "ERROR: $($_.Exception.Message)"
        }
    }
}
