# Fix Stripe API Key Configuration in Azure Container Apps
# This script implements ADR-005 recommendations

param(
    [string]$ResourceGroup = "lankaconnect-staging",
    [string]$ContainerAppName = "lankaconnect-api-staging",
    [string]$KeyVaultName = "lankaconnect-staging-kv",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "Stripe API Key Configuration Fix for Azure Container Apps" -ForegroundColor Cyan
Write-Host "Implementing ADR-005 Recommendations" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Get Container App System-Assigned Managed Identity
Write-Host "[Step 1/5] Retrieving Container App Managed Identity..." -ForegroundColor Yellow
$identity = az containerapp show `
    --name $ContainerAppName `
    --resource-group $ResourceGroup `
    --query "identity" -o json | ConvertFrom-Json

if ($null -eq $identity -or $identity.type -ne "SystemAssigned") {
    Write-Host "ERROR: System-Assigned Managed Identity not found!" -ForegroundColor Red
    Write-Host "The Container App must have a System-Assigned identity." -ForegroundColor Red
    exit 1
}

$principalId = $identity.principalId
$tenantId = $identity.tenantId

Write-Host "SUCCESS: Found System-Assigned Managed Identity" -ForegroundColor Green
Write-Host "  Principal ID: $principalId" -ForegroundColor Gray
Write-Host "  Tenant ID: $tenantId" -ForegroundColor Gray
Write-Host ""

# Step 2: Verify Stripe Secrets Exist in Key Vault
Write-Host "[Step 2/5] Verifying Stripe secrets in Key Vault..." -ForegroundColor Yellow

$stripeSecretKey = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name "stripe-secret-key" `
    --query "name" -o tsv 2>$null

$stripePublishableKey = az keyvault secret show `
    --vault-name $KeyVaultName `
    --name "stripe-publishable-key" `
    --query "name" -o tsv 2>$null

if ([string]::IsNullOrEmpty($stripeSecretKey)) {
    Write-Host "ERROR: Secret 'stripe-secret-key' not found in Key Vault!" -ForegroundColor Red
    Write-Host "Please create this secret first using:" -ForegroundColor Yellow
    Write-Host "  az keyvault secret set --vault-name $KeyVaultName --name stripe-secret-key --value 'sk_live_xxx'" -ForegroundColor Gray
    exit 1
}

if ([string]::IsNullOrEmpty($stripePublishableKey)) {
    Write-Host "ERROR: Secret 'stripe-publishable-key' not found in Key Vault!" -ForegroundColor Red
    Write-Host "Please create this secret first using:" -ForegroundColor Yellow
    Write-Host "  az keyvault secret set --vault-name $KeyVaultName --name stripe-publishable-key --value 'pk_live_xxx'" -ForegroundColor Gray
    exit 1
}

Write-Host "SUCCESS: Stripe secrets found in Key Vault" -ForegroundColor Green
Write-Host "  stripe-secret-key: EXISTS" -ForegroundColor Gray
Write-Host "  stripe-publishable-key: EXISTS" -ForegroundColor Gray
Write-Host ""

# Step 3: Grant Key Vault Access to Managed Identity
Write-Host "[Step 3/5] Granting Key Vault permissions to Managed Identity..." -ForegroundColor Yellow

if ($WhatIf) {
    Write-Host "WHATIF: Would grant 'get' and 'list' secret permissions to principal $principalId" -ForegroundColor Cyan
} else {
    az keyvault set-policy `
        --name $KeyVaultName `
        --object-id $principalId `
        --secret-permissions get list

    if ($LASTEXITCODE -eq 0) {
        Write-Host "SUCCESS: Key Vault access policy updated" -ForegroundColor Green
        Write-Host "  Permissions granted: get, list" -ForegroundColor Gray
    } else {
        Write-Host "ERROR: Failed to update Key Vault access policy" -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# Step 4: Verify Current Container App Environment Variables
Write-Host "[Step 4/5] Checking Container App environment variables..." -ForegroundColor Yellow

$envVars = az containerapp show `
    --name $ContainerAppName `
    --resource-group $ResourceGroup `
    --query "properties.template.containers[0].env" -o json | ConvertFrom-Json

$stripeSecretKeyEnv = $envVars | Where-Object { $_.name -eq "Stripe__SecretKey" }
$stripePublishableKeyEnv = $envVars | Where-Object { $_.name -eq "Stripe__PublishableKey" }

if ($null -eq $stripeSecretKeyEnv) {
    Write-Host "WARNING: Stripe__SecretKey environment variable not configured" -ForegroundColor Yellow
    Write-Host "  This should be added in the deployment workflow" -ForegroundColor Gray
} else {
    Write-Host "FOUND: Stripe__SecretKey = $($stripeSecretKeyEnv.secretRef)" -ForegroundColor Green
}

if ($null -eq $stripePublishableKeyEnv) {
    Write-Host "WARNING: Stripe__PublishableKey environment variable not configured" -ForegroundColor Yellow
    Write-Host "  This should be added in the deployment workflow" -ForegroundColor Gray
} else {
    Write-Host "FOUND: Stripe__PublishableKey = $($stripePublishableKeyEnv.secretRef)" -ForegroundColor Green
}
Write-Host ""

# Step 5: Verify appsettings.Staging.json Configuration
Write-Host "[Step 5/5] Checking appsettings.Staging.json for hardcoded values..." -ForegroundColor Yellow

$appsettingsPath = "src\LankaConnect.API\appsettings.Staging.json"
if (Test-Path $appsettingsPath) {
    $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json

    if ($null -ne $appsettings.Stripe) {
        $secretKey = $appsettings.Stripe.SecretKey
        $publishableKey = $appsettings.Stripe.PublishableKey

        if ($secretKey -like "sk_test_*" -or $secretKey -like "sk_live_*") {
            Write-Host "WARNING: appsettings.Staging.json contains hardcoded Stripe SecretKey!" -ForegroundColor Red
            Write-Host "  Current value: $secretKey" -ForegroundColor Gray
            Write-Host "  Should be: `${STRIPE_SECRET_KEY}" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  This MUST be fixed to use environment variable placeholders" -ForegroundColor Yellow
            Write-Host "  Environment variables will NOT override hardcoded values!" -ForegroundColor Red
        } else {
            Write-Host "OK: Stripe SecretKey uses variable placeholder" -ForegroundColor Green
        }

        if ($publishableKey -like "pk_test_*" -or $publishableKey -like "pk_live_*") {
            Write-Host "WARNING: appsettings.Staging.json contains hardcoded Stripe PublishableKey!" -ForegroundColor Red
            Write-Host "  Current value: $publishableKey" -ForegroundColor Gray
            Write-Host "  Should be: `${STRIPE_PUBLISHABLE_KEY}" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  This MUST be fixed to use environment variable placeholders" -ForegroundColor Yellow
        } else {
            Write-Host "OK: Stripe PublishableKey uses variable placeholder" -ForegroundColor Green
        }
    }
} else {
    Write-Host "WARNING: Could not find $appsettingsPath" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "===================================================" -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "===================================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "Configuration Status:" -ForegroundColor White
Write-Host "  Managed Identity: CONFIGURED" -ForegroundColor Green
Write-Host "  Key Vault Secrets: EXIST" -ForegroundColor Green
Write-Host "  Key Vault Permissions: GRANTED" -ForegroundColor Green
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor White
Write-Host "  1. Update appsettings.Staging.json to remove hardcoded API keys" -ForegroundColor Yellow
Write-Host "  2. Verify deployment workflow includes:" -ForegroundColor Yellow
Write-Host "       Stripe__SecretKey=secretref:stripe-secret-key" -ForegroundColor Gray
Write-Host "       Stripe__PublishableKey=secretref:stripe-publishable-key" -ForegroundColor Gray
Write-Host "  3. Trigger deployment to staging (push to develop branch)" -ForegroundColor Yellow
Write-Host "  4. Monitor Container App logs for successful Stripe initialization" -ForegroundColor Yellow
Write-Host "  5. Test Stripe payment flow" -ForegroundColor Yellow
Write-Host ""

Write-Host "Monitoring Commands:" -ForegroundColor White
Write-Host "  # View Container App logs" -ForegroundColor Gray
Write-Host "  az containerapp logs show --name $ContainerAppName --resource-group $ResourceGroup --tail 50" -ForegroundColor Gray
Write-Host ""
Write-Host "  # Check Container App status" -ForegroundColor Gray
Write-Host "  az containerapp show --name $ContainerAppName --resource-group $ResourceGroup --query 'properties.runningStatus' -o tsv" -ForegroundColor Gray
Write-Host ""

Write-Host "Troubleshooting:" -ForegroundColor White
Write-Host "  If deployment still fails, wait 60 seconds for Key Vault permissions to propagate" -ForegroundColor Gray
Write-Host "  Verify secret names are exactly: 'stripe-secret-key' and 'stripe-publishable-key'" -ForegroundColor Gray
Write-Host "  Check environment variables override appsettings.Staging.json values" -ForegroundColor Gray
Write-Host ""

if ($WhatIf) {
    Write-Host "NOTE: This was a DRY RUN. No changes were made." -ForegroundColor Cyan
    Write-Host "Run without -WhatIf to apply changes." -ForegroundColor Cyan
}

Write-Host "===================================================" -ForegroundColor Cyan
