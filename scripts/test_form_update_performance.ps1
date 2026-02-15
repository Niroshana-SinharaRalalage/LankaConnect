# Phase 6A.111 Diagnostics: Test Form Update Performance
# This script tests form update API directly to isolate frontend vs backend timeout issues

param(
    [string]$Environment = "staging",
    [int]$QuestionCount = 15,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Configuration
$configs = @{
    staging = @{
        ApiUrl = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
        Email = "niroshhh@gmail.com"
        Password = "12!@qwASzx"
    }
    local = @{
        ApiUrl = "http://localhost:5000/api"
        Email = "niroshhh@gmail.com"
        Password = "12!@qwASzx"
    }
}

$config = $configs[$Environment]

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Form Update Performance Test" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "Testing with $QuestionCount questions" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login and get token
Write-Host "[1/6] Logging in..." -ForegroundColor Yellow
$loginBody = @{
    email = $config.Email
    password = $config.Password
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $loginResponse = Invoke-RestMethod -Uri "$($config.ApiUrl)/Auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -SessionVariable session `
        -TimeoutSec 30

    Write-Host "   [OK] Login successful ($($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Green
    if ($Verbose) {
        Write-Host "   Token: $($loginResponse.token.Substring(0, 20))..." -ForegroundColor Gray
    }
} catch {
    Write-Host "   [ERROR] Login failed: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Find an event with a form
Write-Host "[2/6] Finding event with signup form..." -ForegroundColor Yellow
$stopwatch.Restart()
try {
    $events = Invoke-RestMethod -Uri "$($config.ApiUrl)/Events/my-events" `
        -Method GET `
        -Headers @{Authorization = "Bearer $($loginResponse.token)"} `
        -TimeoutSec 30

    Write-Host "   [OK] Found $($events.Count) events ($($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Green

    # Find event with forms
    $eventWithForm = $null
    foreach ($event in $events) {
        $forms = Invoke-RestMethod -Uri "$($config.ApiUrl)/Events/$($event.id)/forms" `
            -Method GET `
            -Headers @{Authorization = "Bearer $($loginResponse.token)"} `
            -TimeoutSec 30

        if ($forms.Count -gt 0) {
            $eventWithForm = $event
            $form = $forms[0]
            Write-Host "   [OK] Found event '$($event.title)' with form '$($form.title)'" -ForegroundColor Green
            break
        }
    }

    if (-not $eventWithForm) {
        Write-Host "   [ERROR] No events with forms found. Please create a signup form first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   [ERROR] Failed to find events: $_" -ForegroundColor Red
    exit 1
}

# Step 3: Get form details
Write-Host "[3/6] Fetching form details..." -ForegroundColor Yellow
$stopwatch.Restart()
try {
    $formDetail = Invoke-RestMethod -Uri "$($config.ApiUrl)/Events/$($eventWithForm.id)/forms/$($form.id)" `
        -Method GET `
        -Headers @{Authorization = "Bearer $($loginResponse.token)"} `
        -TimeoutSec 30

    Write-Host "   [OK] Form has $($formDetail.questions.Count) questions ($($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Green
    if ($Verbose) {
        $formDetail.questions | ForEach-Object {
            Write-Host "      - $($_.questionText) ($($_.questionType))" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "   [ERROR] Failed to fetch form: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Check if user has existing response
Write-Host "[4/6] Checking for existing response..." -ForegroundColor Yellow
$stopwatch.Restart()
$existingResponse = $null
try {
    $existingResponse = Invoke-RestMethod -Uri "$($config.ApiUrl)/Events/$($eventWithForm.id)/forms/$($form.id)/responses/my" `
        -Method GET `
        -Headers @{Authorization = "Bearer $($loginResponse.token)"} `
        -TimeoutSec 30

    if ($existingResponse) {
        Write-Host "   [OK] Found existing response ($($stopwatch.ElapsedMilliseconds)ms)" -ForegroundColor Green
        if ($Verbose) {
            Write-Host "      Response ID: $($existingResponse.id)" -ForegroundColor Gray
            Write-Host "      Submitted: $($existingResponse.submittedAt)" -ForegroundColor Gray
        }
    }
} catch {
    if ($_.Exception.Response.StatusCode -eq 204) {
        Write-Host "   [INFO] No existing response found (will create new)" -ForegroundColor Cyan
    } else {
        Write-Host "   [WARN] Error checking response: $_" -ForegroundColor Yellow
    }
}

# Step 5: Submit or Update response
if ($existingResponse) {
    Write-Host "[5/6] UPDATING existing response..." -ForegroundColor Yellow
    Write-Host "   Simulating update with $($formDetail.questions.Count) answers..." -ForegroundColor Cyan

    # Build answers array (modify each answer)
    $answers = @()
    foreach ($question in $formDetail.questions) {
        $answer = @{
            questionId = $question.id
        }

        switch ($question.questionType) {
            "ShortText" { $answer.textValue = "Updated at $(Get-Date -Format 'HH:mm:ss')" }
            "LongText" { $answer.textValue = "Updated long text response at $(Get-Date -Format 'HH:mm:ss'). This is a longer response to test performance." }
            "YesNo" { $answer.booleanValue = $true }
            "SingleChoice" {
                if ($question.options.Count -gt 0) {
                    $answer.selectedOptionIds = @($question.options[0].id)
                }
            }
            "MultipleChoice" {
                if ($question.options.Count -gt 0) {
                    $answer.selectedOptionIds = @($question.options[0].id)
                }
            }
        }

        $answers += $answer
    }

    $updateBody = @{
        answers = $answers
    } | ConvertTo-Json -Depth 10

    # CRITICAL: Measure update time with extended timeout
    $updateStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Write-Host ""
    Write-Host "   [TIMING] Starting update at $(Get-Date -Format 'HH:mm:ss.fff')..." -ForegroundColor Cyan

    try {
        $updateResponse = Invoke-RestMethod -Uri "$($config.ApiUrl)/Events/$($eventWithForm.id)/forms/$($form.id)/responses/$($existingResponse.id)" `
            -Method PUT `
            -Headers @{
                Authorization = "Bearer $($loginResponse.token)"
                "Content-Type" = "application/json"
            } `
            -Body $updateBody `
            -TimeoutSec 180  # 3 minutes timeout to test if backend completes

        $updateStopwatch.Stop()
        $durationMs = $updateStopwatch.ElapsedMilliseconds
        $durationSec = [math]::Round($durationMs / 1000, 2)

        Write-Host ""
        Write-Host "   [SUCCESS] UPDATE SUCCESSFUL!" -ForegroundColor Green
        Write-Host "   [TIMING] Duration: $durationMs ms ($durationSec seconds)" -ForegroundColor Green

        # Performance assessment
        if ($durationMs -lt 5000) {
            Write-Host "   [PERF] EXCELLENT performance (<5s)" -ForegroundColor Green
        } elseif ($durationMs -lt 30000) {
            Write-Host "   [PERF] GOOD performance (<30s)" -ForegroundColor Green
        } elseif ($durationMs -lt 120000) {
            Write-Host "   [WARN] SLOW performance (30s-2min) - Phase 6A.111 timeout active" -ForegroundColor Yellow
        } else {
            Write-Host "   [ERROR] VERY SLOW performance (>2min) - Exceeds frontend timeout!" -ForegroundColor Red
        }

    } catch {
        $updateStopwatch.Stop()
        $durationMs = $updateStopwatch.ElapsedMilliseconds
        $durationSec = [math]::Round($durationMs / 1000, 2)

        Write-Host ""
        Write-Host "   [ERROR] UPDATE FAILED!" -ForegroundColor Red
        Write-Host "   [TIMING] Failed after: $durationMs ms ($durationSec seconds)" -ForegroundColor Red
        Write-Host "   Error: $_" -ForegroundColor Red

        # Analyze error type
        if ($_.Exception.Message -match "timeout") {
            Write-Host ""
            Write-Host "   [RCA] DIAGNOSIS: BACKEND TIMEOUT" -ForegroundColor Magenta
            Write-Host "      Backend is taking longer than timeout limit" -ForegroundColor Magenta
            Write-Host "      This is a BACKEND PERFORMANCE ISSUE" -ForegroundColor Magenta
        } elseif ($_.Exception.Response.StatusCode -eq 401) {
            Write-Host ""
            Write-Host "   [RCA] DIAGNOSIS: AUTH ISSUE" -ForegroundColor Magenta
        } elseif ($_.Exception.Response.StatusCode -eq 400) {
            Write-Host ""
            Write-Host "   [RCA] DIAGNOSIS: VALIDATION ERROR" -ForegroundColor Magenta
        } else {
            Write-Host ""
            Write-Host "   [RCA] DIAGNOSIS: UNKNOWN ERROR" -ForegroundColor Magenta
        }

        exit 1
    }

} else {
    Write-Host "[5/6] No existing response - skipping update test" -ForegroundColor Yellow
    Write-Host "   To test update performance, please submit a response first via UI" -ForegroundColor Cyan
}

# Step 6: Summary
Write-Host ""
Write-Host "[6/6] Test Summary" -ForegroundColor Yellow
Write-Host "   Event: $($eventWithForm.title)" -ForegroundColor White
Write-Host "   Form: $($form.title)" -ForegroundColor White
Write-Host "   Questions: $($formDetail.questions.Count)" -ForegroundColor White
Write-Host "   Update Duration: $durationMs ms ($durationSec seconds)" -ForegroundColor White
Write-Host ""

if ($durationMs -gt 120000) {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "[CRITICAL] BACKEND PERFORMANCE ISSUE CONFIRMED" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Backend is taking >2 minutes (120s) to process form updates." -ForegroundColor Red
    Write-Host "This exceeds the frontend timeout and causes the error you're seeing." -ForegroundColor Red
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Check Azure Application Insights for backend performance metrics" -ForegroundColor White
    Write-Host "2. Check database query performance (slow queries, missing indexes)" -ForegroundColor White
    Write-Host "3. Review UpdateFormResponseCommandHandler logs for bottlenecks" -ForegroundColor White
} else {
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "[OK] Backend performance is acceptable" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "If UI still shows timeout, this is a FRONTEND issue." -ForegroundColor Yellow
}
