# TDD Systematic Type Consolidation Blueprint

**Purpose:** Detailed Test-Driven Development methodology for eliminating CS0104 namespace ambiguities with zero compilation tolerance and cultural intelligence preservation.

## TDD Philosophy for Architectural Refactoring

### Core Principles
1. **Red-Green-Refactor** applied to architectural changes
2. **Test-First** approach ensures behavior preservation
3. **Incremental Consolidation** maintains system stability
4. **Cultural Intelligence Protection** through comprehensive test coverage
5. **Zero Compilation Tolerance** enforced at every step

## TDD Workflow for Type Consolidation

### Phase 1: RED - Create Failing Tests

#### Step 1: Behavior Capture Tests
```csharp
[TestFixture]
public class AlertSeverityConsolidationTests
{
    [Test]
    [Category("Behavioral Baseline")]
    public void AlertSeverity_ExistingBehaviorCapture()
    {
        // EXPECTED TO FAIL - demonstrates current ambiguity
        // This test documents existing behavior before consolidation

        // Test all current usage patterns
        AssertCurrentBehavior_DomainCommon();
        AssertCurrentBehavior_ValueObjects();
        AssertCurrentBehavior_Database();
    }

    private void AssertCurrentBehavior_DomainCommon()
    {
        // Capture behavior from Domain.Common.AlertSeverity
        var severity = Domain.Common.AlertSeverity.Critical; // CS0104 expected
        Assert.That(severity, Is.EqualTo(4));
    }

    private void AssertCurrentBehavior_ValueObjects()
    {
        // Capture behavior from Domain.Common.ValueObjects.AlertSeverity
        var severity = Domain.Common.ValueObjects.AlertSeverity.SacredEventCritical;
        Assert.That(severity, Is.EqualTo(10)); // Cultural intelligence value
    }

    private void AssertCurrentBehavior_Database()
    {
        // Capture behavior from Domain.Common.Database.AlertSeverity
        var severity = Domain.Common.Database.AlertSeverity.Emergency;
        Assert.That(severity, Is.EqualTo(4));
    }
}
```

#### Step 2: Cultural Intelligence Protection Tests
```csharp
[TestFixture]
[Category("Cultural Intelligence")]
public class CulturalIntelligencePreservationTests
{
    [Test]
    public void AlertSeverity_SacredEventHandling_ShouldBePreserved()
    {
        // RED: This test will fail initially due to CS0104

        // Test cultural intelligence requirements
        var sacredEventSeverity = AlertSeverity.SacredEventCritical; // CS0104 expected

        // Sacred events require highest priority
        Assert.That((int)sacredEventSeverity, Is.EqualTo(10));
        Assert.That(sacredEventSeverity > AlertSeverity.Critical, Is.True);

        // Cultural context validation
        Assert.That(IsculturallySignificant(sacredEventSeverity), Is.True);
    }

    [Test]
    public void CulturalPerformanceThreshold_SacredProtection_ShouldBePreserved()
    {
        // RED: Testing cultural performance threshold consolidation

        var sacred = CulturalPerformanceThreshold.Sacred; // CS0104 expected
        var religious = CulturalPerformanceThreshold.Religious;

        // Sacred events have highest priority
        Assert.That((int)sacred, Is.EqualTo(10));
        Assert.That(sacred > religious, Is.True);

        // Business rule: Sacred events trigger special alerting
        var alertLevel = DetermineAlertLevel(sacred);
        Assert.That(alertLevel, Is.EqualTo(AlertSeverity.SacredEventCritical));
    }

    private bool IsculturallySignificant(AlertSeverity severity)
    {
        return severity == AlertSeverity.SacredEventCritical;
    }

    private AlertSeverity DetermineAlertLevel(CulturalPerformanceThreshold threshold)
    {
        return threshold == CulturalPerformanceThreshold.Sacred
            ? AlertSeverity.SacredEventCritical
            : AlertSeverity.High;
    }
}
```

