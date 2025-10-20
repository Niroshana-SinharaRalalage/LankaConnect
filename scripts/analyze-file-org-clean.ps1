# File Organization Violation Detector - Clean JSON Output
$ErrorActionPreference = 'Stop'
$results = @{
    multiTypeFiles = @()
    interfaceFileViolations = @()
    totalViolations = 0
    refactoringPlan = @()
}

$csFiles = Get-ChildItem -Path "C:\Work\LankaConnect" -Recurse -Filter "*.cs" |
    Where-Object {
        $_.FullName -notmatch '\\obj\\' -and
        $_.FullName -notmatch '\\bin\\' -and
        $_.FullName -notmatch 'AssemblyInfo\.cs$' -and
        $_.FullName -notmatch 'MvcApplicationPartsAssemblyInfo\.cs$' -and
        $_.FullName -notmatch '\.Designer\.cs$' -and
        $_.FullName -notmatch '\\Migrations\\'
    }

$fileCount = 0
foreach ($file in $csFiles) {
    $fileCount++
    $content = Get-Content $file.FullName -Raw
    if (-not $content) { continue }

    $types = @()

    # Match class declarations at namespace/file level (not nested)
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
    $recordMatches = [regex]::Matches($content, '(?m)^\s*(?:public|internal|protected|private)?\s*record\s+(?:class\s+)?(\w+)')
    foreach ($match in $recordMatches) {
        $lineNumber = ($content.Substring(0, $match.Index) -split "`n").Count
        $types += @{
            kind = "record"
            name = $match.Groups[1].Value
            line = $lineNumber
            shouldBeSeparate = $true
        }
    }

    if ($types.Count -gt 1) {
        $relativePath = $file.FullName.Replace("C:\Work\LankaConnect\", "")

        $violation = @{
            file = $relativePath
            typeCount = $types.Count
            types = $types
        }

        $results.multiTypeFiles += $violation
        $results.totalViolations++

        # Check interface files with embedded types
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

# Output only JSON
$results | ConvertTo-Json -Depth 10 -Compress
