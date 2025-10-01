# Clean Architecture Compliance Framework

## Architectural Decision Record: Infrastructure Layer Validation Rules

**Status**: Active
**Date**: 2025-09-22
**Decision**: Establish comprehensive compliance framework for Infrastructure layer error elimination

## Core Principles

### Dependency Inversion Rule
```
Infrastructure (Outer) → Application (Inner) → Domain (Core)
```

**Allowed Dependencies**:
- ✅ Infrastructure → Application interfaces
- ✅ Infrastructure → Domain entities/value objects
- ✅ Application → Domain entities/services
- ✅ Infrastructure → External packages/frameworks

**Forbidden Dependencies**:
- ❌ Domain → Application
- ❌ Domain → Infrastructure
- ❌ Application → Infrastructure (concrete implementations)

### Layer Responsibility Matrix

| Layer | Purpose | Allowed to Contain | Not Allowed |
|-------|---------|-------------------|-------------|
| **Domain** | Core business logic | Entities, Value Objects, Domain Services, Business Rules | Infrastructure concerns, External dependencies |
| **Application** | Use cases, orchestration | Interfaces, DTOs, Application Services, Command/Query handlers | Direct external dependencies, Business entities |
| **Infrastructure** | External concerns | Data access, External APIs, Framework implementations | Business logic, Domain entities creation |

## Type Placement Validation Rules

### 1. Business Domain Types → Domain Layer

**Location**: `src/LankaConnect.Domain/CulturalIntelligence/`

**Types that belong here**:
```csharp
// Cultural business concepts
public class SacredEvent : Entity<SacredEventId>
public class CulturalEvent : Entity<CulturalEventId>

// Value objects (immutable business concepts)
public class RecoveryStep : ValueObject
public class SacredPriorityLevel : ValueObject

// Enums representing business concepts
public enum CulturalSignificance { Low, Medium, High, Sacred }
public enum RecoveryUrgency { Deferred, Standard, Urgent, Critical }
```

**Validation Pattern**:
```csharp
// Domain types should extend base domain classes
public class SacredEvent : Entity<SacredEventId> // ✅ Correct
public class SacredEvent // ❌ Missing base class
```

### 2. Application Service Contracts → Application Layer

**Location**: `src/LankaConnect.Application/Common/`

**Types that belong here**:
```csharp
// Service interfaces (contracts)
public interface ICulturalIntelligenceBackupEngine
public interface ISacredEventRecoveryOrchestrator

// Application DTOs/Results
public class SacredEventRecoveryResult
public class PriorityRecoveryPlan
public class MultiCulturalRecoveryResult

// Application-specific exceptions
public class CulturalIntelligenceException : ApplicationException
```

### 3. Infrastructure Implementations → Infrastructure Layer

**Location**: `src/LankaConnect.Infrastructure/`

**Types that belong here**:
```csharp
// Interface implementations
public class CulturalIntelligenceBackupEngine : ICulturalIntelligenceBackupEngine
public class SacredEventRecoveryOrchestrator : ISacredEventRecoveryOrchestrator

// Infrastructure-specific configurations
public class ApplicationInsightsConfiguration
public class DatabaseConnectionConfiguration
```

## Return Type Pattern Compliance

### Standard Pattern: Task<Result<T>>

**Rule**: All async methods in Infrastructure that implement Application interfaces must return `Task<Result<T>>`

**Compliance Check**:
```csharp
// ✅ COMPLIANT
public async Task<Result<CulturalIntelligenceBackupStatus>> GetBackupStatusAsync(
    string backupId,
    CancellationToken cancellationToken)

// ❌ NON-COMPLIANT
public async Task<CulturalIntelligenceBackupStatus> GetBackupStatusAsync(
    string backupId,
    CancellationToken cancellationToken)
```

### Exception Handling Pattern

**Standard Pattern**: Convert exceptions to Result failures
```csharp
public async Task<Result<T>> SomeMethodAsync()
{
    try
    {
        var result = await PerformOperationAsync();
        return Result<T>.Success(result);
    }
    catch (DomainException ex)
    {
        _logger.LogWarning(ex, "Domain rule violation in {Method}", nameof(SomeMethodAsync));
        return Result<T>.Failure(ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in {Method}", nameof(SomeMethodAsync));
        return Result<T>.Failure($"Operation failed: {ex.Message}");
    }
}
```

## Interface Implementation Validation

### Complete Interface Implementation Rule

**Requirement**: All Infrastructure classes implementing Application interfaces must implement ALL interface members

**Validation Check**:
```csharp
// Get all interface methods
var interfaceMethods = typeof(ICulturalIntelligenceBackupEngine).GetMethods();
var implementationMethods = typeof(CulturalIntelligenceBackupEngine).GetMethods();

// Ensure all are implemented
foreach (var interfaceMethod in interfaceMethods)
{
    var implementation = implementationMethods.FirstOrDefault(m =>
        m.Name == interfaceMethod.Name &&
        m.GetParameters().Length == interfaceMethod.GetParameters().Length);

    Assert.That(implementation, Is.Not.Null,
        $"Method {interfaceMethod.Name} not implemented");
}
```

### Stub vs Full Implementation Pattern

**Phase 1 - Stub Implementation** (for compilation):
```csharp
public async Task<Result<T>> MethodAsync()
{
    _logger.LogWarning("STUB: {Method} requires full implementation", nameof(MethodAsync));
    await Task.Delay(1); // Prevent compiler warnings

    var stubResult = CreateStubResult();
    return Result<T>.Success(stubResult);
}
```