#### Step 3: Cross-Layer Integration Tests
```csharp
[TestFixture]
[Category("Integration")]
public class CrossLayerCompatibilityTests
{
    [Test]
    public void AlertSeverity_DomainToApplicationMapping_ShouldWork()
    {
        // RED: Test domain → application layer compatibility

        var domainSeverity = AlertSeverity.SacredEventCritical; // CS0104 expected
        var applicationDto = MapToApplicationDto(domainSeverity);

        Assert.That(applicationDto.Severity, Is.EqualTo("SacredEventCritical"));
        Assert.That(applicationDto.Level, Is.EqualTo(10));
        Assert.That(applicationDto.IsCultural, Is.True);
    }

    [Test]
    public void AlertSeverity_DatabaseSerialization_ShouldWork()
    {
        // RED: Test domain → infrastructure serialization

        var severity = AlertSeverity.SacredEventCritical; // CS0104 expected
        var serialized = SerializeForDatabase(severity);
        var deserialized = DeserializeFromDatabase(serialized);

        Assert.That(deserialized, Is.EqualTo(severity));
        Assert.That((int)deserialized, Is.EqualTo(10));
    }

    private AlertSeverityDto MapToApplicationDto(AlertSeverity severity)
    {
        return new AlertSeverityDto
        {
            Severity = severity.ToString(),
            Level = (int)severity,
            IsCultural = severity == AlertSeverity.SacredEventCritical
        };
    }

    private string SerializeForDatabase(AlertSeverity severity) => ((int)severity).ToString();
    private AlertSeverity DeserializeFromDatabase(string data) => (AlertSeverity)int.Parse(data);
}

public class AlertSeverityDto
{
    public string Severity { get; set; }
    public int Level { get; set; }
    public bool IsCultural { get; set; }
}
```

### Phase 2: GREEN - Fix Compilation

#### Step 1: Create Canonical Type Definition
```csharp
// File: Domain/Common/ValueObjects/AlertSeverity.cs
namespace LankaConnect.Domain.Common.ValueObjects
{
    /// <summary>
    /// CANONICAL alert severity definition with cultural intelligence support.
    /// Consolidates all AlertSeverity definitions into single source of truth.
    /// </summary>
    public enum AlertSeverity
    {
        /// <summary>Low priority alerts</summary>
        Low = 1,

        /// <summary>Medium priority alerts</summary>
        Medium = 2,

        /// <summary>High priority alerts requiring attention</summary>
        High = 3,

        /// <summary>Critical alerts requiring immediate action</summary>
        Critical = 4,

        /// <summary>
        /// Sacred event critical alerts - highest priority for cultural events.
        /// Used by cultural intelligence system for sacred event protection.
        /// </summary>
        SacredEventCritical = 10,

        /// <summary>Community impacting alerts</summary>
        CommunityImpacting = 8,

        /// <summary>Revenue protection alerts</summary>
        RevenueProtection = 9,

        // Legacy compatibility (marked obsolete for migration)
        [Obsolete("Use Critical instead", false)]
        Emergency = Critical,

        [Obsolete("Use Low instead", false)]
        Info = Low,

        [Obsolete("Use Medium instead", false)]
        Warning = Medium
    }

    /// <summary>
    /// Extension methods for AlertSeverity with cultural intelligence support
    /// </summary>
    public static class AlertSeverityExtensions
    {
        /// <summary>
        /// Determines if the severity is culturally significant
        /// </summary>
        public static bool IsCulturallySignificant(this AlertSeverity severity)
        {
            return severity == AlertSeverity.SacredEventCritical;
        }

        /// <summary>
        /// Gets the priority level for processing order
        /// </summary>
        public static int GetPriorityLevel(this AlertSeverity severity)
        {
            return (int)severity;
        }

        /// <summary>
        /// Determines if severity requires immediate executive attention
        /// </summary>
        public static bool RequiresExecutiveAttention(this AlertSeverity severity)
        {
            return severity >= AlertSeverity.Critical ||
                   severity == AlertSeverity.SacredEventCritical;
        }
    }
}
```

