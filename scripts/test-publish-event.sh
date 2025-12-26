#!/bin/bash
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoQGdtYWlsLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmlyb3NoYW5hIFNpbmhhcmEgUmFsYWxhZ2UiLCJyb2xlIjoiRXZlbnRPcmdhbml6ZXIiLCJmaXJzdE5hbWUiOiJOaXJvc2hhbmEiLCJsYXN0TmFtZSI6IlNpbmhhcmEgUmFsYWxhZ2UiLCJpc0FjdGl2ZSI6InRydWUiLCJqdGkiOiI2ZDVhMGYwNS1iYWE1LTQwNTctOTQzMi1hNThmNjMxZjRiNDYiLCJpYXQiOjE3NjY2OTg3OTEsIm5iZiI6MTc2NjY5ODc5MSwiZXhwIjoxNzY2NzAwNTkxLCJpc3MiOiJodHRwczovL2xhbmthY29ubmVjdC1hcGktc3RhZ2luZy5henVyZXdlYnNpdGVzLm5ldCIsImF1ZCI6Imh0dHBzOi8vbGFua2Fjb25uZWN0LXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQifQ.u5m5UQS_acWEE3moPT2UEi_rTJytZBITgKv8IL_3D-o"

echo "=== Creating draft event ==="
CREATE_RESPONSE=$(curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d @/tmp/create-test-event.json \
  -s)

echo "${CREATE_RESPONSE}"
EVENT_ID=$(echo "${CREATE_RESPONSE}" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo ""
echo "Event ID: ${EVENT_ID}"

if [ -z "${EVENT_ID}" ]; then
  echo "Failed to create event"
  exit 1
fi

echo ""
echo "=== Publishing event ==="
PUBLISH_RESPONSE=$(curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/${EVENT_ID}/publish" \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -s)

echo "${PUBLISH_RESPONSE}"
echo ""
echo "Event published! Event ID: ${EVENT_ID}"
