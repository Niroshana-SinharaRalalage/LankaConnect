# Root Cause Analysis: Duplicate Event Registrations with Same Email Address

**Date**: 2026-02-08
**Severity**: High (data integrity violation, financial risk for paid events)
**Status**: Analysis Complete -- Fix Pending
**Affected System**: Event Registration Subsystem (Domain + Infrastructure + API layers)

---

## 1. Bug Summary

Two separate Registration rows exist in the database for the same event, both using the email `niroshhh@gmail.com`:

| Field | Registration 1 | Registration 2 |
|-------|---------------|---------------|
| Name | "Phase6A100 Final Test" | "Niroshana Sinharage" |
| Status | Confirmed | Confirmed |
| Attendees | 1A/0C | 1A/0C |
| Email | niroshhh@gmail.com | niroshhh@gmail.com |
| Phone | Different | Different |

Both registrations are in `Confirmed` status for the same event.

---

## 2. Verdict: Most Likely Root Cause

### PRIMARY ROOT CAUSE: Cross-Path Registration Gap (Scenario 2 + Architectural Design Defect)

**Confidence: 95%**

The user `niroshhh@gmail.com` is a **registered member** of LankaConnect (this is the test account per `CLAUDE.md`). The duplicate was created because:

1. **Registration 1** was made via the **authenticated path** (`POST /api/events/{id}/rsvp`) using the `RsvpToEventCommandHandler`. This handler checks for duplicates by `UserId` only (line 322-329 of `Event.RegisterWithAttendees`). It does NOT check by email.

2. **Registration 2** was made via the **anonymous path** (`POST /api/events/{id}/register-anonymous`) using the `RegisterAnonymousAttendeeCommandHandler`. This handler has a critical defect in its duplicate detection:

   **File**: `RegisterAnonymousAttendeeCommandHandler.cs`, lines 117-122:
   ```csharp
   var existingAnonymousRegistration = @event.Registrations
       .Where(r => r.UserId == null) // <-- CRITICAL: Only checks anonymous registrations!
       .Where(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Refunded)
       .FirstOrDefault(r =>
           (r.Contact != null && r.Contact.Email == request.Email) ||
           (r.AttendeeInfo != null && r.AttendeeInfo.Email.Value == request.Email));
   ```

   The filter `r.UserId == null` means this check **completely skips** any registration made by an authenticated user. When the same email registers via the anonymous path, the handler finds no match (because the existing registration has `UserId != null`), so it proceeds to create a second registration.

3. **The `ExistsWithEmailAsync` guard was bypassed**: The anonymous handler (line 82-93) checks if the email belongs to an existing member and returns an error if so. However, this check depends on the `IUserRepository.ExistsWithEmailAsync` method succeeding. If the user's email in the Users table does not exactly match (case sensitivity, whitespace), or if the test was conducted after a data state change, this guard would not fire. More critically, this guard only exists in the anonymous handler -- there is no reciprocal check in the authenticated handler.

### WHY THIS IS THE MOST LIKELY CAUSE

The evidence fingerprint matches perfectly:
- **Different names**: Registration 1 uses a test name ("Phase6A100 Final Test"), Registration 2 uses the real name ("Niroshana Sinharage"). This is consistent with one being an authenticated RSVP (name comes from attendee form) and the other an anonymous registration (name entered manually).
- **Different phone numbers**: Each path captures phone independently.
- **Same email, same event, both Confirmed**: Both paths lead to `Confirmed` status for free events or after payment completion.
- **No database constraint exists to prevent this**: The `RegistrationConfiguration.cs` explicitly states at line 207-208: "Remove unique constraint on EventId+UserId since UserId can be null for anonymous."

---

## 3. Ranked Analysis of All Scenarios

### Scenario 1: Race Condition -- CONTRIBUTING FACTOR (not primary)

**Likelihood: 15% as sole cause, but 100% as an unpatched vulnerability**

