# TDD-Based Enum Consolidation Strategy: Zero-Error Incremental Approach

## Executive Summary

This strategy guarantees ZERO compilation errors at each step by following TDD principles: establish baseline tests, make incremental changes, validate after each step, and provide rollback commands.

## Current State Analysis

### Baseline (2025-10-10)
```bash
Current Errors: 6 (all CS0246 namespace issues in ICulturalEventDetector.cs)
```

### Enum Definitions Found
1. **BackupRecoveryModels.cs** (Line 58) - **CANONICAL VERSION**
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

2. **CulturalStateReplicationService.cs** (Line 782) - **DUPLICATE** (Different values!)
   ```csharp
   public enum CulturalDataPriority
   {
       Sacred, Critical, High, Medium, Low
   }
   ```

3. **BackupDisasterRecoveryTests.cs** (Line 1250) - **TEST-LOCAL ALIAS**
   ```csharp
   public enum SacredPriorityLevel
   {
       Level5General = 5,
       Level6Social = 6,
       Level7Community = 7,
       Level8Cultural = 8,
       Level9HighSacred = 9,
       Level10Sacred = 10
   }
   ```

### References Analysis

**SacredPriorityLevel** is used in 7 files:
1. `MissingTypeStubs.cs` - Property definition
2. `BackupTypes.cs` - Property definition
3. `SacredEventRecoveryOrchestrator.cs` - 5 references to property
4. `CulturalIntelligenceBackupEngine.cs` - 10 references
5. `AutoScalingDecisionTests.cs` - Test usage
6. `CulturalEventTypeConsolidationTests.cs` - Test usage
7. `BackupDisasterRecoveryTests.cs` - Test definition and 38 usages

## TDD Strategy: Incremental Zero-Error Consolidation

### Phase 0: Pre-Consolidation (Establish Test Baseline)

**Objective**: Create failing tests that will pass after consolidation

#### Step 0.1: Create Verification Tests
```bash
# Create test file
New-Item -Path "C:\Work\LankaConnect\tests\LankaConnect.Domain.Tests\EnumConsolidation" -ItemType Directory -Force
```

**Test File**: `EnumConsolidationVerificationTests.cs`
```csharp
using Xunit;
using FluentAssertions;

namespace LankaConnect.Domain.Tests.EnumConsolidation
{
    public class EnumConsolidationVerificationTests
    {
        [Fact]
        public void SacredPriorityLevel_ShouldNotExist_InProductionCode()
        {
            // This test will FAIL initially - that's expected (RED phase)
            var domainAssembly = typeof(LankaConnect.Domain.Common.Database.CulturalDataPriority).Assembly;
            var infrastructureAssembly = typeof(LankaConnect.Infrastructure.DisasterRecovery.CulturalIntelligenceBackupEngine).Assembly;

            // Search for any type named SacredPriorityLevel
            var sacredPriorityInDomain = domainAssembly.GetTypes()
                .Any(t => t.Name == "SacredPriorityLevel");
            var sacredPriorityInInfra = infrastructureAssembly.GetTypes()
                .Any(t => t.Name == "SacredPriorityLevel");

            sacredPriorityInDomain.Should().BeFalse("SacredPriorityLevel should be consolidated to CulturalDataPriority");
            sacredPriorityInInfra.Should().BeFalse("SacredPriorityLevel should be consolidated to CulturalDataPriority");
        }

        [Fact]
        public void CulturalDataPriority_ShouldHaveExactlyOneDefinition()
        {
            var domainAssembly = typeof(LankaConnect.Domain.Common.Database.CulturalDataPriority).Assembly;

            var culturalDataPriorityTypes = domainAssembly.GetTypes()
                .Where(t => t.Name == "CulturalDataPriority" && t.IsEnum)
                .ToList();

            culturalDataPriorityTypes.Should().HaveCount(1,
                "There should be exactly one CulturalDataPriority enum definition");
        }

        [Fact]
        public void CulturalDataPriority_ShouldBeInBackupRecoveryModels()
        {
            var canonicalType = typeof(LankaConnect.Domain.Common.Database.CulturalDataPriority);

            canonicalType.Namespace.Should().Be("LankaConnect.Domain.Common.Database");
            canonicalType.DeclaringType.Name.Should().Be("BackupRecoveryModels");
        }
    }
}
```

