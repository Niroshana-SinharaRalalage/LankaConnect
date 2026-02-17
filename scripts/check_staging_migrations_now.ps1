# CRITICAL: Check if Phase6A116 & 6A117 migrations are in staging database
# User reports issues STILL exist in staging despite our earlier verification

Write-Host "========================================" -ForegroundColor Red
Write-Host "CRITICAL STAGING DATABASE CHECK" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

Write-Host "Issue: Email formatting issues STILL present in staging after migrations supposedly applied at 18:20:10 UTC" -ForegroundColor Yellow
Write-Host ""

Write-Host "Step 1: Connect to Staging Database" -ForegroundColor Cyan
Write-Host "Run:" -ForegroundColor White
Write-Host "  az containerapp exec --name lankaconnect-api-staging --resource-group LankaConnect --command '/bin/bash'" -ForegroundColor Gray
Write-Host ""

Write-Host "Step 2: Check Migration History" -ForegroundColor Cyan
Write-Host "Run inside container:" -ForegroundColor White
Write-Host @"
echo `$DATABASE_URL | xargs -I {} psql {} -c "
SELECT
    \"MigrationId\",
    \"ProductVersion\",
    applied_at
FROM \"__EFMigrationsHistory\"
WHERE \"MigrationId\" LIKE '%Phase6A11%'
ORDER BY \"MigrationId\" DESC;
"
"@ -ForegroundColor Gray
Write-Host ""

Write-Host "Step 3: Check ACTUAL Email Template Content" -ForegroundColor Cyan
Write-Host "Run inside container:" -ForegroundColor White
Write-Host @"
echo `$DATABASE_URL | xargs -I {} psql {} -c "
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN '✓ Triple Braces (CORRECT)'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN '✗ Double Braces (BROKEN)'
        ELSE 'N/A'
    END as issue5_status,
    CASE
        WHEN html_template LIKE '%feel free to reply%' THEN '✗ Text Still Present (BROKEN)'
        ELSE '✓ Text Removed (CORRECT)'
    END as issue10_status,
    TO_CHAR(updated_at, 'YYYY-MM-DD HH24:MI:SS') as last_updated
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
)
ORDER BY name;
"
"@ -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Red
Write-Host "CRITICAL SCENARIOS" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "Scenario A: Migrations NOT in history table" -ForegroundColor Yellow
Write-Host "  -> Migrations were NEVER applied to staging" -ForegroundColor White
Write-Host "  -> Need to apply manually" -ForegroundColor White
Write-Host ""
Write-Host "Scenario B: Migrations IN history but templates still broken" -ForegroundColor Yellow
Write-Host "  -> Migration SQL didn't work as expected" -ForegroundColor White
Write-Host "  -> Need to fix SQL and re-run" -ForegroundColor White
Write-Host ""
Write-Host "Scenario C: Templates look correct but emails still broken" -ForegroundColor Yellow
Write-Host "  -> Handler code sending wrong parameters" -ForegroundColor White
Write-Host "  -> Need to debug email handler" -ForegroundColor White
Write-Host ""
