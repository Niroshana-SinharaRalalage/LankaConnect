#!/bin/bash
# Get fresh token for testing

curl -X POST \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d @- <<'EOF'
{
  "email": "niroshhh@gmail.com",
  "password": "12!@qwASzx",
  "rememberMe": true,
  "ipAddress": "string"
}
EOF
