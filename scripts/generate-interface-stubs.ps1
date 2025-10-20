# INTERFACE STUB GENERATOR - Option B Implementation
# Generates NotImplementedException stubs for all missing interface methods

param(
    [Parameter(Mandatory=$true)]
    [string]$InterfaceFile,

    [Parameter(Mandatory=$true)]
    [string]$ImplementationFile,

    [string]$IssueNumber = "TBD"
)

Write-Host "=== INTERFACE STUB GENERATOR ===" -ForegroundColor Cyan
Write-Host "Interface: $InterfaceFile"
Write-Host "Implementation: $ImplementationFile"
Write-Host ""

# Read interface content
$interfaceContent = Get-Content $InterfaceFile -Raw

# Extract all method signatures using regex
$methodPattern = '(?ms)Task<([^>]+)>\s+(\w+Async)\s*\(([^)]*)\)'
$matches = [regex]::Matches($interfaceContent, $methodPattern)

Write-Host "Found $($matches.Count) async methods in interface" -ForegroundColor Green

# Read current implementation to check which methods exist
$implementationContent = Get-Content $ImplementationFile -Raw

$stubMethods = @()
$existingCount = 0

foreach ($match in $matches) {
    $returnType = $match.Groups[1].Value.Trim()
    $methodName = $match.Groups[2].Value.Trim()
    $parameters = $match.Groups[3].Value.Trim()

    # Check if method already exists in implementation
    if ($implementationContent -match "public\s+async\s+Task<[^>]+>\s+$methodName\s*\(") {
        $existingCount++
        continue
    }

    # Generate stub implementation
    $stub = @"

        /// <summary>
        /// [PHASE 2] Deferred implementation
        /// </summary>
        public async Task<$returnType> $methodName($parameters)
        {
            const string issueNumber = "#$IssueNumber";
            const string deferredMessage =
                "This method is deferred to Phase 2 implementation. " +
                "Not required for MVP production release. " +
                `$"Tracked in issue {issueNumber}.";

            _logger.LogWarning(
                "Deferred method called: {MethodName}. {Message}",
                nameof($methodName),
                deferredMessage);

            await Task.CompletedTask;
            throw new NotImplementedException(deferredMessage);
        }
"@

    $stubMethods += $stub
}

Write-Host ""
Write-Host "Methods already implemented: $existingCount" -ForegroundColor Yellow
Write-Host "Methods to generate: $($stubMethods.Count)" -ForegroundColor Cyan

# Output stubs to file
$outputFile = $ImplementationFile + ".stubs.txt"
$stubMethods -join "`n`n" | Out-File $outputFile -Encoding UTF8

Write-Host ""
Write-Host "Stubs generated: $outputFile" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review generated stubs"
Write-Host "2. Add to $ImplementationFile before final closing brace"
Write-Host "3. Run: dotnet build"
Write-Host ""
