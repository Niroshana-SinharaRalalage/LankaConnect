# Phase 6A.112/113: Export 8 email templates for "feel free" text removal
# User analysis found 8 templates containing "feel free to reply" or similar text

$stagingConnectionString = "Host=lankaconnect-staging-db.postgres.database.azure.com;Database=LankaConnectDB;Username=adminuser;Password=1qaz!QAZ;SslMode=Require"

Write-Host "Phase 6A.112/113: Finding templates with 'feel free' text" -ForegroundColor Cyan
Write-Host ""

# SQL to find all templates containing "feel free" (case-insensitive)
$findSql = @"
SELECT
    name,
    subject,
    CASE
        WHEN html_body ILIKE '%feel free%' THEN 'html_body'
        WHEN text_body ILIKE '%feel free%' THEN 'text_body'
        ELSE 'none'
    END as found_in,
    description
FROM communications.email_templates
WHERE html_body ILIKE '%feel free%' OR text_body ILIKE '%feel free%'
ORDER BY name;
"@

Write-Host "Connecting to staging database..." -ForegroundColor Cyan

# Execute query using psql
$env:PGPASSWORD = "1qaz!QAZ"
$findOutput = & psql -h lankaconnect-staging-db.postgres.database.azure.com `
                    -U adminuser `
                    -d LankaConnectDB `
                    -c $findSql

Write-Host $findOutput
Write-Host ""

# Export full templates
$exportSql = @"
SELECT
    name,
    subject,
    html_body,
    text_body,
    description,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE html_body ILIKE '%feel free%' OR text_body ILIKE '%feel free%'
ORDER BY name;
"@

$output = & psql -h lankaconnect-staging-db.postgres.database.azure.com `
                -U adminuser `
                -d LankaConnectDB `
                -c $exportSql `
                --csv

# Save to file
$outputFile = "c:\Work\LankaConnect\scripts\phase6a112_feel_free_templates.csv"
$output | Out-File -FilePath $outputFile -Encoding UTF8

Write-Host "✅ Exported templates to: $outputFile" -ForegroundColor Green
Write-Host ""

# Also export as JSON for easier processing
$jsonSql = @"
SELECT json_agg(t)
FROM (
    SELECT
        name,
        subject,
        html_body,
        text_body,
        description
    FROM communications.email_templates
    WHERE html_body ILIKE '%feel free%' OR text_body ILIKE '%feel free%'
    ORDER BY name
) t;
"@

$jsonOutput = & psql -h lankaconnect-staging-db.postgres.database.azure.com `
                    -U adminuser `
                    -d LankaConnectDB `
                    -c $jsonSql `
                    -t `
                    -A

$jsonFile = "c:\Work\LankaConnect\scripts\phase6a112_feel_free_templates.json"
$jsonOutput | Out-File -FilePath $jsonFile -Encoding UTF8

Write-Host "✅ Exported JSON to: $jsonFile" -ForegroundColor Green

# Extract just the template names for migration creation
$namesSql = @"
SELECT name
FROM communications.email_templates
WHERE html_body ILIKE '%feel free%' OR text_body ILIKE '%feel free%'
ORDER BY name;
"@

$namesOutput = & psql -h lankaconnect-staging-db.postgres.database.azure.com `
                     -U adminuser `
                     -d LankaConnectDB `
                     -c $namesSql `
                     -t `
                     -A

Write-Host ""
Write-Host "Template names with 'feel free' text:" -ForegroundColor Yellow
$namesOutput | ForEach-Object { Write-Host "  - $_" }

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review exported templates"
Write-Host "2. Identify exact 'feel free' text to remove"
Write-Host "3. Create migration with text replacement"

# Clean up password environment variable
Remove-Item env:PGPASSWORD
