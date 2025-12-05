#!/bin/bash
# Quick test for event creation

TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoMkBnbWFpbC5jb20iLCJ1bmlxdWVfbmFtZSI6Ik5pcm9zaGFuYSBTaW5oYXJhIFJhbGFsYWdlIiwicm9sZSI6IkV2ZW50T3JnYW5pemVyIiwiZmlyc3ROYW1lIjoiTmlyb3NoYW5hIiwibGFzdE5hbWUiOiJTaW5oYXJhIFJhbGFsYWdlIiwiaXNBY3RpdmUiOiJ0cnVlIiwianRpIjoiZmFmOTk0Y2YtZjQ1Mi00YjdmLWExZGYtMGRmOWFjZTE0OTU0IiwiaWF0IjoxNzY0OTAyMDAyLCJuYmYiOjE3NjQ5MDIwMDIsImV4cCI6MTc2NDkwMzgwMiwiaXNzIjoiaHR0cHM6Ly9sYW5rYWNvbm5lY3QtYXBpLXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQiLCJhdWQiOiJodHRwczovL2xhbmthY29ubmVjdC1zdGFnaW5nLmF6dXJld2Vic2l0ZXMubmV0In0.cMZsBhVlGiQWxXZgFgr3s-vSUegOBGYjjF1vXAfhVrs"

echo "Testing Free Event Creation..."

RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Phase 6 Test - Free Event","description":"Free event for E2E testing","startDate":"2025-12-15T18:00:00Z","endDate":"2025-12-15T21:00:00Z","capacity":100,"isFree":true,"location":{"address":{"street":"123 Test St","city":"Colombo","state":"Western","zipCode":"00100","country":"Sri Lanka"}},"category":"Community"}')

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | head -n-1)

echo "HTTP Status: $HTTP_CODE"
echo ""
echo "Response:"
echo "$BODY" | head -100
