#!/usr/bin/env pwsh
# LankaConnect Incremental Build Validation
# Provides fast incremental builds with error detection and cultural intelligence validation

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipTests,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipCulturalValidation,
    
    [Parameter(Mandatory=$false)]
    [switch]$CleanBuild,
    
    [Parameter(Mandatory=$false)]
    [switch]$Parallel,
    
    [Parameter(Mandatory=$false)]
    [int]$MaxCpuCount = 0,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose,
    
    [Parameter(Mandatory=$false)]
    [switch]$ContinuousIntegration
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
$ScriptName = "Incremental-Build"
$ProjectRoot = Split-Path $PSScriptRoot -Parent
$StartTime = Get-Date

# Build tracking
$Global:BuildMetrics = @{
    StartTime = $StartTime
    Projects = @{}
    Errors = @()
    Warnings = @()
    CulturalValidations = @{}
    TotalDuration = 0
}

function Initialize-IncrementalBuild {
    Write-Log "🏗️ Starting incremental build validation" "INFO"
    Write-Log "Configuration: $Configuration" "INFO"
    Write-Log "Parallel: $Parallel, MaxCpuCount: $MaxCpuCount" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    # Check for .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>&1
        Write-Log "Using .NET SDK version: $dotnetVersion" "INFO"
    }
    catch {
        Write-Log ".NET SDK not found or not accessible" "ERROR"
        return $false
    }
    
    return $true
}

function Get-ProjectDependencies {
    Write-Log "Analyzing project dependencies for incremental build" "INFO"
    
    $projects = @{
        "Domain" = @{
            Path = "src/LankaConnect.Domain/LankaConnect.Domain.csproj"
            Dependencies = @()
            TestPath = "tests/LankaConnect.Domain.Tests/LankaConnect.Domain.Tests.csproj"
        }
        "Application" = @{
            Path = "src/LankaConnect.Application/LankaConnect.Application.csproj"
            Dependencies = @("Domain")
            TestPath = "tests/LankaConnect.Application.Tests/LankaConnect.Application.Tests.csproj"
        }
        "Infrastructure" = @{
            Path = "src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj"
            Dependencies = @("Domain", "Application")
            TestPath = "tests/LankaConnect.Infrastructure.Tests/LankaConnect.Infrastructure.Tests.csproj"
        }
        "API" = @{
            Path = "src/LankaConnect.API/LankaConnect.API.csproj"
            Dependencies = @("Domain", "Application", "Infrastructure")
            TestPath = "tests/LankaConnect.IntegrationTests/LankaConnect.IntegrationTests.csproj"
        }
    }
    
    return $projects
}

function Test-ProjectChanges {
    param([hashtable]$Projects)
    
    Write-Log "Detecting changed projects for incremental build" "INFO"
    
    $changedProjects = @()
    $lastBuildFile = Join-Path $ProjectRoot ".incremental-build-cache"
    $lastBuildTime = Get-Date "1970-01-01"
    
    if (Test-Path $lastBuildFile) {
        $lastBuildTime = Get-Date (Get-Content $lastBuildFile -Raw)
        Write-Log "Last successful build: $lastBuildTime" "INFO"
    } else {
        Write-Log "No previous build cache found - building all projects" "INFO"
        return @($Projects.Keys)
    }
    
    foreach ($projectName in $Projects.Keys) {
        $project = $Projects[$projectName]
        $projectPath = Join-Path $ProjectRoot $project.Path
        $projectDir = Split-Path $projectPath -Parent
        
        if (Test-Path $projectDir) {
            $sourceFiles = Get-ChildItem -Path $projectDir -Include "*.cs", "*.csproj" -Recurse
            $hasChanges = $false
            
            foreach ($file in $sourceFiles) {
                if ($file.LastWriteTime -gt $lastBuildTime) {
                    $hasChanges = $true
                    break
                }
            }
            
            if ($hasChanges) {
                $changedProjects += $projectName
                Write-Log "Project $projectName has changes since last build" "INFO"
            }
        }
    }
    
    if ($changedProjects.Count -eq 0) {
        Write-Log "No projects have changes - incremental build not needed" "SUCCESS"
    }
    
    return $changedProjects
}

