#!/bin/bash
# Test Event Publication Email - Phase 6A.41
# This script tests the event publication email flow after database fix

# Configuration
API_BASE="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoQGdtYWlsLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmlyb3NoYW5hIFNpbmhhcmEgUmFsYWxhZ2UiLCJyb2xlIjoiRXZlbnRPcmdhbml6ZXIiLCJmaXJzdE5hbWUiOiJOaXJvc2hhbmEiLCJsYXN0TmFtZSI6IlNpbmhhcmEgUmFsYWxhZ2UiLCJpc0FjdGl2ZSI6InRydWUiLCJqdGkiOiI2MGY3ZDdkYi01ZDZhLTRkMjktYWE0Yi0xNzQ1OTNhNGUwMDMiLCJpYXQiOjE3NjY2MDUwNDEsIm5iZiI6MTc2NjYwNTA0MSwiZXhwIjoxNzY2NjA2ODQxLCJpc3MiOiJodHRwczovL2xhbmthY29ubmVjdC1hcGktc3RhZ2luZy5henVyZXdlYnNpdGVzLm5ldCIsImF1ZCI6Imh0dHBzOi8vbGFua2Fjb25uZWN0LXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQifQ.QKVbrJ7asqpLfOo1g9W02oQQ2db3P9CSlV9N43UipZ8"

echo "=== Phase 6A.41: Testing Event Publication Email ==="
echo ""

# Step 1: Create a new draft event
echo "Step 1: Creating new draft event..."
cat > /tmp/create-event.json << 'EOF'
{
  "title": "Test Event - Phase 6A.41 Email Fix",
  "description": "This event tests the email sending fix for event publication",
  "category": "Social",
  "startDate": "2025-12-30T18:00:00Z",
  "endDate": "2025-12-30T21:00:00Z",
  "capacity": 50,
  "location": {
    "address": {
      "street": "123 Test Street",
      "city": "Columbus",
      "state": "OH",
      "zipCode": "43215",
      "country": "USA"
    }
  },
  "pricing": {
    "type": "Free"
  }
}
EOF

CREATE_RESPONSE=$(curl -X POST "${API_BASE}/api/events" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d @/tmp/create-event.json \
  -s -w "\nHTTP_STATUS:%{http_code}")

HTTP_STATUS=$(echo "$CREATE_RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
RESPONSE_BODY=$(echo "$CREATE_RESPONSE" | sed '/HTTP_STATUS/d')

echo "Response status: $HTTP_STATUS"

if [ "$HTTP_STATUS" != "201" ] && [ "$HTTP_STATUS" != "200" ]; then
  echo "ERROR: Failed to create event"
  echo "Response: $RESPONSE_BODY"
  exit 1
fi

# Extract event ID from response
EVENT_ID=$(echo "$RESPONSE_BODY" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$EVENT_ID" ]; then
  # Try alternative JSON parsing
  EVENT_ID=$(echo "$RESPONSE_BODY" | grep -o '"eventId":"[^"]*"' | head -1 | cut -d'"' -f4)
fi

echo "Event created with ID: $EVENT_ID"
echo ""

# Step 2: Publish the event
echo "Step 2: Publishing event to trigger EventPublishedEvent..."
PUBLISH_RESPONSE=$(curl -X POST "${API_BASE}/api/events/${EVENT_ID}/publish" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -s -w "\nHTTP_STATUS:%{http_code}")

PUBLISH_STATUS=$(echo "$PUBLISH_RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
PUBLISH_BODY=$(echo "$PUBLISH_RESPONSE" | sed '/HTTP_STATUS/d')

echo "Publish response status: $PUBLISH_STATUS"

if [ "$PUBLISH_STATUS" != "200" ]; then
  echo "ERROR: Failed to publish event"
  echo "Response: $PUBLISH_BODY"
  exit 1
fi

echo "Event published successfully!"
echo ""

# Step 3: Instructions for monitoring
echo "=== Next Steps ==="
echo ""
echo "1. Check Azure Container Logs:"
echo "   az containerapp logs show --name lankaconnect-api-staging --resource-group LankaConnect-Staging --tail 100 --follow false --format text | grep -E 'event-published|EventPublished|RCA-'"
echo ""
echo "2. Look for SUCCESS indicators:"
echo "   ✅ 'Template event-published rendered from database successfully'"
echo "   ✅ 'Event notification emails completed for event ${EVENT_ID}'"
echo "   ✅ No 'Cannot access value of a failed result' errors"
echo ""
echo "3. Check email inbox:"
echo "   - Email should be sent to: niroshhh@gmail.com"
echo "   - Subject: New Event: Test Event - Phase 6A.41 Email Fix in Columbus, OH"
echo ""
echo "Event ID for reference: $EVENT_ID"
