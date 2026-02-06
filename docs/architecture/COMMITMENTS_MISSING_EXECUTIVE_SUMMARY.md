# Executive Summary: Missing Commitments Root Cause Analysis

**Date**: 2025-12-19
**Issue**: Rice Tay item shows `committedQuantity: 2` but `commitments: []` (empty array)
**Event**: Monthly Dana January 2026 (`0458806b-8672-4ad5-a7cb-f5346f1b282a`)
**Impact**: User cannot see who committed to bringing items
**Status**: Root cause identified, diagnostic queries ready, fix strategy defined

---

## The Problem

API returns inconsistent data for the "Rice Tay" sign-up item:

```json
{
    "id": "9dbce508-743a-4cfd-a222-0c3acafd8bbd",
    "itemDescription": "Rice Tay",
    "quantity": 5,
    "remainingQuantity": 3,
    "commitments": [],  // ← EMPTY (no user names shown)
    "committedQuantity": 2  // ← But this says 2 items committed!
}
```

**User Impact**: Organizer cannot see who committed to bring Rice Tay.

---

## Root Cause Analysis

### Primary Suspect: Missing EF Core Backing Field Configuration

**What We Found**:

1. **Domain Entity** (`SignUpItem.cs`) uses private backing field:
   ```csharp
   private readonly List<SignUpCommitment> _commitments = new();
   public IReadOnlyList<SignUpCommitment> Commitments => _commitments.AsReadOnly();
   ```

2. **EF Core Configuration** (`SignUpItemConfiguration.cs`) is MISSING backing field access mode:
   ```csharp
   builder.HasMany(si => si.Commitments)
       .WithOne()
       .HasForeignKey(sc => sc.SignUpItemId)
       .OnDelete(DeleteBehavior.Cascade);
   // ❌ MISSING: .UsePropertyAccessMode(PropertyAccessMode.Field)
   ```

3. **Comparison**: `SignUpList.Items` works correctly because it HAS the configuration:
   ```csharp
   builder.Navigation(s => s.Items)
       .UsePropertyAccessMode(PropertyAccessMode.Field);  // ✅ CORRECT
   ```

**Why This Matters**:
- `Commitments` property is read-only (no setter)
- EF Core cannot populate read-only properties without explicit field access
- Without the configuration, EF Core may fail to hydrate the `_commitments` backing field
- Result: Collection stays empty even though data exists in database

### Secondary Issue: Orphaned Quantity Values

**How `committedQuantity` is Calculated**:
```csharp
public int GetCommittedQuantity() => Quantity - RemainingQuantity;
// For Rice Tay: 5 - 3 = 2
```

**Problem**: `RemainingQuantity` is a persisted field, not computed.

If commitments were deleted WITHOUT updating `RemainingQuantity`, we get:
- Database: 0 commitments (deleted)
- `RemainingQuantity`: Still shows 3 (should be 5)
- `committedQuantity`: 5 - 3 = 2 (phantom count)

---

## Two Possible Scenarios

### Scenario A: Orphaned Quantity (Data Corruption)
- Commitments were created (decremented `RemainingQuantity` from 5 to 3)
- Commitments were deleted (database cascade or manual deletion)
- `RemainingQuantity` was NOT recalculated back to 5
- **Result**: Database has 0 commitments, but quantity reflects 2 phantom commitments

### Scenario B: EF Core Hydration Failure (Configuration Bug)
- Commitments exist in database with valid foreign keys
- EF Core cannot load them into `_commitments` backing field (missing configuration)
- Collection remains empty in memory
- **Result**: Valid data in database, but not accessible via domain model

---

## Diagnostic Process

### Step 1: Run SQL Diagnostic Queries

**File**: `c:\Work\LankaConnect\scripts\diagnose-commitments.sql`

**Key Query** (determines which scenario):
```sql
SELECT
    si.id AS item_id,
    si.item_description,
    si.quantity AS total_quantity,
    si.remaining_quantity,
    (si.quantity - si.remaining_quantity) AS calculated_committed_qty,
    COUNT(sc.id) AS actual_commitments_in_db,
    SUM(sc.quantity) AS actual_committed_quantity
FROM sign_up_items si
LEFT JOIN sign_up_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd'
GROUP BY si.id, si.item_description, si.quantity, si.remaining_quantity;
```

