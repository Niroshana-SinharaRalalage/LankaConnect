$body = @{
    email = "niroshhh2@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $body

Write-Host "Token obtained successfully"
$response.accessToken | Out-File -FilePath "token_staging.txt" -NoNewline
$response.accessToken