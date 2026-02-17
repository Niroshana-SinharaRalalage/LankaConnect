# Phase 6A.116: Apply HTML Rendering Migration to Staging Database
# This script applies the Phase6A116_FixEmailTemplateHtmlRendering migration

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PHASE 6A.116: Apply HTML Rendering Migration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Connect to Azure Container App
Write-Host "[1/3] Connecting to Azure Container App..." -ForegroundColor Yellow
Write-Host "Command: az containerapp exec --name lankaconnect-api-staging --resource-group LankaConnect --command '/bin/bash'" -ForegroundColor Gray
Write-Host ""
Write-Host "MANUAL ACTION REQUIRED:" -ForegroundColor Red
Write-Host "Run the following command to connect to the staging container:" -ForegroundColor White
Write-Host ""
Write-Host "  az containerapp exec --name lankaconnect-api-staging --resource-group LankaConnect --command '/bin/bash'" -ForegroundColor Green
Write-Host ""
Write-Host "Once connected, run these commands inside the container:" -ForegroundColor Yellow
Write-Host ""

# Step 2: Apply migration
Write-Host "[2/3] Apply Migration (inside container):" -ForegroundColor Yellow
Write-Host "  cd /app" -ForegroundColor Green
Write-Host "  dotnet ef database update --project src/LankaConnect.Infrastructure" -ForegroundColor Green
Write-Host ""

# Step 3: Verify migration applied
Write-Host "[3/3] Verify Migration (inside container):" -ForegroundColor Yellow
Write-Host '  echo $DATABASE_URL | xargs -I {} psql {} -c "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" LIKE ''%Phase6A116%'' ORDER BY \"MigrationId\" DESC;"' -ForegroundColor Green
Write-Host ""

# Step 4: Verify template content
Write-Host "[4/4] Verify Template Content (inside container):" -ForegroundColor Yellow
Write-Host @"
echo `$DATABASE_URL | xargs -I {} psql {} -c "
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'Fixed - Renders HTML (triple braces)'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'Not Fixed - Escapes HTML (double braces)'
        ELSE 'Unknown'
    END as rendering_mode
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
);
"
"@ -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "EXPECTED OUTPUT:" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Migration: 20260216033407_Phase6A116_FixEmailTemplateHtmlRendering" -ForegroundColor White
Write-Host "All 5 templates should show: 'Fixed - Renders HTML (triple braces)'" -ForegroundColor White
Write-Host ""
Write-Host "If you see 'Not Fixed', the migration did not run correctly." -ForegroundColor Yellow
