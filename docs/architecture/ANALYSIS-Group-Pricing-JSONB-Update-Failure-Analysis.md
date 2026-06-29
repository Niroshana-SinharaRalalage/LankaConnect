# ADR-005: Group Pricing JSONB Update Failure - Root Cause Analysis

**Date**: 2025-12-14
**Status**: Analysis Complete
**Session**: 33
**Severity**: Critical (HTTP 500 errors in production)

## Problem Statement

Group pricing tier updates fail with HTTP 500 Internal Server Error when using the `UpdateEvent` endpoint. Simple updates (title only) work correctly, but updates containing group pricing tier data cause the API to crash without logging detailed exception information.

## Context

### Implementation Overview

**Session 33 Implementation**: Added `MarkPricingAsModified()` method to force EF Core JSONB change tracking:

```csharp
// EventRepository.cs (lines 299-304)
public void MarkPricingAsModified(Event @event)
{
    _context.Entry(@event).Property(e => e.Pricing).IsModified = true;
}
```

**UpdateEventCommandHandler Usage** (lines 208-229):
```csharp
if (pricing != null)
{
    Result setPricingResult;
    if (isGroupPricing)
    {
        setPricingResult = @event.SetGroupPricing(pricing);
    }
    else
    {
        setPricingResult = @event.SetDualPricing(pricing);
    }

    if (setPricingResult.IsFailure)
        return setPricingResult;

    // CRITICAL FIX: Explicitly mark Pricing as modified
    _eventRepository.MarkPricingAsModified(@event);
}
```

### EF Core Configuration

**EventConfiguration.cs** (lines 75-90):
```csharp
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");  // JSONB column

    pricing.OwnsOne(p => p.AdultPrice);
    pricing.OwnsOne(p => p.ChildPrice);

    pricing.OwnsMany(p => p.GroupTiers, tier =>
    {
        tier.OwnsOne(t => t.PricePerPerson);
    });
});
```

### Test Results

- ✅ **Simple update** (title only): HTTP 200 Success
- ❌ **Group pricing update**: HTTP 500 (empty response body, server crash)
- ✅ **Local builds**: 0 errors, 0 warnings (backend and frontend)
- ✅ **Deployment**: Success (commit 9707f48)
- ❌ **Azure logs**: No exception traces (only unrelated email queue errors)

## Root Cause Analysis

### Issue #1: Invalid EF Core Change Tracking Call ⚠️

**Problem**: The `MarkPricingAsModified()` implementation is **architecturally incorrect** for JSONB-stored owned entities in EF Core 8.

**Why This Fails**:

1. **JSONB Owned Entities Are Not Tracked As Properties**
   - `Pricing` is configured as `.ToJson("pricing")`, making it a JSONB column
   - EF Core serializes the entire `TicketPricing` object graph into a single JSONB string
   - `_context.Entry(@event).Property(e => e.Pricing)` **does not represent a trackable property in the traditional sense**

2. **Owned Entities vs. JSONB Serialization**
   - Traditional owned entities (without `.ToJson()`) are tracked at the property level
   - JSONB owned entities are tracked as **whole-document replacements**, not property changes
   - Calling `.IsModified = true` on a JSONB property reference is **undefined behavior** in EF Core

3. **Potential Null Reference**
   - If `@event.Pricing` is null, `Entry(@event).Property(e => e.Pricing)` may throw
   - No null check exists in the current implementation

### Issue #2: Incorrect Change Detection Pattern

**The Microsoft-Recommended Pattern** for JSONB owned entities is:

```csharp
// ✅ CORRECT: Replace the entire owned entity instance
@event.Pricing = new TicketPricing(...);  // This triggers EF Core change tracking

// ❌ WRONG: Marking individual properties as modified
_context.Entry(@event).Property(e => e.Pricing).IsModified = true;
```

**Why Replacement Works**:
- EF Core detects the object reference change
- JSONB columns are updated by **full document replacement**, not partial updates
- This aligns with EF Core's JSONB serialization model

### Issue #3: Domain Method Side Effects

Looking at `Event.SetGroupPricing()` (lines 660-679):

```csharp
public Result SetGroupPricing(TicketPricing? pricing)
{
    if (pricing == null)
        return Result.Failure("Pricing cannot be null");

    if (pricing.Type != PricingType.GroupTiered)
        return Result.Failure("Only GroupTiered pricing type is allowed for SetGroupPricing");

    Pricing = pricing;  // ✅ This SHOULD trigger change tracking
    TicketPrice = null; // Side effect: Clears legacy pricing

    MarkAsUpdated();

    RaiseDomainEvent(new EventPricingUpdatedEvent(Id, pricing, DateTime.UtcNow));

    return Result.Success();
}
```

