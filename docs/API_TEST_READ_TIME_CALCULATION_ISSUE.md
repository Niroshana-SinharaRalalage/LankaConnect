# API Test Results - READ-Time Calculation Not Working

**Date**: 2026-01-16
**Test Event**: Christmas Dinner Dance 2025 (d543629f-a5ba-4475-b124-3d0fc5200f2f)
**Deployment**: #833 (SUCCESS) - Commit 2ae133cb

---

## ❌ CRITICAL ISSUE FOUND

The READ-time calculation fix deployed successfully, but **IT'S NOT WORKING** for OLD events.

### API Response Analysis

**Endpoint**: `GET /api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/Attendees`

**Response Summary**:
```json
{
  "hasRevenueBreakdown": false,  ← WRONG! Should be TRUE
  "totalSalesTax": 0,  ← Should have calculated value
  "totalStripeFees": 0,  ← Should have calculated value
  "totalPlatformCommission": 18.75,  ← Legacy 5% calculation
  "totalOrganizerPayout": 356.25,  ← Legacy calculation
  "averageTaxRate": 0  ← Should have calculated value
}
```

**Per-Registration Breakdown** (ALL 8 registrations):
```json
{
  "salesTaxAmount": null,  ← Should be calculated!
  "stripeFeeAmount": null,  ← Should be calculated!
  "platformCommissionAmount": null,  ← Should be calculated!
  "organizerPayoutAmount": null,  ← Should be calculated!
  "salesTaxRate": 0.0  ← Should be calculated!
}
```

**Sample Registration with Payment**:
```json
{
  "registrationId": "ee925721-0494-4278-b60f-b5ae949548dc",
  "totalAmount": 50.0,  ← HAS PRICE (should trigger calculation)
  "netAmount": 47.5,  ← Legacy 5% calculation
  "salesTaxAmount": null,  ← NOT CALCULATED
  "stripeFeeAmount": null,  ← NOT CALCULATED
  "platformCommissionAmount": null,  ← NOT CALCULATED
  "organizerPayoutAmount": null,  ← NOT CALCULATED
  "salesTaxRate": 0.0  ← NOT CALCULATED
}
```

---

## Root Cause Analysis (Hypothesis)

### Event Details
- Event Location: "4314 Clark Ave, Cleveland, Ohio 44109, United States"
- Event State: "Ohio"
- Event has pricing: Adult $50, Child $25
- 8 old registrations (created 2025-12-23, BEFORE fix)

### On-the-Fly Calculation Logic (Lines 114-168 in GetEventAttendeesQueryHandler.cs)

**Calculation Trigger Conditions**:
```csharp
if (!attendeeDto.SalesTaxAmount.HasValue &&  ← TRUE (null)
    attendeeDto.TotalAmount.HasValue &&     ← TRUE ($50)
    attendeeDto.TotalAmount.Value > 0 &&    ← TRUE ($50 > 0)
    @event.Location != null)                 ← ??? Check this!
```

### Possible Causes

**1. Event.Location is NULL** (Most Likely)
- GetEventAttendeesQueryHandler uses `_eventRepository.GetByIdAsync()` at line 44
- This may NOT include the Location navigation property
- **Need to check if Location is loaded with .Include()**

**2. RevenueCalculatorService Failing Silently**
- Tax rate lookup for "Ohio" might be failing
- Check if state tax rates seeded correctly
- Exception caught and logged as warning (lines 162-165)

**3. Event Details Query Shows Location**
- `GET /api/Events/{id}` shows location fields
- But this might use different query with explicit Location loading
- Attendees query might not load Location

**4. Logger Not Injected Properly**
- Logs would show "Calculated revenue breakdown on-the-fly for X old registrations" (line 172)
- No logs = calculation never executed OR logger not working

---

## Investigation Steps

### Step 1: Check Azure Logs ⏳

Look for these log messages:
- ✅ "Calculated revenue breakdown on-the-fly for {Count} old registrations in event {EventId}"
- ⚠️ "Failed to create Money for registration {RegistrationId}: {Error}"
- ⚠️ "Revenue breakdown calculation failed for registration {RegistrationId}: {Error}"
- ❌ "Exception while calculating on-the-fly revenue breakdown for registration {RegistrationId}"

