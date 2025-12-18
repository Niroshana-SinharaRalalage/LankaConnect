# Session 34: Open Items Commitment Deletion Diagnosis

**Date**: 2025-12-17
**Status**: Root Cause Analysis Complete - Awaiting Diagnosis
**Phase**: 6A.28 Follow-up
**Related**: Phase 6A.28 Commitment Deletion Bug Fix

## Issue Summary

After deploying Phase 6A.28 fixes to staging (removed duplicate `.Include()` and simplified CancelRsvpCommandHandler):
- ✅ **Regular signup items** (Mandatory/Preferred/Suggested) commitments ARE being deleted correctly
- ❌ **Open Items commitments** are NOT being deleted when user cancels registration with "delete commitments" checkbox checked

## Key Architectural Insights

### 1. Open Items ARE Regular SignUpItem Entities

**Critical Understanding**: Open Items (Phase 6A.27) are NOT a special type. They are full `SignUpItem` entities with:
- `ItemCategory = SignUpItemCategory.Open` (enum value 3)
- `CreatedByUserId = Guid` (the user who created it)
- Commitments stored in `SignUpItem.Commitments` collection (same as Mandatory/Preferred/Suggested)

```csharp
// Open Item structure in database
SignUpItem {
  Id: Guid,
  SignUpListId: Guid,
  ItemDescription: "User's custom item",
  ItemCategory: 3,  // Open
  CreatedByUserId: <user-guid>,
  Commitments: [
    SignUpCommitment {
      SignUpItemId: <this item's ID>,  // NOT NULL
      UserId: <creator-guid>
    }
  ]
}
```

### 2. Two Types of Commitments in Database

#### Modern Item-Level Commitments (Current)
- **Foreign Key**: `signup_commitments.sign_up_item_id` IS NOT NULL
- **Used for**: Mandatory, Preferred, Suggested, **Open** items
- **Query**: `SELECT * FROM signup_commitments WHERE sign_up_item_id IS NOT NULL;`

#### Legacy List-Level Commitments (Deprecated)
- **Foreign Key**: `signup_commitments.sign_up_item_id` IS NULL
- **Used for**: Old-style "Open sign-ups" before Phase 6A.15
- **Query**: `SELECT * FROM signup_commitments WHERE sign_up_item_id IS NULL;`

### 3. Current Code SHOULD Work

The domain method `Event.CancelAllUserCommitments()` correctly:
1. Iterates through ALL SignUpLists
2. Iterates through ALL Items (including Open items with `ItemCategory = 3`)
3. Removes commitments for the user
4. Restores `RemainingQuantity`

**No category-specific filtering** - processes ALL items equally.

## Root Cause Hypotheses (Priority Order)

### 1. Deployment Failure ⭐ MOST LIKELY

**Evidence**:
- Code logic is correct
- Unit tests pass
- But staging behavior doesn't match

**Possible Causes**:
- Container image not rebuilt with Phase 6A.28 fixes
- Azure Container App running old code version
- Cache not cleared after deployment

**How to Verify**:
```bash
# Check container logs for deployment timestamp
az containerapp logs show --name lankaconnect-api --resource-group LankaConnect-RG --tail 100

# Check container image version
az containerapp show --name lankaconnect-api --resource-group LankaConnect-RG \
  --query "properties.template.containers[0].image"
```

**Fix**: Redeploy with forced container rebuild

### 2. Database State Corruption

**Evidence**:
- Test Open Items may be in legacy format
- Foreign key relationships incorrect

**Possible Causes**:
- Test Open Items created before Phase 6A.27
- Manual database manipulation
- Migration incomplete

**How to Verify**:
```sql
-- Check if test Open Items have correct structure
SELECT
    si.id,
    si.item_description,
    si.item_category,
    si.created_by_user_id,
    COUNT(sc.id) AS commitment_count
FROM signup_items si
LEFT JOIN signup_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.item_category = 3  -- Open items
GROUP BY si.id, si.item_description, si.item_category, si.created_by_user_id;

-- Check for legacy commitments (sign_up_item_id IS NULL)
SELECT COUNT(*) FROM signup_commitments WHERE sign_up_item_id IS NULL;
```

**Fix**: Delete corrupted test data and recreate via API

### 3. Legacy Commitments Not Handled

**Evidence**:
- `Event.CancelAllUserCommitments()` only processes `SignUpList.Items` collection
- Does NOT process `SignUpList.Commitments` (legacy list-level)

**Current Code**:
```csharp
foreach (var signUpList in _signUpLists)
{
    foreach (var item in signUpList.Items)  // ✅ Handles modern commitments
    {
        if (item.Commitments.Any(c => c.UserId == userId))
            item.CancelCommitment(userId);
    }

    // ❌ MISSING: signUpList.Commitments (legacy list-level)
}
```

**Fix Required**:
```csharp
foreach (var signUpList in _signUpLists)
{
    // Handle item-level commitments (modern)
    foreach (var item in signUpList.Items)
    {
        if (item.Commitments.Any(c => c.UserId == userId))
            item.CancelCommitment(userId);
    }

    // ⚠️ ALSO handle list-level commitments (legacy)
    if (signUpList.Commitments.Any(c => c.UserId == userId))
    {
        signUpList.CancelCommitment(userId);
    }
}
```

### 4. EF Core Change Tracking Issue

**Evidence**:
- Despite Phase 6A.28 fix, subtle tracking issue persists
- Commitments removed from collection but not persisted

**Possible Causes**:
- Collection not being tracked
- `MarkAsUpdated()` not setting entity state correctly

**How to Verify**: Add logging to track entity states

## Recommended Diagnosis Workflow

