#!/bin/bash

# Comprehensive test script for Phase 6A.34 fix
# Tests: DetectChanges(), domain event dispatching, email queuing

set -e

API_BASE_URL="${API_BASE_URL:-https://lankaconnect.azurewebsites.net}"
TEST_EVENT_ID="c1f182a9-c957-4a78-a0b2-085917a88900"
AUTH_TOKEN="${AUTH_TOKEN}"  # Set this from your environment

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=========================================="
echo "Phase 6A.34 Fix Verification Test"
echo "=========================================="
echo ""
echo "Testing URL: $API_BASE_URL"
echo "Event ID: $TEST_EVENT_ID"
echo "Timestamp: $(date -u +"%Y-%m-%dT%H:%M:%SZ")"
echo ""

if [ -z "$AUTH_TOKEN" ]; then
    echo -e "${RED}ERROR: AUTH_TOKEN not set${NC}"
    echo "Please set AUTH_TOKEN environment variable"
    echo "Example: export AUTH_TOKEN='Bearer eyJ...'"
    exit 1
fi

# Function to check logs for specific patterns
check_logs() {
    local pattern="$1"
    local description="$2"

    echo -e "${YELLOW}Checking logs for: $description${NC}"
    echo "Pattern: $pattern"
    echo ""

    # Wait a bit for logs to propagate
    sleep 2

    echo "Run this Azure CLI command to check logs:"
    echo "az containerapp logs show --name lankaconnect --resource-group DefaultResourceGroup-EUS --follow --tail 100 | grep -i '$pattern'"
    echo ""
}

# Step 1: Check current registration status
echo "=========================================="
echo "STEP 1: Check Current Registration Status"
echo "=========================================="
echo ""

CURRENT_STATUS=$(curl -s -w "\n%{http_code}" \
    -H "Authorization: $AUTH_TOKEN" \
    "$API_BASE_URL/api/events/$TEST_EVENT_ID/my-registration")

HTTP_CODE=$(echo "$CURRENT_STATUS" | tail -n 1)
RESPONSE=$(echo "$CURRENT_STATUS" | head -n -1)

echo "HTTP Status: $HTTP_CODE"
echo "Response: $RESPONSE"
echo ""

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${YELLOW}User is already registered. Testing cancel/re-register flow.${NC}"
    SHOULD_CANCEL=true
else
    echo -e "${GREEN}User is not registered. Testing fresh registration.${NC}"
    SHOULD_CANCEL=false
fi
echo ""

# Step 2: Cancel existing registration if needed
if [ "$SHOULD_CANCEL" = true ]; then
    echo "=========================================="
    echo "STEP 2: Cancel Existing Registration"
    echo "=========================================="
    echo ""

    CANCEL_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X DELETE \
        -H "Authorization: $AUTH_TOKEN" \
        -H "Content-Type: application/json" \
        "$API_BASE_URL/api/events/$TEST_EVENT_ID/rsvp")

    HTTP_CODE=$(echo "$CANCEL_RESPONSE" | tail -n 1)
    RESPONSE=$(echo "$CANCEL_RESPONSE" | head -n -1)

    echo "HTTP Status: $HTTP_CODE"
    echo "Response: $RESPONSE"
    echo ""

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "204" ]; then
        echo -e "${GREEN}Cancellation successful${NC}"
    else
        echo -e "${RED}Cancellation failed${NC}"
        exit 1
    fi

    # Wait for cancellation to process
    echo "Waiting 3 seconds for cancellation to process..."
    sleep 3
    echo ""

    check_logs "DELETE.*rsvp" "Cancellation request"
    check_logs "CancelRsvpCommand" "Cancellation command processing"
fi

# Step 3: Register for event
echo "=========================================="
echo "STEP 3: Register for Event (POST /rsvp)"
echo "=========================================="
echo ""

REGISTER_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST \
    -H "Authorization: $AUTH_TOKEN" \
    -H "Content-Type: application/json" \
    -d '{}' \
    "$API_BASE_URL/api/events/$TEST_EVENT_ID/rsvp")

HTTP_CODE=$(echo "$REGISTER_RESPONSE" | tail -n 1)
RESPONSE=$(echo "$REGISTER_RESPONSE" | head -n -1)

echo "HTTP Status: $HTTP_CODE"
echo "Response: $RESPONSE"
echo ""

if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
    echo -e "${GREEN}Registration successful${NC}"
else
    echo -e "${RED}Registration failed${NC}"
    echo "Response body: $RESPONSE"
    exit 1
fi
echo ""

# Step 4: Verify registration exists
echo "=========================================="
echo "STEP 4: Verify Registration (GET)"
echo "=========================================="
echo ""

sleep 2

VERIFY_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -H "Authorization: $AUTH_TOKEN" \
    "$API_BASE_URL/api/events/$TEST_EVENT_ID/my-registration")

HTTP_CODE=$(echo "$VERIFY_RESPONSE" | tail -n 1)
RESPONSE=$(echo "$VERIFY_RESPONSE" | head -n -1)

echo "HTTP Status: $HTTP_CODE"
echo "Response: $RESPONSE"
echo ""

if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}Registration verified in database${NC}"
else
    echo -e "${RED}Registration NOT found in database${NC}"
    exit 1
fi
echo ""

# Step 5: Check logs for critical indicators
echo "=========================================="
echo "STEP 5: Log Verification Checklist"
echo "=========================================="
echo ""

echo -e "${YELLOW}CRITICAL: Check Azure logs for these patterns:${NC}"
echo ""

echo "1. POST Request Logged:"
check_logs "POST.*events.*rsvp" "POST /api/events/.../rsvp request"

echo "2. Command Processing:"
check_logs "RsvpToEventCommand" "RSVP command processing"

echo "3. Domain Event Detection:"
check_logs "\[Phase 6A.24\] Found .* domain events" "Domain event detection (Phase 6A.24)"

echo "4. Registration Confirmed Event:"
check_logs "RegistrationConfirmedEvent" "RegistrationConfirmedEvent dispatched"

echo "5. Email Queued:"
check_logs "Queuing email.*confirmation" "Confirmation email queued"
check_logs "OutboxMessage.*Pending" "Email in outbox"

echo ""
echo "=========================================="
echo "EXPECTED LOG SEQUENCE:"
echo "=========================================="
cat << 'EXPECTED'
1. POST /api/events/c1f182a9.../rsvp (200 OK)
2. Processing RsvpToEventCommand for event c1f182a9...
3. [Phase 6A.24] Found 1 domain events to dispatch
4. Dispatching domain event: RegistrationConfirmedEvent
5. Handling RegistrationConfirmedEvent for user ...
6. Queuing confirmation email for registration ...
7. OutboxMessage created with Status=Pending
EXPECTED

echo ""
echo "=========================================="
echo "Manual Verification Steps:"
echo "=========================================="
echo "1. Check Azure Container App logs (last 100 lines):"
echo "   az containerapp logs show --name lankaconnect --resource-group DefaultResourceGroup-EUS --follow --tail 100"
echo ""
echo "2. Query OutboxMessages table:"
echo "   SELECT TOP 10 * FROM OutboxMessages WHERE Type = 'LankaConnect.Domain.Events.RegistrationConfirmedEvent' ORDER BY CreatedAt DESC;"
echo ""
echo "3. Check EmailQueueProcessor logs:"
echo "   Look for 'Processing X pending messages' in recent logs"
echo ""
echo "=========================================="
echo "Test completed at: $(date -u +"%Y-%m-%dT%H:%M:%SZ")"
echo "=========================================="