**Analysis**:
- The domain method **already assigns** `Pricing = pricing`
- This **should** trigger EF Core change tracking automatically
- The additional `MarkPricingAsModified()` call is **redundant** if change tracking is working
- If change tracking isn't working, the issue is elsewhere

### Issue #4: Missing `Pricing` in `GetByIdAsync` Includes?

**EventRepository.GetByIdAsync** (lines 20-32):
```csharp
return await _dbSet
    .Include(e => e.Images)
    .Include(e => e.Videos)
    .Include(e => e.Registrations)
    .Include(e => e.SignUpLists)
        .ThenInclude(s => s.Commitments)
    .Include(e => e.SignUpLists)
        .ThenInclude(s => s.Items)
            .ThenInclude(i => i.Commitments)
    .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
```

**Critical Discovery**: `Pricing` is **NOT explicitly included**!

**Why This Matters**:
- JSONB owned entities configured with `.ToJson()` are **loaded automatically** in EF Core 8
- However, **if the entity is already in the change tracker**, EF Core might not reload JSONB data
- This could lead to stale `Pricing` data being used

### Issue #5: Potential TicketPricing Construction Issue

**UpdateEventCommandHandler** (lines 99-128) builds `TicketPricing` from request:

```csharp
if (request.GroupPricingTiers != null && request.GroupPricingTiers.Count > 0)
{
    var tiers = new List<GroupPricingTier>();
    var currency = request.GroupPricingTiers[0].Currency;

    foreach (var tierRequest in request.GroupPricingTiers)
    {
        var priceResult = Money.Create(tierRequest.PricePerPerson, tierRequest.Currency);
        if (priceResult.IsFailure)
            return Result.Failure(priceResult.Error);

        var tierResult = GroupPricingTier.Create(
            tierRequest.MinAttendees,
            tierRequest.MaxAttendees,
            priceResult.Value
        );

        if (tierResult.IsFailure)
            return Result.Failure(tierResult.Error);

        tiers.Add(tierResult.Value);
    }

    var groupPricingResult = TicketPricing.CreateGroupTiered(tiers, currency);
    if (groupPricingResult.IsFailure)
        return Result.Failure(groupPricingResult.Error);

    pricing = groupPricingResult.Value;
    isGroupPricing = true;
}
```

**Potential Issues**:
1. **Currency Mismatch**: Takes currency from first tier only (line 103)
   - What if tiers have different currencies? Validation in `CreateGroupTiered()` should catch this
2. **Tier Validation**: `CreateGroupTiered()` has extensive validation (lines 135-183)
   - Could fail silently if validation error messages aren't clear
3. **Empty Tiers List**: Check happens at line 99, but what if count is 0?

## Why No Exception Logs?

**Hypothesis**: The HTTP 500 might be caused by:

1. **Synchronous Exception in Change Tracking**
   - EF Core throws during `SaveChangesAsync()` when processing invalid JSONB state
   - Exception might be caught by middleware without detailed logging

2. **Database Constraint Violation**
   - PostgreSQL might reject invalid JSONB structure
   - Connection might be lost before exception is logged

3. **Memory/Serialization Issue**
   - Large `TicketPricing` object graph might fail JSON serialization
   - OutOfMemoryException or SerializationException swallowed by framework

## Recommended Solutions

### Solution #1: Remove Redundant `MarkPricingAsModified()` (Preferred)

**Rationale**: The domain method already assigns `Pricing = pricing`, which should trigger change tracking.

**Implementation**:
```csharp
// UpdateEventCommandHandler.cs (lines 208-229)
if (pricing != null)
{
    Result setPricingResult;
    if (isGroupPricing)
    {
        setPricingResult = @event.SetGroupPricing(pricing);
    }
    else
    {
        setPricingResult = @event.SetDualPricing(pricing);
    }

    if (setPricingResult.IsFailure)
        return setPricingResult;

    // REMOVE THIS LINE:
    // _eventRepository.MarkPricingAsModified(@event);
}
```

**Delete Method**:
```csharp
// EventRepository.cs - DELETE LINES 294-304
// public void MarkPricingAsModified(Event @event) { ... }
```

**Why This Works**:
- EF Core detects object reference changes automatically for JSONB owned entities
- Domain method already handles assignment
- Eliminates undefined behavior from manual property tracking

### Solution #2: Use Detach/Attach Pattern (Alternative)

If EF Core change tracking still fails:

```csharp
// EventRepository.cs
public void ForceReloadPricing(Event @event)
{
    // Detach and reload to ensure JSONB is refreshed
    _context.Entry(@event).State = EntityState.Detached;
    var reloaded = _dbSet.Find(@event.Id);
    if (reloaded != null)
    {
        @event = reloaded;
    }
}
```

