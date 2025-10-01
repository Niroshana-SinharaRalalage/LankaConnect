# TDD Phase D: Simple Type Discovery Script
param(
    [string]$SourcePath = "src",
    [string]$OutputDirectory = "."
)

Write-Host "🔍 TDD Phase D: Type Discovery Starting..." -ForegroundColor Green

# Initialize collections
$typeInventory = @()
$embeddedTypes = @()
$duplicateTypes = @()

# Function to extract type information
function Get-TypeDefinitions {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $types = @()

    # Simple regex patterns for type definitions
    $classPattern = '(?m)^\s*(?:public|internal|private)?\s*(?:abstract|sealed|static)?\s*class\s+(\w+)'
    $interfacePattern = '(?m)^\s*(?:public|internal|private)?\s*interface\s+(\w+)'
    $enumPattern = '(?m)^\s*(?:public|internal|private)?\s*enum\s+(\w+)'
    $structPattern = '(?m)^\s*(?:public|internal|private)?\s*(?:readonly)?\s*struct\s+(\w+)'
    $recordPattern = '(?m)^\s*(?:public|internal|private)?\s*record\s+(\w+)'

    # Extract classes
    $classMatches = [regex]::Matches($content, $classPattern)
    foreach ($match in $classMatches) {
        $types += [PSCustomObject]@{
            TypeName = $match.Groups[1].Value
            TypeKind = "Class"
            FilePath = $FilePath
            FileName = Split-Path $FilePath -Leaf
        }
    }

    # Extract interfaces
    $interfaceMatches = [regex]::Matches($content, $interfacePattern)
    foreach ($match in $interfaceMatches) {
        $types += [PSCustomObject]@{
            TypeName = $match.Groups[1].Value
            TypeKind = "Interface"
            FilePath = $FilePath
            FileName = Split-Path $FilePath -Leaf
        }
    }

    # Extract enums
    $enumMatches = [regex]::Matches($content, $enumPattern)
    foreach ($match in $enumMatches) {
        $types += [PSCustomObject]@{
            TypeName = $match.Groups[1].Value
            TypeKind = "Enum"
            FilePath = $FilePath
            FileName = Split-Path $FilePath -Leaf
        }
    }

    return $types
}

# Main discovery process
Write-Host "📋 Discovering type definitions..." -ForegroundColor Yellow

Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" | ForEach-Object {
    $filePath = $_.FullName
    $fileName = $_.Name

    $types = Get-TypeDefinitions $filePath

    foreach ($type in $types) {
        $typeInventory += $type
    }

    # Check for embedded types
    if ($types.Count -gt 1) {
        $typeNames = ($types.TypeName -join ", ")
        $embeddedTypes += [PSCustomObject]@{
            FilePath = $filePath
            FileName = $fileName
            TypeCount = $types.Count
            Types = $typeNames
        }
        Write-Host "  ⚠️ EMBEDDED TYPES: $fileName - $($types.Count) types" -ForegroundColor Yellow
    }
}

# Analyze duplicates
Write-Host "🔍 Analyzing duplicate types..." -ForegroundColor Yellow

$duplicateGroups = $typeInventory | Group-Object TypeName | Where-Object { $_.Count -gt 1 }

foreach ($group in $duplicateGroups) {
    $locations = ($group.Group | ForEach-Object { $_.FileName }) -join "; "
    $duplicateTypes += [PSCustomObject]@{
        TypeName = $group.Name
        DuplicateCount = $group.Count
        Files = $locations
    }
    Write-Host "  🚨 DUPLICATE: $($group.Name) - $($group.Count) locations" -ForegroundColor Red
}

# Get compilation errors
Write-Host "🔧 Checking compilation errors..." -ForegroundColor Yellow
$buildOutput = dotnet build --no-restore 2>&1
$compilationErrors = $buildOutput | Where-Object { $_ -match "(CS0104|CS0535)" }

# Export results
$typeInventory | Export-Csv "$OutputDirectory\type-inventory.csv" -NoTypeInformation
$embeddedTypes | Export-Csv "$OutputDirectory\embedded-types.csv" -NoTypeInformation
$duplicateTypes | Export-Csv "$OutputDirectory\duplicate-types.csv" -NoTypeInformation
$compilationErrors | Out-File "$OutputDirectory\compilation-errors.txt"

# Summary
Write-Host "`n📊 DISCOVERY SUMMARY" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host "Total Types: $($typeInventory.Count)" -ForegroundColor Cyan
Write-Host "Embedded Type Files: $($embeddedTypes.Count)" -ForegroundColor Yellow
Write-Host "Duplicate Types: $($duplicateTypes.Count)" -ForegroundColor Red
Write-Host "Compilation Errors: $($compilationErrors.Count)" -ForegroundColor Red

if ($duplicateTypes.Count -gt 0) {
    Write-Host "`nTOP DUPLICATE TYPES:" -ForegroundColor Red
    $duplicateTypes | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.TypeName) ($($_.DuplicateCount) locations)" -ForegroundColor Red
    }
}

if ($embeddedTypes.Count -gt 0) {
    Write-Host "`nTOP EMBEDDED TYPE FILES:" -ForegroundColor Yellow
    $embeddedTypes | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.FileName) ($($_.TypeCount) types)" -ForegroundColor Yellow
    }
}

Write-Host "`n✅ TDD Phase D Discovery Complete!" -ForegroundColor Green
Write-Host "📁 Reports saved to: $OutputDirectory" -ForegroundColor Cyan