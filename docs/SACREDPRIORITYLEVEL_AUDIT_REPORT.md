# SacredPriorityLevel Type Consolidation Audit Report
**Generated:** 2025-10-10
**Analyst:** Code Analyzer Agent

## Executive Summary

### Critical Finding
**SacredPriorityLevel does NOT exist as a separate enum** - it is actually a **property name** that references the `CulturalDataPriority` enum.

### The Confusion
The build errors showing "SacredPriorityLevel is an ambiguous reference" are **MISLEADING**. The actual issue is:
- Properties named `SacredPriorityLevel` exist in various classes
- These properties have type `CulturalDataPriority`
- There are TWO duplicate definitions of the `CulturalDataPriority` enum causing ambiguity

## Enum Definitions Found

### 1. CulturalDataPriority (Primary - BackupRecoveryModels.cs)
**Location:** `C:\Work\LankaConnect\src\LankaConnect.Domain\Common\Database\BackupRecoveryModels.cs:58`
```csharp
public enum CulturalDataPriority
{
    Level10Sacred = 10,      // Highest priority - Sacred events/data
    Level9Religious = 9,     // Religious ceremonies
    Level8Traditional = 8,   // Traditional celebrations
    Level7Cultural = 7,      // Cultural festivals
    Level6Community = 6,     // Community events
    Level5General = 5,       // General cultural content
    Level4Social = 4,        // Social gatherings
    Level3Commercial = 3,    // Commercial events
    Level2Administrative = 2, // Administrative data
    Level1System = 1         // System logs/metadata
}
```
**Namespace:** `LankaConnect.Domain.Common.Database`

### 2. CulturalDataPriority (Duplicate - CulturalStateReplicationService.cs)
**Location:** `C:\Work\LankaConnect\src\LankaConnect.Domain\Infrastructure\Failover\CulturalStateReplicationService.cs:782`
```csharp
public enum CulturalDataPriority
{
    Sacred,    // Sacred events, religious data
    Critical,  // Critical cultural operations
    High,      // Important cultural features
    Medium,    // General cultural data
    Low        // Background cultural information
}
```
**Namespace:** `LankaConnect.Domain.Infrastructure.Failover` (at end of file)

### 3. NO SacredPriorityLevel Enum Found
**Zero occurrences** of `public enum SacredPriorityLevel` in the source code.

## Property Usage Analysis

### Properties Named "SacredPriorityLevel"

1. **Domain.Shared.MissingTypeStubs.cs (DomainSacredEvent class)**
   - Line 35: `public CulturalDataPriority SacredPriorityLevel { get; init; }`
   - Purpose: Priority level for sacred events
   - References: `CulturalDataPriority` enum

2. **Domain.Shared.BackupTypes.cs (CulturalEventSnapshot class)**
   - Line 148: `public required LankaConnect.Domain.Common.Database.CulturalDataPriority SacredPriorityLevel { get; set; }`
   - Purpose: Backup snapshot priority
   - References: Fully qualified `CulturalDataPriority` from BackupRecoveryModels

3. **Infrastructure DisasterRecovery files**
   - Multiple references to `sacredEvent.SacredPriorityLevel`
   - All access the property of `DomainSacredEvent` class
   - Property type is `CulturalDataPriority`

## Ambiguity Root Cause

### The Real Problem
When code accesses `event.SacredPriorityLevel`, it's accessing a property of type `CulturalDataPriority`. However, **two different `CulturalDataPriority` enums exist**:

1. `LankaConnect.Domain.Common.Database.CulturalDataPriority` (10-level detailed)
2. `LankaConnect.Domain.Infrastructure.Failover.CulturalDataPriority` (5-level simple)

### Files Importing Both Namespaces
`SacredEventRecoveryOrchestrator.cs` and `CulturalIntelligenceBackupEngine.cs` both:
- Import `LankaConnect.Domain.Infrastructure.Failover` (for replication services)
- Import `LankaConnect.Domain.Common.Database` (for backup models)
- Try to use `CulturalDataPriority` causing ambiguity

## Dependency Mapping

