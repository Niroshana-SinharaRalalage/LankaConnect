#!/usr/bin/env pwsh
# LankaConnect Cultural Intelligence Validation
# Comprehensive validation of cultural intelligence features and Sri Lankan cultural compliance

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Naming", "Content", "Localization", "Business", "API")]
    [string]$ValidationScope = "All",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Sinhala", "Tamil", "English", "All")]
    [string]$Language = "All",
    
    [Parameter(Mandatory=$false)]
    [switch]$FixIssues,
    
    [Parameter(Mandatory=$false)]
    [switch]$DetailedReport,
    
    [Parameter(Mandatory=$false)]
    [switch]$ExportReport,
    
    [Parameter(Mandatory=$false)]
    [string]$ReportFormat = "JSON",
    
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
$ScriptName = "Cultural-Validation"
$ProjectRoot = Split-Path $PSScriptRoot -Parent

# Cultural validation results
$Global:ValidationResults = @{
    Timestamp = Get-Date
    Scope = $ValidationScope
    Language = $Language
    TotalIssues = 0
    CriticalIssues = 0
    Warnings = 0
    Suggestions = 0
    Categories = @{
        Naming = @{ Issues = @(); Status = "Unknown" }
        Content = @{ Issues = @(); Status = "Unknown" }
        Localization = @{ Issues = @(); Status = "Unknown" }
        Business = @{ Issues = @(); Status = "Unknown" }
        API = @{ Issues = @(); Status = "Unknown" }
    }
    CulturalFeatures = @()
    ComplianceScore = 0
    Recommendations = @()
}

# Sri Lankan cultural patterns and standards
$CulturalPatterns = @{
    SinhalaPatterns = @(
        "CulturalContext.*Sinhala",
        "Language.*si-LK",
        "Province.*Western|Central|Southern|Northern|Eastern|North Western|North Central|Uva|Sabaragamuwa",
        "District.*Colombo|Kandy|Galle|Jaffna|Batticaloa|Kurunegala|Anuradhapura|Badulla|Ratnapura"
    )
    TamilPatterns = @(
        "CulturalContext.*Tamil",
        "Language.*ta-LK",
        "Province.*Northern|Eastern",
        "District.*Jaffna|Kilinochchi|Mannar|Vavuniya|Mullaitivu|Batticaloa|Ampara|Trincomalee"
    )
    BusinessPatterns = @(
        "BusinessHours.*Sri.*Lanka",
        "TimeZone.*Colombo|Asia/Colombo",
        "Currency.*LKR|Sri.*Lankan.*Rupee",
        "PostalCode.*\d{5}",
        "PhoneNumber.*\+94|0\d{9}"
    )
    CulturalIntelligencePatterns = @(
        "CulturalIntelligence",
        "DiasporaMapping",
        "CulturalEvent.*Buddhist|Hindu|Islamic|Christian",
        "Festival.*Vesak|Diwali|Ramadan|Christmas",
        "LocalCustom",
        "CommunityEngagement"
    )
}

# Cultural compliance rules
$ComplianceRules = @{
    RequiredFeatures = @(
        "Multi-language support (Sinhala, Tamil, English)",
        "Provincial and district awareness", 
        "Cultural event integration",
        "Time zone handling (Asia/Colombo)",
        "Currency support (LKR)",
        "Phone number validation (+94 format)"
    )
    ProhibitedPatterns = @(
        @{ Pattern = '"[A-Za-z\s]+ only".*english'; Message = "English-only assumptions detected" },
        @{ Pattern = 'India.*cultural|Indian.*context'; Message = "Indian cultural assumptions - should be Sri Lankan" },
        @{ Pattern = 'hardcoded.*"Monday|Tuesday|Wednesday"'; Message = "Hardcoded weekday names without localization" },
        @{ Pattern = 'USD|US\$|Dollar'; Message = "US Dollar references - should support LKR" },
        @{ Pattern = '\+1|\+44|\+61'; Message = "Non-Sri Lankan phone number formats" }
    )
    NamingConventions = @{
        ValidPrefixes = @("Cultural", "Diaspora", "Community", "SriLankan", "Lankan", "Regional")
        RequiredSuffixes = @("Service", "Repository", "Controller", "Handler", "Validator")
        CulturalDomains = @("CulturalIntelligence", "Community", "Events", "Communications", "Business")
    }
}

