# ADR: Type Consolidation Strategy
## Systematic Elimination of Type Duplication

**Status:** Approved
**Date:** 2025-09-28
**Context:** Emergency response to 3+ weeks of build errors caused by massive type duplication

---

## Decision

We will implement a **Systematic Type Consolidation Strategy** to eliminate all duplicate type definitions across the codebase and restore Clean Architecture compliance.

## Problem Statement

**Current Crisis:**
- **16 files** contain duplicate core types
- **5 BackupFrequency** definitions across layers
- **4 DataRetentionPolicy** definitions across layers
- **3 DisasterRecoveryResult** definitions across layers
- **3+ weeks** of persistent build errors
- **CS0104 ambiguous reference errors** blocking development

**Root Cause:** Fundamental violation of Clean Architecture dependency rules with types defined in wrong layers.

## Solution Architecture

### 1. SINGLE SOURCE OF TRUTH PRINCIPLE

**CANONICAL TYPE LOCATION:**
```
C:\Work\LankaConnect\src\LankaConnect.Domain\Shared\Types\CriticalTypes.cs
```

**TYPES TO CONSOLIDATE:**
- `BackupFrequency` (enum) - Keep Domain version only
- `DataRetentionPolicy` (enum) - Keep Domain version only
- `DisasterRecoveryResult` (class) - Keep Domain version only

### 2. CLEAN ARCHITECTURE COMPLIANCE

**LAYER OWNERSHIP RULES:**
```
Domain Layer (Core Business Logic):
- Entities, Value Objects, Enums
- Business Rules and Domain Services
- OWNS: BackupFrequency, DataRetentionPolicy, DisasterRecoveryResult

Application Layer (Use Cases):
- Application Services, DTOs
- REFERENCES Domain types only
- DELETES: Local type definitions

Infrastructure Layer (External Concerns):
- Repository implementations, External services
- REFERENCES Domain types only
- DELETES: Domain type definitions
```

**DEPENDENCY FLOW:**
```
Domain (Core) ← Application ← Infrastructure
```

### 3. NAMESPACE CONSOLIDATION

**CURRENT CHAOS:**
```
❌ LankaConnect.Application.Common.DisasterRecovery
❌ LankaConnect.Application.Common.Models.DisasterRecovery
❌ LankaConnect.Domain.Common.DisasterRecovery
❌ LankaConnect.Domain.Shared.DisasterRecovery
❌ LankaConnect.Infrastructure.DisasterRecovery
```

**TARGET STRUCTURE:**
```
✅ LankaConnect.Domain.Shared.Types (core types)
✅ LankaConnect.Application.DisasterRecovery (use cases)
✅ LankaConnect.Infrastructure.DisasterRecovery (implementations)
```

## Implementation Plan

### PHASE 1: Domain Consolidation (Day 1)

**DELETE DUPLICATES:**
```bash
# Remove duplicate BackupFrequency definitions
DELETE: src/LankaConnect.Domain/Common/Database/BackupFrequency.cs
DELETE: src/LankaConnect.Domain/Shared/BackupTypes.cs (BackupFrequency section)
DELETE: src/LankaConnect.Infrastructure/Common/Models/DisasterRecoveryModels.cs (BackupFrequency class)

# Remove duplicate DataRetentionPolicy definitions
DELETE: src/LankaConnect.Application/Common/Models/DisasterRecovery/RecoveryTypes.cs (DataRetentionPolicy)
DELETE: src/LankaConnect.Application/Common/Models/Security/DataAnonymizationResult.cs (DataRetentionPolicy class)
DELETE: src/LankaConnect.Infrastructure/Common/Models/DisasterRecoveryModels.cs (DataRetentionPolicy class)

# Remove duplicate DisasterRecoveryResult definitions
DELETE: src/LankaConnect.Domain/Common/Database/CulturalConflictModels.cs (DisasterRecoveryResult class)
```

**KEEP CANONICAL:**
```cs
// ONLY VERSION: LankaConnect.Domain.Shared.Types.CriticalTypes.cs
public enum BackupFrequency
{
    Continuous,
    Every15Minutes,
    Hourly,
    Daily,
    Weekly,
    Monthly
}

public enum DataRetentionPolicy
{
    ShortTerm,   // 30 days
    MediumTerm,  // 90 days
    LongTerm,    // 1 year
    Permanent    // Indefinite
}

public class DisasterRecoveryResult
{
    public Guid RecoveryId { get; set; }
    public bool IsSuccessful { get; set; }
    public TimeSpan RecoveryDuration { get; set; }
    public string RecoveryLocation { get; set; } = string.Empty;
    public List<string> RecoveredComponents { get; set; } = new();
    public decimal DataIntegrityScore { get; set; }
    public DateTime RecoveryTimestamp { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
```

### PHASE 2: Reference Updates (Day 2)

**UPDATE ALL USING STATEMENTS:**
```cs
// Replace all local references with Domain references
using LankaConnect.Domain.Shared.Types;

// Remove ambiguous references
// DELETE: using LankaConnect.Application.Common.Models.DisasterRecovery;
// DELETE: using LankaConnect.Infrastructure.Common.Models;
```

