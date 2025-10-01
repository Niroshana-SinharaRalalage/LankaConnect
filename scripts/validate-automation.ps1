#!/usr/bin/env pwsh
# LankaConnect Automation Validation
# Validates all automation scripts and provides usage examples

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("PowerShell", "Bash", "All")]
    [string]$ScriptType = "All",
    
    [Parameter(Mandatory=$false)]
    [switch]$RunExamples,
    
    [Parameter(Mandatory=$false)]
    [switch]$CheckDependencies,
    
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
$ScriptName = "Automation-Validation"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

# Automation scripts to validate
$AutomationScripts = @{
    PowerShell = @{
        "shared-utilities.ps1" = @{
            Path = "scripts/automation/shared-utilities.ps1"
            Description = "Shared utilities and logging functions"
            RequiredFunctions = @("Write-Log", "Test-ProjectStructure", "Invoke-BuildValidation")
            TestCommand = ". `$ScriptPath; Write-Log 'Test message' 'INFO'"
        }
        "tdd-red-phase.ps1" = @{
            Path = "scripts/tdd-red-phase.ps1"
            Description = "TDD RED phase automation for failing test creation"
            Parameters = @("-FeatureName", "-Domain", "-Layer")
            TestCommand = ". `$ScriptPath -FeatureName 'TestFeature' -Domain 'TestDomain' -Layer 'Domain' -WhatIf"
        }
        "tdd-green-phase.ps1" = @{
            Path = "scripts/tdd-green-phase.ps1"
            Description = "TDD GREEN phase automation for implementation"
            Parameters = @("-FeatureName", "-Domain", "-Layer")
            TestCommand = ". `$ScriptPath -FeatureName 'TestFeature' -Domain 'TestDomain' -Layer 'Domain' -WhatIf"
        }
        "incremental-build.ps1" = @{
            Path = "scripts/incremental-build.ps1"
            Description = "Incremental build validation with error reporting"
            Parameters = @("-Configuration", "-SkipTests", "-CleanBuild")
            TestCommand = ". `$ScriptPath -Configuration 'Debug' -SkipTests -WhatIf"
        }
        "cultural-validation.ps1" = @{
            Path = "scripts/cultural-validation.ps1"
            Description = "Cultural intelligence feature validation"
            Parameters = @("-ValidationScope", "-Language", "-ExportReport")
            TestCommand = ". `$ScriptPath -ValidationScope 'Naming' -Language 'All' -WhatIf"
        }
        "component-test.ps1" = @{
            Path = "scripts/component-test.ps1"
            Description = "Component-level testing automation"
            Parameters = @("-Component", "-TestType", "-CollectCoverage")
            TestCommand = ". `$ScriptPath -Component 'Domain' -TestType 'Unit' -WhatIf"
        }
        "quality-gate.ps1" = @{
            Path = "scripts/quality-gate.ps1"
            Description = "Comprehensive quality gate validation"
            Parameters = @("-Environment", "-QualityLevel", "-GenerateReport")
            TestCommand = ". `$ScriptPath -Environment 'Development' -QualityLevel 'Basic' -WhatIf"
        }
    }
    Bash = @{
        "shared-utilities.sh" = @{
            Path = "scripts/bash/shared-utilities.sh"
            Description = "Shared utilities and logging functions (Bash)"
            RequiredFunctions = @("write_log", "test_project_structure", "invoke_build_validation")
            TestCommand = "source `$ScriptPath && write_log 'Test message' 'INFO'"
        }
        "tdd-red-phase.sh" = @{
            Path = "scripts/bash/tdd-red-phase.sh"
            Description = "TDD RED phase automation for failing test creation (Bash)"
            Parameters = @("--feature-name", "--domain", "--layer")
            TestCommand = "`$ScriptPath --feature-name 'TestFeature' --domain 'TestDomain' --layer 'Domain' --help"
        }
        "incremental-build.sh" = @{
            Path = "scripts/bash/incremental-build.sh"
            Description = "Incremental build validation with error reporting (Bash)"
            Parameters = @("--configuration", "--skip-tests", "--clean-build")
            TestCommand = "`$ScriptPath --configuration 'Debug' --skip-tests --help"
        }
    }
}

function Initialize-ValidationTesting {
    Write-Log "🔍 Starting Automation Script Validation" "INFO"
    Write-Log "Script Type: $ScriptType" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    return $true
}

