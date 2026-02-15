# Test staging login with CORRECT password
$body = @{
    email = "niroshhh@gmail.com"
    password = "1qaz!QAZ"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

Write-Host "Testing login with correct credentials..."

try {
    $response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' `
        -Method POST `
        -ContentType 'application/json' `
        -Body $body `
        -ErrorAction Stop

    Write-Host "`n✅ Login successful!" -ForegroundColor Green
    Write-Host "Access Token: $($response.accessToken.Substring(0,30))..."
    $response.accessToken | Out-File -FilePath 'C:\Work\LankaConnect\.token.txt' -Encoding ascii -NoNewline

    Write-Host "`nToken saved to .token.txt"
}
catch {
    Write-Host "`n❌ Login failed!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Error: $($_.Exception.Message)"

    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)"
    }
}
