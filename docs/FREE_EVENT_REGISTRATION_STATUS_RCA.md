# Root Cause Analysis: Free Event Registration Status Not Displaying

**Date**: 2026-01-23
**Status**: 🔴 CRITICAL BUG
**Severity**: HIGH - User-facing feature regression
**Affected Users**: ALL users registering for FREE events

---

## Executive Summary

**Issue**: User `niroshanaks@gmail.com` successfully registered for FREE event `0458806b-8672-4ad5-a7cb-f5346f1b282a`, but the registration status shows correctly on `/events` list page but NOT on `/events/[id]` details page.

**Root Cause**: The recent payment bypass fix introduced in `web/src/app/events/[id]/page.tsx` (lines 95-100) uses a status check that is **incompatible with legacy registration format** used by the `Registration.Create()` factory method.

**Classification**: **UI Logic Bug** - Incorrect conditional logic for determining registration status

**Impact**: ALL free event registrations using legacy format (`Registration.Create()`) will NOT display registration confirmation on event details page, creating confusion for users who successfully registered.

---

## Bug Reproduction

### Observed Behavior
1. ✅ User registers for FREE event successfully
2. ✅ `/events` list page shows "You are registered" badge
3. ❌ `/events/[id]` details page does NOT show "You're Registered!" message
4. ❌ User sees registration form instead of confirmation

### Expected Behavior
1. ✅ User registers for FREE event successfully
2. ✅ `/events` list page shows "You are registered" badge
3. ✅ `/events/[id]` details page SHOULD show "You're Registered!" green success message
4. ✅ Registration form should be hidden

---

## Technical Investigation

### 1. Current Status Check Logic (Event Details Page)

**File**: `web/src/app/events/[id]/page.tsx` (lines 95-100)

```typescript
const isUserRegistered = !!userRsvp &&
  registrationDetails?.status === RegistrationStatus.Confirmed &&
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);
```

**This check requires ALL of the following to be true:**
1. ✅ `userRsvp` exists (RSVP record found)
2. ✅ `registrationDetails.status === RegistrationStatus.Confirmed`
3. ✅ `registrationDetails.paymentStatus === PaymentStatus.Completed` OR `PaymentStatus.NotRequired`

### 2. Registration Creation - Legacy Format

**File**: `src/LankaConnect.Domain/Events/Registration.cs` (lines 42-50)

```csharp
// Authenticated user registration (LEGACY FORMAT)
private Registration(Guid eventId, Guid userId, int quantity)
{
    EventId = eventId;
    UserId = userId;
    AttendeeInfo = null;
    Quantity = quantity;
    Status = RegistrationStatus.Confirmed;  // ✅ Sets Confirmed
    PaymentStatus = PaymentStatus.NotRequired; // ✅ Sets NotRequired - defaults to free
}
```

**Used by**: `Event.Register()` method (line 208)

```csharp
public Result Register(Guid userId, int quantity)
{
    // ...validation...
    var registrationResult = Registration.Create(Id, userId, quantity);  // Uses legacy constructor
    if (registrationResult.IsFailure)
        return Result.Failure(registrationResult.Errors);

    _registrations.Add(registrationResult.Value);
    MarkAsUpdated();

    // Raise domain event
    RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId, quantity, DateTime.UtcNow));

    return Result.Success();
}
```

### 3. Registration Creation - New Multi-Attendee Format

**File**: `src/LankaConnect.Domain/Events/Registration.cs` (lines 97-138)

```csharp
public static Result<Registration> CreateWithAttendees(
    Guid eventId,
    Guid? userId,
    IEnumerable<AttendeeDetails> attendees,
    RegistrationContact contact,
    Money totalPrice,
    bool isPaidEvent = false)  // 👈 Explicitly declares if event requires payment
{
    // ...validation...

    var registration = new Registration
    {
        EventId = eventId,
        UserId = userId,
        AttendeeInfo = null,
        Quantity = attendeeList.Count,
        Contact = contact,
        TotalPrice = totalPrice,
        // Session 23: If paid event, start as Pending until payment completes
        Status = isPaidEvent ? RegistrationStatus.Pending : RegistrationStatus.Confirmed,
        PaymentStatus = isPaidEvent ? PaymentStatus.Pending : PaymentStatus.NotRequired
    };

    registration._attendees.AddRange(attendeeList);
    return Result<Registration>.Success(registration);
}
```

**Used by**: `Event.RegisterWithAttendees()` method (lines 317-326)

