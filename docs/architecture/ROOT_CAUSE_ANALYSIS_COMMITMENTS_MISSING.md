# Root Cause Analysis: Missing Commitments in API Response

**Date**: 2025-12-19
**Status**: Investigation In Progress
**Severity**: High - Data Consistency Issue

## Problem Statement

Event "Monthly Dana January 2026" (`0458806b-8672-4ad5-a7cb-f5346f1b282a`) has a sign-up item "Rice Tay" that shows:
- `committedQuantity: 2` (2 items committed)
- `commitments: []` (empty commitments array)
- User expectation: "Someone has taken 2 items, and that person's name should be displayed"

This is a critical data inconsistency where the aggregated count indicates commitments exist, but the collection is empty.

## Investigation Findings

### 1. Repository Layer Analysis

**File**: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`

**Lines 55-63**: `GetByIdAsync()` uses proper eager loading:
```csharp
var eventEntity = await _dbSet
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Include(e => e.Registrations)
    .Include("_emailGroupEntities")
    .Include(e => e.SignUpLists)
        .ThenInclude(s => s.Items)
            .ThenInclude(i => i.Commitments)  // ✅ Commitments are included
    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
```

**Analysis**:
- EF Core is configured to eagerly load the full object graph
- `.ThenInclude(i => i.Commitments)` should load all commitments for each item
- No filtering or `Where()` clause that would exclude commitments
- **VERDICT**: Repository configuration is correct

### 2. Query Handler Analysis

**File**: `src/LankaConnect.Application/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandler.cs`

**Lines 61-73**: Commitments are mapped directly from domain collection:
```csharp
Commitments = item.Commitments.Select(c => new SignUpCommitmentDto
{
    Id = c.Id,
    SignUpItemId = c.SignUpItemId,
    UserId = c.UserId,
    ItemDescription = c.ItemDescription,
    Quantity = c.Quantity,
    CommittedAt = c.CommittedAt,
    Notes = c.Notes,
    ContactName = c.ContactName,  // Mapped directly
    ContactEmail = c.ContactEmail,
    ContactPhone = c.ContactPhone
}).ToList()
```

**Analysis**:
- No filtering logic - straightforward projection
- If `item.Commitments` is empty, the DTO will have an empty list
- **VERDICT**: Query handler is correct - it maps what it receives from the domain

### 3. Domain Entity Analysis

**File**: `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`

**Lines 14, 28**: Private backing field with read-only collection:
```csharp
private readonly List<SignUpCommitment> _commitments = new();
public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();
```

**Lines 321-322**: Computed property for committed quantity:
```csharp
public int GetCommittedQuantity() => Quantity - RemainingQuantity;
```

**KEY FINDING**: `committedQuantity` is NOT persisted - it's calculated on the fly!

**Analysis**:
- `Commitments` collection uses private backing field `_commitments`
- EF Core must populate `_commitments` during hydration
- `committedQuantity` is derived: `Quantity - RemainingQuantity`
- If `RemainingQuantity` is stale (not updated when commitment removed), we get this exact symptom
- **VERDICT**: This is a calculated vs actual data mismatch

### 4. EF Core Configuration Analysis

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

**Lines 64-67**: Commitments relationship configuration:
```csharp
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

**CRITICAL FINDING**: No explicit backing field mapping for Commitments!

Compare with `SignUpListConfiguration.cs` (Lines 82-84):
```csharp
builder.Navigation(s => s.Items)
    .UsePropertyAccessMode(PropertyAccessMode.Field);  // ✅ Explicit field access
```

**Analysis**:
- `SignUpItem.Commitments` relationship does NOT specify `.UsePropertyAccessMode(PropertyAccessMode.Field)`
- EF Core may be trying to use property setter instead of backing field
- Since `Commitments` is read-only (`IReadOnlyList`), EF Core cannot populate it via property
- **VERDICT**: Missing EF Core configuration for backing field access

### 5. Database State Hypothesis

Two possible scenarios:

**Scenario A: Orphaned Quantity (Most Likely)**
1. Commitments were created and `RemainingQuantity` was decremented
2. Commitments were deleted (database cascade or manual deletion)
3. `RemainingQuantity` was NOT recalculated
4. Result: `committedQuantity = Quantity - RemainingQuantity = 5 - 3 = 2` (phantom count)