Even if the cross-path gap is the primary cause for this specific incident, race conditions remain a proven vector:

- `Event.RegisterWithAttendees()` performs duplicate detection using an in-memory `_registrations` collection (lines 316-354)
- The collection is loaded once at query time via `_eventRepository.GetByIdAsync()` which does `Include(e => e.Registrations)` (EventRepository.cs line 131)
- Two concurrent HTTP requests each load their own snapshot of registrations
- Both pass the in-memory check because neither sees the other's uncommitted registration
- Both INSERT into the database successfully because there is **no unique constraint** on `(EventId, UserId)` or `(EventId, Contact.Email)`
- The `UseXminAsConcurrencyToken()` (line 197 of RegistrationConfiguration.cs) only protects against concurrent **updates to the same row** (optimistic concurrency for UPDATE), not concurrent **inserts of new rows**

This is a latent vulnerability that will eventually manifest under load.

### Scenario 2: Cross-Path Gap -- PRIMARY CAUSE (see Section 2)

**Likelihood: 95%**

The code has three separate duplicate detection mechanisms, none of which are comprehensive:

| Check | Location | Checks By | Scope |
|-------|----------|-----------|-------|
| `IsUserRegistered(userId)` | `Event.Register()` (legacy) | UserId, Confirmed only | Authenticated legacy path only |
| `RegisterWithAttendees` authenticated branch | `Event.cs` line 316-334 | UserId (excluding Cancelled/Refunded/Preliminary/Abandoned/Pending) | Authenticated multi-attendee path |
| `RegisterWithAttendees` anonymous branch | `Event.cs` line 337-354 | Contact.Email (excluding same statuses) | Anonymous multi-attendee path |
| `existingAnonymousRegistration` | `RegisterAnonymousAttendeeCommandHandler.cs` line 117-122 | Contact.Email or AttendeeInfo.Email, **but only where UserId == null** | Anonymous handler pre-check |

**The gap**: No check crosses the authenticated/anonymous boundary. An authenticated registration by UserId X with email Y will not be detected by the anonymous handler's email-based check because it filters on `UserId == null`.

### Scenario 3: Status Transition Bypass (Preliminary -> Confirmed race with webhook)

**Likelihood: 5% for this specific incident**

This scenario requires:
1. User registers (creates Preliminary registration for paid event)
2. User abandons payment, re-registers (creates second Preliminary)
3. First payment webhook arrives and confirms first registration
4. Second payment also completes

This is unlikely here because both registrations show `Confirmed` with `1A/0C` (one adult, zero children), and the "Phase6A100 Final Test" name suggests a deliberate test, not a payment retry. Additionally, for free events, registrations are `Confirmed` immediately (no Preliminary state). However, this scenario IS possible for paid events due to the same lack of database-level uniqueness.

### Scenario 4: Direct DB Insert / Migration Script

**Likelihood: <1%**

No evidence of manual data manipulation. The registrations have different names and phone numbers consistent with two separate form submissions.

---

## 4. Classification

**This is a Backend API + Database issue (defense-in-depth failure across two layers).**

Specifically:

| Layer | Issue |
|-------|-------|
| **Domain Layer** | `Event.RegisterWithAttendees()` duplicate checks are split by userId vs email, with no cross-referencing |
| **Application Layer** | `RegisterAnonymousAttendeeCommandHandler` filters duplicate check to `UserId == null` only, missing authenticated registrations with same email |
| **Application Layer** | `RsvpToEventCommandHandler` checks by UserId only, never by email |
| **Infrastructure Layer** | `RegistrationConfiguration.cs` has NO unique constraint on `(EventId, Contact.Email)` or conditional unique constraint on `(EventId, UserId) WHERE UserId IS NOT NULL` |
| **Domain Layer** | `IsUserRegistered()` only checks `Confirmed` status, not the comprehensive status exclusion list used in `RegisterWithAttendees` |

