#!/bin/bash

TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiNTIxNDU5NDUtNDIyOS00YzJlLWIzOGUtYzNmOTZjOTQxMTJlIiwiaWF0IjoxNzY1Njg5NzkyLCJuYmYiOjE3NjU2ODk3OTIsImV4cCI6MTc2NTY5MTU5MiwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.tm59hWCJRpLO_g9uG8AVhvvXQ600_tCEXVftIE06k4Y"
API_BASE="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"
EVENT_ID="d9fa9a8e-2b54-47b2-bb24-09ee6f8dd656"

echo "=== STEP 1: TEST UPDATE WITH MODIFIED GROUP PRICING TIERS ==="

# Update payload with modified tiers: remove last tier and change prices
cat > update_payload.json <<'EOF'
{
  "eventId": "d9fa9a8e-2b54-47b2-bb24-09ee6f8dd656",
  "title": "Test Group Pricing Update",
  "description": "Testing tier update via API",
  "startDate": "2025-12-25T10:00:00Z",
  "endDate": "2025-12-25T18:00:00Z",
  "capacity": 100,
  "category": 3,
  "groupPricingTiers": [
    {
      "minAttendees": 1,
      "maxAttendees": 1,
      "pricePerPerson": 6.00,
      "currency": 1
    },
    {
      "minAttendees": 2,
      "maxAttendees": null,
      "pricePerPerson": 12.00,
      "currency": 1
    }
  ]
}
EOF

echo ""
echo "Update Payload:"
cat update_payload.json
echo ""

echo "=== SENDING UPDATE REQUEST ==="
curl -v -X PUT "$API_BASE/events/$EVENT_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @update_payload.json 2>&1

echo ""
echo ""
echo "=== STEP 2: VERIFY UPDATE (GET EVENT) ==="
sleep 2
curl -s -X GET "$API_BASE/events/$EVENT_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" | python -m json.tool | grep -A 20 "groupPricingTiers"
