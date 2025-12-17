# Test UPDATE with detailed error logging

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
    $token = $loginResponse.accessToken
    $userId = $loginResponse.user.userId

    Write-Host "✅ Logged in as: $($loginResponse.user.email)" -ForegroundColor Green
    Write-Host "User ID: $userId`n" -ForegroundColor Cyan

    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    # Get user's events with future dates
    $events = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Get -Headers $headers
    $ownEvents = $events | Where-Object { $_.organizerId -eq $userId }

    $futureEvents = $ownEvents | Where-Object {
        $startDate = [DateTime]::Parse($_.startDate)
        $startDate -gt [DateTime]::UtcNow
    }

    if ($futureEvents -and $futureEvents.Count -gt 0) {
        $testEvent = $futureEvents[0]
        Write-Host "Using event: $($testEvent.title)" -ForegroundColor Cyan
        Write-Host "Event ID: $($testEvent.id)`n" -ForegroundColor Gray

        # Get full event details
        $event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

        # Prepare minimal update with email groups
        $updateBody = @{
            eventId = $event.id
            title = $event.title
            description = $event.description
            startDate = $event.startDate
            endDate = $event.endDate
            capacity = $event.capacity
            category = $event.category
            emailGroupIds = @("c74c0635-59f4-42d1-874f-204a67c4b21d")
        }

        Write-Host "Sending PUT request..." -ForegroundColor Yellow
        $jsonBody = $updateBody | ConvertTo-Json -Depth 3

        try {
            $response = Invoke-WebRequest -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Put -Headers $headers -Body $jsonBody -UseBasicParsing

            Write-Host "`n✅ Update successful! Status: $($response.StatusCode)" -ForegroundColor Green

            # Verify persistence
            Start-Sleep -Seconds 2
            $updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

            Write-Host "`nVerification:" -ForegroundColor Cyan
            Write-Host "Email Group IDs Count: $($updatedEvent.emailGroupIds.Count)" -ForegroundColor White
            if ($updatedEvent.emailGroupIds -and $updatedEvent.emailGroupIds.Count -gt 0) {
                Write-Host "✅ Email groups persisted successfully!" -ForegroundColor Green
                foreach ($id in $updatedEvent.emailGroupIds) {
                    Write-Host "  - $id" -ForegroundColor White
                }
            } else {
                Write-Host "❌ Email groups NOT persisted" -ForegroundColor Red
            }

        } catch {
            Write-Host "`n❌ Update failed!" -ForegroundColor Red
            Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
            Write-Host "Status Description: $($_.Exception.Response.StatusDescription)" -ForegroundColor Red

            if ($_.ErrorDetails.Message) {
                Write-Host "`nError Response Body:" -ForegroundColor Yellow
                Write-Host $_.ErrorDetails.Message -ForegroundColor White
            }

            if ($_.Exception.Response) {
                $responseStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($responseStream)
                $responseBody = $reader.ReadToEnd()
                $reader.Close()
                $responseStream.Close()

                if ($responseBody) {
                    Write-Host "`nRaw Response:" -ForegroundColor Yellow
                    Write-Host $responseBody -ForegroundColor White
                }
            }

            Write-Host "`nFull Exception:" -ForegroundColor Yellow
            Write-Host $_.Exception.ToString() -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ No future events found for testing" -ForegroundColor Red
    }

} catch {
    Write-Host "❌ Login or initial request failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor White
}
