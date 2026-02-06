#!/bin/bash

# Simple API Endpoint Test - No Stripe signature required
# Tests if the webhook endpoint is reachable and responds

API_URL="${1:-https://lankaconnect-api.azurewebsites.net/api/payments/webhook}"

echo "=========================================="
echo "Simple Webhook Endpoint Reachability Test"
echo "=========================================="
echo "URL: $API_URL"
echo ""
echo "Testing if endpoint is reachable..."
echo ""

# Simple POST request with minimal payload
RESPONSE=$(curl -i -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: invalid_signature_for_test" \
  -d '{"test": "reachability"}' \
  --connect-timeout 10 \
  --max-time 30 \
  -w "\nHTTP_CODE:%{http_code}\n" \
  2>&1)

echo "$RESPONSE"
echo ""
echo "=========================================="
echo "Interpreting Results:"
echo "=========================================="

if echo "$RESPONSE" | grep -q "HTTP_CODE:400"; then
  echo "✅ GOOD: Endpoint returned 400 Bad Request"
  echo "   This means the endpoint IS reachable and working."
  echo "   The 400 is expected because we sent an invalid signature."
  echo ""
  echo "➡️  Next step: Check Stripe webhook configuration"
elif echo "$RESPONSE" | grep -q "HTTP_CODE:500"; then
  echo "⚠️  PARTIAL: Endpoint returned 500 Internal Server Error"
  echo "   Endpoint is reachable but application has an error."
  echo ""
  echo "➡️  Next step: Check Azure container logs for errors"
elif echo "$RESPONSE" | grep -q "HTTP_CODE:404"; then
  echo "❌ PROBLEM: Endpoint returned 404 Not Found"
  echo "   The webhook endpoint is not registered/deployed."
  echo ""
  echo "➡️  Next step: Verify deployment and routing configuration"
elif echo "$RESPONSE" | grep -q "Could not resolve host\|Connection refused\|Connection timed out"; then
  echo "❌ PROBLEM: Cannot connect to server"
  echo "   The API is not accessible at this URL."
  echo ""
  echo "➡️  Next step: Verify Azure App Service is running"
else
  echo "❓ UNKNOWN: Unexpected response"
  echo ""
  echo "➡️  Next step: Manual investigation required"
fi
echo "=========================================="