**Scenario B: EF Core Hydration Failure**
1. Commitments exist in database with correct foreign key
2. EF Core cannot hydrate them into `_commitments` backing field
3. Collection remains empty but `RemainingQuantity` reflects actual commitments
4. Result: Valid data in DB, but not loaded into domain model

## Root Cause Determination

### Most Likely Cause: Missing EF Core Backing Field Configuration

**Evidence**:
1. `SignUpItemConfiguration` does NOT specify `.UsePropertyAccessMode(PropertyAccessMode.Field)` for Commitments
2. `SignUpItem.Commitments` is `IReadOnlyList` (no setter)
3. Backing field `_commitments` is private
4. EF Core cannot populate read-only properties without explicit field access mode

**Fix Required**:
```csharp
// SignUpItemConfiguration.cs - Add after line 67
builder.Navigation(si => si.Commitments)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

### Secondary Issue: Potential Data Corruption

Even with the fix, existing data may have:
- Orphaned `RemainingQuantity` values
- Deleted commitments without quantity recalculation
- Inconsistent state between database and calculated fields

## Diagnostic SQL Queries

### Query 1: Check Database State for Rice Tay Item
```sql
SELECT
    si.id AS item_id,
    si.item_description,
    si.quantity AS total_quantity,
    si.remaining_quantity,
    (si.quantity - si.remaining_quantity) AS calculated_committed_qty,
    COUNT(sc.id) AS actual_commitments_count,
    SUM(sc.quantity) AS actual_committed_quantity
FROM sign_up_items si
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd'
GROUP BY si.id, si.item_description, si.quantity, si.remaining_quantity;
```

**Expected Results**:
- If `actual_commitments_count = 0` but `calculated_committed_qty = 2`: **Scenario A (orphaned quantity)**
- If `actual_commitments_count > 0` but API returns `[]`: **Scenario B (EF Core hydration failure)**

### Query 2: Check All Commitments for the Item
```sql
SELECT
    sc.id,
    sc.sign_up_item_id,
    sc.user_id,
    sc.item_description,
    sc.quantity,
    sc.contact_name,
    sc.contact_email,
    sc.committed_at,
    sc.created_at
FROM sign_up_commitments sc
WHERE sc.sign_up_item_id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd'
ORDER BY sc.committed_at DESC;
```

### Query 3: Check for Global Query Filters
```sql
-- Check if sign_up_commitments table has soft-delete column
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'sign_up_commitments'
  AND column_name IN ('is_deleted', 'deleted_at', 'deleted', 'status');
```

## Fix Strategy

### Phase 1: Immediate Database Verification
1. Run diagnostic SQL queries against staging database
2. Determine if commitments exist in DB or if quantity is orphaned
3. Document current state

### Phase 2: Code Fix
1. Add explicit backing field configuration to `SignUpItemConfiguration`
2. Test with existing data to confirm commitments load correctly
3. Add integration test to prevent regression

### Phase 3: Data Repair (If Needed)
If Scenario A (orphaned quantity):
```sql
-- Recalculate RemainingQuantity based on actual commitments
UPDATE sign_up_items si
SET remaining_quantity = si.quantity - COALESCE(
    (SELECT SUM(sc.quantity)
     FROM sign_up_commitments sc
     WHERE sc.sign_up_item_id = si.id),
    0
)
WHERE si.id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd';
```

### Phase 4: Prevention
1. Add database constraint to ensure `RemainingQuantity` integrity
2. Add domain event handler to recalculate on commitment changes
3. Add automated test that verifies `committedQuantity` matches actual commitments

## Next Steps

1. **IMMEDIATE**: Run diagnostic SQL queries to determine exact state
2. **CODE FIX**: Add backing field configuration to EF Core
3. **DATA AUDIT**: Check for other items with same inconsistency
4. **TESTING**: Verify fix with integration tests
5. **DOCUMENTATION**: Update ADR with findings and solution

## Related Files

- Domain: `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`
- Repository: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`
- Configuration: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`
- Query Handler: `src/LankaConnect.Application/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandler.cs`

## References

- Phase 6A.28: Similar issue with collection change tracking documented
- ADR-008: Private backing field EF Core change tracking patterns
- `SignUpListConfiguration`: Example of correct backing field usage (line 83-84)
