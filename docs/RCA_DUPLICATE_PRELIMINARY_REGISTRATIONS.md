# Root Cause Analysis: Duplicate Preliminary Registrations and Missing Payment Pending UI

**Date:** 2026-01-27
**Analyst:** System Architect Agent
**Phase:** 6A.81 Part 4 Follow-up
**Severity:** HIGH - Causes duplicate registrations and poor user experience

---

## 1. Executive Summary

Users who register for a paid event, are redirected to Stripe checkout, but abandon the checkout (close browser/navigate away without completing payment) are able to re-register for the same event. This creates duplicate `Preliminary` registrations in the database. Additionally, when users return to the event page, the "Payment Pending" UI with countdown timer is NOT displayed, instead showing the registration form as if they never registered.

### Evidence from Database
User `5e782b4d-29ed-4e1d-9039-6c8f698aeea9` has 3 duplicate Preliminary registrations for the same event:
```
Registration 1: 0329c683-fa2f-4882-87ad-6a9ef6a38ac2 (2026-01-26 16:27:00)
Registration 2: be360dcc-e24b-4eb3-84f8-a353d890a622 (2026-01-27 04:51:40)
Registration 3: b5ecbb4a-ca5b-4b2c-9848-e9b1386e3505 (2026-01-27 04:52:23)
```
Event ID: `d543629f-a5ba-4475-b124-3d0fc5200f2f` (Christmas Dinner Dance 2025)

---

## 2. Issue Categorization

| Category | Applicable | Explanation |
|----------|------------|-------------|
| UI Issue | **YES** | Payment Pending UI not showing due to data fetching chain issue |
| Auth Issue | No | Authentication working correctly |
| Backend API Issue | **PARTIAL** | Backend has fix but there's a gap in the flow |
| Database Issue | No | Database storing data correctly |
| Missing Feature | **PARTIAL** | Gap between GetByUserAsync and frontend detection |

---

## 3. Data Flow Analysis

### 3.1 Expected Flow (Happy Path)
```
1. User visits event page
2. User fills registration form, clicks Submit
3. Backend creates Registration with Status=Preliminary
4. Stripe checkout URL returned to frontend
5. User redirected to Stripe checkout
6. (Scenario A) User completes payment -> Webhook updates status to Confirmed
7. (Scenario B) User abandons checkout -> Returns to event page -> Sees "Payment Pending" UI
```

### 3.2 Actual Flow (Bug Path)
```
1. User visits event page
2. User fills registration form, clicks Submit
3. Backend creates Registration with Status=Preliminary (CORRECT)
4. Stripe checkout URL returned to frontend (CORRECT)
5. User redirected to Stripe checkout
6. User abandons checkout, returns to event page
7. useUserRsvpForEvent hook returns undefined (INCORRECT - see Root Cause)
8. Since userRsvp is undefined, registrationDetails not fetched
9. isPaymentPending is false (due to no registrationDetails)
10. Registration form shown instead of Payment Pending UI
11. User registers again -> Creates DUPLICATE Preliminary registration
```

---

## 4. Root Cause Analysis

### 4.1 PRIMARY ROOT CAUSE: `GetByUserAsync` Excludes Preliminary Registrations

**File:** `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\RegistrationRepository.cs`
**Lines:** 131-141

```csharp
// Phase 6A.81: CRITICAL - Only include CONFIRMED registrations (exclude Preliminary/Abandoned)
// Use whitelist approach to ensure we only get registrations that should be displayed
var result = await _dbSet
    .AsNoTracking()
    .Where(r => r.UserId == userId &&
               (r.Status == RegistrationStatus.Confirmed ||
                r.Status == RegistrationStatus.Waitlisted ||
                r.Status == RegistrationStatus.CheckedIn ||
                r.Status == RegistrationStatus.Attended))
    .OrderByDescending(r => r.CreatedAt)
    .ToListAsync(cancellationToken);
```

**Impact:** This method is used by:
1. `GetUserRsvpsQueryHandler` -> `GetMyRegisteredEventsQuery`
2. `GetMyRegisteredEventsQueryHandler`

These handlers are called by the `/api/events/my-rsvps` endpoint, which the frontend's `useUserRsvpForEvent` hook relies on.

**Result:** When `GetByUserAsync` excludes Preliminary registrations, the frontend's `userRsvp` is undefined, breaking the entire registration detection chain.

### 4.2 SECONDARY ROOT CAUSE: Frontend Detection Chain Dependency

**File:** `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx`
**Lines:** 89-119

```typescript
// useUserRsvpForEvent relies on /api/events/my-rsvps
const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
  user?.userId ? id : undefined
);

// registrationDetails ONLY fetches when userRsvp exists
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
  user?.userId ? id : undefined,
  !!userRsvp  // <-- This is false when Preliminary registrations excluded!
);

// isPaymentPending depends on registrationDetails existing
const isPaymentPending = !!userRsvp &&
  !isLoadingRegistration &&
  registrationDetails?.status === 'Preliminary';  // Never true when userRsvp is undefined
```