**How to Interpret Results**:

| `actual_commitments_in_db` | `calculated_committed_qty` | Diagnosis |
|----------------------------|----------------------------|-----------|
| 0 | 2 | **Scenario A**: Orphaned quantity (data corruption) |
| 2 | 2 | **Scenario B**: EF Core hydration failure (config bug) |
| 0 | 0 | False alarm (API cached response?) |

### Step 2: System-Wide Audit

Query 6 in diagnostic script checks ALL items for same discrepancy:
```sql
-- Find all items where calculated != actual
SELECT ...
HAVING (si.quantity - si.remaining_quantity) != COALESCE(SUM(sc.quantity), 0)
```

This determines if issue is isolated or systemic.

---

## Fix Strategy

### Fix 1: Add EF Core Backing Field Configuration (Required)

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

**Add after line 67**:
```csharp
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);

// ADD THIS:
builder.Navigation(si => si.Commitments)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Why**: Ensures EF Core populates `_commitments` backing field during hydration.

### Fix 2: Data Repair (If Scenario A Confirmed)

**If diagnostic shows `actual_commitments_in_db = 0`**:

```sql
-- Recalculate RemainingQuantity for Rice Tay
UPDATE sign_up_items si
SET remaining_quantity = si.quantity - COALESCE(
    (SELECT SUM(sc.quantity)
     FROM sign_up_commitments sc
     WHERE sc.sign_up_item_id = si.id),
    0
)
WHERE si.id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd';
```

**System-wide repair** (if multiple items affected):
```sql
UPDATE sign_up_items si
SET remaining_quantity = si.quantity - COALESCE(
    (SELECT SUM(sc.quantity)
     FROM sign_up_commitments sc
     WHERE sc.sign_up_item_id = si.id),
    0
)
WHERE (si.quantity - si.remaining_quantity) != COALESCE(
    (SELECT SUM(sc.quantity)
     FROM sign_up_commitments sc
     WHERE sc.sign_up_item_id = si.id),
    0
);
```

### Fix 3: Domain Logic Enhancement (Future Prevention)

**Option A**: Make `RemainingQuantity` a computed property (breaking change):
```csharp
public int RemainingQuantity => Quantity - _commitments.Sum(c => c.Quantity);
```

**Option B**: Add database constraint (safer):
```sql
ALTER TABLE sign_up_items
ADD CONSTRAINT chk_remaining_quantity_valid
CHECK (remaining_quantity >= 0 AND remaining_quantity <= quantity);
```

**Option C**: Add recalculation trigger (PostgreSQL):
```sql
CREATE OR REPLACE FUNCTION recalculate_remaining_quantity()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE sign_up_items
    SET remaining_quantity = quantity - COALESCE(
        (SELECT SUM(quantity) FROM sign_up_commitments WHERE sign_up_item_id = NEW.sign_up_item_id),
        0
    )
    WHERE id = NEW.sign_up_item_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER recalculate_on_commitment_change
