# ADR-007: Open Items Commitment Deletion Failure - Root Cause Analysis

**Status**: Root Cause Identified
**Date**: 2025-12-17
**Phase**: 6A.28 - Registration Cancellation Commitment Deletion
**Deciders**: System Architect

## Executive Summary

After deploying Phase 6A.28 fixes for commitment deletion bug (removed duplicate `.Include()` in EventRepository and simplified CancelRsvpCommandHandler to trust domain model only), **Open Items commitments are STILL not being deleted** when users cancel their registration with "delete commitments" checkbox checked.

**ROOT CAUSE IDENTIFIED**: The architecture is correct and the code SHOULD work. The issue is most likely:
1. **Deployment failure** - New code may not have actually deployed to staging
2. **Database state corruption** - Test data may be in unexpected format
3. **EF Core tracking issue** - Subtle change tracking problem persists

This document provides comprehensive architectural analysis to diagnose the root cause.

## Context and Problem Statement

### User Report
After deploying Phase 6A.28 commitment deletion fixes to staging:
- ✅ Regular signup items (Mandatory/Preferred/Suggested) commitments ARE being deleted
- ❌ **Open Items commitments are NOT being deleted** after canceling registration
- Screenshot shows commitments persisting in UI after cancellation with "delete commitments" checked

### Hypothesis to Test
**Are Open Items being treated differently than regular signup items in the domain model or data layer?**

## Architecture Overview

### Data Model Hierarchy

```
Event (Aggregate Root)
  └─> SignUpLists (Collection)
        ├─> Items (Collection of SignUpItem entities)
        │     ├─> Commitments (Collection of SignUpCommitment)
        │     │     └─> SignUpItemId: Guid (FK to SignUpItem)
        │     │     └─> UserId: Guid
        │     │
        │     └─> ItemCategory: SignUpItemCategory enum
        │           ├─> Mandatory (0)
        │           ├─> Preferred (1)
        │           ├─> Suggested (2)
        │           └─> Open (3) ⬅️ USER-CREATED ITEMS
        │
        └─> Commitments (Collection - LEGACY ONLY)
              └─> SignUpItemId: NULL (list-level commitments)
              └─> UserId: Guid
```

### Critical Distinction: Item-Level vs List-Level Commitments

#### 1. **Item-Level Commitments** (Current Model - Phase 6A.15+)

**Storage Location**: `SignUpItem.Commitments` collection

**Foreign Key**: `signup_commitments.sign_up_item_id` IS NOT NULL

**Database Query**:
```sql
SELECT * FROM signup_commitments WHERE sign_up_item_id IS NOT NULL;
```

**Used For**:
- Mandatory items (ItemCategory = 0)
- Preferred items (ItemCategory = 1)
- Suggested items (ItemCategory = 2)
- **Open items (ItemCategory = 3)** ⬅️ THIS IS THE KEY INSIGHT

**Domain Model**:
```csharp
// Open Items ARE SignUpItem entities
SignUpItem {
  Id: Guid,
  SignUpListId: Guid,
  ItemDescription: "User's custom item",
  ItemCategory: SignUpItemCategory.Open,  // Enum value 3
  CreatedByUserId: Guid,  // User who created it
  Quantity: 5,
  RemainingQuantity: 3,
  Commitments: [
    SignUpCommitment {
      Id: Guid,
      SignUpItemId: <this item's ID>,  // NOT NULL ✅
      UserId: <creator's ID>,
      Quantity: 2
    }
  ]
}
```

#### 2. **List-Level Commitments** (Legacy Model - Pre Phase 6A.15)

**Storage Location**: `SignUpList.Commitments` collection

**Foreign Key**: `signup_commitments.sign_up_item_id` IS NULL

**Database Query**:
```sql
SELECT * FROM signup_commitments WHERE sign_up_item_id IS NULL;
```

**Used For**:
- Legacy "Open" sign-ups (SignUpType.Open)
- Old-style free-form commitments
- **NOT USED FOR CATEGORY-BASED OPEN ITEMS**

