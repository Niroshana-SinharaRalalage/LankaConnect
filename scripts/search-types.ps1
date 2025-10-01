# Type Discovery Script
param(
    [string]$TypesFile = "C:\Work\LankaConnect\missing_types_unique.txt",
    [string]$OutputFile = "C:\Work\LankaConnect\scripts\type_discovery_results.json"
)

$types = Get-Content $TypesFile | Where-Object { $_ -match '\w+' } | ForEach-Object {
    if ($_ -match '-&gt;(.+)$') {
        $matches[1].Trim()
    } else {
        $_.Trim()
    }
} | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$results = @()
$foundCount = 0
$missingCount = 0

Write-Host "Analyzing $($types.Count) types..." -ForegroundColor Cyan

foreach ($type in $types) {
    Write-Host "Searching: $type" -ForegroundColor Yellow

    $searchResult = @{
        TypeName = $type
        Found = $false
        Locations = @()
        DefinitionType = $null
        Category = $null
        Priority = "P3"
        ReferenceCount = 0
    }

    # Search for class definitions
    $classMatches = rg "class\s+$type\b" --type cs -l 2>$null
    if ($classMatches) {
        $searchResult.Found = $true
        $searchResult.DefinitionType = "class"
        $searchResult.Locations += $classMatches
    }

    # Search for record definitions
    $recordMatches = rg "record\s+$type\b" --type cs -l 2>$null
    if ($recordMatches) {
        $searchResult.Found = $true
        $searchResult.DefinitionType = "record"
        $searchResult.Locations += $recordMatches
    }

    # Search for interface definitions
    $interfaceMatches = rg "interface\s+$type\b" --type cs -l 2>$null
    if ($interfaceMatches) {
        $searchResult.Found = $true
        $searchResult.DefinitionType = "interface"
        $searchResult.Locations += $interfaceMatches
    }

    # Search for enum definitions
    $enumMatches = rg "enum\s+$type\b" --type cs -l 2>$null
    if ($enumMatches) {
        $searchResult.Found = $true
        $searchResult.DefinitionType = "enum"
        $searchResult.Locations += $enumMatches
    }

    # Count references
    $refCount = (rg "\b$type\b" --type cs 2>$null | Measure-Object).Count
    $searchResult.ReferenceCount = $refCount

    if ($refCount -gt 10) { $searchResult.Priority = "P0" }
    elseif ($refCount -gt 5) { $searchResult.Priority = "P1" }
    elseif ($refCount -gt 2) { $searchResult.Priority = "P2" }

    # Categorize type
    if ($type -match "(Status|Level|Priority|Type|Category)$") {
        $searchResult.Category = "Enum"
    }
    elseif ($type -match "(Configuration|Settings|Options|Policy)$") {
        $searchResult.Category = "Configuration"
    }
    elseif ($type -match "(Result|Response|Report|Analysis)$") {
        $searchResult.Category = "Result"
    }
    elseif ($type -match "(Request|Command|Query)$") {
        $searchResult.Category = "Command"
    }
    elseif ($type -match "(Metrics|Data|Info)$") {
        $searchResult.Category = "Data"
    }
    elseif ($type -match "^I[A-Z]") {
        $searchResult.Category = "Interface"
    }
    else {
        $searchResult.Category = "Entity"
    }

    if ($searchResult.Found) {
        $foundCount++
        Write-Host "  [FOUND] as $($searchResult.DefinitionType)" -ForegroundColor Green
    } else {
        $missingCount++
        Write-Host "  [MISSING] category: $($searchResult.Category)" -ForegroundColor Red
    }

    $results += $searchResult
}

$summary = @{
    TotalTypes = $types.Count
    FoundTypes = $foundCount
    MissingTypes = $missingCount
}

$output = @{
    Summary = $summary
    Results = $results
    FoundTypes = $results | Where-Object Found
    MissingTypes = $results | Where-Object { -not $_.Found } | Sort-Object Priority, Category
}

$output | ConvertTo-Json -Depth 10 | Out-File $OutputFile -Encoding UTF8

Write-Host "`n=== SUMMARY ===" -ForegroundColor Cyan
Write-Host "Total Types: $($types.Count)" -ForegroundColor White
Write-Host "Found: $foundCount" -ForegroundColor Green
Write-Host "Missing: $missingCount" -ForegroundColor Red
Write-Host "`nResults saved to: $OutputFile" -ForegroundColor Cyan