function Get-BuildOrder {
    param([hashtable]$Projects, [string[]]$ChangedProjects)
    
    # Topological sort based on dependencies
    $buildOrder = @()
    $visited = @{}
    $visiting = @{}
    
    function Visit-Project {
        param([string]$ProjectName)
        
        if ($visiting.ContainsKey($ProjectName)) {
            throw "Circular dependency detected involving $ProjectName"
        }
        
        if ($visited.ContainsKey($ProjectName)) {
            return
        }
        
        $visiting[$ProjectName] = $true
        
        foreach ($dependency in $Projects[$ProjectName].Dependencies) {
            if ($ChangedProjects -contains $dependency -or $ChangedProjects -contains $ProjectName) {
                Visit-Project $dependency
            }
        }
        
        $visiting.Remove($ProjectName)
        $visited[$ProjectName] = $true
        
        if ($ChangedProjects -contains $ProjectName) {
            $script:buildOrder += $ProjectName
        }
    }
    
    foreach ($projectName in $ChangedProjects) {
        Visit-Project $projectName
    }
    
    # Also include dependent projects if their dependencies changed
    foreach ($projectName in $Projects.Keys) {
        foreach ($dependency in $Projects[$projectName].Dependencies) {
            if ($ChangedProjects -contains $dependency -and $buildOrder -notcontains $projectName) {
                $buildOrder += $projectName
                break
            }
        }
    }
    
    Write-Log "Build order: $($buildOrder -join ' -> ')" "INFO"
    return $buildOrder
}

function Invoke-ProjectBuild {
    param([string]$ProjectName, [hashtable]$ProjectInfo)
    
    $projectPath = Join-Path $ProjectRoot $ProjectInfo.Path
    
    if (!(Test-Path $projectPath)) {
        Write-Log "Project file not found: $projectPath" "ERROR"
        return $false
    }
    
    Write-Log "Building project: $ProjectName" "INFO"
    
    $buildArgs = @(
        "build",
        $projectPath,
        "--configuration", $Configuration,
        "--no-restore"
    )
    
    if ($Parallel -and $MaxCpuCount -gt 0) {
        $buildArgs += "--maxcpucount:$MaxCpuCount"
    }
    
    if ($Verbose) {
        $buildArgs += "--verbosity", "detailed"
    }
    
    $projectMetrics = @{
        StartTime = Get-Date
        Success = $false
        Duration = 0
        Errors = @()
        Warnings = @()
        CulturalValidations = @()
    }
    
    try {
        $buildOutput = & dotnet @buildArgs 2>&1
        $buildSuccess = $LASTEXITCODE -eq 0
        
        $projectMetrics.Success = $buildSuccess
        $projectMetrics.Duration = (Get-Date) - $projectMetrics.StartTime
        
        if ($buildSuccess) {
            Write-Log "✅ Project $ProjectName built successfully in $($projectMetrics.Duration.TotalSeconds)s" "SUCCESS"
        } else {
            Write-Log "❌ Project $ProjectName build failed" "ERROR"
            $projectMetrics.Errors += $buildOutput
            $Global:BuildMetrics.Errors += "Project ${ProjectName}: $buildOutput"
        }
        
        # Parse build output for warnings
        $warnings = $buildOutput | Where-Object { $_ -match "warning" }
        if ($warnings) {
            $projectMetrics.Warnings += $warnings
            $Global:BuildMetrics.Warnings += $warnings
            Write-Log "⚠️ Project $ProjectName has $($warnings.Count) warnings" "WARN"
        }
        
        $Global:BuildMetrics.Projects[$ProjectName] = $projectMetrics
        return $buildSuccess
    }
    catch {
        Write-Log "Exception during build of $ProjectName`: $($_.Exception.Message)" "ERROR"
        $projectMetrics.Errors += $_.Exception.Message
        $Global:BuildMetrics.Projects[$ProjectName] = $projectMetrics
        return $false
    }
}

function Invoke-ProjectTests {
    param([string]$ProjectName, [hashtable]$ProjectInfo)
    
    if ($SkipTests) {
        Write-Log "Skipping tests for $ProjectName" "INFO"
        return $true
    }
    
    $testProjectPath = Join-Path $ProjectRoot $ProjectInfo.TestPath
    
    if (!(Test-Path $testProjectPath)) {
        Write-Log "Test project not found: $testProjectPath - skipping tests" "WARN"
        return $true
    }
    
    Write-Log "Running tests for project: $ProjectName" "INFO"
    
    $testResult = Invoke-TestExecution -ProjectPath $testProjectPath -Configuration $Configuration
    
    if ($testResult.Success) {
        Write-Log "✅ Tests passed for $ProjectName" "SUCCESS"
    } else {
        Write-Log "❌ Tests failed for $ProjectName" "ERROR"
        $Global:BuildMetrics.Errors += "Test failure in ${ProjectName}: $($testResult.Output)"
    }
    
    return $testResult.Success
}