**FILES REQUIRING UPDATES:**
- All 16 files currently containing duplicate types
- All interface definitions referencing these types
- All implementation files using these types

### PHASE 3: Application Layer Cleanup (Day 3)

**DELETE APPLICATION TYPE DEFINITIONS:**
```bash
# Remove entire duplicate type files
DELETE: src/LankaConnect.Application/Common/Models/DisasterRecovery/RecoveryTypes.cs
```

**UPDATE APPLICATION INTERFACES:**
```cs
// Before: Local type definition
public class DataRetentionPolicy { ... }

// After: Domain reference
using LankaConnect.Domain.Shared.Types;
// Use: DataRetentionPolicy (from Domain)
```

### PHASE 4: Infrastructure Layer Cleanup (Day 4)

**REMOVE INFRASTRUCTURE TYPE DEFINITIONS:**
```cs
// DELETE from DisasterRecoveryModels.cs:
public class BackupFrequency { ... }           // DELETE
public class DataRetentionPolicy { ... }       // DELETE
public enum BackupFrequencyType { ... }        // DELETE

// KEEP infrastructure-specific implementations:
public class SacredEventRecoveryResult { ... } // KEEP (Infrastructure-specific)
```

**RESTORE CORRECT DEPENDENCIES:**
```cs
// Infrastructure references Domain only
using LankaConnect.Domain.Shared.Types;

// Remove wrong layer references
// DELETE: using LankaConnect.Application.Common.Models.DisasterRecovery;
```

## Validation Criteria

### BUILD SUCCESS METRICS:
- [ ] Zero CS0104 ambiguous reference errors
- [ ] Zero CS0246 type not found errors
- [ ] Full solution builds without warnings
- [ ] All tests pass

### ARCHITECTURE COMPLIANCE:
- [ ] Single definition per core domain type
- [ ] Correct layer ownership verified
- [ ] Dependency flow validates (Domain ← Application ← Infrastructure)
- [ ] Namespace consolidation complete

### VERIFICATION COMMANDS:
```bash
# Verify no duplicates remain
grep -r "class BackupFrequency\|enum BackupFrequency" src/
grep -r "class DataRetentionPolicy\|enum DataRetentionPolicy" src/
grep -r "class DisasterRecoveryResult" src/

# Should return ONLY Domain.Shared.Types locations
```

## Risk Mitigation

### ROLLBACK STRATEGY:
- Git branch for entire consolidation effort
- Incremental commits for each phase
- Build verification after each deletion

### TESTING APPROACH:
- Automated build after each phase
- Unit test execution verification
- Integration test validation

### COMMUNICATION PLAN:
- Team notification before starting
- Progress updates after each phase
- Architecture documentation updates

## Consequences

### POSITIVE IMPACTS:
- **Eliminated build errors** - End 3+ week error cycle
- **Improved maintainability** - Single source of truth
- **Faster development** - No ambiguity resolution needed
- **Architecture compliance** - Restored Clean Architecture
- **Team velocity** - Focus on features vs. build fixes

### BREAKING CHANGES:
- Files using wrong layer references will need updates
- Namespace imports may require changes
- Some complex type references need simplification

### LONG-TERM BENEFITS:
- **Scalable architecture** - Clear ownership model
- **Onboarding efficiency** - Obvious type locations
- **Refactoring safety** - No hidden dependencies
- **Quality assurance** - Architecture enforcement

## Decision Rationale

### WHY NOT ALTERNATIVES:

**❌ Fully Qualified Names Approach:**
- Treats symptoms, not root cause
- Creates verbose, unmaintainable code
- Perpetuates architectural violations

**❌ Namespace Alias Approach:**
- Adds complexity without fixing violations
- Still maintains multiple definitions
- Confuses team about type ownership

**❌ Gradual Migration Approach:**
- Prolongs current pain and build errors
- Risk of incomplete migration
- Blocks all development progress

**✅ SYSTEMATIC CONSOLIDATION:**
- **Addresses root architectural violations**
- **Establishes sustainable patterns**
- **Enables immediate resolution**
- **Prevents future similar issues**

## Success Criteria

### DEFINITION OF DONE:
1. **Zero duplicate type definitions** across entire codebase
2. **Clean build** with no ambiguous reference errors
3. **Architecture compliance** verified through dependency analysis
4. **Full test suite** passing
5. **Documentation updated** with new type ownership rules

### MONITORING:
- **Daily build health** checks
- **Architecture compliance** automated validation
- **Team velocity** metrics post-implementation

---

## Appendix: Type Ownership Matrix

| Type | Canonical Location | Layer | Namespace |
|------|-------------------|-------|-----------|
| BackupFrequency | Domain.Shared.Types.CriticalTypes.cs | Domain | LankaConnect.Domain.Shared.Types |
| DataRetentionPolicy | Domain.Shared.Types.CriticalTypes.cs | Domain | LankaConnect.Domain.Shared.Types |
| DisasterRecoveryResult | Domain.Shared.Types.CriticalTypes.cs | Domain | LankaConnect.Domain.Shared.Types |

**ENFORCEMENT RULE:** Any new core business type MUST be placed in Domain layer with clear business justification.

---

*This ADR provides the architectural foundation for permanent resolution of type duplication and restoration of Clean Architecture compliance.*