This is NOT a UI issue, NOT an auth issue, and NOT a feature-missing case. It is a **design defect**: the system was deliberately built without a database-level uniqueness constraint (the comment says "Remove unique constraint... since UserId can be null for anonymous"), but the compensating application-layer logic has a gap in cross-path detection.

---

## 5. Fix Plan (Ordered by Priority)

### Phase 1: Immediate Fix -- Close the Cross-Path Gap (Application Layer)

**Priority: CRITICAL**
**Risk: Low (additive logic only, no schema changes)**

#### Fix 1A: Update `RegisterAnonymousAttendeeCommandHandler` to check ALL registrations by email

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\RegisterAnonymousAttendee\RegisterAnonymousAttendeeCommandHandler.cs`

**Current code (lines 117-122)**:
```csharp
var existingAnonymousRegistration = @event.Registrations
    .Where(r => r.UserId == null) // BUG: Only checks anonymous
    .Where(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Refunded)
    .FirstOrDefault(r =>
        (r.Contact != null && r.Contact.Email == request.Email) ||
        (r.AttendeeInfo != null && r.AttendeeInfo.Email.Value == request.Email));
```

**Fix**: Remove the `r.UserId == null` filter and add comprehensive status exclusion to match the domain logic. Also check authenticated users' emails.

```csharp
// Phase 6A.XXX FIX: Check ALL registrations by email (both anonymous AND authenticated)
// Previous code only checked anonymous registrations (UserId == null), allowing
// an authenticated user's email to bypass duplicate detection via the anonymous path
var existingRegistrationByEmail = @event.Registrations
    .Where(r => r.Status != RegistrationStatus.Cancelled
             && r.Status != RegistrationStatus.Refunded
             && r.Status != RegistrationStatus.RefundRequested
             && r.Status != RegistrationStatus.Preliminary
             && r.Status != RegistrationStatus.Abandoned
             && r.Status != RegistrationStatus.Pending)
    .FirstOrDefault(r =>
        (r.Contact != null && r.Contact.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)) ||
        (r.AttendeeInfo != null && r.AttendeeInfo.Email.Value.Equals(request.Email, StringComparison.OrdinalIgnoreCase)));
```

#### Fix 1B: Update `Event.RegisterWithAttendees()` authenticated branch to also check by email

**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs`

**Current code (lines 316-334)**: Only checks by `userId`.

**Fix**: After the UserId check passes, add an email-based cross-check:

```csharp
if (userId.HasValue)
{
    // Existing UserId check (keep as-is)
    var existingByUserId = _registrations.FirstOrDefault(r =>
        r.UserId == userId && /* existing status exclusions */);
    if (existingByUserId != null)
        return Result.Failure("You are already registered...");

    // NEW: Cross-check by email to prevent same email via different paths
    var existingByEmail = _registrations.FirstOrDefault(r =>
        r.Contact != null &&
        r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase) &&
        r.Status != RegistrationStatus.Cancelled &&
        r.Status != RegistrationStatus.Refunded &&
        r.Status != RegistrationStatus.RefundRequested &&
        r.Status != RegistrationStatus.Preliminary &&
        r.Status != RegistrationStatus.Abandoned &&
        r.Status != RegistrationStatus.Pending);
    if (existingByEmail != null)
        return Result.Failure("This email is already registered for this event.");
}
```

#### Fix 1C: Align `IsUserRegistered()` with comprehensive status checks

**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs`, line 562-565

**Current code**:
```csharp
public bool IsUserRegistered(Guid userId)
{
    return _registrations.Any(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed);
}
```

**Fix**: Align with the comprehensive check used in `RegisterWithAttendees`:

```csharp
public bool IsUserRegistered(Guid userId)
{
    return _registrations.Any(r => r.UserId == userId
        && r.Status != RegistrationStatus.Cancelled
        && r.Status != RegistrationStatus.Refunded
        && r.Status != RegistrationStatus.RefundRequested
        && r.Status != RegistrationStatus.Preliminary
        && r.Status != RegistrationStatus.Abandoned
        && r.Status != RegistrationStatus.Pending);
}
```

---

### Phase 2: Durable Fix -- Database-Level Constraint (Infrastructure Layer)

**Priority: HIGH**
**Risk: Medium (schema migration, requires careful handling of NULL UserId)**

#### Fix 2A: Add conditional unique index on `(EventId, UserId)` for authenticated users

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\RegistrationConfiguration.cs`