function Invoke-CulturalValidation {
    param([string]$ProjectName, [hashtable]$ProjectInfo)
    
    if ($SkipCulturalValidation) {
        Write-Log "Skipping cultural validation for $ProjectName" "INFO"
        return @()
    }
    
    Write-Log "Running cultural intelligence validation for $ProjectName" "INFO"
    
    $projectDir = Split-Path (Join-Path $ProjectRoot $ProjectInfo.Path) -Parent
    $culturalFeatures = Test-CulturalFeatures -ProjectRoot $projectDir
    
    $validations = @()
    
    # Cultural naming conventions
    $csharpFiles = Get-ChildItem -Path $projectDir -Include "*.cs" -Recurse
    foreach ($file in $csharpFiles) {
        $content = Get-Content $file.FullName -Raw
        
        # Check for cultural intelligence patterns
        $patterns = @(
            @{ Pattern = "CulturalContext"; Description = "Cultural context usage" },
            @{ Pattern = "Language.*Sri.*Lanka|Sinhala|Tamil"; Description = "Sri Lankan language support" },
            @{ Pattern = "Region.*Province|District"; Description = "Geographic region support" },
            @{ Pattern = "Cultural.*Intelligence|Diaspora"; Description = "Cultural intelligence features" }
        )
        
        foreach ($pattern in $patterns) {
            if ($content -match $pattern.Pattern) {
                $validations += @{
                    File = $file.Name
                    Pattern = $pattern.Pattern
                    Description = $pattern.Description
                    Status = "Found"
                }
            }
        }
    }
    
    # Validate cultural compliance
    $complianceIssues = @()
    
    # Check for hardcoded English-only strings in domain/business logic
    foreach ($file in $csharpFiles) {
        if ($file.Name -notmatch "Test|Spec") {
            $content = Get-Content $file.FullName -Raw
            $hardcodedStrings = [regex]::Matches($content, '"[A-Za-z\s]{10,}"')
            
            foreach ($match in $hardcodedStrings) {
                if ($match.Value -notmatch "exception|error|log|debug|test") {
                    $complianceIssues += @{
                        File = $file.Name
                        Issue = "Potential hardcoded English string: $($match.Value)"
                        Line = ($content.Substring(0, $match.Index) -split "`n").Count
                    }
                }
            }
        }
    }
    
    if ($complianceIssues.Count -gt 0) {
        Write-Log "⚠️ Found $($complianceIssues.Count) potential cultural compliance issues in $ProjectName" "WARN"
        $Global:BuildMetrics.CulturalValidations[$ProjectName] = @{
            Features = $culturalFeatures
            Validations = $validations
            ComplianceIssues = $complianceIssues
        }
    } else {
        Write-Log "✅ No cultural compliance issues found in $ProjectName" "SUCCESS"
        $Global:BuildMetrics.CulturalValidations[$ProjectName] = @{
            Features = $culturalFeatures
            Validations = $validations
            ComplianceIssues = @()
        }
    }
    
    return $validations
}

function Update-BuildCache {
    $cacheFile = Join-Path $ProjectRoot ".incremental-build-cache"
    $Global:BuildMetrics.EndTime = Get-Date
    $Global:BuildMetrics.TotalDuration = $Global:BuildMetrics.EndTime - $Global:BuildMetrics.StartTime
    
    # Only update cache if build was successful
    $allProjectsSuccessful = $true
    foreach ($project in $Global:BuildMetrics.Projects.Values) {
        if (!$project.Success) {
            $allProjectsSuccessful = $false
            break
        }
    }
    
    if ($allProjectsSuccessful) {
        Set-Content -Path $cacheFile -Value $Global:BuildMetrics.EndTime.ToString()
        Write-Log "Build cache updated successfully" "INFO"
    } else {
        Write-Log "Build cache not updated due to build failures" "WARN"
    }
}

function New-BuildReport {
    $reportData = @{
        BuildMetrics = $Global:BuildMetrics
        Configuration = $Configuration
        IncrementalBuild = !$CleanBuild
        Timestamp = Get-Date
        Success = ($Global:BuildMetrics.Errors.Count -eq 0)
        Summary = @{
            TotalProjects = $Global:BuildMetrics.Projects.Count
            SuccessfulProjects = ($Global:BuildMetrics.Projects.Values | Where-Object { $_.Success }).Count
            FailedProjects = ($Global:BuildMetrics.Projects.Values | Where-Object { !$_.Success }).Count
            TotalErrors = $Global:BuildMetrics.Errors.Count
            TotalWarnings = $Global:BuildMetrics.Warnings.Count
            CulturalValidations = $Global:BuildMetrics.CulturalValidations.Count
        }
    }
    
    $reportPath = "incremental-build-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    New-AutomationReport -Metrics $reportData -ReportPath $reportPath
    
    return $reportData
}

