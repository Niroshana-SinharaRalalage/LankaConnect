#!/bin/bash

# Fix Stripe API Key Configuration in Azure Container Apps
# This script implements ADR-005 recommendations

set -e

# Default values
RESOURCE_GROUP="${1:-lankaconnect-staging}"
CONTAINER_APP_NAME="${2:-lankaconnect-api-staging}"
KEY_VAULT_NAME="${3:-lankaconnect-staging-kv}"
WHAT_IF="${4:-false}"

echo "==================================================="
echo "Stripe API Key Configuration Fix for Azure Container Apps"
echo "Implementing ADR-005 Recommendations"
echo "==================================================="
echo ""

# Step 1: Get Container App System-Assigned Managed Identity
echo "[Step 1/5] Retrieving Container App Managed Identity..."
IDENTITY_JSON=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "identity" -o json)

IDENTITY_TYPE=$(echo "$IDENTITY_JSON" | jq -r '.type')

if [ -z "$IDENTITY_JSON" ] || [ "$IDENTITY_TYPE" != "SystemAssigned" ]; then
    echo "ERROR: System-Assigned Managed Identity not found!"
    echo "The Container App must have a System-Assigned identity."
    exit 1
fi

PRINCIPAL_ID=$(echo "$IDENTITY_JSON" | jq -r '.principalId')
TENANT_ID=$(echo "$IDENTITY_JSON" | jq -r '.tenantId')

echo "SUCCESS: Found System-Assigned Managed Identity"
echo "  Principal ID: $PRINCIPAL_ID"
echo "  Tenant ID: $TENANT_ID"
echo ""

# Step 2: Verify Stripe Secrets Exist in Key Vault
echo "[Step 2/5] Verifying Stripe secrets in Key Vault..."

STRIPE_SECRET_KEY=$(az keyvault secret show \
    --vault-name "$KEY_VAULT_NAME" \
    --name "stripe-secret-key" \
    --query "name" -o tsv 2>/dev/null || echo "")

STRIPE_PUBLISHABLE_KEY=$(az keyvault secret show \
    --vault-name "$KEY_VAULT_NAME" \
    --name "stripe-publishable-key" \
    --query "name" -o tsv 2>/dev/null || echo "")

if [ -z "$STRIPE_SECRET_KEY" ]; then
    echo "ERROR: Secret 'stripe-secret-key' not found in Key Vault!"
    echo "Please create this secret first using:"
    echo "  az keyvault secret set --vault-name $KEY_VAULT_NAME --name stripe-secret-key --value 'sk_live_xxx'"
    exit 1
fi

if [ -z "$STRIPE_PUBLISHABLE_KEY" ]; then
    echo "ERROR: Secret 'stripe-publishable-key' not found in Key Vault!"
    echo "Please create this secret first using:"
    echo "  az keyvault secret set --vault-name $KEY_VAULT_NAME --name stripe-publishable-key --value 'pk_live_xxx'"
    exit 1
fi

echo "SUCCESS: Stripe secrets found in Key Vault"
echo "  stripe-secret-key: EXISTS"
echo "  stripe-publishable-key: EXISTS"
echo ""

# Step 3: Grant Key Vault Access to Managed Identity
echo "[Step 3/5] Granting Key Vault permissions to Managed Identity..."

if [ "$WHAT_IF" = "true" ]; then
    echo "WHATIF: Would grant 'get' and 'list' secret permissions to principal $PRINCIPAL_ID"
else
    az keyvault set-policy \
        --name "$KEY_VAULT_NAME" \
        --object-id "$PRINCIPAL_ID" \
        --secret-permissions get list

    if [ $? -eq 0 ]; then
        echo "SUCCESS: Key Vault access policy updated"
        echo "  Permissions granted: get, list"
    else
        echo "ERROR: Failed to update Key Vault access policy"
        exit 1
    fi
fi
echo ""

# Step 4: Verify Current Container App Environment Variables
echo "[Step 4/5] Checking Container App environment variables..."

ENV_VARS_JSON=$(az containerapp show \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "properties.template.containers[0].env" -o json)

STRIPE_SECRET_KEY_ENV=$(echo "$ENV_VARS_JSON" | jq -r '.[] | select(.name=="Stripe__SecretKey") | .secretRef // .value // empty')
STRIPE_PUBLISHABLE_KEY_ENV=$(echo "$ENV_VARS_JSON" | jq -r '.[] | select(.name=="Stripe__PublishableKey") | .secretRef // .value // empty')

