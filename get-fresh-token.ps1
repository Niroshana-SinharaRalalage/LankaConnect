# Get fresh authentication token
$loginUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login"

$body = @{
    email = "johndoe@example.com"
    password = "Password123!"
} | ConvertTo-Json

Write-Host "Getting fresh token..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri $loginUrl `
        -Method POST `
        -Headers @{"Content-Type" = "application/json"} `
        -Body $body

    Write-Host "Login successful!" -ForegroundColor Green

    # Save token to file
    $response.token | Out-File "token.txt" -NoNewline
    Write-Host "Token saved to token.txt" -ForegroundColor Green

    # Show user info
    Write-Host "`nUser Info:" -ForegroundColor Cyan
    Write-Host "Email: $($response.email)"
    Write-Host "Role: $($response.role)"
    Write-Host "Name: $($response.firstName) $($response.lastName)"

    # Return token
    return $response.token
}
catch {
    Write-Host "Login failed!" -ForegroundColor Red
    Write-Host $_.Exception.Message

    # Get the actual error response if available
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        $responseBody = $reader.ReadToEnd()
        Write-Host "`nError Response:" -ForegroundColor Yellow
        Write-Host $responseBody
    }
}