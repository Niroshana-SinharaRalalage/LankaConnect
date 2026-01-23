# PowerShell script to detect manual database changes
# Compares migration files with actual staging database content

$ErrorActionPreference = "Continue"

Write-Host "=== ANALYZING MANUAL DATABASE CHANGES ===" -ForegroundColor Cyan
Write-Host ""

# Database connection
$stagingHost = "lankaconnect-staging-db.postgres.database.azure.com"
$stagingDb = "LankaConnectDB"
$stagingUser = "adminuser"
$stagingPassword = "1qaz!QAZ"
$env:PGPASSWORD = $stagingPassword

function Query-Staging {
    param([string]$sql)

    $result = psql -h $stagingHost -U $stagingUser -d $stagingDb -t -c $sql 2>&1
    return $result
}

Write-Host "Step 1: Analyzing Email Templates..." -ForegroundColor Yellow
Write-Host ""

# Check 1: Find templates in database
$dbTemplates = Query-Staging "SELECT name FROM communications.email_templates ORDER BY name;"
$dbTemplateNames = $dbTemplates -split "`n" | Where-Object { $_ -match '\S' } | ForEach-Object { $_.Trim() }

Write-Host "Templates in DATABASE: $($dbTemplateNames.Count)" -ForegroundColor Green
$dbTemplateNames | ForEach-Object { Write-Host "  - $_" }

Write-Host ""

# Check 2: Find templates in migrations
Write-Host "Searching migrations for email template seeds..." -ForegroundColor Yellow
$migrationFiles = Get-ChildItem "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations\*.cs" | Where-Object { $_.Name -notlike "*Designer.cs" }

$migratedTemplates = @{}

foreach ($file in $migrationFiles) {
    $content = Get-Content $file.FullName -Raw

    # Look for INSERT INTO email_templates
    if ($content -match "email_templates") {
        # Extract template names from INSERT statements
        $matches = [regex]::Matches($content, "'([a-z-]+)'[,\s]*'(Transactional|Marketing|System)'")

        foreach ($match in $matches) {
            $templateName = $match.Groups[1].Value
            if ($templateName -and $templateName.Length -gt 3) {
                if (-not $migratedTemplates.ContainsKey($templateName)) {
                    $migratedTemplates[$templateName] = $file.Name
                }
            }
        }
    }
}

Write-Host ""
Write-Host "Templates in MIGRATIONS: $($migratedTemplates.Count)" -ForegroundColor Green
$migratedTemplates.Keys | Sort-Object | ForEach-Object {
    Write-Host "  - $_ (from $($migratedTemplates[$_]))"
}

Write-Host ""
Write-Host "Step 2: Comparing Database vs Migrations..." -ForegroundColor Yellow
Write-Host ""

# Find templates in DB but not in migrations (manually added)
$manuallyAdded = $dbTemplateNames | Where-Object { -not $migratedTemplates.ContainsKey($_) }
if ($manuallyAdded) {
    Write-Host "⚠️  MANUALLY ADDED templates (not in migrations):" -ForegroundColor Red
    $manuallyAdded | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
} else {
    Write-Host "✅ No manually added templates detected" -ForegroundColor Green
}

Write-Host ""

# Check 3: Find templates that were UPDATED after creation
Write-Host "Step 3: Checking for MODIFIED templates..." -ForegroundColor Yellow
Write-Host ""

$modifiedTemplates = Query-Staging @"
SELECT
    name,
    created_at,
    updated_at,
    updated_at - created_at as time_diff
FROM communications.email_templates
WHERE updated_at IS NOT NULL
  AND updated_at > created_at + interval '1 minute'
ORDER BY updated_at DESC;
"@

if ($modifiedTemplates -match '\S') {
    Write-Host "⚠️  MANUALLY MODIFIED templates (updated after creation):" -ForegroundColor Red
    Write-Host $modifiedTemplates
} else {
    Write-Host "✅ No modified templates detected" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 4: Analyzing Reference Data..." -ForegroundColor Yellow
Write-Host ""

# Check metro areas count
$metroCount = Query-Staging "SELECT COUNT(*) FROM events.metro_areas;"
Write-Host "Metro Areas: $metroCount (expected: 22+)" -ForegroundColor $(if ($metroCount -match "22") { "Green" } else { "Yellow" })

# Check reference values count
$refValuesCount = Query-Staging "SELECT COUNT(*) FROM reference_data.reference_values;"
Write-Host "Reference Values: $refValuesCount (expected: 50+)" -ForegroundColor $(if ($refValuesCount -match "\d{2,}") { "Green" } else { "Yellow" })

# Check email templates count
$templateCount = Query-Staging "SELECT COUNT(*) FROM communications.email_templates;"
Write-Host "Email Templates: $templateCount (expected: 15+)" -ForegroundColor $(if ($templateCount -match "\d{2}") { "Green" } else { "Yellow" })

# Check state tax rates
$taxRatesCount = Query-Staging "SELECT COUNT(*) FROM reference_data.state_tax_rates;"
Write-Host "State Tax Rates: $taxRatesCount (expected: 51)" -ForegroundColor $(if ($taxRatesCount -match "51") { "Green" } else { "Yellow" })

Write-Host ""
Write-Host "Step 5: Generating Export SQL for Manual Changes..." -ForegroundColor Yellow
Write-Host ""

# Export only the templates that were manually modified
$exportSql = @"
SELECT
    '-- Manual change detected for: ' || name || E'\n' ||
    'UPDATE communications.email_templates SET' || E'\n' ||
    '  subject_template = ' || quote_literal(subject_template) || ',' || E'\n' ||
    '  html_template = ' || quote_literal(html_template) || ',' || E'\n' ||
    '  text_template = ' || quote_literal(text_template) || ',' || E'\n' ||
    '  updated_at = NOW()' || E'\n' ||
    'WHERE name = ' || quote_literal(name) || ';' || E'\n\n'
FROM communications.email_templates
WHERE updated_at IS NOT NULL
  AND updated_at > created_at + interval '1 minute'
ORDER BY name;
"@

$exportResult = Query-Staging $exportSql

if ($exportResult -match '\S' -and $exportResult -notmatch "0 rows") {
    Write-Host "📝 SQL for manual changes saved to: manual_changes_export.sql" -ForegroundColor Cyan
    $exportResult | Out-File -FilePath "C:\Work\LankaConnect\scripts\manual_changes_export.sql" -Encoding UTF8

    Write-Host ""
    Write-Host "Preview:" -ForegroundColor Cyan
    Write-Host $exportResult.Substring(0, [Math]::Min(500, $exportResult.Length))
} else {
    Write-Host "✅ No manual SQL changes needed!" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== ANALYSIS COMPLETE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Yellow
Write-Host "  Templates in DB: $($dbTemplateNames.Count)"
Write-Host "  Templates in Migrations: $($migratedTemplates.Count)"
Write-Host "  Manually Added: $($manuallyAdded.Count)"
Write-Host "  Modified After Creation: $(($modifiedTemplates -split "`n").Count - 1)"
Write-Host ""

if ($manuallyAdded.Count -eq 0 -and -not ($modifiedTemplates -match '\S')) {
    Write-Host "✅ RESULT: Database matches migrations - NO manual changes detected!" -ForegroundColor Green
    Write-Host "   You can safely deploy with existing migrations." -ForegroundColor Green
} else {
    Write-Host "RESULT: Manual changes detected!" -ForegroundColor Yellow
    Write-Host "   Review manual_changes_export.sql and create a migration." -ForegroundColor Yellow
}
