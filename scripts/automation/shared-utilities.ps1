# LankaConnect Automation Utilities
# Shared functions for build automation and cultural intelligence validation

# Color coding for output
function Write-ColoredOutput {
    param(
        [string]$Message,
        [string]$Color = "White",
        [switch]$NoNewline
    )
    
    $colorMap = @{
        "Success" = "Green"
        "Warning" = "Yellow" 
        "Error" = "Red"
        "Info" = "Cyan"
        "Debug" = "Gray"
    }
    
    $outputColor = if ($colorMap.ContainsKey($Color)) { $colorMap[$Color] } else { $Color }
    
    if ($NoNewline) {
        Write-Host $Message -ForegroundColor $outputColor -NoNewline
    } else {
        Write-Host $Message -ForegroundColor $outputColor
    }
}

# Logging functionality
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO",
        [string]$LogFile = "automation.log"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    
    # Ensure logs directory exists
    $logDir = Join-Path $PSScriptRoot "logs"
    if (!(Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    $fullLogPath = Join-Path $logDir $LogFile
    Add-Content -Path $fullLogPath -Value $logEntry
    
    # Also output to console with color
    switch ($Level) {
        "ERROR" { Write-ColoredOutput "❌ $Message" "Error" }
        "WARN" { Write-ColoredOutput "⚠️ $Message" "Warning" }
        "SUCCESS" { Write-ColoredOutput "✅ $Message" "Success" }
        default { Write-ColoredOutput "ℹ️ $Message" "Info" }
    }
}

# Project structure validation
function Test-ProjectStructure {
    param([string]$RootPath = $PSScriptRoot)
    
    $requiredPaths = @(
        "src/LankaConnect.Domain",
        "src/LankaConnect.Application", 
        "src/LankaConnect.Infrastructure",
        "src/LankaConnect.API",
        "tests/LankaConnect.Domain.Tests",
        "tests/LankaConnect.Application.Tests",
        "tests/LankaConnect.IntegrationTests"
    )
    
    $projectRoot = Split-Path $RootPath -Parent
    $missing = @()
    
    foreach ($path in $requiredPaths) {
        $fullPath = Join-Path $projectRoot $path
        if (!(Test-Path $fullPath)) {
            $missing += $path
        }
    }
    
    if ($missing.Count -gt 0) {
        Write-Log "Missing required project paths: $($missing -join ', ')" "ERROR"
        return $false
    }
    
    Write-Log "Project structure validation passed" "SUCCESS"
    return $true
}

# Cultural intelligence feature detection
function Test-CulturalFeatures {
    param([string]$ProjectRoot)
    
    $culturalPatterns = @(
        "CulturalIntelligence",
        "DiasporaMapping", 
        "CulturalEvent",
        "CommunityEngagement",
        "CulturalContext",
        "LocalizationService"
    )
    
    $foundFeatures = @()
    $searchPaths = @("src", "tests")
    
    foreach ($searchPath in $searchPaths) {
        $fullSearchPath = Join-Path $ProjectRoot $searchPath
        if (Test-Path $fullSearchPath) {
            foreach ($pattern in $culturalPatterns) {
                $matches = Get-ChildItem -Path $fullSearchPath -Recurse -Include "*.cs" | 
                    Select-String -Pattern $pattern -List
                if ($matches) {
                    $foundFeatures += $pattern
                }
            }
        }
    }
    
    $uniqueFeatures = $foundFeatures | Sort-Object | Get-Unique
    
    if ($uniqueFeatures.Count -eq 0) {
        Write-Log "No cultural intelligence features detected" "WARN"
        return @()
    }
    
    Write-Log "Cultural features found: $($uniqueFeatures -join ', ')" "SUCCESS"
    return $uniqueFeatures
}

# Build validation
function Invoke-BuildValidation {
    param(
        [string]$ProjectRoot,
        [string]$Configuration = "Debug"
    )
    
    Write-Log "Starting build validation for $Configuration configuration" "INFO"
    
    try {
        # Clean build
        $cleanResult = & dotnet clean $ProjectRoot --configuration $Configuration 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Log "Clean failed: $cleanResult" "ERROR"
            return $false
        }
        
        # Restore packages
        $restoreResult = & dotnet restore $ProjectRoot 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Log "Restore failed: $restoreResult" "ERROR"
            return $false
        }
        
        # Build
        $buildResult = & dotnet build $ProjectRoot --configuration $Configuration --no-restore 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Log "Build failed: $buildResult" "ERROR"
            return $false
        }
        
        Write-Log "Build validation completed successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Build validation exception: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Test execution with metrics