#### Step 2: Progressive Reference Updates
```csharp
// Strategy: Use temporary using aliases during transition

// File: Application/Common/Interfaces/ICulturalIntelligenceMetricsService.cs
using AlertSeverity = LankaConnect.Domain.Common.ValueObjects.AlertSeverity;

namespace LankaConnect.Application.Common.Interfaces
{
    public interface ICulturalIntelligenceMetricsService
    {
        // Now uses canonical AlertSeverity - no more CS0104
        Task<CulturalMetric> EvaluatePerformanceAsync(
            double value,
            PerformanceThreshold threshold);

        // Cultural intelligence specific method
        AlertSeverity DetermineCulturalSeverity(CulturalPerformanceThreshold threshold);
    }
}
```

#### Step 3: Remove Duplicate Definitions
```bash
# Systematically remove duplicate type definitions

# 1. Remove from Domain.Common (keep only in ValueObjects)
# Delete: Domain/Common/AlertSeverity.cs

# 2. Remove from Domain.Common.Database
# Delete: Domain/Common/Database/DatabaseMonitoringModels.cs (AlertSeverity enum only)

# 3. Update all using statements
find . -name "*.cs" -exec sed -i 's/using.*Domain\.Common\.Database.*AlertSeverity/using AlertSeverity = LankaConnect.Domain.Common.ValueObjects.AlertSeverity/' {} \;
```

#### Step 4: Validate Compilation
```bash
# After each removal, ensure compilation succeeds
dotnet build --verbosity minimal --no-restore

# Ensure specific CS0104 errors are resolved
dotnet build 2>&1 | grep "CS0104.*AlertSeverity" && echo "FAILED: CS0104 still exists" || echo "SUCCESS: CS0104 resolved"
```

### Phase 3: REFACTOR - Optimize and Clean

#### Step 1: Remove Temporary Aliases
```csharp
// Remove temporary using aliases once all references updated
// File: Application/Common/Interfaces/ICulturalIntelligenceMetricsService.cs

// Remove this line after global update:
// using AlertSeverity = LankaConnect.Domain.Common.ValueObjects.AlertSeverity;

namespace LankaConnect.Application.Common.Interfaces
{
    // Add proper using at top of file
    using LankaConnect.Domain.Common.ValueObjects;

    public interface ICulturalIntelligenceMetricsService
    {
        // Clean reference without alias
        Task<CulturalMetric> EvaluatePerformanceAsync(
            double value,
            PerformanceThreshold threshold);

        AlertSeverity DetermineCulturalSeverity(CulturalPerformanceThreshold threshold);
    }
}
```

#### Step 2: Optimize Domain Model
```csharp
// Enhance canonical type with additional cultural intelligence features
namespace LankaConnect.Domain.Common.ValueObjects
{
    public enum AlertSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        CommunityImpacting = 8,
        RevenueProtection = 9,
        SacredEventCritical = 10
    }

    /// <summary>
    /// Cultural intelligence alert severity with enhanced capabilities
    /// </summary>
    public class CulturalAlertSeverity : ValueObject
    {
        public AlertSeverity Severity { get; }
        public string CulturalContext { get; }
        public DateTime EventTime { get; }
        public string Region { get; }

        private CulturalAlertSeverity(AlertSeverity severity, string culturalContext,
            DateTime eventTime, string region)
        {
            Severity = severity;
            CulturalContext = culturalContext;
            EventTime = eventTime;
            Region = region;
        }

        public static CulturalAlertSeverity Create(AlertSeverity severity,
            string culturalContext, DateTime eventTime, string region)
        {
            return new CulturalAlertSeverity(severity, culturalContext, eventTime, region);
        }

        public bool IsActiveDuringSacredEvent()
        {
            return Severity == AlertSeverity.SacredEventCritical &&
                   !string.IsNullOrEmpty(CulturalContext);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Severity;
            yield return CulturalContext;
            yield return EventTime;
            yield return Region;
        }
    }
}
```

