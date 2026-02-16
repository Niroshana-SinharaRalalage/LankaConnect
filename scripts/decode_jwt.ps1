# Decode JWT token to verify claims
$response = Get-Content "c:/Work/LankaConnect/.login_response.json" | ConvertFrom-Json
$token = $response.accessToken

Write-Host "Decoding JWT token..."

# Split token into parts
$parts = $token.Split('.')
if ($parts.Length -ne 3) {
    Write-Host "Invalid JWT token format"
    exit
}

# Decode payload (second part)
$payloadBase64 = $parts[1]
# Add padding if needed
while ($payloadBase64.Length % 4 -ne 0) {
    $payloadBase64 += "="
}

# Base64 decode
$payloadBytes = [System.Convert]::FromBase64String($payloadBase64)
$payloadJson = [System.Text.Encoding]::UTF8.GetString($payloadBytes)
$payload = $payloadJson | ConvertFrom-Json

Write-Host "`nJWT Claims:"
Write-Host "nameid (userId): $($payload.nameid)"
Write-Host "email: $($payload.email)"
Write-Host "unique_name: $($payload.unique_name)"
Write-Host "role: $($payload.role)"
Write-Host "exp: $($payload.exp) (expires at: $(Get-Date -UnixTimeSeconds $payload.exp))"

Write-Host "`nAll Claims:"
$payload | ConvertTo-Json -Depth 10
