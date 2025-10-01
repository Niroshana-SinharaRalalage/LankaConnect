# Coverage Measurement and Validation Framework

## Executive Summary

This framework establishes comprehensive metrics, tooling, and processes to measure, validate, and maintain 100% domain test coverage while ensuring high-quality, meaningful tests.

## Coverage Metrics Definition

### **1. Primary Coverage Metrics**

#### **Line Coverage** (Target: 100%)
```xml
<!-- Measurement: XPlat Code Coverage -->
<coverage line-rate="1.0" branch-rate="1.0" complexity="0">
  <classes>
    <class name="LankaConnect.Domain.Users.User" 
           line-rate="1.0" 
           branch-rate="1.0"/>
  </classes>
</coverage>
```

#### **Branch Coverage** (Target: 100%)
- All conditional paths (if/else, switch, ternary operators)
- Exception handling paths
- Early returns and guard clauses

#### **Method Coverage** (Target: 100%)
- All public methods tested
- All private methods exercised through public API
- All property getters/setters validated

### **2. Quality Coverage Metrics**

#### **Mutation Score** (Target: >85%)
```csharp
// Example: Mutation testing reveals test quality
// Original code:
if (user.FailedLoginAttempts >= 5)
    return Result.Failure("Account locked");

// Mutated code:
if (user.FailedLoginAttempts > 5)  // Changed >= to >
    return Result.Failure("Account locked");

// Quality test should catch this mutation:
[Test]
public void User_Login_WithExactlyFiveFailedAttempts_ShouldBeLocked()
{
    var user = UserTestBuilder.AUser().WithFailedAttempts(5).Build();
    user.IsAccountLocked.Should().BeTrue(); // Catches the mutation
}
```

#### **Business Rule Coverage** (Target: 100%)
- All domain invariants tested
- All business rules validated
- All error conditions covered

### **3. Architectural Coverage Metrics**

#### **Domain Boundary Compliance** (Target: 100%)
- No infrastructure dependencies in domain tests
- Proper aggregate boundary testing
- Clean separation of concerns validation

## Tooling Configuration

### **1. XPlat Code Coverage Setup**

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>cobertura,opencover,json</CoverletOutputFormat>
    <CoverletOutput>$(MSBuildProjectDirectory)/TestResults/coverage</CoverletOutput>
    <ExcludeByFile>**/Migrations/**/*,**/obj/**/*</ExcludeByFile>
    <ExcludeByAttribute>ExcludeFromCodeCoverage</ExcludeByAttribute>
    <Include>[LankaConnect.Domain]*</Include>
    <Exclude>[*.Tests]*,[*]*.Migrations.*</Exclude>
  </PropertyGroup>
</Project>
```

### **2. PowerShell Coverage Analysis Script**

```powershell
# scripts/measure-coverage.ps1
param(
    [string]$Project = "tests/LankaConnect.Domain.Tests",
    [string]$OutputDir = "TestResults/Coverage",
    [decimal]$MinimumCoverage = 100.0
)

Write-Host "🔍 Running domain tests with coverage analysis..." -ForegroundColor Cyan

# Clean previous results
Remove-Item -Path $OutputDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null

# Run tests with coverage
$testResult = dotnet test $Project `
    --collect:"XPlat Code Coverage" `
    --results-directory $OutputDir `
    --configuration Release `
    --no-build `
    --verbosity minimal `
    --logger "console;verbosity=detailed"

