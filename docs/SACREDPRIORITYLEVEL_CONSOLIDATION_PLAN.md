# SacredPriorityLevel Consolidation - Execution Plan

## Quick Reference

### The Truth About "SacredPriorityLevel"
- **NOT AN ENUM** - It's a property name
- **Property Type:** `CulturalDataPriority` (enum)
- **Root Cause:** TWO duplicate `CulturalDataPriority` enums causing ambiguity

## Duplicate Enum Locations

| # | Location | Namespace | Values | Status |
|---|----------|-----------|--------|--------|
| 1 | `BackupRecoveryModels.cs:58` | `LankaConnect.Domain.Common.Database` | 10 levels (Level10Sacred → Level1System) | **KEEP** ✅ |
| 2 | `CulturalStateReplicationService.cs:782` | `LankaConnect.Domain.Infrastructure.Failover` | 5 levels (Sacred → Low) | **DELETE** ❌ |

## Impact Assessment

### Files Using BackupRecoveryModels.CulturalDataPriority (10-level)
1. ✅ `BackupRecoveryModels.cs` - Defines it
2. ✅ `SacredEventRecoveryOrchestrator.cs` - 15 fully qualified references
3. ✅ `CulturalIntelligenceBackupEngine.cs` - 18 fully qualified references
4. ✅ `BackupTypes.cs` - 3 properties
5. ✅ `RecoveryStep.cs` - Value object property
6. ✅ `PriorityRecoveryPlan.cs` - Value object property
7. ✅ Multiple domain value objects and interfaces

**Total Files:** ~15 files ✅ STABLE

### Files Using Failover.CulturalDataPriority (5-level)
1. ❌ `CulturalStateReplicationService.cs` - ONLY file using this enum

**Total Files:** 1 file ❌ ISOLATED DUPLICATE

## Consolidation Strategy

### Step 1: Update CulturalStateReplicationService.cs

#### A. Add Type Alias (Top of file, after usings)
```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Infrastructure.Scaling;
using CulturalDataPriority = LankaConnect.Domain.Common.Database.CulturalDataPriority;

namespace LankaConnect.Domain.Infrastructure.Failover;
```

#### B. Delete Lines 782-789
```csharp
// DELETE THIS:
public enum CulturalDataPriority
{
    Sacred,    // Sacred events, religious data
    Critical,  // Critical cultural operations
    High,      // Important cultural features
    Medium,    // General cultural data
    Low        // Background cultural information
}
```

#### C. Update All References (25 locations)

**Pattern 1: Property Declarations**
```csharp
// BEFORE:
public CulturalDataPriority Priority { get; private set; }

// AFTER: (No change needed - type alias handles it)
public CulturalDataPriority Priority { get; private set; }
```

**Pattern 2: Switch Statements - Map 5-level to 10-level**
```csharp
// BEFORE (Line 502):
return stateData.Priority switch
{
    CulturalDataPriority.Sacred => CulturalConflictResolution.PreserveSacredData,
    CulturalDataPriority.Critical => CulturalConflictResolution.LatestTimestamp,
    CulturalDataPriority.High => CulturalConflictResolution.LatestTimestamp,
    CulturalDataPriority.Medium => CulturalConflictResolution.MergeChanges,
    CulturalDataPriority.Low => CulturalConflictResolution.AcceptRemote,
    _ => CulturalConflictResolution.LatestTimestamp
};

// AFTER:
return stateData.Priority switch
{
    CulturalDataPriority.Level10Sacred => CulturalConflictResolution.PreserveSacredData,
    CulturalDataPriority.Level9Religious => CulturalConflictResolution.PreserveSacredData,
    CulturalDataPriority.Level8Traditional => CulturalConflictResolution.LatestTimestamp,
    CulturalDataPriority.Level7Cultural => CulturalConflictResolution.LatestTimestamp,
    CulturalDataPriority.Level6Community => CulturalConflictResolution.LatestTimestamp,
    CulturalDataPriority.Level5General => CulturalConflictResolution.MergeChanges,
    CulturalDataPriority.Level4Social => CulturalConflictResolution.MergeChanges,
    CulturalDataPriority.Level3Commercial => CulturalConflictResolution.AcceptRemote,
    CulturalDataPriority.Level2Administrative => CulturalConflictResolution.AcceptRemote,
    CulturalDataPriority.Level1System => CulturalConflictResolution.AcceptRemote,
    _ => CulturalConflictResolution.LatestTimestamp
};
```

**Pattern 3: Direct Comparisons**
```csharp
// BEFORE (Line 82):
return Priority == CulturalDataPriority.Sacred || Priority == CulturalDataPriority.Critical;

// AFTER:
return Priority == CulturalDataPriority.Level10Sacred ||
       Priority >= CulturalDataPriority.Level8Traditional;
```

**Pattern 4: Method Parameters & Returns**
```csharp
// BEFORE (Line 173):
public bool CanHandlePriority(CulturalDataPriority priority)
{
    return priority switch
    {
        CulturalDataPriority.Sacred => Capabilities.SupportsSacredDataReplication,
        CulturalDataPriority.Critical => Capabilities.SupportsCriticalDataReplication,
        CulturalDataPriority.High => ReplicationHealth >= 0.7,
        CulturalDataPriority.Medium => ReplicationHealth >= 0.5,
        CulturalDataPriority.Low => true,
        _ => false
    };
}

// AFTER:
public bool CanHandlePriority(CulturalDataPriority priority)
{
    return priority switch
    {
        CulturalDataPriority.Level10Sacred => Capabilities.SupportsSacredDataReplication,
        CulturalDataPriority.Level9Religious => Capabilities.SupportsSacredDataReplication,
        CulturalDataPriority.Level8Traditional => Capabilities.SupportsCriticalDataReplication,
        CulturalDataPriority.Level7Cultural => Capabilities.SupportsCriticalDataReplication,
        CulturalDataPriority.Level6Community => ReplicationHealth >= 0.7,
        CulturalDataPriority.Level5General => ReplicationHealth >= 0.5,
        CulturalDataPriority.Level4Social => ReplicationHealth >= 0.5,
        CulturalDataPriority.Level3Commercial => true,
        CulturalDataPriority.Level2Administrative => true,
        CulturalDataPriority.Level1System => true,
        _ => false
    };
}
```

