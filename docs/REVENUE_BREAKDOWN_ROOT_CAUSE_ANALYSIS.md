# Revenue Breakdown Feature - Root Cause Analysis

**Date:** 2026-01-15
**Phase:** 6A.X Revenue Breakdown Implementation
**Status:** INCOMPLETE - Multiple gaps identified

---

## Executive Summary

The revenue breakdown implementation is **partially complete** but **inconsistent** across the application. While the backend domain model and calculation service are properly implemented, there are significant gaps in:
1. How tax is calculated and displayed in the Attendee Management tab
2. How per-registration revenue breakdown is stored vs displayed
3. Missing tax data in export files (CSV/Excel)
4. Inconsistent calculation methods between Event creation preview and Attendee Management display

---

## Problem Statement

1. **Sales tax is missing** from the revenue breakdown display in AttendeeManagementTab - even though state tax rates ARE in the database (state_tax_rates table)
2. **Revenue calculations only updated in one location** - Event creation form shows new breakdown, but:
   - Attendee Management tab shows "5% platform fee" for legacy events
   - Attendee table shows "Net Amount" without proper breakdown
   - Exported CSV/Excel has old calculations
   - Total payout summary uses approximated calculations

---

## Data Flow Analysis

### Current Flow (Event Creation to Attendee Display)

```
EVENT CREATION FLOW:
====================
1. CreateEventCommandHandler (Line 232)
   |
   v
2. RevenueCalculatorService.CalculateBreakdownAsync()
   |
   v
3. DatabaseSalesTaxService.GetStateTaxRateAsync() --> state_tax_rates table
   |
   v
4. Event.SetRevenueBreakdown() --> Stored as JSONB in events.revenue_breakdown
   |
   v
5. Saved to Database (events table)


ATTENDEE MANAGEMENT FLOW:
=========================
1. GetEventAttendeesQueryHandler
   |
   v
2. Loads Event entity WITH RevenueBreakdown (via _eventRepository.GetByIdAsync)
   |
   v
3. Queries Registrations table (NOT individual breakdown per registration)
   |
   v
4. Calculates totals based on Event.RevenueBreakdown (approximation)
   |
   |  <<< ROOT CAUSE: This calculation is an APPROXIMATION >>>
   |
   v
5. Returns EventAttendeesResponse with hasRevenueBreakdown = true/false
   |
   v
6. AttendeeManagementTab.tsx displays breakdown conditionally
```

---

## Root Cause Analysis

### ROOT CAUSE #1: Tax Data Stored at WRONG Level

**Problem:** Revenue breakdown is stored ONLY at the **Event level**, not at the **Registration level**.

**Evidence from RegistrationConfiguration.cs (Lines 106-159):**
```csharp
// Phase 6A.X: Configure revenue breakdown Money value objects
builder.OwnsOne(r => r.SalesTaxAmount, money => {...});
builder.OwnsOne(r => r.StripeFeeAmount, money => {...});
builder.OwnsOne(r => r.PlatformCommissionAmount, money => {...});
builder.OwnsOne(r => r.OrganizerPayoutAmount, money => {...});
builder.Property(r => r.SalesTaxRate).HasColumnName("sales_tax_rate")...;
```

The schema supports per-registration breakdown, BUT:

**Evidence from Registration.cs (Line 305):**
```csharp
public void SetRevenueBreakdown(ValueObjects.RevenueBreakdown breakdown)
{
    if (breakdown == null)
        return;  // Free events don't have breakdown

    SalesTaxAmount = breakdown.SalesTaxAmount;
    StripeFeeAmount = breakdown.StripeFeeAmount;
    PlatformCommissionAmount = breakdown.PlatformCommission;
    OrganizerPayoutAmount = breakdown.OrganizerPayout;
    SalesTaxRate = breakdown.SalesTaxRate;
    MarkAsUpdated();
}
```

**BUT `SetRevenueBreakdown` is NEVER called on registrations!**

Searching for calls to `Registration.SetRevenueBreakdown`:
- `CreateEventCommandHandler.cs` - Calls `Event.SetRevenueBreakdown()` only
- `RsvpToEventCommandHandler.cs` - Does NOT call `Registration.SetRevenueBreakdown()`
- `RegisterAnonymousAttendeeCommandHandler.cs` - Does NOT call `Registration.SetRevenueBreakdown()`

**Impact:** Individual registration tax/fee breakdown is always NULL in the database.

---

### ROOT CAUSE #2: Attendee Management Uses Approximation

