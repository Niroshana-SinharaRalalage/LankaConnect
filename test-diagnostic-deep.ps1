# Deep diagnostic test with raw HTTP client to capture full error

Add-Type -AssemblyName System.Net.Http

$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
$token = $loginResponse.accessToken
$userId = $loginResponse.user.userId

Write-Host "✅ Logged in: $($loginResponse.user.email)" -ForegroundColor Green

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Use an event ID we know exists
$eventId = "d9fa9a8e-2b54-47b2-bb24-09ee6f8dd656"

# Get event details
$event = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -Method Get -Headers $headers

Write-Host "`nEvent: $($event.title)" -ForegroundColor Cyan
Write-Host "Start Date: $($event.startDate)" -ForegroundColor Gray

# Prepare minimal update
$updateBody = @{
    eventId = $event.id
    title = $event.title
    description = $event.description
    startDate = "2025-12-26T10:00:00Z"  # Change to future date
    endDate = "2025-12-26T18:00:00Z"
    capacity = $event.capacity
    category = $event.category
    emailGroupIds = @("c74c0635-59f4-42d1-874f-204a67c4b21d")
}

$jsonBody = $updateBody | ConvertTo-Json -Depth 3

Write-Host "`nSending PUT request..." -ForegroundColor Yellow
Write-Host "URL: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId" -ForegroundColor Gray

# Use System.Net.Http.HttpClient for better error handling
$handler = New-Object System.Net.Http.HttpClientHandler
$client = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromSeconds(30)

$request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Put, "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$eventId")
$request.Headers.Add("Authorization", "Bearer $token")
$request.Content = New-Object System.Net.Http.StringContent($jsonBody, [System.Text.Encoding]::UTF8, "application/json")

try {
    $response = $client.SendAsync($request).Result

    Write-Host "`nStatus: $($response.StatusCode)" -ForegroundColor $(if ($response.IsSuccessStatusCode) { "Green" } else { "Red" })

    $responseBody = $response.Content.ReadAsStringAsync().Result

    if ($response.IsSuccessStatusCode) {
        Write-Host "✅ Success!" -ForegroundColor Green
        Write-Host $responseBody -ForegroundColor White
    } else {
        Write-Host "❌ Failed!" -ForegroundColor Red
        Write-Host "`nResponse Body:" -ForegroundColor Yellow
        Write-Host $responseBody -ForegroundColor White

        try {
            $errorJson = $responseBody | ConvertFrom-Json
            Write-Host "`nParsed Error:" -ForegroundColor Yellow
            $errorJson | ConvertTo-Json -Depth 5 | Write-Host
        } catch {
            Write-Host "Could not parse as JSON" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "`n❌ Exception occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor White
    Write-Host "`nFull Exception:" -ForegroundColor Yellow
    Write-Host $_.Exception.ToString() -ForegroundColor Gray
} finally {
    $client.Dispose()
}