function Test-ScriptExistence {
    param([hashtable]$Scripts, [string]$Type)
    
    Write-Log "Validating $Type script existence" "INFO"
    
    $results = @{
        Found = @()
        Missing = @()
        TotalScripts = $Scripts.Count
    }
    
    foreach ($scriptName in $Scripts.Keys) {
        $scriptInfo = $Scripts[$scriptName]
        $scriptPath = Join-Path $ProjectRoot $scriptInfo.Path
        
        if (Test-Path $scriptPath) {
            $results.Found += @{
                Name = $scriptName
                Path = $scriptPath
                Info = $scriptInfo
            }
            Write-Log "✅ Found: $scriptName" "SUCCESS"
        } else {
            $results.Missing += @{
                Name = $scriptName
                Path = $scriptPath
                Info = $scriptInfo
            }
            Write-Log "❌ Missing: $scriptName at $scriptPath" "ERROR"
        }
    }
    
    Write-Log "$Type Scripts - Found: $($results.Found.Count), Missing: $($results.Missing.Count)" "INFO"
    return $results
}

function Test-PowerShellScriptSyntax {
    param([array]$FoundScripts)
    
    Write-Log "Validating PowerShell script syntax" "INFO"
    
    $syntaxResults = @{
        Valid = @()
        Invalid = @()
    }
    
    foreach ($script in $FoundScripts) {
        try {
            # Test PowerShell syntax
            $null = [System.Management.Automation.PSParser]::Tokenize((Get-Content $script.Path -Raw), [ref]$null)
            
            $syntaxResults.Valid += $script
            Write-Log "✅ Syntax valid: $($script.Name)" "SUCCESS"
        }
        catch {
            $syntaxResults.Invalid += @{
                Script = $script
                Error = $_.Exception.Message
            }
            Write-Log "❌ Syntax error in $($script.Name): $($_.Exception.Message)" "ERROR"
        }
    }
    
    return $syntaxResults
}

function Test-BashScriptSyntax {
    param([array]$FoundScripts)
    
    Write-Log "Validating Bash script syntax" "INFO"
    
    $syntaxResults = @{
        Valid = @()
        Invalid = @()
    }
    
    foreach ($script in $FoundScripts) {
        try {
            # Test bash syntax using bash -n (syntax check only)
            $bashTest = & bash -n $script.Path 2>&1
            if ($LASTEXITCODE -eq 0) {
                $syntaxResults.Valid += $script
                Write-Log "✅ Syntax valid: $($script.Name)" "SUCCESS"
            } else {
                $syntaxResults.Invalid += @{
                    Script = $script
                    Error = $bashTest
                }
                Write-Log "❌ Syntax error in $($script.Name): $bashTest" "ERROR"
            }
        }
        catch {
            $syntaxResults.Invalid += @{
                Script = $script
                Error = $_.Exception.Message
            }
            Write-Log "❌ Failed to test syntax for $($script.Name): $($_.Exception.Message)" "ERROR"
        }
    }
    
    return $syntaxResults
}

function Test-ScriptParameters {
    param([array]$ValidScripts, [string]$ScriptType)
    
    Write-Log "Validating script parameters for $ScriptType scripts" "INFO"
    
    $parameterResults = @{
        Valid = @()
        Issues = @()
    }
    
    foreach ($script in $ValidScripts) {
        if ($script.Info.Parameters) {
            $parameterIssues = @()
            
            if ($ScriptType -eq "PowerShell") {
                # Check PowerShell parameters
                $content = Get-Content $script.Path -Raw
                foreach ($param in $script.Info.Parameters) {
                    if ($content -notmatch "\[Parameter.*\]\s*\[.*\]\s*\`$$($param.TrimStart('-'))") {
                        $parameterIssues += "Missing or malformed parameter: $param"
                    }
                }
            } elseif ($ScriptType -eq "Bash") {
                # Check Bash parameters
                $content = Get-Content $script.Path -Raw
                foreach ($param in $script.Info.Parameters) {
                    if ($content -notmatch "$param\)") {
                        $parameterIssues += "Missing parameter handling: $param"
                    }
                }
            }
            
            if ($parameterIssues.Count -eq 0) {
                $parameterResults.Valid += $script
                Write-Log "✅ Parameters valid: $($script.Name)" "SUCCESS"
            } else {
                $parameterResults.Issues += @{
                    Script = $script
                    Issues = $parameterIssues
                }
                Write-Log "⚠️ Parameter issues in $($script.Name): $($parameterIssues -join ', ')" "WARN"
            }
        } else {
            $parameterResults.Valid += $script
        }
    }
    
    return $parameterResults
}