**Validation Command**:
```powershell
cd C:\Work\LankaConnect
dotnet test tests/LankaConnect.Domain.Tests --filter "FullyQualifiedName~EnumConsolidation" --no-build
# Expected: All tests FAIL (this is RED phase)
```

**Git Checkpoint**:
```bash
git add tests/LankaConnect.Domain.Tests/EnumConsolidation/
git commit -m "[TDD RED] Add failing tests for enum consolidation verification"
```

### Phase 1: Remove Duplicate CulturalDataPriority in CulturalStateReplicationService.cs

**Risk**: LOW - This enum has DIFFERENT values, likely unused
**Expected Error Change**: 0 (same count or reduction)

#### Step 1.1: Analyze Usage
```powershell
cd C:\Work\LankaConnect
rg "CulturalDataPriority\.(Sacred|Critical|High|Medium|Low)" --type cs
# If NO results: safe to delete
```

**Validation Command**:
```bash
# Before change
dotnet build --no-restore 2>&1 | grep -c "error"
# Record: 6 errors
```

#### Step 1.2: Delete Duplicate Enum
```powershell
# Read the file, remove lines 782-789
$file = "C:\Work\LankaConnect\src\LankaConnect.Domain\Infrastructure\Failover\CulturalStateReplicationService.cs"
$content = Get-Content $file
$newContent = $content[0..781] + $content[790..$content.Length]
$newContent | Set-Content $file
```

**Validation Command**:
```bash
cd C:\Work\LankaConnect
dotnet build --no-restore 2>&1 | grep -c "error"
# Expected: 6 errors (same or less, never more)
```

**Rollback Command**:
```bash
git checkout src/LankaConnect.Domain/Infrastructure/Failover/CulturalStateReplicationService.cs
```

**Git Checkpoint**:
```bash
git add src/LankaConnect.Domain/Infrastructure/Failover/CulturalStateReplicationService.cs
git commit -m "[Step 1.2] Remove duplicate CulturalDataPriority enum (string values)"
git tag "consolidation-step-1.2"
```

### Phase 2: Update All References to Use Canonical Enum

**Risk**: MEDIUM - Multiple files need updates
**Expected Error Change**: Should DECREASE gradually

#### Step 2.1: Update MissingTypeStubs.cs
```csharp
// BEFORE (line 35):
public CulturalDataPriority SacredPriorityLevel { get; init; }

// AFTER:
public LankaConnect.Domain.Common.Database.CulturalDataPriority SacredPriorityLevel { get; init; }
```

**Validation Command**:
```bash
dotnet build src/LankaConnect.Domain/LankaConnect.Domain.csproj --no-restore 2>&1 | tail -5
# Expected: Compiles successfully or same error count
```

**Rollback Command**:
```bash
git checkout src/LankaConnect.Domain/Shared/MissingTypeStubs.cs
```

**Git Checkpoint**:
```bash
git add src/LankaConnect.Domain/Shared/MissingTypeStubs.cs
git commit -m "[Step 2.1] Update MissingTypeStubs to use canonical CulturalDataPriority"
git tag "consolidation-step-2.1"
```

#### Step 2.2: Update BackupTypes.cs
```csharp
// BEFORE (line 148):
public required LankaConnect.Domain.Common.Database.CulturalDataPriority SacredPriorityLevel { get; set; }

// AFTER (no change needed - already using FQN)
# This file is already correct - validate only
```

**Validation Command**:
```bash
dotnet build src/LankaConnect.Domain/LankaConnect.Domain.csproj --no-restore
# Expected: Success or same error count
```

#### Step 2.3: Update SacredEventRecoveryOrchestrator.cs (5 references)

**Add using directive at top**:
```csharp
using LankaConnect.Domain.Common.Database;
```