**Chain of Dependencies:**
1. `useUserRsvpForEvent` -> calls `/api/events/my-rsvps` -> returns empty for Preliminary
2. `userRsvp` = undefined
3. `useUserRegistrationDetails` -> `enabled: !!userRsvp` = false -> NOT fetched
4. `registrationDetails` = undefined
5. `isPaymentPending` = false
6. Registration form shown instead of Payment Pending UI

### 4.3 EXISTING FIX (Phase 6A.81 Part 4) - Backend Duplicate Prevention

**File:** `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\RsvpToEvent\RsvpToEventCommandHandler.cs`
**Lines:** 160-198

```csharp
// Phase 6A.81 Part 4 FIX: Check for existing Preliminary registration to prevent duplicates
// If user already has a Preliminary registration (payment not completed), reuse it
var existingPreliminary = @event.Registrations.FirstOrDefault(r =>
    r.UserId == request.UserId &&
    r.Status == Domain.Events.Enums.RegistrationStatus.Preliminary);

if (existingPreliminary != null && !@event.IsFree())
{
    _logger.LogInformation(
        "Found existing Preliminary registration - RegistrationId={RegistrationId}, UserId={UserId}, EventId={EventId}. Retrieving existing checkout URL.",
        existingPreliminary.Id, request.UserId, @event.Id);

    // Retrieve existing Stripe checkout URL (Phase 6A.81 Part 2)
    // ... returns existing checkout URL instead of creating new registration
}
```

