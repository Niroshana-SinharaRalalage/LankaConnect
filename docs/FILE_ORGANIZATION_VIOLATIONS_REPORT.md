# File Organization Violations Report

**Analysis Date**: 2025-10-07
**Project**: LankaConnect
**Rule Violated**: "For any given type there should be a separate class file"

---

## Executive Summary

| Metric | Count |
|--------|-------|
| **Total Files Analyzed** | 939 |
| **Files with Multiple Types** | 361 |
| **Interface File Violations** | 41 |
| **Total Violations** | 361 |
| **Refactoring Actions Required** | 4,302 |

---

## Violation Categories

### 1. **High Severity - Files with 10+ Types** (16+ files)

#### Top Offenders:

**CulturalIntelligenceBillingDTOs.cs** - 28 types
- Location: `src\LankaConnect.API\DTOs\CulturalIntelligenceBillingDTOs.cs`
- Contains: 28 request/response DTOs mixed in a single file
- Impact: Massive file, difficult to navigate and maintain
- Recommendation: Split into separate folders by feature (Subscriptions, Usage, Enterprise, Analytics)

**CulturalCommunicationsController.cs** - 16 types
- Location: `src\LankaConnect.API\Controllers\CulturalCommunicationsController.cs`
- Contains: 1 controller + 15 request/response DTOs
- Impact: Controller file bloated with DTOs
- Recommendation: Extract all DTOs to `DTOs/CulturalCommunications/` folder

**CulturalEventsController.cs** - 16 types
- Location: `src\LankaConnect.API\Controllers\CulturalEventsController.cs`
- Contains: 1 controller + 15 request/response DTOs
- Impact: Controller file bloated with DTOs
- Recommendation: Extract all DTOs to `DTOs/CulturalEvents/` folder

**AlternativeChannelConfiguration.cs** - 15 types
- Location: `src\LankaConnect.Application\Common\DisasterRecovery\AlternativeChannelConfiguration.cs`
- Contains: 7 classes + 8 enums
- Impact: Mixed types (classes and enums) in single file
- Recommendation: Split classes into separate files, group enums in dedicated enum files

**StripeWebhookHandler.cs** - 15 types
- Location: `src\LankaConnect.Application\Billing\StripeWebhookHandler.cs`
- Contains: 1 interface + 1 handler class + 13 event classes
- Impact: Handler implementation mixed with event type definitions
- Recommendation: Extract event types to `Events/` subfolder

### 2. **Medium Severity - Interface Files with Embedded Types** (41 files)

These violate the principle that interface files should contain only the interface definition.

**Critical Examples:**

1. **StripeWebhookHandler.cs**
   - Interface: `IStripeWebhookHandler`
   - Embedded: 14 additional classes (handler + 13 event types)

2. **ICulturalConflictResolutionEngine.cs**
   - Interface: `ICulturalConflictResolutionEngine`
   - Embedded: 12 classes + 1 enum

3. **ICulturalEventLoadDistributionService.cs**
   - Interface: `ICulturalEventLoadDistributionService`
   - Embedded: 6 classes

4. **IDatabaseSecurityOptimizationEngine.cs**
   - Interface: `IDatabaseSecurityOptimizationEngine`
   - Embedded: 24 records + 6 enums

### 3. **Controller Files with Embedded DTOs** (Multiple files)

**Pattern**: Controllers define request/response DTOs inline instead of separate DTO files.

Examples:
- `BusinessesController.cs` - 6 types (1 controller + 5 DTOs)
- `CulturalCommunicationsController.cs` - 16 types
- `CulturalEventsController.cs` - 16 types
- `CulturalIntelligenceController.cs` - 4 types
- `EmailController.cs` - 3 types

**Impact**: Controllers become bloated and hard to test. DTOs are not reusable.

### 4. **Disaster Recovery Files - Mixed Classes and Enums**

**Pattern**: Configuration and result files contain both classes and enums.

