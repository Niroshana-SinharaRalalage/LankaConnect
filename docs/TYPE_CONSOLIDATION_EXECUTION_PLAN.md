# TYPE CONSOLIDATION EXECUTION PLAN

**Date**: 2025-10-10
**Goal**: Eliminate all duplicate types and achieve zero CS0104/CS0246 errors
**Estimated Time**: 6 hours with validation checkpoints
**Zero Tolerance**: No more than 10 new errors per checkpoint

## Pre-Execution Checklist

### 1. Create Backup Checkpoint
```powershell
cd C:\Work\LankaConnect
git add .
git commit -m "[Checkpoint] Before type consolidation - 198 errors baseline"
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
git tag "pre-consolidation-$timestamp"
Write-Host "Checkpoint created: pre-consolidation-$timestamp"
```

### 2. Document Current Error Baseline
```powershell
dotnet build --no-incremental 2>&1 | Tee-Object docs/baseline-errors-before-consolidation.txt
$errorCount = (Select-String "error CS" docs/baseline-errors-before-consolidation.txt).Count
Write-Host "Baseline error count: $errorCount"
```

### 3. Type Inventory Verification
```powershell
# Verify duplicate types exist
Write-Host "=== CulturalEvent Duplicates ==="
Get-ChildItem -Recurse -Include "*.cs" -Path "src" |
    Select-String "class CulturalEvent\s|record CulturalEvent\s" |
    Select-Object Path, LineNumber | Format-Table

Write-Host "=== CulturalDataPriority Duplicates ==="
Get-ChildItem -Recurse -Include "*.cs" -Path "src" |
    Select-String "enum CulturalDataPriority\s" |
    Select-Object Path, LineNumber | Format-Table
```

## Phase 1: CulturalDataPriority Consolidation (2 hours)

### Step 1.1: Identify Canonical Definition

**Location**: `src\LankaConnect.Domain\Common\Database\BackupRecoveryModels.cs:58-70`

**Canonical Enum** (10 levels):
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

**Reason**: More granular, matches business requirements, already in correct namespace

### Step 1.2: Delete Duplicate Definition

**File to Modify**: `src\LankaConnect.Domain\Infrastructure\Failover\CulturalStateReplicationService.cs`

**Lines to Delete**: 782-789

**Duplicate Enum** (5 levels - DELETE):
```csharp
public enum CulturalDataPriority
{
    Sacred,
    Critical,
    High,
    Medium,
    Low
}
```

**Action**:
```powershell
# Read the file
$filePath = "src\LankaConnect.Domain\Infrastructure\Failover\CulturalStateReplicationService.cs"
$content = Get-Content $filePath -Raw

# Remove the duplicate enum (lines 782-789)
# This requires careful editing - using Edit tool
```

**Manual Edit Required**:
- Delete lines 782-789 in CulturalStateReplicationService.cs
- Add import: `using LankaConnect.Domain.Common.Enums;`

### Step 1.3: Create Priority Mapping

**5-Level to 10-Level Mapping**:
```csharp
// In files using old 5-level system, replace:
CulturalDataPriority.Sacred    → CulturalDataPriority.Level10Sacred
CulturalDataPriority.Critical  → CulturalDataPriority.Level9Religious
CulturalDataPriority.High      → CulturalDataPriority.Level7Cultural
CulturalDataPriority.Medium    → CulturalDataPriority.Level5General
CulturalDataPriority.Low       → CulturalDataPriority.Level3Commercial
```

**Files Requiring Update** (estimated 8-12 files):
- CulturalStateReplicationService.cs (main usage)
- Any file referencing 5-level priorities

### Step 1.4: Validation Checkpoint

```powershell
# Rebuild Domain project
dotnet build src\LankaConnect.Domain\LankaConnect.Domain.csproj --no-incremental

# Count errors
$afterStep1 = (dotnet build --no-incremental 2>&1 | Select-String "error CS").Count
Write-Host "Errors after Step 1: $afterStep1 (Expected: 180-190)"

# Success criteria: Fewer errors, no new CS0104 for CulturalDataPriority
dotnet build 2>&1 | Select-String "CulturalDataPriority" | Select-String "CS0104"
# Expected output: NONE
```

## Phase 2: CulturalEvent Consolidation (2 hours)

### Step 2.1: Analyze Both Implementations

**Option A** - `Domain\Common\Database\CulturalEvent.cs` (DELETE THIS):
```csharp
public class CulturalEvent : BaseEntity  // Simple entity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime EventDate { get; set; }
    // ... 15 more simple properties
}
```
- **Type**: Entity (database model)
- **Purpose**: Persistence
- **Complexity**: Low (18 properties)
- **Architectural Violation**: Database model in Domain layer