function Test-RequiredFunctions {
    param([array]$ValidScripts, [string]$ScriptType)
    
    Write-Log "Validating required functions for $ScriptType scripts" "INFO"
    
    $functionResults = @{
        Valid = @()
        Issues = @()
    }
    
    foreach ($script in $ValidScripts) {
        if ($script.Info.RequiredFunctions) {
            $functionIssues = @()
            $content = Get-Content $script.Path -Raw
            
            foreach ($func in $script.Info.RequiredFunctions) {
                $pattern = if ($ScriptType -eq "PowerShell") { "function\s+$func" } else { "$func\s*\(\)" }
                if ($content -notmatch $pattern) {
                    $functionIssues += "Missing function: $func"
                }
            }
            
            if ($functionIssues.Count -eq 0) {
                $functionResults.Valid += $script
                Write-Log "✅ Functions valid: $($script.Name)" "SUCCESS"
            } else {
                $functionResults.Issues += @{
                    Script = $script
                    Issues = $functionIssues
                }
                Write-Log "⚠️ Function issues in $($script.Name): $($functionIssues -join ', ')" "WARN"
            }
        } else {
            $functionResults.Valid += $script
        }
    }
    
    return $functionResults
}

function Invoke-ScriptExamples {
    param([array]$ValidScripts, [string]$ScriptType)
    
    if (!$RunExamples) {
        Write-Log "Skipping script example execution" "INFO"
        return @{ Executed = @(); Skipped = $ValidScripts }
    }
    
    Write-Log "Running script examples for $ScriptType scripts" "INFO"
    
    $exampleResults = @{
        Successful = @()
        Failed = @()
        Skipped = @()
    }
    
    foreach ($script in $ValidScripts) {
        if ($script.Info.TestCommand) {
            Write-Log "Testing: $($script.Name)" "INFO"
            
            try {
                $testCommand = $script.Info.TestCommand.Replace('$ScriptPath', $script.Path)
                
                if ($ScriptType -eq "PowerShell") {
                    $result = Invoke-Expression $testCommand 2>&1
                } else {
                    $result = & bash -c $testCommand 2>&1
                }
                
                if ($LASTEXITCODE -eq 0) {
                    $exampleResults.Successful += @{
                        Script = $script
                        Output = $result
                    }
                    Write-Log "✅ Example successful: $($script.Name)" "SUCCESS"
                } else {
                    $exampleResults.Failed += @{
                        Script = $script
                        Error = $result
                        ExitCode = $LASTEXITCODE
                    }
                    Write-Log "❌ Example failed: $($script.Name) - Exit Code: $LASTEXITCODE" "ERROR"
                }
            }
            catch {
                $exampleResults.Failed += @{
                    Script = $script
                    Error = $_.Exception.Message
                    ExitCode = -1
                }
                Write-Log "❌ Example exception: $($script.Name) - $($_.Exception.Message)" "ERROR"
            }
        } else {
            $exampleResults.Skipped += $script
            Write-Log "⏭️ No test command for: $($script.Name)" "INFO"
        }
    }
    
    return $exampleResults
}

function Test-Dependencies {
    if (!$CheckDependencies) {
        Write-Log "Skipping dependency checks" "INFO"
        return @{ Status = "Skipped" }
    }
    
    Write-Log "Checking automation dependencies" "INFO"
    
    $dependencies = @{
        PowerShell = @{
            Required = @()
            Optional = @("PowerShell Core")
            ModulesRequired = @()
        }
        DotNet = @{
            Required = @("dotnet")
            Commands = @("dotnet --version", "dotnet build --help", "dotnet test --help")
        }
        Bash = @{
            Required = @("bash", "find", "grep")
            Optional = @("bc")
        }
        Git = @{
            Required = @("git")
            Commands = @("git --version")
        }
    }
    
    $dependencyResults = @{
        Available = @()
        Missing = @()
        Issues = @()
    }
    
    # Check .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            $dependencyResults.Available += "✅ .NET SDK: $dotnetVersion"
            Write-Log "✅ .NET SDK available: $dotnetVersion" "SUCCESS"
        } else {
            $dependencyResults.Missing += "❌ .NET SDK not found"
            Write-Log "❌ .NET SDK not found" "ERROR"
        }
    }
    catch {
        $dependencyResults.Missing += "❌ .NET SDK not accessible"
        Write-Log "❌ .NET SDK not accessible: $($_.Exception.Message)" "ERROR"
    }
    
    # Check Git
    try {
        $gitVersion = & git --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            $dependencyResults.Available += "✅ Git: $gitVersion"
            Write-Log "✅ Git available: $gitVersion" "SUCCESS"
        } else {
            $dependencyResults.Missing += "❌ Git not found"
            Write-Log "❌ Git not found" "ERROR"
        }
    }
    catch {
        $dependencyResults.Missing += "❌ Git not accessible"
        Write-Log "❌ Git not accessible: $($_.Exception.Message)" "ERROR"
    }
    
    # Check Bash (if testing Bash scripts)
    if ($ScriptType -in @("Bash", "All")) {
        try {
            $bashVersion = & bash --version 2>&1
            if ($LASTEXITCODE -eq 0) {
                $bashVersionLine = ($bashVersion -split "`n")[0]
                $dependencyResults.Available += "✅ Bash: $bashVersionLine"
                Write-Log "✅ Bash available: $bashVersionLine" "SUCCESS"
            } else {
                $dependencyResults.Missing += "❌ Bash not found"
                Write-Log "❌ Bash not found" "ERROR"
            }
        }
        catch {
            $dependencyResults.Missing += "❌ Bash not accessible"
            Write-Log "❌ Bash not accessible: $($_.Exception.Message)" "ERROR"
        }
    }
    
    return $dependencyResults
}

