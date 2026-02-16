#!/bin/bash
# Check Production Database Migration Status
# Verifies whether Phase6A116 and Phase6A117 migrations are applied

echo "========================================"
echo "PRODUCTION MIGRATION STATUS CHECK"
echo "========================================"
echo ""

echo "Step 1: Connect to Production API Container"
echo "Run this command:"
echo "  az containerapp exec --name lankaconnect-api-prod --resource-group LankaConnect --command '/bin/bash'"
echo ""

echo "Step 2: Inside container, check migration history:"
echo ""
echo 'echo $DATABASE_URL | xargs -I {} psql {} -c "'
echo "SELECT \"MigrationId\", TO_CHAR(\"ProductVersion\", '999.999') as Version"
echo "FROM \"__EFMigrationsHistory\""
echo "WHERE \"MigrationId\" LIKE '%Phase6A11%'"
echo "ORDER BY \"MigrationId\" DESC;"
echo '"'
echo ""

echo "Expected if NOT applied (current state):"
echo "  (No rows - migrations not in production yet)"
echo ""

echo "Expected if APPLIED (after PR #82 merge):"
echo "                    MigrationId                     | Version "
echo "----------------------------------------------------+---------"
echo " 20260216181052_Phase6A117_FixEmailTemplateTextAndLayout | 8.0.19"
echo " 20260216033407_Phase6A116_FixEmailTemplateHtmlRendering | 8.0.19"
echo ""

echo "========================================"
echo "DEPLOYMENT WORKFLOW"
echo "========================================"
echo ""
echo "To deploy to production:"
echo "1. Merge PR #82 to main branch"
echo "2. GitHub Actions will automatically run deploy-production.yml"
echo "3. Migrations will be applied during deployment"
echo "4. Verify using this script"
echo ""
