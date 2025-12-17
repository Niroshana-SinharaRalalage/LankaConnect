$body = '{"Email":"sam@test.com","Password":"Test123!"}'
try {
    $result = Invoke-WebRequest -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/auth/login' -Method Post -ContentType 'application/json' -Body $body
    Write-Output $result.Content
} catch {
    Write-Output "Status: $($_.Exception.Response.StatusCode)"
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $reader.BaseStream.Position = 0
    $responseBody = $reader.ReadToEnd()
    Write-Output "Body: $responseBody"
}
