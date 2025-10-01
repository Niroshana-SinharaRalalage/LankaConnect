# CS0104 Ambiguous Reference Resolution Strategy

## Executive Summary

**Total CS0104 Errors:** 76 errors
**Affected Layer:** Infrastructure
**Root Cause:** Duplicate type definitions across Application.Common.Interfaces and Domain/Application layers
**Resolution Approach:** Consolidate to canonical namespaces + use fully qualified names where needed

---

## Critical Findings

### 1. PRIMARY AMBIGUOUS TYPES (High Frequency - 28+ occurrences each)

#### A. GeographicRegion (28 errors)
**Conflict:**
- `LankaConnect.Domain.Common.Enums.GeographicRegion` (enum) ✅ CANONICAL
- `LankaConnect.Application.Common.Interfaces.GeographicRegion` (record class) ❌ DUPLICATE

**Analysis:**
- Domain enum at `src/LankaConnect.Domain/Common/Enums/GeographicRegion.cs` is comprehensive with 54 values
- Interface embeds a record class at line 807-811 in `IDatabaseSecurityOptimizationEngine.cs`
- The record class is a wrapper type that should not exist at interface level

**Canonical Source:** `LankaConnect.Domain.Common.Enums.GeographicRegion`

**Resolution:**
1. **DELETE** the embedded record at line 807-811 in `IDatabaseSecurityOptimizationEngine.cs`
2. Use Domain enum directly in all interfaces
3. Update all Infrastructure files to use fully qualified: `Domain.Common.Enums.GeographicRegion`

**Affected Files:**
```
- src/LankaConnect.Infrastructure/Security/ICulturalSecurityService.cs (lines 21-24, 48, 67-68)
- src/LankaConnect.Infrastructure/Security/MockImplementations.cs (lines 30, 36, 42, 48, 121, 157, 163)
- src/LankaConnect.Infrastructure/Database/LoadBalancing/DatabaseSecurityOptimizationEngine.cs (interface implementation)
```

---

#### B. SecurityLevel (12 errors)
**Conflict:**
- `LankaConnect.Application.Common.Security.SecurityLevel` (enum) ❌ DUPLICATE 1
- `LankaConnect.Application.Common.Interfaces.SecurityLevel` (enum) ❌ DUPLICATE 2

**Analysis:**
- TWO duplicate enums in Application layer:
  - `CrossRegionSecurityTypes.cs` line 270-276 (4 values: Low, Medium, High, Maximum)
  - `IDatabaseSecurityOptimizationEngine.cs` line 854-861 (5 values: Basic, Standard, Enhanced, Maximum, UltraSecure)
- Different value sets indicate inconsistent usage
- Should consolidate to Domain layer

**Canonical Source:** Create new `LankaConnect.Domain.Common.Enums.SecurityLevel`

**Resolution:**
1. **CREATE** new enum in `Domain/Common/Enums/SecurityLevel.cs` with merged values:
   ```csharp
   public enum SecurityLevel
   {
       Basic = 1,
       Low = 2,
       Standard = 3,
       Medium = 4,
       Enhanced = 5,
       High = 6,
       Maximum = 7,
       UltraSecure = 8
   }
   ```
2. **DELETE** both Application layer duplicates
3. Update all references to use `Domain.Common.Enums.SecurityLevel`

**Affected Files:**
```
- src/LankaConnect.Infrastructure/Security/ICulturalSecurityService.cs (lines 104, 106, 117, 120, 125)
- src/LankaConnect.Application/Common/Security/CrossRegionSecurityTypes.cs (line 270)
- src/LankaConnect.Application/Common/Interfaces/IDatabaseSecurityOptimizationEngine.cs (line 854)
```

---

#### C. IncidentSeverity (12 errors)
**Conflict:**
- `LankaConnect.Application.Common.Security.IncidentSeverity` (enum) ❌ DUPLICATE 1
- `LankaConnect.Application.Common.Interfaces.IncidentSeverity` (enum) ❌ DUPLICATE 2
- `LankaConnect.Application.Common.Models.Performance.PerformanceIncident.IncidentSeverity` ❌ DUPLICATE 3
- `LankaConnect.Domain.Common.Notifications.NotificationTypes.IncidentSeverity` ✅ CANONICAL

