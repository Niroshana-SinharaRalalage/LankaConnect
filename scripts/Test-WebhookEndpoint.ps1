# Simple Webhook Endpoint Reachability Test
# PowerShell version for Windows

param(
    [string]$ApiUrl = "https://lankaconnect-api.azurewebsites.net/api/payments/webhook"
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Simple Webhook Endpoint Reachability Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "URL: $ApiUrl"
Write-Host ""
Write-Host "Testing if endpoint is reachable..." -ForegroundColor Yellow
Write-Host ""

try {
    # Simple POST request with minimal payload
    $headers = @{
        "Content-Type" = "application/json"
        "Stripe-Signature" = "invalid_signature_for_test"
    }

    $body = @{
        test = "reachability"
    } | ConvertTo-Json

    $response = Invoke-WebRequest -Uri $ApiUrl `
        -Method POST `
        -Headers $headers `
        -Body $body `
        -TimeoutSec 30 `
        -ErrorAction Stop

    Write-Host "Response Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response Body:"
    Write-Host $response.Content
    Write-Host ""

    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "✅ SUCCESS: Endpoint returned 200 OK" -ForegroundColor Green
    Write-Host "   This is unexpected for an invalid signature." -ForegroundColor Yellow
    Write-Host "➡️  Next step: Verify webhook secret validation is enabled" -ForegroundColor Yellow
    Write-Host "==========================================" -ForegroundColor Cyan
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    $errorResponse = $_.Exception.Response

    Write-Host "Response Status Code: $statusCode" -ForegroundColor Yellow
    Write-Host ""

    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "Interpreting Results:" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan

    switch ($statusCode) {
        400 {
            Write-Host "✅ GOOD: Endpoint returned 400 Bad Request" -ForegroundColor Green
            Write-Host "   This means the endpoint IS reachable and working." -ForegroundColor White
            Write-Host "   The 400 is expected because we sent an invalid signature." -ForegroundColor White
            Write-Host ""
            Write-Host "➡️  Next step: Check Stripe webhook configuration" -ForegroundColor Yellow
        }
        500 {
            Write-Host "⚠️  PARTIAL: Endpoint returned 500 Internal Server Error" -ForegroundColor Yellow
            Write-Host "   Endpoint is reachable but application has an error." -ForegroundColor White
            Write-Host ""
            Write-Host "➡️  Next step: Check Azure container logs for errors" -ForegroundColor Yellow
        }
        404 {
            Write-Host "❌ PROBLEM: Endpoint returned 404 Not Found" -ForegroundColor Red
            Write-Host "   The webhook endpoint is not registered/deployed." -ForegroundColor White
            Write-Host ""
            Write-Host "➡️  Next step: Verify deployment and routing configuration" -ForegroundColor Yellow
        }
        Default {
            Write-Host "❓ UNKNOWN: Unexpected response code $statusCode" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Error Details:"
            Write-Host $_.Exception.Message
            Write-Host ""
            Write-Host "➡️  Next step: Manual investigation required" -ForegroundColor Yellow
        }
    }

    Write-Host "==========================================" -ForegroundColor Cyan
}
