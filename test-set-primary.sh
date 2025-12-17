#!/bin/bash

# Login and get token
echo "Logging in..."
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d @test-login.json \
  -s > auth_response.json

# Extract token
TOKEN=$(grep -o '"token":"[^"]*' auth_response.json | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo "Failed to get auth token"
    cat auth_response.json
    exit 1
fi

echo "Got token (first 50 chars): ${TOKEN:0:50}..."

# Test set-primary endpoint
echo ""
echo "Testing set-primary endpoint..."
curl -X POST "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/0458806b-8672-4ad5-a7cb-f5346f1b282a/images/3f55cb35-2bb9-4748-8e0c-f843fc5d5723/set-primary" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -i -s | head -20

echo ""
echo "Done."