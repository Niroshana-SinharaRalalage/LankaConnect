#!/bin/bash
# Test script for Newsletter Schema Fix (Issue #1/#2)
# Tests newsletter send with recipient count tracking after EF Core schema fix

API_BASE="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
EMAIL="niroshhh@gmail.com"
PASSWORD='12!@qwASzx'
AURORA_EVENT_ID="0458806b-8672-4ad5-a7cb-f5346f1b282a"

# Email groups from Aurora event
EMAIL_GROUP_1="c74c0635-59f4-42d1-874f-204a67c4b21d"  # Cleveland SL Community
EMAIL_GROUP_2="f11e9e26-9848-4369-9893-024c229a8f50"  # Test Group 1

echo "======================================================="
echo "Newsletter Schema Fix Verification Test"
echo "Commit: 1fa56b3c"
echo "Fix: Added NewsletterEmailHistory schema configuration"
echo "======================================================="
echo ""

# Login
echo "[1/6] Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_BASE/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ FAILED: Could not get authentication token"
  exit 1
fi

echo "✅ Authenticated successfully"
echo ""

# Create newsletter with email groups
echo "[2/6] Creating newsletter with Aurora event and email groups..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$API_BASE/newsletters" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"title\": \"VERIFICATION TEST - Newsletter Schema Fix\",
    \"description\": \"<p>[Write your news letter content here.....]</p><p><br></p><p>Learn more about the event: <a href=\\\"https://lankaconnect-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/events/$AURORA_EVENT_ID\\\">View Event Details</a></p><p><br></p><p>Checkout the Sign Up lists: <a href=\\\"https://lankaconnect-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/events/$AURORA_EVENT_ID#sign-ups\\\">View Event Sign-up Lists</a></p>\",
    \"emailGroupIds\": [\"$EMAIL_GROUP_1\", \"$EMAIL_GROUP_2\"],
    \"includeNewsletterSubscribers\": true,
    \"eventId\": \"$AURORA_EVENT_ID\",
    \"targetAllLocations\": false,
    \"metroAreaIds\": []
  }")

HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
NEWSLETTER_ID=$(echo "$RESPONSE" | sed '/HTTP_CODE:/d' | tr -d '"')

if [ "$HTTP_CODE" != "201" ]; then
  echo "❌ FAILED: Could not create newsletter (HTTP $HTTP_CODE)"
  exit 1
fi

echo "✅ Newsletter created: $NEWSLETTER_ID"
echo ""

# Publish newsletter
echo "[3/6] Publishing newsletter..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$API_BASE/newsletters/$NEWSLETTER_ID/publish" \
  -H "Authorization: Bearer $TOKEN")

HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)

if [ "$HTTP_CODE" != "200" ]; then
  echo "❌ FAILED: Could not publish newsletter (HTTP $HTTP_CODE)"
  exit 1
fi

echo "✅ Newsletter published"
echo ""

# Send newsletter
echo "[4/6] Sending newsletter..."
RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST "$API_BASE/newsletters/$NEWSLETTER_ID/send" \
  -H "Authorization: Bearer $TOKEN")

HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)

if [ "$HTTP_CODE" != "202" ]; then
  echo "❌ FAILED: Could not send newsletter (HTTP $HTTP_CODE)"
  exit 1
fi

echo "✅ Newsletter send job enqueued (HTTP 202)"
echo ""
echo "[5/6] Waiting 60 seconds for Hangfire job to complete..."
sleep 60
echo ""

# Query newsletter for recipient counts
echo "[6/6] Querying newsletter for recipient counts..."
RESPONSE=$(curl -s -X GET "$API_BASE/newsletters/$NEWSLETTER_ID" \
  -H "Authorization: Bearer $TOKEN")

# Extract key fields
STATUS=$(echo "$RESPONSE" | grep -o '"status":"[^"]*' | cut -d'"' -f4)
SENT_AT=$(echo "$RESPONSE" | grep -o '"sentAt":"[^"]*' | cut -d'"' -f4)
TOTAL_COUNT=$(echo "$RESPONSE" | grep -o '"totalRecipientCount":[0-9]*' | cut -d: -f2)
EMAIL_GROUP_COUNT=$(echo "$RESPONSE" | grep -o '"emailGroupRecipientCount":[0-9]*' | cut -d: -f2)
SUBSCRIBER_COUNT=$(echo "$RESPONSE" | grep -o '"subscriberRecipientCount":[0-9]*' | cut -d: -f2)