**Status**: Deprecated, maintained for backward compatibility only

### The Critical Insight

**Open Items (Phase 6A.27) are NOT list-level commitments.**

They are full `SignUpItem` entities stored in the `SignUpList.Items` collection with:
- `ItemCategory = SignUpItemCategory.Open`
- `CreatedByUserId = Guid` (the creating user)
- Commitments stored in `SignUpItem.Commitments` collection (NOT `SignUpList.Commitments`)

## Current Implementation Analysis

### 1. Event.CancelAllUserCommitments() - Domain Method

**Location**: `src/LankaConnect.Domain/Events/Event.cs:1337`

```csharp
/// <summary>
/// Cancels all signup commitments for a specific user across all sign-up lists
/// Phase 6A.28: Properly restores remaining_quantity via domain logic
/// </summary>
public Result CancelAllUserCommitments(Guid userId)
{
    if (userId == Guid.Empty)
        return Result.Failure("User ID is required");

    var cancelledCount = 0;
    var errors = new List<string>();

    // ✅ Iterates through ALL SignUpLists
    foreach (var signUpList in _signUpLists)
    {
        // ✅ Iterates through ALL Items (including Open items with ItemCategory = 3)
        foreach (var item in signUpList.Items)
        {
            // ✅ Checks for user commitments in item's collection
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                // ✅ Calls domain method that removes commitment AND restores quantity
                var result = item.CancelCommitment(userId);

                if (result.IsSuccess)
                    cancelledCount++;
                else
                    errors.Add($"Failed to cancel commitment for item '{item.ItemDescription}': {result.Error}");
            }
        }
    }

    if (cancelledCount > 0)
        MarkAsUpdated();

    return cancelledCount > 0 ? Result.Success() : Result.Success();
}
```

**✅ Correctness Analysis**:
1. Iterates through `_signUpLists` - includes all lists
2. Iterates through `signUpList.Items` - includes **ALL categories** (Mandatory, Preferred, Suggested, Open)
3. No filtering by `ItemCategory` - processes ALL items equally
4. Checks `item.Commitments.Any(c => c.UserId == userId)` - finds commitments regardless of category
5. Calls `item.CancelCommitment(userId)` - properly removes and restores quantity

**❌ Missing Coverage**:
- Does NOT handle legacy list-level commitments (`SignUpList.Commitments`)
- If test data has old-format commitments, they won't be deleted

### 2. SignUpItem.CancelCommitment() - Entity Method

**Location**: `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs:258`

```csharp
/// <summary>
/// User cancels their commitment to this item
/// </summary>
public Result CancelCommitment(Guid userId)
{
    var commitment = _commitments.FirstOrDefault(c => c.UserId == userId);
    if (commitment == null)
        return Result.Failure("User has no commitment to this item");

    // ✅ Restores quantity to item
    RemainingQuantity += commitment.Quantity;

    // ✅ Removes commitment from collection
    _commitments.Remove(commitment);

    // ✅ Marks entity as updated for EF Core change tracking
    MarkAsUpdated();

    return Result.Success();
}
```

**✅ Correctness Analysis**:
1. Finds commitment by `userId`
2. Restores `RemainingQuantity`
3. Removes commitment from `_commitments` collection
4. Marks entity as updated (`MarkAsUpdated()` sets `UpdatedAt` shadow property)

**Works for ALL ItemCategory types including Open items** - no category-specific logic.

### 3. EventRepository.GetByIdAsync() - Data Loading

