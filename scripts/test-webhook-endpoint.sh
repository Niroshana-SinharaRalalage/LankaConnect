#!/bin/bash

# Test Stripe Webhook Endpoint Directly
# This script simulates a Stripe checkout.session.completed webhook

# Configuration
API_URL="${1:-https://lankaconnect-api.azurewebsites.net/api/payments/webhook}"
WEBHOOK_SECRET="${STRIPE_WEBHOOK_SECRET}"

# Sample webhook payload (checkout.session.completed)
# Replace the IDs with actual values from your test
EVENT_ID="evt_test_$(date +%s)"
SESSION_ID="cs_test_$(date +%s)"
REGISTRATION_ID="your-registration-guid-here"
EVENT_GUID="your-event-guid-here"

# Create test payload
PAYLOAD=$(cat <<EOF
{
  "id": "$EVENT_ID",
  "object": "event",
  "api_version": "2023-10-16",
  "created": $(date +%s),
  "data": {
    "object": {
      "id": "$SESSION_ID",
      "object": "checkout.session",
      "amount_total": 5000,
      "currency": "usd",
      "customer": "cus_test_123",
      "payment_intent": "pi_test_123",
      "payment_status": "paid",
      "status": "complete",
      "metadata": {
        "registration_id": "$REGISTRATION_ID",
        "event_id": "$EVENT_GUID"
      }
    }
  },
  "livemode": false,
  "pending_webhooks": 1,
  "request": {
    "id": null,
    "idempotency_key": null
  },
  "type": "checkout.session.completed"
}
EOF
)

echo "=========================================="
echo "Testing Stripe Webhook Endpoint"
echo "=========================================="
echo "URL: $API_URL"
echo "Event ID: $EVENT_ID"
echo "Session ID: $SESSION_ID"
echo "Registration ID: $REGISTRATION_ID"
echo "Event GUID: $EVENT_GUID"
echo ""

# Generate signature (simplified - you'll need actual Stripe signing)
TIMESTAMP=$(date +%s)
SIGNATURE="t=${TIMESTAMP},v1=test_signature"

echo "Sending webhook request..."
echo ""

# Make request
RESPONSE=$(curl -i -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: $SIGNATURE" \
  -d "$PAYLOAD" \
  2>&1)

echo "Response:"
echo "$RESPONSE"
echo ""

# Check status code
STATUS_CODE=$(echo "$RESPONSE" | grep -oP 'HTTP/\S+ \K\d+' | head -1)

echo "=========================================="
if [ "$STATUS_CODE" == "200" ]; then
  echo "✅ Webhook endpoint returned 200 OK"
elif [ "$STATUS_CODE" == "400" ]; then
  echo "⚠️  Webhook endpoint returned 400 Bad Request"
  echo "This is expected if signature verification fails"
  echo "Check if webhook endpoint is reachable"
elif [ "$STATUS_CODE" == "500" ]; then
  echo "❌ Webhook endpoint returned 500 Internal Server Error"
  echo "Check application logs for errors"
else
  echo "❌ Unexpected status code: $STATUS_CODE"
fi
echo "=========================================="
