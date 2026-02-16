$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "Logging in..."
$loginBody = '{"email":"admin@lankaconnect.com","password":"Admin@2025!","rememberMe":true,"ipAddress":"127.0.0.1"}'
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
$userId = $loginResponse.user.id
Write-Host "Logged in: $userId"

Write-Host "Making commitment..."
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$signupId = "8567645d-7c71-4965-bec3-f696d266b597"
$itemId = "72639fc5-005f-415f-aa94-6326965e1590"

$commitBody = "{`"userId`":`"$userId`",`"quantity`":5,`"notes`":`"FRESH TEST`",`"contactName`":`"Admin`",`"contactEmail`":`"admin@lankaconnect.com`"}"
$headers = @{ Authorization = "Bearer $token" }
$uri = "$baseUrl/api/Events/$eventId/signups/$signupId/items/$itemId/commit"

$commitResponse = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $commitBody -ContentType "application/json"
Write-Host "SUCCESS!"

Start-Sleep -Seconds 5
Write-Host "Check logs now"
