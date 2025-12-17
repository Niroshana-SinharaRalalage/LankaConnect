# Test CREATE event with email groups

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId

Write-Host "✅ Logged in: $($loginResponse.user.email)" -ForegroundColor Green
Write-Host "User ID: $userId`n" -ForegroundColor Cyan

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Get user's email groups
$emailGroups = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/EmailGroups' -Method Get -Headers $headers

Write-Host "Available Email Groups:" -ForegroundColor Cyan
foreach ($group in $emailGroups) {
    Write-Host "  - $($group.name) ($($group.id)) - Active: $($group.isActive)" -ForegroundColor White
}

# Create event with email groups
$createEventBody = @{
    title = "Test Event with Email Groups - $(Get-Date -Format 'yyyy-MM-dd HH:mm')"
    description = "Testing email groups integration from PowerShell"
    startDate = "2025-12-30T10:00:00Z"
    endDate = "2025-12-30T18:00:00Z"
    organizerId = $userId
    capacity = 50
    category = 0  # Religious
    emailGroupIds = @($emailGroups[0].id)  # Use first email group
} | ConvertTo-Json

Write-Host "`nCreating event with email groups..." -ForegroundColor Yellow
Write-Host "Payload:" -ForegroundColor Gray
Write-Host $createEventBody -ForegroundColor White

try {
    $response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Post -Headers $headers -Body $createEventBody

    Write-Host "`n✅ Event created successfully!" -ForegroundColor Green
    Write-Host "Event ID: $($response.id)" -ForegroundColor Cyan

    # Verify event has email groups
    Start-Sleep -Seconds 2
    $createdEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($response.id)" -Method Get -Headers $headers

    Write-Host "`nVerification:" -ForegroundColor Cyan
    Write-Host "Title: $($createdEvent.title)" -ForegroundColor White
    Write-Host "Email Group IDs Count: $($createdEvent.emailGroupIds.Count)" -ForegroundColor White
    if ($createdEvent.emailGroupIds -and $createdEvent.emailGroupIds.Count -gt 0) {
        Write-Host "✅ Email groups persisted successfully!" -ForegroundColor Green
        foreach ($id in $createdEvent.emailGroupIds) {
            Write-Host "  - $id" -ForegroundColor White
        }
    } else {
        Write-Host "❌ Email groups NOT persisted" -ForegroundColor Red
    }

} catch {
    Write-Host "`n❌ Event creation failed!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Red

    if ($_.ErrorDetails.Message) {
        Write-Host "`nError Response Body:" -ForegroundColor Yellow
        Write-Host $_.ErrorDetails.Message -ForegroundColor White

        try {
            $errorJson = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "`nParsed Error:" -ForegroundColor Yellow
            $errorJson | ConvertTo-Json -Depth 5 | Write-Host
        } catch {
            Write-Host "Could not parse error as JSON" -ForegroundColor Gray
        }
    }

    Write-Host "`nFull Exception:" -ForegroundColor Yellow
    Write-Host $_.Exception.ToString() -ForegroundColor Gray
}