if ($LASTEXITCODE -ne 0) {
    Write-Error "❌ Tests failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Find and analyze coverage files
$coverageFiles = Get-ChildItem -Path $OutputDir -Filter "coverage.cobertura.xml" -Recurse

foreach ($file in $coverageFiles) {
    Write-Host "📊 Analyzing coverage: $($file.FullName)" -ForegroundColor Yellow
    
    # Parse XML coverage report
    [xml]$coverage = Get-Content $file.FullName
    
    $lineCoverage = [decimal]$coverage.coverage.'line-rate' * 100
    $branchCoverage = [decimal]$coverage.coverage.'branch-rate' * 100
    
    Write-Host "📈 Line Coverage: $lineCoverage%" -ForegroundColor $(if($lineCoverage -ge $MinimumCoverage){'Green'}else{'Red'})
    Write-Host "📈 Branch Coverage: $branchCoverage%" -ForegroundColor $(if($branchCoverage -ge $MinimumCoverage){'Green'}else{'Red'})
    
    # Detailed class-level analysis
    $classes = $coverage.coverage.packages.package.classes.class
    $uncoveredClasses = $classes | Where-Object { [decimal]$_.'line-rate' -lt 1.0 }
    
    if ($uncoveredClasses.Count -gt 0) {
        Write-Host "⚠️  Classes needing attention:" -ForegroundColor Yellow
        foreach ($class in $uncoveredClasses) {
            $classLineCoverage = [decimal]$class.'line-rate' * 100
            Write-Host "  - $($class.name): $classLineCoverage%" -ForegroundColor Red
            
            # Show uncovered lines
            $uncoveredLines = $class.lines.line | Where-Object { $_.hits -eq "0" }
            if ($uncoveredLines) {
                Write-Host "    Uncovered lines: $($uncoveredLines.number -join ', ')" -ForegroundColor Red
            }
        }
    }
    
    # Generate HTML report
    dotnet tool run reportgenerator `
        -reports:$($file.FullName) `
        -targetdir:"$OutputDir/html" `
        -reporttypes:Html `
        -title:"LankaConnect Domain Coverage Report"
    
    Write-Host "📄 HTML Report generated: $OutputDir/html/index.htm" -ForegroundColor Green
}

# Validation
if ($lineCoverage -lt $MinimumCoverage -or $branchCoverage -lt $MinimumCoverage) {
    Write-Error "❌ Coverage below minimum threshold of $MinimumCoverage%"
    exit 1
}

Write-Host "✅ All coverage targets met!" -ForegroundColor Green
```

### **3. Mutation Testing Configuration**

```xml
<!-- Mutation testing with Stryker.NET -->
<PackageReference Include="Stryker.Core" Version="3.0.1" PrivateAssets="all" />

<!-- stryker-config.json -->
{
  "stryker-config": {
    "project": "src/LankaConnect.Domain/LankaConnect.Domain.csproj",
    "test-projects": ["tests/LankaConnect.Domain.Tests/LankaConnect.Domain.Tests.csproj"],
    "reporters": ["html", "json", "cleartext"],
    "output-dir": "TestResults/Mutation",
    "threshold-high": 90,
    "threshold-low": 70,
    "threshold-break": 60,
    "mutate": [
      "src/LankaConnect.Domain/**/*.cs"
    ],
    "ignore-methods": [
      "*ToString*",
      "*GetHashCode*",
      "*Equals*"
    ]
  }
}
```

## Coverage Analysis Automation

### **1. GitHub Actions Workflow**

```yaml
# .github/workflows/domain-coverage.yml
name: Domain Test Coverage

on:
  push:
    branches: [ main, develop ]
    paths: [ 'src/LankaConnect.Domain/**', 'tests/LankaConnect.Domain.Tests/**' ]
  pull_request:
    branches: [ main ]
    paths: [ 'src/LankaConnect.Domain/**', 'tests/LankaConnect.Domain.Tests/**' ]

jobs:
  coverage:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build solution
      run: dotnet build --configuration Release --no-restore
      
    - name: Run domain tests with coverage
      run: |
        dotnet test tests/LankaConnect.Domain.Tests \
          --configuration Release \
          --no-build \
          --collect:"XPlat Code Coverage" \
          --results-directory TestResults/Coverage \
          --logger "GitHubActions;summary.includePassedTests=true"
    
    - name: Generate coverage report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:"TestResults/Coverage/**/coverage.cobertura.xml" \
          -targetdir:"TestResults/CoverageReport" \
          -reporttypes:"Html;JsonSummary;MarkdownSummaryGithub" \
          -title:"Domain Coverage Report"
    
    - name: Publish coverage results
      uses: 5monkeys/cobertura-action@master
      with:
        path: TestResults/Coverage/**/coverage.cobertura.xml
        minimum_coverage: 100
        fail_below_threshold: true
        
    - name: Upload coverage reports
      uses: actions/upload-artifact@v3
      with:
        name: domain-coverage-report
        path: TestResults/CoverageReport
        
    - name: Add coverage comment to PR
      if: github.event_name == 'pull_request'
      uses: marocchino/sticky-pull-request-comment@v2
      with:
        recreate: true
        path: TestResults/CoverageReport/SummaryGithub.md
```

### **2. Local Development Script**

```bash
#!/bin/bash
# scripts/run-coverage-analysis.sh

set -e

echo "🔍 Starting comprehensive coverage analysis..."

# Configuration
PROJECT_PATH="tests/LankaConnect.Domain.Tests"
OUTPUT_DIR="TestResults/Coverage"
MIN_COVERAGE=100

# Clean previous results
rm -rf $OUTPUT_DIR
mkdir -p $OUTPUT_DIR

echo "🧪 Running domain tests with coverage..."
dotnet test $PROJECT_PATH \
    --collect:"XPlat Code Coverage" \
    --results-directory $OUTPUT_DIR \
    --configuration Release \
    --no-build \
    --verbosity normal

echo "📊 Generating detailed coverage report..."
dotnet tool install -g dotnet-reportgenerator-globaltool 2>/dev/null || true
reportgenerator \
    -reports:"$OUTPUT_DIR/**/coverage.cobertura.xml" \
    -targetdir:"$OUTPUT_DIR/html" \
    -reporttypes:"Html;JsonSummary;Cobertura" \
    -title:"LankaConnect Domain Coverage Report" \
    -historydir:"$OUTPUT_DIR/history"

# Parse coverage results
COVERAGE_FILE="$OUTPUT_DIR/Summary.json"
if [[ -f $COVERAGE_FILE ]]; then
    LINE_COVERAGE=$(cat $COVERAGE_FILE | jq '.summary.linecoverage')
    BRANCH_COVERAGE=$(cat $COVERAGE_FILE | jq '.summary.branchcoverage')
    
    echo "📈 Line Coverage: ${LINE_COVERAGE}%"
    echo "📈 Branch Coverage: ${BRANCH_COVERAGE}%"
    
    # Check thresholds
    if (( $(echo "$LINE_COVERAGE < $MIN_COVERAGE" | bc -l) )) || \
       (( $(echo "$BRANCH_COVERAGE < $MIN_COVERAGE" | bc -l) )); then
        echo "❌ Coverage below minimum threshold of ${MIN_COVERAGE}%"
        exit 1
    fi
fi

echo "🔬 Running mutation testing (optional - takes longer)..."
read -p "Run mutation tests? (y/N): " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    dotnet tool install -g dotnet-stryker 2>/dev/null || true
    dotnet stryker --config-file stryker-config.json
fi

echo "✅ Coverage analysis complete!"
echo "📄 Open: $OUTPUT_DIR/html/index.htm"
```

## Coverage Gap Analysis

### **1. Automated Gap Detection**

```csharp
// Custom coverage analysis tool
public class CoverageGapAnalyzer
{
    public CoverageGapReport AnalyzeCoverageGaps(string domainAssemblyPath, string testAssemblyPath)
    {
        var domainAssembly = Assembly.LoadFrom(domainAssemblyPath);
        var testAssembly = Assembly.LoadFrom(testAssemblyPath);
        
        var report = new CoverageGapReport();
        
        // Find untested public methods
        var domainTypes = domainAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("LankaConnect.Domain") == true)
            .Where(t => !t.Name.Contains("Tests"));
            
        foreach (var type in domainTypes)
        {
            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => !m.IsSpecialName && m.DeclaringType == type);
                
            foreach (var method in publicMethods)
            {
                var hasTest = HasTestForMethod(testAssembly, type, method);
                if (!hasTest)
                {
                    report.AddGap(new CoverageGap
                    {
                        Type = type.Name,
                        Method = method.Name,
                        Priority = DeterminePriority(type, method)
                    });
                }
            }
        }
        
        return report;
    }
    
    private bool HasTestForMethod(Assembly testAssembly, Type domainType, MethodInfo method)
    {
        var testTypes = testAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Tests"));
            
        var expectedTestMethods = new[]
        {
            $"{domainType.Name}_{method.Name}_",
            $"{method.Name}_When",
            $"{method.Name}_Should"
        };
        
        return testTypes.SelectMany(t => t.GetMethods())
            .Any(tm => expectedTestMethods.Any(pattern => tm.Name.Contains(pattern)));
    }
}
```

### **2. Coverage Monitoring Dashboard**

```csharp
// Real-time coverage monitoring
public class CoverageMonitoringService
{
    public CoverageDashboard GenerateDashboard(string projectPath)
    {
        return new CoverageDashboard
        {
            OverallCoverage = GetOverallCoverage(),
            DomainBreakdown = GetDomainBreakdown(),
            TrendAnalysis = GetCoverageTrend(),
            TopGaps = GetTopCoverageGaps(),
            QualityMetrics = GetQualityMetrics()
        };
    }
    