**Analysis:**
- FOUR duplicate enums across layers
- Domain.Common.Notifications already has this enum (line 215)
- Application layer has 3 duplicates with identical or similar values

**Canonical Source:** `LankaConnect.Domain.Common.Notifications.IncidentSeverity`

**Resolution:**
1. **KEEP** Domain.Common.Notifications version
2. **DELETE** all 3 Application layer duplicates:
   - `CrossRegionSecurityTypes.cs` line 599-605
   - `IDatabaseSecurityOptimizationEngine.cs` line 881-888
   - `PerformanceIncident.cs` line 24-30
3. Update all references to use `Domain.Common.Notifications.IncidentSeverity`

**Affected Files:**
```
- src/LankaConnect.Infrastructure/Security/ICulturalSecurityService.cs (line 111)
```

---

### 2. SECONDARY AMBIGUOUS TYPES (Medium Frequency - 2-4 occurrences each)

#### D. CulturalContext (4 errors)
**Conflict:**
- `LankaConnect.Domain.Common.Database.CulturalContext` ✅ CANONICAL (full entity)
- `LankaConnect.Application.Common.Interfaces.CulturalContext` ❌ DUPLICATE (record)

**Canonical Source:** `LankaConnect.Domain.Common.Database.CulturalContext`

**Resolution:**
1. **DELETE** embedded record at line 714-718 in `IDatabaseSecurityOptimizationEngine.cs`
2. Use Domain entity directly

---

#### E. CulturalSignificance (2 errors)
**Conflict:**
- `LankaConnect.Domain.Common.CulturalSignificance` ✅ CANONICAL
- `LankaConnect.Domain.Common.Database.CulturalSignificance` ❌ DUPLICATE
- `LankaConnect.Application.Common.Interfaces.CulturalSignificance` ❌ DUPLICATE

**Canonical Source:** `LankaConnect.Domain.Common.CulturalSignificance`

**Resolution:**
1. **DELETE** Database and Interface duplicates
2. Consolidate to Domain.Common

---

#### F. CulturalRegion (2 errors)
**Conflict:**
- `LankaConnect.Domain.Common.Database.CulturalRegion` ❌ DUPLICATE
- `LankaConnect.Domain.Shared.CulturalRegion` ✅ CANONICAL

**Canonical Source:** `LankaConnect.Domain.Shared.CulturalRegion`

---

#### G. ComplianceLevel (2 errors)
**Conflict:**
- `LankaConnect.Application.Common.Security.ComplianceLevel` ❌ DUPLICATE
- `LankaConnect.Application.Common.Interfaces.ComplianceLevel` ❌ DUPLICATE

**Canonical Source:** Create in `Domain.Common.Enums.ComplianceLevel`

---

#### H. DateRange (2 errors)
**Conflict:**
- `LankaConnect.Domain.Shared.DateRange` ✅ CANONICAL
- `LankaConnect.Domain.Common.ValueObjects.DateRange` ❌ DUPLICATE

**Canonical Source:** `LankaConnect.Domain.Shared.DateRange`

---

### 3. LANGUAGE ROUTING TYPES (2 errors each - 10 types)

All embedded in `Application.Common.Interfaces` and duplicated in `Domain.Shared`:

- LanguageComplexityAnalysis
- MultiCulturalEventResolution
- CulturalEventLanguagePrediction
- CulturalAppropriatenessValidation
- BatchMultiLanguageRoutingResponse
- HeritageLanguageLearningRecommendations
- LanguageProficiencyLevel
- CulturalEducationPathway
- LanguageServiceType
- LanguageInteractionData
- ScriptComplexity

**Canonical Source:** `LankaConnect.Domain.Shared.*` (KEEP Domain.Shared versions)