function Invoke-TestExecution {
    param(
        [string]$ProjectPath,
        [string]$TestPattern = "*",
        [string]$Configuration = "Debug",
        [switch]$CollectCoverage
    )
    
    Write-Log "Executing tests for: $ProjectPath" "INFO"
    
    try {
        $testArgs = @(
            "test",
            $ProjectPath,
            "--configuration", $Configuration,
            "--logger", "console;verbosity=detailed",
            "--no-build"
        )
        
        if ($CollectCoverage) {
            $testArgs += "--collect", "XPlat Code Coverage"
        }
        
        if ($TestPattern -ne "*") {
            $testArgs += "--filter", $TestPattern
        }
        
        $testResult = & dotnet @testArgs 2>&1
        $success = $LASTEXITCODE -eq 0
        
        if ($success) {
            Write-Log "Tests passed for $ProjectPath" "SUCCESS"
        } else {
            Write-Log "Tests failed for $ProjectPath. Output: $testResult" "ERROR"
        }
        
        return @{
            Success = $success
            Output = $testResult
            ExitCode = $LASTEXITCODE
        }
    }
    catch {
        Write-Log "Test execution exception: $($_.Exception.Message)" "ERROR"
        return @{
            Success = $false
            Output = $_.Exception.Message
            ExitCode = -1
        }
    }
}

# Quality metrics collection
function Get-QualityMetrics {
    param([string]$ProjectRoot)
    
    $metrics = @{
        Timestamp = Get-Date
        BuildStatus = "Unknown"
        TestResults = @{}
        CoverageData = @{}
        CulturalFeatures = @()
        Warnings = @()
        Errors = @()
    }
    
    # Collect build metrics
    $buildSuccess = Invoke-BuildValidation -ProjectRoot $ProjectRoot
    $metrics.BuildStatus = if ($buildSuccess) { "Success" } else { "Failed" }
    
    # Collect cultural intelligence metrics
    $metrics.CulturalFeatures = Test-CulturalFeatures -ProjectRoot $ProjectRoot
    
    Write-Log "Quality metrics collected: Build=$($metrics.BuildStatus), Cultural Features=$($metrics.CulturalFeatures.Count)" "INFO"
    
    return $metrics
}

# Performance monitoring
function Measure-PerformanceMetrics {
    param(
        [scriptblock]$ScriptBlock,
        [string]$OperationName
    )
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $result = & $ScriptBlock
        $stopwatch.Stop()
        
        Write-Log "Performance: $OperationName completed in $($stopwatch.ElapsedMilliseconds)ms" "INFO"
        
        return @{
            Success = $true
            Result = $result
            ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds
            ElapsedSeconds = $stopwatch.Elapsed.TotalSeconds
        }
    }
    catch {
        $stopwatch.Stop()
        Write-Log "Performance: $OperationName failed after $($stopwatch.ElapsedMilliseconds)ms - $($_.Exception.Message)" "ERROR"
        
        return @{
            Success = $false
            Error = $_.Exception.Message
            ElapsedMilliseconds = $stopwatch.ElapsedMilliseconds
        }
    }
}

# Report generation
function New-AutomationReport {
    param(
        [hashtable]$Metrics,
        [string]$ReportPath = "automation-report.json"
    )
    
    $reportDir = Join-Path $PSScriptRoot "reports"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $fullReportPath = Join-Path $reportDir $ReportPath
    $Metrics | ConvertTo-Json -Depth 10 | Set-Content -Path $fullReportPath
    
    Write-Log "Automation report saved to: $fullReportPath" "SUCCESS"
    return $fullReportPath
}

# Export functions for use in other scripts
Export-ModuleMember -Function @(
    'Write-ColoredOutput',
    'Write-Log', 
    'Test-ProjectStructure',
    'Test-CulturalFeatures',
    'Invoke-BuildValidation',
    'Invoke-TestExecution',
    'Get-QualityMetrics',
    'Measure-PerformanceMetrics',
    'New-AutomationReport'
)