**Replace all occurrences**:
```csharp
// BEFORE: sacredEvent.SacredPriorityLevel
// AFTER: Same (property name doesn't change)

// BEFORE: LankaConnect.Domain.Common.Database.CulturalDataPriority.Level8Traditional
// AFTER: CulturalDataPriority.Level8Traditional (after adding using)
```

**PowerShell Update Script**:
```powershell
$file = "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\DisasterRecovery\SacredEventRecoveryOrchestrator.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'LankaConnect\.Domain\.Common\.Database\.CulturalDataPriority', 'CulturalDataPriority'
$content | Set-Content $file

# Add using statement if not present
$lines = Get-Content $file
if ($lines -notcontains "using LankaConnect.Domain.Common.Database;") {
    $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
    $lines = $lines[0..$usingIndex] + "using LankaConnect.Domain.Common.Database;" + $lines[($usingIndex+1)..($lines.Length-1)]
    $lines | Set-Content $file
}
```

**Validation Command**:
```bash
dotnet build src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj --no-restore 2>&1 | tail -10
# Expected: Fewer errors or success
```

**Rollback Command**:
```bash
git checkout src/LankaConnect.Infrastructure/DisasterRecovery/SacredEventRecoveryOrchestrator.cs
```

**Git Checkpoint**:
```bash
git add src/LankaConnect.Infrastructure/DisasterRecovery/SacredEventRecoveryOrchestrator.cs
git commit -m "[Step 2.3] Update SacredEventRecoveryOrchestrator to use canonical CulturalDataPriority"
git tag "consolidation-step-2.3"
```

#### Step 2.4: Update CulturalIntelligenceBackupEngine.cs (10 references)

**Same process as Step 2.3**:
```powershell
$file = "C:\Work\LankaConnect\src\LankaConnect.Infrastructure\DisasterRecovery\CulturalIntelligenceBackupEngine.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'LankaConnect\.Domain\.Common\.Database\.CulturalDataPriority', 'CulturalDataPriority'
$content | Set-Content $file

# Add using statement
$lines = Get-Content $file
if ($lines -notcontains "using LankaConnect.Domain.Common.Database;") {
    $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
    $lines = $lines[0..$usingIndex] + "using LankaConnect.Domain.Common.Database;" + $lines[($usingIndex+1)..($lines.Length-1)]
    $lines | Set-Content $file
}
```

**Validation Command**:
```bash
dotnet build src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj --no-restore 2>&1 | tail -10
# Expected: Fewer errors or success
```

**Git Checkpoint**:
```bash
git add src/LankaConnect.Infrastructure/DisasterRecovery/CulturalIntelligenceBackupEngine.cs
git commit -m "[Step 2.4] Update CulturalIntelligenceBackupEngine to use canonical CulturalDataPriority"
git tag "consolidation-step-2.4"
```

### Phase 3: Update Test Files (Isolated from Production)

**Risk**: LOW - Test changes don't affect production build

#### Step 3.1: Update AutoScalingDecisionTests.cs
```powershell
# Replace SacredPriorityLevel with CulturalDataPriority
$file = "C:\Work\LankaConnect\tests\LankaConnect.Domain.Tests\Shared\AutoScalingDecisionTests.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'SacredPriorityLevel', 'CulturalDataPriority'
$content | Set-Content $file

# Add using statement
$lines = Get-Content $file
if ($lines -notcontains "using LankaConnect.Domain.Common.Database;") {
    $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
    $lines = $lines[0..$usingIndex] + "using LankaConnect.Domain.Common.Database;" + $lines[($usingIndex+1)..($lines.Length-1)]
    $lines | Set-Content $file
}
```

**Validation Command**:
```bash
dotnet build tests/LankaConnect.Domain.Tests/LankaConnect.Domain.Tests.csproj --no-restore
# Expected: Success or same error count
```

**Git Checkpoint**:
```bash
git add tests/LankaConnect.Domain.Tests/Shared/AutoScalingDecisionTests.cs
git commit -m "[Step 3.1] Update AutoScalingDecisionTests to use canonical CulturalDataPriority"
git tag "consolidation-step-3.1"
```

