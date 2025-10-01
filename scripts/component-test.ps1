#!/usr/bin/env pwsh
# LankaConnect Component Testing Automation
# Individual component and focused testing with cultural intelligence validation

param(
    [Parameter(Mandatory=$false)]
    [string]$Component = "All",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Unit", "Integration", "Cultural", "All")]
    [string]$TestType = "All",
    
    [Parameter(Mandatory=$false)]
    [string]$TestPattern = "*",
    
    [Parameter(Mandatory=$false)]
    [switch]$CollectCoverage,
    
    [Parameter(Mandatory=$false)]
    [switch]$ParallelExecution,
    
    [Parameter(Mandatory=$false)]
    [int]$MaxParallel = 4,
    
    [Parameter(Mandatory=$false)]
    [switch]$ContinueOnFailure,
    
    [Parameter(Mandatory=$false)]
    [switch]$DetailedOutput,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Import shared utilities
$UtilitiesPath = Join-Path $PSScriptRoot "automation\shared-utilities.ps1"
if (Test-Path $UtilitiesPath) {
    . $UtilitiesPath
} else {
    Write-Host "ERROR: Shared utilities not found at $UtilitiesPath" -ForegroundColor Red
    exit 1
}

# Script configuration
$ScriptName = "Component-Testing"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

# Test execution tracking
$Global:TestResults = @{
    StartTime = Get-Date
    Components = @{}
    Summary = @{
        TotalTests = 0
        PassedTests = 0
        FailedTests = 0
        SkippedTests = 0
        TotalDuration = 0
        CoveragePercentage = 0
    }
    CulturalTests = @{
        SinhalaTests = @()
        TamilTests = @()
        CulturalIntelligenceTests = @()
    }
    Errors = @()
    Warnings = @()
}

# Component definitions with cultural intelligence focus
$Components = @{
    "Domain" = @{
        ProjectPath = "tests/LankaConnect.Domain.Tests"
        Description = "Domain layer with business rules and cultural entities"
        CulturalAreas = @("CulturalIntelligence", "Community", "Events", "Shared")
        TestPatterns = @("*DomainTests.cs", "*AggregateTests.cs", "*ValueObjectTests.cs", "*EventTests.cs")
        RequiredCoverage = 90
    }
    "Application" = @{
        ProjectPath = "tests/LankaConnect.Application.Tests"
        Description = "Application services and cultural use cases"
        CulturalAreas = @("CulturalIntelligence", "Communications", "Users", "Businesses")
        TestPatterns = @("*HandlerTests.cs", "*ServiceTests.cs", "*ValidatorTests.cs", "*MappingTests.cs")
        RequiredCoverage = 85
    }
    "Infrastructure" = @{
        ProjectPath = "tests/LankaConnect.Infrastructure.Tests"
        Description = "Data access and external integrations"
        CulturalAreas = @("Database", "Cache", "Email", "Storage")
        TestPatterns = @("*RepositoryTests.cs", "*ServiceTests.cs", "*ConfigurationTests.cs")
        RequiredCoverage = 80
    }
    "Integration" = @{
        ProjectPath = "tests/LankaConnect.IntegrationTests"
        Description = "End-to-end cultural intelligence workflows"
        CulturalAreas = @("Controllers", "CulturalIntelligence", "Email", "Storage")
        TestPatterns = @("*IntegrationTests.cs", "*ApiTests.cs", "*WorkflowTests.cs")
        RequiredCoverage = 75
    }
}

function Initialize-ComponentTesting {
    Write-Log "🧪 Starting Component Testing Automation" "INFO"
    Write-Log "Component: $Component, Type: $TestType, Pattern: $TestPattern" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    # Build validation before testing
    Write-Log "Validating build before running tests" "INFO"
    if (!(Invoke-BuildValidation -ProjectRoot $ProjectRoot)) {
        Write-Log "Build validation failed - cannot run tests" "ERROR"
        return $false
    }
    
    return $true
}

function Get-ComponentsToTest {
    param([string]$ComponentFilter)
    
    if ($ComponentFilter -eq "All") {
        return $Components.Keys
    }
    
    $matchingComponents = @()
    foreach ($componentName in $Components.Keys) {
        if ($componentName -match $ComponentFilter) {
            $matchingComponents += $componentName
        }
    }
    
    if ($matchingComponents.Count -eq 0) {
        Write-Log "No components found matching filter: $ComponentFilter" "WARN"
        Write-Log "Available components: $($Components.Keys -join ', ')" "INFO"
    }
    
    return $matchingComponents
}

function Invoke-ComponentTests {
    param([string]$ComponentName)
    
    $componentInfo = $Components[$ComponentName]
    $projectPath = Join-Path $ProjectRoot $componentInfo.ProjectPath
    
    if (!(Test-Path $projectPath)) {
        Write-Log "Test project not found: $projectPath" "WARN"
        return @{
            ComponentName = $ComponentName
            Success = $false
            Skipped = $true
            Reason = "Project not found"
            TestResults = @{}
        }
    }
    
    Write-Log "Running tests for component: $ComponentName" "INFO"
    
    $componentResults = @{
        ComponentName = $ComponentName
        StartTime = Get-Date
        Success = $true
        TestResults = @{}
        CulturalTests = @{
            SinhalaSpecific = @()
            TamilSpecific = @()
            CulturalIntelligence = @()
        }
        Coverage = @{}
        Duration = 0
        Errors = @()
        Warnings = @()
    }
    
    try {
        # Run different test types based on selection
        if ($TestType -eq "All" -or $TestType -eq "Unit") {
            $unitTestResult = Invoke-UnitTests -ProjectPath $projectPath -ComponentInfo $componentInfo
            $componentResults.TestResults["Unit"] = $unitTestResult
            if (!$unitTestResult.Success) { $componentResults.Success = $false }
        }
        
        if ($TestType -eq "All" -or $TestType -eq "Integration") {
            $integrationTestResult = Invoke-IntegrationTests -ProjectPath $projectPath -ComponentInfo $componentInfo
            $componentResults.TestResults["Integration"] = $integrationTestResult
            if (!$integrationTestResult.Success) { $componentResults.Success = $false }
        }
        
        if ($TestType -eq "All" -or $TestType -eq "Cultural") {
            $culturalTestResult = Invoke-CulturalTests -ProjectPath $projectPath -ComponentInfo $componentInfo
            $componentResults.TestResults["Cultural"] = $culturalTestResult
            $componentResults.CulturalTests = $culturalTestResult.CulturalBreakdown
            if (!$culturalTestResult.Success) { $componentResults.Success = $false }
        }
        
        # Collect coverage if requested
        if ($CollectCoverage) {
            $coverageResult = Invoke-CoverageCollection -ProjectPath $projectPath -ComponentInfo $componentInfo
            $componentResults.Coverage = $coverageResult
            
            # Check coverage requirements
            if ($coverageResult.Percentage -lt $componentInfo.RequiredCoverage) {
                Write-Log "Coverage ($($coverageResult.Percentage)%) below required threshold ($($componentInfo.RequiredCoverage)%) for $ComponentName" "WARN"
                $componentResults.Warnings += "Coverage below required threshold"
            }
        }
        
        $componentResults.Duration = (Get-Date) - $componentResults.StartTime
        
        if ($componentResults.Success) {
            Write-Log "✅ Component tests passed for $ComponentName in $($componentResults.Duration.TotalSeconds)s" "SUCCESS"
        } else {
            Write-Log "❌ Component tests failed for $ComponentName" "ERROR"
        }
        
    }
    catch {
        $componentResults.Success = $false
        $componentResults.Errors += $_.Exception.Message
        Write-Log "Exception during testing of $ComponentName`: $($_.Exception.Message)" "ERROR"
    }
    
    $Global:TestResults.Components[$ComponentName] = $componentResults
    return $componentResults
}

function Invoke-UnitTests {
    param([string]$ProjectPath, [hashtable]$ComponentInfo)
    
    Write-Log "Running unit tests for $(Split-Path $ProjectPath -Leaf)" "INFO"
    
    $testArgs = @(
        "test",
        $ProjectPath,
        "--logger", "console;verbosity=detailed",
        "--no-build",
        "--filter", "Category!=Integration&Category!=Cultural"
    )
    
    if ($TestPattern -ne "*") {
        $testArgs += "--filter", "FullyQualifiedName~$TestPattern"
    }
    
    if ($ParallelExecution) {
        $testArgs += "--parallel", "--maxcpucount:$MaxParallel"
    }
    
    $testOutput = & dotnet @testArgs 2>&1
    $success = $LASTEXITCODE -eq 0
    
    # Parse test results
    $testCount = 0
    $passedCount = 0
    $failedCount = 0
    $skippedCount = 0
    
    if ($testOutput -match "Total tests: (\d+)") {
        $testCount = [int]$Matches[1]
    }
    if ($testOutput -match "Passed: (\d+)") {
        $passedCount = [int]$Matches[1]
    }
    if ($testOutput -match "Failed: (\d+)") {
        $failedCount = [int]$Matches[1]
    }
    if ($testOutput -match "Skipped: (\d+)") {
        $skippedCount = [int]$Matches[1]
    }
    
    return @{
        Success = $success
        TestCount = $testCount
        Passed = $passedCount
        Failed = $failedCount
        Skipped = $skippedCount
        Output = $testOutput
        Duration = 0 # Will be calculated by caller
    }
}

function Invoke-IntegrationTests {
    param([string]$ProjectPath, [hashtable]$ComponentInfo)
    
    Write-Log "Running integration tests for $(Split-Path $ProjectPath -Leaf)" "INFO"
    
    $testArgs = @(
        "test",
        $ProjectPath,
        "--logger", "console;verbosity=detailed",
        "--no-build",
        "--filter", "Category=Integration"
    )
    
    if ($ParallelExecution) {
        $testArgs += "--parallel", "--maxcpucount:$MaxParallel"
    }
    
    $testOutput = & dotnet @testArgs 2>&1
    $success = $LASTEXITCODE -eq 0
    
    # Parse results similar to unit tests
    $testCount = 0
    $passedCount = 0
    $failedCount = 0
    
    if ($testOutput -match "Total tests: (\d+)") {
        $testCount = [int]$Matches[1]
    }
    if ($testOutput -match "Passed: (\d+)") {
        $passedCount = [int]$Matches[1]
    }
    if ($testOutput -match "Failed: (\d+)") {
        $failedCount = [int]$Matches[1]
    }
    
    return @{
        Success = $success
        TestCount = $testCount
        Passed = $passedCount
        Failed = $failedCount
        Output = $testOutput
    }
}

function Invoke-CulturalTests {
    param([string]$ProjectPath, [hashtable]$ComponentInfo)
    
    Write-Log "Running cultural intelligence tests for $(Split-Path $ProjectPath -Leaf)" "INFO"
    
    # Run cultural-specific tests
    $culturalResults = @{
        Success = $true
        SinhalaTests = @()
        TamilTests = @()
        CulturalIntelligenceTests = @()
        TotalCulturalTests = 0
        PassedCulturalTests = 0
        CulturalBreakdown = @{}
    }
    
    # Test for Sinhala cultural patterns
    $sinhalaTestArgs = @(
        "test",
        $ProjectPath,
        "--logger", "console;verbosity=detailed",
        "--no-build",
        "--filter", "Category=Cultural&(TestName~Sinhala|TestName~si-LK)"
    )
    
    $sinhalaOutput = & dotnet @sinhalaTestArgs 2>&1
    $sinhalaSuccess = $LASTEXITCODE -eq 0
    
    if ($sinhalaOutput -match "Total tests: (\d+)") {
        $culturalResults.SinhalaTests = @{ Count = [int]$Matches[1]; Success = $sinhalaSuccess }
    }
    
    # Test for Tamil cultural patterns
    $tamilTestArgs = @(
        "test",
        $ProjectPath,
        "--logger", "console;verbosity=detailed",
        "--no-build",
        "--filter", "Category=Cultural&(TestName~Tamil|TestName~ta-LK)"
    )
    
    $tamilOutput = & dotnet @tamilTestArgs 2>&1
    $tamilSuccess = $LASTEXITCODE -eq 0
    
    if ($tamilOutput -match "Total tests: (\d+)") {
        $culturalResults.TamilTests = @{ Count = [int]$Matches[1]; Success = $tamilSuccess }
    }
    
    # Test for general cultural intelligence
    $ciTestArgs = @(
        "test",
        $ProjectPath,
        "--logger", "console;verbosity=detailed",
        "--no-build",
        "--filter", "Category=Cultural"
    )
    
    $ciOutput = & dotnet @ciTestArgs 2>&1
    $ciSuccess = $LASTEXITCODE -eq 0
    
    if ($ciOutput -match "Total tests: (\d+)") {
        $culturalResults.CulturalIntelligenceTests = @{ Count = [int]$Matches[1]; Success = $ciSuccess }
        $culturalResults.TotalCulturalTests = [int]$Matches[1]
    }
    if ($ciOutput -match "Passed: (\d+)") {
        $culturalResults.PassedCulturalTests = [int]$Matches[1]
    }
    
    $culturalResults.Success = $sinhalaSuccess -and $tamilSuccess -and $ciSuccess
    $culturalResults.CulturalBreakdown = @{
        SinhalaSpecific = $culturalResults.SinhalaTests
        TamilSpecific = $culturalResults.TamilTests
        CulturalIntelligence = $culturalResults.CulturalIntelligenceTests
    }
    
    # Add to global cultural test tracking
    $Global:TestResults.CulturalTests.SinhalaTests += $culturalResults.SinhalaTests
    $Global:TestResults.CulturalTests.TamilTests += $culturalResults.TamilTests
    $Global:TestResults.CulturalTests.CulturalIntelligenceTests += $culturalResults.CulturalIntelligenceTests
    
    return $culturalResults
}

function Invoke-CoverageCollection {
    param([string]$ProjectPath, [hashtable]$ComponentInfo)
    
    Write-Log "Collecting test coverage for $(Split-Path $ProjectPath -Leaf)" "INFO"
    
    $coverageArgs = @(
        "test",
        $ProjectPath,
        "--collect", "XPlat Code Coverage",
        "--logger", "console;verbosity=minimal",
        "--no-build"
    )
    
    $coverageOutput = & dotnet @coverageArgs 2>&1
    $coverageSuccess = $LASTEXITCODE -eq 0
    
    $coverageData = @{
        Success = $coverageSuccess
        Percentage = 0
        LinesTotal = 0
        LinesCovered = 0
        BranchesTotal = 0
        BranchesCovered = 0
        ReportPath = ""
    }
    
    if ($coverageSuccess) {
        # Find coverage file
        $testResultsPath = Join-Path $ProjectPath "TestResults"
        if (Test-Path $testResultsPath) {
            $coverageFiles = Get-ChildItem -Path $testResultsPath -Include "coverage.cobertura.xml" -Recurse
            if ($coverageFiles.Count -gt 0) {
                $coverageFile = $coverageFiles[0].FullName
                $coverageData.ReportPath = $coverageFile
                
                # Parse coverage XML (simplified)
                try {
                    [xml]$coverageXml = Get-Content $coverageFile
                    if ($coverageXml.coverage) {
                        $coverageData.Percentage = [Math]::Round([double]$coverageXml.coverage."line-rate" * 100, 2)
                        Write-Log "Coverage for $(Split-Path $ProjectPath -Leaf): $($coverageData.Percentage)%" "INFO"
                    }
                }
                catch {
                    Write-Log "Failed to parse coverage report: $($_.Exception.Message)" "WARN"
                }
            }
        }
    }
    
    return $coverageData
}

function Invoke-ParallelTesting {
    param([string[]]$ComponentsToTest)
    
    if (!$ParallelExecution -or $ComponentsToTest.Count -le 1) {
        # Run sequentially
        foreach ($componentName in $ComponentsToTest) {
            $result = Invoke-ComponentTests -ComponentName $componentName
            if (!$result.Success -and !$ContinueOnFailure) {
                Write-Log "Stopping component testing due to failure in $componentName" "ERROR"
                return $false
            }
        }
        return $true
    }
    
    Write-Log "Running parallel component testing with max $MaxParallel threads" "INFO"
    
    # PowerShell parallel execution
    $jobs = @()
    $componentBatches = @()
    
    # Group components into batches
    for ($i = 0; $i -lt $ComponentsToTest.Count; $i += $MaxParallel) {
        $batch = $ComponentsToTest[$i..([Math]::Min($i + $MaxParallel - 1, $ComponentsToTest.Count - 1))]
        $componentBatches += ,$batch
    }
    
    $allSuccess = $true
    
    foreach ($batch in $componentBatches) {
        foreach ($componentName in $batch) {
            $scriptBlock = {
                param($ComponentName, $ScriptRoot, $ProjectRoot, $TestType, $TestPattern, $CollectCoverage)
                
                # Re-import utilities in job context
                $UtilitiesPath = Join-Path $ScriptRoot "automation\shared-utilities.ps1"
                if (Test-Path $UtilitiesPath) {
                    . $UtilitiesPath
                } else {
                    throw "Shared utilities not found"
                }
                
                # Import component definitions (simplified for job context)
                $Components = @{
                    "Domain" = @{ ProjectPath = "tests/LankaConnect.Domain.Tests"; RequiredCoverage = 90 }
                    "Application" = @{ ProjectPath = "tests/LankaConnect.Application.Tests"; RequiredCoverage = 85 }
                    "Infrastructure" = @{ ProjectPath = "tests/LankaConnect.Infrastructure.Tests"; RequiredCoverage = 80 }
                    "Integration" = @{ ProjectPath = "tests/LankaConnect.IntegrationTests"; RequiredCoverage = 75 }
                }
                
                # Run component tests
                return Invoke-ComponentTests -ComponentName $ComponentName
            }
            
            $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $componentName, $PSScriptRoot, $ProjectRoot, $TestType, $TestPattern, $CollectCoverage
            $jobs += $job
        }
        
        # Wait for batch to complete
        $jobs | Wait-Job | ForEach-Object {
            $result = Receive-Job $_
            Remove-Job $_
            
            if ($result -and !$result.Success) {
                $allSuccess = $false
                if (!$ContinueOnFailure) {
                    Write-Log "Parallel testing failed, stopping remaining jobs" "ERROR"
                    $jobs | Stop-Job
                    $jobs | Remove-Job
                    return $false
                }
            }
        }
        
        $jobs = @()
    }
    
    return $allSuccess
}

function Update-TestSummary {
    $Global:TestResults.Summary.TotalDuration = (Get-Date) - $Global:TestResults.StartTime
    
    foreach ($componentResult in $Global:TestResults.Components.Values) {
        foreach ($testType in $componentResult.TestResults.Keys) {
            $testResult = $componentResult.TestResults[$testType]
            $Global:TestResults.Summary.TotalTests += $testResult.TestCount
            $Global:TestResults.Summary.PassedTests += $testResult.Passed
            $Global:TestResults.Summary.FailedTests += $testResult.Failed
            $Global:TestResults.Summary.SkippedTests += if ($testResult.Skipped) { $testResult.Skipped } else { 0 }
        }
        
        if ($componentResult.Coverage.Percentage -gt 0) {
            $Global:TestResults.Summary.CoveragePercentage = [Math]::Max($Global:TestResults.Summary.CoveragePercentage, $componentResult.Coverage.Percentage)
        }
    }
}

function New-TestReport {
    $reportData = @{
        TestResults = $Global:TestResults
        Configuration = @{
            Component = $Component
            TestType = $TestType
            TestPattern = $TestPattern
            ParallelExecution = $ParallelExecution
            CollectCoverage = $CollectCoverage
        }
        Timestamp = Get-Date
        Success = ($Global:TestResults.Summary.FailedTests -eq 0)
    }
    
    $reportPath = "component-test-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    New-AutomationReport -Metrics $reportData -ReportPath $reportPath
    
    return $reportData
}

function Write-TestSummary {
    Write-ColoredOutput "`n=== COMPONENT TESTING SUMMARY ===" "Info"
    Write-ColoredOutput "Component Filter: $Component" "Info"
    Write-ColoredOutput "Test Type: $TestType" "Info"
    Write-ColoredOutput "Total Duration: $($Global:TestResults.Summary.TotalDuration.TotalSeconds)s" "Info"
    Write-ColoredOutput "Components Tested: $($Global:TestResults.Components.Count)" "Info"
    
    Write-ColoredOutput "`n--- Test Results ---" "Info"
    Write-ColoredOutput "Total Tests: $($Global:TestResults.Summary.TotalTests)" "Info"
    Write-ColoredOutput "Passed: $($Global:TestResults.Summary.PassedTests)" "Success"
    Write-ColoredOutput "Failed: $($Global:TestResults.Summary.FailedTests)" "Error"
    Write-ColoredOutput "Skipped: $($Global:TestResults.Summary.SkippedTests)" "Warning"
    
    if ($CollectCoverage) {
        Write-ColoredOutput "Max Coverage: $($Global:TestResults.Summary.CoveragePercentage)%" "Info"
    }
    
    Write-ColoredOutput "`n--- Cultural Intelligence Tests ---" "Info"
    $sinhalaCount = ($Global:TestResults.CulturalTests.SinhalaTests | Measure-Object -Property Count -Sum).Sum
    $tamilCount = ($Global:TestResults.CulturalTests.TamilTests | Measure-Object -Property Count -Sum).Sum
    $ciCount = ($Global:TestResults.CulturalTests.CulturalIntelligenceTests | Measure-Object -Property Count -Sum).Sum
    
    Write-ColoredOutput "Sinhala-specific Tests: $sinhalaCount" "Info"
    Write-ColoredOutput "Tamil-specific Tests: $tamilCount" "Info"
    Write-ColoredOutput "Cultural Intelligence Tests: $ciCount" "Info"
    
    Write-ColoredOutput "`n--- Component Details ---" "Info"
    foreach ($componentName in $Global:TestResults.Components.Keys) {
        $component = $Global:TestResults.Components[$componentName]
        $status = if ($component.Success) { "✅" } else { "❌" }
        $duration = $component.Duration.TotalSeconds
        Write-ColoredOutput "$status $componentName ($duration`s)" "Info"
        
        foreach ($testType in $component.TestResults.Keys) {
            $testResult = $component.TestResults[$testType]
            Write-ColoredOutput "  $testType`: $($testResult.Passed)/$($testResult.TestCount) passed" "Info"
        }
        
        if ($component.Coverage.Percentage -gt 0) {
            Write-ColoredOutput "  Coverage: $($component.Coverage.Percentage)%" "Info"
        }
    }
    
    Write-ColoredOutput "====================================" "Info"
}

# Main execution
try {
    Write-Log "🧪 Component Testing Automation Started" "INFO"
    
    # Initialize testing
    if (!(Initialize-ComponentTesting)) {
        Write-Log "Component testing initialization failed" "ERROR"
        exit 1
    }
    
    # Get components to test
    $componentsToTest = Get-ComponentsToTest -ComponentFilter $Component
    
    if ($componentsToTest.Count -eq 0) {
        Write-Log "No components to test" "ERROR"
        exit 1
    }
    
    Write-Log "Testing components: $($componentsToTest -join ', ')" "INFO"
    
    # Run tests (parallel or sequential)
    $testingSuccess = Invoke-ParallelTesting -ComponentsToTest $componentsToTest
    
    # Update summary and generate report
    Update-TestSummary
    $report = New-TestReport
    
    # Write summary
    Write-TestSummary
    
    if ($testingSuccess -and $Global:TestResults.Summary.FailedTests -eq 0) {
        Write-Log "🧪 Component testing completed successfully" "SUCCESS"
        exit 0
    } else {
        Write-Log "🧪 Component testing completed with failures" "ERROR"
        exit 1
    }
}
catch {
    Write-Log "Component testing automation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}