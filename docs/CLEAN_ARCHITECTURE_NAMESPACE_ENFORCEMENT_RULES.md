# Clean Architecture Namespace Enforcement Rules

**Purpose:** Prevent future CS0104 namespace ambiguity violations through systematic architectural boundaries and automated enforcement.

## Namespace Architecture Principles

### 1. Single Source of Truth (SSOT)
**Rule:** Each concept MUST exist in exactly one namespace location
- ❌ `AlertSeverity` in both `Domain.Common` AND `Domain.Common.ValueObjects`
- ✅ `AlertSeverity` ONLY in `Domain.Common.ValueObjects`

### 2. Namespace Semantic Clarity
**Rule:** Namespace names MUST clearly indicate abstraction level and purpose
```csharp
// ✅ CORRECT - Clear semantic boundaries
Domain.Common.ValueObjects.AlertSeverity          // Value object
Domain.Common.Enums.GeographicRegion              // Enumeration
Domain.Common.Abstractions.IRepository<T>         // Interface abstraction
Domain.Business.Entities.BusinessProfile          // Domain entity
Application.Common.DTOs.BusinessProfileDto        // Data transfer object

// ❌ WRONG - Semantic confusion
Domain.Common.AlertSeverity                       // Ambiguous abstraction level
Domain.Common.Database.AlertSeverity              // Mixed concerns
Application.Models.AlertSeverity                  // Domain concept in application layer
```

### 3. Layer Dependency Direction
**Rule:** Dependencies MUST flow inward according to Clean Architecture
```csharp
// ✅ CORRECT dependency flow
Infrastructure → Application → Domain
Infrastructure → Domain (allowed)
Application → Domain (allowed)

// ❌ WRONG dependency flow
Domain → Application (FORBIDDEN)
Domain → Infrastructure (FORBIDDEN)
Application → Infrastructure (FORBIDDEN - use abstractions)
```

## Namespace Hierarchy Standards

### Domain Layer Namespaces

#### Core Domain (`LankaConnect.Domain.Common`)
```csharp
LankaConnect.Domain.Common
├── Abstractions/           // Core interfaces and abstract base classes
│   ├── IAggregateRoot      // ✅ Root entity interface
│   ├── IRepository<T>      // ✅ Repository abstraction
│   └── ISpecification<T>   // ✅ Specification pattern
├── Enums/                  // Domain-wide enumerations
│   ├── GeographicRegion    // ✅ Geographic enumeration
│   ├── CulturalEventType   // ✅ Cultural event enumeration
│   └── PerformanceObjective// ✅ Performance enumeration
├── ValueObjects/           // All value objects
│   ├── AlertSeverity       // ✅ CANONICAL monitoring severity
│   ├── PerformanceThreshold// ✅ CANONICAL performance threshold
│   ├── DateRange          // ✅ Date range value object
│   └── CulturalContext    // ✅ Cultural intelligence value object
├── Exceptions/             // Domain-specific exceptions
│   ├── DomainException     // ✅ Base domain exception
│   ├── ValidationException// ✅ Validation failure
│   └── BusinessNotFoundException // ✅ Business rule violation
└── Events/                 // Domain events
    ├── IDomainEvent        // ✅ Domain event interface
    └── DomainEvent         // ✅ Base domain event
```

**Enforcement Rules:**
- **NO business logic** in `Domain.Common` - only shared abstractions
- **NO concrete implementations** - interfaces and abstractions only
- **NO infrastructure concerns** - database, external services forbidden
- **Cultural intelligence concepts** go in appropriate value objects

#### Specialized Domain Areas
```csharp
LankaConnect.Domain.Business/
├── Entities/               // Business domain entities
│   ├── BusinessProfile     // ✅ Business aggregate root
│   ├── Advertisement      // ✅ Advertisement entity
│   └── ServiceOffering    // ✅ Service entity
├── ValueObjects/           // Business-specific value objects
│   ├── BusinessCategory   // ✅ Business categorization
│   ├── ContactInformation // ✅ Contact details
│   └── ServiceDescription // ✅ Service metadata
├── Services/               // Business domain services
│   ├── BusinessMatchingService // ✅ Cross-entity business logic
│   └── CulturalRecommendationService // ✅ Cultural intelligence
└── Specifications/         // Business rules and specifications
    ├── BusinessEligibilitySpec // ✅ Business rule specification
    └── CulturalAffinitySpec    // ✅ Cultural matching rules
```

### Application Layer Namespaces

