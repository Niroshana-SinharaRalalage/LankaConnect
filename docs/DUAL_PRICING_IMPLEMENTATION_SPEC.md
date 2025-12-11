# Dual Pricing Implementation Specification

**Status**: Backend Implementation Required
**Estimated Effort**: 4-6 hours (Backend: 3-4h, Frontend: 1-2h)
**Priority**: HIGH - Frontend UI already sends dual pricing data that backend cannot receive
**Created**: 2025-01-02

---

## 🎯 Overview

**Current State**: Frontend has complete dual pricing UI (Session 21), but backend API cannot accept or return dual pricing data.

**Goal**: Complete backend implementation so organizers can create events with age-based pricing (adult/child rates).

**Data Integrity Risk**: Frontend currently sends dual pricing fields that backend silently ignores, causing events to be created as FREE instead of paid.

---

## 📊 Background: Pricing Modes

### **Mode 1: Single Pricing** (Everyone pays same price)
```typescript
{
  adultPriceAmount: 25.00,
  adultPriceCurrency: USD,
  childPriceAmount: null,    // Null means no child pricing
  childPriceCurrency: null,
  childAgeLimit: null
}
```

### **Mode 2: Dual Pricing** (Age-based pricing)
```typescript
{
  adultPriceAmount: 25.00,
  adultPriceCurrency: USD,
  childPriceAmount: 15.00,
  childPriceCurrency: USD,
  childAgeLimit: 12           // Under 12 = child price, 12+ = adult price
}
```

---

## ✅ What's Already Done

### **Domain Layer** ✅ COMPLETE
- **File**: `backend/src/Domain/Events/Event.cs`
- **Property**: `TicketPricing? Pricing` (JSONB column)
- **Methods**: `SetDualPricing()`, `IsFree` property
- **Validation**: Full validation in TicketPricing value object

### **Database** ✅ COMPLETE
- **Column**: `pricing` (JSONB) in `events` table
- **Migration**: Applied (December 2024)
- **Legacy Support**: Old `ticket_price_amount`, `ticket_price_currency` columns still exist

### **Frontend** ✅ COMPLETE
- **Form**: Event creation form with dual pricing UI
- **Validation**: Zod schema with cross-field validation
- **Payload**: Sends all 5 dual pricing fields to API
- **Files**:
  - `web/src/presentation/components/features/events/EventCreationForm.tsx`
  - `web/src/presentation/lib/validators/event.schemas.ts`

---

## ❌ What's Missing (YOUR TASKS)

### **Backend API Layer** - All Changes Needed

| File | Location | What to Add |
|------|----------|-------------|
| `CreateEventCommand.cs` | `/Events/Commands/CreateEvent/` | 5 dual pricing properties |
| `UpdateEventCommand.cs` | `/Events/Commands/UpdateEvent/` | 5 dual pricing properties |
| `EventDto.cs` | `/Events/Common/` | 5 dual pricing properties + `HasDualPricing` flag |
| `CreateEventCommandHandler.cs` | `/Events/Commands/CreateEvent/` | Logic to build `TicketPricing` object |
| `UpdateEventCommandHandler.cs` | `/Events/Commands/UpdateEvent/` | Logic to update `TicketPricing` object |
| `EventMappingProfile.cs` | `/Common/Mappings/` | Map `Event.Pricing` → `EventDto` |

---

## 📋 TASK 1: Update CreateEventCommand (30 min)

### **File**: `backend/src/Application/Events/Commands/CreateEvent/CreateEventCommand.cs`

### **Current Code** (Lines ~20-50):
```csharp
public record CreateEventCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string OrganizerId,
    int Capacity,
    EventCategory Category,

    // Location (optional)
    string? LocationAddress,
    string? LocationCity,
    string? LocationState,
    string? LocationZipCode,
    string? LocationCountry,
    decimal? LocationLatitude,
    decimal? LocationLongitude,

    // Legacy single pricing
    decimal? TicketPriceAmount,
    Currency? TicketPriceCurrency
) : IRequest<Guid>;
```

