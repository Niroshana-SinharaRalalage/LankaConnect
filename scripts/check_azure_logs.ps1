# Check Azure Container App Logs for Signup List Email Activity
# Focus on recent signup commitment attempts

Write-Host "=== Checking Azure Staging Logs for Signup List Activity ===" -ForegroundColor Cyan
Write-Host ""

# Check if az cli is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Azure CLI not found. Please install: https://aka.ms/installazurecli" -ForegroundColor Red
    exit 1
}

# Check recent logs for signup-related activity
Write-Host "Searching for signup commitment activity in last 30 minutes..." -ForegroundColor Yellow
Write-Host ""

az containerapp logs show `
    --name lankaconnect-api-staging `
    --resource-group LankaConnect-RG `
    --follow false `
    --tail 500 `
    --filter "UserCommittedToSignUp OR CommitmentUpdated OR CommitmentCancelled OR SignUpCommitment OR template-signup-list-commitment" `
    --output table

Write-Host ""
Write-Host "=== Key Log Patterns to Look For ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Domain Event Raised:" -ForegroundColor Green
Write-Host "   [DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem, DomainEvents: 1"
Write-Host ""
Write-Host "2. Domain Event Dispatched:" -ForegroundColor Green
Write-Host "   [Phase 6A.24] Successfully dispatched domain event: UserCommittedToSignUpEvent"
Write-Host ""
Write-Host "3. Handler Executed:" -ForegroundColor Green
Write-Host "   UserCommittedToSignUp START"
Write-Host ""
Write-Host "4. Email Sent:" -ForegroundColor Green
Write-Host "   [DIAG-EMAIL] SendTemplatedEmailAsync START - Template: template-signup-list-commitment-confirmation"
Write-Host ""
Write-Host "5. If Missing Any of Above:" -ForegroundColor Red
Write-Host "   That's where the breakdown occurs!"