**Evidence from GetEventAttendeesQueryHandler.cs (Lines 117-155):**
```csharp
// Phase 6A.X: Calculate detailed breakdown totals from event's RevenueBreakdown
// If event has RevenueBreakdown, use it to calculate per-registration totals
bool hasRevenueBreakdown = @event.RevenueBreakdown != null;

if (hasRevenueBreakdown && @event.RevenueBreakdown != null)
{
    // Calculate totals based on event's breakdown multiplied by registrations
    // Each registration pays the same breakdown structure
    var breakdown = @event.RevenueBreakdown;
    var totalRegistrationCount = attendeeDtos.Sum(a => a.TotalAttendees);

    // For single pricing (no dual/group), multiply breakdown by total attendees
    // For dual/group pricing, this is an approximation (actual breakdown per registration may vary)
    if (totalRegistrationCount > 0 && breakdown.GrossAmount.Amount > 0)
    {
        var expectedGross = breakdown.GrossAmount.Amount * totalRegistrationCount;
        var actualGross = grossRevenue;
        var ratio = expectedGross > 0 ? actualGross / expectedGross : 0m;

        // Scale breakdown components by actual revenue
        totalSalesTax = breakdown.SalesTaxAmount.Amount * totalRegistrationCount * ratio;
        // ... etc
    }
}
```

**Problem:** This approximation is INCORRECT for:
1. **Dual pricing events** (Adult/Child different prices) - approximation assumes uniform pricing
2. **Group tiered pricing** - approximation uses first tier price only
3. **Events with mixed attendee ages** - actual tax varies by price paid

---

### ROOT CAUSE #3: Per-Registration Net Amount Uses WRONG Rate

**Evidence from GetEventAttendeesQueryHandler.cs (Lines 90-92):**
```csharp
// Phase 6A.71: Calculate NET amount per registration (organizer's payout after 5% commission)
// For free events or null amounts, NetAmount will be null
NetAmount = r.TotalPrice != null
    ? r.TotalPrice.Amount * (1 - _commissionSettings.EventTicketCommissionRate)
    : null,
```

**Problem:**
- Uses `EventTicketCommissionRate` (5% combined fee - OLD calculation)
- Does NOT use actual breakdown formula (Tax + Stripe 2.9%+$0.30 + Platform 2%)
- Does NOT account for state sales tax at all

**Configuration Reference (CommissionSettings):**
```
EventTicketCommissionRate = 0.05 (5% - legacy combined fee)
PlatformCommissionRate = 0.02 (2% - new platform fee)
StripeFeeRate = 0.029 (2.9% - Stripe fee)
StripeFeeFixed = 0.30 ($0.30 - Stripe per-transaction)
```

---

### ROOT CAUSE #4: Export Services Use Incorrect Data

**Evidence from ExcelExportService.cs (Lines 376-385):**
```csharp
// Phase 6A.71: Show NET amount (after commission) instead of GROSS
if (attendee.NetAmount.HasValue)
{
    sheet.Cell(row, col).Value = attendee.NetAmount.Value;
}
```

**Evidence from CsvExportService.cs (Lines 96-98):**
```csharp
// Phase 6A.71: Show NET amount (after commission) instead of GROSS
csv.WriteField(a.NetAmount?.ToString("F2") ?? "");
```

**Problem:** Both export services use `attendee.NetAmount` which is calculated using the incorrect 5% commission rate (see ROOT CAUSE #3).

---

## Complete File List - Files That Reference Revenue/Commission/Fee

### Backend Files (Must Be Updated)

| File | Location | Issue |
|------|----------|-------|
| `GetEventAttendeesQueryHandler.cs` | `src/LankaConnect.Application/Events/Queries/GetEventAttendees/` | Uses 5% commission, approximates tax totals |
| `EventAttendeesResponse.cs` | `src/LankaConnect.Application/Events/Common/` | Has correct properties, but receives wrong values |
| `RsvpToEventCommandHandler.cs` | `src/LankaConnect.Application/Events/Commands/RsvpToEvent/` | Missing: SetRevenueBreakdown on Registration |
| `RegisterAnonymousAttendeeCommandHandler.cs` | `src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/` | Missing: SetRevenueBreakdown on Registration |
| `ExcelExportService.cs` | `src/LankaConnect.Infrastructure/Services/Export/` | Uses incorrect NetAmount |
| `CsvExportService.cs` | `src/LankaConnect.Infrastructure/Services/Export/` | Uses incorrect NetAmount |
| `RevenueCalculatorService.cs` | `src/LankaConnect.Infrastructure/Services/` | Correct - no changes needed |
| `DatabaseSalesTaxService.cs` | `src/LankaConnect.Infrastructure/Services/` | Correct - no changes needed |

### Frontend Files (Display Issues)

| File | Location | Issue |
|------|----------|-------|
| `AttendeeManagementTab.tsx` | `web/src/presentation/components/features/events/` | Displays hasRevenueBreakdown-based UI, but data is wrong |
| `RevenueBreakdownPreview.tsx` | `web/src/presentation/components/features/events/` | Correct - used in Event creation |
| `revenue-calculator.ts` | `web/src/presentation/lib/utils/` | Correct - frontend preview calculation |
| `events.types.ts` | `web/src/infrastructure/api/types/` | Correct - has all DTO types |

### Domain/Infrastructure Files (Correct but Not Utilized)

| File | Location | Status |
|------|----------|--------|
| `RevenueBreakdown.cs` | `src/LankaConnect.Domain/Events/ValueObjects/` | CORRECT |
| `Registration.cs` | `src/LankaConnect.Domain/Events/` | Has SetRevenueBreakdown but NEVER CALLED |
| `RegistrationConfiguration.cs` | `src/LankaConnect.Infrastructure/Data/Configurations/` | Schema CORRECT - columns exist |
| `StateTaxRate.cs` | `src/LankaConnect.Domain/Tax/` | CORRECT |
| `state_tax_rates table` | Database | CORRECT - has tax rates |

---

## Gap Analysis Matrix

| Location | Expected Behavior | Actual Behavior | Gap |
|----------|------------------|-----------------|-----|
| **Event Creation Form** | Show tax breakdown preview | WORKING | None |
| **Event DTO Response** | Return RevenueBreakdown | WORKING | None |
| **Registration Entity** | Store per-registration breakdown | Columns exist but NULL | SetRevenueBreakdown never called |
| **GetEventAttendees Query** | Accurate tax/fee totals | Approximated values | Uses ratio-based approximation |
| **Per-Registration NetAmount** | Accurate per-attendee payout | 5% deduction only | Wrong formula used |
| **AttendeeManagementTab Summary** | Show actual tax breakdown | Shows approximated values | Data source incorrect |
| **Per-Row Net Amount** | Show accurate payout per registration | 5% deduction only | Wrong formula |
| **CSV Export** | Export actual breakdown | Exports 5% calculation | Wrong source data |
| **Excel Export** | Export actual breakdown | Exports 5% calculation | Wrong source data |

---

## Prioritized Fix Plan

### Priority 1: Critical - Fix Per-Registration Revenue Storage

**Files to modify:**
1. `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs`
2. `src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs`

**Changes:**
- After creating Registration, call `Registration.SetRevenueBreakdown()` with calculated breakdown
- Calculate breakdown based on registration's actual price (not event's base price)

