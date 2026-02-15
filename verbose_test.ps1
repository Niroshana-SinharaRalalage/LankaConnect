$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

Write-Host "Logging in..."
$loginBody = '{"email":"admin@lankaconnect.com","password":"Admin@2025!","rememberMe":true,"ipAddress":"127.0.0.1"}'
$loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/Auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId
Write-Host "Logged in: $userId"
Write-Host ""

$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$signupId = "8567645d-7c71-4965-bec3-f696d266b597"
$itemId = "72639fc5-005f-415f-aa94-6326965e1590"
$uri = "$baseUrl/api/Events/$eventId/signups/$signupId/items/$itemId/commit"

Write-Host "Making request to:"
Write-Host $uri
Write-Host ""

$commitBody = "{`"userId`":`"$userId`",`"quantity`":8,`"notes`":`"VERBOSE TEST`",`"contactName`":`"Admin`",`"contactEmail`":`"admin@lankaconnect.com`"}"
$headers = @{ Authorization = "Bearer $token" }

try {
    $response = Invoke-WebRequest -Uri $uri -Method POST -Headers $headers -Body $commitBody -ContentType "application/json" -UseBasicParsing

    Write-Host "STATUS CODE: $($response.StatusCode)"
    Write-Host "STATUS DESCRIPTION: $($response.StatusDescription)"
    Write-Host "HEADERS:"
    $response.Headers | Format-Table
    Write-Host "CONTENT:"
    Write-Host $response.Content
} catch {
    Write-Host "ERROR!"
    Write-Host "Status: $($_.Exception.Response.StatusCode.value__)"
    Write-Host "Message: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody"
    }
}