**Issue:** This fix works when loading the event through `_eventRepository.GetByIdAsync()` which includes ALL registrations. However, the FRONTEND never reaches this code because it shows the registration form (thinking user isn't registered).

### 4.4 WHY DUPLICATES STILL OCCUR

The backend fix only prevents duplicates IF the user submits the registration form again. But the UI shows the registration form as if the user never registered, so they naturally try to register again.

The fix reuses the existing checkout session when found, but this only works AFTER the user clicks "Register" again. The proper UX should PREVENT the user from seeing the registration form in the first place.

---

## 5. Component-by-Component Analysis

### 5.1 Backend Components

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| RsvpToEventCommandHandler | `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs` | **PARTIAL FIX** | Has duplicate prevention, but UI doesn't trigger it |
| Event.RegisterWithAttendees | `src/LankaConnect.Domain/Events/Event.cs:269-378` | **CORRECT** | Correctly excludes Preliminary from duplicate check |
| GetUserRegistrationForEventQueryHandler | `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs` | **CORRECT** | Includes Preliminary status |
| RegistrationRepository.GetByUserAsync | `src/LankaConnect.Infrastructure/Data/Repositories/RegistrationRepository.cs:119-166` | **BUG** | Excludes Preliminary registrations |
| GetMyRegisteredEventsQueryHandler | `src/LankaConnect.Application/Events/Queries/GetMyRegisteredEvents/GetMyRegisteredEventsQueryHandler.cs` | **AFFECTED** | Uses GetByUserAsync |

### 5.2 Frontend Components

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| EventDetailsPage | `web/src/app/events/[id]/page.tsx` | **AFFECTED** | isPaymentPending always false |
| useUserRsvpForEvent | `web/src/presentation/hooks/useEvents.ts:548-572` | **AFFECTED** | Returns undefined for Preliminary |
| useUserRegistrationDetails | `web/src/presentation/hooks/useEvents.ts:598-650` | **CORRECT** | Would work if enabled |
| CheckoutCountdownTimer | `web/src/presentation/components/features/events/CheckoutCountdownTimer.tsx` | **CORRECT** | Would work if data available |
| RegistrationDetailsDto | `web/src/infrastructure/api/types/events.types.ts:654-684` | **CORRECT** | Has all required fields |

---

## 6. Detailed Fix Plan

### 6.1 FIX 1: Add Preliminary to GetByUserAsync (REQUIRED)

**File:** `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\RegistrationRepository.cs`
**Lines:** 131-141
**Action:** Add `RegistrationStatus.Preliminary` to the whitelist

**Before:**
```csharp
.Where(r => r.UserId == userId &&
           (r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Waitlisted ||
            r.Status == RegistrationStatus.CheckedIn ||
            r.Status == RegistrationStatus.Attended))
```

**After:**
```csharp
.Where(r => r.UserId == userId &&
           (r.Status == RegistrationStatus.Preliminary ||  // Phase 6A.81 Fix: Include for Payment Pending UI
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Waitlisted ||
            r.Status == RegistrationStatus.CheckedIn ||
            r.Status == RegistrationStatus.Attended))
```

**Rationale:** Including Preliminary allows:
1. Frontend's `useUserRsvpForEvent` to detect existing registration
2. `useUserRegistrationDetails` to be enabled
3. Payment Pending UI to show correctly
4. Countdown timer to display

### 6.2 FIX 2: Alternative Frontend-Only Fix (OPTIONAL - Defense in Depth)

**File:** `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx`
**Lines:** 98-100

**Change:** Always fetch registration details for logged-in users (don't depend on userRsvp)

**Before:**
```typescript
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
  user?.userId ? id : undefined,
  !!userRsvp  // Only fetch when userRsvp exists
);
```

**After:**
```typescript
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
  user?.userId ? id : undefined,
  !!user?.userId  // Always fetch for logged-in users
);
```

**Note:** This is a defense-in-depth fix. The backend fix (6.1) is the primary solution.

### 6.3 FIX 3: Update isPaymentPending Logic (OPTIONAL - Defense in Depth)

**File:** `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx`
**Lines:** 117-119

**Before:**
```typescript
const isPaymentPending = !!userRsvp &&
  !isLoadingRegistration &&
  registrationDetails?.status === 'Preliminary';
```

**After:**
```typescript
// Phase 6A.81 Fix: Check registrationDetails directly, don't depend on userRsvp
const isPaymentPending = !isLoadingRegistration &&
  registrationDetails?.status === 'Preliminary';
```

---

## 7. Impact Analysis

### 7.1 Components Affected by Fix

| Component | Impact | Risk |
|-----------|--------|------|
| My RSVPs Page | Will now show Preliminary registrations | LOW - May need UI treatment |
| Dashboard | Preliminary registrations visible | LOW - May need badge/indicator |
| Event Card | Could show "Payment Pending" badge | LOW - Enhancement opportunity |

### 7.2 Similar Patterns Elsewhere

| Location | Pattern | Needs Update |
|----------|---------|--------------|
| GetByUserAsync (RegistrationRepository) | Whitelist filter | **YES** |
| GetUserRegistrationForEventQueryHandler | Includes Preliminary | No - Already correct |
| RsvpToEventCommandHandler | Duplicate check | No - Already correct |

### 7.3 Test Cases Required

1. **Test Preliminary Registration Detection**
   - User registers for paid event
   - User abandons Stripe checkout
   - User returns to event page
   - **Expected:** Payment Pending UI shown with countdown timer

2. **Test Duplicate Prevention UI**
   - User with existing Preliminary registration visits event page
   - **Expected:** Cannot see registration form, only Payment Pending UI

3. **Test My RSVPs Page**
   - User has Preliminary registration
   - User visits My RSVPs page
   - **Expected:** Event shown with "Payment Pending" indicator

4. **Test Countdown Timer**
   - User with Preliminary registration visits event page
   - **Expected:** Countdown timer shows time until checkout expires

5. **Test Checkout Session Retrieval**
   - User clicks "Complete Payment" on Payment Pending UI
   - **Expected:** Redirected to valid Stripe checkout URL

---

## 8. Recommended Fix Order

1. **CRITICAL (Fix 1):** Update `GetByUserAsync` to include Preliminary - This is the root cause
2. **MEDIUM (Fix 2):** Update frontend to fetch registrationDetails for all logged-in users
3. **LOW (Fix 3):** Update isPaymentPending to not depend on userRsvp

---

## 9. Summary

The root cause is a **filtering mismatch** between:
- `GetByUserAsync` (excludes Preliminary)
- `GetUserRegistrationForEventQueryHandler` (includes Preliminary)

This creates a scenario where:
1. Backend correctly stores Preliminary registrations
2. Backend correctly returns Preliminary data via `/api/events/{id}/my-registration`
3. But Frontend never calls that endpoint because `useUserRsvpForEvent` returns undefined

The fix is straightforward: add `RegistrationStatus.Preliminary` to the whitelist in `GetByUserAsync`. This aligns the "My RSVPs" endpoint with the registration detection endpoint, ensuring the frontend data chain works correctly.

---

## 10. Appendix: File Locations

| Description | Absolute Path |
|-------------|---------------|
| Registration Repository | `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\RegistrationRepository.cs` |
| RsvpToEventCommandHandler | `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\RsvpToEvent\RsvpToEventCommandHandler.cs` |
| Event Domain (RegisterWithAttendees) | `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs` |
| GetUserRegistrationForEvent Handler | `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetUserRegistrationForEvent\GetUserRegistrationForEventQueryHandler.cs` |
| EventDetailsPage | `c:\Work\LankaConnect\web\src\app\events\[id]\page.tsx` |
| useEvents Hooks | `c:\Work\LankaConnect\web\src\presentation\hooks\useEvents.ts` |
| Events Types | `c:\Work\LankaConnect\web\src\infrastructure\api\types\events.types.ts` |
| CheckoutCountdownTimer | `c:\Work\LankaConnect\web\src\presentation\components\features\events\CheckoutCountdownTimer.tsx` |