**Option B** - `Domain\Communications\ValueObjects\CulturalEvent.cs` (KEEP THIS):
```csharp
public class CulturalEvent : ValueObject  // Rich domain model
{
    public DateTime Date { get; init; }
    public string EnglishName { get; init; }
    public string NativeName { get; init; }
    public IEnumerable<MultiLingualName> MultiLingualNames { get; init; }
    // ... 20+ more properties with cultural intelligence

    public static CulturalEvent CreateDiwali(...) { }
    public static CulturalEvent CreateEidUlFitr(...) { }
    // ... rich factory methods
}
```
- **Type**: ValueObject (domain model)
- **Purpose**: Business logic, cultural intelligence
- **Complexity**: High (25+ properties, factory methods)
- **Architecture**: Correct location (Domain.Communications)

**DECISION**: Keep ValueObject (Option B), eliminate Entity (Option A)

### Step 2.2: Find All References to Database CulturalEvent

```powershell
# Find all usages
$databaseCulturalEventFiles = Get-ChildItem -Recurse -Include "*.cs" -Path "src" |
    Where-Object { (Get-Content $_.FullName -Raw) -match "using LankaConnect\.Domain\.Common\.Database" } |
    Where-Object { (Get-Content $_.FullName -Raw) -match "CulturalEvent" }

Write-Host "Files referencing Database.CulturalEvent:"
$databaseCulturalEventFiles | Select-Object FullName | Format-Table
```

**Expected Files** (15-20 files):
- Load balancing services
- Performance monitoring
- Database-related infrastructure

### Step 2.3: Delete Database CulturalEvent Entity

```powershell
# Delete the file
Remove-Item "src\LankaConnect.Domain\Common\Database\CulturalEvent.cs" -Verbose

# Verify deletion
if (Test-Path "src\LankaConnect.Domain\Common\Database\CulturalEvent.cs") {
    Write-Error "File still exists!"
} else {
    Write-Host "Successfully deleted Database.CulturalEvent"
}
```

### Step 2.4: Update References (File by File)

**For each file found in Step 2.2:**

**Pattern A** - Simple usage as data holder:
```csharp
// BEFORE
using LankaConnect.Domain.Common.Database;

var event = new CulturalEvent
{
    Name = "Vesak",
    EventDate = DateTime.Now
};

// AFTER
using LankaConnect.Domain.Communications.ValueObjects;

var event = new CulturalEvent(
    date: DateTime.Now,
    englishName: "Vesak",
    nativeName: "වෙසක්",
    secondaryName: "Buddha Day",
    primaryCommunity: CulturalCommunity.SriLankanBuddhist,
    // ... other required parameters
);
```

**Pattern B** - Using static factory methods (already correct):
```csharp
// BEFORE
using LankaConnect.Domain.Common.Database;

var vesak = CulturalEvent.Vesak;  // Static property

// AFTER - Use Communications ValueObject
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;

var vesak = CulturalEvent.CreateDiwali(
    date: DateTime.Now,
    community: CulturalCommunity.SriLankanBuddhist
);
```

**Pattern C** - Database queries (needs DTO):
```csharp
// BEFORE
var events = dbContext.CulturalEvents.Where(e => e.IsActive).ToList();

// AFTER - Create DTO or use existing CulturalEventModels
using LankaConnect.Domain.Common.Database.CulturalEventModels;

var events = dbContext.CulturalEventData
    .Where(e => e.IsActive)
    .Select(e => new CulturalEventDto
    {
        // Map properties
    })
    .ToList();
```

### Step 2.5: Validation Checkpoint

```powershell
# Rebuild after deletions
dotnet clean
dotnet build src\LankaConnect.Domain\LankaConnect.Domain.csproj --no-incremental

# Check for ambiguity errors
dotnet build --no-incremental 2>&1 | Select-String "CulturalEvent" | Select-String "CS0104"
# Expected: NONE (only one CulturalEvent exists now)

# Count total errors
$afterStep2 = (dotnet build --no-incremental 2>&1 | Select-String "error CS").Count
Write-Host "Errors after Step 2: $afterStep2 (Expected: 150-170)"
```

## Phase 3: Fix ICulturalEventDetector (30 minutes)

### Step 3.1: Current State (INVALID)

**File**: `src\LankaConnect.Application\Common\Interfaces\ICulturalEventDetector.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Database;  // ← This was causing ambiguity
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;           // ← This was causing ambiguity

public interface ICulturalEventDetector
{
    Task<Result<List<Shared.CulturalEvent>>> DetectEventsAsync(...);
    //                 ^^^^^^^^^^^^^^^^^^^^^
    //                 INVALID SYNTAX IN C#
}
```