```csharp
LankaConnect.Application.Common/
├── Interfaces/             // Application service interfaces
│   ├── IBusinessService    // ✅ Business orchestration interface
│   ├── ICulturalIntelligenceService // ✅ Cultural processing interface
│   └── INotificationService// ✅ Notification interface
├── DTOs/                   // Data transfer objects ONLY
│   ├── BusinessDto         // ✅ Business data transfer
│   ├── CulturalEventDto    // ✅ Cultural event data
│   └── PerformanceDto      // ✅ Performance metrics data
├── Mappings/               // Domain ↔ DTO mappings
│   ├── BusinessMappings    // ✅ Business entity mappings
│   └── CulturalMappings    // ✅ Cultural intelligence mappings
└── Behaviors/              // Cross-cutting application concerns
    ├── ValidationBehavior  // ✅ Input validation
    ├── LoggingBehavior     // ✅ Application logging
    └── CachingBehavior     // ✅ Application caching
```

**Enforcement Rules:**
- **DTOs MUST NOT contain business logic** - data containers only
- **NO domain entities** in Application layer - use DTOs
- **NO infrastructure implementations** - interfaces only
- **Cultural intelligence** orchestration only, logic stays in domain

### Infrastructure Layer Namespaces

```csharp
LankaConnect.Infrastructure/
├── Data/                   // Database implementations
│   ├── Repositories/       // Repository implementations
│   ├── Configurations/     // EF configurations
│   └── Migrations/         // Database migrations
├── External/               // Third-party integrations
│   ├── CulturalAPIs/       // External cultural data sources
│   ├── PaymentGateways/    // Payment processing
│   └── NotificationServices// SMS, Email, WhatsApp services
├── Monitoring/             // Infrastructure monitoring
│   ├── PerformanceCounters // ✅ Infrastructure metrics
│   ├── HealthChecks       // ✅ System health monitoring
│   └── AlertingService    // ✅ Infrastructure alerting
└── Security/               // Security implementations
    ├── AuthenticationServices // ✅ Identity management
    ├── AuthorizationPolicies  // ✅ Access control
    └── CulturalDataEncryption // ✅ Cultural data protection
```

## Type Ownership Matrix

| **Type Category** | **Canonical Namespace** | **Alternative Locations** | **Enforcement** |
|-------------------|-------------------------|---------------------------|-----------------|
| **Value Objects** | `Domain.Common.ValueObjects` | Domain area-specific | Compiler enforced |
| **Enumerations** | `Domain.Common.Enums` | Domain area-specific | Compiler enforced |
| **Domain Entities** | `Domain.[Area].Entities` | NEVER in Application/Infrastructure | Compiler enforced |
| **Domain Services** | `Domain.[Area].Services` | NEVER cross-layer | Compiler enforced |
| **Application DTOs** | `Application.Common.DTOs` | Application area-specific | Compiler enforced |
| **Infrastructure Models** | `Infrastructure.[Technology]` | NEVER in Domain/Application | Compiler enforced |

## Automated Enforcement Strategies

### 1. Compiler-Based Enforcement
```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsAsErrors />
  <!-- Enforce namespace ambiguity as compilation error -->
  <WarningsAsErrors>CS0104</WarningsAsErrors>

  <!-- Custom analyzers for architectural rules -->
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
</PropertyGroup>
```

### 2. Architecture Testing
```csharp
[TestFixture]
public class ArchitectureComplianceTests
{
    [Test]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        var domainAssembly = typeof(BusinessProfile).Assembly;
        var applicationAssembly = typeof(IBusinessService).Assembly;
        var infrastructureAssembly = typeof(BusinessRepository).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(applicationAssembly.GetName().Name)
            .And()
            .ShouldNot()
            .HaveDependencyOn(infrastructureAssembly.GetName().Name)
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, result.FailingTypes);
    }

    [Test]
    public void ValueObjects_ShouldOnlyExistInDesignatedNamespaces()
    {
        var valueObjectTypes = Types.InCurrentDomain()
            .That()
            .Inherit(typeof(ValueObject))
            .GetTypes();

        var allowedNamespaces = new[]
        {
            "LankaConnect.Domain.Common.ValueObjects",
            "LankaConnect.Domain.*.ValueObjects" // Area-specific allowed
        };

        foreach (var type in valueObjectTypes)
        {
            var isInAllowedNamespace = allowedNamespaces.Any(ns =>
                type.Namespace.Matches(ns));

            Assert.That(isInAllowedNamespace, Is.True,
                $"Value object {type.FullName} is not in allowed namespace");
        }
    }

    [Test]
    public void CulturalIntelligenceTypes_ShouldMaintainProperOwnership()
    {
        // Ensure cultural intelligence concepts maintain proper boundaries
        var culturalTypes = Types.InCurrentDomain()
            .That()
            .HaveNameMatching(".*Cultural.*")
            .GetTypes();

        foreach (var type in culturalTypes)
        {
            // Cultural value objects go in Domain.Common.ValueObjects
            if (type.IsValueObject())
            {
                Assert.That(type.Namespace,
                    Is.EqualTo("LankaConnect.Domain.Common.ValueObjects") |
                    StringContains("Domain.*.ValueObjects"));
            }

            // Cultural services go in Domain services
            if (type.IsService())
            {
                Assert.That(type.Namespace, StringContains("Domain.*.Services"));
            }
        }
    }
}
```