#### Step 3.2: Update CulturalEventTypeConsolidationTests.cs
```powershell
# Same process as Step 3.1
$file = "C:\Work\LankaConnect\tests\LankaConnect.Domain.Tests\Common\Enums\CulturalEventTypeConsolidationTests.cs"
$content = Get-Content $file -Raw
$content = $content -replace 'SacredPriorityLevel', 'CulturalDataPriority'
$content | Set-Content $file

# Add using statement
$lines = Get-Content $file
if ($lines -notcontains "using LankaConnect.Domain.Common.Database;") {
    $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
    $lines = $lines[0..$usingIndex] + "using LankaConnect.Domain.Common.Database;" + $lines[($usingIndex+1)..($lines.Length-1)]
    $lines | Set-Content $file
}
```

**Validation Command**:
```bash
dotnet build tests/LankaConnect.Domain.Tests/LankaConnect.Domain.Tests.csproj --no-restore
# Expected: Success
```

**Git Checkpoint**:
```bash
git add tests/LankaConnect.Domain.Tests/Common/Enums/CulturalEventTypeConsolidationTests.cs
git commit -m "[Step 3.2] Update CulturalEventTypeConsolidationTests to use canonical enum"
git tag "consolidation-step-3.2"
```

#### Step 3.3: Update BackupDisasterRecoveryTests.cs (Remove local enum)

**Action**: Delete lines 1250-1258 (local enum definition)
```powershell
$file = "C:\Work\LankaConnect\tests\LankaConnect.Infrastructure.Tests\Database\BackupDisasterRecoveryTests.cs"
$content = Get-Content $file
$newContent = $content[0..1249] + $content[1259..$content.Length]
$newContent | Set-Content $file

# Add using statement at top
$lines = Get-Content $file
if ($lines -notcontains "using LankaConnect.Domain.Common.Database;") {
    $usingIndex = ($lines | Select-String "^using" | Select-Object -Last 1).LineNumber - 1
    $lines = $lines[0..$usingIndex] + "using LankaConnect.Domain.Common.Database;" + $lines[($usingIndex+1)..($lines.Length-1)]
    $lines | Set-Content $file
}

# Update all SacredPriorityLevel references to CulturalDataPriority
$content = Get-Content $file -Raw
$content = $content -replace 'SacredPriorityLevel\.Level', 'CulturalDataPriority.Level'
$content = $content -replace 'SacredPriorityLevel\s+SacredPriorityLevel', 'CulturalDataPriority SacredPriorityLevel'
$content | Set-Content $file
```

**Validation Command**:
```bash
dotnet build tests/LankaConnect.Infrastructure.Tests/LankaConnect.Infrastructure.Tests.csproj --no-restore
# Expected: Success
```

**Git Checkpoint**:
```bash
git add tests/LankaConnect.Infrastructure.Tests/Database/BackupDisasterRecoveryTests.cs
git commit -m "[Step 3.3] Remove local SacredPriorityLevel enum from BackupDisasterRecoveryTests"
git tag "consolidation-step-3.3"
```

### Phase 4: Final Validation and Green Tests

#### Step 4.1: Full Solution Build
```bash
cd C:\Work\LankaConnect
dotnet build --no-restore 2>&1 | tee docs/consolidation-build-output.txt
```

**Expected Result**: Error count should be ≤ 6 (same or better)

**Validation Command**:
```bash
dotnet build --no-restore 2>&1 | grep -E "error|Build (FAILED|succeeded)"
```

#### Step 4.2: Run Verification Tests (Should Now PASS)
```bash
cd C:\Work\LankaConnect
dotnet test tests/LankaConnect.Domain.Tests --filter "FullyQualifiedName~EnumConsolidation"
# Expected: All tests PASS (GREEN phase)
```

#### Step 4.3: Grep Verification
```bash
# Verify no SacredPriorityLevel in production code
rg "enum\s+SacredPriorityLevel" --type cs src/
# Expected: No results

# Verify single CulturalDataPriority definition
rg "enum\s+CulturalDataPriority" --type cs src/
# Expected: 1 result (BackupRecoveryModels.cs)
```