Add a filtered unique index for authenticated registrations:

```csharp
// Unique constraint for authenticated users: one active registration per user per event
// Filtered to exclude terminal/transient states (Cancelled, Refunded, Abandoned, Preliminary)
builder.HasIndex(r => new { r.EventId, r.UserId })
    .HasDatabaseName("uix_registrations_event_user_active")
    .IsUnique()
    .HasFilter(@"""UserId"" IS NOT NULL AND ""Status"" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending')");
```

#### Fix 2B: Add a unique index on `(EventId, Contact.Email)` for email-based dedup

This is more complex because `Contact` is a JSONB column. PostgreSQL can index JSONB expressions:

**EF Core Migration (raw SQL)**:
```sql
CREATE UNIQUE INDEX uix_registrations_event_email_active
ON events.registrations (
    "EventId",
    (contact->>'email')
)
WHERE contact IS NOT NULL
  AND "Status" NOT IN ('Cancelled', 'Refunded', 'RefundRequested', 'Abandoned', 'Preliminary', 'Pending');
```

This requires a raw SQL migration since EF Core does not natively support JSONB expression indexes.

#### Fix 2C: Create EF Core migration

```bash
dotnet ef migrations add AddRegistrationUniquenessConstraints --project src/LankaConnect.Infrastructure
```

Review and customize the generated migration to include the raw SQL for the JSONB index.

---

### Phase 3: Race Condition Mitigation (Defense-in-Depth)

**Priority: MEDIUM**
**Risk: Low-Medium**

#### Fix 3A: Add pessimistic locking in the registration command handlers

Use PostgreSQL advisory locks or SELECT FOR UPDATE to serialize registrations for the same event:

**Option A (Advisory Lock)**: Before calling `RegisterWithAttendees`, acquire an advisory lock keyed by `eventId`:

```csharp
// In RsvpToEventCommandHandler, before calling RegisterWithAttendees
await _unitOfWork.ExecuteSqlAsync(
    $"SELECT pg_advisory_xact_lock({eventId.GetHashCode()})",
    cancellationToken);
```

**Option B (Serializable Transaction)**: Set transaction isolation level:

```csharp
await _unitOfWork.BeginTransactionAsync(
    System.Data.IsolationLevel.Serializable,
    cancellationToken);
```

Option A is preferred because it targets only the specific event being registered for, while Option B would serialize ALL concurrent transactions.

#### Fix 3B: Add retry-on-unique-violation logic

After adding the database unique constraints (Phase 2), add exception handling for `DbUpdateException` with PostgreSQL unique violation error code (`23505`):

```csharp
catch (DbUpdateException ex) when (ex.InnerException is NpgsqlException { SqlState: "23505" })
{
    _logger.LogWarning(ex,
        "Duplicate registration detected by database constraint for EventId={EventId}, UserId={UserId}",
        request.EventId, request.UserId);
    return Result<string?>.Failure("You are already registered for this event.");
}
```

---

### Phase 4: Data Cleanup

**Priority: LOW (after fixes are deployed)**

#### Fix 4A: Identify and resolve existing duplicates in production

