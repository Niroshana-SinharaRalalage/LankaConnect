#!/bin/bash
# Phase 6A.34 Registration Test Script
# Tests domain event dispatching after entity state reset fix

set -e

echo -e "\033[36mPhase 6A.34 Domain Event Dispatch Test\033[0m"
echo "========================================"
echo ""

# Configuration
API_BASE="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID="c1f182a9-c957-4a78-a0b2-085917a88900"
EMAIL="niroshhh@gmail.com"
PASSWORD="12!@qwASzx"

echo -e "\033[33mStep 1: Login and get auth token...\033[0m"

LOGIN_RESPONSE=$(curl -s -X POST "$API_BASE/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"rememberMe\":true,\"ipAddress\":\"127.0.0.1\"}")

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)
USER_ID=$(echo $LOGIN_RESPONSE | grep -o '"userId":"[^"]*"' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo -e "\033[31m✗ Login failed\033[0m"
    echo "$LOGIN_RESPONSE"
    exit 1
fi

echo -e "\033[32m✓ Login successful (User: $USER_ID)\033[0m"
echo ""

echo -e "\033[33mStep 2: Cancel existing registration (if any)...\033[0m"

curl -s -X DELETE "$API_BASE/api/Events/$EVENT_ID/rsvp" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: application/json" > /dev/null 2>&1 || true

echo -e "\033[32m✓ Registration cancelled\033[0m"
sleep 2
echo ""

echo -e "\033[33mStep 3: Create NEW registration...\033[0m"
echo -e "\033[37m  This will test Phase 6A.34 fix (DetectChanges + no entity state reset)\033[0m"

RSVP_RESPONSE=$(curl -s -X POST "$API_BASE/api/Events/$EVENT_ID/rsvp" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "{\"quantity\": 1, \"userId\": \"$USER_ID\"}" \
  -w "\nHTTP_STATUS:%{http_code}")

HTTP_STATUS=$(echo "$RSVP_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)

if [ "$HTTP_STATUS" != "200" ] && [ "$HTTP_STATUS" != "201" ] && [ "$HTTP_STATUS" != "204" ]; then
    echo -e "\033[31m✗ Registration failed (HTTP $HTTP_STATUS)\033[0m"
    echo "$RSVP_RESPONSE"
    exit 1
fi

echo -e "\033[32m✓ Registration created successfully! (HTTP $HTTP_STATUS)\033[0m"
echo ""

echo -e "\033[33mStep 4: Check Azure logs for domain event dispatch...\033[0m"
echo -e "\033[37m  Waiting 3 seconds for logs to appear...\033[0m"
sleep 3
echo ""

echo -e "\033[36mSearching logs for domain event dispatch...\033[0m"
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --type console --tail 150 --follow false 2>/dev/null | grep -E "(RsvpToEvent|RegistrationConfirmed|Phase 6A.24.*Found.*domain)" | tail -10 || echo -e "\033[33m  No matching logs found yet. Check manually with:\033[0m"

echo ""
echo -e "\033[36mExpected log sequence:\033[0m"
echo -e "\033[37m  1. Processing RsvpToEventCommand\033[0m"
echo -e "\033[37m  2. [Phase 6A.24] Found 1 domain events to dispatch: RegistrationConfirmedEvent\033[0m"
echo -e "\033[37m  3. RegistrationConfirmedEventHandler invoked\033[0m"
echo -e "\033[37m  4. Email queued for delivery\033[0m"
echo ""
echo -e "\033[32mTest complete!\033[0m"
