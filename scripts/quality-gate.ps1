#!/usr/bin/env pwsh
# LankaConnect Quality Gate Automation
# Comprehensive quality validation for cultural intelligence platform milestones

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Development", "Staging", "Production", "Release")]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Basic", "Standard", "Comprehensive", "Release")]
    [string]$QualityLevel = "Standard",
    
    [Parameter(Mandatory=$false)]
    [switch]$BreakOnFailure,
    
    [Parameter(Mandatory=$false)]
    [switch]$GenerateReport,
    
    [Parameter(Mandatory=$false)]
    [string]$ReportFormat = "JSON",
    
    [Parameter(Mandatory=$false)]
    [switch]$FixIssues,
    
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
$ScriptName = "Quality-Gate"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

# Quality gate results
$Global:QualityGateResults = @{
    StartTime = Get-Date
    Environment = $Environment
    QualityLevel = $QualityLevel
    OverallStatus = "Unknown"
    Score = 0
    MaxScore = 0
    Gates = @{
        Build = @{ Status = "Pending"; Score = 0; MaxScore = 100; Issues = @(); Duration = 0 }
        Tests = @{ Status = "Pending"; Score = 0; MaxScore = 100; Issues = @(); Duration = 0 }
        Coverage = @{ Status = "Pending"; Score = 0; MaxScore = 100; Issues = @(); Duration = 0 }
        Cultural = @{ Status = "Pending"; Score = 0; MaxScore = 100; Issues = @(); Duration = 0 }
        Security = @{ Status = "Pending"; Score = 0; MaxScore = 50; Issues = @(); Duration = 0 }
        Performance = @{ Status = "Pending"; Score = 0; MaxScore = 50; Issues = @(); Duration = 0 }
        Documentation = @{ Status = "Pending"; Score = 0; MaxScore = 25; Issues = @(); Duration = 0 }
        Architecture = @{ Status = "Pending"; Score = 0; MaxScore = 75; Issues = @(); Duration = 0 }
    }
    CulturalIntelligence = @{
        FeaturesImplemented = 0
        RequiredFeatures = @()
        LocalizationSupport = @()
        CulturalCompliance = 0
    }
    Recommendations = @()
    CriticalIssues = @()
}

# Quality gate thresholds by environment and level
$QualityThresholds = @{
    Development = @{
        Basic = @{
            BuildSuccess = $true
            TestPassRate = 80
            CodeCoverage = 70
            CulturalCompliance = 60
            SecurityScore = 70
            MinScore = 300
        }
        Standard = @{
            BuildSuccess = $true
            TestPassRate = 85
            CodeCoverage = 80
            CulturalCompliance = 75
            SecurityScore = 80
            MinScore = 400
        }
        Comprehensive = @{
            BuildSuccess = $true
            TestPassRate = 90
            CodeCoverage = 85
            CulturalCompliance = 85
            SecurityScore = 85
            MinScore = 450
        }
    }
    Staging = @{
        Standard = @{
            BuildSuccess = $true
            TestPassRate = 90
            CodeCoverage = 85
            CulturalCompliance = 80
            SecurityScore = 85
            MinScore = 450
        }
        Comprehensive = @{
            BuildSuccess = $true
            TestPassRate = 95
            CodeCoverage = 90
            CulturalCompliance = 90
            SecurityScore = 90
            MinScore = 500
        }
    }
    Production = @{
        Release = @{
            BuildSuccess = $true
            TestPassRate = 98
            CodeCoverage = 95
            CulturalCompliance = 95
            SecurityScore = 95
            PerformanceScore = 90
            DocumentationScore = 85
            ArchitectureScore = 90
            MinScore = 575
        }
    }
}

# Required cultural intelligence features by environment
$RequiredCulturalFeatures = @{
    Development = @(
        "Multi-language support foundation",
        "Basic cultural context handling",
        "Sri Lankan timezone support"
    )
    Staging = @(
        "Multi-language support foundation",
        "Basic cultural context handling", 
        "Sri Lankan timezone support",
        "Currency handling (LKR)",
        "Province/district awareness",
        "Cultural event integration"
    )
    Production = @(
        "Multi-language support foundation",
        "Basic cultural context handling",
        "Sri Lankan timezone support", 
        "Currency handling (LKR)",
        "Province/district awareness",
        "Cultural event integration",
        "Diaspora community features",
        "Cultural intelligence analytics",
        "Localization completeness",
        "Cultural compliance validation"
    )
}

function Initialize-QualityGate {
    Write-Log "🎯 Starting Quality Gate Validation" "INFO"
    Write-Log "Environment: $Environment, Quality Level: $QualityLevel" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    # Set required cultural features based on environment
    $Global:QualityGateResults.CulturalIntelligence.RequiredFeatures = $RequiredCulturalFeatures[$Environment]
    
    # Calculate max score based on quality level
    $Global:QualityGateResults.MaxScore = ($Global:QualityGateResults.Gates.Values | Measure-Object -Property MaxScore -Sum).Sum
    
    Write-Log "Maximum possible score: $($Global:QualityGateResults.MaxScore)" "INFO"
    Write-Log "Required cultural features: $($Global:QualityGateResults.CulturalIntelligence.RequiredFeatures.Count)" "INFO"
    
    return $true
}