    private CoverageBreakdown GetDomainBreakdown()
    {
        return new CoverageBreakdown
        {
            Domains = new[]
            {
                new DomainCoverage { Name = "Users", LineCoverage = 98.5m, BranchCoverage = 95.2m },
                new DomainCoverage { Name = "Business", LineCoverage = 89.3m, BranchCoverage = 85.7m },
                new DomainCoverage { Name = "Communications", LineCoverage = 92.1m, BranchCoverage = 88.9m },
                new DomainCoverage { Name = "Events", LineCoverage = 76.8m, BranchCoverage = 72.3m },
                new DomainCoverage { Name = "Community", LineCoverage = 68.4m, BranchCoverage = 65.1m }
            }
        };
    }
}
```

## Quality Gates Integration

### **1. Pre-commit Hooks**

```bash
#!/bin/sh
# .git/hooks/pre-commit

echo "🔍 Running coverage check before commit..."

# Run fast domain tests
dotnet test tests/LankaConnect.Domain.Tests --no-build --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Domain tests failed. Commit blocked."
    exit 1
fi

# Check if coverage-critical files changed
CHANGED_FILES=$(git diff --cached --name-only)
DOMAIN_FILES=$(echo "$CHANGED_FILES" | grep "src/LankaConnect.Domain/")

