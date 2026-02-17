# Phase 6A.116 & 6A.117 Manual Migration Application Script
# Purpose: Manually apply email template migrations to production database
# Date: 2026-02-16
# CRITICAL: Only run this if migrations were NOT applied during deployment

Write-Host "=====================================================================" -ForegroundColor Red
Write-Host "⚠️  PRODUCTION DATABASE MIGRATION - MANUAL APPLICATION" -ForegroundColor Red
Write-Host "=====================================================================" -ForegroundColor Red
Write-Host ""
Write-Host "This script will apply Phase 6A.116 and 6A.117 migrations to production." -ForegroundColor Yellow
Write-Host ""
Write-Host "Migrations to be applied:" -ForegroundColor Yellow
Write-Host "  1. Phase6A116_FixEmailTemplateHtmlRendering" -ForegroundColor Yellow
Write-Host "  2. Phase6A117_FixEmailTemplateTextAndLayout" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  WARNING: This will modify production database!" -ForegroundColor Red
Write-Host ""

# Confirmation prompt
$confirmation = Read-Host "Are you sure you want to proceed? (yes/no)"
if ($confirmation -ne "yes") {
    Write-Host "❌ Aborted by user" -ForegroundColor Yellow
    exit 0
}

Write-Host ""

# Step 1: Get production database connection string
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

    Write-Host "✅ Connection string retrieved" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ ERROR: Exception retrieving connection string: $_" -ForegroundColor Red
    exit 1
}

# Step 2: Check current state
Write-Host "[2/4] Checking current migration state..." -ForegroundColor Yellow

$checkQuery = @"
SELECT COUNT(*) as count
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20260216033407_Phase6A116_FixEmailTemplateHtmlRendering',
    '20260216181052_Phase6A117_FixEmailTemplateTextAndLayout'
);
"@

$existingCount = psql "$connectionString" -t -c $checkQuery | Out-String | ForEach-Object { $_.Trim() }

if ($existingCount -eq "2") {
    Write-Host "⚠️  WARNING: Migrations already applied!" -ForegroundColor Yellow
    Write-Host "   Both Phase6A116 and Phase6A117 are already in migration history" -ForegroundColor Yellow
    Write-Host ""
    $overwrite = Read-Host "Do you want to re-apply anyway? (yes/no)"
    if ($overwrite -ne "yes") {
        Write-Host "❌ Aborted by user" -ForegroundColor Yellow
        exit 0
    }
}
elseif ($existingCount -eq "1") {
    Write-Host "⚠️  WARNING: One migration already applied" -ForegroundColor Yellow
    Write-Host "   Will apply missing migration" -ForegroundColor Yellow
}
else {
    Write-Host "✅ No migrations applied yet - will apply both" -ForegroundColor Green
}

Write-Host ""

# Step 3: Apply migrations using EF Core
Write-Host "[3/4] Applying migrations via EF Core..." -ForegroundColor Yellow

# Install or update dotnet-ef tool
Write-Host "Installing dotnet-ef tool..." -ForegroundColor Gray
dotnet tool install -g dotnet-ef --version 8.0.0 2>$null
if ($LASTEXITCODE -ne 0) {
    dotnet tool update -g dotnet-ef --version 8.0.0
}

# Navigate to API project directory
$apiProjectPath = Join-Path $PSScriptRoot "..\src\LankaConnect.API"
$infraProjectPath = Join-Path $PSScriptRoot "..\src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj"

Write-Host "Project paths:" -ForegroundColor Gray
Write-Host "  API: $apiProjectPath" -ForegroundColor Gray
Write-Host "  Infrastructure: $infraProjectPath" -ForegroundColor Gray
Write-Host ""

Push-Location $apiProjectPath

try {
    Write-Host "Running: dotnet ef database update --connection '***' --project '$infraProjectPath' --context AppDbContext --verbose" -ForegroundColor Gray
    Write-Host ""

    dotnet ef database update `
        --connection "$connectionString" `
        --project "$infraProjectPath" `
        --context AppDbContext `
        --verbose

    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERROR: Migration failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }

    Write-Host ""
    Write-Host "✅ Migrations applied successfully" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "❌ ERROR: Exception during migration: $_" -ForegroundColor Red
    Pop-Location
    exit 1
}
finally {
    Pop-Location
}

# Step 4: Verify migrations applied
Write-Host "[4/4] Verifying migrations..." -ForegroundColor Yellow

$verifyQuery = @"
SELECT
    "MigrationId",
    "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20260216033407_Phase6A116_FixEmailTemplateHtmlRendering',
    '20260216181052_Phase6A117_FixEmailTemplateTextAndLayout'
)
ORDER BY "MigrationId";
"@

$migrationsApplied = psql "$connectionString" -t -c $verifyQuery

if ([string]::IsNullOrWhiteSpace($migrationsApplied)) {
    Write-Host "❌ VERIFICATION FAILED: Migrations not found in history table!" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "✅ Migrations verified in history table:" -ForegroundColor Green
Write-Host $migrationsApplied
Write-Host ""

# Verify template changes
Write-Host "Verifying template changes..." -ForegroundColor Yellow

$templateCheckQuery = @"
SELECT
    name,
    CASE
        WHEN html_template LIKE '%{{{ResponseSummary}}}%' THEN 'HTML Fix: ✅'
        WHEN html_template LIKE '%{{ResponseSummary}}%' THEN 'HTML Fix: ❌'
        ELSE 'HTML Fix: N/A'
    END as html_status,
    CASE
        WHEN html_template LIKE '%feel free to reply%' THEN 'Text Fix: ❌'
        ELSE 'Text Fix: ✅'
    END as text_status
FROM communications.email_templates
WHERE name IN (
    'template-form-response-confirmation',
    'template-form-response-update',
    'template-signup-list-commitment-confirmation',
    'template-signup-list-commitment-update',
    'template-event-registration-cancellation',
    'template-event-reminder'
)
ORDER BY name;
"@

$templateStatus = psql "$connectionString" -c $templateCheckQuery

Write-Host $templateStatus
Write-Host ""

# Summary
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host "✅ MIGRATION APPLICATION COMPLETE" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Applied Migrations:" -ForegroundColor Green
Write-Host "  ✅ Phase6A116_FixEmailTemplateHtmlRendering" -ForegroundColor Green
Write-Host "  ✅ Phase6A117_FixEmailTemplateTextAndLayout" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run verification script: scripts/verify_production_migrations_phase6a116_117.ps1" -ForegroundColor Yellow
Write-Host "  2. Test emails in production environment" -ForegroundColor Yellow
Write-Host "  3. Monitor Azure logs for any issues" -ForegroundColor Yellow
Write-Host ""
