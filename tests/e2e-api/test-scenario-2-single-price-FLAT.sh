#!/bin/bash
# Phase 6 E2E API Test - Scenario 2: Create Single Price Event (CORRECTED FLAT FORMAT)
# Tests single price event creation using FLAT JSON matching CreateEventCommand

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TEST_NAME="Scenario 2: Single Price Event Creation (FLAT FORMAT)"
TIMESTAMP=$(date +%s)

echo "========================================="
echo "$TEST_NAME"
echo "========================================="
echo ""
echo "First, logging in to get fresh auth token..."
echo ""

# Get fresh token
LOGIN_RESPONSE=$(curl -s -X POST "$STAGING_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "niroshhh2@gmail.com",
    "password": "12!@qwASzx",
    "rememberMe": true,
    "ipAddress": "string"
  }')

AUTH_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiZmFmOTk0Y2YtZjQ1Mi00YjdmLWExZGYtMGRmOWFjZTE0OTU0IiwiaWF0IjoxNzY0OTAyMDAyLCJuYmYiOjE3NjQ5MDIwMDIsImV4cCI6MTc2NDkwMzgwMiwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.cMZsBhVlGiQWxXZgFgr3s-vSUegOBGYjjF1vXAfhVrs"

if [ -z "$AUTH_TOKEN" ]; then
    echo "❌ FAIL: Could not obtain auth token"
    exit 1
fi

echo "✅ Auth token obtained"
echo ""

# Test 2: Create Single Price Event with FLAT JSON
echo "Test 2.1: POST /api/events (Single Price Event - FLAT FORMAT)"
echo "Expected: HTTP 201 Created with ticketPriceAmount=25.00"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$STAGING_URL/api/events" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d "{
    \"title\": \"API Test - Single Price FLAT $TIMESTAMP\",
    \"description\": \"This is a single price event created via API test using FLAT JSON format. Ticket price: \$25.00 USD per person.\",
    \"startDate\": \"2025-12-16T14:00:00Z\",
    \"endDate\": \"2025-12-16T18:00:00Z\",
    \"capacity\": 50,
    \"category\": \"Professional\",
    \"locationAddress\": \"456 Event Avenue\",
    \"locationCity\": \"Kandy\",
    \"locationState\": \"Central\",
    \"locationZipCode\": \"20000\",
    \"locationCountry\": \"Sri Lanka\",
    \"ticketPriceAmount\": 25.00,
    \"ticketPriceCurrency\": \"USD\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

echo "HTTP Status: $HTTP_CODE"
echo ""

if [ "$HTTP_CODE" -eq 201 ] || [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ PASS: Event created successfully with FLAT JSON format"
    echo ""
    echo "Response Body:"
    echo "$BODY"

    EVENT_ID="$BODY"
    EVENT_ID=$(echo "$EVENT_ID" | tr -d '"')

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
            echo "Event Details:"
            echo "$GET_BODY" | head -50
        else
            echo "❌ FAIL: Could not retrieve event (HTTP $GET_HTTP_CODE)"
        fi
    fi

else
    echo "❌ FAIL: Event creation failed (HTTP $HTTP_CODE)"
    echo ""
    echo "Response Body:"
    echo "$BODY"
fi

echo ""
echo "========================================="
echo "Test Scenario 2 Complete (FLAT FORMAT)"
echo "========================================="
