#!/bin/bash
# Simple Signup Commitment Test - No dependencies

BASE_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID="d543629f-a5ba-4475-b124-3d0fc5200f2f"

echo "=== Signup List Email Flow Test ==="
echo ""

# Step 1: Login
echo "Step 1: Logging in..."
curl -s -X 'POST' \
  "$BASE_URL/api/Auth/login" \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "niroshhh@gmail.com",
    "password": "1qaz!QAZ",
    "rememberMe": true,
    "ipAddress": "127.0.0.1"
  }' > .login_response.json

echo "Login response saved to .login_response.json"
cat .login_response.json
echo ""

# Extract token manually (simple grep)
TOKEN=$(grep -o '"token":"[^"]*' .login_response.json | grep -o '[^"]*$')

if [ -z "$TOKEN" ]; then
    echo "❌ Failed to extract token"
    exit 1
fi

echo "✅ Token: ${TOKEN:0:50}..."
echo "$TOKEN" > .staging_token.txt
echo ""

# Step 2: Get signup lists
echo "Step 2: Getting signup lists for event $EVENT_ID..."
curl -s -X 'GET' \
  "$BASE_URL/api/Events/$EVENT_ID/signup-lists" \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool

echo ""
echo ""
echo "=== MANUAL TEST INSTRUCTIONS ==="
echo "1. Copy the token from .staging_token.txt"
echo "2. Look at the signup lists above and find an available item ID"
echo "3. Run this command with the item ID:"
echo ""
echo "TOKEN=\$(cat .staging_token.txt)"
echo "ITEM_ID=\"<paste-item-id-here>\""
echo ""
echo "curl -v -X 'POST' \\"
echo "  \"$BASE_URL/api/Events/$EVENT_ID/signup-items/\$ITEM_ID/commit\" \\"
echo "  -H \"Authorization: Bearer \$TOKEN\" \\"
echo "  -H 'Content-Type: application/json' \\"
echo "  -d '{\"quantity\": 1}'"
echo ""
echo "4. Then immediately check Azure logs:"
echo "az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 100"
