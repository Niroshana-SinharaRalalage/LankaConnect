#!/bin/bash

# Get fresh token
curl -s -X POST 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d @test-login.json > login-result.json

TOKEN=$(python -c "import json; print(json.load(open('login-result.json'))['accessToken'])")

echo "Testing API endpoint..."
curl -s "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events/89f8ef9f-af11-4b1a-8dec-b440faef9ad0/signups" \
  -H "Authorization: Bearer $TOKEN" > api-result.json

echo ""
echo "=== API Response Analysis ==="
python << 'EOF'
import json
data = json.load(open('api-result.json'))
for i, item in enumerate(data, 1):
    print(f"\n{i}. {item['category']}")
    print(f"   hasMandatoryItems: {item.get('hasMandatoryItems', 'MISSING')}")
    print(f"   hasPreferredItems: {item.get('hasPreferredItems', 'MISSING')}")
    print(f"   hasSuggestedItems: {item.get('hasSuggestedItems', 'MISSING')}")
    print(f"   hasOpenItems: {item.get('hasOpenItems', 'MISSING ❌')}")
    print(f"   items count: {len(item['items'])}")
EOF
