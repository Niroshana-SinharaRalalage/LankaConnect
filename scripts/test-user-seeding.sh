#!/bin/bash

# Test User Seeding and Login
# Comprehensive test script to verify user seeding and authentication

API_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"

echo "=========================================="
echo "  LankaConnect User Seeding Test Script"
echo "=========================================="
echo ""

# Test users to create
declare -a USERS=(
    "admin@lankaconnect.com:Admin@123:AdminManager"
    "admin1@lankaconnect.com:Admin@123:Admin"
    "organizer@lankaconnect.com:Organizer@123:EventOrganizer"
    "user@lankaconnect.com:User@123:GeneralUser"
)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Seed users
echo -e "${YELLOW}[STEP 1] Seeding users...${NC}"
SEED_RESPONSE=$(curl -s -X POST \
    "${API_URL}/Admin/seed?seedType=users" \
    -H "Content-Type: application/json" \
    -w "\n%{http_code}")

SEED_HTTP_CODE=$(echo "$SEED_RESPONSE" | tail -1)
SEED_BODY=$(echo "$SEED_RESPONSE" | head -1)

echo "HTTP Status: $SEED_HTTP_CODE"
echo "Response: $SEED_BODY"
echo ""

if [ "$SEED_HTTP_CODE" != "200" ]; then
    echo -e "${RED}❌ FAILED: Seeding returned HTTP $SEED_HTTP_CODE${NC}"
    echo "Response body:"
    echo "$SEED_BODY"
    exit 1
fi

echo -e "${GREEN}✅ PASSED: Seeding returned HTTP 200${NC}"
echo ""

# Step 2: Test logins
echo -e "${YELLOW}[STEP 2] Testing user logins...${NC}"
echo ""

PASSED=0
FAILED=0

for USER_DATA in "${USERS[@]}"; do
    IFS=':' read -r EMAIL PASSWORD ROLE <<< "$USER_DATA"

    echo "Testing: $EMAIL ($ROLE)"

    LOGIN_RESPONSE=$(curl -s -X POST \
        "${API_URL}/Auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"email\": \"$EMAIL\", \"password\": \"$PASSWORD\", \"ipAddress\": \"127.0.0.1\"}" \
        -w "\n%{http_code}")

    LOGIN_HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -1)
    LOGIN_BODY=$(echo "$LOGIN_RESPONSE" | head -1)

    if [ "$LOGIN_HTTP_CODE" = "200" ]; then
        echo -e "  ${GREEN}✅ LOGIN SUCCESS${NC}"

        # Extract token info
        TOKEN=$(echo "$LOGIN_BODY" | jq -r '.data.accessToken' 2>/dev/null)
        USER_ID=$(echo "$LOGIN_BODY" | jq -r '.data.id' 2>/dev/null)
        USER_ROLE=$(echo "$LOGIN_BODY" | jq -r '.data.role' 2>/dev/null)

        echo "  - User ID: $USER_ID"
        echo "  - Role: $USER_ROLE"
        echo "  - Token: ${TOKEN:0:20}..."

        ((PASSED++))
    else
        echo -e "  ${RED}❌ LOGIN FAILED (HTTP $LOGIN_HTTP_CODE)${NC}"
        echo "  Response: $LOGIN_BODY"
        ((FAILED++))
    fi

    echo ""
done

# Step 3: Summary
echo "=========================================="
echo "  TEST SUMMARY"
echo "=========================================="
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ ALL TESTS PASSED!${NC}"
    exit 0
else
    echo -e "${RED}❌ SOME TESTS FAILED${NC}"
    exit 1
fi
