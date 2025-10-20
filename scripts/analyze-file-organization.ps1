# File Organization Violation Detector
# Identifies .cs files with multiple type definitions

$ErrorActionPreference = 'Stop'
$results = @{
    multiTypeFiles = @()
    interfaceFileViolations = @()
    totalViolations = 0
    refactoringPlan = @()
}

# Get all C# files excluding build artifacts and migrations
$csFiles = Get-ChildItem -Path "C:\Work\LankaConnect" -Recurse -Filter "*.cs" |
    Where-Object {
        $_.FullName -notmatch '\\obj\\' -and
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch 'AssemblyInfo\.cs$' -and
        $_.FullName -notmatch 'MvcApplicationPartsAssemblyInfo\.cs$' -and
        $_.FullName -notmatch '\.Designer\.cs$'
    }

Write-Host "Analyzing $($csFiles.Count) C# files..." -ForegroundColor Cyan
$fileCount = 0

foreach ($file in $csFiles) {
    $fileCount++
    if ($fileCount % 50 -eq 0) {
        Write-Host "Progress: $fileCount / $($csFiles.Count) files analyzed..." -ForegroundColor Yellow
    }

    $content = Get-Content $file.FullName -Raw
    if (-not $content) { continue }

    # Detect type declarations (excluding nested types within braces)
    $types = @()

    # Match class declarations
    $classMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal|protected|private)?\s*(?:static|abstract|sealed|partial)?\s*class\s+(\w+)')
    foreach ($match in $classMatches) {
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $types += @{
            kind = "class"
            name = $match.Groups[1].Value
            line = $lineNumber
            shouldBeSeparate = $true
        }
    }

    # Match interface declarations
    $interfaceMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal|protected|private)?\s*(?:partial)?\s*interface\s+(\w+)')
    foreach ($match in $interfaceMatches) {
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $types += @{
            kind = "interface"
            name = $match.Groups[1].Value
            line = $lineNumber
            shouldBeSeparate = $true
        }
    }

    # Match enum declarations
    $enumMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal|protected|private)?\s*enum\s+(\w+)')
    foreach ($match in $enumMatches) {
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $types += @{
            kind = "enum"
            name = $match.Groups[1].Value
            line = $lineNumber
            shouldBeSeparate = $true
        }
    }

    # Match struct declarations
    $structMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal|protected|private)?\s*(?:readonly)?\s*struct\s+(\w+)')
    foreach ($match in $structMatches) {
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $types += @{
            kind = "struct"
            name = $match.Groups[1].Value
            line = $lineNumber
            shouldBeSeparate = $true
        }
    }

    # Match record declarations
    $recordMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal|protected|private)?\s*record\s+(\w+)')
    foreach ($match in $recordMatches) {
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $types += @{
            kind = "record"
            name = $match.Groups[1].Value
            line = $lineNumber
            shouldBeSeparate = $true
        }
    }

    # If more than one type declaration found
    if ($types.Count -gt 1) {
        $relativePath = $file.FullName.Replace("C:\Work\LankaConnect\", "")

        $violation = @{
            file = $relativePath
            typeCount = $types.Count
            types = $types
        }

        $results.multiTypeFiles += $violation
        $results.totalViolations++

        # Check if this is an interface file with embedded types
        $interfaceTypes = $types | Where-Object { $_.kind -eq "interface" }
        if ($interfaceTypes.Count -gt 0) {
            $embeddedTypes = $types | Where-Object { $_.kind -ne "interface" }
            if ($embeddedTypes.Count -gt 0) {
                $results.interfaceFileViolations += @{
                    file = $relativePath
                    primaryInterface = $interfaceTypes[0].name
                    embeddedTypes = $embeddedTypes
                }
            }
        }

        # Generate refactoring plan
        foreach ($type in $types) {
            $suggestedFileName = "$($type.name).cs"
            $currentDir = Split-Path $file.FullName -Parent
            $suggestedPath = Join-Path $currentDir $suggestedFileName

            $results.refactoringPlan += @{
                currentFile = $relativePath
                typeName = $type.name
                typeKind = $type.kind
                line = $type.line
                suggestedNewFile = $suggestedPath.Replace("C:\Work\LankaConnect\", "")
                action = "Extract to separate file"
            }
        }
    }
}

# Output results as JSON
$jsonOutput = $results | ConvertTo-Json -Depth 10
Write-Output $jsonOutput

# Summary
Write-Host "`n========================================" -ForegroundColor Green
Write-Host "FILE ORGANIZATION AUDIT SUMMARY" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "Total Files Analyzed: $($csFiles.Count)" -ForegroundColor White
Write-Host "Files with Multiple Types: $($results.multiTypeFiles.Count)" -ForegroundColor Yellow
Write-Host "Interface File Violations: $($results.interfaceFileViolations.Count)" -ForegroundColor Red
Write-Host "Total Violations: $($results.totalViolations)" -ForegroundColor Red
Write-Host "Refactoring Actions Required: $($results.refactoringPlan.Count)" -ForegroundColor Magenta
Write-Host "========================================`n" -ForegroundColor Green
