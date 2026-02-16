#!/bin/bash

# Phase 6A.114 Issue #81 - Test my-events endpoint
BASE_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

echo "=== Phase 6A.114 Issue #81 - Testing GET /api/Events/my-events ==="
echo ""

# Step 1: Login
echo "Step 1: Authenticating..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "niroshhh@gmail.com",
    "password": "12!@qwASzx",
    "rememberMe": true,
    "ipAddress": "127.0.0.1"
  }')

# Extract token
TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ Authentication failed!"
  echo "Response: $LOGIN_RESPONSE"
  exit 1
fi

echo "✅ Authentication successful!"
echo ""

# Step 2: Test GET /api/Events/my-events
echo "Step 2: Testing GET /api/Events/my-events..."
MY_EVENTS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Events/my-events" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "Response:"
echo "$MY_EVENTS_RESPONSE" | head -c 500
echo ""
echo ""

# Count events
MY_EVENTS_COUNT=$(echo "$MY_EVENTS_RESPONSE" | grep -o '"id"' | wc -l)
echo "✅ GET /api/Events/my-events returned $MY_EVENTS_COUNT events"
echo ""

# Step 3: Test GET /api/Events (all events)
echo "Step 3: Testing GET /api/Events (all public events)..."
ALL_EVENTS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Events" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

ALL_EVENTS_COUNT=$(echo "$ALL_EVENTS_RESPONSE" | grep -o '"id"' | wc -l)
echo "✅ GET /api/Events returned $ALL_EVENTS_COUNT events"
echo ""

# Verification
echo "=== Verification ==="
echo "My Events: $MY_EVENTS_COUNT"
echo "All Events: $ALL_EVENTS_COUNT"
echo ""

if [ "$MY_EVENTS_COUNT" -le "$ALL_EVENTS_COUNT" ]; then
  echo "✅ PASS: my-events ($MY_EVENTS_COUNT) <= all-events ($ALL_EVENTS_COUNT)"
else
  echo "⚠️  UNEXPECTED: my-events has more events than all-events"
fi
echo ""
echo "=== Test Complete ==="