#### Step 3: Update Tests for Clean Implementation
```csharp
[TestFixture]
public class AlertSeverityConsolidationTests
{
    [Test]
    [Category("Consolidated Behavior")]
    public void AlertSeverity_ConsolidatedBehavior_ShouldWork()
    {
        // GREEN: Now tests pass with consolidated type

        // Test basic severity levels
        Assert.That((int)AlertSeverity.Low, Is.EqualTo(1));
        Assert.That((int)AlertSeverity.Critical, Is.EqualTo(4));

        // Test cultural intelligence feature
        Assert.That((int)AlertSeverity.SacredEventCritical, Is.EqualTo(10));
        Assert.That(AlertSeverity.SacredEventCritical.IsCulturallySignificant(), Is.True);

        // Test priority ordering
        Assert.That(AlertSeverity.SacredEventCritical > AlertSeverity.Critical, Is.True);
    }

    [Test]
    public void CulturalAlertSeverity_EnhancedFeatures_ShouldWork()
    {
        // REFACTOR: Test enhanced cultural intelligence features

        var culturalAlert = CulturalAlertSeverity.Create(
            AlertSeverity.SacredEventCritical,
            "Vesak Full Moon Poya Day",
            DateTime.UtcNow,
            "Sri Lanka"
        );

        Assert.That(culturalAlert.IsActiveDuringSacredEvent(), Is.True);
        Assert.That(culturalAlert.Severity, Is.EqualTo(AlertSeverity.SacredEventCritical));
        Assert.That(culturalAlert.CulturalContext, Is.EqualTo("Vesak Full Moon Poya Day"));
    }
}
```

## TDD Implementation Sequence by Priority

### Priority 0 (Week 1) - Critical Types

#### AlertSeverity Consolidation
```bash
# Day 1-2: AlertSeverity
1. Create failing tests for all current AlertSeverity usages
2. Implement canonical AlertSeverity in Domain.Common.ValueObjects
3. Progressive reference updates with using aliases
4. Remove duplicate definitions
5. Clean up aliases and optimize

# Validation: Zero CS0104 for AlertSeverity
dotnet build 2>&1 | grep "CS0104.*AlertSeverity"
```

#### ComplianceViolation Consolidation
```csharp
// Day 3-4: ComplianceViolation
[TestFixture]
public class ComplianceViolationConsolidationTests
{
    [Test]
    public void ComplianceViolation_MonitoringDomainOwnership()
    {
        // RED: CS0104 between Application.Models and Domain.Monitoring

        var violation = new ComplianceViolation( // CS0104 expected
            "GDPR-2024-001",
            "SLA-Cultural-Events",
            ComplianceViolationType.DataProcessing,
            ViolationSeverity.High,
            "Cultural data processing delay during Diwali",
            "Sacred event data processing impacted",
            TimeSpan.FromMinutes(45)
        );

        // Test domain behavior
        Assert.That(violation.HasCulturalImpact, Is.True);
        Assert.That(violation.RequiresExecutiveAttention, Is.True);
        Assert.That(violation.Severity, Is.EqualTo(ViolationSeverity.High));
    }
}

// GREEN: Implement canonical ComplianceViolation in Domain.Common.Monitoring
namespace LankaConnect.Domain.Common.Monitoring
{
    public class ComplianceViolation // CANONICAL definition
    {
        public string ViolationId { get; private set; }
        public string SLAId { get; private set; }
        public ComplianceViolationType ViolationType { get; private set; }
        public ViolationSeverity Severity { get; private set; }
        public string Description { get; private set; }
        public string? CulturalImpact { get; private set; }
        public TimeSpan ViolationDuration { get; private set; }
        public DateTime OccurredAt { get; private set; }

        public bool HasCulturalImpact => !string.IsNullOrEmpty(CulturalImpact);
        public bool RequiresExecutiveAttention =>
            Severity >= ViolationSeverity.High || HasCulturalImpact;

        // Factory method for cultural violations
        public static ComplianceViolation CreateCulturalViolation(
            string violationId, string slaId, string description,
            string culturalImpact, TimeSpan duration)
        {
            return new ComplianceViolation(violationId, slaId,
                ComplianceViolationType.CulturalEventImpact,
                ViolationSeverity.High, description, culturalImpact, duration);
        }
    }
}
```

