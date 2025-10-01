# TDD Phase D: Systematic Type Discovery and Analysis Script
# Zero Tolerance Compilation Error Elimination - Type Extraction Preparation

param(
    [string]$SourcePath = "src",
    [string]$OutputDirectory = ".",
    [switch]$Verbose = $false
)

# Ensure output directory exists
if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

Write-Host "🔍 TDD Phase D: Systematic Type Discovery Starting..." -ForegroundColor Green
Write-Host "📁 Source Path: $SourcePath" -ForegroundColor Cyan
Write-Host "📊 Output Directory: $OutputDirectory" -ForegroundColor Cyan

# Initialize collections for analysis
$typeInventory = @()
$embeddedTypes = @()
$duplicateTypes = @()
$compilationErrors = @()

# Function to extract type information from C# files
function Get-TypeDefinitions {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $types = @()

    # Regex patterns for different type definitions
    $typePatterns = @{
        'Class' = '(?m)^\s*(?:public|internal|private)?\s*(?:abstract|sealed|static)?\s*class\s+(\w+)'
        'Interface' = '(?m)^\s*(?:public|internal|private)?\s*interface\s+(\w+)'
        'Enum' = '(?m)^\s*(?:public|internal|private)?\s*enum\s+(\w+)'
        'Struct' = '(?m)^\s*(?:public|internal|private)?\s*(?:readonly)?\s*struct\s+(\w+)'
        'Record' = '(?m)^\s*(?:public|internal|private)?\s*record\s+(\w+)'
    }

    foreach ($typeKind in $typePatterns.Keys) {
        $matches = [regex]::Matches($content, $typePatterns[$typeKind])
        foreach ($match in $matches) {
            $types += [PSCustomObject]@{
                TypeName = $match.Groups[1].Value
                TypeKind = $typeKind
                FilePath = $FilePath
                FileName = Split-Path $FilePath -Leaf
                RelativePath = $FilePath.Replace("$pwd\", "")
                LineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
            }
        }
    }

    return $types
}

# Function to extract namespace from file
function Get-FileNamespace {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $namespaceMatch = [regex]::Match($content, '(?m)^\s*namespace\s+([\w\.]+)')

    if ($namespaceMatch.Success) {
        return $namespaceMatch.Groups[1].Value
    }

    return "Unknown"
}

# Function to detect CS0104 and CS0535 errors
function Get-CompilationErrors {
    Write-Host "🔧 Building solution to detect compilation errors..." -ForegroundColor Yellow

    $buildOutput = dotnet build --no-restore 2>&1
    $errors = @()

    foreach ($line in $buildOutput) {
        if ($line -match "(CS0104|CS0535)") {
            $errors += $line
        }
    }

    return $errors
}

# Main discovery process
Write-Host "📋 Discovering all type definitions..." -ForegroundColor Yellow

Get-ChildItem -Path $SourcePath -Recurse -Filter "*.cs" | ForEach-Object {
    $filePath = $_.FullName
    $fileName = $_.Name
    $namespace = Get-FileNamespace $filePath

    if ($Verbose) {
        Write-Host "  Processing: $fileName" -ForegroundColor Gray
    }

    $types = Get-TypeDefinitions $filePath

    foreach ($type in $types) {
        $type | Add-Member -NotePropertyName "Namespace" -NotePropertyValue $namespace
        $typeInventory += $type
    }

    # Check for embedded types (multiple types in single file)
    if ($types.Count -gt 1) {
        $embeddedTypes += [PSCustomObject]@{
            FilePath = $filePath
            FileName = $fileName
            Namespace = $namespace
            TypeCount = $types.Count
            Types = ($types.TypeName -join ", ")
        }

        if ($Verbose) {
            Write-Host "    ⚠️  EMBEDDED TYPES: $($types.Count) types found" -ForegroundColor Red
        }
    }
}

# Analyze duplicates
Write-Host "🔍 Analyzing duplicate types..." -ForegroundColor Yellow

$duplicateGroups = $typeInventory | Group-Object TypeName | Where-Object { $_.Count -gt 1 }

foreach ($group in $duplicateGroups) {
    $duplicateTypes += [PSCustomObject]@{
        TypeName = $group.Name
        DuplicateCount = $group.Count
        Locations = ($group.Group | ForEach-Object { "$($_.RelativePath):$($_.LineNumber)" }) -join "; "
        Namespaces = ($group.Group.Namespace | Sort-Object -Unique) -join "; "
        Files = ($group.Group.FileName | Sort-Object -Unique) -join "; "
    }
}

# Get current compilation errors
Write-Host "🔧 Analyzing current compilation errors..." -ForegroundColor Yellow
$compilationErrors = Get-CompilationErrors

# Generate comprehensive analysis report
Write-Host "📊 Generating analysis reports..." -ForegroundColor Yellow

# 1. Type Inventory Report
$typeInventory | Export-Csv "$OutputDirectory\type-inventory.csv" -NoTypeInformation
Write-Host "✅ Type inventory saved: type-inventory.csv ($($typeInventory.Count) types)" -ForegroundColor Green

# 2. Embedded Types Report
$embeddedTypes | Export-Csv "$OutputDirectory\embedded-types.csv" -NoTypeInformation
Write-Host "✅ Embedded types saved: embedded-types.csv ($($embeddedTypes.Count) files)" -ForegroundColor Green

# 3. Duplicate Types Report
$duplicateTypes | Export-Csv "$OutputDirectory\duplicate-types.csv" -NoTypeInformation
Write-Host "✅ Duplicate types saved: duplicate-types.csv ($($duplicateTypes.Count) duplicates)" -ForegroundColor Green

# 4. Compilation Errors Report
$compilationErrors | Out-File "$OutputDirectory\compilation-errors.txt" -Encoding UTF8
Write-Host "✅ Compilation errors saved: compilation-errors.txt ($($compilationErrors.Count) errors)" -ForegroundColor Green

# Generate summary analysis
$summary = @"
# TDD Phase D: Type Discovery Analysis Summary

## Overall Statistics
- **Total Types Discovered**: $($typeInventory.Count)
- **Files with Embedded Types**: $($embeddedTypes.Count)
- **Duplicate Type Names**: $($duplicateTypes.Count)
- **Current Compilation Errors**: $($compilationErrors.Count)

## Type Distribution by Kind
$(($typeInventory | Group-Object TypeKind | ForEach-Object { "- **$($_.Name)**: $($_.Count)" }) -join "`n")

## Layer Distribution
$(($typeInventory | Group-Object { ($_.Namespace -split '\.')[2] } | ForEach-Object { "- **$($_.Name)**: $($_.Count)" }) -join "`n")

## Critical Issues Identified

### 1. Files with Multiple Embedded Types (Architectural Violation)
$(if ($embeddedTypes.Count -gt 0) {
    ($embeddedTypes | Select-Object -First 10 | ForEach-Object { "- **$($_.FileName)**: $($_.TypeCount) types ($($_.Types))" }) -join "`n"
} else {
    "✅ No embedded types found"
})

### 2. Duplicate Type Names (CS0104 Risk)
$(if ($duplicateTypes.Count -gt 0) {
    ($duplicateTypes | Select-Object -First 10 | ForEach-Object { "- **$($_.TypeName)**: $($_.DuplicateCount) definitions across $($_.Namespaces)" }) -join "`n"
} else {
    "✅ No duplicate types found"
})

### 3. Current Compilation Errors (CS0104/CS0535)
$(if ($compilationErrors.Count -gt 0) {
    "``````"
    ($compilationErrors | Select-Object -First 5) -join "`n"
    "``````"
} else {
    "✅ No CS0104/CS0535 errors found"
})

## Recommended Extraction Priority

### Phase 1: Foundation Types (Low Risk)
$(($typeInventory | Where-Object { $_.TypeKind -eq 'Enum' } | Select-Object -First 5 | ForEach-Object { "- $($_.TypeName) ($($_.TypeKind))" }) -join "`n")

### Phase 2: Value Objects (Medium Risk)
$(($typeInventory | Where-Object { $_.TypeKind -eq 'Record' -or ($_.TypeKind -eq 'Class' -and $_.TypeName -like '*ValueObject*') } | Select-Object -First 5 | ForEach-Object { "- $($_.TypeName) ($($_.TypeKind))" }) -join "`n")

### Phase 3: Domain Models (High Risk)
$(($typeInventory | Where-Object { $_.TypeKind -eq 'Class' -and $_.Namespace -like '*Domain*' } | Select-Object -First 5 | ForEach-Object { "- $($_.TypeName) ($($_.TypeKind))" }) -join "`n")

## Next Steps
1. Review duplicate types for consolidation opportunities
2. Extract embedded types using incremental approach
3. Validate compilation after each extraction phase
4. Update using statements and namespace references

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
"@

$summary | Out-File "$OutputDirectory\type-discovery-summary.md" -Encoding UTF8
Write-Host "✅ Summary analysis saved: type-discovery-summary.md" -ForegroundColor Green

# Display summary to console
Write-Host "`n📋 DISCOVERY SUMMARY" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host "Total Types: $($typeInventory.Count)" -ForegroundColor Cyan
Write-Host "Embedded Type Files: $($embeddedTypes.Count)" -ForegroundColor Yellow
Write-Host "Duplicate Types: $($duplicateTypes.Count)" -ForegroundColor Red
Write-Host "Compilation Errors: $($compilationErrors.Count)" -ForegroundColor Red

if ($duplicateTypes.Count -gt 0) {
    Write-Host "`n🚨 TOP DUPLICATE TYPES TO ADDRESS:" -ForegroundColor Red
    $duplicateTypes | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.TypeName) ($($_.DuplicateCount) locations)" -ForegroundColor Red
    }
}

if ($embeddedTypes.Count -gt 0) {
    Write-Host "`n⚠️ TOP EMBEDDED TYPE FILES TO EXTRACT:" -ForegroundColor Yellow
    $embeddedTypes | Select-Object -First 5 | ForEach-Object {
        Write-Host "  - $($_.FileName) ($($_.TypeCount) types)" -ForegroundColor Yellow
    }
}

Write-Host "`n✅ TDD Phase D Discovery Complete!" -ForegroundColor Green
Write-Host "📁 Review generated reports in: $OutputDirectory" -ForegroundColor Cyan
Write-Host "🎯 Ready for systematic type extraction phase" -ForegroundColor Green