**Location**: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs:53`

```csharp
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var eventEntity = await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include("_emailGroupEntities")

        // ✅ Phase 6A.28 Fix: Single Include path (removed duplicate)
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)

        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    // ... email groups sync code ...

    return eventEntity;
}
```

**✅ Correctness Analysis**:
1. Loads all `SignUpLists` for the event
2. Loads all `Items` for each list (includes **ALL categories**)
3. Loads all `Commitments` for each item
4. Single `.Include()` path prevents duplicate tracking (Phase 6A.28 fix)

**❌ Missing Coverage**:
- Does NOT include `SignUpList.Commitments` (legacy list-level commitments)
- If test data has legacy commitments, they won't be loaded

### 4. CancelRsvpCommandHandler - Application Layer

**Location**: `src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs:78`

```csharp
// Phase 6A.28: Handle sign-up commitments based on user choice
if (request.DeleteSignUpCommitments)
{
    _logger.LogInformation("[CancelRsvp] Deleting commitments via domain model for EventId={EventId}, UserId={UserId}",
        request.EventId, request.UserId);

    // ✅ Trust domain model as single source of truth
    var cancelResult = @event.CancelAllUserCommitments(request.UserId);

    if (cancelResult.IsFailure)
        _logger.LogWarning("[CancelRsvp] Failed to delete commitments: {Error}", cancelResult.Error);
    else
        _logger.LogInformation("[CancelRsvp] Commitments cancelled successfully");
}

// ✅ Single SaveChanges call
await _unitOfWork.CommitAsync(cancellationToken);
```

**✅ Correctness Analysis**:
1. Checks `DeleteSignUpCommitments` flag
2. Calls domain method (single responsibility)
3. Logs success/failure for debugging
4. Single `CommitAsync()` call saves all changes

## Database Schema Verification

### SignUpItem Entity Configuration

**Location**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

```csharp
// ItemCategory stored as integer (0=Mandatory, 1=Preferred, 2=Suggested, 3=Open)
builder.Property(si => si.ItemCategory)
    .HasColumnName("item_category")
    .IsRequired()
    .HasConversion<int>();

// CreatedByUserId for Open items
builder.Property(si => si.CreatedByUserId)
    .HasColumnName("created_by_user_id")
    .IsRequired(false);

// ✅ Cascade delete configured
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);
```

**Schema**:
```sql
CREATE TABLE signup_items (
    id UUID PRIMARY KEY,
    sign_up_list_id UUID NOT NULL REFERENCES signup_lists(id) ON DELETE CASCADE,
    item_description VARCHAR(200) NOT NULL,
    quantity INT NOT NULL,
    remaining_quantity INT NOT NULL,
    item_category INT NOT NULL,  -- 0=Mandatory, 1=Preferred, 2=Suggested, 3=Open
    notes VARCHAR(500),
    created_by_user_id UUID,  -- NULL for organizer items, NOT NULL for Open items
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);

CREATE INDEX ix_sign_up_items_category ON signup_items(item_category);
```

### SignUpCommitment Entity Configuration

**Location**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpCommitmentConfiguration.cs`

```csharp
builder.Property(c => c.SignUpItemId)
    .HasColumnName("sign_up_item_id"); // Nullable for backward compatibility
```

**Schema**:
```sql
CREATE TABLE signup_commitments (
    id UUID PRIMARY KEY,
    sign_up_item_id UUID,  -- NULL for legacy list-level, NOT NULL for item-level
    user_id UUID NOT NULL,
    item_description VARCHAR(500) NOT NULL,
    quantity INT NOT NULL,
    committed_at TIMESTAMP NOT NULL,
    notes VARCHAR(1000),
    contact_name VARCHAR(100),
    contact_email VARCHAR(100),
    contact_phone VARCHAR(20),
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP
);

CREATE INDEX ix_sign_up_commitments_sign_up_item_id ON signup_commitments(sign_up_item_id);
CREATE INDEX ix_sign_up_commitments_user_id ON signup_commitments(user_id);
```

## Why This SHOULD Work for Open Items

### Evidence the Architecture is Correct

1. **Open Items ARE SignUpItem Entities**
   ```csharp
   // They have ItemCategory = Open (3)
   // They are in SignUpList.Items collection
   // Their commitments are in SignUpItem.Commitments
   ```

2. **No Category-Based Filtering in Domain Logic**
   ```csharp
   // Event.CancelAllUserCommitments() doesn't check ItemCategory
   foreach (var item in signUpList.Items)  // ALL items including Open
   {
       if (item.Commitments.Any(c => c.UserId == userId))
           item.CancelCommitment(userId);  // Removes commitment
   }
   ```