function Initialize-CulturalValidation {
    Write-Log "🇱🇰 Starting Cultural Intelligence Validation" "INFO"
    Write-Log "Validation Scope: $ValidationScope" "INFO"
    Write-Log "Target Language: $Language" "INFO"
    
    # Validate project structure
    if (!(Test-ProjectStructure -RootPath $ProjectRoot)) {
        Write-Log "Project structure validation failed" "ERROR"
        return $false
    }
    
    # Detect existing cultural features
    $Global:ValidationResults.CulturalFeatures = Test-CulturalFeatures -ProjectRoot $ProjectRoot
    Write-Log "Detected $($Global:ValidationResults.CulturalFeatures.Count) cultural intelligence features" "INFO"
    
    return $true
}

function Test-NamingConventions {
    Write-Log "Validating cultural naming conventions" "INFO"
    
    $issues = @()
    $sourcePaths = @("src", "tests")
    
    foreach ($sourcePath in $sourcePaths) {
        $fullPath = Join-Path $ProjectRoot $sourcePath
        if (!(Test-Path $fullPath)) { continue }
        
        $csharpFiles = Get-ChildItem -Path $fullPath -Include "*.cs" -Recurse
        
        foreach ($file in $csharpFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Replace($ProjectRoot, "").TrimStart('\')
            
            # Check class/interface naming
            $classMatches = [regex]::Matches($content, 'public\s+(class|interface)\s+(\w+)')
            foreach ($match in $classMatches) {
                $className = $match.Groups[2].Value
                
                # Cultural domain classes should have appropriate prefixes
                if ($relativePath -match "CulturalIntelligence|Community|Diaspora") {
                    $hasValidPrefix = $false
                    foreach ($prefix in $ComplianceRules.NamingConventions.ValidPrefixes) {
                        if ($className -match "^$prefix") {
                            $hasValidPrefix = $true
                            break
                        }
                    }
                    
                    if (!$hasValidPrefix -and $className -notmatch "Test|Mock|Base|Abstract") {
                        $issues += @{
                            File = $relativePath
                            Type = "Naming"
                            Severity = "Warning"
                            Message = "Class '$className' in cultural domain should have cultural prefix"
                            Line = ($content.Substring(0, $match.Index) -split "`n").Count
                            Suggestion = "Consider prefixing with: $($ComplianceRules.NamingConventions.ValidPrefixes -join ', ')"
                        }
                    }
                }
            }
            
            # Check for hardcoded cultural assumptions
            $hardcodedPatterns = @(
                @{ Pattern = 'english.*only|only.*english'; Message = "English-only assumptions" },
                @{ Pattern = 'default.*language.*english'; Message = "English as default language assumption" },
                @{ Pattern = 'american|usa|united.*states'; Message = "US-centric cultural assumptions" }
            )
            
            foreach ($pattern in $hardcodedPatterns) {
                $matches = [regex]::Matches($content, $pattern.Pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
                foreach ($match in $matches) {
                    $issues += @{
                        File = $relativePath
                        Type = "Naming"
                        Severity = "Critical"
                        Message = $pattern.Message
                        Line = ($content.Substring(0, $match.Index) -split "`n").Count
                        Context = $match.Value
                        Suggestion = "Replace with Sri Lankan cultural context or make culturally neutral"
                    }
                }
            }
        }
    }
    
    $Global:ValidationResults.Categories.Naming.Issues = $issues
    $Global:ValidationResults.Categories.Naming.Status = if ($issues.Count -eq 0) { "Pass" } else { "Issues Found" }
    
    Write-Log "Naming validation: $($issues.Count) issues found" "INFO"
    return $issues
}

function Test-ContentLocalization {
    Write-Log "Validating content localization and cultural appropriateness" "INFO"
    
    $issues = @()
    $sourcePaths = @("src")
    
    foreach ($sourcePath in $sourcePaths) {
        $fullPath = Join-Path $ProjectRoot $sourcePath
        if (!(Test-Path $fullPath)) { continue }
        
        $csharpFiles = Get-ChildItem -Path $fullPath -Include "*.cs" -Recurse
        
        foreach ($file in $csharpFiles) {
            if ($file.Name -match "Test|Spec") { continue }
            
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Replace($ProjectRoot, "").TrimStart('\')
            
            # Check for hardcoded strings that should be localized
            $stringMatches = [regex]::Matches($content, '"([^"]{10,})"')
            foreach ($match in $stringMatches) {
                $stringValue = $match.Groups[1].Value
                
                # Skip technical strings
                if ($stringValue -match "exception|error|debug|log|test|connectionstring|sql") {
                    continue
                }
                
                # Check for user-facing content
                if ($stringValue -match "welcome|hello|thank|please|sorry|success|failure|message|notification") {
                    $issues += @{
                        File = $relativePath
                        Type = "Content"
                        Severity = "Warning"
                        Message = "Potential user-facing string should be localized"
                        Line = ($content.Substring(0, $match.Index) -split "`n").Count
                        Context = $stringValue
                        Suggestion = "Move to resource file and support Sinhala/Tamil translations"
                    }
                }
                
                # Check for cultural assumptions
                if ($stringValue -match "western|american|european|indian") {
                    $issues += @{
                        File = $relativePath
                        Type = "Content"
                        Severity = "Critical"
                        Message = "Cultural assumption in content"
                        Line = ($content.Substring(0, $match.Index) -split "`n").Count
                        Context = $stringValue
                        Suggestion = "Replace with Sri Lankan context or make culturally neutral"
                    }
                }
            }
            
            # Check for missing cultural context in business logic
            if ($relativePath -match "Business|Domain|Service" -and $content -notmatch "CulturalContext") {
                $classMatches = [regex]::Matches($content, 'public\s+class\s+(\w+)')
                foreach ($classMatch in $classMatches) {
                    $className = $classMatch.Groups[1].Value
                    if ($className -match "Service|Handler|Manager" -and $className -notmatch "Test|Mock") {
                        $issues += @{
                            File = $relativePath
                            Type = "Content"
                            Severity = "Suggestion"
                            Message = "Business class '$className' should consider cultural context"
                            Line = ($content.Substring(0, $classMatch.Index) -split "`n").Count
                            Suggestion = "Add CulturalContext parameter or property for Sri Lankan cultural awareness"
                        }
                    }
                }
            }
        }
    }
    
    $Global:ValidationResults.Categories.Content.Issues = $issues
    $Global:ValidationResults.Categories.Content.Status = if ($issues.Count -eq 0) { "Pass" } else { "Issues Found" }
    
    Write-Log "Content validation: $($issues.Count) issues found" "INFO"
    return $issues
}

function Test-LocalizationSupport {
    Write-Log "Validating localization infrastructure and multi-language support" "INFO"
    
    $issues = @()
    
    # Check for localization infrastructure
    $requiredLocalizationFiles = @(
        "Resources/SharedResource.resx",
        "Resources/SharedResource.si-LK.resx", 
        "Resources/SharedResource.ta-LK.resx",
        "appsettings.json"
    )
    
    foreach ($file in $requiredLocalizationFiles) {
        $fullPath = Join-Path $ProjectRoot "src" $file
        if (!(Test-Path $fullPath)) {
            $issues += @{
                File = $file
                Type = "Localization"
                Severity = if ($file -match "si-LK|ta-LK") { "Critical" } else { "Warning" }
                Message = "Missing localization file"
                Suggestion = "Create localization resource file for $(if ($file -match 'si-LK') { 'Sinhala' } elseif ($file -match 'ta-LK') { 'Tamil' } else { 'shared resources' })"
            }
        }
    }
    
    # Check appsettings.json for culture configuration
    $appsettingsPath = Join-Path $ProjectRoot "src/LankaConnect.API/appsettings.json"
    if (Test-Path $appsettingsPath) {
        $appsettingsContent = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        
        # Check for supported cultures
        $expectedCultures = @("en-US", "si-LK", "ta-LK")
        if (!$appsettingsContent.Localization -or !$appsettingsContent.Localization.SupportedCultures) {
            $issues += @{
                File = "appsettings.json"
                Type = "Localization"
                Severity = "Critical"
                Message = "Missing supported cultures configuration"
                Suggestion = "Add Localization section with SupportedCultures: [$($expectedCultures -join ', ')]"
            }
        }
        
        # Check for default culture
        if ($appsettingsContent.Localization.DefaultCulture -eq "en-US") {
            $issues += @{
                File = "appsettings.json"
                Type = "Localization"
                Severity = "Warning"
                Message = "Default culture is English - consider si-LK for Sri Lankan context"
                Suggestion = "Set DefaultCulture to 'si-LK' or provide culture selection mechanism"
            }
        }
    }
    
    # Check for IStringLocalizer usage
    $sourcePaths = @("src")
    $localizerUsageFound = $false
    
    foreach ($sourcePath in $sourcePaths) {
        $fullPath = Join-Path $ProjectRoot $sourcePath
        if (!(Test-Path $fullPath)) { continue }
        
        $csharpFiles = Get-ChildItem -Path $fullPath -Include "*.cs" -Recurse
        
        foreach ($file in $csharpFiles) {
            $content = Get-Content $file.FullName -Raw
            if ($content -match "IStringLocalizer") {
                $localizerUsageFound = $true
                break
            }
        }
        
        if ($localizerUsageFound) { break }
    }
    
    if (!$localizerUsageFound) {
        $issues += @{
            File = "General"
            Type = "Localization"
            Severity = "Warning"
            Message = "No IStringLocalizer usage found in codebase"
            Suggestion = "Implement IStringLocalizer for multi-language support in controllers and services"
        }
    }
    
    $Global:ValidationResults.Categories.Localization.Issues = $issues
    $Global:ValidationResults.Categories.Localization.Status = if ($issues.Count -eq 0) { "Pass" } else { "Issues Found" }
    
    Write-Log "Localization validation: $($issues.Count) issues found" "INFO"
    return $issues
}

function Test-BusinessLogicCompliance {
    Write-Log "Validating business logic cultural compliance" "INFO"
    
    $issues = @()
    $businessPaths = @(
        "src/LankaConnect.Domain",
        "src/LankaConnect.Application"
    )
    
    foreach ($businessPath in $businessPaths) {
        $fullPath = Join-Path $ProjectRoot $businessPath
        if (!(Test-Path $fullPath)) { continue }
        
        $csharpFiles = Get-ChildItem -Path $fullPath -Include "*.cs" -Recurse
        
        foreach ($file in $csharpFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Replace($ProjectRoot, "").TrimStart('\')
            
            # Check for Sri Lankan business rules
            $businessRulePatterns = @(
                @{ Pattern = "BusinessHours|WorkingHours"; RequiredContext = "Sri.*Lanka|Colombo|TimeZone.*Asia/Colombo"; Message = "Business hours should consider Sri Lankan context" },
                @{ Pattern = "Currency|Money|Price"; RequiredContext = "LKR|Sri.*Lankan.*Rupee"; Message = "Currency handling should support LKR" },
                @{ Pattern = "Address|Location|Geographic"; RequiredContext = "Province|District|PostalCode"; Message = "Address handling should support Sri Lankan geographic structure" },
                @{ Pattern = "PhoneNumber|ContactNumber"; RequiredContext = "\+94|0\d{9}"; Message = "Phone number validation should support Sri Lankan format" }
            )
            
            foreach ($pattern in $businessRulePatterns) {
                if ($content -match $pattern.Pattern -and $content -notmatch $pattern.RequiredContext) {
                    $matches = [regex]::Matches($content, $pattern.Pattern)
                    foreach ($match in $matches) {
                        $issues += @{
                            File = $relativePath
                            Type = "Business"
                            Severity = "Warning"
                            Message = $pattern.Message
                            Line = ($content.Substring(0, $match.Index) -split "`n").Count
                            Context = $match.Value
                            Suggestion = "Add Sri Lankan cultural context: $($pattern.RequiredContext)"
                        }
                    }
                }
            }
            
            # Check for cultural event integration
            if ($relativePath -match "Event|Calendar|Schedule" -and $content -notmatch "Buddhist|Hindu|Islamic|Christian|Vesak|Diwali|Ramadan|Christmas") {
                $issues += @{
                    File = $relativePath
                    Type = "Business"
                    Severity = "Suggestion"
                    Message = "Event/Calendar classes should consider Sri Lankan cultural events"
                    Suggestion = "Add support for Buddhist, Hindu, Islamic, and Christian events relevant to Sri Lanka"
                }
            }
            
            # Check for diaspora community patterns
            if ($relativePath -match "Community|User|Profile" -and $content -notmatch "Diaspora|Origin|Heritage") {
                $issues += @{
                    File = $relativePath
                    Type = "Business"
                    Severity = "Suggestion"
                    Message = "Community classes could enhance diaspora support"
                    Suggestion = "Consider adding diaspora community tracking and heritage information"
                }
            }
        }
    }
    
    $Global:ValidationResults.Categories.Business.Issues = $issues
    $Global:ValidationResults.Categories.Business.Status = if ($issues.Count -eq 0) { "Pass" } else { "Issues Found" }
    
    Write-Log "Business logic validation: $($issues.Count) issues found" "INFO"
    return $issues
}

function Test-APICompliance {
    Write-Log "Validating API cultural compliance and internationalization" "INFO"
    
    $issues = @()
    $apiPath = Join-Path $ProjectRoot "src/LankaConnect.API"
    
    if (!(Test-Path $apiPath)) {
        $issues += @{
            File = "API Project"
            Type = "API"
            Severity = "Critical"
            Message = "API project not found"
            Suggestion = "Ensure API project exists for cultural intelligence validation"
        }
        
        $Global:ValidationResults.Categories.API.Issues = $issues
        $Global:ValidationResults.Categories.API.Status = "Not Found"
        return $issues
    }
    
    $csharpFiles = Get-ChildItem -Path $apiPath -Include "*.cs" -Recurse
    
    foreach ($file in $csharpFiles) {
        $content = Get-Content $file.FullName -Raw
        $relativePath = $file.FullName.Replace($ProjectRoot, "").TrimStart('\')
        
        # Check controllers for cultural headers
        if ($file.Name -match "Controller") {
            if ($content -notmatch "Accept-Language|CultureInfo|Localization") {
                $issues += @{
                    File = $relativePath
                    Type = "API"
                    Severity = "Warning"
                    Message = "Controller should handle Accept-Language headers"
                    Suggestion = "Add culture/localization support in controller actions"
                }
            }
            
            # Check for hardcoded response messages
            $responseMatches = [regex]::Matches($content, 'return\s+\w+\s*\(\s*"([^"]+)"')
            foreach ($match in $responseMatches) {
                $message = $match.Groups[1].Value
                if ($message.Length -gt 10 -and $message -notmatch "exception|error") {
                    $issues += @{
                        File = $relativePath
                        Type = "API"
                        Severity = "Warning"
                        Message = "Hardcoded response message should be localized"
                        Line = ($content.Substring(0, $match.Index) -split "`n").Count
                        Context = $message
                        Suggestion = "Use IStringLocalizer for response messages"
                    }
                }
            }
        }
        
        # Check for API documentation cultural context
        if ($file.Name -match "Program|Startup") {
            if ($content -notmatch "Swagger.*Culture|OpenAPI.*Localization") {
                $issues += @{
                    File = $relativePath
                    Type = "API"
                    Severity = "Suggestion"
                    Message = "API documentation should include cultural context"
                    Suggestion = "Add Swagger/OpenAPI documentation for cultural intelligence features"
                }
            }
        }
    }
    
    # Check for API endpoint cultural patterns
    $controllerFiles = $csharpFiles | Where-Object { $_.Name -match "Controller" }
    $expectedCulturalEndpoints = @(
        "cultural-context",
        "localization",
        "regional-preferences",
        "community-features"
    )
    
    $foundEndpoints = @()
    foreach ($file in $controllerFiles) {
        $content = Get-Content $file.FullName -Raw
        foreach ($endpoint in $expectedCulturalEndpoints) {
            if ($content -match $endpoint) {
                $foundEndpoints += $endpoint
            }
        }
    }
    
    $missingEndpoints = $expectedCulturalEndpoints | Where-Object { $foundEndpoints -notcontains $_ }
    foreach ($endpoint in $missingEndpoints) {
        $issues += @{
            File = "API Endpoints"
            Type = "API"
            Severity = "Suggestion"
            Message = "Missing cultural intelligence endpoint: $endpoint"
            Suggestion = "Consider adding API endpoint for $endpoint functionality"
        }
    }
    
    $Global:ValidationResults.Categories.API.Issues = $issues
    $Global:ValidationResults.Categories.API.Status = if ($issues.Count -eq 0) { "Pass" } else { "Issues Found" }
    
    Write-Log "API validation: $($issues.Count) issues found" "INFO"
    return $issues
}

function Get-ComplianceScore {
    $totalIssues = 0
    $criticalIssues = 0
    $warnings = 0
    $suggestions = 0
    
    foreach ($category in $Global:ValidationResults.Categories.Values) {
        foreach ($issue in $category.Issues) {
            $totalIssues++
            switch ($issue.Severity) {
                "Critical" { $criticalIssues++ }
                "Warning" { $warnings++ }
                "Suggestion" { $suggestions++ }
            }
        }
    }
    
    $Global:ValidationResults.TotalIssues = $totalIssues
    $Global:ValidationResults.CriticalIssues = $criticalIssues
    $Global:ValidationResults.Warnings = $warnings
    $Global:ValidationResults.Suggestions = $suggestions
    
    # Calculate compliance score (0-100)
    $maxDeductions = 100
    $criticalDeduction = $criticalIssues * 15
    $warningDeduction = $warnings * 5
    $suggestionDeduction = $suggestions * 1
    
    $totalDeductions = $criticalDeduction + $warningDeduction + $suggestionDeduction
    $complianceScore = [Math]::Max(0, $maxDeductions - $totalDeductions)
    
    $Global:ValidationResults.ComplianceScore = $complianceScore
    
    return $complianceScore
}

function New-Recommendations {
    $recommendations = @()
    
    # High-level recommendations based on findings
    if ($Global:ValidationResults.CriticalIssues -gt 0) {
        $recommendations += "Address $($Global:ValidationResults.CriticalIssues) critical cultural compliance issues immediately"
    }
    
    if ($Global:ValidationResults.CulturalFeatures.Count -lt 3) {
        $recommendations += "Implement more cultural intelligence features (currently: $($Global:ValidationResults.CulturalFeatures.Count))"
    }
    
    # Category-specific recommendations
    $namingIssues = $Global:ValidationResults.Categories.Naming.Issues | Where-Object { $_.Severity -eq "Critical" }
    if ($namingIssues.Count -gt 0) {
        $recommendations += "Review and fix cultural naming conventions in $($namingIssues.Count) locations"
    }
    
    $localizationIssues = $Global:ValidationResults.Categories.Localization.Issues
    if ($localizationIssues.Count -gt 5) {
        $recommendations += "Implement comprehensive localization infrastructure for Sinhala and Tamil support"
    }
    
    $businessIssues = $Global:ValidationResults.Categories.Business.Issues | Where-Object { $_.Severity -ne "Suggestion" }
    if ($businessIssues.Count -gt 0) {
        $recommendations += "Update business logic to incorporate Sri Lankan cultural context"
    }
    
    # Compliance score recommendations
    if ($Global:ValidationResults.ComplianceScore -lt 60) {
        $recommendations += "Cultural compliance score is low ($($Global:ValidationResults.ComplianceScore)%) - immediate action required"
    } elseif ($Global:ValidationResults.ComplianceScore -lt 80) {
        $recommendations += "Cultural compliance score is moderate ($($Global:ValidationResults.ComplianceScore)%) - improvements recommended"
    }
    
    $Global:ValidationResults.Recommendations = $recommendations
    return $recommendations
}

function Export-ValidationReport {
    param([string]$Format = "JSON")
    
    $reportDir = Join-Path $PSScriptRoot "reports"
    if (!(Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    
    switch ($Format.ToUpper()) {
        "JSON" {
            $reportPath = Join-Path $reportDir "cultural-validation-$timestamp.json"
            $Global:ValidationResults | ConvertTo-Json -Depth 10 | Set-Content -Path $reportPath
        }
        "CSV" {
            $reportPath = Join-Path $reportDir "cultural-validation-$timestamp.csv"
            $csvData = @()
            foreach ($categoryName in $Global:ValidationResults.Categories.Keys) {
                $category = $Global:ValidationResults.Categories[$categoryName]
                foreach ($issue in $category.Issues) {
                    $csvData += [PSCustomObject]@{
                        Category = $categoryName
                        File = $issue.File
                        Severity = $issue.Severity
                        Message = $issue.Message
                        Line = $issue.Line
                        Suggestion = $issue.Suggestion
                    }
                }
            }
            $csvData | Export-Csv -Path $reportPath -NoTypeInformation
        }
        "HTML" {
            $reportPath = Join-Path $reportDir "cultural-validation-$timestamp.html"
            $htmlContent = New-HTMLReport
            Set-Content -Path $reportPath -Value $htmlContent
        }
    }
    
    Write-Log "Cultural validation report exported to: $reportPath" "SUCCESS"
    return $reportPath
}

function New-HTMLReport {
    $complianceColor = if ($Global:ValidationResults.ComplianceScore -ge 80) { "green" } elseif ($Global:ValidationResults.ComplianceScore -ge 60) { "orange" } else { "red" }
    
    $html = @"
<!DOCTYPE html>
<html>
<head>
    <title>LankaConnect Cultural Intelligence Validation Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background: #f4f4f4; padding: 20px; border-radius: 5px; }
        .score { font-size: 24px; font-weight: bold; color: $complianceColor; }
        .category { margin: 20px 0; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }
        .critical { background: #ffe6e6; }
        .warning { background: #fff4e6; }
        .suggestion { background: #e6f3ff; }
        .issue { margin: 10px 0; padding: 10px; border-left: 3px solid #ccc; }
    </style>
</head>
<body>
    <div class="header">
        <h1>🇱🇰 LankaConnect Cultural Intelligence Validation Report</h1>
        <p><strong>Generated:</strong> $($Global:ValidationResults.Timestamp)</p>
        <p><strong>Compliance Score:</strong> <span class="score">$($Global:ValidationResults.ComplianceScore)%</span></p>
        <p><strong>Total Issues:</strong> $($Global:ValidationResults.TotalIssues) (Critical: $($Global:ValidationResults.CriticalIssues), Warnings: $($Global:ValidationResults.Warnings), Suggestions: $($Global:ValidationResults.Suggestions))</p>
    </div>
"@
    
    foreach ($categoryName in $Global:ValidationResults.Categories.Keys) {
        $category = $Global:ValidationResults.Categories[$categoryName]
        $html += @"
    <div class="category">
        <h2>$categoryName Validation ($($category.Issues.Count) issues)</h2>
"@
        
        foreach ($issue in $category.Issues) {
            $cssClass = $issue.Severity.ToLower()
            $html += @"
        <div class="issue $cssClass">
            <strong>$($issue.Severity):</strong> $($issue.Message)<br>
            <em>File:</em> $($issue.File)<br>
            <em>Suggestion:</em> $($issue.Suggestion)
        </div>
"@
        }
        
        $html += "    </div>"
    }
    
    $html += @"
    <div class="category">
        <h2>Recommendations</h2>
        <ul>
"@
    
    foreach ($recommendation in $Global:ValidationResults.Recommendations) {
        $html += "            <li>$recommendation</li>"
    }
    
    $html += @"
        </ul>
    </div>
</body>
</html>
"@
    
    return $html
}

function Write-ValidationSummary {
    Write-ColoredOutput "`n=== CULTURAL INTELLIGENCE VALIDATION SUMMARY ===" "Info"
    Write-ColoredOutput "Validation Scope: $ValidationScope" "Info"
    Write-ColoredOutput "Target Language: $Language" "Info"
    Write-ColoredOutput "Compliance Score: $($Global:ValidationResults.ComplianceScore)%" $(if ($Global:ValidationResults.ComplianceScore -ge 80) { "Success" } elseif ($Global:ValidationResults.ComplianceScore -ge 60) { "Warning" } else { "Error" })
    Write-ColoredOutput "Total Issues: $($Global:ValidationResults.TotalIssues)" "Info"
    Write-ColoredOutput "Critical: $($Global:ValidationResults.CriticalIssues)" "Error"
    Write-ColoredOutput "Warnings: $($Global:ValidationResults.Warnings)" "Warning"
    Write-ColoredOutput "Suggestions: $($Global:ValidationResults.Suggestions)" "Info"
    Write-ColoredOutput "Cultural Features: $($Global:ValidationResults.CulturalFeatures.Count)" "Info"
    
    Write-ColoredOutput "`n--- Category Summary ---" "Info"
    foreach ($categoryName in $Global:ValidationResults.Categories.Keys) {
        $category = $Global:ValidationResults.Categories[$categoryName]
        $status = $category.Status
        $color = if ($status -eq "Pass") { "Success" } else { "Warning" }
        Write-ColoredOutput "$categoryName`: $status ($($category.Issues.Count) issues)" $color
    }
    
    if ($Global:ValidationResults.Recommendations.Count -gt 0) {
        Write-ColoredOutput "`n--- Recommendations ---" "Info"
        foreach ($recommendation in $Global:ValidationResults.Recommendations) {
            Write-ColoredOutput "• $recommendation" "Info"
        }
    }
    
    Write-ColoredOutput "=================================================" "Info"
}

# Main execution
try {
    Write-Log "🇱🇰 Cultural Intelligence Validation Started" "INFO"
    
    # Initialize validation
    if (!(Initialize-CulturalValidation)) {
        Write-Log "Cultural validation initialization failed" "ERROR"
        exit 1
    }
    
    # Run validations based on scope
    if ($ValidationScope -eq "All" -or $ValidationScope -eq "Naming") {
        Test-NamingConventions | Out-Null
    }
    
    if ($ValidationScope -eq "All" -or $ValidationScope -eq "Content") {
        Test-ContentLocalization | Out-Null
    }
    
    if ($ValidationScope -eq "All" -or $ValidationScope -eq "Localization") {
        Test-LocalizationSupport | Out-Null
    }
    
    if ($ValidationScope -eq "All" -or $ValidationScope -eq "Business") {
        Test-BusinessLogicCompliance | Out-Null
    }
    
    if ($ValidationScope -eq "All" -or $ValidationScope -eq "API") {
        Test-APICompliance | Out-Null
    }
    
    # Calculate compliance score and generate recommendations
    $complianceScore = Get-ComplianceScore
    $recommendations = New-Recommendations
    
    # Export report if requested
    if ($ExportReport) {
        Export-ValidationReport -Format $ReportFormat | Out-Null
    }
    
    # Write summary
    Write-ValidationSummary
    
    Write-Log "🇱🇰 Cultural Intelligence Validation completed" "SUCCESS"
    Write-Log "Compliance Score: $complianceScore%" "INFO"
    
    # Exit with appropriate code
    if ($Global:ValidationResults.CriticalIssues -gt 0) {
        Write-Log "Critical cultural compliance issues found" "ERROR"
        exit 1
    } elseif ($complianceScore -lt 60) {
        Write-Log "Cultural compliance score below acceptable threshold" "WARN"
        exit 2
    } else {
        exit 0
    }
}
catch {
    Write-Log "Cultural validation failed: $($_.Exception.Message)" "ERROR"
    if ($Verbose) {
        Write-Log "Stack trace: $($_.ScriptStackTrace)" "ERROR"
    }
    exit 1
}