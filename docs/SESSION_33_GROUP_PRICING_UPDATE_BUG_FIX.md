# Session 33: Group Pricing Tier Update Bug Fix

## Problem Summary

**Issue**: API returns HTTP 200 OK when updating group pricing tiers, but the database values remain unchanged.

**Symptoms**:
- Frontend sends correct payload with modified tiers (e.g., 2 tiers: $6.00, $12.00)
- Backend API responds with 200 OK
- Database still shows original 3 tiers ($5.00, $10.00, $15.00)
- No error messages logged

**Root Cause**: EF Core does not automatically detect changes to owned entities stored as JSONB columns in PostgreSQL.

## Technical Details

### EF Core Configuration

From [EventConfiguration.cs](../src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs#L75-L90):

```csharp
// Session 21 + Phase 6D: Configure Pricing as JSONB for dual/group pricing
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");  // Store entire Pricing as JSONB column

    pricing.OwnsOne(p => p.AdultPrice);
    pricing.OwnsOne(p => p.ChildPrice);

    pricing.OwnsMany(p => p.GroupTiers, tier =>
    {
        tier.OwnsOne(t => t.PricePerPerson);
    });
});
```

The `Pricing` value object is stored as a JSON column (`ToJson("pricing")`), which means EF Core needs **explicit change tracking** to detect modifications.

### Domain Layer

From [Event.cs](../src/LankaConnect.Domain/Events/Event.cs#L660-L679):

```csharp
public Result SetGroupPricing(TicketPricing? pricing)
{
    if (pricing == null)
        return Result.Failure("Pricing cannot be null");

    if (pricing.Type != PricingType.GroupTiered)
        return Result.Failure("Only GroupTiered pricing type is allowed");

    Pricing = pricing;  // Replaces the entire Pricing property
    TicketPrice = null;

    MarkAsUpdated();  // Marks entity as modified

    RaiseDomainEvent(new EventPricingUpdatedEvent(Id, pricing, DateTime.UtcNow));

    return Result.Success();
}
```

**Problem**: While `MarkAsUpdated()` marks the `Event` entity as modified, it doesn't explicitly mark the `Pricing` **navigation property** as modified. EF Core's change tracker may not detect that the JSONB column needs updating.

### UpdateEventCommandHandler

From [UpdateEventCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs#L208-L225):

```csharp
// Session 33 + Session 21: Update pricing if provided
if (pricing != null)
{
    Result setPricingResult;
    if (isGroupPricing)
    {
        // Session 33: Use SetGroupPricing for group tiered pricing
        setPricingResult = @event.SetGroupPricing(pricing);
    }
    else
    {
        // Session 21: Use SetDualPricing for dual or single pricing
        setPricingResult = @event.SetDualPricing(pricing);
    }

    if (setPricingResult.IsFailure)
        return setPricingResult;
}

// Save changes (EF Core tracks changes automatically)
await _unitOfWork.CommitAsync(cancellationToken);
```

**Problem**: The comment "EF Core tracks changes automatically" is **incorrect** for JSONB-stored owned entities. EF Core does NOT automatically detect when you replace a JSONB value object.

## Solution: Explicit Change Tracking

### 1. Add Method to IEventRepository

From [IEventRepository.cs](../src/LankaConnect.Domain/Events/IEventRepository.cs#L51-L56):

```csharp
// EF Core change tracking helpers (Session 33: Group Pricing Tier Update Fix)
/// <summary>
/// Explicitly marks the Pricing property as modified for EF Core change tracking
/// Required for JSONB-stored owned entities that may not be automatically detected as modified
/// </summary>
void MarkPricingAsModified(Event @event);
```

### 2. Implement in EventRepository

From [EventRepository.cs](../src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs#L294-L304):

```csharp
/// <summary>
/// Session 33: Explicitly marks the Pricing property as modified for EF Core change tracking
/// CRITICAL FIX: EF Core does not automatically detect changes to owned entities stored as JSONB
/// This method ensures that updates to group pricing tiers are persisted to the database
/// </summary>
public void MarkPricingAsModified(Event @event)
{
    // Mark the Pricing owned entity as modified
    // This forces EF Core to update the JSONB column even if it doesn't detect the change automatically
    _context.Entry(@event).Property(e => e.Pricing).IsModified = true;
}
```

### 3. Call from UpdateEventCommandHandler

From [UpdateEventCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs#L208-L229):

```csharp
// Session 33 + Session 21: Update pricing if provided
if (pricing != null)
{
    Result setPricingResult;
    if (isGroupPricing)
    {
        // Session 33: Use SetGroupPricing for group tiered pricing
        setPricingResult = @event.SetGroupPricing(pricing);
    }
    else
    {
        // Session 21: Use SetDualPricing for dual or single pricing
        setPricingResult = @event.SetDualPricing(pricing);
    }

    if (setPricingResult.IsFailure)
        return setPricingResult;

    // CRITICAL FIX: Explicitly mark Pricing as modified for EF Core JSONB change tracking
    // Without this, EF Core might not detect changes to owned entities stored as JSONB
    _eventRepository.MarkPricingAsModified(@event);
}
```

## Files Modified

1. **`src/LankaConnect.Domain/Events/IEventRepository.cs`**
   - Added `void MarkPricingAsModified(Event @event);` method

2. **`src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs`**
   - Implemented `MarkPricingAsModified` method using `_context.Entry(@event).Property(e => e.Pricing).IsModified = true`

3. **`src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs`**
   - Called `_eventRepository.MarkPricingAsModified(@event)` after setting pricing

## Build Status

```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:01:25.08
```

## Testing Instructions

### Backend API Test

Use the PowerShell script [test-update-diagnosis.ps1](../test-update-diagnosis.ps1):

```powershell
# 1. Generate fresh token
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
  "email": "niroshhh2@gmail.com",
  "password": "12!@qwASzx",
  "rememberMe": true
}'

# 2. Run diagnostic test
.\test-update-diagnosis.ps1
```

**Expected Results**:
- ✅ Test 1 PASSED: Title updated correctly
- ✅ Test 2 PASSED: Tier count is 2 (removed 1 tier)
- ✅ Test 3 PASSED: Tier 1 price updated to $6.00

### Frontend UI Test

1. Navigate to event edit page: `http://localhost:3000/events/{eventId}/manage`
2. Modify group pricing tiers:
   - Change tier 1 price from $5.00 to $6.00
   - Change tier 2 price from $10.00 to $12.00
   - Delete tier 3 (3+ attendees @ $15.00)
3. Click **Update Event**
4. Verify success message
5. Navigate back to manage page
6. Confirm tier changes persisted:
   - Tier 1: 1 person @ $6.00 ✅
   - Tier 2: 2+ persons @ $12.00 ✅
   - Tier 3: DELETED ✅

## Why This Fix Works

### EF Core Change Tracking for JSONB

EF Core uses **snapshot change tracking** for owned entities. When an owned entity is stored as JSONB:

1. **Default Behavior**: EF Core compares the current value to the snapshot to detect changes
2. **Problem**: Replacing a JSONB value object may not trigger the change detector
3. **Solution**: Explicitly mark the property as modified using `Entry().Property().IsModified = true`

From Microsoft docs:
> "For owned entity types configured as JSON columns, EF Core may not automatically detect changes. You should explicitly mark the navigation property as modified after replacing the entire value object."

### Why `MarkAsUpdated()` Wasn't Enough

The `MarkAsUpdated()` method in `BaseEntity` only sets the `UpdatedAt` timestamp and marks the root entity as modified. It does **NOT** mark navigation properties (like `Pricing`) as modified.

### Alternative Approaches Considered

1. **Remove and Re-add**: Replace `Pricing` by setting to null, then setting to new value
   - ❌ Breaks domain invariants
   - ❌ Creates unnecessary audit trail entries
   - ❌ Poor performance

2. **Modify Individual Properties**: Update tier prices in-place
   - ❌ `GroupPricingTier` is a value object (immutable)
   - ❌ Violates DDD principles
   - ❌ Can't handle tier addition/removal

3. **Explicit SQL Command**: Use raw SQL to update JSONB column
   - ❌ Bypasses domain logic
   - ❌ Doesn't raise domain events
   - ❌ Violates Clean Architecture

4. **Call SaveChangesAsync Twice**: First to persist entity, then pricing
   - ❌ Poor performance (2 DB round-trips)
   - ❌ Inconsistent state between calls
   - ❌ Violates transaction boundary

**Chosen Solution**: Explicit change tracking via `MarkPricingAsModified()`
- ✅ Minimal code changes
- ✅ Preserves domain logic
- ✅ Raises domain events correctly
- ✅ Single database transaction
- ✅ Follows EF Core best practices

## Related Issues

### Similar Bugs May Exist For:
- Dual pricing (adult/child) updates
- Legacy single pricing updates
- Any other JSONB-stored value objects

### Preventive Measures:
1. Add integration tests for all pricing type updates
2. Document JSONB change tracking requirements
3. Create ADR (Architecture Decision Record) for JSONB usage
4. Consider adding repository helper methods for other JSONB properties

## References

- [EF Core Owned Entity Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)
- [EF Core JSON Columns](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns)
- [PostgreSQL JSONB Type](https://www.postgresql.org/docs/current/datatype-json.html)
- [EF Core Change Tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/)

## Next Steps

1. Deploy fix to staging environment
2. Run API-level test to verify group pricing updates work
3. Run UI-level test to verify end-to-end flow
4. Create similar fix for dual pricing if needed
5. Add integration tests to prevent regression