**Resolution:** DELETE all embedded types in interface files, use Domain.Shared

---

## Resolution Priority Matrix

### PHASE 1: HIGH PRIORITY (Eliminate 68 errors)
**Target:** GeographicRegion, SecurityLevel, IncidentSeverity
**Impact:** 52 errors resolved
**Effort:** 4 hours

**Actions:**
1. Delete `GeographicRegion` record from `IDatabaseSecurityOptimizationEngine.cs` line 807-811
2. Create canonical `SecurityLevel` enum in `Domain.Common.Enums`
3. Delete 3 `IncidentSeverity` duplicates in Application layer
4. Add using directives with aliases where needed:
   ```csharp
   using DomainGeo = LankaConnect.Domain.Common.Enums.GeographicRegion;
   using DomainSecurity = LankaConnect.Domain.Common.Enums.SecurityLevel;
   using DomainIncident = LankaConnect.Domain.Common.Notifications.IncidentSeverity;
   ```

### PHASE 2: MEDIUM PRIORITY (Eliminate 12 errors)
**Target:** CulturalContext, CulturalSignificance, CulturalRegion, ComplianceLevel
**Impact:** 10 errors resolved
**Effort:** 2 hours

**Actions:**
1. Delete embedded records in `IDatabaseSecurityOptimizationEngine.cs`
2. Consolidate to Domain canonical sources
3. Update Infrastructure references

### PHASE 3: LOW PRIORITY (Eliminate remaining errors)
**Target:** Language routing types, DateRange
**Impact:** 14 errors resolved
**Effort:** 3 hours

**Actions:**
1. Delete all embedded interface types
2. Use Domain.Shared types directly
3. Add proper using directives

---

## Implementation Guide

### Step 1: Pre-Implementation Verification
```bash
# Count current errors
dotnet build 2>&1 | grep "CS0104" | wc -l
# Expected: 76
```

### Step 2: Phase 1 Implementation

#### 2A: Fix GeographicRegion (28 errors)
```csharp
// FILE: src/LankaConnect.Application/Common/Interfaces/IDatabaseSecurityOptimizationEngine.cs
// DELETE lines 807-811 (GeographicRegion record)

// FILE: src/LankaConnect.Infrastructure/Security/ICulturalSecurityService.cs
// ADD at top:
using DomainGeo = LankaConnect.Domain.Common.Enums.GeographicRegion;

// REPLACE all instances of:
GeographicRegion region
// WITH:
DomainGeo region
```

#### 2B: Fix SecurityLevel (12 errors)
```csharp
// FILE: src/LankaConnect.Domain/Common/Enums/SecurityLevel.cs (NEW FILE)
namespace LankaConnect.Domain.Common.Enums;

public enum SecurityLevel
{
    Basic = 1,
    Low = 2,
    Standard = 3,
    Medium = 4,
    Enhanced = 5,
    High = 6,
    Maximum = 7,
    UltraSecure = 8
}

// DELETE:
// - src/LankaConnect.Application/Common/Security/CrossRegionSecurityTypes.cs line 270-276
// - src/LankaConnect.Application/Common/Interfaces/IDatabaseSecurityOptimizationEngine.cs line 854-861

// ADD to all affected files:
using LankaConnect.Domain.Common.Enums;
```

#### 2C: Fix IncidentSeverity (12 errors)
```csharp
// KEEP: src/LankaConnect.Domain/Common/Notifications/NotificationTypes.cs line 215

// DELETE:
// - src/LankaConnect.Application/Common/Security/CrossRegionSecurityTypes.cs line 599-605
// - src/LankaConnect.Application/Common/Interfaces/IDatabaseSecurityOptimizationEngine.cs line 881-888
// - src/LankaConnect.Application/Common/Models/Performance/PerformanceIncident.cs line 24-30

// ADD to affected files:
using LankaConnect.Domain.Common.Notifications;
```

