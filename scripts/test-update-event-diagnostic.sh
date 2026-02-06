#!/bin/bash
# Diagnostic script to capture UPDATE event error details

API_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TOKEN=$(cat token_staging.txt | tr -d '\r\n' | tr -d '\000')

# Get a real event ID from the system
echo "=== Step 1: Get Event ID ==="
EVENT_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events" | \
  python -c "import sys, json; events = json.load(sys.stdin); print(events[0]['id'] if events else '')" 2>/dev/null)

if [ -z "$EVENT_ID" ]; then
  echo "ERROR: Could not get event ID"
  exit 1
fi

echo "Event ID: $EVENT_ID"
echo ""

# Get current event details
echo "=== Step 2: Get Current Event ==="
CURRENT_EVENT=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "$API_URL/api/events/$EVENT_ID")

echo "Current event details:"
echo "$CURRENT_EVENT" | python -m json.tool 2>/dev/null | head -30
echo ""

# Try minimal UPDATE (only required fields)
echo "=== Step 3: Minimal UPDATE Test ==="
UPDATE_PAYLOAD=$(cat <<EOF
{
  "eventId": "$EVENT_ID",
  "title": "Updated Test Event",
  "description": "Test update description",
  "startDate": "2025-12-25T10:00:00Z",
  "endDate": "2025-12-25T12:00:00Z",
  "capacity": 50
}
EOF
)

echo "Sending UPDATE request..."
echo "Payload: $UPDATE_PAYLOAD"
echo ""

RESPONSE=$(curl -X PUT -s -w "\n---HTTP_STATUS:%{http_code}---" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$UPDATE_PAYLOAD" \
  "$API_URL/api/events/$EVENT_ID")

HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2 | tr -d '-')
BODY=$(echo "$RESPONSE" | sed '/---HTTP_STATUS/d')

echo "HTTP Status: $HTTP_CODE"
echo "Response Body:"
echo "$BODY" | python -m json.tool 2>/dev/null || echo "$BODY"
echo ""

# Try UPDATE with email groups (the problematic field)
echo "=== Step 4: UPDATE with Email Groups ==="
UPDATE_WITH_GROUPS=$(cat <<EOF
{
  "eventId": "$EVENT_ID",
  "title": "Updated Test Event",
  "description": "Test update description",
  "startDate": "2025-12-25T10:00:00Z",
  "endDate": "2025-12-25T12:00:00Z",
  "capacity": 50,
  "emailGroupIds": []
}
EOF
)

echo "Sending UPDATE with emailGroupIds..."
RESPONSE2=$(curl -X PUT -s -w "\n---HTTP_STATUS:%{http_code}---" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$UPDATE_WITH_GROUPS" \
  "$API_URL/api/events/$EVENT_ID")

HTTP_CODE2=$(echo "$RESPONSE2" | grep "HTTP_STATUS" | cut -d':' -f2 | tr -d '-')
BODY2=$(echo "$RESPONSE2" | sed '/---HTTP_STATUS/d')

echo "HTTP Status: $HTTP_CODE2"
echo "Response Body:"
echo "$BODY2" | python -m json.tool 2>/dev/null || echo "$BODY2"
echo ""

echo "=== Diagnostic Complete ==="
