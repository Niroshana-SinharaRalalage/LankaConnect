# Export Signup List and Signup Forms email templates from staging database
# Save to C:\Work\LankaConnect\Template_Correction\staging

$token = Get-Content 'C:\Work\LankaConnect\.token.txt'
$outputDir = "C:\Work\LankaConnect\Template_Correction\staging"

Write-Host "Exporting Signup List and Signup Forms templates from staging..." -ForegroundColor Cyan
Write-Host "Output directory: $outputDir" -ForegroundColor Gray
Write-Host ""

# Query staging database via diagnostic endpoint or direct query
# We'll use az postgres to query the database directly

Write-Host "Querying staging database for templates..." -ForegroundColor Yellow

# Get all templates with 'signup' or 'form' in the name
$query = @"
SELECT
    name,
    subject_template,
    html_template,
    text_template,
    type,
    category,
    is_active,
    created_at,
    updated_at
FROM communications.email_templates
WHERE LOWER(name) LIKE '%signup%'
   OR LOWER(name) LIKE '%form%'
   OR LOWER(name) LIKE '%commitment%'
ORDER BY name;
"@

# Since we don't have direct database access, let's export via a script
# We'll create a SQL file that can be run on staging

$sqlFile = "$outputDir\export_templates.sql"
$query | Out-File -FilePath $sqlFile -Encoding UTF8

Write-Host "✅ SQL query saved to: $sqlFile" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run this SQL query on staging database" -ForegroundColor Gray
Write-Host "2. Export results as JSON or individual HTML files" -ForegroundColor Gray
Write-Host ""
Write-Host "Alternatively, we can query via Azure CLI..." -ForegroundColor Cyan
