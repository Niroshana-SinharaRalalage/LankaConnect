# LankaConnect Automation Tools

Comprehensive automation suite for LankaConnect's cultural intelligence platform development, supporting Clean Architecture + DDD + TDD methodology with Sri Lankan cultural context validation.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- PowerShell 7+ (for Windows) or Bash (for Linux/macOS)
- Git
- Visual Studio Code or Visual Studio (recommended)

### Validation
Run the automation validation to ensure all scripts are working:

```powershell
# PowerShell (Windows)
.\scripts\validate-automation.ps1 -CheckDependencies -RunExamples

# Bash (Linux/macOS)
bash scripts/bash/validate-automation.sh --check-dependencies --run-examples
```

## 📚 Available Automation Scripts

### 1. TDD Workflow Scripts

#### TDD RED Phase
Creates failing tests following TDD methodology:

```powershell
# PowerShell
.\scripts\tdd-red-phase.ps1 -FeatureName "DiasporaMapping" -Domain "CulturalIntelligence" -Layer "Domain"

# Bash
bash scripts/bash/tdd-red-phase.sh --feature-name "DiasporaMapping" --domain "CulturalIntelligence" --layer "Domain"
```

**Parameters:**
- `FeatureName` (required): Name of the feature to implement
- `Domain`: Target domain (default: "CulturalIntelligence")
- `Layer`: Architecture layer - Domain|Application|Infrastructure|API (default: "Domain")
- `SkipBuild`: Skip build validation before creating tests

#### TDD GREEN Phase
Implements features to make failing tests pass:

```powershell
# PowerShell
.\scripts\tdd-green-phase.ps1 -FeatureName "DiasporaMapping" -Domain "CulturalIntelligence" -Layer "Domain" -AutoRefactor

# Bash
bash scripts/bash/tdd-green-phase.sh --feature-name "DiasporaMapping" --domain "CulturalIntelligence" --layer "Domain" --auto-refactor
```

**Parameters:**
- `FeatureName` (required): Name of the feature to implement
- `Domain`: Target domain
- `Layer`: Architecture layer
- `SkipTests`: Skip test execution
- `AutoRefactor`: Apply automatic code refactoring

### 2. Build and Testing Scripts

#### Incremental Build
Fast incremental builds with cultural intelligence validation:

```powershell
# PowerShell
.\scripts\incremental-build.ps1 -Configuration "Release" -Parallel -MaxCpuCount 4

# Bash
bash scripts/bash/incremental-build.sh --configuration "Release" --parallel --max-cpu-count 4
```

**Parameters:**
- `Configuration`: Debug|Release (default: Debug)
- `SkipTests`: Skip test execution
- `CleanBuild`: Force clean build
- `Parallel`: Enable parallel build
- `ContinuousIntegration`: Continue on failures (CI mode)

#### Component Testing
Individual component and focused testing:

```powershell
.\scripts\component-test.ps1 -Component "Domain" -TestType "Cultural" -CollectCoverage -ParallelExecution
```

**Parameters:**
- `Component`: All|Domain|Application|Infrastructure|Integration (default: All)
- `TestType`: Unit|Integration|Cultural|All (default: All)
- `TestPattern`: Filter tests by pattern
- `CollectCoverage`: Collect code coverage metrics
- `ParallelExecution`: Run tests in parallel

### 3. Cultural Intelligence Validation

#### Cultural Validation
Comprehensive cultural intelligence feature validation:

```powershell
.\scripts\cultural-validation.ps1 -ValidationScope "All" -Language "All" -ExportReport -ReportFormat "HTML"
```

**Parameters:**
- `ValidationScope`: All|Naming|Content|Localization|Business|API (default: All)
- `Language`: Sinhala|Tamil|English|All (default: All)
- `FixIssues`: Attempt to fix identified issues automatically
- `ExportReport`: Generate validation report
- `ReportFormat`: JSON|CSV|HTML (default: JSON)

**Validation Areas:**
- **Naming Conventions**: Cultural naming patterns and anti-patterns
- **Content Localization**: Multi-language support and hardcoded strings
- **Business Logic**: Sri Lankan cultural context in business rules
- **API Compliance**: Cultural headers and localization support
- **Localization Infrastructure**: Resource files and culture configuration

### 4. Quality Gates

#### Quality Gate Validation
Comprehensive quality validation for different environments:

```powershell
.\scripts\quality-gate.ps1 -Environment "Production" -QualityLevel "Release" -GenerateReport -ReportFormat "HTML"
```

