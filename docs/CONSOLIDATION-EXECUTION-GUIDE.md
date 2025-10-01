# Duplicate Type Consolidation - Quick Execution Guide

**Status**: Ready for Execution
**Baseline Errors**: 510
**Target Errors**: <200
**Estimated Duration**: 90-120 minutes

## Pre-Flight Checklist ✅

```powershell
# 1. Establish baseline
cd C:/Work/LankaConnect
dotnet build --no-incremental 2>&1 | tee baseline_errors.txt
grep "Error(s)" baseline_errors.txt  # Should show: 510 Error(s)

# 2. Create git checkpoint
git add -A
git commit -m "Pre-consolidation checkpoint: 510 errors"
git tag pre-consolidation-phase1a

# 3. Ready to execute
```

## Execution Sequence (7 Types)

### 1. RegionalComplianceStatus (5 min) - Risk: LOW
```powershell
# Remove duplicate
Remove-Item src/LankaConnect.Application/Common/Models/Performance/PerformanceMonitoringTypes.cs

# Build and verify
dotnet build --no-incremental
# Expected: 502-508 errors (2-8 reduction)

# Checkpoint
git add -A && git commit -m "Consolidate RegionalComplianceStatus: 510 → [COUNT] errors"
```

### 2. DisasterRecoveryProcedure (5 min) - Risk: LOW
```powershell
# Surgically remove from SecurityFoundationTypes.cs
# Use Edit tool: Remove DisasterRecoveryProcedure class only

# Build and verify
dotnet build --no-incremental
# Expected: 492-502 errors (8-10 reduction cumulative)

# Checkpoint
git add -A && git commit -m "Consolidate DisasterRecoveryProcedure: [OLD] → [NEW] errors"
```

### 3. AccessPatternAnalysis (7 min) - Risk: MEDIUM
```powershell
# Remove from AuditAccessTypes.cs
# Add using LankaConnect.Domain.Common.Security; where needed

# Build and verify
dotnet build --no-incremental
# Expected: 477-492 errors (15-18 reduction cumulative)

# Checkpoint
git add -A && git commit -m "Consolidate AccessPatternAnalysis: [OLD] → [NEW] errors"
```

### 4. FailoverConfiguration (10 min) - Risk: MEDIUM
```powershell
# Remove from AutoScalingExtendedTypes.cs
# Add using LankaConnect.Domain.Infrastructure.Failover; where needed

# Build and verify
dotnet build --no-incremental
# Expected: 462-477 errors (15-20 reduction cumulative)

# Checkpoint
git add -A && git commit -m "Consolidate FailoverConfiguration: [OLD] → [NEW] errors"

# ⚠️ ROLLBACK TRIGGER: If CS0029 (cannot convert) errors appear
```

### 5. PerformanceThreshold (15 min) - Risk: MEDIUM-HIGH
```powershell
# Step 1: Remove first duplicate
Remove-Item src/LankaConnect.Application/Common/Models/Critical/ComprehensiveRemainingTypes.cs
# (Or edit if file has other needed types)

# Build checkpoint
dotnet build --no-incremental

# Step 2: Remove second duplicate
# Remove from HighImpactResultTypes.cs

# Build and verify
dotnet build --no-incremental
# Expected: 431-462 errors (31-40 reduction cumulative)

# Checkpoint
git add -A && git commit -m "Consolidate PerformanceThreshold: [OLD] → [NEW] errors"

# ⚠️ ROLLBACK TRIGGER: If >20 new errors from factory method issues
```

### 6. SecurityLevel (20 min) - Risk: HIGH
```powershell
# Step 1: Fix incorrect enum values FIRST
# Find and replace SecurityLevel.Basic with SecurityLevel.Internal
grep -rl "SecurityLevel\.Basic" src/ --include="*.cs"
# Edit: SecurityLevel.Basic → SecurityLevel.Internal

# Step 2: Build with fixes
dotnet build --no-incremental

# Step 3: Remove test duplicate (if exists)
# Add using LankaConnect.Domain.Common.Database; to test files

# Build and verify
dotnet build --no-incremental
# Expected: 348-431 errors (83+ reduction cumulative)

# Checkpoint
git add -A && git commit -m "Consolidate SecurityLevel: [OLD] → [NEW] errors"

# ⚠️ ROLLBACK TRIGGER: If >50 new errors appear
```

