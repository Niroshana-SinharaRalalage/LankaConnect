# Duplicate Enum Consolidation Plan
**Date**: 2025-10-10
**Current Errors**: 71
**Task**: Consolidate duplicate `SacredPriorityLevel` enum definitions

---

## Problem Analysis

### Duplicate Enum Discovery
Found **3 different versions** of `SacredPriorityLevel`:

1. **`Domain.CulturalIntelligence.Enums.SacredPriorityLevel`** (SIMPLE VERSION)
   - Location: `src/LankaConnect.Domain/CulturalIntelligence/Enums/SacredPriorityLevel.cs`
   - Values: Low=1, Medium=2, High=3, Critical=4, Sacred=5
   - **SHOULD BE DELETED**

2. **`Domain.Shared.CulturalPriorityTypes.SacredPriorityLevel`** (DOCUMENTED VERSION)
   - Location: `src/LankaConnect.Domain/Shared/CulturalPriorityTypes.cs`
   - Values: Standard=1, Important=2, High=3, Critical=4, Sacred=5, UltraSacred=6
   - Has extension methods: `GetProcessingWeight()`, `RequiresSpecialValidation()`
   - **SHOULD BE DELETED**

3. **`Domain.Common.Database.BackupRecoveryModels.CulturalDataPriority`** (CANONICAL VERSION - KEEP THIS)
   - Location: `src/LankaConnect.Domain/Common/Database/BackupRecoveryModels.cs:58`
   - Values: Level10Sacred=10, Level9Religious=9, Level8Traditional=8, Level7Cultural=7, Level6Community=6, Level5General=5, Level4Social=4, Level3Commercial=3, Level2Administrative=2, Level1System=1
   - **THIS IS THE CORRECT ENUM** - Used throughout backup/recovery infrastructure
   - Already integrated into domain models (BackupRequest, DisasterRecoveryConfiguration, etc.)

### Current Code References

**Files using the WRONG enum name `SacredPriorityLevel`**:
- `SacredEventRecoveryOrchestrator.cs` - lines 357, 389, 393, 403, 407
- `CulturalIntelligenceBackupEngine.cs` - lines 214, 235, 254, 268
- Test files (16 locations)

**Ambiguity Errors (CS0104)**:
- `SacredEventRecoveryOrchestrator.cs:357` - ambiguous between Domain.CulturalIntelligence.Enums vs Domain.Shared
- Multiple other infrastructure files

---

## Consolidation Strategy

### Phase 1: Verify Canonical Enum (COMPLETED)
✅ Confirmed `CulturalDataPriority` is the canonical enum
✅ Located all duplicate definitions
✅ Identified all usage locations

### Phase 2: Replace References (NEXT)
**Goal**: Update all `SacredPriorityLevel` references to `CulturalDataPriority`

**Files to Update**:
1. `SacredEventRecoveryOrchestrator.cs`
   - Line 120: `SacredPriorityLevel.Level5General` → `CulturalDataPriority.Level5General`
   - Line 136: `SacredPriorityLevel.Level8Cultural` → `CulturalDataPriority.Level8Traditional`
   - Line 357: method parameter `SacredPriorityLevel priorityLevel` → `CulturalDataPriority priorityLevel`
   - Line 364: `SacredPriorityLevel.Level8Cultural` → `CulturalDataPriority.Level8Traditional`
   - Lines 389-414: All switch cases update to `CulturalDataPriority`

2. `CulturalIntelligenceBackupEngine.cs`
   - Similar replacements for all `SacredPriorityLevel` → `CulturalDataPriority`

3. **Note Value Mapping**:
   - `Level8Cultural` → `Level8Traditional`
   - `Level9HighSacred` → `Level9Religious`
   - `Level10Sacred` → `Level10Sacred` (same)
   - Other levels map 1:1

**Expected Impact**: -3 to -5 errors (resolve CS0104 ambiguities)

### Phase 3: Delete Duplicate Files (AFTER Phase 2)
**Goal**: Remove duplicate enum definitions

**Files to DELETE**:
1. `src/LankaConnect.Domain/CulturalIntelligence/Enums/SacredPriorityLevel.cs` (entire file)
2. `src/LankaConnect.Domain/Shared/CulturalPriorityTypes.cs` (only the enum, keep other types if present)

**Expected Impact**: -0 errors (should already be fixed by Phase 2)

### Phase 4: Update Test Files
**Goal**: Fix test references to use canonical enum

**Files to Update**:
- All test files using `SacredPriorityLevel` → update to `CulturalDataPriority`
- Update test enum definitions in test files to match canonical

**Expected Impact**: Tests compile successfully

### Phase 5: Validation
1. ✅ Build succeeds
2. ✅ All tests pass
3. ✅ No CS0104 ambiguity errors
4. ✅ Grep confirms no `SacredPriorityLevel` references remain (except in docs/history)

---

## Implementation Order

1. **CONSULT ARCHITECT** - Confirm this consolidation strategy aligns with architectural goals
2. Update `SacredEventRecoveryOrchestrator.cs` references
3. Update `CulturalIntelligenceBackupEngine.cs` references
4. Build and validate (-3 to -5 errors expected)
5. Delete duplicate enum files
6. Build and validate (should maintain error count)
7. Update test files
8. Final validation build
9. Git commit with detailed message

---

## Risk Assessment

**Risk**: LOW
- Canonical enum already used in 20+ domain models
- Simple find/replace operation
- No behavioral changes
- All values map clearly

**Validation Strategy**:
- Incremental: Update one file at a time
- Build after each file
- Zero tolerance for compilation errors

---

**Next Action**: Await architect approval, then proceed with Phase 2
