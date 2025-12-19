#!/bin/bash
# Test script to cancel and re-register for Phase 6A.34 verification

API_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID="c1f182a9-c957-4a78-a0b2-085917a88900"

echo "==================================="
echo "Phase 6A.34 - Complete Flow Test"
echo "==================================="

# Step 1: Login
echo ""
echo "Step 1: Login..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email": "niroshhh@gmail.com", "password": "Test@123"}')

# Extract token using grep and sed
TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"token":"[^"]*"' | sed 's/"token":"\([^"]*\)"/\1/')
USER_ID=$(echo "$LOGIN_RESPONSE" | grep -o '"userId":"[^"]*"' | sed 's/"userId":"\([^"]*\)"/\1/')

if [ -z "$TOKEN" ]; then
    echo "❌ Login failed"
    echo "$LOGIN_RESPONSE"
    exit 1
fi

echo "✓ Login successful (User: $USER_ID)"

# Step 2: Cancel existing registration
echo ""
echo "Step 2: Cancel existing registration..."
CANCEL_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X DELETE \
  "$API_URL/api/Events/$EVENT_ID/registrations" \
  -H "Authorization: Bearer $TOKEN")

HTTP_STATUS=$(echo "$CANCEL_RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)

if [ "$HTTP_STATUS" == "204" ] || [ "$HTTP_STATUS" == "200" ]; then
    echo "✓ Registration cancelled (HTTP $HTTP_STATUS)"
elif [ "$HTTP_STATUS" == "404" ]; then
    echo "✓ No existing registration to cancel (HTTP 404)"
else
    echo "⚠ Unexpected status: HTTP $HTTP_STATUS"
fi

# Wait for processing
sleep 2

# Step 3: Create NEW registration
echo ""
echo "Step 3: Create NEW registration..."
RSVP_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST \
  "$API_URL/api/Events/$EVENT_ID/rsvp" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "quantity": 2,
    "attendees": [
      {"firstName": "Test1", "lastName": "User1", "email": "test1@example.com"},
      {"firstName": "Test2", "lastName": "User2", "email": "test2@example.com"}
    ],
    "contactEmail": "niroshhh@gmail.com",
    "contactPhone": "+94771234567"
  }')

HTTP_STATUS=$(echo "$RSVP_RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)
RESPONSE_BODY=$(echo "$RSVP_RESPONSE" | grep -v "HTTP_STATUS:")

if [ "$HTTP_STATUS" == "204" ] || [ "$HTTP_STATUS" == "200" ] || [ "$HTTP_STATUS" == "201" ]; then
    echo "✓ Registration created (HTTP $HTTP_STATUS)"
else
    echo "❌ Registration failed (HTTP $HTTP_STATUS)"
    echo "Response: $RESPONSE_BODY"
    exit 1
fi

# Step 4: Wait and check logs
echo ""
echo "Step 4: Checking Azure logs for domain events..."
echo "  Waiting 5 seconds for logs..."
sleep 5

echo ""
echo "Expected log sequence:"
echo "  1. Processing RsvpToEventCommand"
echo "  2. [Phase 6A.24] Found 1 domain events to dispatch: RegistrationConfirmedEvent"
echo "  3. RegistrationConfirmedEventHandler invoked"
echo "  4. SendTemplatedEmailAsync: registration-confirmation"
echo ""

# Check container app logs
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 \
  --follow false \
  --format text | grep -E "(RsvpToEvent|RegistrationConfirmed|domain event|SendTemplatedEmail)" | tail -20

echo ""
echo "✓ Test complete! Check logs above for domain event dispatch."