**Parameters:**
- `Environment`: Development|Staging|Production (default: Development)
- `QualityLevel`: Basic|Standard|Comprehensive|Release (default: Standard)
- `BreakOnFailure`: Break build on quality gate failures
- `GenerateReport`: Export quality gate report

**Quality Gates:**
- **Build Gate**: Compilation and build validation
- **Test Gate**: Unit, integration, and cultural tests
- **Coverage Gate**: Code coverage requirements
- **Cultural Gate**: Cultural intelligence compliance
- **Security Gate**: Security best practices validation
- **Performance Gate**: Performance anti-patterns detection
- **Documentation Gate**: Code and API documentation
- **Architecture Gate**: Clean Architecture compliance

## 🎯 Quality Thresholds by Environment

### Development Environment
- **Basic**: 80% test pass rate, 70% coverage, 60% cultural compliance
- **Standard**: 85% test pass rate, 80% coverage, 75% cultural compliance  
- **Comprehensive**: 90% test pass rate, 85% coverage, 85% cultural compliance

### Staging Environment
- **Standard**: 90% test pass rate, 85% coverage, 80% cultural compliance
- **Comprehensive**: 95% test pass rate, 90% coverage, 90% cultural compliance

### Production Environment
- **Release**: 98% test pass rate, 95% coverage, 95% cultural compliance, 90% performance, 85% documentation

## 🇱🇰 Cultural Intelligence Features

### Required Features by Environment

#### Development
- Multi-language support foundation
- Basic cultural context handling
- Sri Lankan timezone support

#### Staging
- Currency handling (LKR)
- Province/district awareness
- Cultural event integration

#### Production
- Diaspora community features
- Cultural intelligence analytics
- Localization completeness
- Cultural compliance validation

### Cultural Patterns Detected
- `CulturalContext` and `CultureService` implementations
- Sri Lankan language support (Sinhala, Tamil, English)
- Geographic region handling (provinces, districts)
- Cultural event integration (Buddhist, Hindu, Islamic, Christian)
- Diaspora community mapping
- Localization infrastructure

## 🔧 Utility Functions

### Shared Utilities (PowerShell)
Located in `scripts/automation/shared-utilities.ps1`:

```powershell
# Import utilities
. .\scripts\automation\shared-utilities.ps1

# Available functions
Write-Log "Message" "INFO"                    # Colored logging
Test-ProjectStructure                         # Validate project structure
Test-CulturalFeatures                         # Detect cultural features
Invoke-BuildValidation                        # Build validation
Invoke-TestExecution                          # Test execution with metrics
Get-QualityMetrics                           # Collect quality metrics
Measure-PerformanceMetrics                   # Performance monitoring
New-AutomationReport                         # Generate JSON reports
```

### Shared Utilities (Bash)
Located in `scripts/bash/shared-utilities.sh`:

```bash
# Source utilities
source scripts/bash/shared-utilities.sh

# Available functions
write_log "Message" "INFO"                   # Colored logging
test_project_structure                       # Validate project structure  
test_cultural_features                       # Detect cultural features
invoke_build_validation                      # Build validation
invoke_test_execution                        # Test execution with metrics
get_quality_metrics                          # Collect quality metrics
measure_performance_metrics                  # Performance monitoring
new_automation_report                        # Generate JSON reports
```

## 📊 Reports and Logging

### Report Generation
All scripts generate detailed JSON reports in `scripts/reports/`:

- `red-phase-YYYYMMDD-HHMMSS.json` - TDD RED phase results
- `green-phase-YYYYMMDD-HHMMSS.json` - TDD GREEN phase results  
- `incremental-build-YYYYMMDD-HHMMSS.json` - Build results and metrics
- `cultural-validation-YYYYMMDD-HHMMSS.json` - Cultural compliance report
- `component-test-YYYYMMDD-HHMMSS.json` - Component testing results
- `quality-gate-YYYYMMDD-HHMMSS.json` - Quality gate validation report

### Logging
Detailed logs are stored in `scripts/logs/automation.log` with:
- Timestamp and log level
- Colored console output  
- Error tracking and stack traces
- Performance metrics

## 🚦 CI/CD Integration

### GitHub Actions Example
```yaml
name: LankaConnect Quality Gates

on: [push, pull_request]

jobs:
  quality-gates:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0'
    
    - name: Incremental Build
      run: bash scripts/bash/incremental-build.sh --configuration Release --continuous-integration
    
    - name: Cultural Validation
      run: |
        pwsh scripts/cultural-validation.ps1 -ValidationScope All -ExportReport -ReportFormat JSON
    
    - name: Quality Gates
      run: |
        pwsh scripts/quality-gate.ps1 -Environment Staging -QualityLevel Standard -BreakOnFailure
```

