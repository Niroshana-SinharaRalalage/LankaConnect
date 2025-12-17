# Capture Badge API error with real-time Azure logs
$ErrorActionPreference = "Stop"

Write-Host "Starting real-time log capture..." -ForegroundColor Yellow
Write-Host ""

# Start tailing logs in background
$logJob = Start-Job -ScriptBlock {
    az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 50 --follow 2>&1 |
    Select-String -Pattern "Badge|Exception|Error|500" -Context 3,3
}

# Wait a moment for logs to start streaming
Start-Sleep -Seconds 2

# Trigger the error
Write-Host "Triggering Badge API call..." -ForegroundColor Yellow
try {
    # Login
    $loginBody = @{
        email = "niroshhh@gmail.com"
        password = "12!@qwASzx"
        rememberMe = $true
        ipAddress = "string"
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
    $token = $loginResponse.accessToken

    # Call Badges API
    $headers = @{
        "Authorization" = "Bearer $token"
    }

    $response = Invoke-WebRequest -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Badges' -Method Get -Headers $headers -ErrorAction Stop

    Write-Host "SUCCESS: HTTP $($response.StatusCode)" -ForegroundColor Green

} catch {
    Write-Host "ERROR: HTTP $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red

    # Try to extract response body
    try {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $reader.ReadToEnd()
        $reader.Close()

        Write-Host "`nError Response Body:" -ForegroundColor Yellow
        Write-Host $errorBody
    } catch {
        Write-Host "Could not read error response" -ForegroundColor Gray
    }
}

# Wait for logs to catch up
Write-Host "`nWaiting for logs..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Stop log job and get output
Stop-Job -Job $logJob
$logOutput = Receive-Job -Job $logJob
Remove-Job -Job $logJob

Write-Host "`nCaptured Logs:" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
if ($logOutput) {
    $logOutput | ForEach-Object { Write-Host $_ }
} else {
    Write-Host "No Badge/Exception/Error logs captured" -ForegroundColor Yellow
}