function New-ValidationReport {
    param([hashtable]$Results)
    
    $reportData = @{
        ValidationResults = $Results
        Timestamp = Get-Date
        ProjectRoot = $ProjectRoot
        ScriptType = $ScriptType
        Summary = @{
            TotalScripts = 0
            ValidScripts = 0
            FailedScripts = 0
            MissingScripts = 0
        }
    }
    
    # Calculate summary
    foreach ($scriptType in $Results.Keys) {
        if ($Results[$scriptType].ScriptExistence) {
            $reportData.Summary.TotalScripts += $Results[$scriptType].ScriptExistence.TotalScripts
            $reportData.Summary.ValidScripts += $Results[$scriptType].ScriptExistence.Found.Count
            $reportData.Summary.MissingScripts += $Results[$scriptType].ScriptExistence.Missing.Count
        }
    }
    
    $reportPath = "automation-validation-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    New-AutomationReport -Metrics $reportData -ReportPath $reportPath
    
    return $reportData
}

function Write-ValidationSummary {
    param([hashtable]$Results)
    
    Write-ColoredOutput "`n=== AUTOMATION VALIDATION SUMMARY ===" "Info"
    Write-ColoredOutput "Script Type: $ScriptType" "Info"
    Write-ColoredOutput "Run Examples: $RunExamples" "Info"
    Write-ColoredOutput "Check Dependencies: $CheckDependencies" "Info"
    
    foreach ($scriptTypeName in $Results.Keys) {
        Write-ColoredOutput "`n--- $scriptTypeName Scripts ---" "Info"
        
        $typeResults = $Results[$scriptTypeName]
        
        if ($typeResults.ScriptExistence) {
            $existence = $typeResults.ScriptExistence
            Write-ColoredOutput "Total Scripts: $($existence.TotalScripts)" "Info"
            Write-ColoredOutput "Found: $($existence.Found.Count)" "Success"
            Write-ColoredOutput "Missing: $($existence.Missing.Count)" "Error"
        }
        
        if ($typeResults.SyntaxValidation) {
            $syntax = $typeResults.SyntaxValidation
            Write-ColoredOutput "Syntax Valid: $($syntax.Valid.Count)" "Success"
            Write-ColoredOutput "Syntax Invalid: $($syntax.Invalid.Count)" "Error"
        }
        
        if ($typeResults.ParameterValidation) {
            $params = $typeResults.ParameterValidation
            Write-ColoredOutput "Parameters Valid: $($params.Valid.Count)" "Success"
            Write-ColoredOutput "Parameter Issues: $($params.Issues.Count)" "Warning"
        }
        
        if ($typeResults.FunctionValidation) {
            $functions = $typeResults.FunctionValidation
            Write-ColoredOutput "Functions Valid: $($functions.Valid.Count)" "Success"
            Write-ColoredOutput "Function Issues: $($functions.Issues.Count)" "Warning"
        }
        
        if ($typeResults.ExampleExecution) {
            $examples = $typeResults.ExampleExecution
            Write-ColoredOutput "Examples Successful: $($examples.Successful.Count)" "Success"
            Write-ColoredOutput "Examples Failed: $($examples.Failed.Count)" "Error"
            Write-ColoredOutput "Examples Skipped: $($examples.Skipped.Count)" "Info"
        }
    }
    
    if ($Results.Dependencies) {
        Write-ColoredOutput "`n--- Dependencies ---" "Info"
        foreach ($dep in $Results.Dependencies.Available) {
            Write-ColoredOutput $dep "Success"
        }
        foreach ($dep in $Results.Dependencies.Missing) {
            Write-ColoredOutput $dep "Error"
        }
    }
    
    Write-ColoredOutput "==============================" "Info"
}