### Azure DevOps Example
```yaml
trigger:
- main
- develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '8.0.x'

- script: bash scripts/bash/incremental-build.sh --configuration $(buildConfiguration) --continuous-integration
  displayName: 'Incremental Build'

- task: PowerShell@2
  inputs:
    filePath: 'scripts/cultural-validation.ps1'
    arguments: '-ValidationScope All -ExportReport -ReportFormat JSON'
  displayName: 'Cultural Intelligence Validation'

- task: PowerShell@2
  inputs:
    filePath: 'scripts/quality-gate.ps1'
    arguments: '-Environment $(environment) -QualityLevel $(qualityLevel) -GenerateReport'
  displayName: 'Quality Gate Validation'
```

## 🛠️ Development Workflow

### Daily Development
1. **Feature Development**: Use TDD RED-GREEN cycle
2. **Incremental Builds**: Fast validation during development
3. **Cultural Validation**: Ensure Sri Lankan context compliance
4. **Component Testing**: Focused testing by layer

### Pre-commit Hooks
```bash
#!/bin/bash
# .git/hooks/pre-commit
set -e

echo "Running pre-commit quality checks..."

# Incremental build with tests
bash scripts/bash/incremental-build.sh --configuration Debug

# Cultural validation
pwsh scripts/cultural-validation.ps1 -ValidationScope All

# Quality gate for development
pwsh scripts/quality-gate.ps1 -Environment Development -QualityLevel Basic
```

### Release Preparation
```powershell
# Full quality validation for production release
.\scripts\quality-gate.ps1 -Environment Production -QualityLevel Release -GenerateReport -ReportFormat HTML

# Comprehensive cultural validation
.\scripts\cultural-validation.ps1 -ValidationScope All -Language All -ExportReport -ReportFormat HTML

# Full component testing with coverage
.\scripts\component-test.ps1 -Component All -TestType All -CollectCoverage -ParallelExecution
```

## 📋 Best Practices

### Script Usage
- Always run `validate-automation.ps1` after setup
- Use incremental builds during development
- Run cultural validation regularly
- Execute quality gates before commits
- Generate reports for documentation

### Error Handling
- Scripts provide colored output and detailed logging
- Exit codes: 0 (success), 1 (error), 2 (warnings)
- Comprehensive error reporting with suggestions
- Automatic issue detection and recommendations

### Performance Optimization
- Use parallel execution for builds and tests
- Leverage incremental build caching
- Enable maximum CPU utilization for builds
- Cultural validation caching for repeated runs

## 🐛 Troubleshooting

### Common Issues

#### Build Failures
```powershell
# Clean and rebuild
.\scripts\incremental-build.ps1 -CleanBuild -Configuration Debug -Verbose

# Check project structure
.\scripts\validate-automation.ps1 -CheckDependencies -Verbose
```

#### Test Failures
```powershell
# Run specific component tests
.\scripts\component-test.ps1 -Component Domain -TestType Unit -DetailedOutput

# Check cultural tests specifically
.\scripts\component-test.ps1 -Component All -TestType Cultural -Verbose
```

#### Cultural Validation Issues
```powershell
# Detailed cultural analysis
.\scripts\cultural-validation.ps1 -ValidationScope All -DetailedReport -ExportReport

# Fix issues automatically (where possible)
.\scripts\cultural-validation.ps1 -ValidationScope All -FixIssues
```

### Debug Mode
Add `-Verbose` flag to any PowerShell script for detailed debugging:
```powershell
.\scripts\quality-gate.ps1 -Environment Development -QualityLevel Basic -Verbose
```

## 📝 Contributing

### Adding New Scripts
1. Follow the established parameter patterns
2. Use shared utilities for common operations
3. Include comprehensive error handling
4. Add validation to `validate-automation.ps1`
5. Update this README with usage examples

### Cultural Intelligence Enhancements
1. Add new cultural patterns to detection logic
2. Update compliance rules for Sri Lankan context
3. Expand localization support
4. Enhance diaspora community features

## 📄 License

This automation suite is part of the LankaConnect project and follows the same licensing terms.

---

**LankaConnect Automation Suite** - Empowering Sri Lankan cultural intelligence through automated quality assurance.