### **Required Change**:
```csharp
public record CreateEventCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    string OrganizerId,
    int Capacity,
    EventCategory Category,

    // Location (optional)
    string? LocationAddress,
    string? LocationCity,
    string? LocationState,
    string? LocationZipCode,
    string? LocationCountry,
    decimal? LocationLatitude,
    decimal? LocationLongitude,

    // Legacy single pricing (backward compatible)
    decimal? TicketPriceAmount,
    Currency? TicketPriceCurrency,

    // NEW: Dual pricing support
    decimal? AdultPriceAmount,
    Currency? AdultPriceCurrency,
    decimal? ChildPriceAmount,
    Currency? ChildPriceCurrency,
    int? ChildAgeLimit
) : IRequest<Guid>;
```

### **Validation** (Add to CreateEventCommandValidator.cs):
```csharp
// Rule 1: If child pricing set, both price AND age limit required
RuleFor(x => x)
    .Must(x => (x.ChildPriceAmount == null && x.ChildAgeLimit == null) ||
               (x.ChildPriceAmount != null && x.ChildAgeLimit != null))
    .WithMessage("If child pricing is enabled, both child price and age limit must be provided");

// Rule 2: Child price cannot exceed adult price
RuleFor(x => x)
    .Must(x => x.ChildPriceAmount == null || x.ChildPriceAmount <= x.AdultPriceAmount)
    .WithMessage("Child price cannot exceed adult price");

// Rule 3: Currencies must match
RuleFor(x => x)
    .Must(x => x.ChildPriceCurrency == null || x.ChildPriceCurrency == x.AdultPriceCurrency)
    .WithMessage("Adult and child prices must use the same currency");

// Rule 4: Age limit must be 1-18
RuleFor(x => x.ChildAgeLimit)
    .InclusiveBetween(1, 18)
    .When(x => x.ChildAgeLimit != null)
    .WithMessage("Child age limit must be between 1 and 18");
```

---

## 📋 TASK 2: Update CreateEventCommandHandler (45 min)

### **File**: `backend/src/Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs`

### **Current Code** (Lines ~85-105):
```csharp
// Legacy pricing - creates event with TicketPrice only
if (command.TicketPriceAmount.HasValue && command.TicketPriceCurrency.HasValue)
{
    var ticketPrice = new Money(
        command.TicketPriceAmount.Value,
        command.TicketPriceCurrency.Value
    );
    @event.SetTicketPrice(ticketPrice);
}
```

### **Required Change**:
```csharp
// Pricing logic: Use new dual pricing if provided, otherwise legacy format
if (command.AdultPriceAmount.HasValue && command.AdultPriceCurrency.HasValue)
{
    // NEW: Dual pricing or single pricing via adult price
    var adultPrice = new Money(
        command.AdultPriceAmount.Value,
        command.AdultPriceCurrency.Value
    );

    Money? childPrice = null;
    if (command.ChildPriceAmount.HasValue && command.ChildPriceCurrency.HasValue)
    {
        childPrice = new Money(
            command.ChildPriceAmount.Value,
            command.ChildPriceCurrency.Value
        );
    }

    var pricing = TicketPricing.Create(
        adultPrice,
        childPrice,
        command.ChildAgeLimit
    );

    @event.SetDualPricing(pricing);
}
else if (command.TicketPriceAmount.HasValue && command.TicketPriceCurrency.HasValue)
{
    // Legacy format: Migrate to dual pricing with no child pricing
    var price = new Money(
        command.TicketPriceAmount.Value,
        command.TicketPriceCurrency.Value
    );

    var pricing = TicketPricing.Create(price, null, null);
    @event.SetDualPricing(pricing);
}
```

---

## 📋 TASK 3: Update EventDto (20 min)

### **File**: `backend/src/Application/Events/Common/EventDto.cs`

