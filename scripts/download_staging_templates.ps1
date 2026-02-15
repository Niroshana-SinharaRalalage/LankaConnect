# Download Signup List & Forms email templates from staging database
# Save to C:\Work\LankaConnect\Template_Correction\staging

$outputDir = "C:\Work\LankaConnect\Template_Correction\staging"

Write-Host "Downloading templates from staging database..." -ForegroundColor Cyan
Write-Host ""

# Query to get signup and form-related templates
$query = @"
COPY (
    SELECT
        name,
        subject_template,
        html_template,
        text_template,
        type,
        category,
        is_active
    FROM communications.email_templates
    WHERE LOWER(name) LIKE '%signup%'
       OR LOWER(name) LIKE '%form%'
       OR LOWER(name) LIKE '%commitment%'
    ORDER BY name
) TO STDOUT WITH (FORMAT CSV, HEADER true, DELIMITER ',', QUOTE '"', ESCAPE '"');
"@

# Execute query via Azure CLI
Write-Host "Executing query on lankaconnect-staging-db..." -ForegroundColor Yellow

$csvOutput = az postgres flexible-server execute `
    --name lankaconnect-staging-db `
    --resource-group lankaconnect-staging `
    --admin-user lcadmin `
    --database-name lankaconnect `
    --querytext $query `
    2>&1

if ($LASTEXITCODE -eq 0) {
    # Save CSV output
    $csvFile = "$outputDir\templates.csv"
    $csvOutput | Out-File -FilePath $csvFile -Encoding UTF8

    Write-Host "✅ Templates exported to: $csvFile" -ForegroundColor Green
}
else {
    Write-Host "❌ Error querying database" -ForegroundColor Red
    Write-Host $csvOutput -ForegroundColor Yellow

    Write-Host ""
    Write-Host "Trying alternative approach with JSON output..." -ForegroundColor Cyan

    # Try JSON format
    $jsonQuery = "SELECT row_to_json(t) FROM (SELECT name, subject_template, html_template, text_template FROM communications.email_templates WHERE LOWER(name) LIKE '%signup%' OR LOWER(name) LIKE '%form%' OR LOWER(name) LIKE '%commitment%' ORDER BY name) t;"

    $jsonOutput = az postgres flexible-server execute `
        --name lankaconnect-staging-db `
        --resource-group lankaconnect-staging `
        --admin-user lcadmin `
        --database-name lankaconnect `
        --querytext $jsonQuery `
        --output json `
        2>&1

    if ($LASTEXITCODE -eq 0) {
        $jsonFile = "$outputDir\templates.json"
        $jsonOutput | Out-File -FilePath $jsonFile -Encoding UTF8
        Write-Host "✅ Templates exported to: $jsonFile" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Cyan