### Files Depending on BackupRecoveryModels.CulturalDataPriority (Detailed 10-level)
1. `BackupRecoveryModels.cs` - Defines it (line 58)
2. `SacredEventRecoveryOrchestrator.cs` - Uses fully qualified references
3. `CulturalIntelligenceBackupEngine.cs` - Uses fully qualified references
4. `BackupTypes.cs` - Uses fully qualified reference
5. Multiple value objects in CulturalIntelligence namespace

**Dependents:** ~15 files

### Files Depending on Failover.CulturalDataPriority (Simple 5-level)
1. `CulturalStateReplicationService.cs` - Defines and uses it (line 782)
2. All classes in same file reference it

**Dependents:** ~1 file (self-contained)

## Consolidation Strategy

### Recommended Action: DELETE Duplicate #2

**Delete:** `CulturalDataPriority` enum from `CulturalStateReplicationService.cs` (line 782-789)

**Reason:**
1. Only 1 file uses this simpler version
2. The detailed 10-level version in BackupRecoveryModels is more comprehensive
3. All other files already reference the BackupRecoveryModels version
4. Simple 5-level version can be mapped to 10-level version

### Mapping Strategy
Old (Failover) → New (BackupRecoveryModels):
- `Sacred` → `Level10Sacred`
- `Critical` → `Level9Religious` or `Level8Traditional`
- `High` → `Level7Cultural` or `Level6Community`
- `Medium` → `Level5General`
- `Low` → `Level4Social` or lower

### Impact Analysis

**Breaking Changes:** MINIMAL
- Only `CulturalStateReplicationService.cs` needs updates
- All switch statements need mapping updates
- Property types already correct (both use `CulturalDataPriority`)

**Zero Risk Areas:**
- Properties named `SacredPriorityLevel` won't change
- Backup and recovery systems unaffected
- Domain models remain intact

## Build Error Analysis

### Current Errors
```
C:\Work\LankaConnect\src\LankaConnect.Infrastructure\DisasterRecovery\SacredEventRecoveryOrchestrator.cs(357,13):
error CS0104: 'SacredPriorityLevel' is an ambiguous reference between
'LankaConnect.Domain.CulturalIntelligence.Enums.SacredPriorityLevel' and
'LankaConnect.Domain.Shared.SacredPriorityLevel'
```

**Status:** MISLEADING ERROR MESSAGE
- Compiler is confused by property name
- Real issue: Two `CulturalDataPriority` enums in scope
- Error message incorrectly identifies "SacredPriorityLevel" as the type

### Post-Fix Expectations
After deleting the duplicate `CulturalDataPriority` from `CulturalStateReplicationService.cs`:
- **91 errors → ~85 errors** (6-error reduction expected)
- All `SacredPriorityLevel` ambiguity errors resolved
- Remaining errors: unrelated type issues

## Action Plan

### Phase 1: Delete Duplicate Enum (5 minutes)
1. Remove lines 782-789 from `CulturalStateReplicationService.cs`
2. Add using statement: `using CulturalDataPriority = LankaConnect.Domain.Common.Database.CulturalDataPriority;`

### Phase 2: Update Mappings (10 minutes)
1. Update all switch statements in `CulturalStateReplicationService.cs`
2. Map 5-level priorities to 10-level equivalents
3. Update default values and comparisons

### Phase 3: Validation (5 minutes)
1. Build and verify error count reduction
2. Run tests for disaster recovery components
3. Validate priority mappings are semantically correct

**Total Estimated Time:** 20 minutes
**Risk Level:** LOW
**Expected Error Reduction:** 6-8 errors

## Files Requiring Changes

### Single File to Modify
```
C:\Work\LankaConnect\src\LankaConnect.Domain\Infrastructure\Failover\CulturalStateReplicationService.cs
```

**Lines to Delete:** 782-789 (enum definition)
**Lines to Update:** ~25 references throughout file

## Conclusion

This audit reveals that **"SacredPriorityLevel" is NOT a duplicate type** - it's a property name that references the duplicate `CulturalDataPriority` enum. The consolidation is straightforward:

1. Delete the simpler 5-level `CulturalDataPriority` from Failover namespace
2. Update one file to use the detailed 10-level version
3. Resolve 6-8 ambiguity errors with minimal risk

**Next Step:** Execute Phase 1 deletion and mapping update.
