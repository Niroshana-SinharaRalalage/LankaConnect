# PowerShell script to test the authentication fix
# This script verifies that the backend is properly returning refresh tokens

Write-Host "`n=== LankaConnect Authentication Fix Verification ===" -ForegroundColor Cyan
Write-Host "This script will test the authentication flow with the deployed fixes" -ForegroundColor Gray

# Configuration
$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
$localProxy = "http://localhost:3000/api/proxy"

# Test credentials
$email = "niroshhh2@gmail.com"
$password = "1qaz!QAZ"

function Test-LoginEndpoint {
    param (
        [string]$url,
        [string]$environment
    )

    Write-Host "`n[LOGIN] Testing $environment login..." -ForegroundColor Yellow

    $body = @{
        email = $email
        password = $password
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$url/Auth/login" -Method POST -Body $body -ContentType "application/json" -ErrorAction Stop

        Write-Host "[SUCCESS] Login successful!" -ForegroundColor Green
        Write-Host "   Email: $($response.user.email)" -ForegroundColor Gray

        # Check for tokens
        if ($response.accessToken) {
            Write-Host "   [OK] Access Token: Present (expires: $($response.tokenExpiresAt))" -ForegroundColor Green
        } else {
            Write-Host "   [FAIL] Access Token: Missing" -ForegroundColor Red
        }

        if ($response.refreshToken) {
            Write-Host "   [OK] Refresh Token: Present" -ForegroundColor Green
            Write-Host "   [VERIFIED] FIX CONFIRMED: Refresh token is now included in response!" -ForegroundColor Cyan
        } else {
            Write-Host "   [FAIL] Refresh Token: Missing" -ForegroundColor Red
            Write-Host "   [WARNING] The fix may not be deployed yet or environment check is failing" -ForegroundColor Yellow
        }

        # Save tokens for further testing
        $global:accessToken = $response.accessToken
        $global:refreshToken = $response.refreshToken

        return $response
    }
    catch {
        Write-Host "[ERROR] Login failed: $_" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "   Error details: $errorBody" -ForegroundColor Red
        }
        return $null
    }
}

function Test-RefreshToken {
    param (
        [string]$url,
        [string]$refreshToken
    )

    if (-not $refreshToken) {
        Write-Host "`n[SKIP] Skipping refresh test - no refresh token available" -ForegroundColor Yellow
        return
    }

    Write-Host "`n[REFRESH] Testing token refresh..." -ForegroundColor Yellow

    $body = @{
        refreshToken = $refreshToken
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$url/Auth/refresh" -Method POST -Body $body -ContentType "application/json" -ErrorAction Stop

        Write-Host "[SUCCESS] Token refresh successful!" -ForegroundColor Green

        if ($response.accessToken) {
            Write-Host "   [OK] New Access Token: Present" -ForegroundColor Green
        }

        if ($response.refreshToken) {
            Write-Host "   [OK] New Refresh Token: Present" -ForegroundColor Green
        }

        return $response
    }
    catch {
        Write-Host "[ERROR] Token refresh failed: $_" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
            $errorBody = $reader.ReadToEnd()
            Write-Host "   Error details: $errorBody" -ForegroundColor Red
        }
        return $null
    }
}

function Clear-BrowserStorage {
    Write-Host "`nOpening browser storage cleaner..." -ForegroundColor Yellow

    $clearerPath = "C:\Work\LankaConnect\web\public\clear-auth.html"
    if (Test-Path $clearerPath) {
        Start-Process "file:///$clearerPath"
        Write-Host "Browser cleaner opened. Use it to clear all auth data before testing." -ForegroundColor Green
    } else {
        Write-Host "Browser cleaner not found at: $clearerPath" -ForegroundColor Red
    }
}

# Main execution
Write-Host "`n[1] Testing Staging Environment (Azure)" -ForegroundColor Cyan
$stagingResult = Test-LoginEndpoint -url $baseUrl -environment "Staging"

if ($stagingResult -and $stagingResult.refreshToken) {
    Test-RefreshToken -url $baseUrl -refreshToken $stagingResult.refreshToken
}

Write-Host "`n[2] Testing Local Development (via proxy)" -ForegroundColor Cyan
# Check if local server is running
try {
    $localCheck = Invoke-WebRequest -Uri "http://localhost:3000" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
    if ($localCheck.StatusCode -eq 200) {
        $localResult = Test-LoginEndpoint -url $localProxy -environment "Local"
        if ($localResult -and $localResult.refreshToken) {
            Test-RefreshToken -url $localProxy -refreshToken $localResult.refreshToken
        }
    }
} catch {
    Write-Host "[WARNING] Local server not running on http://localhost:3000" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if ($stagingResult -and $stagingResult.refreshToken) {
    Write-Host "[CONFIRMED] STAGING FIX VERIFIED: Refresh tokens are being returned!" -ForegroundColor Green
    Write-Host "   The backend fix for Phase 6A.10 is working correctly." -ForegroundColor Gray
    Write-Host "`n[Next Steps]:" -ForegroundColor Yellow
    Write-Host "   1. Clear your browser storage using the tool" -ForegroundColor Gray
    Write-Host "   2. Login fresh at http://localhost:3000/login" -ForegroundColor Gray
    Write-Host "   3. Navigate to event pages - no more 401 errors!" -ForegroundColor Gray
} else {
    Write-Host "[WARNING] Refresh tokens not found in staging response" -ForegroundColor Yellow
    Write-Host "   The backend fix may still be deploying or needs investigation" -ForegroundColor Gray
}

# Offer to open browser cleaner
Write-Host "`nWould you like to open the browser storage cleaner? (Y/N): " -NoNewline -ForegroundColor Cyan
$answer = Read-Host
if ($answer -eq 'Y' -or $answer -eq 'y') {
    Clear-BrowserStorage
}

Write-Host "`nTest complete!" -ForegroundColor Green