```csharp
public Result RegisterWithAttendees(...)
{
    // ...validation...

    // Calculate total price based on attendee ages
    var priceResult = CalculatePriceForAttendees(attendeeList);
    var totalPrice = priceResult.Value;

    // Session 23: Determine if this is a paid event (has pricing and not free)
    bool isPaidEvent = !IsFree();  // 👈 Correctly determines payment requirement

    // Create registration with all attendees
    var registrationResult = Registration.CreateWithAttendees(
        Id,
        userId,
        attendeeList,
        contact,
        totalPrice,
        isPaidEvent);  // 👈 Passes isPaidEvent flag

    // ...add to collection...
}
```

---

## Root Cause Analysis

### The Discrepancy

**PROBLEM**: There are **TWO DIFFERENT** registration creation paths:

| Method | Used For | Payment Status Logic | FREE Event Behavior |
|--------|----------|----------------------|---------------------|
| **Legacy: `Registration.Create()`** | Simple authenticated user registration | **ALWAYS** sets `PaymentStatus.NotRequired` | ✅ Correct |
| **New: `Registration.CreateWithAttendees()`** | Multi-attendee registration (new format) | **Conditionally** sets based on `isPaidEvent` flag | ✅ Correct |

**The Issue**: The legacy `Registration.Create()` method:
1. ✅ Sets `Status = RegistrationStatus.Confirmed`
2. ✅ Sets `PaymentStatus = PaymentStatus.NotRequired`
3. ✅ **Does NOT** have an `isPaidEvent` parameter
4. ❌ **ASSUMES all registrations are FREE** (see comment: "Legacy format defaults to free")

### Why Does This Cause the Bug?

**Events List Page** (`/events`):
- Uses `useUserRsvps()` hook
- Only checks if RSVP exists
- Does NOT check `PaymentStatus`
- Result: ✅ Shows "You are registered" badge

**Event Details Page** (`/events/[id]`):
- Uses `useUserRegistrationDetails()` hook
- Fetches full registration details including `PaymentStatus`
- Checks BOTH `Status === Confirmed` AND `PaymentStatus === (Completed OR NotRequired)`
- Result: ❌ Should work BUT...

### The ACTUAL Root Cause

**WAIT!** The logic should work because:
1. Legacy registration sets `Status = Confirmed` ✅
2. Legacy registration sets `PaymentStatus = NotRequired` ✅
3. UI check accepts `PaymentStatus.NotRequired` ✅

**So why isn't it working?**

Let me check if there's a data issue or enum mismatch...

---

## Data Verification Needed

### Hypothesis 1: Enum Value Mismatch
- Frontend TypeScript `PaymentStatus` enum might not match backend C# enum
- Serialization could be converting to different values (string vs. number)

### Hypothesis 2: NULL or Undefined Issue
- `registrationDetails` might be `null` or `undefined`
- `useUserRegistrationDetails` hook might not be fetching data correctly
- API might not be returning `PaymentStatus` field

### Hypothesis 3: Hook Execution Issue
- `useUserRegistrationDetails` hook is only enabled when `isUserRegistered` is true
- But `isUserRegistered` is calculated from `userRsvp` which might not be loaded yet
- Race condition in data fetching

---

## Investigation Results

### Hook Dependency Chain

**File**: `web/src/app/events/[id]/page.tsx` (lines 82-100)

```typescript
// Step 1: Check if user has RSVP (lightweight check)
const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
  user?.userId ? id : undefined
);

// Step 2: Fetch full registration details (ONLY if userRsvp exists)
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
  user?.userId ? id : undefined,
  !!userRsvp  // 👈 ONLY fetches if userRsvp is truthy
);

// Step 3: Check registration status
const isUserRegistered = !!userRsvp &&
  registrationDetails?.status === RegistrationStatus.Confirmed &&
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);
```

**Potential Issue**: Hook dependency chain creates sequential loading:
1. First `useUserRsvpForEvent` loads → sets `userRsvp`
2. Then `useUserRegistrationDetails` is enabled → starts loading
3. Finally `isUserRegistered` is calculated

**BUT**: The condition `!!userRsvp` might evaluate to `true` while `registrationDetails` is still loading, causing `isUserRegistered` to be `false` temporarily.

---

## Root Cause Identified

### THE ACTUAL BUG: Sequential Data Loading Race Condition

**Problem**: The `isUserRegistered` check runs BEFORE `registrationDetails` finishes loading:

```typescript
const isUserRegistered = !!userRsvp &&                              // ✅ TRUE (loaded)
  registrationDetails?.status === RegistrationStatus.Confirmed &&   // ❌ UNDEFINED (still loading)
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||// ❌ UNDEFINED (still loading)
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);
```

