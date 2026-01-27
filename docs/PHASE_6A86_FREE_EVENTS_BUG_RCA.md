# Phase 6A.86: Free Events Showing as "Paid Events" - Root Cause Analysis

**Date**: January 26, 2026
**Severity**: CRITICAL - User-facing bug
**Status**: Identified - Fix In Progress

---

## Executive Summary

Free events in production are incorrectly displaying "Paid Event" labels instead of "Free Event" labels. This is causing user confusion and potential loss of event registrations.

**Root Cause**: Phase 6A.81 security fix (commit `36c6a56c`) changed the `Event.IsFree()` method to return `false` when pricing is not configured, breaking existing free events that had NULL pricing fields.

---

## Issue Description

### User Report
User screenshot shows several events with "Paid Event" labels that should be free events (circled in red).

### Observed Behavior
- Events that are truly free showing "Paid Event" label
- Affects events created before Phase 6A.81 deployment
- Frontend UI displaying incorrect pricing information

### Expected Behavior
- Free events should display "Free Event" label
- Only events with configured ticket prices should show "Paid Event" or price amount

---

## Root Cause Analysis

### 1. Code Change Investigation

#### Commit History
```bash
git log --all --oneline --grep="IsFree"
```

Key commits:
- **36c6a56c** (Jan 24, 2026): "fix(phase-6a81): Prevent payment bypass for events with missing pricing configuration"
- **2bce0b63**: "test(phase-6a81): Fix 15 failing tests due to IsFree() security fix"

#### Code Comparison

**BEFORE Phase 6A.81** (Legacy Behavior):
```csharp
// File: src/LankaConnect.Domain/Events/Event.cs
public bool IsFree()
{
    // Phase 6D: Group tiered pricing
    if (Pricing != null && Pricing.Type == PricingType.GroupTiered)
        return !Pricing.HasGroupTiers;

    // New dual pricing system
    if (Pricing != null)
        return Pricing.AdultPrice.IsZero;

    // Legacy single pricing
    return TicketPrice == null || TicketPrice.IsZero;  // ✅ NULL = FREE
}
```

**AFTER Phase 6A.81** (Security Fix):
```csharp
// File: src/LankaConnect.Domain/Events/Event.cs:723-743
public bool IsFree()
{
    // Phase 6D: Group tiered pricing
    if (Pricing != null && Pricing.Type == PricingType.GroupTiered)
        return !Pricing.HasGroupTiers;

    // New dual pricing system
    if (Pricing != null)
        return Pricing.AdultPrice.IsZero;

    // Legacy single pricing
    if (TicketPrice != null)
        return TicketPrice.IsZero;

    // Phase 6A.81 Security Fix: If both Pricing and TicketPrice are null, default to FALSE (paid)
    // This prevents payment bypass vulnerability for events with misconfigured/missing pricing
    // Rationale: CalculatePriceForAttendees() will fail with "Event pricing is not configured" error,
    // preventing registration creation, which is safer than allowing free bypass
    // Events with truly free pricing MUST explicitly set TicketPrice or Pricing to $0
    return false;  // ❌ NULL = PAID (security-first)
}
```

### 2. Impact Analysis

#### Affected Events
Events with ALL of the following characteristics are affected:
1. `Pricing` field is NULL
2. `TicketPrice` field is NULL
3. Were intended to be free events
4. Created before Phase 6A.81 deployment

#### Frontend Display Logic
```typescript
// File: web/src/app/events/page.tsx:533-548
{event.isFree
  ? 'Free Event'  // ✅ Shows when isFree = true
  : event.hasGroupPricing && event.groupPricingTiers && event.groupPricingTiers.length > 0
    ? /* price range */
    : event.hasDualPricing
      ? /* adult/child prices */
      : event.ticketPriceAmount != null
        ? /* single price */
        : 'Paid Event'}  // ❌ SHOWS THIS when isFree = false but no price configured
```

When `isFree()` returns `false` (due to NULL pricing) but no actual price amount is set, the UI falls through to "Paid Event" as the default label.

### 3. Why Phase 6A.81 Made This Change

**Security Vulnerability**: Events with misconfigured pricing (both `Pricing` and `TicketPrice` as NULL) were being treated as free, allowing users to bypass payment.

**Example**:
- Christmas Dinner Dance event ($100 adult/$50 child) had NULL pricing due to migration issue
- Original `IsFree()` returned TRUE → Users could register for free
- Stripe checkout was skipped → Payment bypass vulnerability

**The Fix**: Make misconfigured events fail-fast (return FALSE) so `CalculatePriceForAttendees()` throws an error instead of allowing free bypass.

**Unintended Consequence**: Broke existing truly-free events that had NULL pricing.

---

## Solution

### Option 1: Data Migration (RECOMMENDED)

Backfill all existing free events with explicit $0 pricing:

