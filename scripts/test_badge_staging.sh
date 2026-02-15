#!/bin/bash

# Login
echo "Logging in..."
TOKEN=$(curl -s -X POST \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{"email":"niroshhh@gmail.com","password":"12!@qwASzx","rememberMe":true,"ipAddress":"192.168.1.1"}' \
  | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo "Token length: ${#TOKEN}"

if [ -z "$TOKEN" ]; then
  echo "ERROR: Failed to get token"
  exit 1
fi

# Get events with authentication
echo ""
echo "Fetching events with authentication..."
curl -s -X GET \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events' \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  > events_auth_response.json

echo "Response saved to events_auth_response.json"

# Check for userRegistrationStatus
COUNT=$(grep -c '"userRegistrationStatus"' events_auth_response.json 2>/dev/null || echo "0")
echo ""
echo "Events with userRegistrationStatus: $COUNT"

if [ "$COUNT" -gt "0" ]; then
  echo ""
  echo "=== Sample Registered Events ==="
  grep -B2 '"userRegistrationStatus"' events_auth_response.json | grep '"title"' | head -5
fi