if [ -n "$DOMAIN_FILES" ]; then
    echo "🔍 Domain files changed. Running coverage analysis..."
    
    # Quick coverage check
    dotnet test tests/LankaConnect.Domain.Tests \
        --collect:"XPlat Code Coverage" \
        --results-directory TestResults/PreCommit \
        --verbosity quiet
        
    # Parse results (simplified)
    COVERAGE_RESULT=$(find TestResults/PreCommit -name "coverage.cobertura.xml" | head -1)
    
    if [ -f "$COVERAGE_RESULT" ]; then
        # Basic coverage validation (could be enhanced with proper XML parsing)
        LINE_RATE=$(grep -o 'line-rate="[^"]*"' "$COVERAGE_RESULT" | head -1 | sed 's/line-rate="//;s/"//')
        
        # Convert to percentage for comparison (simplified)
        if [ "$(echo "$LINE_RATE < 1.0" | bc)" -eq 1 ]; then
            echo "⚠️  Coverage may have decreased. Please run full analysis."
            echo "Continue? (y/N)"
            read -r response
            if [ "$response" != "y" ]; then
                exit 1
            fi
        fi
    fi
    
    echo "✅ Coverage check passed."
fi
```

### **2. CI/CD Pipeline Integration**

```yaml
# Pipeline stage for coverage validation
- stage: CoverageValidation
  jobs:
  - job: DomainCoverage
    steps:
    - script: |
        dotnet test tests/LankaConnect.Domain.Tests \
          --collect:"XPlat Code Coverage" \
          --logger trx \
          --results-directory $(Agent.TempDirectory)
      displayName: 'Run Domain Tests with Coverage'
      
    - script: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:$(Agent.TempDirectory)/**/coverage.cobertura.xml \
          -targetdir:$(Agent.TempDirectory)/CoverageReport \
          -reporttypes:Cobertura \
          -assemblyfilters:+LankaConnect.Domain
      displayName: 'Generate Coverage Report'
      
    - task: PublishCodeCoverageResults@1
      inputs:
        codeCoverageTool: 'Cobertura'
        summaryFileLocation: $(Agent.TempDirectory)/CoverageReport/Cobertura.xml
        failIfCoverageEmpty: true
        
    - script: |
        # Custom validation script
        python scripts/validate-coverage.py \
          --coverage-file $(Agent.TempDirectory)/CoverageReport/Cobertura.xml \
          --minimum-line-coverage 100 \
          --minimum-branch-coverage 100
      displayName: 'Validate Coverage Thresholds'