### 7. CulturalCommunityType (30 min) - Risk: CRITICAL
```powershell
# Add using statement to test file
# Add to CulturalEventLoadDistributionServiceFocusedTests.cs:
# using LankaConnect.Domain.Common;

# Remove enum from test file
# Edit: Delete enum CulturalCommunityType { ... }

# Build and verify
dotnet build --no-incremental
# Expected: <200 errors (187+ reduction cumulative)
# 🎯 TARGET: <200 TOTAL ERRORS

# Run affected tests
dotnet test --filter "FullyQualifiedName~CulturalEventLoadDistribution"

# Final checkpoint
git add -A && git commit -m "Consolidate CulturalCommunityType: [OLD] → [NEW] errors - PHASE 1-A COMPLETE"

# ⚠️ ROLLBACK TRIGGER: If error count INCREASES
```

## Post-Consolidation Validation (10 min)

```powershell
# 1. Final build
dotnet build --no-incremental 2>&1 | tee post_consolidation_errors.txt

# 2. Extract final error count
grep "Error(s)" post_consolidation_errors.txt
# 🎯 SUCCESS: <200 errors

# 3. Run test suite
dotnet test --no-build

# 4. Generate summary report
# Document: 510 → [FINAL] errors ([REDUCTION] eliminated, [PERCENT]% reduction)
```

## Success Criteria ✅

- [ ] All 7 types consolidated successfully
- [ ] Final error count <200 (from 510 baseline)
- [ ] Zero test failures introduced
- [ ] Clean Architecture principles enforced
- [ ] No rollbacks required (ideal case)

## Rollback Procedures 🔄

### Single Type Rollback
```powershell
# Rollback last commit
git reset --hard HEAD~1

# Verify restoration
dotnet build --no-incremental

# Re-evaluate strategy for that type
```

### Full Rollback
```powershell
# Return to pre-consolidation state
git reset --hard pre-consolidation-phase1a

# Verify baseline restored
dotnet build --no-incremental  # Should show 510 errors
```

## Error Count Tracking

| Step | Type | Target Error Count | Status |
|------|------|--------------------|--------|
| 0 | Baseline | 510 | ✅ |
| 1 | RegionalComplianceStatus | 502-508 | ⬜ |
| 2 | DisasterRecoveryProcedure | 492-502 | ⬜ |
| 3 | AccessPatternAnalysis | 477-492 | ⬜ |
| 4 | FailoverConfiguration | 462-477 | ⬜ |
| 5 | PerformanceThreshold | 431-462 | ⬜ |
| 6 | SecurityLevel | 348-431 | ⬜ |
| 7 | CulturalCommunityType | **<200** | ⬜ |

## Key Decision Points

### Stop Points (When to Abort)
- Error count INCREASES after any step
- >50 new errors in single step (except step 5)
- CS0029 (cannot convert) errors from ValueObject changes
- >2 rollbacks required (indicates strategy failure)

### Continue Points (When to Proceed)
- Error count decreases (even if less than expected)
- Only expected error types (CS0246, CS0535, CS0738)
- Tests remain passing (or failures don't increase)

## Communication Template

After each type:
```markdown
✅ Type [N]/7: [TypeName] consolidated
- Duration: [X] minutes
- Errors: [OLD] → [NEW] ([REDUCTION] eliminated)
- Status: SUCCESS
- Next: [NextType] (Risk: [LEVEL])
```

## Reference

Full strategy: `docs/ADR-DUPLICATE-TYPE-CONSOLIDATION-STRATEGY.md`
Memory key: `swarm/architect/consolidation-strategy`

---

**Ready to Execute**: All prerequisites met, strategy validated
**Estimated Completion**: 90-120 minutes from start
**Success Probability**: HIGH (low-risk-first approach, incremental validation)