### Step 1: Verify Deployment (10 min) ⭐ DO THIS FIRST
```bash
# Check Azure logs
az containerapp logs show --name lankaconnect-api --resource-group LankaConnect-RG --tail 100

# Look for Phase 6A.28 logging statements
# If not found = deployment failed
```

### Step 2: Inspect Database (15 min)
```sql
-- Check Open Items structure
SELECT
    si.id,
    si.item_category,
    si.created_by_user_id,
    sc.sign_up_item_id,
    sc.user_id
FROM signup_items si
INNER JOIN signup_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.item_category = 3;

-- Check for legacy commitments
SELECT COUNT(*) FROM signup_commitments WHERE sign_up_item_id IS NULL;
```

### Step 3: Add Detailed Logging (20 min)
Update `Event.CancelAllUserCommitments()`:
```csharp
_logger.LogInformation("[CancelCommitments] Starting: SignUpLists={Count}", _signUpLists.Count);

foreach (var signUpList in _signUpLists)
{
    _logger.LogInformation("[CancelCommitments] List '{Category}': Items={Count}",
        signUpList.Category, signUpList.Items.Count);

    foreach (var item in signUpList.Items)
    {
        var hasCommit = item.Commitments.Any(c => c.UserId == userId);
        _logger.LogInformation("[CancelCommitments] Item '{Desc}' (Cat={Cat}): HasCommit={Has}",
            item.ItemDescription, item.ItemCategory, hasCommit);

        if (hasCommit)
        {
            var result = item.CancelCommitment(userId);
            _logger.LogInformation("[CancelCommitments] Cancel result: {Success}", result.IsSuccess);
        }
    }
}
```

### Step 4: Test Simplified Scenario (15 min)
1. Create NEW event with ONLY Open Items enabled
2. User creates single Open item
3. User cancels registration with "delete commitments" checked
4. If this works → Issue is with mixed categories
5. If this fails → Issue is specific to Open Items

## Quick Diagnostic Queries

### Check Test User's Commitments
```sql
-- Replace USER_GUID with actual test user ID
SELECT
    sc.id,
    sc.sign_up_item_id,
    si.item_category,
    si.item_description,
    sc.quantity,
    CASE
        WHEN sc.sign_up_item_id IS NULL THEN 'LEGACY'
        ELSE 'MODERN'
    END AS commitment_type
FROM signup_commitments sc
LEFT JOIN signup_items si ON sc.sign_up_item_id = si.id
WHERE sc.user_id = 'USER_GUID_HERE'
ORDER BY commitment_type, si.item_category;
```

### Check Event's Open Items
```sql
-- Replace EVENT_GUID with actual event ID
SELECT
    sl.id AS signup_list_id,
    sl.category,
    sl.has_open_items,
    si.id AS item_id,
    si.item_description,
    si.item_category,
    si.created_by_user_id,
    COUNT(sc.id) AS commitment_count
FROM signup_lists sl
LEFT JOIN signup_items si ON si.sign_up_list_id = sl.id
LEFT JOIN signup_commitments sc ON sc.sign_up_item_id = si.id
WHERE sl.event_id = 'EVENT_GUID_HERE'
  AND si.item_category = 3
GROUP BY sl.id, sl.category, sl.has_open_items, si.id, si.item_description, si.item_category, si.created_by_user_id;
```

## Implementation Files

### Domain Layer
- `src/LankaConnect.Domain/Events/Event.cs:1337` - CancelAllUserCommitments()
- `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs:258` - CancelCommitment()
- `src/LankaConnect.Domain/Events/Entities/SignUpList.cs:447` - AddOpenItem()

### Application Layer
- `src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs:78` - Cancellation logic

### Infrastructure Layer
- `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs:53` - GetByIdAsync() with .Include()
- `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs` - EF Core mapping

## Test Plan

### Test Case 1: Pure Open Items
```
Given: Event with ONLY Open Items (no Mandatory/Preferred/Suggested)
When: User creates Open item and cancels registration with deleteCommitments=true
Then: Commitment deleted, remaining_quantity restored
```

### Test Case 2: Mixed Categories
```
Given: Event with Mandatory, Preferred, Suggested, AND Open items
When: User commits to all types and cancels with deleteCommitments=true
Then: ALL commitments deleted (including Open)
```

### Test Case 3: Multiple Users
```
Given: User A creates Open item, User B also commits to it
When: User A cancels with deleteCommitments=true
Then: Only User A's commitment deleted, User B's remains
```

## Next Steps

1. **User verifies deployment status** in Azure Portal
2. **User runs diagnostic SQL queries** to check data structure
3. **User adds logging** to CancelAllUserCommitments() method
4. **User deploys with logging** and tests again
5. **User shares logs** for further analysis

If deployment is confirmed good and data structure is correct, then we need to investigate:
- EF Core tracking state
- Collection change detection
- Possible need to handle legacy commitments

## Reference Documents

- **Full Analysis**: `docs/architecture/ADR-007-Open-Items-Deletion-Failure-Analysis.md`
- **Original Issue**: `docs/SESSION_33_GROUP_PRICING_UPDATE_BUG_FIX.md`
- **Phase Summary**: `docs/PHASE_6A_28_OPEN_SIGNUP_ITEMS_SUMMARY.md`
- **Related ADR**: `docs/architecture/ADR-006-Open-Items-Commit-Flow-Architecture.md`

## Key Takeaways

1. **Open Items are NOT special** - They're regular SignUpItem entities with ItemCategory=3
2. **The code logic is correct** - No category-based filtering, processes ALL items
3. **Most likely deployment issue** - Check if new code is actually running
4. **Legacy commitments gap** - Current code doesn't handle old-format commitments
5. **Need better observability** - Add comprehensive logging for diagnosis

---

**Status**: Waiting for user to verify deployment and run diagnostic queries before proceeding with code changes.
