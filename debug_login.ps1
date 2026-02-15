$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "Logging in..."
$loginBody = '{"email":"admin@lankaconnect.com","password":"Admin@2025!","rememberMe":true,"ipAddress":"127.0.0.1"}'
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"

Write-Host "Login response:"
$loginResponse | ConvertTo-Json -Depth 5
Write-Host ""
Write-Host "Token exists: $($null -ne $loginResponse.token)"
Write-Host "User exists: $($null -ne $loginResponse.user)"
if ($loginResponse.user) {
    Write-Host "User ID: $($loginResponse.user.id)"
}