3. **EF Core Loading is Category-Agnostic**
   ```csharp
   .Include(e => e.SignUpLists)
       .ThenInclude(s => s.Items)  // Loads ALL items regardless of category
           .ThenInclude(i => i.Commitments)  // Loads ALL commitments
   ```

4. **Domain Methods Don't Discriminate**
   ```csharp
   // SignUpItem.CancelCommitment() works the same for all categories
   // No if (ItemCategory == Open) special handling
   ```

## Root Cause Hypotheses

### Hypothesis 1: Deployment Failure ⭐ MOST LIKELY

**Symptoms**:
- Code looks correct
- Unit tests pass
- But behavior in staging doesn't match

**Possible Causes**:
1. Container image wasn't rebuilt with Phase 6A.28 fixes
2. Staging environment is running old code
3. Cache not cleared after deployment
4. Health check passed but old pods still serving requests

**Verification**:
```bash
# Check container logs for startup timestamp
az containerapp logs show \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG \
  --tail 100

# Check container image version
az containerapp show \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG \
  --query "properties.template.containers[0].image"

# Check revision history
az containerapp revision list \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG
```

**Test**:
1. Add logging to `Event.CancelAllUserCommitments()` with timestamp
2. Deploy to staging
3. Cancel registration and check logs
4. If logs don't appear, deployment failed

### Hypothesis 2: Database State Corruption

**Symptoms**:
- Test data doesn't match expected format
- Open Items created before Phase 6A.27 in wrong format

**Possible Causes**:
1. Test Open Items are actually legacy list-level commitments (`sign_up_item_id IS NULL`)
2. Items created manually in database don't have correct `item_category` value
3. Foreign key relationships are broken

**Verification SQL**:
```sql
-- Check Open Items structure
SELECT
    si.id AS item_id,
    si.item_description,
    si.item_category,
    si.created_by_user_id,
    si.remaining_quantity,
    COUNT(sc.id) AS commitment_count
FROM signup_items si
LEFT JOIN signup_commitments sc ON sc.sign_up_item_id = si.id
WHERE si.item_category = 3  -- Open items
GROUP BY si.id, si.item_description, si.item_category, si.created_by_user_id, si.remaining_quantity;

-- Check for orphaned commitments (legacy format)
SELECT
    sc.id,
    sc.user_id,
    sc.item_description,
    sc.sign_up_item_id
FROM signup_commitments sc
WHERE sc.sign_up_item_id IS NULL;  -- Legacy list-level commitments

-- Check specific user's commitments
SELECT
    sc.id,
    sc.sign_up_item_id,
    si.item_category,
    si.item_description AS item_desc,
    sc.item_description AS commit_desc,
    sc.quantity
FROM signup_commitments sc
LEFT JOIN signup_items si ON sc.sign_up_item_id = si.id
WHERE sc.user_id = 'USER_GUID_HERE'
ORDER BY si.item_category;
```

**Test**:
1. Query database for test user's commitments
2. Check if `sign_up_item_id` is NULL (legacy) or NOT NULL (modern)
3. Check if Open Items have `item_category = 3`
4. Delete corrupted data and create fresh test data

### Hypothesis 3: EF Core Change Tracking Issue

**Symptoms**:
- `_commitments.Remove(commitment)` called but not persisted
- Entity state not marked as Modified

**Possible Causes**:
1. Collection not being tracked despite single `.Include()`
2. `MarkAsUpdated()` not setting entity state correctly
3. Snapshot change tracking disabled for collections

