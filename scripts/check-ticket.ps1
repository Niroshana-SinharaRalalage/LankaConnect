$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIzODAxMmVhNi0xMjQ4LTQ3YWEtYTQ2MS0zN2MyY2M4MmJmM2EiLCJlbWFpbCI6Im5pcm9zaGFuYWtzQGdtYWlsLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmlyb3NoYW5hIFNpbmhhcmFnZSIsInJvbGUiOiJHZW5lcmFsVXNlciIsImZpcnN0TmFtZSI6Ik5pcm9zaGFuYSIsImxhc3ROYW1lIjoiU2luaGFyYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiOGU3NzNhOGYtYjBkNC00YjI3LTg4MWYtZjZiMWVkZDA4OTVkIiwiaWF0IjoxNzY2MjkyMDQ0LCJuYmYiOjE3NjYyOTIwNDQsImV4cCI6MTc2NjI5Mzg0NCwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.VOo9o-FPkFGv3XSnnti1wkCnS8Z0m4vzMBvny34gjvk"

$eventId = "89f8ef9f-af11-4b1a-8dec-b440faef9ad0"

$headers = @{
    "Authorization" = "Bearer $token"
}

# Try to get ticket for this event
try {
    $response = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/$eventId/my-registration/ticket" -Method GET -Headers $headers
    $response | ConvertTo-Json -Depth 10
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Status: $($_.Exception.Response.StatusCode)"
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $reader.BaseStream.Position = 0
    $reader.ReadToEnd()
}
