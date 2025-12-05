#!/bin/bash
# Phase 6 E2E API Test - Scenario 6: Performance Testing
# Tests API response times and performance metrics

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TEST_NAME="Scenario 6: Performance Testing"
TIMESTAMP=$(date +%s)

# Auth token from login endpoint (expires at 2025-12-05T00:04:57Z)
AUTH_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiNTk3ODk1YzEtOTI0My00ZmE2LTgxYTEtMjJhNjQ3M2M5YzFlIiwiaWF0IjoxNzY0ODkxMjk3LCJuYmYiOjE3NjQ4OTEyOTcsImV4cCI6MTc2NDg5MzA5NywiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.PbbaqS8Sdh3YBPce2LNNX8aX1loC1RMVR4X4Do5QKCA"

echo "========================================="
echo "$TEST_NAME"
echo "========================================="
echo ""

# Performance targets (from Phase 6 E2E test plan)
TARGET_CREATE_TIME=2.0  # seconds
TARGET_LIST_TIME=1.0    # seconds

echo "Performance Targets:"
echo "  Event Creation: < ${TARGET_CREATE_TIME}s"
echo "  Event List: < ${TARGET_LIST_TIME}s"
echo ""

# Test 6.1: Event List Performance
echo "Test 6.1: GET /api/events?pageSize=50 (Performance)"
echo "Target: < ${TARGET_LIST_TIME}s response time"
echo ""

START_TIME=$(date +%s.%N)

RESPONSE=$(curl -s -w "\n%{http_code}" "$STAGING_URL/api/events?pageSize=50")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

END_TIME=$(date +%s.%N)
DURATION=$(echo "$END_TIME - $START_TIME" | bc)

echo "HTTP Status: $HTTP_CODE"
echo "Response Time: ${DURATION}s"
echo ""

if [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ PASS: Event list retrieved successfully"

    # Check if response time meets target
    IS_UNDER_TARGET=$(echo "$DURATION < $TARGET_LIST_TIME" | bc)
    if [ "$IS_UNDER_TARGET" -eq 1 ]; then
        echo "✅ PASS: Response time under target (${DURATION}s < ${TARGET_LIST_TIME}s)"
    else
        echo "⚠️ WARNING: Response time exceeds target (${DURATION}s > ${TARGET_LIST_TIME}s)"
    fi
else
    echo "❌ FAIL: Event list failed (HTTP $HTTP_CODE)"
fi

echo ""

# Test 6.2: Event Creation Performance
echo "Test 6.2: POST /api/events (Performance Test)"
echo "Target: < ${TARGET_CREATE_TIME}s response time"
echo ""

START_TIME=$(date +%s.%N)

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$STAGING_URL/api/events" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $AUTH_TOKEN" \
  -d "{
    \"title\": \"Performance Test Event $TIMESTAMP\",
    \"description\": \"This event is created to test API performance and response times.\",
    \"startDate\": \"2025-12-20T14:00:00Z\",
    \"endDate\": \"2025-12-20T18:00:00Z\",
    \"capacity\": 100,
    \"isFree\": false,
    \"ticketPriceAmount\": 20.00,
    \"ticketPriceCurrency\": \"USD\",
    \"location\": {
      \"address\": {
        \"street\": \"999 Performance Ave\",
        \"city\": \"Colombo\",
        \"state\": \"Western\",
        \"zipCode\": \"00100\",
        \"country\": \"Sri Lanka\"
      }
    },
    \"category\": \"Professional\"
  }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

END_TIME=$(date +%s.%N)
DURATION=$(echo "$END_TIME - $START_TIME" | bc)

echo "HTTP Status: $HTTP_CODE"
echo "Response Time: ${DURATION}s"
echo ""