**If NO logs**: Calculation loop never executed → Event.Location is NULL

### Step 2: Check GetEventAttendeesQueryHandler Line 44

**Current Code**:
```csharp
var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
```

**Expected**: This should load Location with the Event
**Reality**: Might not include Location navigation property

**Fix Needed**:
```csharp
// Option A: Update repository to include Location
var @event = await _eventRepository.GetByIdWithLocationAsync(request.EventId, cancellationToken);

// Option B: Explicit Include in query (if using DbContext)
var @event = await _context.Events
    .Include(e => e.Location)
    .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken);
```

### Step 3: Verify State Tax Rates Seeded

Query database to check if "Ohio" tax rate exists:
```sql
SELECT * FROM state_tax_rates WHERE state_code = 'OH' OR state_name = 'Ohio';
```

Expected: Row with tax rate (e.g., 0.0575 for 5.75%)

### Step 4: Check IEventRepository Implementation

Look for GetByIdAsync method:
- Does it include Location?
- Does it use .Include() for navigation properties?

---

## Expected vs Actual Behavior

### EXPECTED (After READ-time fix):
```json
{
  "hasRevenueBreakdown": true,  ← Calculated on-the-fly
  "totalSalesTax": 20.14,  ← Sum of all registrations
  "totalStripeFees": 11.16,  ← Sum of all registrations
  "totalPlatformCommission": 6.64,  ← Sum of all registrations (2%)
  "totalOrganizerPayout": 337.06,  ← Sum of all registrations
  "averageTaxRate": 0.0575,  ← Ohio state tax
  "attendees": [
    {
      "totalAmount": 50.0,
      "salesTaxAmount": 2.69,  ← CALCULATED
      "stripeFeeAmount": 1.67,  ← CALCULATED
      "platformCommissionAmount": 0.89,  ← CALCULATED
      "organizerPayoutAmount": 44.75,  ← CALCULATED
      "salesTaxRate": 0.0575  ← CALCULATED
    }
  ]
}
```

### ACTUAL (Current broken state):
```json
{
  "hasRevenueBreakdown": false,  ← NOT calculated
  "totalSalesTax": 0,  ← DEFAULT
  "totalStripeFees": 0,  ← DEFAULT
  "totalPlatformCommission": 18.75,  ← Legacy 5%
  "totalOrganizerPayout": 356.25,  ← Legacy calculation
  "averageTaxRate": 0,  ← DEFAULT
  "attendees": [
    {
      "totalAmount": 50.0,
      "salesTaxAmount": null,  ← NOT calculated
      "stripeFeeAmount": null,  ← NOT calculated
      "platformCommissionAmount": null,  ← NOT calculated
      "organizerPayoutAmount": null,  ← NOT calculated
      "salesTaxRate": 0.0  ← DEFAULT
    }
  ]
}
```

---

## Next Actions

1. ✅ Check Azure logs for calculation messages
2. ✅ Investigate EventRepository.GetByIdAsync() - does it load Location?
3. ✅ Add explicit `.Include(e => e.Location)` if needed
4. ✅ Verify state tax rates seeded for "Ohio"
5. ✅ Redeploy with fix
6. ✅ Retest API endpoint

---

## Code References

- [GetEventAttendeesQueryHandler.cs:44](src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L44) - Event loading
- [GetEventAttendeesQueryHandler.cs:114-168](src/LankaConnect.Application/Events/Queries/GetEventAttendees/GetEventAttendeesQueryHandler.cs#L114-L168) - On-the-fly calculation loop
- [IEventRepository.cs](src/LankaConnect.Domain/Events/IEventRepository.cs) - Repository interface
- [EventRepository.cs](src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs) - Repository implementation

---

## Status

**Current**: ❌ READ-time calculation NOT working - investigating why
**Deployment**: ✅ Code deployed successfully (#833)
**Issue**: Calculation logic present but not executing (likely Event.Location is NULL)
**Next**: Check Azure logs and EventRepository implementation
