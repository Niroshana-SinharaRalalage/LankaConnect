#!/bin/bash
# Phase 6 E2E API Test - Scenario 1: Create Free Event
# Tests free event creation with no pricing

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TEST_NAME="Scenario 1: Free Event Creation"
TIMESTAMP=$(date +%s)

echo "========================================="
echo "$TEST_NAME"
echo "========================================="
echo ""

# Test 1: Create Free Event
echo "Test 1.1: POST /api/events (Free Event)"
echo "Expected: HTTP 201 Created"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$STAGING_URL/api/events" \
  -H "Content-Type: application/json" \
  -d "{
    \"title\": \"API Test - Free Community Event $TIMESTAMP\",
    \"description\": \"This is a free event created via API test for Phase 6 E2E testing. No ticket price required.\",
    \"startDate\": \"2025-12-15T18:00:00Z\",
    \"endDate\": \"2025-12-15T21:00:00Z\",
    \"capacity\": 100,
    \"isFree\": true,
    \"location\": {
      \"address\": {
        \"street\": \"123 Test Street\",
        \"city\": \"Colombo\",
        \"state\": \"Western\",
        \"zipCode\": \"00100\",
        \"country\": \"Sri Lanka\"
      }
    },
    \"category\": \"Community\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

echo "HTTP Status: $HTTP_CODE"
echo ""

if [ "$HTTP_CODE" -eq 201 ] || [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ PASS: Event created successfully"
    echo ""
    echo "Response Body:"
    echo "$BODY" | head -50

    # Extract event ID for retrieval test
    EVENT_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    if [ -n "$EVENT_ID" ]; then
        echo ""
        echo "Event ID: $EVENT_ID"
        echo ""

        # Test 1.2: Retrieve the created event
        echo "Test 1.2: GET /api/events/$EVENT_ID"
        echo "Expected: HTTP 200 OK with isFree=true"
        echo ""

        sleep 2  # Brief delay to ensure consistency

        GET_RESPONSE=$(curl -s -w "\n%{http_code}" "$STAGING_URL/api/events/$EVENT_ID")
        GET_HTTP_CODE=$(echo "$GET_RESPONSE" | tail -n1)
        GET_BODY=$(echo "$GET_RESPONSE" | head -n-1)

        echo "HTTP Status: $GET_HTTP_CODE"
        echo ""

        if [ "$GET_HTTP_CODE" -eq 200 ]; then
            echo "✅ PASS: Event retrieved successfully"
            echo ""

            # Verify free event properties
            if echo "$GET_BODY" | grep -q '"isFree":true'; then
                echo "✅ PASS: isFree field is true"
            else
                echo "❌ FAIL: isFree field is not true"
            fi

            if echo "$GET_BODY" | grep -q '"ticketPriceAmount":null' || ! echo "$GET_BODY" | grep -q '"ticketPriceAmount"'; then
                echo "✅ PASS: ticketPriceAmount is null/absent"
            else
                echo "❌ FAIL: ticketPriceAmount should be null"
            fi

            if echo "$GET_BODY" | grep -q '"pricingType":null' || ! echo "$GET_BODY" | grep -q '"pricingType"'; then
                echo "✅ PASS: pricingType is null/absent (legacy format)"
            else
                echo "❌ FAIL: pricingType should be null for free events"
            fi

        else
            echo "❌ FAIL: Could not retrieve event (HTTP $GET_HTTP_CODE)"
        fi
    else
        echo "⚠️ WARNING: Could not extract event ID from response"
    fi

else
    echo "❌ FAIL: Event creation failed (HTTP $HTTP_CODE)"
    echo ""
    echo "Response Body:"
    echo "$BODY"
fi

echo ""
echo "========================================="
echo "Test Scenario 1 Complete"
echo "========================================="