### Step 3: Verification After Each Phase
```bash
# After Phase 1
dotnet build 2>&1 | grep "CS0104" | wc -l
# Expected: 24 or fewer

# After Phase 2
dotnet build 2>&1 | grep "CS0104" | wc -l
# Expected: 14 or fewer

# After Phase 3
dotnet build 2>&1 | grep "CS0104" | wc -l
# Expected: 0
```

---

## Type Consolidation Mapping

| Ambiguous Type | Canonical Namespace | Action | Priority |
|---|---|---|---|
| GeographicRegion | Domain.Common.Enums | Delete Interface record | P1 |
| SecurityLevel | Domain.Common.Enums (NEW) | Create + Delete duplicates | P1 |
| IncidentSeverity | Domain.Common.Notifications | Delete 3 duplicates | P1 |
| CulturalContext | Domain.Common.Database | Delete Interface record | P2 |
| CulturalSignificance | Domain.Common | Delete 2 duplicates | P2 |
| CulturalRegion | Domain.Shared | Delete Database duplicate | P2 |
| ComplianceLevel | Domain.Common.Enums (NEW) | Create + Delete duplicates | P2 |
| DateRange | Domain.Shared | Delete ValueObject duplicate | P3 |
| Language Types (10) | Domain.Shared | Delete Interface embeds | P3 |

---

## Anti-Patterns Identified

### 1. **Embedded Types in Interface Files**
**Problem:** `IDatabaseSecurityOptimizationEngine.cs` contains 50+ type definitions
**Impact:** Creates namespace pollution and ambiguity
**Solution:** Extract to separate files in Domain/Shared

### 2. **Duplicate Enums Across Layers**
**Problem:** Same enum defined in multiple layers with different values
**Impact:** Inconsistent behavior and ambiguity
**Solution:** Single canonical enum in Domain.Common.Enums

### 3. **Application Layer Type Ownership**
**Problem:** Application.Common.Interfaces owns domain types
**Impact:** Violates Clean Architecture dependency rules
**Solution:** Move all domain types to Domain layer

---

## Testing Strategy

### Unit Test Updates Required
```csharp
// Before
var region = GeographicRegion.SouthAsia; // Ambiguous

// After
using DomainGeo = LankaConnect.Domain.Common.Enums.GeographicRegion;
var region = DomainGeo.SouthAsia; // Clear
```

### Integration Test Verification
1. Build succeeds with 0 CS0104 errors
2. All existing tests pass
3. Type references resolve correctly
4. No runtime ambiguity exceptions

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Breaking changes to tests | High | Medium | Update test using directives |
| Runtime type resolution failures | Low | High | Comprehensive build verification |
| Merge conflicts during implementation | Medium | Low | Implement in phases |
| Missing type references | Low | Medium | Use compiler errors as checklist |

---

## Success Criteria

1. ✅ Zero CS0104 errors in build output
2. ✅ All types resolve to canonical namespaces
3. ✅ No embedded types in interface files
4. ✅ All tests pass
5. ✅ Clean Architecture principles preserved

---

## Estimated Timeline

- **Phase 1 (High Priority):** 4 hours - 52 errors eliminated
- **Phase 2 (Medium Priority):** 2 hours - 10 errors eliminated
- **Phase 3 (Low Priority):** 3 hours - 14 errors eliminated
- **Testing & Verification:** 2 hours
- **Total:** 11 hours over 2 days

---

## Next Steps

1. Review this strategy with team/architect
2. Create feature branch: `fix/cs0104-ambiguous-references`
3. Implement Phase 1 first (highest impact)
4. Verify build after each phase
5. Update tests incrementally
6. Create PR with detailed testing results

---

## Memory Storage

Key findings stored at: `swarm/analyzer/cs0104-analysis`

**Summary:**
- 76 CS0104 errors identified
- 3 high-priority types (68 errors): GeographicRegion, SecurityLevel, IncidentSeverity
- Root cause: Interface file embedding domain types
- Solution: Consolidate to Domain layer + delete duplicates
- Phased approach: P1 (52 errors) → P2 (10 errors) → P3 (14 errors)