### **Current Code** (Lines ~30-60):
```csharp
public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    // ... other properties ...

    // Legacy pricing
    public decimal? TicketPriceAmount { get; init; }
    public Currency? TicketPriceCurrency { get; init; }
    public bool IsFree { get; init; }
}
```

### **Required Change**:
```csharp
public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    // ... other properties ...

    // Legacy pricing (keep for backward compatibility)
    public decimal? TicketPriceAmount { get; init; }
    public Currency? TicketPriceCurrency { get; init; }

    // NEW: Dual pricing support
    public decimal? AdultPriceAmount { get; init; }
    public Currency? AdultPriceCurrency { get; init; }
    public decimal? ChildPriceAmount { get; init; }
    public Currency? ChildPriceCurrency { get; init; }
    public int? ChildAgeLimit { get; init; }
    public bool HasDualPricing { get; init; }  // True if child pricing is set

    public bool IsFree { get; init; }
}
```

---

## 📋 TASK 4: Update EventMappingProfile (30 min)

### **File**: `backend/src/Application/Common/Mappings/EventMappingProfile.cs`

### **Current Code** (Lines ~20-50):
```csharp
CreateMap<Event, EventDto>()
    .ForMember(d => d.TicketPriceAmount, opt => opt.MapFrom(s => s.TicketPrice != null ? s.TicketPrice.Amount : (decimal?)null))
    .ForMember(d => d.TicketPriceCurrency, opt => opt.MapFrom(s => s.TicketPrice != null ? s.TicketPrice.Currency : (Currency?)null))
    .ForMember(d => d.IsFree, opt => opt.MapFrom(s => s.IsFree));
```

### **Required Change**:
```csharp
CreateMap<Event, EventDto>()
    // Legacy fields (for backward compatibility)
    .ForMember(d => d.TicketPriceAmount, opt => opt.MapFrom(s =>
        s.Pricing != null ? s.Pricing.AdultPrice.Amount : (decimal?)null))
    .ForMember(d => d.TicketPriceCurrency, opt => opt.MapFrom(s =>
        s.Pricing != null ? s.Pricing.AdultPrice.Currency : (Currency?)null))

    // NEW: Dual pricing fields
    .ForMember(d => d.AdultPriceAmount, opt => opt.MapFrom(s =>
        s.Pricing != null ? s.Pricing.AdultPrice.Amount : (decimal?)null))
    .ForMember(d => d.AdultPriceCurrency, opt => opt.MapFrom(s =>
        s.Pricing != null ? s.Pricing.AdultPrice.Currency : (Currency?)null))
    .ForMember(d => d.ChildPriceAmount, opt => opt.MapFrom(s =>
        s.Pricing != null && s.Pricing.ChildPrice != null ? s.Pricing.ChildPrice.Amount : (decimal?)null))
    .ForMember(d => d.ChildPriceCurrency, opt => opt.MapFrom(s =>
        s.Pricing != null && s.Pricing.ChildPrice != null ? s.Pricing.ChildPrice.Currency : (Currency?)null))
    .ForMember(d => d.ChildAgeLimit, opt => opt.MapFrom(s =>
        s.Pricing != null ? s.Pricing.ChildAgeLimit : (int?)null))
    .ForMember(d => d.HasDualPricing, opt => opt.MapFrom(s =>
        s.Pricing != null && s.Pricing.ChildPrice != null))

    .ForMember(d => d.IsFree, opt => opt.MapFrom(s => s.IsFree));
```

---

## 📋 TASK 5: Update UpdateEventCommand (15 min)

### **File**: `backend/src/Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs`

**Same changes as CreateEventCommand** - add 5 dual pricing fields.

---

## 📋 TASK 6: Update UpdateEventCommandHandler (30 min)

### **File**: `backend/src/Application/Events/Commands/UpdateEvent/UpdateEventCommandHandler.cs`

**Same logic as CreateEventCommandHandler** - build/update TicketPricing object.

---

## 📋 TASK 7: Data Migration Script (30 min)

### **Purpose**: Migrate existing events from legacy pricing to new format

