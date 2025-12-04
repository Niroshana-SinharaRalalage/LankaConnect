#!/bin/bash
# Phase 6 E2E API Test - Scenario 3: Create Dual Price Event (Adult/Child)
# Tests age-based dual pricing with adult and child prices

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TEST_NAME="Scenario 3: Dual Price Event (Adult/Child)"
TIMESTAMP=$(date +%s)

echo "========================================="
echo "$TEST_NAME"
echo "========================================="
echo ""

# Test 3: Create Dual Price Event
echo "Test 3.1: POST /api/events (Dual Price Event)"
echo "Expected: HTTP 201 Created with adult=$30, child=$15, age limit=12"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$STAGING_URL/api/events" \
  -H "Content-Type: application/json" \
  -d "{
    \"title\": \"API Test - Dual Price Family Event $TIMESTAMP\",
    \"description\": \"Family-friendly event with dual pricing. Adults: \$30, Children (under 12): \$15.\",
    \"startDate\": \"2025-12-17T10:00:00Z\",
    \"endDate\": \"2025-12-17T16:00:00Z\",
    \"capacity\": 150,
    \"isFree\": false,
    \"pricing\": {
      \"type\": \"AgeDual\",
      \"adultPrice\": {
        \"amount\": 30.00,
        \"currency\": \"USD\"
      },
      \"childPrice\": {
        \"amount\": 15.00,
        \"currency\": \"USD\"
      },
      \"childAgeLimit\": 12
    },
    \"location\": {
      \"address\": {
        \"street\": \"789 Family Park\",
        \"city\": \"Galle\",
        \"state\": \"Southern\",
        \"zipCode\": \"80000\",
        \"country\": \"Sri Lanka\"
      }
    },
    \"category\": \"Cultural\"
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

        # Test 3.2: Retrieve the created event
        echo "Test 3.2: GET /api/events/$EVENT_ID"
        echo "Expected: HTTP 200 OK with dual pricing structure"
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

            # Verify dual pricing properties
            if echo "$GET_BODY" | grep -q '"isFree":false'; then
                echo "✅ PASS: isFree field is false"
            else
                echo "❌ FAIL: isFree field should be false"
            fi

            if echo "$GET_BODY" | grep -q '"pricingType":"AgeDual"'; then
                echo "✅ PASS: pricingType is AgeDual"
            else
                echo "❌ FAIL: pricingType should be AgeDual"
            fi

            if echo "$GET_BODY" | grep -q '"hasDualPricing":true'; then
                echo "✅ PASS: hasDualPricing is true"
            else
                echo "❌ FAIL: hasDualPricing should be true"
            fi

            if echo "$GET_BODY" | grep -q '"adultPriceAmount":30'; then
                echo "✅ PASS: adultPriceAmount is 30.00"
            else
                echo "❌ FAIL: adultPriceAmount should be 30.00"
            fi

            if echo "$GET_BODY" | grep -q '"childPriceAmount":15'; then
                echo "✅ PASS: childPriceAmount is 15.00"
            else
                echo "❌ FAIL: childPriceAmount should be 15.00"
            fi

            if echo "$GET_BODY" | grep -q '"childAgeLimit":12'; then
                echo "✅ PASS: childAgeLimit is 12"
            else
                echo "❌ FAIL: childAgeLimit should be 12"
            fi

            # Should not have group pricing
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
echo "Test Scenario 3 Complete"
echo "========================================="