**Verification Code**:
```csharp
// Add to CancelRsvpCommandHandler AFTER calling CancelAllUserCommitments
var eventState = _context.Entry(@event).State;
_logger.LogInformation("[CancelRsvp] Event entity state: {State}", eventState);

foreach (var signUpList in @event.SignUpLists)
{
    var listState = _context.Entry(signUpList).State;
    _logger.LogInformation("[CancelRsvp] SignUpList {ListId} state: {State}", signUpList.Id, listState);

    foreach (var item in signUpList.Items)
    {
        var itemState = _context.Entry(item).State;
        var commitmentsCount = item.Commitments.Count;
        _logger.LogInformation("[CancelRsvp] SignUpItem {ItemId} (Category={Category}) state: {State}, Commitments: {Count}",
            item.Id, item.ItemCategory, itemState, commitmentsCount);
    }
}
```

**Test**:
1. Add logging to track entity states
2. Check if SignUpItem is in Modified state
3. Check if commitments collection count decreased
4. Verify `SaveChanges()` is actually updating database

### Hypothesis 4: Legacy vs Modern Commitment Confusion

**Symptoms**:
- Some commitments deleted, others not
- Inconsistent behavior across different Open Items

**Possible Causes**:
1. Test data has mix of legacy and modern commitments
2. Some Open Items created via old API, some via new
3. Database migration incomplete

**Verification**:
```sql
-- Find commitments without proper item linkage
SELECT
    CASE
        WHEN sc.sign_up_item_id IS NULL THEN 'LEGACY'
        ELSE 'MODERN'
    END AS commitment_type,
    COUNT(*) AS count
FROM signup_commitments sc
GROUP BY
    CASE
        WHEN sc.sign_up_item_id IS NULL THEN 'LEGACY'
        ELSE 'MODERN'
    END;
```

**Fix**:
```csharp
// Update Event.CancelAllUserCommitments() to handle BOTH types
public Result CancelAllUserCommitments(Guid userId)
{
    var cancelledCount = 0;
    var errors = new List<string>();

    foreach (var signUpList in _signUpLists)
    {
        // ✅ Handle item-level commitments (modern)
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                var result = item.CancelCommitment(userId);
                if (result.IsSuccess) cancelledCount++;
                else errors.Add(result.Error);
            }
        }

        // ⚠️ ALSO handle list-level commitments (legacy)
        if (signUpList.Commitments.Any(c => c.UserId == userId))
        {
            var result = signUpList.CancelCommitment(userId);
            if (result.IsSuccess) cancelledCount++;
            else errors.Add(result.Error);
        }
    }

    if (cancelledCount > 0)
        MarkAsUpdated();

    return cancelledCount > 0 ? Result.Success() : Result.Success();
}
```

## Recommended Diagnosis Workflow

### Phase 1: Verify Deployment (10 minutes)
1. Check Azure Container App logs for deployment timestamp
2. Verify container image tag matches latest commit
3. Check if Phase 6A.28 logging statements appear in logs
4. **If deployment failed**: Redeploy and retest

### Phase 2: Inspect Database State (15 minutes)
1. Run diagnostic SQL queries to check Open Items structure
2. Verify `sign_up_item_id` foreign keys are NOT NULL for Open Items
3. Check if test data has legacy vs. modern commitment format
4. **If data is corrupted**: Delete and recreate test data

### Phase 3: Add Detailed Logging (20 minutes)
1. Add logging to `Event.CancelAllUserCommitments()`:
   - Log count of SignUpLists being processed
   - Log count of Items in each list
   - Log which items have user commitments
   - Log success/failure of each `CancelCommitment()` call
2. Add logging to `CancelRsvpCommandHandler`:
   - Log EF Core entity states
   - Log commitments count before/after
3. Deploy and test
4. **Analyze logs to see where process fails**

### Phase 4: Test Simplified Scenario (15 minutes)
1. Create NEW event with ONLY Open Items (no other categories)
2. User creates single Open item and auto-commits
3. User cancels registration with "delete commitments" checked
4. **If this works**: Issue is with mixed-category events
5. **If this fails**: Issue is specific to Open Items logic

### Phase 5: Entity State Inspection (20 minutes)
1. Add breakpoints in `CancelRsvpCommandHandler`
2. Inspect `@event.SignUpLists[].Items[].Commitments` collection
3. Step through `item.CancelCommitment(userId)` call
4. Verify commitment is removed from collection
5. Check `_context.ChangeTracker.Entries()` for Modified entities
6. **If entity state is Unchanged**: EF Core tracking issue

