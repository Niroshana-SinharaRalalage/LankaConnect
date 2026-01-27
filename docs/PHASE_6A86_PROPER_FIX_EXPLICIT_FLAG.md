# Phase 6A.86: Proper Fix - Add Explicit IsFreeEvent Flag

**Date**: January 26, 2026
**Issue**: Free events showing as "Paid Event" due to NULL pricing ambiguity
**Proper Solution**: Add explicit `IsFreeEvent` boolean flag to eliminate ambiguity

---

## Problem Analysis

### Current Flawed Design (Computed State)

```csharp
// src/LankaConnect.Domain/Events/Event.cs
public bool IsFree()  // ❌ COMPUTED - No explicit flag
{
    // Tries to derive "free" from pricing amounts
    if (Pricing != null) return Pricing.AdultPrice.IsZero;
    if (TicketPrice != null) return TicketPrice.IsZero;

    // NULL is ambiguous: "free" or "not configured"?
    return false;  // ❌ GUESSING!
}
```

**Problems:**
1. **Ambiguity**: NULL could mean "free" OR "misconfigured"
2. **Fragile**: Logic changes break existing data (Phase 6A.81 bug)
3. **Complexity**: Every new pricing type adds branches
4. **Maintenance**: Hard to reason about edge cases

---

## Proper Solution: Explicit Boolean Flag

### Architecture: Source of Truth Pattern

```csharp
// Domain Model
public class Event
{
    // ✅ EXPLICIT FLAG - Single source of truth
    public bool IsFreeEvent { get; private set; }

    // Pricing can still be set for free events (for display/reporting)
    public Money? TicketPrice { get; private set; }
    public TicketPricing? Pricing { get; private set; }

    // ✅ Simple, unambiguous method
    public bool IsFree() => IsFreeEvent;

    // Business rule validation
    public Result SetAsFreeEvent()
    {
        IsFreeEvent = true;
        // Optional: Can still set $0 pricing for reporting
        TicketPrice = Money.Zero(Currency.USD);
        return Result.Success();
    }

    public Result SetPricing(Money ticketPrice)
    {
        if (ticketPrice.IsZero)
        {
            IsFreeEvent = true;
            TicketPrice = ticketPrice;
        }
        else
        {
            IsFreeEvent = false;
            TicketPrice = ticketPrice;
        }
        return Result.Success();
    }
}
```

---

## Benefits of Explicit Flag

### 1. **Unambiguous Intent**
```csharp
// ✅ CLEAR: Organizer explicitly marked as free
IsFreeEvent = true

// ✅ CLEAR: Organizer set pricing, not free
IsFreeEvent = false
```

### 2. **Security: No Bypass Possible**
```csharp
// Can't bypass payment by setting NULL
if (!IsFreeEvent && TicketPrice == null)
    return Result.Failure("Paid events must have pricing configured");
```

### 3. **Validation is Simple**
```csharp
// Free events can have NULL or $0 pricing (both valid)
if (IsFreeEvent)
{
    // Allow NULL or $0 - doesn't matter, flag is source of truth
    return Result.Success();
}
else
{
    // Paid events MUST have pricing
    if (TicketPrice == null && Pricing == null)
        return Result.Failure("Pricing required for paid events");
}
```

### 4. **Backwards Compatible**
```csharp
// Migration: Infer flag from existing data
UPDATE events."Events"
SET "IsFreeEvent" = (
    "TicketPrice_Amount" = 0
    OR "Pricing_AdultPrice_Amount" = 0
    OR ("TicketPrice_Amount" IS NULL AND "Pricing_AdultPrice_Amount" IS NULL)
);
```

---

## Implementation Plan

### Phase 1: Add Flag to Domain Model

```csharp
// src/LankaConnect.Domain/Events/Event.cs

public class Event : BaseEntity
{
    // ✅ NEW: Explicit flag
    public bool IsFreeEvent { get; private set; }

    // Existing pricing fields (keep for backwards compatibility)
    public Money? TicketPrice { get; private set; }
    public TicketPricing? Pricing { get; private set; }

    // ✅ UPDATED: Simple method uses flag
    public bool IsFree() => IsFreeEvent;

    // ✅ NEW: Factory method for free events
    public static Result<Event> CreateFreeEvent(
        EventTitle title,
        EventDescription description,
        DateTime startDate,
        DateTime endDate,
        Guid organizerId,
        int capacity)
    {
        var result = Create(title, description, startDate, endDate,
                           organizerId, capacity);

        if (result.IsSuccess)
        {
            result.Value.IsFreeEvent = true;
            result.Value.TicketPrice = Money.Zero(Currency.USD);  // Optional for display
        }

        return result;
    }

    // ✅ UPDATED: SetPricing updates flag
    public Result SetPricing(Money ticketPrice)
    {
        if (ticketPrice == null)
            return Result.Failure("Ticket price cannot be null. Use CreateFreeEvent for free events.");

        IsFreeEvent = ticketPrice.IsZero;
        TicketPrice = ticketPrice;
        MarkAsUpdated();

        return Result.Success();
    }

    // ✅ UPDATED: SetDualPricing updates flag
    public Result SetDualPricing(TicketPricing pricing)
    {
        if (pricing == null)
            return Result.Failure("Pricing cannot be null");

        IsFreeEvent = pricing.AdultPrice.IsZero;
        Pricing = pricing;
        MarkAsUpdated();

        return Result.Success();
    }
}
```

### Phase 2: Database Migration