#### CulturalPerformanceThreshold Consolidation
```csharp
// Day 5: CulturalPerformanceThreshold
[TestFixture]
public class CulturalPerformanceThresholdConsolidationTests
{
    [Test]
    public void CulturalPerformanceThreshold_SacredEventPriority()
    {
        // RED: CS0104 between ValueObjects and Database

        var sacred = CulturalPerformanceThreshold.Sacred; // CS0104 expected
        var religious = CulturalPerformanceThreshold.Religious;

        // Sacred events must have highest priority
        Assert.That((int)sacred, Is.EqualTo(10));
        Assert.That(sacred > religious, Is.True);

        // Integration with AlertSeverity
        var alertLevel = DetermineAlertLevel(sacred);
        Assert.That(alertLevel, Is.EqualTo(AlertSeverity.SacredEventCritical));
    }
}

// GREEN: Canonical implementation in Domain.Common.ValueObjects
namespace LankaConnect.Domain.Common.ValueObjects
{
    public enum CulturalPerformanceThreshold // CANONICAL definition
    {
        General = 5,
        Regional = 6,
        National = 7,
        Religious = 8,
        Sacred = 10  // Highest priority for sacred events
    }

    public static class CulturalPerformanceThresholdExtensions
    {
        public static AlertSeverity ToAlertSeverity(this CulturalPerformanceThreshold threshold)
        {
            return threshold == CulturalPerformanceThreshold.Sacred
                ? AlertSeverity.SacredEventCritical
                : AlertSeverity.High;
        }

        public static bool RequiresSacredEventProtection(this CulturalPerformanceThreshold threshold)
        {
            return threshold == CulturalPerformanceThreshold.Sacred;
        }
    }
}
```

### Priority 1 (Week 2) - High Impact Types

Follow same TDD pattern for:
- `HistoricalPerformanceData` → `Domain.Common.Database`
- `ConnectionPoolMetrics` → `Domain.Common.Database`
- `CoordinationStrategy` → `Domain.Common`

### Priority 2 (Week 3) - Medium Impact Types

Follow same TDD pattern for:
- `SynchronizationPriority` → `Domain.Common.DisasterRecovery`
- Remaining ambiguous types

## Automated TDD Validation

### Continuous Integration Pipeline
```yaml
# .github/workflows/tdd-type-consolidation.yml
name: TDD Type Consolidation

on: [push, pull_request]

jobs:
  red-phase-validation:
    name: Red Phase - Failing Tests
    runs-on: ubuntu-latest
    if: contains(github.event.head_commit.message, '[RED]')
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3

    - name: Run Red Phase Tests
      run: |
        # Expect tests to fail during RED phase
        dotnet test --filter "Category=Behavioral Baseline" --logger trx || echo "Expected failure during RED phase"

    - name: Validate CS0104 Errors Present
      run: |
        # Ensure CS0104 errors exist (expected during RED phase)
        dotnet build 2>&1 | grep "CS0104" && echo "CS0104 errors detected (expected in RED phase)" || exit 1

  green-phase-validation:
    name: Green Phase - Compilation Success
    runs-on: ubuntu-latest
    if: contains(github.event.head_commit.message, '[GREEN]')
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3

    - name: Validate Zero CS0104 Errors
      run: |
        # Ensure no CS0104 errors during GREEN phase
        dotnet build --verbosity minimal 2>&1 | grep "CS0104" && exit 1 || echo "No CS0104 errors (GREEN phase success)"

    - name: Run All Tests
      run: |
        # All tests must pass during GREEN phase
        dotnet test --no-build --logger trx

  refactor-phase-validation:
    name: Refactor Phase - Optimization
    runs-on: ubuntu-latest
    if: contains(github.event.head_commit.message, '[REFACTOR]')
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3

    - name: Run Full Test Suite
      run: dotnet test --logger trx

    - name: Cultural Intelligence Validation
      run: dotnet test --filter "Category=Cultural Intelligence" --logger trx

    - name: Architecture Compliance
      run: dotnet test tests/LankaConnect.ArchitectureTests/ --logger trx
```