```

## Reporting and Visualization

### **1. Coverage Report Template**

```html
<!-- coverage-report-template.html -->
<!DOCTYPE html>
<html>
<head>
    <title>LankaConnect Domain Coverage Report</title>
    <style>
        .coverage-high { color: #28a745; }
        .coverage-medium { color: #ffc107; }
        .coverage-low { color: #dc3545; }
        .progress-bar { width: 100%; height: 20px; background: #f0f0f0; }
        .progress-fill { height: 100%; background: linear-gradient(90deg, #dc3545 0%, #ffc107 50%, #28a745 100%); }
    </style>
</head>
<body>
    <h1>Domain Test Coverage Report</h1>
    
    <div class="summary">
        <h2>Overall Summary</h2>
        <div class="metric">
            <span>Line Coverage:</span>
            <div class="progress-bar">
                <div class="progress-fill" style="width: {{LINE_COVERAGE}}%"></div>
            </div>
            <span class="coverage-{{LINE_COVERAGE_CLASS}}">{{LINE_COVERAGE}}%</span>
        </div>
    </div>
    
    <div class="domains">
        <h2>Domain Breakdown</h2>
        {{#DOMAINS}}
        <div class="domain">
            <h3>{{NAME}}</h3>
            <p>Line Coverage: <span class="coverage-{{LINE_CLASS}}">{{LINE_COVERAGE}}%</span></p>
            <p>Branch Coverage: <span class="coverage-{{BRANCH_CLASS}}">{{BRANCH_COVERAGE}}%</span></p>
        </div>
        {{/DOMAINS}}
    </div>
</body>
</html>
```

## Success Metrics and KPIs

### **Coverage Metrics**
- **Line Coverage**: 100% (Zero tolerance for uncovered code)
- **Branch Coverage**: 100% (All decision paths tested)
- **Method Coverage**: 100% (All public methods tested)

### **Quality Metrics**
- **Mutation Score**: >85% (High-quality test assertions)
- **Test Execution Time**: <5 seconds (Fast feedback loop)
- **Coverage Report Generation**: <30 seconds

### **Process Metrics**
- **Coverage Trend**: Consistently 100% over time
- **Gap Detection Time**: <1 minute after code changes
- **Fix Time**: <2 hours for coverage gaps

This comprehensive framework ensures that 100% domain test coverage is not only achieved but maintained through automated monitoring, quality gates, and continuous validation processes.