if [ "$HTTP_CODE" -eq 201 ] || [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ PASS: Event created successfully"

    # Check if response time meets target
    IS_UNDER_TARGET=$(echo "$DURATION < $TARGET_CREATE_TIME" | bc)
    if [ "$IS_UNDER_TARGET" -eq 1 ]; then
        echo "✅ PASS: Response time under target (${DURATION}s < ${TARGET_CREATE_TIME}s)"
    else
        echo "⚠️ WARNING: Response time exceeds target (${DURATION}s > ${TARGET_CREATE_TIME}s)"
    fi

    # Extract event ID for retrieval test
    EVENT_ID=$(echo "$BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

    if [ -n "$EVENT_ID" ]; then
        echo ""
        echo "Event ID: $EVENT_ID"
    fi

else
    echo "❌ FAIL: Event creation failed (HTTP $HTTP_CODE)"
fi

echo ""

# Test 6.3: Individual Event Retrieval Performance
echo "Test 6.3: GET /api/events/{id} (Performance Test)"
echo "Target: < 1s response time"
echo ""

if [ -n "$EVENT_ID" ]; then
    START_TIME=$(date +%s.%N)

    RESPONSE=$(curl -s -w "\n%{http_code}" "$STAGING_URL/api/events/$EVENT_ID")
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

    END_TIME=$(date +%s.%N)
    DURATION=$(echo "$END_TIME - $START_TIME" | bc)

    echo "HTTP Status: $HTTP_CODE"
    echo "Response Time: ${DURATION}s"
    echo ""

    if [ "$HTTP_CODE" -eq 200 ]; then
        echo "✅ PASS: Event retrieved successfully"

        IS_UNDER_TARGET=$(echo "$DURATION < 1.0" | bc)
        if [ "$IS_UNDER_TARGET" -eq 1 ]; then
            echo "✅ PASS: Response time under target (${DURATION}s < 1.0s)"
        else
            echo "⚠️ WARNING: Response time exceeds target (${DURATION}s > 1.0s)"
        fi
    else
        echo "❌ FAIL: Event retrieval failed (HTTP $HTTP_CODE)"
    fi
else
    echo "⚠️ SKIP: No event ID available for retrieval test"
fi

echo ""

# Test 6.4: Health Check Performance
echo "Test 6.4: GET /health (Performance Test)"
echo "Target: < 0.5s response time"
echo ""

START_TIME=$(date +%s.%N)

RESPONSE=$(curl -s -w "\n%{http_code}" "$STAGING_URL/health")
HTTP_CODE=$(echo "$RESPONSE" | tail -n1)

END_TIME=$(date +%s.%N)
DURATION=$(echo "$END_TIME - $START_TIME" | bc)

echo "HTTP Status: $HTTP_CODE"
echo "Response Time: ${DURATION}s"
echo ""

if [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ PASS: Health check successful"

    IS_UNDER_TARGET=$(echo "$DURATION < 0.5" | bc)
    if [ "$IS_UNDER_TARGET" -eq 1 ]; then
        echo "✅ PASS: Response time under target (${DURATION}s < 0.5s)"
    else
        echo "⚠️ WARNING: Response time exceeds target (${DURATION}s > 0.5s)"
    fi
else
    echo "❌ FAIL: Health check failed (HTTP $HTTP_CODE)"
fi

echo ""

# Test 6.5: Concurrent Request Simulation (3 parallel requests)
echo "Test 6.5: Concurrent Requests Test (3 parallel)"
echo "Testing system performance under concurrent load"
echo ""

echo "Starting 3 parallel GET /api/events requests..."
START_TIME=$(date +%s.%N)

# Run 3 requests in parallel using background processes
curl -s -o /dev/null -w "Request 1: %{http_code} in %{time_total}s\n" "$STAGING_URL/api/events?pageSize=20" &
PID1=$!

curl -s -o /dev/null -w "Request 2: %{http_code} in %{time_total}s\n" "$STAGING_URL/api/events?pageSize=20" &
PID2=$!

curl -s -o /dev/null -w "Request 3: %{http_code} in %{time_total}s\n" "$STAGING_URL/api/events?pageSize=20" &
PID3=$!

# Wait for all background processes to complete
wait $PID1
wait $PID2
wait $PID3

END_TIME=$(date +%s.%N)
TOTAL_DURATION=$(echo "$END_TIME - $START_TIME" | bc)

echo ""
echo "Total Time for 3 Parallel Requests: ${TOTAL_DURATION}s"

# If concurrent requests complete in < 3s, that's good
IS_UNDER_TARGET=$(echo "$TOTAL_DURATION < 3.0" | bc)
if [ "$IS_UNDER_TARGET" -eq 1 ]; then
    echo "✅ PASS: Concurrent requests handled efficiently (${TOTAL_DURATION}s < 3.0s)"
else
    echo "⚠️ WARNING: Concurrent requests slower than expected (${TOTAL_DURATION}s > 3.0s)"
fi

echo ""
echo "========================================="
echo "Test Scenario 6 Complete"
echo "========================================="
echo ""
echo "Performance Summary:"
echo "  Event List (50 items): Response time measured"
echo "  Event Creation: Response time measured"
echo "  Event Retrieval: Response time measured"
echo "  Health Check: Response time measured"
echo "  Concurrent Load (3 parallel): Response time measured"
echo ""
echo "Note: Actual response times documented in test output above"
echo "✅ Performance testing complete - Review times against targets"