### Priority 2: High - Fix GetEventAttendees Query

**Files to modify:**
1. `src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs`

**Changes:**
- Read per-registration breakdown from Registration entity (once Priority 1 is complete)
- Calculate NetAmount using actual stored breakdown, not 5% approximation
- Sum actual tax/fees from registrations instead of approximating from event breakdown

### Priority 3: Medium - Update Export Services

**Files to modify:**
1. `src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs`
2. `src/LankaConnect.Infrastructure/Services/Export/CsvExportService.cs`

**Changes:**
- Export will automatically use correct data once Priority 1 & 2 are complete
- Consider adding dedicated columns for: SalesTax, StripeFee, PlatformCommission, OrganizerPayout

### Priority 4: Low - Frontend Enhancement

**Files to modify:**
1. `web/src/presentation/components/features/events/AttendeeManagementTab.tsx`

**Changes:**
- Once backend is fixed, frontend will automatically display correct values
- Consider adding per-registration breakdown in expanded row view

---

## Migration Strategy for Existing Data

**Problem:** Existing registrations have NULL breakdown columns.

**Options:**
1. **Backfill Script** - Calculate and update breakdown for existing registrations based on Event's stored breakdown
2. **Lazy Calculation** - If registration breakdown is NULL, calculate on-the-fly in GetEventAttendees query
3. **Accept Legacy** - Continue using approximation for old registrations, use accurate data for new ones

**Recommendation:** Option 3 (Accept Legacy) with Option 1 (Backfill) for high-value events.

---

## Testing Checklist

- [ ] Create paid event with US location (has tax)
- [ ] Verify Event.RevenueBreakdown is populated correctly
- [ ] Register for event and verify Registration.SalesTaxAmount is populated
- [ ] View AttendeeManagementTab and verify:
  - [ ] Tax shows non-zero value
  - [ ] Stripe fee shows correct calculation
  - [ ] Platform commission shows 2%
  - [ ] Organizer payout = GrossRevenue - Tax - Stripe - Platform
- [ ] Export CSV and verify Net Amount is accurate
- [ ] Export Excel and verify Net Amount is accurate
- [ ] Test with dual pricing (adult/child)
- [ ] Test with group tiered pricing
- [ ] Test with non-US location (no tax)

---

## Appendix: Key Code Snippets

### Correct Revenue Breakdown Formula (RevenueBreakdown.cs)
```csharp
// 1. Gross Amount (GA) = ticket price (what buyer pays)
// 2. Sales Tax (ST) = GA - (GA / (1 + TaxRate))
// 3. Taxable Amount (TA) = GA - ST
// 4. Stripe Fee (SF) = (TA x StripeFeeRate) + StripeFeeFixed
// 5. Platform Commission (PC) = TA x PlatformCommissionRate
// 6. Organizer Payout (OP) = TA - SF - PC
```

### Current Incorrect NetAmount Calculation (GetEventAttendeesQueryHandler.cs)
```csharp
NetAmount = r.TotalPrice != null
    ? r.TotalPrice.Amount * (1 - _commissionSettings.EventTicketCommissionRate) // 5%
    : null,
```

### Correct NetAmount Calculation (Should Be)
```csharp
NetAmount = r.OrganizerPayoutAmount?.Amount ?? (r.TotalPrice != null
    ? CalculateFallbackNetAmount(r.TotalPrice.Amount)
    : null),
```

---

## Document Version

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-15 | RCA Agent | Initial comprehensive analysis |
