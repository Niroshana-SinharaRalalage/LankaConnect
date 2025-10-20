# Enum Consolidation Quick Reference

## One-Command Execution

```powershell
cd C:\Work\LankaConnect
.\scripts\execute-enum-consolidation.ps1
```

## Manual Step-by-Step

### 1. Run Verification Tests (Should FAIL initially)
```bash
cd C:\Work\LankaConnect
dotnet test tests/LankaConnect.Domain.Tests --filter "FullyQualifiedName~EnumConsolidation"
# Expected: Tests FAIL (RED phase)
```

### 2. Check Current Errors
```bash
dotnet build --no-restore 2>&1 | grep -c "error"
# Record baseline: 6 errors
```

### 3. Run Automated Consolidation
```powershell
.\scripts\execute-enum-consolidation.ps1
```

### 4. Verify Success
```bash
# All verification tests should PASS
dotnet test tests/LankaConnect.Domain.Tests --filter "FullyQualifiedName~EnumConsolidation"

# No SacredPriorityLevel in production code
rg "enum\s+SacredPriorityLevel" --type cs src/
# Expected: No results

# Exactly one CulturalDataPriority
rg "enum\s+CulturalDataPriority" --type cs src/
# Expected: 1 result (BackupRecoveryModels.cs)

# Error count same or better
dotnet build --no-restore 2>&1 | grep -c "error"
# Expected: ≤ 6
```

## Rollback Commands

### Emergency Rollback (Last Step)
```bash
git reset --hard HEAD~1
```

### Rollback to Specific Checkpoint
```bash
git tag | grep consolidation
git reset --hard consolidation-step-2.1
```

### Complete Rollback
```bash
git reset --hard HEAD~10
git tag -d consolidation-step-*
git tag -d consolidation-complete
```

## Key Files Modified

1. **CulturalStateReplicationService.cs** - Duplicate enum removed
2. **SacredEventRecoveryOrchestrator.cs** - Updated references
3. **CulturalIntelligenceBackupEngine.cs** - Updated references
4. **BackupDisasterRecoveryTests.cs** - Local enum removed

## Success Criteria

- ✓ Build errors: ≤ 6 (never increases)
- ✓ Verification tests: All PASS
- ✓ No `enum SacredPriorityLevel` in src/
- ✓ Exactly 1 `enum CulturalDataPriority` in src/

## Canonical Enum Location

**File**: `src/LankaConnect.Domain/Common/Database/BackupRecoveryModels.cs`
**Line**: 58
**Namespace**: `LankaConnect.Domain.Common.Database`

```csharp
public enum CulturalDataPriority
{
    Level10Sacred = 10,
    Level9Religious = 9,
    Level8Traditional = 8,
    Level7Cultural = 7,
    Level6Community = 6,
    Level5General = 5,
    Level4Social = 4,
    Level3Commercial = 3,
    Level2Administrative = 2,
    Level1System = 1
}
```

## Timeline

**Estimated Duration**: 5-10 minutes (automated)
**Manual Duration**: 45-60 minutes

## Support

**Full Documentation**: `docs/TDD_ENUM_CONSOLIDATION_STRATEGY.md`
**Script**: `scripts/execute-enum-consolidation.ps1`
**Tests**: `tests/LankaConnect.Domain.Tests/EnumConsolidation/`
