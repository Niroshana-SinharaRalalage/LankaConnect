#!/bin/bash
# Phase 6 E2E API Test - Scenario 2: Create Single Price Event
# Tests single price event creation (legacy pricing format)

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TEST_NAME="Scenario 2: Single Price Event Creation"
TIMESTAMP=$(date +%s)

# Auth token from login endpoint (expires at 2025-12-05T00:04:57Z)
AUTH_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiNTk3ODk1YzEtOTI0My00ZmE2LTgxYTEtMjJhNjQ3M2M5YzFlIiwiaWF0IjoxNzY0ODkxMjk3LCJuYmYiOjE3NjQ4OTEyOTcsImV4cCI6MTc2NDg5MzA5NywiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.PbbaqS8Sdh3YBPce2LNNX8aX1loC1RMVR4X4Do5QKCA"

echo "========================================="
echo "$TEST_NAME"
echo "========================================="
echo ""

# Test 2: Create Single Price Event
echo "Test 2.1: POST /api/events (Single Price Event)"
echo "Expected: HTTP 201 Created with ticketPriceAmount=$25.00"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$STAGING_URL/api/events" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d "{
    \"title\": \"API Test - Single Price Event $TIMESTAMP\",
    \"description\": \"This is a single price event created via API test. Ticket price: \$25.00 USD per person.\",
    \"startDate\": \"2025-12-16T14:00:00Z\",
    \"endDate\": \"2025-12-16T18:00:00Z\",
    \"capacity\": 50,
    \"isFree\": false,
    \"ticketPriceAmount\": 25.00,
    \"ticketPriceCurrency\": \"USD\",
    \"location\": {
      \"address\": {
        \"street\": \"456 Event Avenue\",
        \"city\": \"Kandy\",
        \"state\": \"Central\",
        \"zipCode\": \"20000\",
        \"country\": \"Sri Lanka\"
      }
    },
    \"category\": \"Professional\"
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

    # Extract event ID
    EVENT_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    if [ -n "$EVENT_ID" ]; then
        echo ""
        echo "Event ID: $EVENT_ID"
        echo ""

        # Test 2.2: Retrieve the created event
        echo "Test 2.2: GET /api/events/$EVENT_ID"
        echo "Expected: HTTP 200 OK with pricing details"
        echo ""

        sleep 2

        GET_RESPONSE=$(curl -s -w "\n%{http_code}" "$STAGING_URL/api/events/$EVENT_ID")
        GET_HTTP_CODE=$(echo "$GET_RESPONSE" | tail -n1)
        GET_BODY=$(echo "$GET_RESPONSE" | head -n-1)

        echo "HTTP Status: $GET_HTTP_CODE"
        echo ""

        if [ "$GET_HTTP_CODE" -eq 200 ]; then
            echo "✅ PASS: Event retrieved successfully"
            echo ""

            # Verify single price properties
            if echo "$GET_BODY" | grep -q '"isFree":false'; then
                echo "✅ PASS: isFree field is false"
            else
                echo "❌ FAIL: isFree field should be false"
            fi

            if echo "$GET_BODY" | grep -q '"ticketPriceAmount":25'; then
                echo "✅ PASS: ticketPriceAmount is 25.00"
            else
                echo "❌ FAIL: ticketPriceAmount should be 25.00"
            fi

            if echo "$GET_BODY" | grep -q '"ticketPriceCurrency":"USD"'; then
                echo "✅ PASS: ticketPriceCurrency is USD"
            else
                echo "❌ FAIL: ticketPriceCurrency should be USD"
            fi

            # Legacy format should have pricingType: null
            if echo "$GET_BODY" | grep -q '"pricingType":null' || ! echo "$GET_BODY" | grep -q '"pricingType"'; then
                echo "✅ PASS: pricingType is null (legacy format)"
            else
                echo "⚠️ WARNING: pricingType should be null for legacy single price"
            fi

            # Should not have dual or group pricing fields
            if echo "$GET_BODY" | grep -q '"hasDualPricing":false'; then
                echo "✅ PASS: hasDualPricing is false"
            else
                echo "⚠️ WARNING: hasDualPricing should be false"
            fi

            if echo "$GET_BODY" | grep -q '"hasGroupPricing":false'; then
                echo "✅ PASS: hasGroupPricing is false"
            else
                echo "⚠️ WARNING: hasGroupPricing should be false"
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
echo "Test Scenario 2 Complete"
echo "========================================="
