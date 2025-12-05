#!/bin/bash
# Phase 6 E2E API Test - Scenario 4: Create Group Tiered Event (Phase 6D)
# Tests group tiered pricing with quantity-based discounts

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TEST_NAME="Scenario 4: Group Tiered Pricing (Phase 6D)"
TIMESTAMP=$(date +%s)

# Auth token from login endpoint (expires at 2025-12-05T03:03:22Z)
AUTH_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiZmFmOTk0Y2YtZjQ1Mi00YjdmLWExZGYtMGRmOWFjZTE0OTU0IiwiaWF0IjoxNzY0OTAyMDAyLCJuYmYiOjE3NjQ5MDIwMDIsImV4cCI6MTc2NDkwMzgwMiwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.cMZsBhVlGiQWxXZgFgr3s-vSUegOBGYjjF1vXAfhVrs"

echo "========================================="
echo "$TEST_NAME"
echo "========================================="
echo ""

# Test 4: Create Group Tiered Event
echo "Test 4.1: POST /api/events (Group Tiered Event)"
echo "Expected: HTTP 201 Created with 3 pricing tiers"
echo "  Tier 1: 1-2 people @ \$25.00/person"
echo "  Tier 2: 3-5 people @ \$20.00/person"
echo "  Tier 3: 6+ people @ \$15.00/person"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$STAGING_URL/api/events" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d "{
    \"title\": \"API Test - Group Tiered Corporate Event $TIMESTAMP\",
    \"description\": \"Corporate event with group discounts. Bring more colleagues for better rates!\",
    \"startDate\": \"2025-12-18T09:00:00Z\",
    \"endDate\": \"2025-12-18T17:00:00Z\",
    \"capacity\": 200,
    \"isFree\": false,
    \"pricing\": {
      \"type\": \"GroupTiered\",
      \"groupTiers\": [
        {
          \"minGroupSize\": 1,
          \"maxGroupSize\": 2,
          \"pricePerPerson\": {
            \"amount\": 25.00,
            \"currency\": \"USD\"
          }
        },
        {
          \"minGroupSize\": 3,
          \"maxGroupSize\": 5,
          \"pricePerPerson\": {
            \"amount\": 20.00,
            \"currency\": \"USD\"
          }
        },
        {
          \"minGroupSize\": 6,
          \"maxGroupSize\": null,
          \"pricePerPerson\": {
            \"amount\": 15.00,
            \"currency\": \"USD\"
          }
        }
      ]
    },
    \"location\": {
      \"address\": {
        \"street\": \"321 Corporate Plaza\",
        \"city\": \"Colombo\",
        \"state\": \"Western\",
        \"zipCode\": \"00200\",
        \"country\": \"Sri Lanka\"
      }
    },
    \"category\": \"Business\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

echo "HTTP Status: $HTTP_CODE"
echo ""

if [ "$HTTP_CODE" -eq 201 ] || [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ PASS: Event created successfully"
    echo ""
    echo "Response Body:"
    echo "$BODY" | head -80

    # Extract event ID
    EVENT_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    if [ -n "$EVENT_ID" ]; then
        echo ""
        echo "Event ID: $EVENT_ID"
        echo ""

        # Test 4.2: Retrieve the created event
        echo "Test 4.2: GET /api/events/$EVENT_ID"
        echo "Expected: HTTP 200 OK with group tiered pricing structure"
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

            # Verify group tiered pricing properties
            if echo "$GET_BODY" | grep -q '"isFree":false'; then
                echo "✅ PASS: isFree field is false"
            else
                echo "❌ FAIL: isFree field should be false"
            fi

            if echo "$GET_BODY" | grep -q '"pricingType":"GroupTiered"'; then
                echo "✅ PASS: pricingType is GroupTiered"
            else
                echo "❌ FAIL: pricingType should be GroupTiered"
            fi

            if echo "$GET_BODY" | grep -q '"hasGroupPricing":true'; then
                echo "✅ PASS: hasGroupPricing is true"
            else
                echo "❌ FAIL: hasGroupPricing should be true"
            fi

            # Verify tier 1 (1-2 @ $25)
            if echo "$GET_BODY" | grep -q '"minGroupSize":1' && echo "$GET_BODY" | grep -q '"maxGroupSize":2'; then
                echo "✅ PASS: Tier 1 range (1-2) found"
            else
                echo "❌ FAIL: Tier 1 range should be 1-2"
            fi

            # Verify tier 2 (3-5 @ $20)
            if echo "$GET_BODY" | grep -q '"minGroupSize":3' && echo "$GET_BODY" | grep -q '"maxGroupSize":5'; then
                echo "✅ PASS: Tier 2 range (3-5) found"
            else
                echo "❌ FAIL: Tier 2 range should be 3-5"
            fi

            # Verify tier 3 (6+ @ $15)
            if echo "$GET_BODY" | grep -q '"minGroupSize":6'; then
                echo "✅ PASS: Tier 3 starts at 6"
            else
                echo "❌ FAIL: Tier 3 should start at 6"
            fi

            # Count number of tiers
            TIER_COUNT=$(echo "$GET_BODY" | grep -o '"minGroupSize"' | wc -l)
            if [ "$TIER_COUNT" -eq 3 ]; then
                echo "✅ PASS: 3 pricing tiers found"
            else
                echo "❌ FAIL: Expected 3 tiers, found $TIER_COUNT"
            fi

            # Should not have dual pricing
            if echo "$GET_BODY" | grep -q '"hasDualPricing":false'; then
                echo "✅ PASS: hasDualPricing is false"
            else
                echo "⚠️ WARNING: hasDualPricing should be false"
            fi

            echo ""
            echo "Full Response (showing tier structure):"
            echo "$GET_BODY" | head -100

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
echo "Test Scenario 4 Complete"
echo "========================================="