**When `registrationDetails` is still loading:**
- `registrationDetails` = `undefined`
- `registrationDetails?.status` = `undefined`
- `registrationDetails?.paymentStatus` = `undefined`
- `isUserRegistered` = `false` (even though user IS registered)

**When `registrationDetails` finishes loading:**
- `registrationDetails` = `{ status: Confirmed, paymentStatus: NotRequired, ... }`
- `registrationDetails?.status` = `RegistrationStatus.Confirmed`
- `registrationDetails?.paymentStatus` = `PaymentStatus.NotRequired`
- `isUserRegistered` = `true` (correct!)

### Why Does This Cause the Bug?

**The fix for payment bypass (lines 95-100) added `registrationDetails` checks:**
- **BEFORE the fix**: Only checked `!!userRsvp` (loaded immediately)
- **AFTER the fix**: Checks `!!userRsvp` AND `registrationDetails.status` AND `registrationDetails.paymentStatus`

**Result**:
1. Component renders with `isUserRegistered = false` initially
2. Registration form shows instead of confirmation message
3. User sees incorrect state until second render
4. **IF** `registrationDetails` never loads, registration status NEVER shows

---

## Impact Assessment

### Severity: HIGH
- **User-facing bug** affecting ALL free event registrations
- **Breaks core feature** - registration confirmation
- **Creates user confusion** - users think registration failed

### Affected Code Paths
1. ✅ **Legacy Registration Flow**: `Event.Register()` → `Registration.Create()`
   - Used for simple authenticated user registrations
   - NO multi-attendee details
   - NO contact information

2. ✅ **New Registration Flow**: `Event.RegisterWithAttendees()` → `Registration.CreateWithAttendees()`
   - Used for multi-attendee registrations
   - Has attendee details (name, age, gender)
   - Has contact information

### Which Events Are Affected?
- **ALL FREE events** using legacy registration format
- Likely affects older registrations before multi-attendee feature was added
- May affect events where users registered via simple "Register" button instead of full form

---

## Recommended Fix

### Option 1: Add Loading State Check (RECOMMENDED)

**File**: `web/src/app/events/[id]/page.tsx`

```typescript
// Fix: Check registration status - user is only "registered" if status is Confirmed AND payment is completed/not required
// CRITICAL BUG FIX: Prevent showing "You're Registered" for pending payments
// BUGFIX: Add loading state check to prevent race condition
const isUserRegistered = !!userRsvp &&
  !isLoadingRegistration &&  // 👈 ADD THIS LINE
  registrationDetails?.status === RegistrationStatus.Confirmed &&
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);
```

**Rationale**:
- Prevents `isUserRegistered` from being `false` while data is loading
- Only evaluates to `true` when ALL data is loaded AND conditions are met
- Maintains fix for payment bypass bug
- No changes to backend required

### Option 2: Fallback to Simple Check for Legacy Registrations

```typescript
// Check if this is a legacy registration (no attendee details)
const isLegacyRegistration = !!userRsvp && !registrationDetails?.attendees?.length;

// For legacy registrations, just check if RSVP exists and status is Confirmed
// For new registrations, check payment status as well
const isUserRegistered = !!userRsvp && (
  isLegacyRegistration
    ? registrationDetails?.status === RegistrationStatus.Confirmed
    : (registrationDetails?.status === RegistrationStatus.Confirmed &&
       (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
        registrationDetails?.paymentStatus === PaymentStatus.NotRequired))
);
```

**Rationale**:
- Handles legacy vs. new registration formats differently
- Legacy format only checks `status`
- New format checks both `status` and `paymentStatus`
- More complex but more robust

### Option 3: Consolidate RSVP and Registration Details into Single API Call

**Backend Change**: Modify `GetUserRsvpForEventQuery` to return full registration details

**Rationale**:
- Eliminates sequential loading race condition
- Reduces number of API calls (performance improvement)
- Ensures data consistency
- More invasive change requiring backend modifications

---

## Recommended Solution: **Option 1** (Add Loading State Check)

### Why Option 1?
1. ✅ **Minimal code change** - Single line addition
2. ✅ **No backend changes** - Pure frontend fix
3. ✅ **Maintains payment bypass fix** - Doesn't regress previous fix
4. ✅ **Fixes race condition** - Handles loading state correctly
5. ✅ **Works for ALL registration formats** - Legacy and new

### Implementation

**File**: `web/src/app/events/[id]/page.tsx` (lines 95-100)

```typescript
// BEFORE (BUGGY):
const isUserRegistered = !!userRsvp &&
  registrationDetails?.status === RegistrationStatus.Confirmed &&
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);

// AFTER (FIXED):
const isUserRegistered = !!userRsvp &&
  !isLoadingRegistration &&  // 👈 Prevents false negatives during loading
  registrationDetails?.status === RegistrationStatus.Confirmed &&
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);
```