Examples (all in `src\LankaConnect.Application\Common\DisasterRecovery\`):
- `AlternativeChannelConfiguration.cs` - 7 classes + 8 enums
- `AlternativeRevenueChannelResult.cs` - 6 classes + 10 enums
- `BillingContinuityConfiguration.cs` - 7 classes + 9 enums
- `BillingContinuityResult.cs` - 6 classes + 10 enums
- `ComplianceReportingScope.cs` - 4 classes + 7 enums
- `RevenueContinuityStrategy.cs` - 8 classes + 13 enums
- `RevenueImpactMonitoringConfiguration.cs` - 8 classes + 11 enums

**Impact**: Large files mixing different type kinds, reducing code clarity.

---

## Refactoring Strategy

### Phase 1: Critical - Split Large Files (Priority 1)

**Target**: Files with 10+ types

1. **CulturalIntelligenceBillingDTOs.cs** (28 types)
   ```
   Create structure:
   DTOs/CulturalIntelligenceBilling/
   ├── Subscriptions/
   │   ├── CreateCulturalIntelligenceSubscriptionRequest.cs
   │   ├── CulturalIntelligenceSubscriptionActivatedEvent.cs
   │   └── ...
   ├── Usage/
   │   ├── ProcessCulturalIntelligenceUsageRequest.cs
   │   ├── BuddhistCalendarUsageResponse.cs
   │   └── ...
   ├── Enterprise/
   │   ├── CreateEnterpriseContractRequest.cs
   │   └── EnterpriseContractResponse.cs
   └── Analytics/
       ├── GetRevenueAnalyticsRequest.cs
       └── RevenueAnalyticsResponse.cs
   ```

2. **Controller DTOs** (CulturalCommunications, CulturalEvents, etc.)
   ```
   Extract inline DTOs to:
   DTOs/[ControllerName]/
   ├── [Feature]Request.cs
   └── [Feature]Response.cs
   ```

3. **Disaster Recovery Classes**
   ```
   For each configuration file:
   DisasterRecovery/[Feature]/
   ├── [Feature]Configuration.cs (main class only)
   ├── [Feature]Result.cs (main class only)
   ├── Supporting/
   │   ├── [Class1].cs
   │   ├── [Class2].cs
   │   └── ...
   └── Enums/
       ├── [Enum1].cs
       └── [Enum2].cs
   ```

### Phase 2: High - Fix Interface Files (Priority 2)

**Target**: 41 interface files with embedded types

**Pattern**:
```
Before:
  Interfaces/IService.cs
    - interface IService
    - class ServiceResult
    - class ServiceRequest
    - enum ServiceType

After:
  Interfaces/IService.cs (interface only)
  Models/Service/
    ├── ServiceResult.cs
    ├── ServiceRequest.cs
    └── ServiceType.cs (enum)
```

**Files to refactor**:
1. `StripeWebhookHandler.cs` - Extract 14 types
2. `ICulturalConflictResolutionEngine.cs` - Extract 12 classes + 1 enum
3. `IDatabaseSecurityOptimizationEngine.cs` - Extract 24 records + 6 enums
4. `IDatabasePerformanceMonitoringEngine.cs` - Extract 11 classes + 10 enums
5. `ICulturalEventLoadDistributionService.cs` - Extract 6 classes
6. All other interface files (36 remaining)

### Phase 3: Medium - Model Files (Priority 3)

**Target**: Domain model files with multiple related types

**Examples**:
- `HeritageLanguageModels.cs` (5 classes) → Split into individual files
- `LanguageRoutingModels.cs` (5 classes) → Split into individual files
- `ConsentManagement.cs` (3 classes) → Split into individual files
- `DataMinimization.cs` (3 classes) → Split into individual files

### Phase 4: Low - Filter/Attribute Files (Priority 4)

**Target**: Files with main class + supporting types

**Example**: `ApiExceptionFilterAttribute.cs`
```
Before:
  - class ApiExceptionFilterAttribute
  - class ApiErrorResponse

After:
  Filters/ApiExceptionFilterAttribute.cs
  Models/ApiErrorResponse.cs
```

---

## Implementation Checklist

### Automated Refactoring Steps:

1. [ ] **Create directory structure** for extracted types
2. [ ] **Extract types** using automated tools or scripts
3. [ ] **Update namespaces** in extracted files
4. [ ] **Update using statements** in dependent files
5. [ ] **Run full build** to verify no compilation errors
6. [ ] **Run test suite** to verify no behavioral changes
7. [ ] **Update documentation** if needed
8. [ ] **Commit changes** in logical batches by feature area

### Validation Criteria:

- [ ] Each .cs file contains exactly ONE top-level type (class/interface/enum/struct/record)
- [ ] No interface files contain embedded types
- [ ] No controller files contain DTO definitions
- [ ] All namespaces are correctly updated
- [ ] All using statements are correct
- [ ] Full solution builds without errors
- [ ] All tests pass
- [ ] No duplicate type names across different files

---

## Risk Analysis

### High Risk Areas:

1. **Namespace Changes**: 4,302 files will need updated using statements
2. **Circular Dependencies**: Some extracted types may reveal hidden circular dependencies
3. **Build Breaking**: Large-scale refactoring may temporarily break the build
4. **Test Impact**: Tests may need updated references

### Mitigation Strategy:

1. **Incremental Approach**: Refactor one feature area at a time
2. **Automated Tools**: Use Roslyn analyzers and refactoring tools
3. **Continuous Testing**: Run tests after each batch of changes
4. **Version Control**: Use feature branches and commit frequently
5. **Code Review**: Have at least one other developer review each batch

---

## Estimated Effort

| Phase | Files Affected | Estimated Time | Priority |
|-------|----------------|----------------|----------|
| Phase 1: Large Files | 30 files → 200+ files | 16-24 hours | Critical |
| Phase 2: Interface Files | 41 files → 200+ files | 12-16 hours | High |
| Phase 3: Model Files | 100 files → 300+ files | 20-30 hours | Medium |
| Phase 4: Other Files | 190 files → 400+ files | 15-20 hours | Low |
| **TOTAL** | **361 files → 1,100+ files** | **63-90 hours** | - |

**Note**: Actual time may vary based on tooling support and automation capabilities.

---

## Tooling Recommendations

### Recommended Tools:

1. **ReSharper** or **Rider** - Automated refactoring support
2. **Roslyn Analyzers** - Custom rules to prevent future violations
3. **PowerShell Scripts** - Automate file extraction and namespace updates
4. **Git** - Use feature branches and atomic commits
5. **Visual Studio Code Navigation** - Find all references during refactoring

### Custom Analyzer Rule:

Create a custom Roslyn analyzer to enforce "one type per file" rule:

```csharp
// Analyzer: CA-LankaConnect-001
// Title: Each file should contain exactly one type definition
// Severity: Warning
// Description: Files should not contain multiple classes, interfaces, enums, or structs
```

---

## Next Steps

1. **Review this report** with the development team
2. **Prioritize** which phases to tackle first based on current sprint goals
3. **Set up tooling** (analyzers, scripts) before starting refactoring
4. **Create feature branch** for refactoring work
5. **Start with Phase 1** (large files) as proof of concept
6. **Establish pattern** that works well, then scale to other phases
7. **Update coding standards** to prevent future violations

---

## Appendix: Complete Violation List

See `docs/violations-raw.json` for the complete machine-readable list of all 361 violations with:
- Exact file paths
- Line numbers for each type
- Type kinds (class/interface/enum/struct/record)
- Suggested new file paths
- Refactoring action items

**Note**: The raw JSON file is 1.7 MB and contains 4,302 individual refactoring actions.
