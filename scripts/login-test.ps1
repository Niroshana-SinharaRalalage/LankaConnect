$loginBody = @{
    email = "niroshhh2@gmail.com"
    password = "Niroshana@2025"
} | ConvertTo-Json

Write-Host "Logging in to staging API..."
try {
    $response = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody

    Write-Host "Login successful!"
    Write-Host "Token: $($response.accessToken)"

    # Save token to file for later use
    $response.accessToken | Out-File -FilePath "c:\Work\LankaConnect\token.txt" -NoNewline
    Write-Host "`nToken saved to token.txt"
} catch {
    Write-Host "Login failed: $($_.Exception.Message)"
    Write-Host "Response: $($_.ErrorDetails.Message)"
}
