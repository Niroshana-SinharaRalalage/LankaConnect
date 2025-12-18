#!/bin/bash

# Test registration API for Phase 6A.34 domain event fix

# Login
echo "Logging in..."
RESPONSE=$(curl -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d "{\"email\":\"niroshhh@gmail.com\",\"password\":\"12!@qwASzx\",\"rememberMe\":true,\"ipAddress\":\"127.0.0.1\"}" \
  -s)

TOKEN=$(echo $RESPONSE | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "Login failed:"
  echo "$RESPONSE"
  exit 1
fi

echo "Login successful, token obtained"

# Create registration
echo "Creating registration for event c1f182a9-c957-4a78-a0b2-085917a88900..."
curl -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/c1f182a9-c957-4a78-a0b2-085917a88900/rsvp" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"quantity": 1}' \
  -v

echo ""
echo "Registration complete. Check logs for domain event dispatch."
