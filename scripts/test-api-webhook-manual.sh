#!/bin/bash

# Test webhook endpoint by sending a test checkout.session.completed event
# This simulates what Stripe sends when resending an event

STAGING_URL="https://lankaconnect-api-staging.nicecoast-40f222b0.australiaeast.azurecontainerapps.io"

# Use the actual session ID from the previous successful payment
SESSION_ID="cs_test_a1oLcAf0EqvyJFg7XwaPL82aS9uQ0PQrL1fKWTZrvYNHOvSJOa7QShBKE3"

echo "Testing webhook endpoint with checkout.session.completed event..."
echo "Session ID: $SESSION_ID"
echo ""

# Send POST request to webhook endpoint
curl -X POST "$STAGING_URL/api/payments/webhook" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: mock_signature_for_testing" \
  -d '{
    "id": "evt_test_webhook_manual",
    "object": "event",
    "api_version": "2025-11-17.clover",
    "type": "checkout.session.completed",
    "data": {
      "object": {
        "id": "'"$SESSION_ID"'",
        "object": "checkout.session",
        "payment_status": "paid",
        "status": "complete"
      }
    }
  }' \
  -w "\n\nHTTP Status: %{http_code}\n" \
  -v

echo ""
echo "Check Azure logs for webhook processing"
