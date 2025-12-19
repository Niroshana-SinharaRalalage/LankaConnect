#!/bin/bash

# Script to verify when user registration was created
# This helps determine if registration happened before or after Phase 6A.34 deployment

EVENT_ID="c1f182a9-c957-4a78-a0b2-085917a88900"
DEPLOYMENT_TIME="2025-12-18T04:38:00Z"  # Phase 6A.34 deployment time

echo "=========================================="
echo "Registration Timestamp Verification"
echo "=========================================="
echo ""
echo "Event ID: $EVENT_ID"
echo "Phase 6A.34 Deployed: $DEPLOYMENT_TIME"
echo ""

# Query the database for registration details
# Replace with your actual database connection details
echo "Querying database for registration timestamp..."
echo ""

# Example Azure SQL query (adjust for your connection method)
cat << 'SQL'
SELECT TOP 10
    er.Id,
    er.UserId,
    er.EventId,
    er.CreatedAt,
    er.UpdatedAt,
    er.Status,
    CASE
        WHEN er.CreatedAt < '2025-12-18T04:38:00Z' THEN 'BEFORE Phase 6A.34'
        ELSE 'AFTER Phase 6A.34'
    END AS DeploymentRelation
FROM EventRegistrations er
WHERE er.EventId = 'c1f182a9-c957-4a78-a0b2-085917a88900'
ORDER BY er.CreatedAt DESC;
SQL

echo ""
echo "=========================================="
echo "Instructions:"
echo "1. Run this query against your Azure SQL database"
echo "2. Check CreatedAt timestamp"
echo "3. Compare with deployment time: 2025-12-18T04:38:00Z"
echo "4. If BEFORE: Registration happened before fix (explains missing logs)"
echo "5. If AFTER: We have a logging problem to investigate"
echo "=========================================="
