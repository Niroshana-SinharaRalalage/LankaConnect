#!/bin/bash
# Test the new CancelRsvp API endpoint with both scenarios

EVENT_ID="89f8ef9f-af11-4b1a-8dec-b440faef9ad0"
API_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Read token from file
TOKEN=$(cat token_staging.txt | tr -d '\r\n' | tr -d '\000')

echo "=================================================="
echo "Phase 6A.28 - Testing Cancel RSVP API"
echo "=================================================="
echo ""

# Test 1: Check API accessibility
echo "=== Test 1: API Accessibility ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID" | python -c "import sys, json; data = json.load(sys.stdin); print(f'Event: {data.get(\"title\", \"N/A\")}')"
echo ""

# Test 2: Scenario 1 - Cancel WITHOUT deleting commitments (default)
echo "=== Test 2: Cancel Registration (Keep Commitments) ==="
curl -X DELETE -s -w "\nHTTP Status: %{http_code}\n" \
  -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/rsvp"
echo ""

# Wait a bit
sleep 2

# Test 3: Check signup lists after cancellation
echo "=== Test 3: Verify Commitments Still Exist ==="
curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID/signup-lists" | \
  python -c "import sys, json; lists = json.load(sys.stdin); print(f'Total lists: {len(lists)}'); [print(f'  {lst[\"category\"]}: {sum(len(item.get(\"commitments\", [])) for item in lst.get(\"items\", []))} commitments') for lst in lists]"
echo ""

echo "=================================================="
echo "Testing Complete!"
echo "=================================================="
echo ""
echo "NOTE: To test Scenario 2 (delete commitments):"
echo "1. Re-register for the event"
echo "2. Run: curl -X DELETE -H 'Authorization: Bearer \$TOKEN' '$API_URL/api/events/$EVENT_ID/rsvp?deleteSignUpCommitments=true'"
