#!/bin/bash

# Script to verify required Azure Communication Services secrets exist in Key Vault
# Usage: ./verify-secrets.sh [staging|production]

set -e

ENVIRONMENT="${1:-staging}"
VAULT_NAME="lankaconnect-${ENVIRONMENT}-kv"

echo "========================================="
echo "Verifying Azure Email Secrets"
echo "Environment: $ENVIRONMENT"
echo "Key Vault: $VAULT_NAME"
echo "========================================="
echo ""

# Required secrets for Azure Communication Services
REQUIRED_SECRETS=(
  "AZURE-EMAIL-CONNECTION-STRING"
  "AZURE-EMAIL-SENDER-ADDRESS"
)

# Optional SMTP secrets (for reference)
OPTIONAL_SECRETS=(
  "SMTP-HOST"
  "SMTP-PORT"
  "SMTP-USERNAME"
  "SMTP-PASSWORD"
  "EMAIL-FROM-ADDRESS"
)

# Check if vault exists
echo "Checking if Key Vault exists..."
if ! az keyvault show --name "$VAULT_NAME" &>/dev/null; then
  echo "❌ ERROR: Key Vault '$VAULT_NAME' not found"
  echo "   Please verify the vault name and your Azure subscription"
  exit 1
fi
echo "✅ Key Vault found"
echo ""

# Check required secrets
echo "Checking required Azure Communication Services secrets..."
ALL_REQUIRED_FOUND=true

for SECRET in "${REQUIRED_SECRETS[@]}"; do
  echo -n "  Checking $SECRET... "

  if az keyvault secret show --vault-name "$VAULT_NAME" --name "$SECRET" &>/dev/null; then
    # Get first 20 characters of value (masked)
    VALUE=$(az keyvault secret show --vault-name "$VAULT_NAME" --name "$SECRET" --query value -o tsv)
    VALUE_PREFIX="${VALUE:0:20}"
    echo "✅ EXISTS (starts with: ${VALUE_PREFIX}...)"
  else
    echo "❌ MISSING"
    ALL_REQUIRED_FOUND=false
  fi
done
echo ""

# Check optional SMTP secrets (for reference)
echo "Checking SMTP secrets (optional, for reference only)..."
for SECRET in "${OPTIONAL_SECRETS[@]}"; do
  echo -n "  Checking $SECRET... "

  if az keyvault secret show --vault-name "$VAULT_NAME" --name "$SECRET" &>/dev/null; then
    echo "✅ EXISTS"
  else
    echo "⚠️  MISSING (not needed for Azure provider)"
  fi
done
echo ""

# Summary
echo "========================================="
echo "Summary"
echo "========================================="

if [ "$ALL_REQUIRED_FOUND" = true ]; then
  echo "✅ All required Azure Communication Services secrets found!"
  echo ""
  echo "You can now proceed with deploying the workflow fix."
  echo ""
  echo "Next steps:"
  echo "1. Review the patch file: deploy-staging.yml.patch"
  echo "2. Apply the fix to .github/workflows/deploy-staging.yml"
  echo "3. Commit and push to trigger deployment"
  echo "4. Monitor deployment logs for email provider initialization"
  echo "5. Test email functionality (registration, password reset)"
  exit 0
else
  echo "❌ Missing required secrets!"
  echo ""
  echo "Please add the missing secrets using these commands:"
  echo ""

  for SECRET in "${REQUIRED_SECRETS[@]}"; do
    if ! az keyvault secret show --vault-name "$VAULT_NAME" --name "$SECRET" &>/dev/null; then
      echo "# Add $SECRET"
      echo "az keyvault secret set \\"
      echo "  --vault-name $VAULT_NAME \\"
      echo "  --name $SECRET \\"
      echo "  --value '<YOUR_VALUE_HERE>'"
      echo ""
    fi
  done

  echo "To get the required values:"
  echo ""
  echo "1. Connection String:"
  echo "   az communication list-key \\"
  echo "     --name <communication-service-name> \\"
  echo "     --resource-group <resource-group> \\"
  echo "     --query primaryConnectionString -o tsv"
  echo ""
  echo "2. Sender Address:"
  echo "   Obtain from Azure Portal → Communication Services → Email → Domains"
  echo "   Example: DoNotReply@<verified-domain>.azurecomm.net"
  echo ""
  exit 1
fi
