# Get staging API token
$body = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' `
    -Method POST `
    -ContentType 'application/json' `
    -Body $body

Write-Host "Token obtained successfully"
$response.accessToken | Out-File -FilePath 'C:\Work\LankaConnect\.token.txt' -Encoding ascii -NoNewline
$token = Get-Content 'C:\Work\LankaConnect\.token.txt'
Write-Host "Token: $($token.Substring(0,30))..."
