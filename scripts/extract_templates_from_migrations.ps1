# Extract email templates from migration files to Template_Correction/staging
# This gives us the templates that are currently in staging

$outputDir = "C:\Work\LankaConnect\Template_Correction\staging"
$migrationsDir = "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Migrations"

Write-Host "Extracting templates from migration files..." -ForegroundColor Cyan
Write-Host "Output: $outputDir" -ForegroundColor Gray
Write-Host ""

# Find migrations with "signup" or "form" in the name
$migrationFiles = Get-ChildItem -Path $migrationsDir -Filter "*Signup*.cs" -Recurse
$migrationFiles += Get-ChildItem -Path $migrationsDir -Filter "*Form*.cs" -Recurse
$migrationFiles += Get-ChildItem -Path $migrationsDir -Filter "*Commitment*.cs" -Recurse

Write-Host "Found $($migrationFiles.Count) migration files with signup/form templates" -ForegroundColor Yellow
Write-Host ""

foreach ($file in $migrationFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Cyan

    # Read file content
    $content = Get-Content $file.FullName -Raw

    # Extract template names and HTML
    if ($content -match "template-(signup|form|commitment)") {
        Write-Host "  ✅ Contains template definitions" -ForegroundColor Green

        # Save file info
        $infoFile = "$outputDir\$($file.BaseName).txt"
        "Migration: $($file.Name)`n" | Out-File -FilePath $infoFile -Encoding UTF8
        "Contains signup/form/commitment templates`n" | Out-File -FilePath $infoFile -Append -Encoding UTF8

        # Extract GetStandardTemplate calls or template HTML
        $htmlMatches = [regex]::Matches($content, '(?s)GetStandardTemplate\s*\([^)]+\)')

        if ($htmlMatches.Count -gt 0) {
            "Found $($htmlMatches.Count) template(s)" | Out-File -FilePath $infoFile -Append -Encoding UTF8
        }
    }
}

Write-Host ""
Write-Host "Extraction complete!" -ForegroundColor Green
Write-Host ""

# Now let's specifically extract Phase6A108 form response templates
Write-Host "Extracting Phase6A108 form response templates..." -ForegroundColor Cyan

$phase6a108 = Get-ChildItem -Path $migrationsDir -Filter "*Phase6A108*.cs" | Select-Object -First 1

if ($phase6a108) {
    Write-Host "Found: $($phase6a108.Name)" -ForegroundColor Green

    $content = Get-Content $phase6a108.FullName -Raw

    # Save full migration content for reference
    $refFile = "$outputDir\Phase6A108_FormResponseTemplates_Reference.cs"
    $content | Out-File -FilePath $refFile -Encoding UTF8

    Write-Host "✅ Saved reference: Phase6A108_FormResponseTemplates_Reference.cs" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done! Check $outputDir for extracted templates" -ForegroundColor Cyan