**Pattern 5: ConflictResolution Strategy (Line 579)**
```csharp
// BEFORE:
ConflictResolutionStrategy.PreserveSacred =>
    localData.Priority == CulturalDataPriority.Sacred ?
        Result<CulturalStateData>.Success(localData) :
        Result<CulturalStateData>.Success(remoteData),

// AFTER:
ConflictResolutionStrategy.PreserveSacred =>
    localData.Priority >= CulturalDataPriority.Level9Religious ?
        Result<CulturalStateData>.Success(localData) :
        Result<CulturalStateData>.Success(remoteData),
```

### Complete File Reference Locations

**All locations in `CulturalStateReplicationService.cs` that reference the old enum:**

| Line | Context | Change Required |
|------|---------|-----------------|
| 17 | Property declaration | None (type alias) |
| 28 | Constructor parameter | None (type alias) |
| 49 | Method parameter | None (type alias) |
| 82 | Comparison (`Sacred`, `Critical`) | Map to Level10/Level8+ |
| 173 | Method parameter | None (type alias) |
| 177-181 | Switch statement (5 cases) | Map to 10-level values |
| 377 | Dictionary declaration | None (type alias) |
| 389 | Dictionary parameter | None (type alias) |
| 393 | Dictionary initialization | None (type alias) |
| 405 | Dictionary parameter | None (type alias) |
| 473 | Method parameter | None (type alias) |
| 502-506 | Switch statement (5 cases) | Map to 10-level values |
| 519 | Dictionary declaration | None (type alias) |
| 524 | Dictionary parameter | None (type alias) |
| 528 | Dictionary initialization | None (type alias) |
| 534 | Dictionary parameter | None (type alias) |
| 579 | Comparison (`Sacred`) | Map to Level10/Level9+ |
| 624 | Property declaration | None (type alias) |
| 633 | Constructor parameter | None (type alias) |
| 651 | Method parameter | None (type alias) |
| 692 | Dictionary declaration | None (type alias) |
| 702 | Dictionary parameter | None (type alias) |
| 710 | Dictionary initialization | None (type alias) |
| 719 | Dictionary initialization | None (type alias) |
| 724 | Method parameter | None (type alias) |
| 726 | Dictionary declaration | None (type alias) |
| 782-789 | **ENUM DEFINITION** | **DELETE COMPLETELY** |

**Total Changes:** 3 switch statements + 2 comparisons = 5 logical updates

## Validation Checklist

### Pre-Execution
- [ ] Backup current working state
- [ ] Note current error count: **91 errors**
- [ ] Identify all test files that may be affected

### During Execution
- [ ] Add type alias to top of file
- [ ] Delete enum definition (lines 782-789)
- [ ] Update first switch statement (line 177)
- [ ] Update second switch statement (line 502)
- [ ] Update comparisons (lines 82, 579)
- [ ] Build and check error count

### Post-Execution
- [ ] Verify error reduction (target: 85 errors, -6 reduction)
- [ ] No new errors introduced
- [ ] Run affected unit tests
- [ ] Validate semantic correctness of priority mappings

## Expected Outcomes

### Build Metrics
- **Before:** 91 errors
- **After:** ~85 errors
- **Reduction:** 6-8 errors

### Affected Error Types
- ✅ CS0104: Ambiguous `SacredPriorityLevel` references
- ✅ CS0104: Ambiguous `CulturalDataPriority` references in Failover namespace

### Unaffected Areas
- ✅ All BackupRecoveryModels references (already fully qualified)
- ✅ All disaster recovery orchestrator logic
- ✅ All domain value objects
- ✅ All application interfaces

## Risk Assessment

**Risk Level:** 🟢 LOW

**Reasons:**
1. Single file affected
2. Type alias provides backward compatibility for declarations
3. Only switch statements and comparisons need semantic updates
4. No interface changes
5. No breaking changes to public APIs

## Rollback Plan

If unexpected issues occur:

1. **Immediate Rollback:**
   ```bash
   git checkout src/LankaConnect.Domain/Infrastructure/Failover/CulturalStateReplicationService.cs
   ```

2. **Restore enum definition at line 782**

3. **Remove type alias**

4. **Rebuild to confirm return to 91 errors**

## Execution Timeline

| Phase | Duration | Status |
|-------|----------|--------|
| Preparation | 2 min | ⏳ Pending |
| Add type alias | 1 min | ⏳ Pending |
| Delete enum | 1 min | ⏳ Pending |
| Update switch statements | 5 min | ⏳ Pending |
| Update comparisons | 2 min | ⏳ Pending |
| Build validation | 3 min | ⏳ Pending |
| Testing | 5 min | ⏳ Pending |
| **TOTAL** | **19 min** | ⏳ Pending |

## Next Steps

1. **Execute consolidation** using this plan
2. **Build and validate** error reduction
3. **Document results** in completion report
4. **Proceed to next duplicate type** (if any remain)

---

**Plan Status:** ✅ READY FOR EXECUTION
**Risk Level:** 🟢 LOW
**Expected Impact:** 6-8 error reduction with zero regressions
