#!/bin/bash

# Badge Migration Verification Script
# Purpose: Verify that migration 20251216150703_UpdateBadgeLocationConfigsWithDefaults is deployed
# Usage: ./scripts/verify-badge-migration.sh

set -e

API_BASE="https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
MIGRATION_NAME="20251216150703_UpdateBadgeLocationConfigsWithDefaults"

echo "================================================================="
echo "Badge Migration Deployment Verification"
echo "================================================================="
echo ""

# Check if token file exists
if [ ! -f "token.txt" ]; then
    echo "❌ ERROR: token.txt not found"
    echo "Please create a token.txt file with a valid Bearer token"
    exit 1
fi

TOKEN=$(cat token.txt)

echo "1️⃣  Testing Badge Management API Endpoint..."
echo "-----------------------------------------------------------------"

HTTP_CODE=$(curl -s -o /tmp/badge-response.json -w "%{http_code}" \
    -X GET "$API_BASE/api/badges" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Accept: application/json")

echo "HTTP Status Code: $HTTP_CODE"

if [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ SUCCESS: Badge API returned 200 OK"
    echo ""
    echo "Response preview:"
    cat /tmp/badge-response.json | jq '.' 2>/dev/null || cat /tmp/badge-response.json
    echo ""
    echo "✅ MIGRATION DEPLOYED SUCCESSFULLY"
    MIGRATION_STATUS="DEPLOYED ✅"
elif [ "$HTTP_CODE" -eq 500 ]; then
    echo "❌ FAILURE: Badge API returned 500 Internal Server Error"
    echo ""
    echo "Error response:"
    cat /tmp/badge-response.json
    echo ""
    echo "❌ MIGRATION NOT YET DEPLOYED"
    MIGRATION_STATUS="NOT DEPLOYED ❌"
else
    echo "⚠️  UNEXPECTED: HTTP $HTTP_CODE"
    echo ""
    echo "Response:"
    cat /tmp/badge-response.json
    echo ""
    MIGRATION_STATUS="UNKNOWN ⚠️"
fi

echo ""
echo "2️⃣  Checking GitHub Actions Deployment Status..."
echo "-----------------------------------------------------------------"

# Get latest workflow run for develop branch
echo "Latest GitHub Actions workflows:"
echo "https://github.com/[org]/LankaConnect/actions?query=branch:develop"
echo ""
echo "Look for workflow triggered by commit: a359fea"
echo "Check 'Run EF Migrations' step for: '✅ Migrations completed successfully'"

echo ""
echo "================================================================="
echo "VERIFICATION SUMMARY"
echo "================================================================="
echo "Migration: $MIGRATION_NAME"
echo "Status:    $MIGRATION_STATUS"
echo ""

if [ "$HTTP_CODE" -eq 200 ]; then
    echo "✅ ALL SYSTEMS OPERATIONAL"
    echo ""
    echo "Next Steps:"
    echo "1. Test Badge Management page in UI: http://localhost:3000/dashboard"
    echo "2. Verify existing badges display correctly"
    echo "3. Try creating a new badge to test defaults"
    exit 0
elif [ "$HTTP_CODE" -eq 500 ]; then
    echo "❌ MIGRATION DEPLOYMENT REQUIRED"
    echo ""
    echo "Resolution Steps:"
    echo "1. Check GitHub Actions: https://github.com/[org]/LankaConnect/actions"
    echo "2. If workflow not triggered, run:"
    echo "   git commit --allow-empty -m 'chore: trigger staging deployment'"
    echo "   git push origin develop"
    echo "3. Wait 2-3 minutes for deployment to complete"
    echo "4. Re-run this script to verify"
    exit 1
else
    echo "⚠️  UNEXPECTED STATUS - MANUAL INVESTIGATION REQUIRED"
    exit 2
fi
