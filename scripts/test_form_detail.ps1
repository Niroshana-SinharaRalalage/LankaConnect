# Test Form Detail Endpoint
# Check if questions are returned in the form detail response

$apiBase = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
$eventId = "62bf37a7-08c5-49e9-84ad-2be388e26caa"
$formId = "ade5a7ac-748a-4b0d-a602-c26226010d59"

# Login to get token
$loginPayload = @{
    email = "niroshhh@gmail.com"
    password = "1qaz!QAZ"
    rememberMe = $true
    ipAddress = "127.0.0.1"
} | ConvertTo-Json

Write-Host "Logging in..." -ForegroundColor Cyan
$loginResponse = Invoke-RestMethod -Uri "$apiBase/Auth/login" -Method Post -Body $loginPayload -ContentType "application/json"
$token = $loginResponse.accessToken

# Get form detail
Write-Host "`nFetching form detail..." -ForegroundColor Cyan
$headers = @{
    "Authorization" = "Bearer $token"
}

$formDetail = Invoke-RestMethod -Uri "$apiBase/events/$eventId/forms/$formId" -Method Get -Headers $headers

Write-Host "`n=== FORM DETAIL ===" -ForegroundColor Green
Write-Host "Form ID: $($formDetail.id)"
Write-Host "Title: $($formDetail.title)"
Write-Host "Status: $($formDetail.status)"
Write-Host "Question Count: $($formDetail.questions.Count)" -ForegroundColor Yellow
Write-Host "`nQuestions:" -ForegroundColor Green
if ($formDetail.questions.Count -eq 0) {
    Write-Host "  NO QUESTIONS FOUND!" -ForegroundColor Red
} else {
    foreach ($question in $formDetail.questions) {
        Write-Host "  - [$($question.questionType)] $($question.questionText) (Required: $($question.isRequired))"
    }
}

Write-Host "`n=== FULL JSON ===" -ForegroundColor Cyan
$formDetail | ConvertTo-Json -Depth 10