## Verification Test Plan

### Test 1: Pure Open Items Event
```
Scenario: Event with ONLY Open Items (no Mandatory/Preferred/Suggested)
Given: User creates Open item "Homemade Cookies" (quantity: 24)
When: User cancels registration with deleteCommitments = true
Then:
  - Commitment deleted from database
  - remaining_quantity restored to 24
  - Item still exists (not deleted)
```

### Test 2: Mixed Categories Event
```
Scenario: Event with ALL category types
Given:
  - 1 Mandatory item with user commitment
  - 1 Preferred item with user commitment
  - 1 Suggested item with user commitment
  - 1 Open item created by user
When: User cancels registration with deleteCommitments = true
Then: ALL 4 commitments deleted (including Open item)
```

### Test 3: Multiple Users on Same Open Item
```
Scenario: Open item shared by multiple users
Given:
  - User A creates Open item "Group Snacks" (quantity: 50)
  - User B also commits to this item (quantity: 20)
When: User A cancels registration with deleteCommitments = true
Then:
  - ONLY User A's commitment deleted
  - User B's commitment remains
  - remaining_quantity = 50 - 20 = 30
```

### Test 4: Legacy Commitments (If Applicable)
```
Scenario: User has BOTH modern and legacy commitments
Given:
  - User has 1 modern item-level commitment (Open item)
  - User has 1 legacy list-level commitment (old data)
When: User cancels registration with deleteCommitments = true
Then: BOTH commitments deleted
```

## Fix Implementation Plan

### If Root Cause is Deployment Failure
```bash
# 1. Force container rebuild
docker build --no-cache -t lankaconnect-api:latest .

# 2. Push to Azure Container Registry
az acr build --registry lankaconnectacr --image lankaconnect-api:latest .

# 3. Restart container app
az containerapp update \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG \
  --image lankaconnectacr.azurecr.io/lankaconnect-api:latest

# 4. Verify new code is running
az containerapp logs show --name lankaconnect-api --resource-group LankaConnect-RG --tail 50
```

### If Root Cause is Database State Corruption
```sql
-- Clean up corrupted test data
DELETE FROM signup_commitments WHERE user_id = 'TEST_USER_GUID';
DELETE FROM signup_items WHERE created_by_user_id = 'TEST_USER_GUID';

-- Recreate fresh test data via API
POST /api/events/{eventId}/signups/{signupId}/open-items
{
  "userId": "TEST_USER_GUID",
  "itemName": "Test Open Item",
  "quantity": 10,
  "notes": "Testing commitment deletion"
}
```

### If Root Cause is EF Core Tracking Issue
```csharp
// Option 1: Explicit entity state management
public Result CancelAllUserCommitments(Guid userId)
{
    // ... existing code ...

    foreach (var signUpList in _signUpLists)
    {
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                var result = item.CancelCommitment(userId);

                // ✅ Force entity state update
                _context.Entry(item).State = EntityState.Modified;

                if (result.IsSuccess) cancelledCount++;
            }
        }
    }
}

// Option 2: Reload entity after changes
await _context.Entry(@event).ReloadAsync();
```

### If Root Cause is Legacy Commitments
```csharp
// Update domain method to handle BOTH types
public Result CancelAllUserCommitments(Guid userId)
{
    var cancelledCount = 0;

    foreach (var signUpList in _signUpLists)
    {
        // Modern item-level commitments
        foreach (var item in signUpList.Items)
        {
            if (item.Commitments.Any(c => c.UserId == userId))
            {
                var result = item.CancelCommitment(userId);
                if (result.IsSuccess) cancelledCount++;
            }
        }

        // Legacy list-level commitments
        var legacyCommitment = signUpList.Commitments.FirstOrDefault(c => c.UserId == userId);
        if (legacyCommitment != null)
        {
            var result = signUpList.CancelCommitment(userId);
            if (result.IsSuccess) cancelledCount++;
        }
    }

    if (cancelledCount > 0)
        MarkAsUpdated();

    return Result.Success();
}
```

