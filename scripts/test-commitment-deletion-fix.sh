#!/bin/bash
# Test Phase 6A.28 commitment deletion fix
# Tests both scenarios after the architectural fixes

EVENT_ID="89f8ef9f-af11-4b1a-8dec-b440faef9ad0"
USER_ID="5e782b4d-29ed-4e1d-9039-6c8f698aeea9"
API_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Read token from file
TOKEN=$(cat token_staging.txt | tr -d '\r\n' | tr -d '\000')

echo "=========================================="
echo "Phase 6A.28 - Testing Commitment Deletion Fix"
echo "=========================================="
echo ""

# Test 1: Check current state
echo "=== Test 1: Check Current Signup Lists ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/signup-lists" | \
  python -c "import sys, json; lists = json.load(sys.stdin); print(f'Total lists: {len(lists)}'); [print(f'  {lst[\"category\"]}: {len(lst.get(\"items\", []))} items, {sum(len(item.get(\"commitments\", [])) for item in lst.get(\"items\", []))} total commitments') for lst in lists]"
echo ""

# Test 2: Scenario 1 - Cancel WITHOUT deleting commitments (default)
echo "=== Test 2: Scenario 1 - Cancel Without Deleting Commitments ==="
echo "Expected: Registration cancelled, commitments remain"
RESPONSE1=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
  -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/rsvp")

HTTP_STATUS1=$(echo "$RESPONSE1" | grep "HTTP_STATUS" | cut -d: -f2)
echo "HTTP Status: $HTTP_STATUS1"

if [ "$HTTP_STATUS1" = "200" ]; then
  echo "✅ Cancellation successful (kept commitments)"
else
  echo "❌ Cancellation failed with status $HTTP_STATUS1"
  echo "$RESPONSE1" | grep -v "HTTP_STATUS"
fi
echo ""

# Wait a bit for changes to propagate
sleep 2

# Test 3: Verify commitments still exist
echo "=== Test 3: Verify Commitments Still Exist ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/signup-lists" | \
  python -c "import sys, json; lists = json.load(sys.stdin); user_commitments = sum(1 for lst in lists for item in lst.get('items', []) for c in item.get('commitments', []) if c.get('userId') == '$USER_ID'); print(f'User commitments after Scenario 1: {user_commitments}'); print('✅ PASS: Commitments preserved' if user_commitments > 0 else '❌ FAIL: Commitments were deleted')"
echo ""

echo "=========================================="
echo "Re-register for Scenario 2 Test"
echo "=========================================="
echo "Please manually:"
echo "1. Re-register for the event at: https://lankaconnect-staging.azurewebsites.net/events/$EVENT_ID"
echo "2. Commit to some signup items"
echo "3. Press Enter when ready to test Scenario 2..."
read -p ""

# Test 4: Scenario 2 - Cancel WITH deleting commitments
echo ""
echo "=== Test 4: Scenario 2 - Cancel With Deleting Commitments ==="
echo "Expected: Registration cancelled, commitments deleted"

# Check commitments before deletion
echo "Commitments BEFORE deletion:"
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/signup-lists" | \
  python -c "import sys, json; lists = json.load(sys.stdin); user_commitments = [(item['itemDescription'], c['quantity']) for lst in lists for item in lst.get('items', []) for c in item.get('commitments', []) if c.get('userId') == '$USER_ID']; [print(f'  - {desc}: qty={qty}') for desc, qty in user_commitments]; print(f'Total: {len(user_commitments)} commitments')"
echo ""

RESPONSE2=$(curl -s -w "\nHTTP_STATUS:%{http_code}" \
  -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/rsvp?deleteSignUpCommitments=true")

HTTP_STATUS2=$(echo "$RESPONSE2" | grep "HTTP_STATUS" | cut -d: -f2)
echo "HTTP Status: $HTTP_STATUS2"

if [ "$HTTP_STATUS2" = "200" ]; then
  echo "✅ Cancellation successful (should have deleted commitments)"
else
  echo "❌ Cancellation failed with status $HTTP_STATUS2"
  echo "$RESPONSE2" | grep -v "HTTP_STATUS"
fi
echo ""

# Wait a bit for changes to propagate
sleep 2

# Test 5: Verify commitments are deleted
echo "=== Test 5: Verify Commitments Are Deleted ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/signup-lists" | \
  python -c "import sys, json; lists = json.load(sys.stdin); user_commitments = sum(1 for lst in lists for item in lst.get('items', []) for c in item.get('commitments', []) if c.get('userId') == '$USER_ID'); print(f'User commitments after Scenario 2: {user_commitments}'); print('✅ PASS: Commitments deleted' if user_commitments == 0 else '❌ FAIL: Commitments still exist')"
echo ""

# Test 6: Verify remaining quantities restored
echo "=== Test 6: Check Remaining Quantities ==="
echo "If quantities increased, the fix is working correctly"
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/signup-lists" | \
  python -c "import sys, json; lists = json.load(sys.stdin); [print(f'  {lst[\"category\"]}: {item[\"itemDescription\"]} - Remaining: {item.get(\"remainingQuantity\", \"N/A\")}') for lst in lists for item in lst.get('items', [])]"
echo ""

echo "=========================================="
echo "Test Complete!"
echo "=========================================="
echo ""
echo "SUMMARY:"
echo "- Scenario 1 (keep): Commitments should remain after cancellation"
echo "- Scenario 2 (delete): Commitments should be deleted and quantities restored"