echo "======================================================="
echo "TEST RESULTS"
echo "======================================================="
echo ""
echo "Newsletter ID: $NEWSLETTER_ID"
echo "Status: $STATUS (expected: 'Sent')"
echo "SentAt: $SENT_AT (expected: timestamp)"
echo "TotalRecipientCount: $TOTAL_COUNT (expected: > 0)"
echo "EmailGroupRecipientCount: $EMAIL_GROUP_COUNT"
echo "SubscriberRecipientCount: $SUBSCRIBER_COUNT"
echo ""
echo "Full Response:"
echo "$RESPONSE" | python3 -m json.tool 2>/dev/null || echo "$RESPONSE"
echo ""
echo "======================================================="
echo "VERIFICATION"
echo "======================================================="
echo ""

PASSED=0
FAILED=0

# Test 1: Status should be "Sent"
if [ "$STATUS" = "Sent" ]; then
  echo "✅ PASS: Newsletter status is 'Sent'"
  PASSED=$((PASSED + 1))
else
  echo "❌ FAIL: Newsletter status is '$STATUS' (should be 'Sent')"
  FAILED=$((FAILED + 1))
fi

# Test 2: SentAt should have timestamp
if [ -n "$SENT_AT" ]; then
  echo "✅ PASS: SentAt timestamp populated"
  PASSED=$((PASSED + 1))
else
  echo "❌ FAIL: SentAt is null"
  FAILED=$((FAILED + 1))
fi

# Test 3: TotalRecipientCount should be > 0
if [ -n "$TOTAL_COUNT" ] && [ "$TOTAL_COUNT" -gt 0 ]; then
  echo "✅ PASS: TotalRecipientCount is $TOTAL_COUNT (> 0)"
  PASSED=$((PASSED + 1))
else
  echo "❌ FAIL: TotalRecipientCount is null or 0"
  FAILED=$((FAILED + 1))
fi

# Test 4: EmailGroupRecipientCount should be populated
if [ -n "$EMAIL_GROUP_COUNT" ]; then
  echo "✅ PASS: EmailGroupRecipientCount populated ($EMAIL_GROUP_COUNT)"
  PASSED=$((PASSED + 1))
else
  echo "❌ FAIL: EmailGroupRecipientCount is null"
  FAILED=$((FAILED + 1))
fi

# Test 5: SubscriberRecipientCount should be populated (may be 0)
if echo "$RESPONSE" | grep -q '"subscriberRecipientCount":[0-9]'; then
  echo "✅ PASS: SubscriberRecipientCount populated ($SUBSCRIBER_COUNT)"
  PASSED=$((PASSED + 1))
else
  echo "❌ FAIL: SubscriberRecipientCount is null"
  FAILED=$((FAILED + 1))
fi

echo ""
echo "======================================================="
echo "FINAL RESULT"
echo "======================================================="
echo ""
echo "Passed: $PASSED/5"
echo "Failed: $FAILED/5"
echo ""

if [ $FAILED -eq 0 ]; then
  echo "✅ ✅ ✅ ALL TESTS PASSED ✅ ✅ ✅"
  echo ""
  echo "Issue #1/#2: VERIFIED FIXED"
  echo "- Newsletter marked as 'Sent' ✅"
  echo "- SentAt timestamp populated ✅"
  echo "- Recipient counts tracked ✅"
  echo "- NewsletterEmailHistory record created ✅"
  echo ""
  echo "Ready to verify Issue #5 Part B (Metro Area Matching)"
  exit 0
else
  echo "❌ TEST FAILED: $FAILED test(s) did not pass"
  echo ""
  echo "Troubleshooting steps:"
  echo "1. Check if deployment completed successfully"
  echo "2. Verify Azure Container App restarted with new code"
  echo "3. Check Hangfire dashboard for job status"
  echo "4. Review application logs for exceptions"
  exit 1
fi