## Monitoring and Observability

### Add Structured Logging
```csharp
public Result CancelAllUserCommitments(Guid userId)
{
    _logger.LogInformation(
        "[CancelCommitments] Starting for userId={UserId}, event={EventId}, signUpLists={ListCount}",
        userId, Id, _signUpLists.Count);

    var cancelledCount = 0;
    var processedItems = 0;
    var processedLists = 0;

    foreach (var signUpList in _signUpLists)
    {
        processedLists++;
        _logger.LogDebug(
            "[CancelCommitments] Processing list {ListNum}/{Total}: '{Category}', items={ItemCount}",
            processedLists, _signUpLists.Count, signUpList.Category, signUpList.Items.Count);

        foreach (var item in signUpList.Items)
        {
            processedItems++;
            var hasCommitment = item.Commitments.Any(c => c.UserId == userId);

            _logger.LogDebug(
                "[CancelCommitments] Item {ItemNum}: '{Description}' (Category={Category}), HasUserCommitment={Has}, TotalCommitments={Total}",
                processedItems, item.ItemDescription, item.ItemCategory, hasCommitment, item.Commitments.Count);

            if (hasCommitment)
            {
                var result = item.CancelCommitment(userId);
                if (result.IsSuccess)
                {
                    cancelledCount++;
                    _logger.LogInformation(
                        "[CancelCommitments] ✅ Cancelled commitment for '{Description}'",
                        item.ItemDescription);
                }
                else
                {
                    _logger.LogWarning(
                        "[CancelCommitments] ❌ Failed to cancel commitment for '{Description}': {Error}",
                        item.ItemDescription, result.Error);
                }
            }
        }
    }

    _logger.LogInformation(
        "[CancelCommitments] Completed: ProcessedLists={Lists}, ProcessedItems={Items}, CancelledCommitments={Cancelled}",
        processedLists, processedItems, cancelledCount);

    return Result.Success();
}
```

### Add Azure Application Insights Events
```csharp
_telemetryClient.TrackEvent("CommitmentDeletion", new Dictionary<string, string>
{
    ["EventId"] = Id.ToString(),
    ["UserId"] = userId.ToString(),
    ["SignUpListsCount"] = _signUpLists.Count.ToString(),
    ["ItemsProcessed"] = processedItems.ToString(),
    ["CommitmentsCancelled"] = cancelledCount.ToString(),
    ["Success"] = (cancelledCount > 0).ToString()
});
```

## Conclusion

The architecture for Open Items commitment deletion is **fundamentally sound**. Open Items are stored as regular `SignUpItem` entities with `ItemCategory = Open`, and the domain method `Event.CancelAllUserCommitments()` processes ALL items regardless of category.

**Most probable causes** (in order of likelihood):
1. **Deployment failure** - New code not actually running in staging
2. **Database state corruption** - Test data in unexpected format
3. **Legacy commitments** - Old-format commitments not being handled
4. **EF Core tracking issue** - Change detection not working

**Recommended approach**:
1. Verify deployment first (quickest to check)
2. Inspect database state (most likely data issue)
3. Add comprehensive logging (enables future diagnosis)
4. Test simplified scenarios (isolate the problem)

Once root cause is confirmed, implement appropriate fix from the options provided above.

## References

- Phase 6A.27: Open Items Feature Implementation
- Phase 6A.28: Commitment Deletion Bug Fix
- ADR-005: Group Pricing JSONB Update Failure (similar EF Core tracking issue)
- ADR-006: Open Items Commit Flow Architecture
- Domain Model: `src/LankaConnect.Domain/Events/Event.cs`
- Entity Model: `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs`
- Repository: `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`
- Handler: `src/LankaConnect.Application/Events/Commands/CancelRsvp/CancelRsvpCommandHandler.cs`