**Drawback**: This is a workaround, not a proper solution.

### Solution #3: Add Explicit JSONB Update (Last Resort)

```csharp
// EventRepository.cs
public async Task UpdatePricingJsonb(Guid eventId, string pricingJson)
{
    await _context.Database.ExecuteSqlRawAsync(
        "UPDATE events SET pricing = @p0::jsonb WHERE id = @p1",
        pricingJson,
        eventId
    );
}
```

**Drawback**: Bypasses domain model and ORM; violates Clean Architecture.

## Testing Strategy

### Step 1: Verify Change Tracking

```csharp
[Fact]
public async Task SetGroupPricing_ShouldTriggerChangeTracking()
{
    // Arrange
    var @event = CreateTestEvent();
    _context.Events.Add(@event);
    await _context.SaveChangesAsync();
    _context.Entry(@event).State = EntityState.Unchanged;

    // Act
    var pricing = TicketPricing.CreateGroupTiered(...);
    @event.SetGroupPricing(pricing.Value);

    // Assert
    var entry = _context.Entry(@event);
    Assert.True(entry.Property(e => e.Pricing).IsModified);
}
```

### Step 2: Integration Test with Repository

```csharp
[Fact]
public async Task UpdateEventWithGroupPricing_ShouldPersist()
{
    // Arrange
    var @event = CreateTestEvent();
    await _eventRepository.AddAsync(@event, CancellationToken.None);
    await _unitOfWork.CommitAsync();

    // Act
    var loaded = await _eventRepository.GetByIdAsync(@event.Id);
    var pricing = TicketPricing.CreateGroupTiered(...);
    loaded.SetGroupPricing(pricing.Value);
    await _unitOfWork.CommitAsync();

    // Assert
    var reloaded = await _eventRepository.GetByIdAsync(@event.Id);
    Assert.NotNull(reloaded.Pricing);
    Assert.Equal(PricingType.GroupTiered, reloaded.Pricing.Type);
}
```

### Step 3: API Integration Test

```bash
# Test group pricing update
curl -X PUT "https://api/events/{id}" \
  -H "Content-Type: application/json" \
  -d '{
    "groupPricingTiers": [
      { "minAttendees": 1, "maxAttendees": 5, "pricePerPerson": 100, "currency": "USD" },
      { "minAttendees": 6, "maxAttendees": 10, "pricePerPerson": 80, "currency": "USD" }
    ]
  }'
```

## Impact Assessment

### Current State
- ❌ **Production**: Group pricing updates fail with HTTP 500
- ✅ **Builds**: Compile successfully (no TypeScript/C# errors)
- ❌ **Functionality**: Feature is completely broken in production

### Risk Level
- **Severity**: Critical
- **User Impact**: High (organizers cannot update event pricing)
- **Data Integrity**: Unknown (need to verify if partial updates occurred)

### Rollback Options
1. **Code Rollback**: Revert to commit before Session 33 changes
2. **Feature Flag**: Disable group pricing in UI until fixed
3. **Database Migration**: Add SQL script to manually update pricing JSONB

## Decision

**Recommended Action**: Implement **Solution #1** (Remove `MarkPricingAsModified()`)

**Rationale**:
1. Aligns with EF Core 8 JSONB best practices
2. Simplifies codebase (removes unnecessary method)
3. Leverages built-in change tracking
4. No database migration required
5. Minimal code changes

**Validation**:
1. Add unit tests for `SetGroupPricing()` change tracking
2. Add integration tests for repository persistence
3. Test in staging environment before production deployment
4. Monitor Azure logs for any new exceptions

## Next Steps

1. Remove `MarkPricingAsModified()` implementation
2. Remove call from `UpdateEventCommandHandler`
3. Add comprehensive test coverage
4. Deploy to staging
5. Verify fix with actual group pricing update request
6. Monitor production logs after deployment

## Lessons Learned

1. **JSONB Owned Entities Require Different Patterns**
   - Cannot treat JSONB columns like regular properties
   - Change tracking works via object replacement, not property marking

2. **Testing Gap**
   - No integration tests for group pricing updates
   - Need end-to-end API tests for complex pricing scenarios

3. **Logging Improvements**
   - Azure logs should capture all EF Core exceptions
   - Need better error handling in API layer

4. **Documentation**
   - EF Core JSONB patterns should be documented in ADR
   - Repository patterns need architecture guidelines

---

**References**:
- [EF Core 8 JSON Columns Documentation](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-8.0/whatsnew#json-columns)
- [PostgreSQL JSONB Best Practices](https://www.postgresql.org/docs/current/datatype-json.html)
- Session 33 Commit: 9707f48