# Main execution
try {
    Write-Log "🔍 Automation Validation Started" "INFO"
    
    # Initialize validation
    if (!(Initialize-ValidationTesting)) {
        Write-Log "Validation initialization failed" "ERROR"
        exit 1
    }
    
    $allResults = @{}
    
    # Check dependencies first
    if ($CheckDependencies) {
        $allResults.Dependencies = Test-Dependencies
    }
    
    # Validate PowerShell scripts
    if ($ScriptType -in @("PowerShell", "All")) {
        Write-Log "Validating PowerShell automation scripts" "INFO"
        
        $psResults = @{}
        $psScripts = $AutomationScripts.PowerShell
        
        # Test script existence
        $psResults.ScriptExistence = Test-ScriptExistence -Scripts $psScripts -Type "PowerShell"
        
        if ($psResults.ScriptExistence.Found.Count -gt 0) {
            # Test syntax
            $psResults.SyntaxValidation = Test-PowerShellScriptSyntax -FoundScripts $psResults.ScriptExistence.Found
            
            if ($psResults.SyntaxValidation.Valid.Count -gt 0) {
                # Test parameters
                $psResults.ParameterValidation = Test-ScriptParameters -ValidScripts $psResults.SyntaxValidation.Valid -ScriptType "PowerShell"
                
                # Test required functions
                $psResults.FunctionValidation = Test-RequiredFunctions -ValidScripts $psResults.SyntaxValidation.Valid -ScriptType "PowerShell"
                
                # Run examples
                $psResults.ExampleExecution = Invoke-ScriptExamples -ValidScripts $psResults.SyntaxValidation.Valid -ScriptType "PowerShell"
            }
        }
        
        $allResults.PowerShell = $psResults
    }
    
    # Validate Bash scripts
    if ($ScriptType -in @("Bash", "All")) {
        Write-Log "Validating Bash automation scripts" "INFO"
        
        $bashResults = @{}
        $bashScripts = $AutomationScripts.Bash
        
        # Test script existence
        $bashResults.ScriptExistence = Test-ScriptExistence -Scripts $bashScripts -Type "Bash"
        
        if ($bashResults.ScriptExistence.Found.Count -gt 0) {
            # Test syntax
            $bashResults.SyntaxValidation = Test-BashScriptSyntax -FoundScripts $bashResults.ScriptExistence.Found
            
            if ($bashResults.SyntaxValidation.Valid.Count -gt 0) {
                # Test parameters
                $bashResults.ParameterValidation = Test-ScriptParameters -ValidScripts $bashResults.SyntaxValidation.Valid -ScriptType "Bash"
                
                # Test required functions
                $bashResults.FunctionValidation = Test-RequiredFunctions -ValidScripts $bashResults.SyntaxValidation.Valid -ScriptType "Bash"
                
                # Run examples
                $bashResults.ExampleExecution = Invoke-ScriptExamples -ValidScripts $bashResults.SyntaxValidation.Valid -ScriptType "Bash"
            }
        }
        
        $allResults.Bash = $bashResults
    }
    
    # Generate report and summary
    $report = New-ValidationReport -Results $allResults
    Write-ValidationSummary -Results $allResults
    
    # Determine exit code
    $hasErrors = $false
    foreach ($scriptTypeResults in $allResults.Values) {
        if ($scriptTypeResults.ScriptExistence -and $scriptTypeResults.ScriptExistence.Missing.Count -gt 0) {
            $hasErrors = $true
        }
        if ($scriptTypeResults.SyntaxValidation -and $scriptTypeResults.SyntaxValidation.Invalid.Count -gt 0) {
            $hasErrors = $true
        }
        if ($scriptTypeResults.ExampleExecution -and $scriptTypeResults.ExampleExecution.Failed.Count -gt 0) {
            $hasErrors = $true
        }
    }
    
    if ($allResults.Dependencies -and $allResults.Dependencies.Missing.Count -gt 0) {
        $hasErrors = $true
    }
    
    if ($hasErrors) {
        Write-Log "🔍 Automation validation completed with issues" "WARN"
        exit 2
    } else {
        Write-Log "🔍 Automation validation completed successfully" "SUCCESS"
        exit 0
    }
}
catch {
    Write-Log "Automation validation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}