function Write-BuildSummary {
    param([hashtable]$Report)
    
    Write-ColoredOutput "`n=== INCREMENTAL BUILD SUMMARY ===" "Info"
    Write-ColoredOutput "Configuration: $Configuration" "Info"
    Write-ColoredOutput "Total Duration: $($Global:BuildMetrics.TotalDuration.TotalSeconds)s" "Info"
    Write-ColoredOutput "Projects Built: $($Report.Summary.TotalProjects)" "Info"
    Write-ColoredOutput "Successful: $($Report.Summary.SuccessfulProjects)" "Success"
    Write-ColoredOutput "Failed: $($Report.Summary.FailedProjects)" "Error"
    Write-ColoredOutput "Errors: $($Report.Summary.TotalErrors)" "Error"
    Write-ColoredOutput "Warnings: $($Report.Summary.TotalWarnings)" "Warning"
    Write-ColoredOutput "Cultural Validations: $($Report.Summary.CulturalValidations)" "Info"
    
    # Project-level details
    foreach ($projectName in $Global:BuildMetrics.Projects.Keys) {
        $project = $Global:BuildMetrics.Projects[$projectName]
        $status = if ($project.Success) { "✅" } else { "❌" }
        $duration = $project.Duration.TotalSeconds
        Write-ColoredOutput "$status $projectName ($duration`s)" "Info"
    }
    
    Write-ColoredOutput "=================================" "Info"
}

# Main execution
try {
    Write-Log "🏗️ Incremental Build Validation Started" "INFO"
    
    # Initialize
    if (!(Initialize-IncrementalBuild)) {
        Write-Log "Incremental build initialization failed" "ERROR"
        exit 1
    }
    
    # Get project information
    $projects = Get-ProjectDependencies
    
    # Clean build if requested
    if ($CleanBuild) {
        Write-Log "Performing clean build" "INFO"
        & dotnet clean $ProjectRoot --configuration $Configuration | Out-Null
        $changedProjects = @($projects.Keys)
    } else {
        # Detect changes for incremental build
        $changedProjects = Test-ProjectChanges -Projects $projects
        
        if ($changedProjects.Count -eq 0) {
            Write-Log "No changes detected - build not required" "SUCCESS"
            exit 0
        }
    }
    
    # Restore packages
    Write-Log "Restoring NuGet packages" "INFO"
    & dotnet restore $ProjectRoot --verbosity minimal | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Package restore failed" "ERROR"
        exit 1
    }
    
    # Determine build order
    $buildOrder = Get-BuildOrder -Projects $projects -ChangedProjects $changedProjects
    
    # Build projects in dependency order
    $buildSuccess = $true
    foreach ($projectName in $buildOrder) {
        $projectInfo = $projects[$projectName]
        
        # Build project
        $projectBuildSuccess = Invoke-ProjectBuild -ProjectName $projectName -ProjectInfo $projectInfo
        if (!$projectBuildSuccess) {
            $buildSuccess = $false
            if (!$ContinuousIntegration) {
                Write-Log "Stopping build due to failure in $projectName" "ERROR"
                break
            }
        }
        
        # Run tests
        $testSuccess = Invoke-ProjectTests -ProjectName $projectName -ProjectInfo $projectInfo
        if (!$testSuccess) {
            $buildSuccess = $false
            if (!$ContinuousIntegration) {
                Write-Log "Stopping build due to test failure in $projectName" "ERROR"
                break
            }
        }
        
        # Cultural validation
        Invoke-CulturalValidation -ProjectName $projectName -ProjectInfo $projectInfo | Out-Null
    }
    
    # Update build cache and generate report
    Update-BuildCache
    $report = New-BuildReport
    
    # Write summary
    Write-BuildSummary -Report $report
    
    if ($buildSuccess) {
        Write-Log "🏗️ Incremental build completed successfully" "SUCCESS"
        exit 0
    } else {
        Write-Log "🏗️ Incremental build completed with errors" "ERROR"
        exit 1
    }
}
catch {
    Write-Log "Incremental build automation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}