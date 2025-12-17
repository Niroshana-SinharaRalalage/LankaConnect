# Verify email groups were persisted

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

$eventId = "d9fa9a8e-2b54-47b2-bb24-09ee6f8dd656"
$event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

Write-Host "Event: $($event.title)" -ForegroundColor Cyan
Write-Host "`nEmail Group IDs:" -ForegroundColor Yellow

if ($event.emailGroupIds -and $event.emailGroupIds.Count -gt 0) {
    Write-Host "  Count: $($event.emailGroupIds.Count)" -ForegroundColor Green
    foreach ($id in $event.emailGroupIds) {
        Write-Host "  - $id" -ForegroundColor White
    }
} else {
    Write-Host "  ❌ No email group IDs found" -ForegroundColor Red
}

Write-Host "`nEmail Groups:" -ForegroundColor Yellow

if ($event.emailGroups -and $event.emailGroups.Count -gt 0) {
    Write-Host "  Count: $($event.emailGroups.Count)" -ForegroundColor Green
    foreach ($group in $event.emailGroups) {
        Write-Host "  - Name: $($group.name)" -ForegroundColor White
        Write-Host "    ID: $($group.id)" -ForegroundColor Gray
        Write-Host "    Active: $($group.isActive)" -ForegroundColor Gray
    }
} else {
    Write-Host "  ❌ No email groups found" -ForegroundColor Red
}

# Output full JSON for debugging
Write-Host "`nFull Event JSON:" -ForegroundColor Yellow
$event | ConvertTo-Json -Depth 5 | Write-Host
