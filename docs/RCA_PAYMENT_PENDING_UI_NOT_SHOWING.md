# RCA: Payment Pending UI Not Showing for Preliminary Registrations

**Date**: 2026-01-27
**Phase**: 6A.81 Part 4
**Status**: CONFIRMED ROOT CAUSE - FIX PLANNED

---

## Executive Summary

The Payment Pending UI was not showing for users with Preliminary registrations due to **TWO issues**:

1. **ISSUE 1 (FIXED)**: `GetByUserAsync` excluded Preliminary registrations from whitelist query - **RESOLVED in commit badaa016**
2. **ISSUE 2 (ACTIVE)**: `stripeCheckoutUrl` returns NULL because the database stores the **FULL URL** in `StripeCheckoutSessionId` field, but the retrieval code expects a **session ID** to call Stripe API

---

## Issue Classification

| Issue | Category | Severity | Status |
|-------|----------|----------|--------|
| Issue 1: my-rsvps not returning event | Backend | HIGH | FIXED |
| Issue 2: stripeCheckoutUrl is NULL | Backend Data Bug | CRITICAL | ACTIVE |

---

## Issue 1: my-rsvps Not Returning Preliminary Events (FIXED)

### Symptoms
- `/api/events/my-rsvps` did not return Christmas Dinner Dance event
- User had Preliminary registration for this event
- Event was Published status

### Root Cause
`RegistrationRepository.GetByUserAsync` excluded `RegistrationStatus.Preliminary` from whitelist query.