```sql
-- Phase 6A.86: Fix free events broken by Phase 6A.81 security fix
UPDATE events."Events"
SET
    "TicketPrice_Amount" = 0.00,
    "TicketPrice_Currency" = 1  -- USD
WHERE
    "TicketPrice_Amount" IS NULL
    AND "Pricing_AdultPrice_Amount" IS NULL
    AND "DeletedAt" IS NULL
    AND /* Add additional filters to identify truly-free events */;
```

**Pros**:
- Keeps Phase 6A.81 security fix intact
- Explicit pricing makes future behavior predictable
- No code changes required

**Cons**:
- Requires careful identification of truly-free vs misconfigured events
- One-time data migration complexity

### Option 2: Code Fix with Backwards Compatibility Flag

Add an explicit `IsFreeEvent` boolean flag to Event entity:

```csharp
public bool IsFreeEvent { get; private set; }  // Explicit free event flag

public bool IsFree()
{
    if (IsFreeEvent) return true;  // Explicit flag takes precedence

    // Rest of existing logic...
}
```

**Pros**:
- Explicit intent - no ambiguity
- Supports future event types

**Cons**:
- Requires database schema change
- Migration complexity
- More code to maintain

### Option 3: Hybrid - UI Fallback Logic

Update frontend to handle NULL pricing explicitly:

```typescript
{event.isFree || (!event.ticketPriceAmount && !event.adultPriceAmount && !event.hasGroupPricing)
  ? 'Free Event'
  : /* ... other cases ... */}
```

**Pros**:
- Quick UI-only fix
- No database migration needed

**Cons**:
- Doesn't fix backend behavior
- Frontend becomes source of truth (anti-pattern)
- Doesn't prevent registration issues

---

## Recommended Solution: Option 1 (Data Migration)

### Implementation Plan

1. **Identify Truly-Free Events**
   - Query database for events with NULL pricing
   - Cross-reference with event organizer intent
   - Filter out misconfigured paid events

2. **Create Migration Script**
   ```sql
   -- Phase 6A.86: Backfill explicit $0 pricing for free events
   UPDATE events."Events"
   SET
       "TicketPrice_Amount" = 0.00,
       "TicketPrice_Currency" = 1,  -- USD
       "UpdatedAt" = NOW()
   WHERE
       "TicketPrice_Amount" IS NULL
       AND "Pricing_AdultPrice_Amount" IS NULL
       AND "DeletedAt" IS NULL
       -- Add event-specific filters here
   ;
   ```

3. **Verify Fix**
   - Check events page in staging
   - Verify "Free Event" labels appear correctly
   - Test registration flow for free events

4. **Document for Future**
   - Update event creation docs to require explicit $0 pricing
   - Add validation in CreateEventCommand to prevent NULL pricing
   - Update UI to show error if pricing is not configured

---

## Testing Strategy

### 1. Unit Tests
```csharp
[Fact]
public void IsFree_WhenExplicitZeroPricing_ReturnsTrue()
{
    // Test explicit $0 pricing
}

[Fact]
public void IsFree_WhenNullPricing_ReturnsFalse_SecurityFix()
{
    // Verify Phase 6A.81 security fix remains intact
}
```

### 2. Integration Tests
- Create test event with $0 pricing
- Verify API returns `isFree: true`
- Verify frontend shows "Free Event" label

### 3. Staging Verification
- Check existing free events display correctly
- Test event creation with $0 pricing
- Verify registration flow

---

## Prevention Measures

### 1. Event Creation Validation
Add validation to prevent NULL pricing:

```csharp
// In CreateEventCommandValidator
RuleFor(x => x)
    .Must(HaveValidPricing)
    .WithMessage("Event must have explicit pricing configuration. Use $0 for free events.");
```

### 2. Database Constraints
Consider adding CHECK constraint:

```sql
ALTER TABLE events."Events"
ADD CONSTRAINT chk_pricing_configured
CHECK (
    "TicketPrice_Amount" IS NOT NULL
    OR "Pricing_AdultPrice_Amount" IS NOT NULL
);
```

### 3. Documentation Updates
- [docs/UI_STYLE_GUIDE.md](./UI_STYLE_GUIDE.md): Update pricing display rules
- [docs/API_GUIDELINES.md](./API_GUIDELINES.md): Document pricing configuration requirements
- Event creation wizard: Add tooltip explaining $0 for free events

---

## Timeline

1. **Immediate** (Today): Document root cause ✅
2. **Next** (Within 2 hours): Create and test data migration script
3. **Deployment** (Same day): Apply fix to staging → verify → production
4. **Follow-up** (Next sprint): Add validation to prevent recurrence

---

## Related Issues

- Phase 6A.81: Payment bypass security vulnerability fix
- Phase 6D: Group tiered pricing implementation
- Session 21: Dual pricing feature

---

## Sign-Off

**Analyzed By**: Claude Sonnet 4.5
**Reviewed By**: [Pending]
**Approved For Fix**: [Pending]

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