### Local TDD Script
```bash
#!/bin/bash
# scripts/tdd-type-consolidation.sh

TYPE_NAME=$1
PHASE=$2

case $PHASE in
  "red")
    echo "🔴 RED Phase: Creating failing tests for $TYPE_NAME"
    # Create failing tests
    dotnet test --filter "Category=Behavioral Baseline&TestCase=$TYPE_NAME" || echo "Expected failures in RED phase"
    # Verify CS0104 exists
    dotnet build 2>&1 | grep "CS0104.*$TYPE_NAME" && echo "✅ CS0104 detected for $TYPE_NAME"
    ;;

  "green")
    echo "🟢 GREEN Phase: Implementing canonical $TYPE_NAME"
    # Ensure compilation succeeds
    dotnet build --verbosity minimal
    if [ $? -eq 0 ]; then
      echo "✅ Compilation successful"
      # Run tests
      dotnet test --filter "TestCase=$TYPE_NAME"
      if [ $? -eq 0 ]; then
        echo "✅ Tests passing for $TYPE_NAME"
      else
        echo "❌ Tests failing for $TYPE_NAME"
        exit 1
      fi
    else
      echo "❌ Compilation failed"
      exit 1
    fi
    ;;

  "refactor")
    echo "🔄 REFACTOR Phase: Optimizing $TYPE_NAME"
    # Full validation
    dotnet build --verbosity minimal
    dotnet test --no-build
    dotnet test --filter "Category=Cultural Intelligence&TestCase=$TYPE_NAME"
    echo "✅ Refactor complete for $TYPE_NAME"
    ;;

  *)
    echo "Usage: $0 <TYPE_NAME> <red|green|refactor>"
    exit 1
    ;;
esac
```

## Success Metrics and Validation

### Per-Type Success Criteria
```bash
# After each type consolidation:

# 1. Zero CS0104 errors for the type
dotnet build 2>&1 | grep "CS0104.*AlertSeverity" && echo "FAILED" || echo "✅ SUCCESS"

# 2. All tests passing
dotnet test --filter "TestCase=AlertSeverity" --logger trx

# 3. Cultural intelligence preserved
dotnet test --filter "Category=Cultural Intelligence&TestCase=AlertSeverity"

# 4. Performance maintained
dotnet test tests/LankaConnect.PerformanceTests/ --filter "AlertSeverity"
```

### Overall Success Metrics
- **Zero CS0104 compilation errors** across entire solution
- **100% test coverage** for consolidated types
- **Cultural intelligence functionality** fully preserved
- **Performance benchmarks** maintained or improved
- **Clean Architecture compliance** validated through architecture tests

## Cultural Intelligence Preservation Checklist

For each type consolidation, verify:

✅ **Sacred Event Handling** - Special priority levels maintained
✅ **Regional Cultural Rules** - Geographic-specific logic preserved
✅ **Cultural Context Awareness** - Cultural metadata handling intact
✅ **Performance Thresholds** - Cultural performance levels maintained
✅ **Community Impact Assessment** - Community-affecting logic preserved
✅ **Revenue Protection** - Business continuity during cultural events
✅ **Compliance Requirements** - Cultural data protection maintained

This TDD blueprint ensures that the systematic elimination of CS0104 namespace ambiguities is achieved through a disciplined, test-driven approach that preserves all critical cultural intelligence functionality while maintaining zero compilation tolerance throughout the process.