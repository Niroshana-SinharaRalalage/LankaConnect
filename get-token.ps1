$body = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $body

    Write-Host "✅ Login successful!" -ForegroundColor Green
    $response.accessToken | Out-File -FilePath "token.txt" -NoNewline

    Write-Host "`n📋 User Info:"
    Write-Host "  Email: $($response.user.email)"
    Write-Host "  Role: $($response.user.role)"
    Write-Host "  User ID: $($response.user.userId)"

    Write-Host "`n🔑 Token saved to token.txt"
    Write-Host "`nToken (first 50 chars): $($response.accessToken.Substring(0, 50))..."
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Response: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
