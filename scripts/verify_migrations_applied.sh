#!/bin/bash
# Verify Phase 6A.116 & 6A.117 Migrations Applied to Staging

echo "========================================"
echo "VERIFY MIGRATIONS APPLIED"
echo "========================================"
echo ""

# Connect to Azure and check database
echo "Step 1: Connect to Azure Container App"
echo "  az containerapp exec --name lankaconnect-api-staging --resource-group LankaConnect --command '/bin/bash'"
echo ""
echo "Step 2: Inside container, verify migrations:"
echo ""
echo '  echo $DATABASE_URL | xargs -I {} psql {} -c "SELECT \"MigrationId\", TO_CHAR(\"ProductVersion\", '\''999.999'\'') as Version FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '\''%Phase6A11%'\'' ORDER BY \"MigrationId\" DESC;"'
echo ""
echo "Expected Output:"
echo "                    MigrationId                     | Version "
echo "----------------------------------------------------+---------"
echo " 20260216181052_Phase6A117_FixEmailTemplateTextAndLayout | 8.0.0"
echo " 20260216033407_Phase6A116_FixEmailTemplateHtmlRendering | 8.0.0"
echo ""