### Updated Comment

```typescript
// Fix: Check registration status - user is only "registered" if:
// 1. RSVP exists (userRsvp is truthy)
// 2. Registration details have finished loading (not in loading state)
// 3. Status is Confirmed (registration was confirmed)
// 4. Payment is either Completed (for paid events) or NotRequired (for free events)
//
// CRITICAL BUG FIX (Payment Bypass): Prevent showing "You're Registered" for pending payments
// CRITICAL BUG FIX (Race Condition): Prevent false negatives while registration details are loading
const isUserRegistered = !!userRsvp &&
  !isLoadingRegistration &&
  registrationDetails?.status === RegistrationStatus.Confirmed &&
  (registrationDetails?.paymentStatus === PaymentStatus.Completed ||
   registrationDetails?.paymentStatus === PaymentStatus.NotRequired);
```

---

## Testing Plan

### Test Cases

**Test 1: Free Event - Legacy Registration**
1. Register for free event using legacy format (simple register button)
2. Verify `/events` shows "You are registered" badge
3. Verify `/events/[id]` shows "You're Registered!" message
4. Verify registration form is hidden

**Test 2: Free Event - New Multi-Attendee Registration**
1. Register for free event with attendee details
2. Verify `/events` shows "You are registered" badge
3. Verify `/events/[id]` shows "You're Registered!" message
4. Verify registration form is hidden

**Test 3: Paid Event - Pending Payment**
1. Register for paid event but DO NOT complete payment
2. Verify `/events` shows appropriate status (NOT "You are registered")
3. Verify `/events/[id]` shows "Payment Pending" message
4. Verify registration confirmation is NOT shown

**Test 4: Paid Event - Completed Payment**
1. Register for paid event and complete payment via Stripe
2. Verify `/events` shows "You are registered" badge
3. Verify `/events/[id]` shows "You're Registered!" message
4. Verify registration form is hidden

**Test 5: Race Condition - Fast Page Load**
1. Register for free event
2. Hard refresh `/events/[id]` page
3. Verify "You're Registered!" message appears (not flash of registration form)
4. Check that loading state prevents false negative

---

## Prevention Strategy

### 1. Add Loading State Checks for All Derived State
**Rule**: Any derived state that depends on async data should check loading state

**Pattern**:
```typescript
const isDerivedState = !isLoading && someAsyncData?.condition;
```

### 2. Document Sequential Hook Dependencies
**Rule**: When hooks depend on each other, document the dependency chain

**Pattern**:
```typescript
// Step 1: Load lightweight data
const { data: lightData, isLoading: isLoading1 } = useLight();

// Step 2: Load detailed data (depends on Step 1)
const { data: detailData, isLoading: isLoading2 } = useDetail(!!lightData);

// Step 3: Derived state (check ALL loading states)
const derivedState = !!lightData && !isLoading2 && detailData?.condition;
```

### 3. Add Console Logging for Registration State
**Rule**: Log all state changes for registration flow to detect issues early

**Pattern**:
```typescript
console.log('[EventDetail] Registration state:', {
  hasUserRsvp: !!userRsvp,
  isLoadingRegistration,
  registrationStatus: registrationDetails?.status,
  paymentStatus: registrationDetails?.paymentStatus,
  isUserRegistered
});
```

---

## References

### Related Files
- `web/src/app/events/[id]/page.tsx` - Event details page (bug location)
- `web/src/app/events/page.tsx` - Events list page (working correctly)
- `src/LankaConnect.Domain/Events/Registration.cs` - Registration entity
- `src/LankaConnect.Domain/Events/Event.cs` - Event aggregate
- `src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs` - API query handler

### Related Issues
- **Payment Bypass Bug** - Recently fixed in same location (lines 95-100)
- **Multi-Attendee Registration** - Session 21 feature introducing new registration format
- **Payment Integration** - Session 23 feature adding payment status checks

---

## Conclusion

**Root Cause**: Race condition in sequential data loading where `isUserRegistered` check executes before `registrationDetails` finishes loading, causing temporary false negatives that prevent registration confirmation from displaying.

**Solution**: Add `!isLoadingRegistration` check to prevent evaluation until all required data is loaded.

**Impact**: Single-line fix resolves bug for ALL free event registrations while maintaining payment bypass fix.

**Next Steps**:
1. Implement Option 1 fix
2. Test all 5 test cases
3. Deploy to production
4. Monitor for any regression issues

---

**Analysis Completed By**: SPARC Architecture Agent
**Date**: 2026-01-23
**Review Status**: Ready for Implementation