AFTER INSERT OR UPDATE OR DELETE ON sign_up_commitments
FOR EACH ROW EXECUTE FUNCTION recalculate_remaining_quantity();
```

---

## Implementation Plan

### Phase 1: Immediate Diagnosis (30 minutes)
1. ✅ Root cause analysis document created
2. ✅ Diagnostic SQL script created
3. ⏳ Run diagnostic queries against staging database
4. ⏳ Determine Scenario A vs Scenario B
5. ⏳ Document findings

### Phase 2: Code Fix (1 hour)
1. Add backing field configuration to `SignUpItemConfiguration.cs`
2. Create database migration (if needed)
3. Test with existing staging data
4. Verify commitments now load correctly
5. Create integration test to prevent regression

### Phase 3: Data Repair (If Needed - 30 minutes)
1. If Scenario A: Run repair SQL for Rice Tay item
2. Run system-wide audit (Query 6)
3. If multiple items affected: Run system-wide repair
4. Verify all items now show correct data
5. Document repaired items

### Phase 4: Prevention (2 hours)
1. Add database constraint for `remaining_quantity`
2. Add integration test: verify `committedQuantity` matches actual commitments
3. Consider PostgreSQL trigger for automatic recalculation
4. Update ADR with findings and solution
5. Add monitoring/alerting for future discrepancies

---

## Testing Strategy

### Integration Test (New)
```csharp
[Fact]
public async Task GetEventSignUpLists_ShouldReturnCommitmentsMatchingCalculatedQuantity()
{
    // Arrange
    var eventId = /* existing event with commitments */;
    var query = new GetEventSignUpListsQuery(eventId);

    // Act
    var result = await _handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    foreach (var list in result.Value)
    {
        foreach (var item in list.Items)
        {
            var calculatedCommitted = item.Quantity - item.RemainingQuantity;
            var actualCommitted = item.Commitments.Sum(c => c.Quantity);

            actualCommitted.Should().Be(calculatedCommitted,
                $"Item '{item.ItemDescription}' has inconsistent commitment data");

            item.Commitments.Should().NotBeEmpty(
                $"Item '{item.ItemDescription}' shows committedQuantity={calculatedCommitted} but commitments array is empty");
        }
    }
}
```

---

## Risk Assessment

### Risks if Not Fixed

**High Severity**:
- ✅ **User Trust**: Organizers cannot see who committed (user reported this!)
- ✅ **Data Integrity**: Calculated values don't match actual data
- ✅ **Business Logic**: Sign-up system is unreliable

**Medium Severity**:
- ⚠️ **System-Wide**: Issue may affect multiple events (need audit)
- ⚠️ **Data Loss**: If commitments were deleted, user contact info is lost

**Low Severity**:
- ℹ️ **Performance**: Minimal (EF Core loading commitments is standard)

### Risks of Fix

**Code Changes**:
- ✅ Low risk: Adding `.UsePropertyAccessMode()` is standard EF Core configuration
- ✅ No breaking changes to API or domain model
- ✅ Backwards compatible with existing data

**Data Repair**:
- ⚠️ Medium risk: SQL updates require testing and verification
- ⚠️ Need database backup before system-wide repair
- ✅ Can be rolled back if issues occur

---

## Success Criteria

1. ✅ Diagnostic queries run successfully and identify root cause
2. ✅ Code fix applied and tested with existing data
3. ✅ Rice Tay item shows correct commitments (or empty if truly none exist)
4. ✅ All items pass integration test (committedQuantity matches commitments.sum)
5. ✅ System-wide audit shows no remaining discrepancies
6. ✅ ADR document created with findings and solution
7. ✅ Monitoring in place to detect future occurrences

---

## Related Documentation

- **Root Cause Analysis**: `c:\Work\LankaConnect\docs\architecture\ROOT_CAUSE_ANALYSIS_COMMITMENTS_MISSING.md`
- **Diagnostic SQL**: `c:\Work\LankaConnect\scripts\diagnose-commitments.sql`
- **Domain Entity**: `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`
- **EF Configuration**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`
- **Query Handler**: `src/LankaConnect.Application/Events/Queries/GetEventSignUpLists/GetEventSignUpListsQueryHandler.cs`
- **Similar Pattern**: Phase 6A.28 ADR-008 (private backing field issues)

---

## Conclusion

**Most Likely Root Cause**: Missing EF Core backing field configuration preventing commitment collection from loading.

**Immediate Action Required**: Run diagnostic SQL queries to confirm diagnosis.

**Fix Complexity**: Low - single line of configuration code + optional data repair.

**Confidence Level**: High - identical pattern exists in `SignUpListConfiguration` which works correctly.

**User Impact After Fix**: Organizers will see who committed to bring items, restoring full functionality.

---

**Next Step**: Run diagnostic queries from `c:\Work\LankaConnect\scripts\diagnose-commitments.sql` against staging database.
