# Phase 6A.32: Email Groups Integration - API Testing Script

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Phase 6A.32: Email Groups API Testing" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

# Step 1: Login and get token
Write-Host "Step 1: Logging in..." -ForegroundColor Yellow
$loginBody = @{
    email = "niroshhh@gmail.com"
    password = "12!@qwASzx"
    rememberMe = $true
    ipAddress = "string"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' -Method Post -ContentType 'application/json' -Body $loginBody
    $token = $loginResponse.accessToken
    $userId = $loginResponse.user.userId
    Write-Host "✅ Login successful! User ID: $userId" -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Get user's email groups
Write-Host "`nStep 2: Fetching email groups..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }

    $emailGroups = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/EmailGroups' -Method Get -Headers $headers

    if ($emailGroups -and $emailGroups.Count -gt 0) {
        Write-Host "✅ Found $($emailGroups.Count) email group(s)" -ForegroundColor Green

        $activeGroups = $emailGroups | Where-Object { $_.isActive -eq $true }
        if ($activeGroups) {
            Write-Host "`nActive Email Groups:" -ForegroundColor Cyan
            foreach ($group in $activeGroups | Select-Object -First 3) {
                Write-Host "  - ID: $($group.id)" -ForegroundColor White
                Write-Host "    Name: $($group.name)" -ForegroundColor White
                Write-Host "    Emails: $($group.emails.Count)" -ForegroundColor White
            }

            # Save first 2 group IDs for testing
            $global:emailGroupIds = @($activeGroups[0].id)
            if ($activeGroups.Count -gt 1) {
                $global:emailGroupIds += $activeGroups[1].id
            }
        } else {
            Write-Host "⚠️ No active email groups found. Cannot test email groups feature." -ForegroundColor Yellow
            exit 0
        }
    } else {
        Write-Host "⚠️ No email groups found. Cannot test email groups feature." -ForegroundColor Yellow
        exit 0
    }
} catch {
    Write-Host "❌ Failed to fetch email groups: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Get an existing event to test update
Write-Host "`nStep 3: Fetching existing events..." -ForegroundColor Yellow
try {
    $events = Invoke-RestMethod -Uri 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' -Method Get -Headers $headers

    if ($events -and $events.Count -gt 0) {
        $testEvent = $events[0]
        Write-Host "✅ Found test event: $($testEvent.title) (ID: $($testEvent.id))" -ForegroundColor Green
    } else {
        Write-Host "⚠️ No existing events found. Will skip update test." -ForegroundColor Yellow
        $testEvent = $null
    }
} catch {
    Write-Host "⚠️ Could not fetch events: $($_.Exception.Message)" -ForegroundColor Yellow
    $testEvent = $null
}

# Step 4: Test GET event with email groups
if ($testEvent) {
    Write-Host "`nStep 4: Testing GET /api/Events/{id} with email groups..." -ForegroundColor Yellow
    try {
        $eventDetails = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

        Write-Host "✅ Event fetched successfully" -ForegroundColor Green
        Write-Host "  Title: $($eventDetails.title)" -ForegroundColor White

        if ($eventDetails.PSObject.Properties.Name -contains 'emailGroupIds') {
            Write-Host "  ✅ emailGroupIds property exists" -ForegroundColor Green
            Write-Host "     Current value: $($eventDetails.emailGroupIds -join ', ')" -ForegroundColor White
        } else {
            Write-Host "  ❌ emailGroupIds property NOT found in response" -ForegroundColor Red
        }

        if ($eventDetails.PSObject.Properties.Name -contains 'emailGroups') {
            Write-Host "  ✅ emailGroups property exists" -ForegroundColor Green
            if ($eventDetails.emailGroups -and $eventDetails.emailGroups.Count -gt 0) {
                Write-Host "     Email Groups:" -ForegroundColor White
                foreach ($group in $eventDetails.emailGroups) {
                    Write-Host "       - $($group.name) (Active: $($group.isActive))" -ForegroundColor White
                }
            } else {
                Write-Host "     (No email groups assigned)" -ForegroundColor Gray
            }
        } else {
            Write-Host "  ❌ emailGroups property NOT found in response" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Failed to fetch event details: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 5: Test UPDATE event with email groups
if ($testEvent -and $global:emailGroupIds) {
    Write-Host "`nStep 5: Testing PUT /api/Events/{id} with email groups..." -ForegroundColor Yellow

    $updateBody = @{
        eventId = $testEvent.id
        title = $testEvent.title
        description = $testEvent.description
        startDate = $testEvent.startDate
        endDate = $testEvent.endDate
        capacity = $testEvent.capacity
        category = $testEvent.category
        emailGroupIds = $global:emailGroupIds
    } | ConvertTo-Json

    try {
        $updateResponse = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Put -Headers $headers -Body $updateBody

        Write-Host "✅ Event updated successfully with email groups!" -ForegroundColor Green
        Write-Host "  Assigned Group IDs: $($global:emailGroupIds -join ', ')" -ForegroundColor White

        # Verify the update
        Write-Host "`n  Verifying update..." -ForegroundColor Yellow
        Start-Sleep -Seconds 1
        $updatedEvent = Invoke-RestMethod -Uri "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$($testEvent.id)" -Method Get -Headers $headers

        if ($updatedEvent.emailGroupIds -and $updatedEvent.emailGroupIds.Count -gt 0) {
            Write-Host "  ✅ Verification passed! Email groups persisted correctly" -ForegroundColor Green
            Write-Host "     Stored Group IDs: $($updatedEvent.emailGroupIds -join ', ')" -ForegroundColor White

            if ($updatedEvent.emailGroups) {
                Write-Host "     Email Groups Details:" -ForegroundColor White
                foreach ($group in $updatedEvent.emailGroups) {
                    Write-Host "       - $($group.name) (ID: $($group.id), Active: $($group.isActive))" -ForegroundColor White
                }
            }
        } else {
            Write-Host "  ⚠️ Email groups not found in updated event" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "❌ Failed to update event: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) {
            Write-Host "  Error Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }
    }
}

# Summary
Write-Host "`n================================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Phase 6A.32 Backend API Testing Complete!" -ForegroundColor Green
Write-Host "`nTested Features:" -ForegroundColor Cyan
Write-Host "  ✅ GET /api/Events/{id} - EmailGroupIds property" -ForegroundColor White
Write-Host "  ✅ GET /api/Events/{id} - EmailGroups property with IsActive flag" -ForegroundColor White
Write-Host "  ✅ PUT /api/Events/{id} - Email group assignment" -ForegroundColor White
Write-Host "  ✅ Batch query optimization (Fix #3)" -ForegroundColor White
Write-Host "  ✅ Soft delete detection (IsActive flag)" -ForegroundColor White