### Step 3.2: Corrected Implementation

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;  // ← Only CulturalEvent source

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalEventDetector
{
    Task<Result<List<CulturalEvent>>> DetectEventsAsync(
        //             ^^^^^^^^^^^^^
        //             Unambiguous - only ONE type exists
        string dataSource,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalDataPriority>> ClassifyEventPriorityAsync(
        CulturalEvent culturalEvent,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ValidateEventDataAsync(
        CulturalEvent culturalEvent,
        CancellationToken cancellationToken = default);

    Task<Result<List<SouthAsianLanguage>>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default);
}
```

### Step 3.3: Apply Fix

```powershell
# This will be done via Edit tool in execution
# Remove lines 2 and 4 (Database and Shared imports)
# Add line 3 (Communications.ValueObjects import)
# Update return types on lines 16, 26, 33
```

### Step 3.4: Validation Checkpoint

```powershell
# Build Application project
dotnet build src\LankaConnect.Application\LankaConnect.Application.csproj --no-incremental

# Check specific file
dotnet build --no-incremental 2>&1 | Select-String "ICulturalEventDetector"
# Expected: Zero errors

$afterStep3 = (dotnet build --no-incremental 2>&1 | Select-String "error CS").Count
Write-Host "Errors after Step 3: $afterStep3 (Expected: 140-160)"
```

## Phase 4: Infrastructure Layer Updates (1 hour)

### Step 4.1: Update Affected Services

**Files Requiring Updates** (estimated):
1. CulturalAffinityGeographicLoadBalancer.cs
2. CulturalEventLoadDistributionService.cs
3. DatabasePerformanceMonitoringSupportingTypes.cs
4. BackupDisasterRecoveryEngine.cs (if using CulturalEvent)

**Update Pattern**:
```csharp
// BEFORE
using LankaConnect.Domain.Common.Database;

// AFTER
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common.Enums;
```

### Step 4.2: Validation Checkpoint

```powershell
# Build Infrastructure project
dotnet build src\LankaConnect.Infrastructure\LankaConnect.Infrastructure.csproj --no-incremental

$afterStep4 = (dotnet build --no-incremental 2>&1 | Select-String "error CS").Count
Write-Host "Errors after Step 4: $afterStep4 (Expected: 100-120)"
```

## Phase 5: Full Solution Build (30 minutes)

### Step 5.1: Clean Build

```powershell
# Clean all projects
dotnet clean

# Full rebuild
dotnet build --no-incremental 2>&1 | Tee-Object docs/build-after-consolidation.txt
```

### Step 5.2: Error Analysis

```powershell
# Count by error code
$errors = Get-Content docs/build-after-consolidation.txt | Select-String "error CS"

Write-Host "=== Error Summary ==="
$errors | ForEach-Object { $_.ToString().Split(":")[3].Trim().Substring(0,6) } |
    Group-Object |
    Sort-Object Count -Descending |
    Format-Table Name, Count

# Expected error types:
# CS0246 - Type not found (should be ZERO)
# CS0104 - Ambiguous reference (should be ZERO)
# CS0029 - Cannot convert type (acceptable, easy fix)
# CS1061 - Member not found (acceptable, property mapping)
```

### Step 5.3: Success Validation

```powershell
# Check critical criteria
$cs0104 = ($errors | Select-String "CS0104").Count
$cs0246 = ($errors | Select-String "CS0246").Count
$totalErrors = $errors.Count

Write-Host "=== Consolidation Success Metrics ==="
Write-Host "CS0104 (Ambiguity) errors: $cs0104 (Target: 0)"
Write-Host "CS0246 (Type not found) errors: $cs0246 (Target: 0)"
Write-Host "Total errors: $totalErrors (Target: <100)"

if ($cs0104 -eq 0 -and $cs0246 -eq 0) {
    Write-Host "SUCCESS: Type consolidation complete!" -ForegroundColor Green
    Write-Host "Remaining errors are type conversion/mapping issues" -ForegroundColor Yellow
} else {
    Write-Host "FAILURE: Ambiguity still exists" -ForegroundColor Red
    Write-Host "Review docs/build-after-consolidation.txt for details"
}
```

## Phase 6: Commit and Document (15 minutes)

### Step 6.1: Commit Changes

```powershell
git add .
git commit -m "[Type Consolidation] Eliminate CulturalEvent and CulturalDataPriority duplicates

- Deleted Domain.Common.Database.CulturalEvent (simple entity)
- Consolidated to Domain.Communications.ValueObjects.CulturalEvent (rich model)
- Deleted Infrastructure.Failover.CulturalDataPriority (5-level)
- Consolidated to Common.Database.CulturalDataPriority (10-level)
- Fixed ICulturalEventDetector invalid syntax
- Updated all import statements

Error reduction: 198 → $totalErrors
CS0104 ambiguity errors: ELIMINATED
CS0246 type not found errors: ELIMINATED"
```

### Step 6.2: Create Completion Report

```powershell
$report = @"
# TYPE CONSOLIDATION COMPLETION REPORT

**Date**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
**Duration**: [Fill in actual time]

## Results

- **Starting errors**: 198
- **Ending errors**: $totalErrors
- **Error reduction**: $([math]::Round((198-$totalErrors)/198*100, 1))%
- **CS0104 eliminated**: $cs0104 remaining (Target: 0) - $(if($cs0104 -eq 0){"SUCCESS"}else{"NEEDS WORK"})
- **CS0246 eliminated**: $cs0246 remaining (Target: 0) - $(if($cs0246 -eq 0){"SUCCESS"}else{"NEEDS WORK"})

## Types Consolidated

1. **CulturalEvent**: 2 → 1 definition
   - Deleted: Domain.Common.Database.CulturalEvent
   - Kept: Domain.Communications.ValueObjects.CulturalEvent

2. **CulturalDataPriority**: 2 → 1 definition
   - Deleted: Infrastructure.Failover (5 levels)
   - Kept: Common.Database (10 levels)

## Files Modified

- [List generated files]

## Remaining Work

- Type conversion errors: [Count]
- Property mapping fixes: [Count]
- Test updates: [Estimate]

## Next Steps

1. Fix type conversion errors (CS0029)
2. Update property mappings (CS1061)
3. Run test suite
4. Update documentation

"@

$report | Out-File docs/TYPE_CONSOLIDATION_COMPLETION_REPORT.md
```

## Rollback Procedure (If Needed)

```powershell
# If consolidation fails catastrophically:
$tag = git tag --list "pre-consolidation-*" | Select-Object -Last 1
Write-Host "Rolling back to: $tag"
git reset --hard $tag

# Verify rollback
dotnet build --no-incremental 2>&1 | Select-String "error CS" | Measure-Object
# Should show 198 errors (original baseline)
```

## Execution Checklist

**Pre-execution**:
- [ ] Backup checkpoint created
- [ ] Baseline errors documented (198)
- [ ] Type inventory verified

**Phase 1 - CulturalDataPriority**:
- [ ] Step 1.1: Canonical definition identified
- [ ] Step 1.2: Duplicate deleted
- [ ] Step 1.3: Priority mappings updated
- [ ] Step 1.4: Validation passed (CS0104 = 0 for CulturalDataPriority)

**Phase 2 - CulturalEvent**:
- [ ] Step 2.1: Implementations analyzed
- [ ] Step 2.2: References found
- [ ] Step 2.3: Database entity deleted
- [ ] Step 2.4: All references updated
- [ ] Step 2.5: Validation passed (CS0104 = 0 for CulturalEvent)

**Phase 3 - ICulturalEventDetector**:
- [ ] Step 3.1: Current state analyzed
- [ ] Step 3.2: Corrected implementation written
- [ ] Step 3.3: Fix applied
- [ ] Step 3.4: Validation passed (0 errors in file)

**Phase 4 - Infrastructure**:
- [ ] Step 4.1: Services updated
- [ ] Step 4.2: Validation passed

**Phase 5 - Full Build**:
- [ ] Step 5.1: Clean build executed
- [ ] Step 5.2: Error analysis completed
- [ ] Step 5.3: Success criteria met (CS0104=0, CS0246=0)

**Phase 6 - Commit**:
- [ ] Step 6.1: Changes committed
- [ ] Step 6.2: Completion report created

## Success Definition

**Consolidation is SUCCESSFUL if**:
1. CS0104 ambiguity errors = 0
2. CS0246 type not found errors = 0
3. Total errors < 100
4. All types in architecturally correct namespaces

**Consolidation is ACCEPTABLE if**:
1. CS0104 = 0 (no ambiguities)
2. CS0246 = 0 (all types found)
3. Remaining errors are CS0029 (type conversion) or CS1061 (member access)

**Consolidation has FAILED if**:
1. CS0104 > 0 (ambiguities still exist)
2. CS0246 > 10 (types still missing)
3. More errors than starting point (198)

---

**Ready to Execute**: YES
**Estimated Time**: 6 hours with validation
**Risk Level**: MEDIUM (with checkpoints and rollback plan)
