#!/bin/bash

# Login and get token
TOKEN=$(curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d @login-payload-temp.json | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

echo "Token: $TOKEN"
echo ""

# Query signup lists
curl -s "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/89f8ef9f-af11-4b1a-8dec-b440faef9ad0/signup-lists" \
  -H "Authorization: Bearer $TOKEN"