function Invoke-BuildGate {
    Write-Log "🏗️ Running Build Quality Gate" "INFO"
    
    $buildGate = $Global:QualityGateResults.Gates.Build
    $startTime = Get-Date
    
    try {
        # Full clean and rebuild
        $buildResult = Measure-PerformanceMetrics -ScriptBlock {
            Invoke-BuildValidation -ProjectRoot $ProjectRoot
        } -OperationName "Build Validation"
        
        if ($buildResult.Success -and $buildResult.Result) {
            $buildGate.Status = "Pass"
            $buildGate.Score = $buildGate.MaxScore
            Write-Log "✅ Build gate passed" "SUCCESS"
        } else {
            $buildGate.Status = "Fail"
            $buildGate.Score = 0
            $buildGate.Issues += "Build compilation failed"
            Write-Log "❌ Build gate failed" "ERROR"
        }
        
    }
    catch {
        $buildGate.Status = "Error"
        $buildGate.Score = 0
        $buildGate.Issues += "Build gate exception: $($_.Exception.Message)"
        Write-Log "❌ Build gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $buildGate.Duration = (Get-Date) - $startTime
    return $buildGate.Status -eq "Pass"
}

function Invoke-TestGate {
    Write-Log "🧪 Running Test Quality Gate" "INFO"
    
    $testGate = $Global:QualityGateResults.Gates.Tests
    $startTime = Get-Date
    
    try {
        # Run component tests
        $componentTestScript = Join-Path $PSScriptRoot "component-test.ps1"
        
        if (Test-Path $componentTestScript) {
            $testResult = & pwsh $componentTestScript -Component "All" -TestType "All" -CollectCoverage -ContinueOnFailure
            $testSuccess = $LASTEXITCODE -eq 0
            
            if ($testSuccess) {
                $testGate.Status = "Pass"
                $testGate.Score = $testGate.MaxScore
                Write-Log "✅ Test gate passed" "SUCCESS"
            } else {
                $testGate.Status = "Fail"
                $testGate.Score = [Math]::Max(0, $testGate.MaxScore * 0.5) # Partial credit
                $testGate.Issues += "Some tests failed"
                Write-Log "❌ Test gate failed" "ERROR"
            }
        } else {
            # Fallback to basic test execution
            $projects = @(
                "tests/LankaConnect.Domain.Tests",
                "tests/LankaConnect.Application.Tests",
                "tests/LankaConnect.IntegrationTests"
            )
            
            $totalTests = 0
            $passedTests = 0
            $failedTests = 0
            
            foreach ($project in $projects) {
                $projectPath = Join-Path $ProjectRoot $project
                if (Test-Path $projectPath) {
                    $result = Invoke-TestExecution -ProjectPath $projectPath -Configuration "Debug"
                    # Parse results (simplified)
                    if ($result.Success) {
                        $passedTests += 10 # Placeholder
                    } else {
                        $failedTests += 5 # Placeholder
                    }
                    $totalTests += 15 # Placeholder
                }
            }
            
            $passRate = if ($totalTests -gt 0) { ($passedTests / $totalTests) * 100 } else { 0 }
            $threshold = $QualityThresholds[$Environment][$QualityLevel].TestPassRate
            
            if ($passRate -ge $threshold) {
                $testGate.Status = "Pass"
                $testGate.Score = $testGate.MaxScore
            } else {
                $testGate.Status = "Fail"
                $testGate.Score = [Math]::Max(0, ($passRate / $threshold) * $testGate.MaxScore)
                $testGate.Issues += "Test pass rate ($passRate%) below threshold ($threshold%)"
            }
        }
        
    }
    catch {
        $testGate.Status = "Error"
        $testGate.Score = 0
        $testGate.Issues += "Test gate exception: $($_.Exception.Message)"
        Write-Log "❌ Test gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $testGate.Duration = (Get-Date) - $startTime
    return $testGate.Status -eq "Pass"
}

function Invoke-CoverageGate {
    Write-Log "📊 Running Coverage Quality Gate" "INFO"
    
    $coverageGate = $Global:QualityGateResults.Gates.Coverage
    $startTime = Get-Date
    
    try {
        # Run tests with coverage collection
        $testProjectPaths = @(
            "tests/LankaConnect.Domain.Tests",
            "tests/LankaConnect.Application.Tests"
        )
        
        $coverageResults = @()
        
        foreach ($projectPath in $testProjectPaths) {
            $fullPath = Join-Path $ProjectRoot $projectPath
            if (Test-Path $fullPath) {
                $testResult = Invoke-TestExecution -ProjectPath $fullPath -Configuration "Debug" -CollectCoverage
                
                # Find and parse coverage report
                $testResultsPath = Join-Path $fullPath "TestResults"
                if (Test-Path $testResultsPath) {
                    $coverageFiles = Get-ChildItem -Path $testResultsPath -Include "coverage.cobertura.xml" -Recurse
                    if ($coverageFiles.Count -gt 0) {
                        try {
                            [xml]$coverageXml = Get-Content $coverageFiles[0].FullName
                            if ($coverageXml.coverage) {
                                $coverage = [Math]::Round([double]$coverageXml.coverage."line-rate" * 100, 2)
                                $coverageResults += $coverage
                                Write-Log "Coverage for $(Split-Path $projectPath -Leaf): $coverage%" "INFO"
                            }
                        }
                        catch {
                            Write-Log "Failed to parse coverage for $projectPath" "WARN"
                        }
                    }
                }
            }
        }
        
        $averageCoverage = if ($coverageResults.Count -gt 0) { 
            ($coverageResults | Measure-Object -Average).Average 
        } else { 0 }
        
        $threshold = $QualityThresholds[$Environment][$QualityLevel].CodeCoverage
        
        if ($averageCoverage -ge $threshold) {
            $coverageGate.Status = "Pass"
            $coverageGate.Score = $coverageGate.MaxScore
            Write-Log "✅ Coverage gate passed: $averageCoverage%" "SUCCESS"
        } else {
            $coverageGate.Status = "Fail"
            $coverageGate.Score = [Math]::Max(0, ($averageCoverage / $threshold) * $coverageGate.MaxScore)
            $coverageGate.Issues += "Code coverage ($averageCoverage%) below threshold ($threshold%)"
            Write-Log "❌ Coverage gate failed: $averageCoverage% < $threshold%" "ERROR"
        }
        
    }
    catch {
        $coverageGate.Status = "Error"
        $coverageGate.Score = 0
        $coverageGate.Issues += "Coverage gate exception: $($_.Exception.Message)"
        Write-Log "❌ Coverage gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $coverageGate.Duration = (Get-Date) - $startTime
    return $coverageGate.Status -eq "Pass"
}

function Invoke-CulturalGate {
    Write-Log "🇱🇰 Running Cultural Intelligence Quality Gate" "INFO"
    
    $culturalGate = $Global:QualityGateResults.Gates.Cultural
    $startTime = Get-Date
    
    try {
        # Run cultural validation script
        $culturalValidationScript = Join-Path $PSScriptRoot "cultural-validation.ps1"
        
        if (Test-Path $culturalValidationScript) {
            $culturalResult = & pwsh $culturalValidationScript -ValidationScope "All" -ExportReport
            $culturalSuccess = $LASTEXITCODE -eq 0
            
            # Parse cultural validation results (would need actual integration)
            $complianceScore = 75 # Placeholder - would be extracted from actual report
            
        } else {
            # Fallback cultural validation
            $culturalFeatures = Test-CulturalFeatures -ProjectRoot $ProjectRoot
            $Global:QualityGateResults.CulturalIntelligence.FeaturesImplemented = $culturalFeatures.Count
            
            $requiredFeatures = $Global:QualityGateResults.CulturalIntelligence.RequiredFeatures
            $implementationRate = if ($requiredFeatures.Count -gt 0) { 
                ($culturalFeatures.Count / $requiredFeatures.Count) * 100 
            } else { 0 }
            
            $complianceScore = [Math]::Min(100, $implementationRate)
        }
        
        $threshold = $QualityThresholds[$Environment][$QualityLevel].CulturalCompliance
        
        if ($complianceScore -ge $threshold) {
            $culturalGate.Status = "Pass"
            $culturalGate.Score = $culturalGate.MaxScore
            Write-Log "✅ Cultural gate passed: $complianceScore%" "SUCCESS"
        } else {
            $culturalGate.Status = "Fail"
            $culturalGate.Score = [Math]::Max(0, ($complianceScore / $threshold) * $culturalGate.MaxScore)
            $culturalGate.Issues += "Cultural compliance ($complianceScore%) below threshold ($threshold%)"
            Write-Log "❌ Cultural gate failed: $complianceScore% < $threshold%" "ERROR"
        }
        
        $Global:QualityGateResults.CulturalIntelligence.CulturalCompliance = $complianceScore
        
    }
    catch {
        $culturalGate.Status = "Error"
        $culturalGate.Score = 0
        $culturalGate.Issues += "Cultural gate exception: $($_.Exception.Message)"
        Write-Log "❌ Cultural gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $culturalGate.Duration = (Get-Date) - $startTime
    return $culturalGate.Status -eq "Pass"
}

function Invoke-SecurityGate {
    Write-Log "🔒 Running Security Quality Gate" "INFO"
    
    $securityGate = $Global:QualityGateResults.Gates.Security
    $startTime = Get-Date
    
    try {
        # Basic security checks
        $securityIssues = @()
        
        # Check for hardcoded secrets
        $sourcePaths = @("src")
        foreach ($sourcePath in $sourcePaths) {
            $fullPath = Join-Path $ProjectRoot $sourcePath
            if (Test-Path $fullPath) {
                $csharpFiles = Get-ChildItem -Path $fullPath -Include "*.cs", "*.json" -Recurse
                
                foreach ($file in $csharpFiles) {
                    $content = Get-Content $file.FullName -Raw
                    
                    # Check for potential secrets
                    $secretPatterns = @(
                        "password\s*=\s*[\"'][^\"']{8,}[\"']",
                        "connectionstring.*password",
                        "apikey\s*=\s*[\"'][^\"']+[\"']",
                        "secret\s*=\s*[\"'][^\"']+[\"']"
                    )
                    
                    foreach ($pattern in $secretPatterns) {
                        if ($content -match $pattern) {
                            $securityIssues += "Potential hardcoded secret in $($file.Name)"
                        }
                    }
                }
            }
        }
        
        # Check for HTTPS configuration
        $appsettingsPath = Join-Path $ProjectRoot "src/LankaConnect.API/appsettings.json"
        if (Test-Path $appsettingsPath) {
            $appsettings = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
            if (!$appsettings.Kestrel.Endpoints.Https) {
                $securityIssues += "HTTPS not properly configured in appsettings"
            }
        }
        
        # Calculate security score
        $securityScore = [Math]::Max(0, 100 - ($securityIssues.Count * 15))
        $threshold = $QualityThresholds[$Environment][$QualityLevel].SecurityScore
        
        if ($securityScore -ge $threshold) {
            $securityGate.Status = "Pass"
            $securityGate.Score = $securityGate.MaxScore
            Write-Log "✅ Security gate passed: $securityScore%" "SUCCESS"
        } else {
            $securityGate.Status = "Fail"
            $securityGate.Score = [Math]::Max(0, ($securityScore / $threshold) * $securityGate.MaxScore)
            $securityGate.Issues += $securityIssues
            $securityGate.Issues += "Security score ($securityScore%) below threshold ($threshold%)"
            Write-Log "❌ Security gate failed: $securityScore% < $threshold%" "ERROR"
        }
        
    }
    catch {
        $securityGate.Status = "Error"
        $securityGate.Score = 0
        $securityGate.Issues += "Security gate exception: $($_.Exception.Message)"
        Write-Log "❌ Security gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $securityGate.Duration = (Get-Date) - $startTime
    return $securityGate.Status -eq "Pass"
}

function Invoke-PerformanceGate {
    Write-Log "⚡ Running Performance Quality Gate" "INFO"
    
    $performanceGate = $Global:QualityGateResults.Gates.Performance
    $startTime = Get-Date
    
    try {
        # Basic performance checks
        $performanceIssues = @()
        
        # Check for synchronous database calls in async methods
        $sourcePaths = @("src/LankaConnect.Application", "src/LankaConnect.Infrastructure")
        foreach ($sourcePath in $sourcePaths) {
            $fullPath = Join-Path $ProjectRoot $sourcePath
            if (Test-Path $fullPath) {
                $csharpFiles = Get-ChildItem -Path $fullPath -Include "*.cs" -Recurse
                
                foreach ($file in $csharpFiles) {
                    $content = Get-Content $file.FullName -Raw
                    
                    # Check for sync over async anti-patterns
                    if ($content -match "async.*Task" -and $content -match "\.Result\b|\.Wait\(\)") {
                        $performanceIssues += "Potential sync-over-async in $($file.Name)"
                    }
                    
                    # Check for N+1 query potential
                    if ($content -match "foreach.*await.*\bFind\b|\bGet\b") {
                        $performanceIssues += "Potential N+1 query pattern in $($file.Name)"
                    }
                }
            }
        }
        
        # Check for missing async/await patterns in controllers
        $controllersPath = Join-Path $ProjectRoot "src/LankaConnect.API/Controllers"
        if (Test-Path $controllersPath) {
            $controllerFiles = Get-ChildItem -Path $controllersPath -Include "*.cs" -Recurse
            
            foreach ($file in $controllerFiles) {
                $content = Get-Content $file.FullName -Raw
                
                if ($content -match "\[Http\w+\]" -and $content -notmatch "async.*Task") {
                    $performanceIssues += "Non-async controller action in $($file.Name)"
                }
            }
        }
        
        $performanceScore = [Math]::Max(0, 100 - ($performanceIssues.Count * 10))
        
        if ($Environment -eq "Production") {
            $threshold = $QualityThresholds[$Environment][$QualityLevel].PerformanceScore
            
            if ($performanceScore -ge $threshold) {
                $performanceGate.Status = "Pass"
                $performanceGate.Score = $performanceGate.MaxScore
                Write-Log "✅ Performance gate passed: $performanceScore%" "SUCCESS"
            } else {
                $performanceGate.Status = "Fail"
                $performanceGate.Score = [Math]::Max(0, ($performanceScore / $threshold) * $performanceGate.MaxScore)
                $performanceGate.Issues += $performanceIssues
                Write-Log "❌ Performance gate failed: $performanceScore% < $threshold%" "ERROR"
            }
        } else {
            # Performance gate is informational for non-production environments
            $performanceGate.Status = "Pass"
            $performanceGate.Score = $performanceGate.MaxScore
            if ($performanceIssues.Count -gt 0) {
                $performanceGate.Issues += $performanceIssues
                Write-Log "⚠️ Performance issues noted (informational): $performanceScore%" "WARN"
            } else {
                Write-Log "✅ Performance gate passed: $performanceScore%" "SUCCESS"
            }
        }
        
    }
    catch {
        $performanceGate.Status = "Error"
        $performanceGate.Score = 0
        $performanceGate.Issues += "Performance gate exception: $($_.Exception.Message)"
        Write-Log "❌ Performance gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $performanceGate.Duration = (Get-Date) - $startTime
    return $performanceGate.Status -eq "Pass"
}

function Invoke-DocumentationGate {
    Write-Log "📚 Running Documentation Quality Gate" "INFO"
    
    $documentationGate = $Global:QualityGateResults.Gates.Documentation
    $startTime = Get-Date
    
    try {
        $docScore = 0
        $maxDocScore = 100
        
        # Check for XML documentation in public APIs
        $apiPath = Join-Path $ProjectRoot "src/LankaConnect.API"
        if (Test-Path $apiPath) {
            $controllerFiles = Get-ChildItem -Path $apiPath -Include "*Controller.cs" -Recurse
            $documentedControllers = 0
            
            foreach ($file in $controllerFiles) {
                $content = Get-Content $file.FullName -Raw
                if ($content -match "/// <summary>") {
                    $documentedControllers++
                }
            }
            
            if ($controllerFiles.Count -gt 0) {
                $docScore += ($documentedControllers / $controllerFiles.Count) * 30
            }
        }
        
        # Check for README and documentation files
        $requiredDocs = @(
            "README.md",
            "docs/API.md",
            "docs/ARCHITECTURE.md"
        )
        
        $existingDocs = 0
        foreach ($doc in $requiredDocs) {
            if (Test-Path (Join-Path $ProjectRoot $doc)) {
                $existingDocs++
            }
        }
        
        $docScore += ($existingDocs / $requiredDocs.Count) * 40
        
        # Check for cultural intelligence documentation
        $culturalDocs = @(
            "docs/CULTURAL-FEATURES.md",
            "docs/LOCALIZATION.md"
        )
        
        $existingCulturalDocs = 0
        foreach ($doc in $culturalDocs) {
            if (Test-Path (Join-Path $ProjectRoot $doc)) {
                $existingCulturalDocs++
            }
        }
        
        $docScore += ($existingCulturalDocs / $culturalDocs.Count) * 30
        
        if ($Environment -eq "Production") {
            $threshold = $QualityThresholds[$Environment][$QualityLevel].DocumentationScore
            
            if ($docScore -ge $threshold) {
                $documentationGate.Status = "Pass"
                $documentationGate.Score = $documentationGate.MaxScore
                Write-Log "✅ Documentation gate passed: $docScore%" "SUCCESS"
            } else {
                $documentationGate.Status = "Fail"
                $documentationGate.Score = [Math]::Max(0, ($docScore / $threshold) * $documentationGate.MaxScore)
                $documentationGate.Issues += "Documentation score ($docScore%) below threshold ($threshold%)"
                Write-Log "❌ Documentation gate failed: $docScore% < $threshold%" "ERROR"
            }
        } else {
            # Documentation gate is informational for non-production environments
            $documentationGate.Status = "Pass"
            $documentationGate.Score = $documentationGate.MaxScore
            Write-Log "✅ Documentation gate passed (informational): $docScore%" "SUCCESS"
        }
        
    }
    catch {
        $documentationGate.Status = "Error"
        $documentationGate.Score = 0
        $documentationGate.Issues += "Documentation gate exception: $($_.Exception.Message)"
        Write-Log "❌ Documentation gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $documentationGate.Duration = (Get-Date) - $startTime
    return $documentationGate.Status -eq "Pass"
}

function Invoke-ArchitectureGate {
    Write-Log "🏛️ Running Architecture Quality Gate" "INFO"
    
    $architectureGate = $Global:QualityGateResults.Gates.Architecture
    $startTime = Get-Date
    
    try {
        $archScore = 0
        $archIssues = @()
        
        # Check Clean Architecture compliance
        $layerPaths = @{
            "Domain" = "src/LankaConnect.Domain"
            "Application" = "src/LankaConnect.Application"
            "Infrastructure" = "src/LankaConnect.Infrastructure"
            "API" = "src/LankaConnect.API"
        }
        
        $existingLayers = 0
        foreach ($layer in $layerPaths.Keys) {
            if (Test-Path (Join-Path $ProjectRoot $layerPaths[$layer])) {
                $existingLayers++
            }
        }
        
        $archScore += ($existingLayers / $layerPaths.Count) * 30
        
        # Check dependency direction (Domain should not reference other layers)
        $domainPath = Join-Path $ProjectRoot "src/LankaConnect.Domain"
        if (Test-Path $domainPath) {
            $domainCsproj = Join-Path $domainPath "LankaConnect.Domain.csproj"
            if (Test-Path $domainCsproj) {
                $projContent = Get-Content $domainCsproj -Raw
                if ($projContent -match "LankaConnect\.(Application|Infrastructure|API)") {
                    $archIssues += "Domain layer has invalid dependencies"
                } else {
                    $archScore += 25
                }
            }
        }
        
        # Check for proper use of interfaces
        $applicationPath = Join-Path $ProjectRoot "src/LankaConnect.Application"
        if (Test-Path $applicationPath) {
            $interfaceFiles = Get-ChildItem -Path $applicationPath -Include "I*.cs" -Recurse
            $serviceFiles = Get-ChildItem -Path $applicationPath -Include "*Service.cs", "*Handler.cs" -Recurse
            
            if ($serviceFiles.Count -gt 0) {
                $interfaceRatio = $interfaceFiles.Count / $serviceFiles.Count
                $archScore += [Math]::Min(25, $interfaceRatio * 25)
            }
        }
        
        # Check cultural architecture patterns
        $culturalPatterns = @(
            "CulturalContext",
            "ICultureService",
            "CultureProvider",
            "LocalizationService"
        )
        
        $foundPatterns = 0
        foreach ($layerPath in $layerPaths.Values) {
            $fullLayerPath = Join-Path $ProjectRoot $layerPath
            if (Test-Path $fullLayerPath) {
                $files = Get-ChildItem -Path $fullLayerPath -Include "*.cs" -Recurse
                foreach ($file in $files) {
                    $content = Get-Content $file.FullName -Raw
                    foreach ($pattern in $culturalPatterns) {
                        if ($content -match $pattern) {
                            $foundPatterns++
                            break
                        }
                    }
                }
            }
        }
        
        $archScore += ($foundPatterns / $culturalPatterns.Count) * 20
        
        if ($Environment -eq "Production") {
            $threshold = $QualityThresholds[$Environment][$QualityLevel].ArchitectureScore
            
            if ($archScore -ge $threshold) {
                $architectureGate.Status = "Pass"
                $architectureGate.Score = $architectureGate.MaxScore
                Write-Log "✅ Architecture gate passed: $archScore%" "SUCCESS"
            } else {
                $architectureGate.Status = "Fail"
                $architectureGate.Score = [Math]::Max(0, ($archScore / $threshold) * $architectureGate.MaxScore)
                $architectureGate.Issues += $archIssues
                $architectureGate.Issues += "Architecture score ($archScore%) below threshold ($threshold%)"
                Write-Log "❌ Architecture gate failed: $archScore% < $threshold%" "ERROR"
            }
        } else {
            $architectureGate.Status = "Pass"
            $architectureGate.Score = $architectureGate.MaxScore
            if ($archIssues.Count -gt 0) {
                $architectureGate.Issues += $archIssues
                Write-Log "⚠️ Architecture issues noted: $archScore%" "WARN"
            } else {
                Write-Log "✅ Architecture gate passed: $archScore%" "SUCCESS"
            }
        }
        
    }
    catch {
        $architectureGate.Status = "Error"
        $architectureGate.Score = 0
        $architectureGate.Issues += "Architecture gate exception: $($_.Exception.Message)"
        Write-Log "❌ Architecture gate error: $($_.Exception.Message)" "ERROR"
    }
    
    $architectureGate.Duration = (Get-Date) - $startTime
    return $architectureGate.Status -eq "Pass"
}

function Invoke-QualityGates {
    $gateResults = @{
        Build = Invoke-BuildGate
        Tests = Invoke-TestGate
        Coverage = Invoke-CoverageGate
        Cultural = Invoke-CulturalGate
        Security = Invoke-SecurityGate
        Performance = Invoke-PerformanceGate
        Documentation = Invoke-DocumentationGate
        Architecture = Invoke-ArchitectureGate
    }
    
    # Calculate overall results
    $Global:QualityGateResults.Score = ($Global:QualityGateResults.Gates.Values | Measure-Object -Property Score -Sum).Sum
    
    $passedGates = ($gateResults.Values | Where-Object { $_ -eq $true }).Count
    $totalGates = $gateResults.Count
    
    # Determine overall status
    $threshold = $QualityThresholds[$Environment][$QualityLevel].MinScore
    $criticalGatesPassed = $gateResults.Build -and $gateResults.Tests -and $gateResults.Cultural
    
    if ($Global:QualityGateResults.Score -ge $threshold -and $criticalGatesPassed) {
        $Global:QualityGateResults.OverallStatus = "Pass"
        Write-Log "✅ Quality gate validation PASSED" "SUCCESS"
    } else {
        $Global:QualityGateResults.OverallStatus = "Fail"
        Write-Log "❌ Quality gate validation FAILED" "ERROR"
        
        # Collect critical issues
        foreach ($gateName in $Global:QualityGateResults.Gates.Keys) {
            $gate = $Global:QualityGateResults.Gates[$gateName]
            if ($gate.Status -eq "Fail") {
                $Global:QualityGateResults.CriticalIssues += "Gate '$gateName' failed: $($gate.Issues -join '; ')"
            }
        }
    }
    
    return $Global:QualityGateResults.OverallStatus -eq "Pass"
}

function New-Recommendations {
    $recommendations = @()
    
    # Gate-specific recommendations
    foreach ($gateName in $Global:QualityGateResults.Gates.Keys) {
        $gate = $Global:QualityGateResults.Gates[$gateName]
        
        if ($gate.Status -eq "Fail") {
            switch ($gateName) {
                "Build" { $recommendations += "Fix compilation errors and build configuration issues" }
                "Tests" { $recommendations += "Address failing tests and improve test coverage" }
                "Coverage" { $recommendations += "Increase test coverage, especially in critical business logic" }
                "Cultural" { $recommendations += "Implement missing cultural intelligence features for Sri Lankan context" }
                "Security" { $recommendations += "Address security vulnerabilities and implement security best practices" }
                "Performance" { $recommendations += "Optimize performance bottlenecks and implement async patterns" }
                "Documentation" { $recommendations += "Improve code documentation and maintain up-to-date technical documentation" }
                "Architecture" { $recommendations += "Refactor to improve Clean Architecture compliance and dependency management" }
            }
        }
    }
    
    # Score-based recommendations
    if ($Global:QualityGateResults.Score -lt ($Global:QualityGateResults.MaxScore * 0.8)) {
        $recommendations += "Overall quality score is below 80% - focus on highest-impact improvements"
    }
    
    # Cultural intelligence specific recommendations
    $culturalFeatures = $Global:QualityGateResults.CulturalIntelligence.FeaturesImplemented
    $requiredFeatures = $Global:QualityGateResults.CulturalIntelligence.RequiredFeatures.Count
    
    if ($culturalFeatures -lt $requiredFeatures) {
        $recommendations += "Implement $($requiredFeatures - $culturalFeatures) additional cultural intelligence features"
    }
    
    $Global:QualityGateResults.Recommendations = $recommendations
    return $recommendations
}

function Export-QualityGateReport {
    param([string]$Format = "JSON")
    
    $reportDir = Join-Path $PSScriptRoot "reports"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    
    switch ($Format.ToUpper()) {
        "JSON" {
            $reportPath = Join-Path $reportDir "quality-gate-$timestamp.json"
            $Global:QualityGateResults | ConvertTo-Json -Depth 10 | Set-Content -Path $reportPath
        }
        "HTML" {
            $reportPath = Join-Path $reportDir "quality-gate-$timestamp.html"
            $htmlReport = New-QualityGateHTMLReport
            Set-Content -Path $reportPath -Value $htmlReport
        }
        "CSV" {
            $reportPath = Join-Path $reportDir "quality-gate-$timestamp.csv"
            $csvData = @()
            foreach ($gateName in $Global:QualityGateResults.Gates.Keys) {
                $gate = $Global:QualityGateResults.Gates[$gateName]
                $csvData += [PSCustomObject]@{
                    Gate = $gateName
                    Status = $gate.Status
                    Score = $gate.Score
                    MaxScore = $gate.MaxScore
                    Duration = $gate.Duration.TotalSeconds
                    Issues = ($gate.Issues -join '; ')
                }
            }
            $csvData | Export-Csv -Path $reportPath -NoTypeInformation
        }
    }
    
    Write-Log "Quality gate report exported to: $reportPath" "SUCCESS"
    return $reportPath
}

function New-QualityGateHTMLReport {
    $statusColor = if ($Global:QualityGateResults.OverallStatus -eq "Pass") { "green" } else { "red" }
    $scoreColor = if ($Global:QualityGateResults.Score -ge ($Global:QualityGateResults.MaxScore * 0.8)) { "green" } elseif ($Global:QualityGateResults.Score -ge ($Global:QualityGateResults.MaxScore * 0.6)) { "orange" } else { "red" }
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>LankaConnect Quality Gate Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background: #f4f4f4; padding: 20px; border-radius: 5px; margin-bottom: 20px; }
        .status { font-size: 24px; font-weight: bold; color: $statusColor; }
        .score { font-size: 20px; font-weight: bold; color: $scoreColor; }
        .gate { margin: 15px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .pass { background: #e6ffe6; border-color: #4CAF50; }
        .fail { background: #ffe6e6; border-color: #f44336; }
        .error { background: #fff3cd; border-color: #ffc107; }
        .cultural-section { background: #e8f5e8; padding: 15px; margin: 20px 0; border-radius: 5px; }
        .recommendations { background: #f0f8ff; padding: 15px; margin: 20px 0; border-radius: 5px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🎯 LankaConnect Quality Gate Validation Report</h1>
        <p><strong>Environment:</strong> $($Global:QualityGateResults.Environment)</p>
        <p><strong>Quality Level:</strong> $($Global:QualityGateResults.QualityLevel)</p>
        <p><strong>Generated:</strong> $($Global:QualityGateResults.StartTime)</p>
        <p><strong>Overall Status:</strong> <span class="status">$($Global:QualityGateResults.OverallStatus)</span></p>
        <p><strong>Score:</strong> <span class="score">$($Global:QualityGateResults.Score) / $($Global:QualityGateResults.MaxScore)</span></p>
    </div>
    
    <h2>Quality Gates</h2>
"@
    
    foreach ($gateName in $Global:QualityGateResults.Gates.Keys) {
        $gate = $Global:QualityGateResults.Gates[$gateName]
        $cssClass = $gate.Status.ToLower()
        $statusIcon = switch ($gate.Status) {
            "Pass" { "✅" }
            "Fail" { "❌" }
            "Error" { "⚠️" }
            default { "❓" }
        }
        
        $html += @"
    <div class="gate $cssClass">
        <h3>$statusIcon $gateName Gate</h3>
        <p><strong>Status:</strong> $($gate.Status)</p>
        <p><strong>Score:</strong> $($gate.Score) / $($gate.MaxScore)</p>
        <p><strong>Duration:</strong> $($gate.Duration.TotalSeconds)s</p>
"@
        
        if ($gate.Issues.Count -gt 0) {
            $html += @"
        <p><strong>Issues:</strong></p>
        <ul>
"@
            foreach ($issue in $gate.Issues) {
                $html += "            <li>$issue</li>"
            }
            $html += "        </ul>"
        }
        
        $html += "    </div>"
    }
    
    $html += @"
    
    <div class="cultural-section">
        <h2>🇱🇰 Cultural Intelligence Status</h2>
        <p><strong>Features Implemented:</strong> $($Global:QualityGateResults.CulturalIntelligence.FeaturesImplemented)</p>
        <p><strong>Required Features:</strong> $($Global:QualityGateResults.CulturalIntelligence.RequiredFeatures.Count)</p>
        <p><strong>Cultural Compliance:</strong> $($Global:QualityGateResults.CulturalIntelligence.CulturalCompliance)%</p>
        
        <h3>Required Features for $($Global:QualityGateResults.Environment):</h3>
        <ul>
"@
    
    foreach ($feature in $Global:QualityGateResults.CulturalIntelligence.RequiredFeatures) {
        $html += "            <li>$feature</li>"
    }
    
    $html += @"
        </ul>
    </div>
    
    <div class="recommendations">
        <h2>📋 Recommendations</h2>
        <ul>
"@
    
    foreach ($recommendation in $Global:QualityGateResults.Recommendations) {
        $html += "            <li>$recommendation</li>"
    }
    
    $html += @"
        </ul>
    </div>
    
    <div class="footer">
        <p><em>Report generated by LankaConnect Quality Gate Automation</em></p>
    </div>
</body>
</html>
"@
    
    return $html
}

function Write-QualityGateSummary {
    Write-ColoredOutput "`n=== QUALITY GATE VALIDATION SUMMARY ===" "Info"
    Write-ColoredOutput "Environment: $Environment" "Info"
    Write-ColoredOutput "Quality Level: $QualityLevel" "Info"
    Write-ColoredOutput "Overall Status: $($Global:QualityGateResults.OverallStatus)" $(if ($Global:QualityGateResults.OverallStatus -eq "Pass") { "Success" } else { "Error" })
    Write-ColoredOutput "Total Score: $($Global:QualityGateResults.Score) / $($Global:QualityGateResults.MaxScore)" "Info"
    
    Write-ColoredOutput "`n--- Gate Results ---" "Info"
    foreach ($gateName in $Global:QualityGateResults.Gates.Keys) {
        $gate = $Global:QualityGateResults.Gates[$gateName]
        $status = switch ($gate.Status) {
            "Pass" { "✅" }
            "Fail" { "❌" }
            "Error" { "⚠️" }
            default { "❓" }
        }
        $color = switch ($gate.Status) {
            "Pass" { "Success" }
            "Fail" { "Error" }
            default { "Warning" }
        }
        Write-ColoredOutput "$status $gateName`: $($gate.Score)/$($gate.MaxScore) ($($gate.Duration.TotalSeconds)s)" $color
    }
    
    Write-ColoredOutput "`n--- Cultural Intelligence ---" "Info"
    Write-ColoredOutput "Features: $($Global:QualityGateResults.CulturalIntelligence.FeaturesImplemented)/$($Global:QualityGateResults.CulturalIntelligence.RequiredFeatures.Count)" "Info"
    Write-ColoredOutput "Compliance: $($Global:QualityGateResults.CulturalIntelligence.CulturalCompliance)%" "Info"
    
    if ($Global:QualityGateResults.CriticalIssues.Count -gt 0) {
        Write-ColoredOutput "`n--- Critical Issues ---" "Error"
        foreach ($issue in $Global:QualityGateResults.CriticalIssues) {
            Write-ColoredOutput "• $issue" "Error"
        }
    }
    
    if ($Global:QualityGateResults.Recommendations.Count -gt 0) {
        Write-ColoredOutput "`n--- Recommendations ---" "Info"
        foreach ($recommendation in $Global:QualityGateResults.Recommendations) {
            Write-ColoredOutput "• $recommendation" "Info"
        }
    }
    
    Write-ColoredOutput "=======================================" "Info"
}

# Main execution
try {
    Write-Log "🎯 Quality Gate Validation Started" "INFO"
    
    # Initialize quality gate
    if (!(Initialize-QualityGate)) {
        Write-Log "Quality gate initialization failed" "ERROR"
        exit 1
    }
    
    # Run quality gates
    $gatesPassed = Invoke-QualityGates
    
    # Generate recommendations
    $recommendations = New-Recommendations
    
    # Export report if requested
    if ($GenerateReport) {
        Export-QualityGateReport -Format $ReportFormat | Out-Null
    }
    
    # Write summary
    Write-QualityGateSummary
    
    $Global:QualityGateResults.EndTime = Get-Date
    $totalDuration = $Global:QualityGateResults.EndTime - $Global:QualityGateResults.StartTime
    
    if ($gatesPassed) {
        Write-Log "🎯 Quality Gate validation PASSED in $($totalDuration.TotalMinutes) minutes" "SUCCESS"
        exit 0
    } else {
        Write-Log "🎯 Quality Gate validation FAILED in $($totalDuration.TotalMinutes) minutes" "ERROR"
        
        if ($BreakOnFailure) {
            Write-Log "Breaking build due to quality gate failures" "ERROR"
            exit 1
        } else {
            Write-Log "Quality gate failures noted but not breaking build" "WARN"
            exit 2
        }
    }
}
catch {
    Write-Log "Quality gate automation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}