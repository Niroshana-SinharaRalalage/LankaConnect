#!/bin/bash
# Phase 6A.116 & 6A.117: Apply Both Migrations to Staging Database
# This script applies HTML rendering fix (6A.116) and template text/layout fixes (6A.117)

echo "========================================"
echo "PHASE 6A.116 & 6A.117: APPLY MIGRATIONS"
echo "========================================"
echo ""

echo "Migrations to Apply:"
echo "  1. Phase6A116_FixEmailTemplateHtmlRendering"
echo "     - Fix {{ResponseSummary}} → {{{ResponseSummary}}} (5 templates)"
echo ""
echo "  2. Phase6A117_FixEmailTemplateTextAndLayout"
echo "     - Remove 'feel free to reply' text (3 templates)"
echo "     - Remove empty PICKUP/DELIVERY card (2 templates)"
echo ""

echo "========================================"
echo "STEP 1: Connect to Azure Container App"
echo "========================================"
echo ""
echo "Run this command:"
echo "  az containerapp exec --name lankaconnect-api-staging --resource-group LankaConnect --command '/bin/bash'"
echo ""
read -p "Press Enter after connecting to container..."

echo ""
echo "========================================"
echo "STEP 2: Apply Migrations (Inside Container)"
echo "========================================"
echo ""
echo "Run these commands:"
echo ""
echo "  cd /app"
echo "  dotnet ef database update --project src/LankaConnect.Infrastructure --context AppDbContext"
echo ""
read -p "Press Enter after migrations applied..."

echo ""
echo "========================================"
echo "STEP 3: Verify Migrations Applied"
echo "========================================"
echo ""
echo "Run this command (inside container):"
echo ""
echo '  echo $DATABASE_URL | xargs -I {} psql {} -c "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE '\''%Phase6A11%'\'' ORDER BY \"MigrationId\" DESC;"'
echo ""
echo "Expected Output: You should see BOTH migrations:"
echo "  - 20260216033407_Phase6A116_FixEmailTemplateHtmlRendering"
echo "  - 20260216181052_Phase6A117_FixEmailTemplateTextAndLayout"
echo ""
read -p "Press Enter after verifying..."

echo ""
echo "========================================"
echo "STEP 4: Verify Template Changes"
echo "========================================"
echo ""
echo "Run this command (inside container):"
echo ""
cat <<'SQL'
echo $DATABASE_URL | xargs -I {} psql {} -c "
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN '✓ HTML Rendering Fixed'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN '✗ Still Escaped'
        ELSE 'N/A'
    END as issue_5_status,
    CASE
        WHEN html_template LIKE '%feel free to reply%' THEN '✗ Text Still Present'
        ELSE '✓ Text Removed'
    END as issue_10_status,
    CASE
        WHEN html_template LIKE '%PICKUP/DELIVERY CARD%' THEN '✗ Empty Card Present'
        ELSE '✓ Card Removed'
    END as issue_11_12_status,
    updated_at
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-event-registration-cancellation',
    'template-event-reminder'
)
ORDER BY name;
"
SQL

echo ""
echo "Expected Results:"
echo "  Issue #5 (HTML Rendering): All form-response templates should show '✓ HTML Rendering Fixed'"
echo "  Issue #10 (Feel Free Text): All 3 templates should show '✓ Text Removed'"
echo "  Issues #11, #12 (Empty Card): Both signup-list-commitment templates should show '✓ Card Removed'"
echo ""
read -p "Press Enter after verifying template changes..."

echo ""
echo "========================================"
echo "STEP 5: Exit Container"
echo "========================================"
echo ""
echo "Run: exit"
echo ""

echo "========================================"
echo "NEXT STEPS"
echo "========================================"
echo ""
echo "1. Test form response emails:"
echo "   - Submit/update a form response"
echo "   - Check email for:"
echo "     ✓ Line breaks rendering (not <br/> text)"
echo "     ✓ All placeholders replaced"
echo "     ✓ Edit button URL correct"
echo "     ✓ Signup buttons clickable"
echo ""
echo "2. Test signup list commitment emails:"
echo "   - Create signup list commitment"
echo "   - Check confirmation email for:"
echo "     ✓ No 'feel free to reply' text"
echo "     ✓ No extra whitespace in footer"
echo "   - Update commitment"
echo "   - Check update email for:"
echo "     ✓ No 'feel free to reply' text"
echo "     ✓ No extra whitespace in footer"
echo ""
echo "3. Test event reminder emails:"
echo "   - Trigger event reminder"
echo "   - Check email for:"
echo "     ✓ No 'feel free to reply' text"
echo ""
echo "4. Update tracking documents:"
echo "   - PROGRESS_TRACKER.md"
echo "   - STREAMLINED_ACTION_PLAN.md"
echo ""
echo "========================================"
echo "MIGRATIONS APPLIED SUCCESSFULLY"
echo "========================================"