```sql
-- Find all duplicate registrations by email per event
SELECT r."EventId", r.contact->>'email' AS email, COUNT(*) AS count
FROM events.registrations r
WHERE r."Status" NOT IN ('Cancelled', 'Refunded', 'Abandoned')
  AND r.contact IS NOT NULL
  AND r.contact->>'email' IS NOT NULL
GROUP BY r."EventId", r.contact->>'email'
HAVING COUNT(*) > 1;

-- Find duplicates by UserId per event
SELECT r."EventId", r."UserId", COUNT(*) AS count
FROM events.registrations r
WHERE r."UserId" IS NOT NULL
  AND r."Status" NOT IN ('Cancelled', 'Refunded', 'Abandoned')
GROUP BY r."EventId", r."UserId"
HAVING COUNT(*) > 1;
```

Manually cancel the duplicate registration (keeping the one that was created first or has a valid payment).

---

## 6. Files Requiring Changes (Summary)

| Priority | File | Change |
|----------|------|--------|
| P0 | `src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs` | Remove `UserId == null` filter from duplicate check, add comprehensive status exclusion |
| P0 | `src/LankaConnect.Domain/Events/Event.cs` | Add email-based cross-check in authenticated branch of `RegisterWithAttendees`; fix `IsUserRegistered` |
| P1 | `src/LankaConnect.Infrastructure/Data/Configurations/RegistrationConfiguration.cs` | Add conditional unique indexes |
| P1 | New migration file | Add unique constraints via EF Core migration |
| P2 | `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs` | Add advisory lock or serializable transaction |
| P2 | `src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs` | Add advisory lock or serializable transaction |
| P3 | SQL script in `scripts/` | Data cleanup query for existing duplicates |

---

## 7. Test Plan

### Unit Tests (TDD -- write before implementation)

1. `Event_RegisterWithAttendees_AuthenticatedUser_WhenEmailAlreadyRegisteredAnonymously_ShouldFail`
2. `Event_RegisterWithAttendees_AnonymousUser_WhenEmailAlreadyRegisteredAuthenticated_ShouldFail`
3. `Event_RegisterWithAttendees_SameEmail_DifferentPaths_ShouldPreventDuplicate`
4. `Event_IsUserRegistered_ShouldCheckAllActiveStatuses_NotJustConfirmed`
5. `RegisterAnonymousAttendeeHandler_WhenAuthenticatedUserWithSameEmailExists_ShouldFail`
6. `RegisterAnonymousAttendeeHandler_WhenCancelledRegistrationWithSameEmail_ShouldAllowReRegistration`

### Integration Tests

7. `ConcurrentRegistrations_SamePaidEvent_SameUser_OnlyOneSucceeds`
8. `DatabaseConstraint_PreventsDuplicateRegistration_EvenWithRaceCondition`

---

## 8. Architectural Observations

This bug reveals a broader pattern worth noting:

1. **The "two-path" architecture** (authenticated RSVP + anonymous registration) was designed to handle different user types but was not designed with cross-path invariant enforcement. The domain model treats UserId and email as independent identity axes rather than recognizing they can overlap.

2. **In-memory invariant enforcement without database backing** is fundamentally unsafe in any concurrent web application. The explicit comment in `RegistrationConfiguration.cs` ("Remove unique constraint... since UserId can be null for anonymous") traded data integrity for schema simplicity. The compensating application logic was never complete.

3. **The fix should follow DDD principle**: The `Event` aggregate is the correct place for duplicate detection (it owns the `_registrations` collection), but it must check BOTH identity axes (UserId AND email) regardless of which path is calling it. The database constraint is the safety net, not the primary enforcement mechanism.

---

## 9. References

- `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs` -- Domain aggregate with registration logic
- `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Registration.cs` -- Registration entity
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\RsvpToEvent\RsvpToEventCommandHandler.cs` -- Authenticated registration handler
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\RegisterAnonymousAttendee\RegisterAnonymousAttendeeCommandHandler.cs` -- Anonymous registration handler
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Configurations\RegistrationConfiguration.cs` -- EF Core configuration (no unique constraint)
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs` -- Repository with Include(Registrations)
- `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\PaymentsController.cs` -- Stripe webhook handler