### 3. CI/CD Pipeline Enforcement
```yaml
# .github/workflows/architecture-compliance.yml
name: Architecture Compliance
on: [push, pull_request]

jobs:
  architectural-compliance:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Check for CS0104 errors
      run: |
        dotnet build --verbosity quiet 2>&1 | grep "CS0104" && exit 1 || exit 0

    - name: Run Architecture Tests
      run: dotnet test tests/LankaConnect.ArchitectureTests/ --logger trx

    - name: Validate Cultural Intelligence Boundaries
      run: dotnet test tests/LankaConnect.CulturalIntelligence.BoundaryTests/ --logger trx
```

### 4. IDE-Based Prevention
```json
// .editorconfig
[*.cs]
# Enforce consistent naming and organization
dotnet_diagnostic.SA1200.severity = error  # Using directive placement
dotnet_diagnostic.SA1208.severity = error  # System using directives first
dotnet_diagnostic.SA1210.severity = error  # Using directives alphabetical

# Custom rules for namespace enforcement
# (Implement via custom analyzers)
custom_architecture_rule.CS0104 = error
custom_architecture_rule.domain_dependencies = error
custom_architecture_rule.cultural_boundaries = error
```

## Migration Guidelines for Existing Violations

### Step-by-Step Resolution Process

1. **Identify Canonical Location**
   ```bash
   # Find all CS0104 errors
   dotnet build 2>&1 | grep "CS0104" > cs0104_errors.txt
   # Analyze each type's proper ownership based on rules above
   ```

2. **Create Consolidated Type**
   ```csharp
   // Create in canonical location with comprehensive tests
   namespace LankaConnect.Domain.Common.ValueObjects
   {
       public enum AlertSeverity  // SINGLE DEFINITION
       {
           Low = 1,
           Medium = 2,
           High = 3,
           Critical = 4,
           SacredEventCritical = 10  // Cultural intelligence preserved
       }
   }
   ```

3. **Progressive Migration**
   ```csharp
   // Temporary using aliases during transition
   using AlertSeverity = LankaConnect.Domain.Common.ValueObjects.AlertSeverity;

   // Update references progressively
   // Remove duplicate definitions
   // Remove temporary aliases
   ```

4. **Validation**
   ```bash
   # Ensure zero CS0104 errors after each step
   dotnet build --verbosity minimal
   dotnet test --no-build
   ```

## Cultural Intelligence Preservation

### Special Considerations
Cultural intelligence concepts require careful handling during namespace consolidation:

```csharp
// ✅ Cultural concepts properly organized
namespace LankaConnect.Domain.Common.ValueObjects
{
    public enum CulturalPerformanceThreshold  // CANONICAL location
    {
        General = 5,
        Regional = 6,
        National = 7,
        Religious = 8,
        Sacred = 10  // Highest priority for sacred events
    }
}

namespace LankaConnect.Domain.CulturalIntelligence.Services
{
    public class SacredEventProtectionService  // Business logic
    {
        public AlertSeverity DetermineSeverity(CulturalPerformanceThreshold threshold)
        {
            // Cultural intelligence business logic
            return threshold == CulturalPerformanceThreshold.Sacred
                ? AlertSeverity.SacredEventCritical
                : AlertSeverity.High;
        }
    }
}
```

## Success Metrics

1. **Zero CS0104 Compilation Errors** - Complete elimination
2. **Architecture Test Compliance** - 100% passing architecture tests
3. **Cultural Intelligence Preservation** - All cultural features functional
4. **Performance Maintenance** - No degradation in system performance
5. **Developer Experience** - Clear namespace boundaries, easy to understand

## Conclusion

These enforcement rules ensure that the LankaConnect cultural intelligence platform maintains:
- ✅ **Clean Architecture compliance**
- ✅ **Zero namespace ambiguities**
- ✅ **Cultural intelligence domain integrity**
- ✅ **Maintainable codebase structure**
- ✅ **Automated architectural governance**

The rules provide both immediate resolution of current CS0104 errors and long-term prevention of future architectural violations.