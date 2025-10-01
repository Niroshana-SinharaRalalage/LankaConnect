#!/usr/bin/env pwsh

# PowerShell script to systematically fix missing using directives
# Target: Fix CS0246 errors for the most common missing types

# Define the mappings of types to their namespaces
$typeNamespaceMap = @{
    'SouthAsianLanguage' = 'LankaConnect.Domain.Common.Enums'
    'GeographicRegion' = 'LankaConnect.Domain.Common.Enums'
    'CulturalEventType' = 'LankaConnect.Domain.Common.Enums'
    'GenerationalCohort' = 'LankaConnect.Domain.Shared'
    'SacredContentType' = 'LankaConnect.Domain.Shared'
    'CulturalEvent' = 'LankaConnect.Domain.Shared'
    'CulturalRegion' = 'LankaConnect.Domain.Shared'
    'SecurityIncident' = 'LankaConnect.Domain.Infrastructure'
    'ResponseAction' = 'LankaConnect.Domain.Infrastructure'
    'ComplianceValidationResult' = 'LankaConnect.Domain.Infrastructure'
    'SensitivityLevel' = 'LankaConnect.Domain.Infrastructure'
    'CulturalProfile' = 'LankaConnect.Domain.Infrastructure'
    'CulturalUserProfile' = 'LankaConnect.Domain.Infrastructure'
    'SecurityProfile' = 'LankaConnect.Domain.Infrastructure'
    'AutoScalingDecision' = 'LankaConnect.Domain.Infrastructure.Scaling'
    'DatabaseScalingMetrics' = 'LankaConnect.Domain.Common.Database'
    'ConnectionPoolHealth' = 'LankaConnect.Domain.Common.Database'
}

# Get all C# files in Infrastructure and Application layers
$files = Get-ChildItem -Path "src\LankaConnect.Infrastructure", "src\LankaConnect.Application" -Recurse -Filter "*.cs"

$fixedFiles = 0
$totalFixes = 0

foreach ($file in $files) {
    Write-Host "Processing: $($file.FullName)"

    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    $modified = $false

    # Check which types are used in this file and add missing using directives
    foreach ($type in $typeNamespaceMap.Keys) {
        $namespace = $typeNamespaceMap[$type]

        # Check if the type is used in the file
        if ($content -match "\b$type\b" -and $content -notmatch "using\s+$namespace\s*;") {
            # Add the using directive if not already present
            if ($content -match "using\s+System") {
                $content = $content -replace "(using\s+System[^;]*;)", "`$1`nusing $namespace;"
                $modified = $true
                $totalFixes++
                Write-Host "  Added: using $namespace; for type $type"
            }
        }
    }

    # Write back if modified
    if ($modified) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $fixedFiles++
        Write-Host "  Fixed file: $($file.Name)"
    }
}

Write-Host "`n=== SUMMARY ==="
Write-Host "Files processed: $($files.Count)"
Write-Host "Files modified: $fixedFiles"
Write-Host "Total using directives added: $totalFixes"
Write-Host "`nRunning build to check progress..."

# Run build to check progress
$errorCount = (dotnet build 2>&1 | Select-String "error CS" | Measure-Object).Count
Write-Host "Current error count: $errorCount"