### **SQL Script**:
```sql
-- Migration: Convert legacy single pricing to new unified TicketPricing format
UPDATE events
SET pricing = jsonb_build_object(
    'adultPrice', jsonb_build_object(
        'amount', ticket_price_amount,
        'currency', ticket_price_currency
    ),
    'childPrice', null,
    'childAgeLimit', null
)
WHERE ticket_price_amount IS NOT NULL
  AND pricing IS NULL;

-- Verify migration
SELECT
    id,
    title,
    ticket_price_amount as legacy_price,
    pricing->>'adultPrice'->>'amount' as new_adult_price,
    pricing->>'childPrice' as child_price
FROM events
WHERE ticket_price_amount IS NOT NULL
LIMIT 10;
```

---

## 📋 TASK 8: Frontend Updates (1-2 hours)

### **8.1: Update Event Display** (30 min)

**File**: `web/src/presentation/components/features/events/EventCard.tsx` (or similar)

**Show Pricing**:
```tsx
{event.isFree ? (
  <Badge variant="success">Free Event</Badge>
) : event.hasDualPricing ? (
  <div className="text-sm">
    <div>Adult (age {event.childAgeLimit}+): ${event.adultPriceAmount}</div>
    <div>Child (under {event.childAgeLimit}): ${event.childPriceAmount}</div>
  </div>
) : (
  <div className="text-sm">Ticket: ${event.adultPriceAmount}</div>
)}
```

### **8.2: Update Registration Form** (1 hour)

**File**: `web/src/presentation/components/features/events/EventRegistrationForm.tsx`

**Add Real-Time Price Calculation**:
```tsx
const calculateTotal = (attendees: Attendee[], event: EventDto) => {
  if (event.isFree) return 0;

  if (!event.hasDualPricing) {
    // Single pricing
    return attendees.length * (event.adultPriceAmount || 0);
  }

  // Dual pricing - calculate based on ages
  return attendees.reduce((total, attendee) => {
    if (attendee.age < (event.childAgeLimit || 18)) {
      return total + (event.childPriceAmount || 0);
    }
    return total + (event.adultPriceAmount || 0);
  }, 0);
};

// Display breakdown
<div className="pricing-breakdown">
  {attendees.map((attendee, idx) => (
    <div key={idx}>
      {attendee.name}, Age {attendee.age}:
      ${attendee.age < childAgeLimit ? childPrice : adultPrice}
      <Badge>{attendee.age < childAgeLimit ? 'Child' : 'Adult'}</Badge>
    </div>
  ))}
  <div className="total font-bold">Total: ${calculateTotal(attendees, event)}</div>
</div>
```

### **8.3: Stripe Integration** (30 min)

**File**: `web/src/presentation/components/features/events/EventRegistrationForm.tsx`

**Pass Calculated Total to Stripe**:
```tsx
const handlePayment = async () => {
  const total = calculateTotal(attendees, event);

  // Create Stripe checkout session
  const session = await createCheckoutSession({
    eventId: event.id,
    amount: total,
    currency: event.adultPriceCurrency,
    attendees: attendees
  });

  // Redirect to Stripe
  window.location.href = session.url;
};
```

---

## 🧪 Testing Checklist

### **Backend Tests**
- [ ] CreateEventCommand validation tests
- [ ] UpdateEventCommand validation tests
- [ ] Handler tests: Build TicketPricing correctly
- [ ] Handler tests: Single pricing (childPrice = null)
- [ ] Handler tests: Dual pricing (childPrice set)
- [ ] Mapping tests: Event → EventDto
- [ ] Integration tests: POST /api/events with dual pricing
- [ ] Integration tests: PUT /api/events/{id} with dual pricing

### **Frontend Tests**
- [ ] Event creation form: Single pricing mode
- [ ] Event creation form: Dual pricing mode
- [ ] Event creation form: Validation (child > adult rejected)
- [ ] Event display: Shows dual pricing correctly
- [ ] Registration form: Calculates total correctly
- [ ] Registration form: Real-time updates as attendees added
- [ ] Stripe integration: Passes correct total

