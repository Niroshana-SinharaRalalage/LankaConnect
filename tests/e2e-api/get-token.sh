#!/bin/bash
# Get fresh authentication token

STAGING_URL="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Login to get token
RESPONSE=$(curl -s -X POST "$STAGING_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"niroshhh2@gmail.com","password":"Password123!"}')

# Extract token using grep
TOKEN=$(echo "$RESPONSE" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

if [ -n "$TOKEN" ]; then
    echo "$TOKEN"
else
    echo "Error: Could not get token" >&2
    echo "$RESPONSE" >&2
    exit 1
fi
