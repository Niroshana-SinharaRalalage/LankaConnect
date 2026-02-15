# Test Signup List Commitment Flow
# This script triggers a signup commitment and monitors Azure logs

$ErrorActionPreference = "Stop"

Write-Host "=== Signup List Email Flow Test ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$email = "niroshhh@gmail.com"
$password = "1qaz!QAZ"

# Step 1: Login and get token
Write-Host "Step 1: Getting auth token..." -ForegroundColor Yellow
$loginPayload = @{
    email = $email
    password = $password
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Method Post `
        -Uri "$baseUrl/api/Auth/login" `
        -ContentType "application/json" `
        -Body $loginPayload

    $token = $loginResponse.token
    Write-Host "✅ Token obtained: $($token.Substring(0, 20))..." -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Get signup lists for the event
Write-Host ""
Write-Host "Step 2: Getting signup lists for event $eventId..." -ForegroundColor Yellow

$headers = @{
    Authorization = "Bearer $token"
}

try {
    $signupLists = Invoke-RestMethod -Method Get `
        -Uri "$baseUrl/api/Events/$eventId/signup-lists" `
        -Headers $headers

    Write-Host "✅ Found $($signupLists.Count) signup lists" -ForegroundColor Green

    if ($signupLists.Count -eq 0) {
        Write-Host "⚠️ No signup lists found for this event!" -ForegroundColor Yellow
        exit 0
    }

    # Display signup lists
    foreach ($list in $signupLists) {
        Write-Host ""
        Write-Host "  Signup List: $($list.title)" -ForegroundColor Cyan
        Write-Host "    ID: $($list.id)"
        Write-Host "    Items: $($list.items.Count)"

        foreach ($item in $list.items) {
            Write-Host "      - $($item.description) (ID: $($item.id), Needed: $($item.quantityNeeded), Committed: $($item.quantityCommitted))" -ForegroundColor Gray
        }
    }

} catch {
    Write-Host "❌ Failed to get signup lists: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
    exit 1
}

# Step 3: Get the first available signup item
Write-Host ""
Write-Host "Step 3: Finding available signup item..." -ForegroundColor Yellow

$availableItem = $null
foreach ($list in $signupLists) {
    foreach ($item in $list.items) {
        if ($item.quantityCommitted -lt $item.quantityNeeded) {
            $availableItem = $item
            break
        }
    }
    if ($availableItem) { break }
}

if (-not $availableItem) {
    Write-Host "⚠️ No available signup items found (all items are fully committed)" -ForegroundColor Yellow
    exit 0
}

Write-Host "✅ Found available item: $($availableItem.description)" -ForegroundColor Green
Write-Host "   ID: $($availableItem.id)"
Write-Host "   Needed: $($availableItem.quantityNeeded), Committed: $($availableItem.quantityCommitted)"

# Step 4: Commit to the signup item
Write-Host ""
Write-Host "Step 4: Committing to signup item..." -ForegroundColor Yellow

$commitPayload = @{
    quantity = 1
} | ConvertTo-Json

try {
    $commitResponse = Invoke-RestMethod -Method Post `
        -Uri "$baseUrl/api/Events/$eventId/signup-items/$($availableItem.id)/commit" `
        -Headers $headers `
        -ContentType "application/json" `
        -Body $commitPayload

    Write-Host "✅ Commitment successful!" -ForegroundColor Green
    Write-Host "   Response: $($commitResponse | ConvertTo-Json -Depth 3)"

} catch {
    Write-Host "❌ Commitment failed: $_" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red

    # Try to read error details
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response Body: $responseBody" -ForegroundColor Red
    }
    exit 1
}

# Step 5: Check Azure logs
Write-Host ""
Write-Host "Step 5: Checking Azure logs (last 100 entries)..." -ForegroundColor Yellow
Write-Host "Looking for domain event and email activity..." -ForegroundColor Cyan

Start-Sleep -Seconds 3  # Wait for logs to propagate

az containerapp logs show `
    --name lankaconnect-api-staging `
    --resource-group lankaconnect-staging `
    --follow false `
    --tail 100 `
    --output table | Select-String -Pattern "DIAG|UserCommittedToSignUp|SignUpItem|template-signup-list"

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Expected log patterns if working correctly:" -ForegroundColor Yellow
Write-Host "  1. [DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem" -ForegroundColor Green
Write-Host "  2. [Phase 6A.24] Successfully dispatched domain event: UserCommittedToSignUpEvent" -ForegroundColor Green
Write-Host "  3. UserCommittedToSignUp START" -ForegroundColor Green
Write-Host "  4. [DIAG-EMAIL] SendTemplatedEmailAsync START - Template: template-signup-list-commitment-confirmation" -ForegroundColor Green
Write-Host "  5. [DIAG-EMAIL] Domain entity marked as SENT - SUCCESS" -ForegroundColor Green
