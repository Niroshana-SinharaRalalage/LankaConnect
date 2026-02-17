# Phase 6A.116 & 6A.117 Production Migration Verification Script
# Purpose: Verify if email template migrations were applied to production database
# Date: 2026-02-16

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "Phase 6A.116 & 6A.117 Migration Verification - PRODUCTION DATABASE" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

# Get production database connection string from Azure Key Vault
Write-Host "[1/4] Retrieving production database connection string..." -ForegroundColor Yellow

try {
    $connectionString = az keyvault secret show `
        --vault-name "lankaconnect-prod-kv" `
        --name "database-connection-string" `
        --query value -o tsv

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($connectionString)) {
        Write-Host "❌ ERROR: Failed to retrieve database connection string from Key Vault" -ForegroundColor Red
        Write-Host "   Make sure you're logged in: az login" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "✅ Connection string retrieved (length: $($connectionString.Length) chars)" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ ERROR: Exception retrieving connection string: $_" -ForegroundColor Red
    exit 1
}

# Check migration history table
Write-Host "[2/4] Checking EF Core migration history..." -ForegroundColor Yellow

$migrationQuery = @"
SELECT
    "MigrationId",
    "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Phase6A116%' OR "MigrationId" LIKE '%Phase6A117%'
ORDER BY "MigrationId";
"@

Write-Host "Executing query:" -ForegroundColor Gray
Write-Host $migrationQuery -ForegroundColor Gray
Write-Host ""

$migrationResult = psql "$connectionString" -t -c $migrationQuery

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ ERROR: Failed to query migration history" -ForegroundColor Red
    exit 1
}

if ([string]::IsNullOrWhiteSpace($migrationResult)) {
    Write-Host "❌ MIGRATION NOT APPLIED: Phase6A116 and Phase6A117 migrations are MISSING from production!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Expected migrations:" -ForegroundColor Yellow
    Write-Host "  - 20260216033407_Phase6A116_FixEmailTemplateHtmlRendering" -ForegroundColor Yellow
    Write-Host "  - 20260216181052_Phase6A117_FixEmailTemplateTextAndLayout" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Action Required:" -ForegroundColor Yellow
    Write-Host "  Run manual migration application script: apply_phase6a116_117_migrations.ps1" -ForegroundColor Yellow
    Write-Host ""
}
else {
    Write-Host "✅ MIGRATIONS FOUND in production database:" -ForegroundColor Green
    Write-Host $migrationResult
    Write-Host ""
}

# Check template HTML rendering fix (Phase6A116)
Write-Host "[3/4] Verifying Phase6A116 HTML rendering fix..." -ForegroundColor Yellow

$htmlCheckQuery = @"
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'FIXED ✅'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'BROKEN ❌'
        ELSE 'N/A'
    END as html_status
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-form-response-cancellation',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update'
)
ORDER BY name;
"@

Write-Host "Templates using ResponseSummary:" -ForegroundColor Gray
$htmlResult = psql "$connectionString" -t -c $htmlCheckQuery

if ($htmlResult -match "BROKEN") {
    Write-Host "❌ HTML RENDERING FIX NOT APPLIED" -ForegroundColor Red
    Write-Host $htmlResult
    Write-Host ""
    Write-Host "Issue: Templates still use {{ResponseSummary}} instead of {{{ResponseSummary}}}" -ForegroundColor Red
    Write-Host "Impact: Email line breaks show as literal <br/> tags instead of actual line breaks" -ForegroundColor Red
    Write-Host ""
}
elseif ($htmlResult -match "FIXED") {
    Write-Host "✅ HTML RENDERING FIX APPLIED" -ForegroundColor Green
    Write-Host $htmlResult
    Write-Host ""
}
else {
    Write-Host "⚠️  WARNING: Could not verify HTML fix status" -ForegroundColor Yellow
    Write-Host $htmlResult
    Write-Host ""
}

# Check "feel free to reply" text removal (Phase6A117)
Write-Host "[4/4] Verifying Phase6A117 'feel free to reply' removal..." -ForegroundColor Yellow

$textCheckQuery = @"
SELECT
    name,
    CASE
        WHEN html_template LIKE '%feel free to reply%' THEN 'BROKEN ❌'
        ELSE 'FIXED ✅'
    END as text_status
FROM communications.email_templates
WHERE name IN (
    'template-event-registration-cancellation',
    'template-event-reminder',
    'template-signup-list-commitment-update'
)
ORDER BY name;
"@

Write-Host "Templates with 'feel free to reply' text:" -ForegroundColor Gray
$textResult = psql "$connectionString" -t -c $textCheckQuery

if ($textResult -match "BROKEN") {
    Write-Host "❌ TEXT REMOVAL FIX NOT APPLIED" -ForegroundColor Red
    Write-Host $textResult
    Write-Host ""
    Write-Host "Issue: Templates still contain 'feel free to reply to this email' text" -ForegroundColor Red
    Write-Host "Impact: Users encouraged to reply to automated emails (poor UX)" -ForegroundColor Red
    Write-Host ""
}
elseif ($textResult -match "FIXED") {
    Write-Host "✅ TEXT REMOVAL FIX APPLIED" -ForegroundColor Green
    Write-Host $textResult
    Write-Host ""
}
else {
    Write-Host "⚠️  WARNING: Could not verify text removal status" -ForegroundColor Yellow
    Write-Host $textResult
    Write-Host ""
}

# Summary
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "VERIFICATION SUMMARY" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

$allPassed = $true

if ([string]::IsNullOrWhiteSpace($migrationResult)) {
    Write-Host "❌ Migration History: NOT APPLIED" -ForegroundColor Red
    $allPassed = $false
}
else {
    Write-Host "✅ Migration History: APPLIED" -ForegroundColor Green
}

if ($htmlResult -match "BROKEN") {
    Write-Host "❌ HTML Rendering Fix: NOT APPLIED" -ForegroundColor Red
    $allPassed = $false
}
elseif ($htmlResult -match "FIXED") {
    Write-Host "✅ HTML Rendering Fix: APPLIED" -ForegroundColor Green
}

if ($textResult -match "BROKEN") {
    Write-Host "❌ Text Removal Fix: NOT APPLIED" -ForegroundColor Red
    $allPassed = $false
}
elseif ($textResult -match "FIXED") {
    Write-Host "✅ Text Removal Fix: APPLIED" -ForegroundColor Green
}

Write-Host ""

if ($allPassed) {
    Write-Host "🎉 SUCCESS: All Phase 6A.116 & 6A.117 fixes are applied to production!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "⚠️  FAILURE: Some fixes are NOT applied to production database" -ForegroundColor Red
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Run: scripts/apply_phase6a116_117_migrations.ps1" -ForegroundColor Yellow
    Write-Host "  2. Re-run this verification script" -ForegroundColor Yellow
    Write-Host "  3. Test emails in production environment" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
