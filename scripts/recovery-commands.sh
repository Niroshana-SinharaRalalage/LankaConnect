#!/bin/bash
# ============================================================================
# Phase 6A.24 Webhook Recovery - Executable Commands
# ============================================================================

# Step 1: Login to Staging API and get access token
echo "Step 1: Logging in to staging API..."
echo ""
echo "🔑 REPLACE 'your-email@example.com' and 'your-password' with your actual credentials:"
echo ""

# IMPORTANT: Replace with your actual email and password
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your-email@example.com",
    "password": "your-password"
  }' | jq '.'

echo ""
echo "📋 Copy the 'accessToken' value from above"
echo "📋 Then run this command with your token and registration ID:"
echo ""
echo "export ACCESS_TOKEN=\"paste-token-here\""
echo "export REGISTRATION_ID=\"paste-registration-id-from-sql-query-1\""
echo ""
echo "# Step 2: Trigger Recovery"
echo "curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \\"
echo "  -H \"Authorization: Bearer \$ACCESS_TOKEN\" \\"
echo "  -H \"Content-Type: application/json\" \\"
echo "  -d '{\"registrationId\": \"'\$REGISTRATION_ID'\"}' | jq '.'"
echo ""
echo "# Step 3: Monitor logs in real-time"
echo "az containerapp logs show \\"
echo "  --name lankaconnect-api-staging \\"
echo "  --resource-group lankaconnect-staging \\"
echo "  --tail 50 \\"
echo "  --follow \\"
echo "  | grep -E 'ADMIN RECOVERY|PaymentCompleted|Ticket|Email'"

# ============================================================================
# READY-TO-USE COMMANDS (copy these after setting variables)
# ============================================================================
cat << 'EOF'

# ============================================================================
# COPY-PASTE THESE COMMANDS AFTER SETTING YOUR VARIABLES
# ============================================================================

# 1. Set your credentials (EDIT THESE)
export USER_EMAIL="your-email@example.com"
export USER_PASSWORD="your-password"
export REGISTRATION_ID="paste-from-sql-query-1"

# 2. Login and extract token
export ACCESS_TOKEN=$(curl -s -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"$USER_EMAIL\", \"password\": \"$USER_PASSWORD\"}" \
  | jq -r '.accessToken')

echo "Access Token: $ACCESS_TOKEN"

# 3. Trigger Recovery
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"registrationId\": \"$REGISTRATION_ID\"}" \
  | jq '.'

# 4. Monitor logs (run in separate terminal)
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 \
  --follow \
  | grep -E "ADMIN RECOVERY|PaymentCompleted|Ticket|Email"

EOF
