#!/bin/bash
# Test Signup List Commitment Flow
# This script triggers a signup commitment and monitors Azure logs

set -e

echo "=== Signup List Email Flow Test ==="
echo ""

BASE_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID="d543629f-a5ba-4475-b124-3d0fc5200f2f"
EMAIL="niroshhh@gmail.com"
PASSWORD="1qaz!QAZ"

# Step 1: Login and get token
echo "Step 1: Getting auth token..."
LOGIN_RESPONSE=$(curl -s -X 'POST' \
  "$BASE_URL/api/Auth/login" \
  -H 'Content-Type: application/json' \
  -d "{
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\",
    \"rememberMe\": true,
    \"ipAddress\": \"127.0.0.1\"
  }")

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
    echo "❌ Login failed"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "✅ Token obtained: ${TOKEN:0:20}..."

# Step 2: Get signup lists for the event
echo ""
echo "Step 2: Getting signup lists for event $EVENT_ID..."

SIGNUP_LISTS=$(curl -s -X 'GET' \
  "$BASE_URL/api/Events/$EVENT_ID/signup-lists" \
  -H "Authorization: Bearer $TOKEN")

echo "Response: $SIGNUP_LISTS" | jq '.'

# Count lists
LIST_COUNT=$(echo "$SIGNUP_LISTS" | jq 'length')
echo "✅ Found $LIST_COUNT signup lists"

if [ "$LIST_COUNT" == "0" ]; then
    echo "⚠️ No signup lists found for this event!"
    exit 0
fi

# Step 3: Find first available signup item
echo ""
echo "Step 3: Finding available signup item..."

ITEM_ID=$(echo "$SIGNUP_LISTS" | jq -r '
  .[0].items[] |
  select(.quantityCommitted < .quantityNeeded) |
  .id' | head -n 1)

if [ -z "$ITEM_ID" ] || [ "$ITEM_ID" == "null" ]; then
    echo "⚠️ No available signup items found (all items are fully committed)"
    exit 0
fi

ITEM_DESCRIPTION=$(echo "$SIGNUP_LISTS" | jq -r ".[] | .items[] | select(.id == \"$ITEM_ID\") | .description")

echo "✅ Found available item: $ITEM_DESCRIPTION"
echo "   ID: $ITEM_ID"

# Step 4: Commit to the signup item
echo ""
echo "Step 4: Committing to signup item..."

COMMIT_RESPONSE=$(curl -s -w "\n%{http_code}" -X 'POST' \
  "$BASE_URL/api/Events/$EVENT_ID/signup-items/$ITEM_ID/commit" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "quantity": 1
  }')

HTTP_CODE=$(echo "$COMMIT_RESPONSE" | tail -n 1)
RESPONSE_BODY=$(echo "$COMMIT_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" == "200" ] || [ "$HTTP_CODE" == "201" ]; then
    echo "✅ Commitment successful! (HTTP $HTTP_CODE)"
    echo "Response: $RESPONSE_BODY"
else
    echo "❌ Commitment failed! (HTTP $HTTP_CODE)"
    echo "Response: $RESPONSE_BODY"
    exit 1
fi

# Step 5: Check Azure logs
echo ""
echo "Step 5: Checking Azure logs (waiting 3 seconds for propagation)..."
sleep 3

az containerapp logs show \
    --name lankaconnect-api-staging \
    --resource-group lankaconnect-staging \
    --follow false \
    --tail 100 \
    --output table | grep -E "DIAG|UserCommittedToSignUp|SignUpItem|template-signup-list" || echo "No matching log entries found"

echo ""
echo "=== Test Complete ==="
echo ""
echo "Expected log patterns if working correctly:"
echo "  1. [DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem"
echo "  2. [Phase 6A.24] Successfully dispatched domain event: UserCommittedToSignUpEvent"
echo "  3. UserCommittedToSignUp START"
echo "  4. [DIAG-EMAIL] SendTemplatedEmailAsync START - Template: template-signup-list-commitment-confirmation"
echo "  5. [DIAG-EMAIL] Domain entity marked as SENT - SUCCESS"