#### Step 4.4: Final Git Checkpoint
```bash
git add -A
git commit -m "[TDD GREEN] Complete enum consolidation: SacredPriorityLevel → CulturalDataPriority"
git tag "consolidation-complete"
```

## Rollback Strategy

### Complete Rollback
```bash
# Revert everything back to pre-consolidation state
git reset --hard HEAD~10
git tag -d consolidation-step-*
git tag -d consolidation-complete
```

### Partial Rollback (to specific checkpoint)
```bash
# List all checkpoints
git tag | grep consolidation

# Rollback to specific step
git reset --hard consolidation-step-2.3
```

### Emergency Rollback (if build breaks)
```bash
# Immediate rollback to last working state
git stash
git reset --hard HEAD~1
dotnet build --no-restore
```

## Success Metrics

### Green Criteria
- [ ] Build error count: ≤ 6 (never increases)
- [ ] Verification tests: All PASS
- [ ] Grep check: No `enum SacredPriorityLevel` in src/
- [ ] Grep check: Exactly 1 `enum CulturalDataPriority` in src/
- [ ] All unit tests: PASS
- [ ] No new CS0104 ambiguity errors

### Red Flags (Immediate Rollback)
- Build error count increases
- New CS0104 ambiguity errors appear
- Verification tests fail after supposed completion
- More than 1 CulturalDataPriority enum definition found

## Execution Timeline

**Estimated Duration**: 45-60 minutes
- Phase 0: 10 minutes (test creation)
- Phase 1: 5 minutes (delete duplicate)
- Phase 2: 20 minutes (update production code)
- Phase 3: 15 minutes (update tests)
- Phase 4: 10 minutes (validation)

## Automated Execution Script

Save this as `scripts/execute-enum-consolidation.ps1`:

```powershell
# TDD Enum Consolidation Automated Script
$ErrorActionPreference = "Stop"

function Test-BuildErrors {
    $errors = (dotnet build --no-restore 2>&1 | Select-String "error").Count
    Write-Host "Current error count: $errors" -ForegroundColor Yellow
    return $errors
}

function Checkpoint {
    param($message, $tag)
    git add -A
    git commit -m $message
    git tag $tag
    Write-Host "✓ Checkpoint: $tag" -ForegroundColor Green
}

Write-Host "=== Phase 0: Baseline ===" -ForegroundColor Cyan
$baseline = Test-BuildErrors
Write-Host "Baseline errors: $baseline" -ForegroundColor Yellow

Write-Host "`n=== Phase 1: Remove Duplicate Enum ===" -ForegroundColor Cyan
# [Insert Step 1.2 code here]
$phase1 = Test-BuildErrors
if ($phase1 -gt $baseline) {
    Write-Host "ERROR: Errors increased! Rolling back..." -ForegroundColor Red
    git checkout src/LankaConnect.Domain/Infrastructure/Failover/CulturalStateReplicationService.cs
    exit 1
}
Checkpoint "[Step 1.2] Remove duplicate CulturalDataPriority" "consolidation-step-1.2"

# [Continue for all phases...]

Write-Host "`n=== SUCCESS: Consolidation Complete ===" -ForegroundColor Green
Write-Host "Final error count: $(Test-BuildErrors)" -ForegroundColor Green
```

## Key Principles Applied

1. **TDD Red-Green-Refactor**: Start with failing tests, make them pass
2. **Incremental Changes**: One file at a time, validate each step
3. **Reversibility**: Every step has a rollback command
4. **Zero Error Increase**: Never allow error count to increase
5. **Test-First**: Create verification tests before making changes
6. **Isolation**: Update production code before test code
7. **Validation Gates**: Build check after every step

## References

- Canonical Enum: `src/LankaConnect.Domain/Common/Database/BackupRecoveryModels.cs:58`
- Test Verification: `tests/LankaConnect.Domain.Tests/EnumConsolidation/`
- Rollback Guide: See "Rollback Strategy" section above