### **End-to-End Tests**
- [ ] Create free event → Register → Success
- [ ] Create paid event (single pricing) → Register → Stripe → Success
- [ ] Create paid event (dual pricing) → Register 2 adults, 1 child → Stripe shows correct total → Success
- [ ] Edit event: Change from single to dual pricing → Displays correctly
- [ ] Legacy event: Migrated data displays correctly

---

## 📊 Success Criteria

✅ **Backend**:
- CreateEventCommand accepts all 5 dual pricing fields
- UpdateEventCommand accepts all 5 dual pricing fields
- EventDto returns all 5 dual pricing fields + HasDualPricing flag
- Handlers correctly build/update TicketPricing object
- API returns pricing data to frontend
- All tests pass (90% coverage minimum)

✅ **Frontend**:
- Event creation form sends correct payload
- Event display shows pricing correctly
- Registration form calculates total based on ages
- Real-time total updates as attendees added
- Stripe receives correct calculated total

✅ **Data Integrity**:
- Existing events migrated to new format
- Legacy fields still populated (backward compatibility)
- No events created as FREE when they should be PAID
- Price calculations match expected values

---

## 🚨 Critical Notes

### **Data Integrity Risk**
**CURRENT STATE**: Frontend sends dual pricing that backend ignores → Events created as FREE

**SOLUTION**: Complete backend implementation ASAP

### **Backward Compatibility**
- Keep legacy `TicketPriceAmount`, `TicketPriceCurrency` fields
- Populate both legacy AND new pricing fields in DTO
- Frontend checks `HasDualPricing` flag to determine display mode

### **Migration Safety**
- Test migration script on staging database first
- Verify all existing events have correct pricing after migration
- Keep legacy columns until migration confirmed successful

---

## 📚 Reference Files

### **Domain Layer** (Already Complete)
- `backend/src/Domain/Events/Event.cs` - Event entity with Pricing property
- `backend/src/Domain/Events/TicketPricing.cs` - Value object with validation
- `backend/src/Domain/Events/Money.cs` - Money value object

### **Frontend** (Already Complete)
- `web/src/presentation/components/features/events/EventCreationForm.tsx` - Form with dual pricing UI
- `web/src/presentation/lib/validators/event.schemas.ts` - Zod validation schema

### **Investigation Report**
- `docs/DUAL_PRICING_INVESTIGATION_REPORT.md` - Complete analysis with code snippets

---

## 🎯 Handover Checklist

- [ ] Review this specification document
- [ ] Understand pricing modes (single vs. dual)
- [ ] Review domain layer implementation (Event, TicketPricing)
- [ ] Review frontend implementation (EventCreationForm)
- [ ] Set up development environment
- [ ] Create feature branch: `feature/dual-pricing-backend`
- [ ] Implement tasks 1-7 (backend)
- [ ] Run all backend tests
- [ ] Implement task 8 (frontend)
- [ ] Test end-to-end flow
- [ ] Create pull request
- [ ] Update documentation

---

## ⏱️ Estimated Timeline

| Task | Estimated Time |
|------|---------------|
| 1. Update CreateEventCommand | 30 min |
| 2. Update CreateEventCommandHandler | 45 min |
| 3. Update EventDto | 20 min |
| 4. Update EventMappingProfile | 30 min |
| 5. Update UpdateEventCommand | 15 min |
| 6. Update UpdateEventCommandHandler | 30 min |
| 7. Data Migration Script | 30 min |
| **Backend Subtotal** | **3 hours** |
| 8.1 Update Event Display | 30 min |
| 8.2 Update Registration Form | 1 hour |
| 8.3 Stripe Integration | 30 min |
| **Frontend Subtotal** | **2 hours** |
| Testing & QA | 1 hour |
| **TOTAL** | **6 hours** |

---

**Questions?** Contact the session that completed the redirect fix for additional context.

**Good luck! 🚀**