**Phase 2 - Full Implementation** (TDD-driven):
```csharp
public async Task<Result<T>> MethodAsync()
{
    try
    {
        // Full business logic implementation
        var result = await ExecuteBusinessLogicAsync();
        return Result<T>.Success(result);
    }
    catch (Exception ex)
    {
        return Result<T>.Failure(ex.Message);
    }
}
```

## Namespace Organization Rules

### Layer-Based Namespace Strategy

```csharp
// Domain Layer
namespace LankaConnect.Domain.CulturalIntelligence
namespace LankaConnect.Domain.CulturalIntelligence.ValueObjects
namespace LankaConnect.Domain.CulturalIntelligence.Entities

// Application Layer
namespace LankaConnect.Application.Common.Interfaces
namespace LankaConnect.Application.Common.Models
namespace LankaConnect.Application.CulturalIntelligence

// Infrastructure Layer
namespace LankaConnect.Infrastructure.DisasterRecovery
namespace LankaConnect.Infrastructure.Monitoring
namespace LankaConnect.Infrastructure.Security
```

### Using Directive Compliance

**Infrastructure classes must import from Application and Domain**:
```csharp
// ✅ CORRECT
using LankaConnect.Application.Common.Interfaces;  // Application interface
using LankaConnect.Domain.Common;                  // Domain base classes
using LankaConnect.Domain.CulturalIntelligence;   // Domain entities

// ❌ FORBIDDEN
using SomeExternalLibrary.BusinessLogic;  // Business logic from external source
```

## Automated Validation Framework

### Compilation Gate Checks

```powershell
# Phase validation script
function Test-CleanArchitectureCompliance {
    # 1. Check Domain layer compiles independently
    dotnet build src/LankaConnect.Domain/ --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Domain layer must compile independently" }

    # 2. Check Application layer with Domain reference only
    dotnet build src/LankaConnect.Application/ --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Application layer violation detected" }

    # 3. Check Infrastructure last
    dotnet build src/LankaConnect.Infrastructure/ --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "Infrastructure implementation errors" }
}
```

### Dependency Analysis

```csharp
[Test]
public void Infrastructure_ShouldNotReferenceDomainConcretions()
{
    var infraAssembly = Assembly.LoadFrom("LankaConnect.Infrastructure.dll");
    var domainAssembly = Assembly.LoadFrom("LankaConnect.Domain.dll");

    foreach (var type in infraAssembly.GetTypes())
    {
        var referencedTypes = type.GetReferencedTypes();
        var domainConcretions = referencedTypes.Where(t =>
            t.Assembly == domainAssembly &&
            !t.IsInterface &&
            !t.IsAbstract &&
            t != typeof(Result) &&
            t != typeof(Result<>));

        Assert.That(domainConcretions, Is.Empty,
            $"Type {type.Name} references domain concretions: {string.Join(", ", domainConcretions.Select(t => t.Name))}");
    }
}
```

## Quality Gates

### Pre-Implementation Checklist

- [ ] **Type Placement**: All new types placed in correct layer
- [ ] **Interface Contracts**: All interfaces properly defined in Application layer
- [ ] **Return Types**: All async methods use `Task<Result<T>>` pattern
- [ ] **Dependencies**: No forbidden dependency directions
- [ ] **Namespaces**: Consistent namespace organization

### Post-Implementation Validation

- [ ] **Compilation**: Zero errors across all layers
- [ ] **Interface Coverage**: All interface members implemented
- [ ] **Exception Handling**: All exceptions converted to Result failures
- [ ] **Logging**: Appropriate logging for Infrastructure operations
- [ ] **Tests**: Unit tests for all new implementations

## Error Pattern Analysis

### CS0738 (Return Type Mismatch)
**Root Cause**: Infrastructure method returns `Task<T>` but interface expects `Task<Result<T>>`
**Solution**: Modify Infrastructure implementation, preserve interface contract

### CS0246 (Type Not Found)
**Root Cause**: Type referenced but not defined in accessible namespace
**Solution**: Create type in appropriate layer (usually Domain or Application)

### CS0535 (Missing Implementation)
**Root Cause**: Interface method not implemented in Infrastructure class
**Solution**: Add stub implementation initially, enhance with TDD

## Monitoring and Maintenance

### Continuous Compliance

```yaml
# GitHub Actions compliance check
- name: Validate Clean Architecture
  run: |
    # Check for forbidden dependencies
    dotnet list package --framework net8.0 --include-transitive | grep -E "(Domain.*Infrastructure|Application.*Infrastructure)"
    if [ $? -eq 0 ]; then
      echo "❌ Forbidden dependency detected"
      exit 1
    fi

    # Verify compilation order
    dotnet build src/LankaConnect.Domain/
    dotnet build src/LankaConnect.Application/
    dotnet build src/LankaConnect.Infrastructure/
```

### Architectural Drift Detection

```csharp
[Test]
public void Architecture_ShouldMaintainLayerSeparation()
{
    var architecture = new ArchitectureTestBuilder()
        .WithAssemblies("LankaConnect.Domain", "LankaConnect.Application", "LankaConnect.Infrastructure")
        .Build();

    architecture
        .Domain().Should().NotDependOn("Application", "Infrastructure")
        .Application().Should().NotDependOn("Infrastructure")
        .Infrastructure().Should().DependOn("Application", "Domain");
}
```

This compliance framework ensures that the Infrastructure layer error elimination maintains architectural integrity while achieving compilation success.