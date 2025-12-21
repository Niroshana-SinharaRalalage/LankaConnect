$body = @{
    email = 'niroshanaks@gmail.com'
    password = '1qaz!QAZ'
    rememberMe = $true
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method POST -ContentType 'application/json' -Body $body
$response | ConvertTo-Json -Depth 5
