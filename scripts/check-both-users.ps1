# Token for niroshhh@gmail.com (the WORKING user - EventOrganizer)
$token1 = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoQGdtYWlsLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmlyb3NoYW5hIFNpbmhhcmEgUmFsYWxhZ2UiLCJyb2xlIjoiRXZlbnRPcmdhbml6ZXIiLCJmaXJzdE5hbWUiOiJOaXJvc2hhbmEiLCJsYXN0TmFtZSI6IlNpbmhhcmEgUmFsYWxhZ2UiLCJpc0FjdGl2ZSI6InRydWUiLCJqdGkiOiI2YzE3ZmEyMi1iZTNlLTQ4ZmItYTMyNy03ZjZmMDE3ZTk0MzEiLCJpYXQiOjE3NjYyOTY5NzQsIm5iZiI6MTc2NjI5Njk3NCwiZXhwIjoxNzY2Mjk4Nzc0LCJpc3MiOiJodHRwczovL2xhbmthY29ubmVjdC1hcGktc3RhZ2luZy5henVyZXdlYnNpdGVzLm5ldCIsImF1ZCI6Imh0dHBzOi8vbGFua2Fjb25uZWN0LXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQifQ.7KJQNk78Li2rPrhy-K7yDRn4qpCnUWI6Aqu2DUvc_oA"

$eventId = "0458806b-8672-4ad5-a7cb-f5346f1b282a"

$headers1 = @{
    "Authorization" = "Bearer $token1"
}

Write-Host "=========================================="
Write-Host "User: niroshhh@gmail.com (WORKING user - EventOrganizer)"
Write-Host "=========================================="

# Check RSVPs for user 1
Write-Host "`n--- My RSVPs ---"
try {
    $rsvps = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/my-rsvps" -Method GET -Headers $headers1
    Write-Host "Total RSVPs: $($rsvps.Count)"
    foreach ($rsvp in $rsvps) {
        Write-Host "  - Event: $($rsvp.title) (ID: $($rsvp.id))"
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}

# Check specific event registration
Write-Host "`n--- Registration for event $eventId ---"
try {
    $reg = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$eventId/my-registration" -Method GET -Headers $headers1
    $reg | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode"
    }
}

# Get event details
Write-Host "`n--- Event Details ---"
try {
    $event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$eventId" -Method GET -Headers $headers1
    Write-Host "Title: $($event.title)"
    Write-Host "Is Free: $($event.isFree)"
    Write-Host "Current Registrations: $($event.currentRegistrations)"
    Write-Host "Max Capacity: $($event.maxAttendees)"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