```csharp
// Migration: 20260126_Phase6A86_AddIsFreeEventFlag.cs

public partial class Phase6A86_AddIsFreeEventFlag : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add column with default
        migrationBuilder.AddColumn<bool>(
            name: "IsFreeEvent",
            schema: "events",
            table: "Events",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Backfill: Infer from existing pricing
        migrationBuilder.Sql(@"
            UPDATE events.""Events""
            SET ""IsFreeEvent"" = (
                COALESCE(""TicketPrice_Amount"", 0) = 0
                AND COALESCE(""Pricing_AdultPrice_Amount"", 0) = 0
            )
            WHERE ""DeletedAt"" IS NULL;
        ");

        // Add index for query performance
        migrationBuilder.CreateIndex(
            name: "IX_Events_IsFreeEvent",
            schema: "events",
            table: "Events",
            column: "IsFreeEvent");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Events_IsFreeEvent",
            schema: "events",
            table: "Events");

        migrationBuilder.DropColumn(
            name: "IsFreeEvent",
            schema: "events",
            table: "Events");
    }
}
```

### Phase 3: Update Application Layer

```csharp
// src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs

public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    Result<Event> eventResult;

    // ✅ Use explicit free event factory
    if (request.IsFreeEvent || (request.TicketPriceAmount.HasValue && request.TicketPriceAmount.Value == 0))
    {
        eventResult = Event.CreateFreeEvent(
            title, description, startDate, endDate, organizerId, capacity);
    }
    else
    {
        eventResult = Event.Create(
            title, description, startDate, endDate, organizerId, capacity);

        if (eventResult.IsSuccess && request.TicketPriceAmount.HasValue)
        {
            var money = Money.Create(request.TicketPriceAmount.Value, request.TicketPriceCurrency ?? Currency.USD);
            eventResult.Value.SetPricing(money.Value);
        }
    }

    // ... rest of handler
}
```

### Phase 4: Update API DTOs

```csharp
// src/LankaConnect.Application/Events/Common/EventDto.cs

public record EventDto
{
    // ✅ Explicit flag from domain
    public bool IsFree { get; init; }  // Maps from Event.IsFreeEvent

    // Pricing amounts (still exposed for UI display)
    public decimal? TicketPriceAmount { get; init; }
    public Currency? TicketPriceCurrency { get; init; }

    // ... other fields
}

// Mapping
CreateMap<Event, EventDto>()
    .ForMember(dest => dest.IsFree, opt => opt.MapFrom(src => src.IsFreeEvent));  // ✅ Direct mapping
```

### Phase 5: Update Frontend

```typescript
// web/src/app/events/page.tsx

{/* ✅ Simple, explicit check */}
{event.isFree  // This comes from IsFreeEvent flag now, not computed!
  ? 'Free Event'
  : event.ticketPriceAmount != null
    ? `$${event.ticketPriceAmount.toFixed(2)}`
    : 'Paid Event'}
```

---

## Comparison: Quick Fix vs Proper Fix

### Option A: Quick Fix (Data Migration)
```sql
-- Just backfill $0 pricing
UPDATE events."Events"
SET "TicketPrice_Amount" = 0.00
WHERE "TicketPrice_Amount" IS NULL;
```

**Pros:**
- ✅ Quick (1 hour)
- ✅ No code changes
- ✅ Fixes immediate bug

**Cons:**
- ❌ Doesn't fix root cause (still relies on computed state)
- ❌ Fragile - same problem will happen again
- ❌ Ambiguity remains (NULL still means "not configured")

### Option B: Proper Fix (Explicit Flag)
```csharp
// Add explicit flag
public bool IsFreeEvent { get; private set; }
public bool IsFree() => IsFreeEvent;
```

**Pros:**
- ✅ Fixes root cause (explicit intent)
- ✅ Eliminates ambiguity (NULL is clearly invalid)
- ✅ Future-proof (won't break with new pricing models)
- ✅ Better security (can't bypass with NULL)
- ✅ Simpler logic (one boolean, not complex derivation)

**Cons:**
- ⏱️ Takes longer (4-6 hours for migration + testing)
- 🔄 Requires database migration
- 📝 More comprehensive testing needed

---

## Recommendation: Do BOTH

### Immediate (Today):
1. **Quick Fix**: Apply SQL migration to unblock users
   - Backfill $0 pricing for free events
   - Deploy to staging → production
   - **Time**: 1-2 hours

### Proper Fix (Next Sprint - Phase 6A.87):
1. **Add explicit flag**: Implement `IsFreeEvent` property
   - Create migration
   - Update domain model
   - Update application layer
   - Update API DTOs
   - Update frontend
   - **Time**: 1-2 days (including testing)

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public void CreateFreeEvent_SetsIsFreeEventFlag()
{
    var result = Event.CreateFreeEvent(title, desc, start, end, orgId, capacity);

    Assert.True(result.IsSuccess);
    Assert.True(result.Value.IsFreeEvent);
    Assert.True(result.Value.IsFree());
}

[Fact]
public void SetPricing_WithZeroAmount_SetsIsFreeEventFlag()
{
    var @event = CreateTestEvent();
    var zeroPricing = Money.Zero(Currency.USD);

    @event.SetPricing(zeroPricing);

    Assert.True(@event.IsFreeEvent);
    Assert.True(@event.IsFree());
}

[Fact]
public void SetPricing_WithNullPrice_ReturnsFailure()
{
    var @event = CreateTestEvent();

    var result = @event.SetPricing(null);

    Assert.True(result.IsFailure);
    Assert.Contains("cannot be null", result.Error);
}
```

---

## Summary

**Current Problem**: Derived state (`IsFree()` computed from amounts) is ambiguous and fragile

**User's Suggestion**: Add explicit `IsFreeEvent` boolean flag ✅ **CORRECT SOLUTION**

**Action Plan**:
1. **Immediate**: Apply data migration (SQL fix) to unblock users
2. **Proper Fix**: Add `IsFreeEvent` flag in next sprint (Phase 6A.87)

---

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