if [ -z "$STRIPE_SECRET_KEY_ENV" ]; then
    echo "WARNING: Stripe__SecretKey environment variable not configured"
    echo "  This should be added in the deployment workflow"
else
    echo "FOUND: Stripe__SecretKey = $STRIPE_SECRET_KEY_ENV"
fi

if [ -z "$STRIPE_PUBLISHABLE_KEY_ENV" ]; then
    echo "WARNING: Stripe__PublishableKey environment variable not configured"
    echo "  This should be added in the deployment workflow"
else
    echo "FOUND: Stripe__PublishableKey = $STRIPE_PUBLISHABLE_KEY_ENV"
fi
echo ""

# Step 5: Verify appsettings.Staging.json Configuration
echo "[Step 5/5] Checking appsettings.Staging.json for hardcoded values..."

APPSETTINGS_PATH="src/LankaConnect.API/appsettings.Staging.json"
if [ -f "$APPSETTINGS_PATH" ]; then
    SECRET_KEY=$(jq -r '.Stripe.SecretKey // empty' "$APPSETTINGS_PATH")
    PUBLISHABLE_KEY=$(jq -r '.Stripe.PublishableKey // empty' "$APPSETTINGS_PATH")

    if [[ "$SECRET_KEY" == sk_test_* ]] || [[ "$SECRET_KEY" == sk_live_* ]]; then
        echo "WARNING: appsettings.Staging.json contains hardcoded Stripe SecretKey!"
        echo "  Current value: $SECRET_KEY"
        echo "  Should be: \${STRIPE_SECRET_KEY}"
        echo ""
        echo "  This MUST be fixed to use environment variable placeholders"
        echo "  Environment variables will NOT override hardcoded values!"
    else
        echo "OK: Stripe SecretKey uses variable placeholder"
    fi

    if [[ "$PUBLISHABLE_KEY" == pk_test_* ]] || [[ "$PUBLISHABLE_KEY" == pk_live_* ]]; then
        echo "WARNING: appsettings.Staging.json contains hardcoded Stripe PublishableKey!"
        echo "  Current value: $PUBLISHABLE_KEY"
        echo "  Should be: \${STRIPE_PUBLISHABLE_KEY}"
        echo ""
        echo "  This MUST be fixed to use environment variable placeholders"
    else
        echo "OK: Stripe PublishableKey uses variable placeholder"
    fi
else
    echo "WARNING: Could not find $APPSETTINGS_PATH"
fi
echo ""

# Summary
echo "==================================================="
echo "SUMMARY"
echo "==================================================="

echo ""
echo "Configuration Status:"
echo "  Managed Identity: CONFIGURED"
echo "  Key Vault Secrets: EXIST"
echo "  Key Vault Permissions: GRANTED"
echo ""

echo "Next Steps:"
echo "  1. Update appsettings.Staging.json to remove hardcoded API keys"
echo "  2. Verify deployment workflow includes:"
echo "       Stripe__SecretKey=secretref:stripe-secret-key"
echo "       Stripe__PublishableKey=secretref:stripe-publishable-key"
echo "  3. Trigger deployment to staging (push to develop branch)"
echo "  4. Monitor Container App logs for successful Stripe initialization"
echo "  5. Test Stripe payment flow"
echo ""

echo "Monitoring Commands:"
echo "  # View Container App logs"
echo "  az containerapp logs show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --tail 50"
echo ""
echo "  # Check Container App status"
echo "  az containerapp show --name $CONTAINER_APP_NAME --resource-group $RESOURCE_GROUP --query 'properties.runningStatus' -o tsv"
echo ""

echo "Troubleshooting:"
echo "  If deployment still fails, wait 60 seconds for Key Vault permissions to propagate"
echo "  Verify secret names are exactly: 'stripe-secret-key' and 'stripe-publishable-key'"
echo "  Check environment variables override appsettings.Staging.json values"
echo ""

if [ "$WHAT_IF" = "true" ]; then
    echo "NOTE: This was a DRY RUN. No changes were made."
    echo "Run without 'true' as 4th argument to apply changes."
fi

echo "==================================================="
