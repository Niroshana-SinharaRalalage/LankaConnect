# Phase 6A.116: Verify if Phase6A112 migration was applied to staging database
# CRITICAL: This must be run FIRST before any P0 email fixes

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PHASE 6A.116: Database Migration Verification" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target: Azure Staging Database" -ForegroundColor Yellow
Write-Host "Purpose: Verify Phase6A112 migration status before email fixes" -ForegroundColor Yellow
Write-Host ""

# Read staging database connection string from environment or config
# Note: You'll need to set this in your environment or pass as parameter
$connectionString = $env:STAGING_DATABASE_URL
if (-not $connectionString) {
    Write-Host "ERROR: STAGING_DATABASE_URL environment variable not set" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please set the connection string:" -ForegroundColor Yellow
    Write-Host '  $env:STAGING_DATABASE_URL = "Host=your-host;Database=your-db;Username=your-user;Password=your-pass"' -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or pass it as a parameter:" -ForegroundColor Yellow
    Write-Host '  .\verify_phase6a112_migration.ps1 -ConnectionString "Host=..."' -ForegroundColor Gray
    exit 1
}

# Query 1: Check migration history
Write-Host "[1/3] Checking migration history..." -ForegroundColor Yellow
$migrationQuery = @"
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A112%'
ORDER BY "MigrationId" DESC;
"@

Write-Host "Query:" -ForegroundColor Gray
Write-Host $migrationQuery -ForegroundColor DarkGray
Write-Host ""

try {
    $migrationResult = psql $connectionString -c $migrationQuery -t -A 2>&1
    if ($LASTEXITCODE -eq 0) {
        if ($migrationResult) {
            Write-Host "✅ Phase6A112 migration FOUND in history:" -ForegroundColor Green
            Write-Host $migrationResult -ForegroundColor White
            $migrationApplied = $true
        } else {
            Write-Host "❌ Phase6A112 migration NOT FOUND in history" -ForegroundColor Red
            $migrationApplied = $false
        }
    } else {
        Write-Host "❌ Error querying migration history:" -ForegroundColor Red
        Write-Host $migrationResult -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Failed to connect to database:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
Write-Host ""

# Query 2: Check template content and rendering mode
Write-Host "[2/3] Checking email template content..." -ForegroundColor Yellow
$templateQuery = @"
SELECT
    name,
    subject_template,
    CASE
        WHEN html_template LIKE '%{{UserName}}%' THEN 'Has UserName placeholder'
        WHEN html_template LIKE '%{{{UserName}}}%' THEN 'Has UserName (triple braces)'
        ELSE 'No UserName placeholder'
    END as username_status,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'Renders HTML (triple braces) ✅'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'Escapes HTML (double braces) ❌'
        ELSE 'No ResponseSummary'
    END as rendering_mode,
    updated_at
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation'
)
ORDER BY name;
"@

Write-Host "Query:" -ForegroundColor Gray
Write-Host $templateQuery -ForegroundColor DarkGray
Write-Host ""

$templateResult = psql $connectionString -c $templateQuery 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "📧 Email Template Analysis:" -ForegroundColor Green
    Write-Host $templateResult -ForegroundColor White
} else {
    Write-Host "❌ Error querying templates:" -ForegroundColor Red
    Write-Host $templateResult -ForegroundColor Red
}
Write-Host ""

# Query 3: Check for placeholder count
Write-Host "[3/3] Checking placeholder patterns..." -ForegroundColor Yellow
$placeholderQuery = @"
SELECT
    name,
    (LENGTH(html_template) - LENGTH(REPLACE(html_template, '{{', ''))) / 2 as total_placeholders,
    CASE
        WHEN html_template LIKE '%{{UserName}}%' THEN 'YES'
        ELSE 'NO'
    END as has_username,
    CASE
        WHEN html_template LIKE '%{{EventTitle}}%' OR html_template LIKE '%{{{EventTitle}}}%' THEN 'YES'
        ELSE 'NO'
    END as has_event_title,
    CASE
        WHEN html_template LIKE '%{{FormTitle}}%' OR html_template LIKE '%{{{FormTitle}}}%' THEN 'YES'
        ELSE 'NO'
    END as has_form_title,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'TRIPLE (HTML)'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'DOUBLE (ESCAPED)'
        ELSE 'NONE'
    END as response_summary_type
FROM communications.email_templates
WHERE name LIKE 'template-form-response%'
ORDER BY name;
"@

Write-Host "Query:" -ForegroundColor Gray
Write-Host $placeholderQuery -ForegroundColor DarkGray
Write-Host ""

$placeholderResult = psql $connectionString -c $placeholderQuery 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "🔍 Placeholder Analysis:" -ForegroundColor Green
    Write-Host $placeholderResult -ForegroundColor White
} else {
    Write-Host "❌ Error analyzing placeholders:" -ForegroundColor Red
    Write-Host $placeholderResult -ForegroundColor Red
}
Write-Host ""

# Summary and Next Steps
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SUMMARY & NEXT STEPS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if ($migrationApplied) {
    Write-Host "✅ Migration Status: Phase6A112 IS APPLIED" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Check template content above" -ForegroundColor White
    Write-Host "  2. If ResponseSummary uses DOUBLE braces:" -ForegroundColor White
    Write-Host "     → Proceed with Issue #5 (Create Phase6A116 migration)" -ForegroundColor Cyan
    Write-Host "  3. If placeholders like {{UserName}} not being replaced:" -ForegroundColor White
    Write-Host "     → Proceed with Issue #4 (Check parameter mapping)" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "❌ Migration Status: Phase6A112 NOT APPLIED" -ForegroundColor Red
    Write-Host ""
    Write-Host "CRITICAL ACTION REQUIRED:" -ForegroundColor Red
    Write-Host "  1. Connect to Azure Container Apps staging" -ForegroundColor White
    Write-Host "     az containerapp exec --name lankaconnect-api-staging --resource-group <rg> --command /bin/bash" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  2. Inside container, apply migration:" -ForegroundColor White
    Write-Host "     cd /app" -ForegroundColor Gray
    Write-Host "     dotnet ef database update --project src/LankaConnect.Infrastructure" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  3. Verify migration applied:" -ForegroundColor White
    Write-Host "     psql \$DATABASE_URL -c \"\"SELECT * FROM __EFMigrationsHistory WHERE MigrationId LIKE '%Phase6A112%';\"\"" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  4. Re-run this verification script" -ForegroundColor White
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "VERIFICATION COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
