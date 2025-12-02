# DUAL PRICING BACKEND SUPPORT INVESTIGATION REPORT

## Executive Summary
**Status: PARTIALLY IMPLEMENTED - CRITICAL GAPS IDENTIFIED**

The backend has the domain model and database infrastructure in place to support dual pricing (adult/child), but the API endpoints and command handlers DO NOT expose these fields. The frontend cannot currently set dual pricing through the API.

---

## 1. BACKEND DTO ANALYSIS

### CreateEventCommand & UpdateEventCommand
**File:** `/src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommand.cs`
**File:** `/src/LankaConnect.Application/Events/Commands/UpdateEvent/UpdateEventCommand.cs`

**CRITICAL ISSUE:** Commands only support single pricing:
```
// Current fields (LEGACY - single pricing only)
decimal? TicketPriceAmount = null,
Currency? TicketPriceCurrency = null
```

**Missing Fields:**
- `adultPriceAmount` (decimal)
- `adultPriceCurrency` (Currency enum)
- `childPriceAmount` (decimal, nullable)
- `childPriceCurrency` (Currency enum, nullable)
- `childAgeLimit` (int, nullable, range 1-18)

**Result:** Frontend cannot send dual pricing data to backend during event creation/updates.

---

## 2. DATABASE ENTITY - Event Aggregate

**File:** `/src/LankaConnect.Domain/Events/Event.cs`

Lines 32-33 show FULL dual pricing support:
```
public Money? TicketPrice { get; private set; } // Legacy - single price
public TicketPricing? Pricing { get; private set; } // Session 21: Dual pricing
```

**Supporting Methods:**
- `SetDualPricing(TicketPricing pricing)` - Line 539
- `CalculatePriceForAttendees()` - Line 563
- `IsFree()` - Line 524 (supports both systems)
- `RegisterWithAttendees()` - Line 211 (uses age-based pricing)

---

## 3. TicketPricing VALUE OBJECT

**File:** `/src/LankaConnect.Domain/Events/ValueObjects/TicketPricing.cs`

FULLY IMPLEMENTED with validations:
- AdultPrice (required)
- ChildPrice (optional)
- ChildAgeLimit (optional, 1-18)
- HasChildPricing property
- IsChildAge() method
- CalculateForAttendee() method

Validations:
- Adult price required
- Child price and age limit must both be provided or both null
- Child age limit: 1-18 years
- Adult and child prices must use same currency
- Child price cannot exceed adult price

---

## 4. DATABASE SCHEMA

**File:** `/src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs`

FULLY CONFIGURED (Lines 81-101):
- pricing JSONB column configured
- Nested Money objects for adult and child prices
- ChildAgeLimit property mapped

**Status:** Migrations applied - snapshot shows pricing column configured

---

## 5. API ENDPOINT EXPOSURE - CRITICAL GAPS

### EventDto
**File:** `/src/LankaConnect.Application/Events/Common/EventDto.cs`

ONLY exposes legacy single pricing:
- TicketPriceAmount
- TicketPriceCurrency
- IsFree

**MISSING:**
- adultPriceAmount
- adultPriceCurrency
- childPriceAmount
- childPriceCurrency
- childAgeLimit
- hasChildPricing

### EventMappingProfile
**File:** `/src/LankaConnect.Application/Common/Mappings/EventMappingProfile.cs`

INCOMPLETE - only maps legacy TicketPrice, NOT the new Pricing property.

---

## 6. COMMAND HANDLERS - INCOMPLETE

### CreateEventCommandHandler (Lines 90-99)
Only handles legacy pricing. Missing:
- TicketPricing object creation
- Validation of dual pricing parameters
- Call to event.SetDualPricing()

### UpdateEventCommandHandler (Lines 92-101)
Same issues as CreateEventCommandHandler

---

## CRITICAL GAPS SUMMARY

| Component | Status | Issue |
|-----------|--------|-------|
| Domain Entity | Complete | Full support |
| TicketPricing Value Object | Complete | Full implementation |
| Database Schema | Applied | JSONB exists |
| EF Core Configuration | Complete | Mapping correct |
| DTOs | MISSING | Only legacy fields |
| CreateEventCommand | INCOMPLETE | Missing dual pricing params |
| UpdateEventCommand | INCOMPLETE | Missing dual pricing params |
| CreateEventCommandHandler | INCOMPLETE | No TicketPricing creation |
| UpdateEventCommandHandler | INCOMPLETE | No TicketPricing update |
| EventMappingProfile | INCOMPLETE | Missing Pricing mappings |

---

## WHAT NEEDS IMPLEMENTATION

1. Update CreateEventCommand - Add dual pricing fields
2. Update UpdateEventCommand - Add dual pricing fields
3. Update CreateEventCommandHandler - Create TicketPricing object
4. Update UpdateEventCommandHandler - Update TicketPricing object
5. Update EventDto - Add dual pricing properties
6. Update EventMappingProfile - Map Pricing to DTO fields
7. Update frontend to send/receive dual pricing
8. Add comprehensive validation tests

---

## DATA INTEGRITY RISKS

If frontend tries to set dual pricing now:
- API rejects unknown parameters
- Legacy pricing fields ignored
- Database receives NULL for pricing column
- Event defaults to free (silent data loss)

---

## RECOMMENDATIONS

### Phase 6A.X: Complete Dual Pricing API Support (HIGH PRIORITY)

**Backend Effort:** 2-3 hours
- Straightforward field additions
- Mapping updates
- Validation logic

**Frontend Effort:** 3-4 hours
- UI form updates
- API integration

**Testing:** 2-3 hours
- Validation tests
- Integration tests