**File**: [RegistrationRepository.cs:131-142](../src/LankaConnect.Infrastructure/Data/Repositories/RegistrationRepository.cs#L131-L142)

### Fix Applied (commit badaa016)
```csharp
// Phase 6A.81 Part 4 FIX: Include Preliminary to show Payment Pending UI
.Where(r => r.UserId == userId &&
           (r.Status == RegistrationStatus.Preliminary ||  // ADDED
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Waitlisted ||
            r.Status == RegistrationStatus.CheckedIn ||
            r.Status == RegistrationStatus.Attended))
```

### Verification
```
API Test: /api/events/my-rsvps
Result: Christmas Dinner Dance NOW returned
```

---

## Issue 2: stripeCheckoutUrl is NULL (ACTIVE BUG)

### Symptoms
```json
{
  "status": "Preliminary",
  "paymentStatus": "Pending",
  "stripeCheckoutSessionId": "EXISTS",
  "stripeCheckoutUrl": "NULL",
  "checkoutSessionExpiresAt": "2026-01-28T14:53:34.451093Z"
}
```

Even though:
- Registration is Preliminary
- Session hasn't expired (future date)
- stripeCheckoutSessionId field has a value

...the `stripeCheckoutUrl` is still NULL.

### Root Cause Analysis

#### Step 1: Data Storage (BUG ORIGIN)

**File**: [StripePaymentService.cs:134](../src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs#L134)

```csharp
var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

// Return the checkout session URL (not ID) for frontend redirect
return Result<string>.Success(session.Url);  // <-- Returns URL, not ID!
```

The method returns `session.Url` (full checkout URL), NOT `session.Id`.

**File**: [RsvpToEventCommandHandler.cs:299](../src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs#L299)

```csharp
var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);

// Set checkout session ID on registration
var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value);  // <-- Stores URL in "SessionId" field!
```

**RESULT**: The `StripeCheckoutSessionId` database field contains:
```
https://checkout.stripe.com/c/pay/cs_test_a14pPWYa4YDAoNqeAJuPYHIYve...
```

Instead of:
```
cs_test_a14pPWYa4YDAoNqeAJuPYHIYve...
```

#### Step 2: Data Retrieval (FAILURE POINT)

**File**: [GetUserRegistrationForEventQueryHandler.cs:127-129](../src/LankaConnect.Application/Events/Queries/GetUserRegistrationForEvent/GetUserRegistrationForEventQueryHandler.cs#L127-L129)

```csharp
var checkoutUrlResult = await _stripePaymentService.GetCheckoutSessionUrlAsync(
    registration.StripeCheckoutSessionId,  // <-- Passes full URL as "sessionId"
    cancellationToken);
```

**File**: [StripePaymentService.cs:172](../src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs#L172)

```csharp
var sessionService = new SessionService(_stripeClient);
var session = await sessionService.GetAsync(sessionId, null, null, cancellationToken);  // <-- FAILS!
```

**FAILURE**: Stripe's `SessionService.GetAsync` expects a session ID like `cs_test_xxx`, NOT a full URL. The call fails, and `stripeCheckoutUrl` stays NULL.

### Data Flow Diagram

```
STORAGE FLOW (Bug Origin):
+------------------------------------------------------------------+
| Stripe API                                                       |
| session.Id = "cs_test_xxx"                                       |
| session.Url = "https://checkout.stripe.com/c/pay/cs_test_xxx"    |
+------------------------------------------------------------------+
                             |
                             v Returns session.Url (not Id!)
+------------------------------------------------------------------+
| StripePaymentService.CreateEventCheckoutSessionAsync()           |
| return Result<string>.Success(session.Url);  // LINE 134         |
+------------------------------------------------------------------+
                             |
                             v URL stored in "SessionId" field
+------------------------------------------------------------------+
| Registration.SetStripeCheckoutSession(checkoutResult.Value)      |
| StripeCheckoutSessionId = "https://checkout.stripe.com/..."      |
+------------------------------------------------------------------+
                             |
                             v Saved to database
+------------------------------------------------------------------+
| Database                                                         |
| stripe_checkout_session_id = "https://checkout.stripe.com/..."   |
+------------------------------------------------------------------+


RETRIEVAL FLOW (Failure Point):
+------------------------------------------------------------------+
| GetUserRegistrationForEventQueryHandler                          |
| registration.StripeCheckoutSessionId = "https://checkout..."     |
+------------------------------------------------------------------+
                             |
                             v Passes URL as "sessionId"
+------------------------------------------------------------------+
| StripePaymentService.GetCheckoutSessionUrlAsync(sessionId)       |
| sessionService.GetAsync("https://checkout...", ...)  // FAILS!   |
+------------------------------------------------------------------+
                             |
                             v Stripe API rejects invalid ID
+------------------------------------------------------------------+
| Result: checkoutUrlResult.IsFailure = true                       |
| stripeCheckoutUrl = NULL                                         |
+------------------------------------------------------------------+
```

---

## Fix Plan

### Recommended Fix (Quick, No Migration Required)

Modify `GetCheckoutSessionUrlAsync` to detect if the input is already a URL and return it directly:

**File**: `src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs`

```csharp
public async Task<Result<string>> GetCheckoutSessionUrlAsync(
    string sessionId,
    CancellationToken cancellationToken = default)
{
    // Phase 6A.81 Part 4 FIX: Handle legacy data where URL was stored instead of ID
    if (!string.IsNullOrWhiteSpace(sessionId) &&
        sessionId.StartsWith("https://checkout.stripe.com", StringComparison.OrdinalIgnoreCase))
    {
        _logger.LogInformation(
            "[Phase 6A.81-Part4] Detected legacy URL in StripeCheckoutSessionId - returning directly");
        return Result<string>.Success(sessionId);
    }

    // ... existing code for session ID lookup ...
}
```

### Benefits
- No database migration required
- Handles existing corrupted data gracefully
- Minimal code change
- Backwards compatible

### Alternative Fix (Cleaner, But More Work)

1. Add `StripeCheckoutUrl` field to Registration entity
2. Create migration to add column
3. Modify `CreateEventCheckoutSessionAsync` to return both ID and URL
4. Store ID in `StripeCheckoutSessionId`, URL in `StripeCheckoutUrl`
5. Backfill existing data

This is cleaner but requires more changes. Recommended for future refactoring.

---

## Testing Strategy

1. **Unit Test**: Add test for URL detection in `GetCheckoutSessionUrlAsync`
2. **API Test**: Call `/api/events/{eventId}/my-registration` and verify `stripeCheckoutUrl` is populated
3. **UI Test**: Navigate to event details page with Preliminary registration and verify "Complete Payment" button works

---

## Files to Modify

| File | Change |
|------|--------|
| `src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs` | Add URL detection in `GetCheckoutSessionUrlAsync` |

---

## Impact Assessment

- **User Impact**: HIGH - Payment Pending UI now displays but "Complete Payment" button is disabled due to NULL URL
- **Revenue Impact**: HIGH - Users cannot complete payments
- **Data Impact**: LOW - No data corruption, just misnamed field usage

---

## Conclusion

The root cause is a **data storage inconsistency** where the full checkout URL is stored in a field named `StripeCheckoutSessionId`. The quick fix is to detect this pattern and return the URL directly instead of calling Stripe API.
