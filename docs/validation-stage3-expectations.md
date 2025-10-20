# Stage 3: Duplicate Type Resolution - Expected Checkpoints

**Timeline:** First 30-60 minutes of 2-day emergency
**Primary Error Type:** CS0101 (duplicate type definitions)
**Current Baseline:** 676 total errors, 4 CS0101

## Duplicate Type Fixes

### 1. ServiceLevelAgreement (DatabasePerformanceMonitoringSupportingTypes.cs line 342)
**Location:** `src/LankaConnect.Infrastructure/Database/LoadBalancing/DatabasePerformanceMonitoringSupportingTypes.cs:342`
**Conflict:** Already defined in same namespace
**Expected Impact:** -1 error (676 → 675)

**Fix Strategy:**
```csharp
// Remove duplicate at line 342, keep canonical definition
// Verify all usages reference correct definition
```

### 2. PerformanceMonitoringConfiguration (DatabasePerformanceMonitoringSupportingTypes.cs line 17)
**Location:** `src/LankaConnect.Infrastructure/Database/LoadBalancing/DatabasePerformanceMonitoringSupportingTypes.cs:17`
**Conflict:** Already defined in same namespace
**Expected Impact:** -1 error (675 → 674)

**Fix Strategy:**
```csharp
// Remove duplicate at line 17, keep canonical definition
// Ensure all references are updated
```

## Stage 3 Total Impact
**Expected Reduction:** 2 errors
**Final Stage 3 Count:** 674 errors
**Remaining CS0101:** 2 (other files, if any)

## Post-Stage 3 Error Distribution
**Expected:**
- CS0535 (Missing implementations): ~402 (unchanged)
- CS0738 (Return type mismatch): ~76 (unchanged)
- CS0246 (Missing types): ~176 (unchanged)
- CS0234 (Missing namespace members): ~12 (unchanged)
- CS0101 (Duplicates): 2 (reduced from 4)

## Validation Commands

### After ServiceLevelAgreement Fix:
```bash
# Run manual checkpoint
pwsh scripts/manual-checkpoint.ps1 "Stage 3.1: ServiceLevelAgreement fix"

# Verify error reduction
dotnet build 2>&1 | grep -c "error CS"  # Expect: 675
dotnet build 2>&1 | grep "CS0101" | wc -l  # Expect: 3
```

### After PerformanceMonitoringConfiguration Fix:
```bash
# Run manual checkpoint
pwsh scripts/manual-checkpoint.ps1 "Stage 3.2: PerformanceMonitoringConfiguration fix"

# Verify error reduction
dotnet build 2>&1 | grep -c "error CS"  # Expect: 674
dotnet build 2>&1 | grep "CS0101" | wc -l  # Expect: 2
```

## Critical Success Criteria
1. Total errors decrease by exactly 2
2. CS0101 count decreases by exactly 2
3. No new error types introduced
4. CS0535/CS0738 remain stable (Stage 4 will reduce these)

## Alert Conditions
- **CRITICAL:** If total errors INCREASE
- **WARNING:** If CS0535 or CS0738 change unexpectedly
- **WARNING:** If new error types appear
- **INFO:** If CS0101 doesn't decrease as expected

## Next Stage Transition
After Stage 3 completion (674 errors):
→ **Stage 4:** Interface implementation fixes
→ Target: Reduce CS0535 (402) and CS0738 (76) to ZERO
→ Final target: ~194 errors (all CS0246 missing type definitions)
