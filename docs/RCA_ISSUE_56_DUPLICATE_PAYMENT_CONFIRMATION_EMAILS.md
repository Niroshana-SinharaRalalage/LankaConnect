# Root Cause Analysis: Issue #56 - Duplicate Payment Confirmation Emails

## Issue Summary

**Symptom**: Users receive TWO payment confirmation emails after completing Stripe payment for paid events.

**Evidence from User**:
- Two emails received at nearly identical times (22 min ago vs 21 min ago, both at 11:43 PM)
- Event: "Test Group Pricing - Corrected Fix Verification"
- Both emails have "Event Registration Confirmed!" header
- One email shows "Date: {{EventDateTime}}" - template placeholder NOT being replaced

**Previous Fix Attempts**:
1. **Fix 1 (Domain Layer)**: Added PaymentIntentId idempotency guard in `Registration.CompletePayment()` - returns success without raising event if same payment intent was already processed
2. **Fix 2 (Infrastructure Layer)**: Added PostgreSQL xmin concurrency token via `UseXminAsConcurrencyToken()` - should cause `DbUpdateConcurrencyException` on concurrent saves

**Result**: Neither fix prevented the duplicate emails.

---

## Architecture Deep Dive

### The Complete Flow from Stripe Webhook to Email

```
1. Stripe fires checkout.session.completed webhook
   |
2. PaymentsController.Webhook() receives request
   |
3. IsEventProcessedAsync(stripeEvent.Id) check
   - Query: WHERE EventId = 'evt_xxx' AND Processed = true
   - If true: Return 200 OK (skip processing)
   |
4. EventExistsAsync(stripeEvent.Id) check
   - If false: RecordEventAsync() - INSERT new row with Processed=false
   - If true: Log "reprocessing" and continue
   |
5. HandleCheckoutSessionCompletedAsync()
   |
   5a. Load Registration via _registrationRepository.GetByIdAsync()
       - Entity is now TRACKED by EF Core Change Tracker
       - Status = Preliminary, PaymentStatus = Pending
   |
   5b. registration.CompletePayment(paymentIntentId)
       - Idempotency check: If StripePaymentIntentId == paymentIntentId, return Success (NO event raised)
       - Status check: If Status != Preliminary, return Failure
       - State transition: Preliminary -> Confirmed, Pending -> Completed
       - RAISES PaymentCompletedEvent domain event
   |
   5c. _registrationRepository.Update(registration)
       - Marks entity as Modified in Change Tracker
   |
   5d. await _unitOfWork.CommitAsync()
       |
       5d-i.   DetectChanges()
       5d-ii.  Collect domain events from tracked entities
       5d-iii. SaveChangesAsync() - Registration saved with xmin token
       5d-iv.  FOR EACH domain event:
               - Publish via MediatR (in try-catch, exceptions logged but NOT re-thrown)
               - PaymentCompletedEventHandler.Handle() is invoked
                 |
                 - Load Event and Registration
                 - Generate ticket via TicketService
                 - Render email template
                 - Send email via EmailService
       5d-v.   ClearDomainEvents()
   |
6. MarkEventAsProcessedAsync(stripeEvent.Id)
   - UPDATE stripe_webhook_events SET Processed=true WHERE EventId='evt_xxx'
   |
7. Return 200 OK to Stripe
```

---

## ROOT CAUSE ANALYSIS

### Finding 1: The Idempotency Guard in CompletePayment() IS Correctly Implemented

Looking at `Registration.cs` lines 347-405:

```csharp
public Result CompletePayment(string paymentIntentId)
{
    // Validation
    if (string.IsNullOrWhiteSpace(paymentIntentId))
        return Result.Failure("Payment intent ID cannot be empty");

    // Issue #56 FIX: Idempotency guard for duplicate webhook handling
    if (!string.IsNullOrEmpty(StripePaymentIntentId) &&
        StripePaymentIntentId.Equals(paymentIntentId, StringComparison.OrdinalIgnoreCase))
    {
        // Already completed with this payment intent - idempotent success (no domain event raised)
        return Result.Success();
    }

    // Critical validation - registration must be in Preliminary state
    if (Status != RegistrationStatus.Preliminary)
    {
        return Result.Failure(...);
    }

    // ... state transition and raise PaymentCompletedEvent
}
```

**Analysis**: This guard should prevent duplicate events IF the same registration is loaded with the already-set PaymentIntentId. However, this guard only works when:
1. The first webhook request has COMPLETED and saved to database
2. The second request loads the registration AFTER the first save

### Finding 2: The xmin Concurrency Token IS Correctly Configured

Looking at `RegistrationConfiguration.cs` lines 189-198:

```csharp
// Issue #56 FIX: Add concurrency token for optimistic locking
#pragma warning disable CS0618
builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618
```

**Analysis**: This should cause `DbUpdateConcurrencyException` when two concurrent requests try to save the same registration. However:
1. The exception would be thrown in `SaveChangesAsync()`
2. This happens BEFORE domain events are dispatched
3. If the exception is thrown, no email should be sent

### Finding 3: CRITICAL - The Race Condition Window

**THE ACTUAL ROOT CAUSE**: Both fixes assume serial processing, but there's a race window where:

```
Timeline (concurrent requests from Stripe):
=====================================

T0: Stripe sends checkout.session.completed (Event ID: evt_ABC)
T1: Request 1 - IsEventProcessedAsync() returns FALSE
T2: Request 2 arrives (Stripe retry or duplicate send) - IsEventProcessedAsync() returns FALSE (not processed yet!)
T3: Request 1 - EventExistsAsync() returns FALSE, RecordEventAsync() inserts evt_ABC
T4: Request 2 - EventExistsAsync() returns TRUE (row exists), continues processing
T5: Request 1 - LoadRegistration() - Status=Preliminary, PaymentIntentId=null
T6: Request 2 - LoadRegistration() - Status=Preliminary, PaymentIntentId=null (SAME STATE!)
T7: Request 1 - CompletePayment() - Idempotency guard: PaymentIntentId is NULL, proceeds
T8: Request 2 - CompletePayment() - Idempotency guard: PaymentIntentId is NULL, proceeds (SAME!)
T9: Request 1 - PaymentCompletedEvent RAISED
T10: Request 2 - PaymentCompletedEvent RAISED (DUPLICATE!)
T11: Request 1 - SaveChangesAsync() - SUCCEEDS, xmin updated
T12: Request 2 - SaveChangesAsync() - DbUpdateConcurrencyException (xmin mismatch)
T13: Request 1 - Domain events dispatched, EMAIL SENT
T14: Request 2 - Exception caught, BUT domain events were ALREADY RAISED before SaveChanges!
```

**THE CRITICAL BUG**: Domain events are collected from entities BEFORE SaveChangesAsync() is called. The PaymentCompletedEvent is raised in CompletePayment() and added to the entity's domain events list. Even if SaveChangesAsync() fails due to concurrency, the domain events were already added to the list at T10.

### Finding 4: Domain Event Collection Happens BEFORE Save

Looking at `AppDbContext.CommitAsync()` lines 417-437:

```csharp
// Collect domain events before saving (LINE 417-421)
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.Entity.DomainEvents.Any())
    .SelectMany(e => e.Entity.DomainEvents)
    .ToList();

// ... (collection complete, events in memory)

// Save changes to database (LINE 437)
var result = await SaveChangesAsync(cancellationToken);

// Dispatch domain events after successful save (LINE 441-476)
if (domainEvents.Any())
{
    foreach (var domainEvent in domainEvents)
    {
        await _publisher.Publish(notification, cancellationToken);
    }
}
```

**Wait - this is actually CORRECT!** Domain events are dispatched AFTER SaveChangesAsync(). If SaveChangesAsync() throws DbUpdateConcurrencyException, the code should exit before dispatching.

Let me re-analyze...

### Finding 5: THE TRUE ROOT CAUSE - TicketService Nested CommitAsync

Looking at `TicketService.GenerateTicketAsync()` lines 195-209:

```csharp
try
{
    var changeCount = await _unitOfWork.CommitAsync(cancellationToken);
}
catch (Exception commitEx)
{
    // Log but don't rethrow - the ticket may still have been saved
    _logger.LogWarning(commitEx, "...");
}
```

**THE NESTED COMMIT ISSUE**:
1. PaymentsController calls `_unitOfWork.CommitAsync()` (outer commit)
2. This saves Registration, then dispatches PaymentCompletedEvent
3. PaymentCompletedEventHandler calls TicketService.GenerateTicketAsync()
4. TicketService calls `_unitOfWork.CommitAsync()` AGAIN (nested commit)
5. The nested commit may trigger ANOTHER domain event dispatch cycle!

But wait - looking at the flow, the second CommitAsync is just for the Ticket entity, not the Registration. The Registration's domain events were already cleared after the first dispatch. So this shouldn't cause duplicate emails...

### Finding 6: RE-EXAMINING THE EVIDENCE

The user evidence shows:
- Two emails 1 minute apart (22 min ago vs 21 min ago)
- One email has `{{EventDateTime}}` - template placeholder not replaced

**Key Insight**: The template placeholder issue suggests the second email was generated from a different code path or with incomplete data.

**Possible Scenarios**:

1. **Stripe sends TWO different webhook events**:
   - `checkout.session.completed` (Event ID: evt_ABC)
   - `payment_intent.succeeded` (Event ID: evt_XYZ) - DIFFERENT event ID!
   - Both would pass the idempotency check because they have different Stripe event IDs

2. **The AttendeesAddedEvent handler also sends emails**:
   - Looking at PaymentsController line 375-382, there's a separate handler for "addition" payments
   - But this is for a different flow (adding attendees to existing registration)

3. **Another event handler sends emails for the same registration**:
   - RegistrationConfirmedEventHandler (for FREE events) vs PaymentCompletedEventHandler (for PAID events)
   - If both are triggered somehow, duplicate emails would occur

---

## DEFINITIVE ROOT CAUSE

After thorough analysis, the root cause is a **RACE CONDITION in the webhook idempotency check combined with domain event timing**.

### The Specific Bug:

**Phase 1: Race in IsEventProcessedAsync**
```
Request 1: IsEventProcessedAsync("evt_ABC") -> FALSE (row doesn't exist OR Processed=false)
Request 2: IsEventProcessedAsync("evt_ABC") -> FALSE (same - first request hasn't marked as processed yet)
```

**Phase 2: Both Requests Process**
Both requests load Registration with Status=Preliminary, PaymentIntentId=null, so the idempotency guard in CompletePayment() doesn't prevent either from proceeding.

**Phase 3: Domain Events Raised BEFORE Save**
```
Request 1: CompletePayment() raises PaymentCompletedEvent (event in memory)
Request 2: CompletePayment() raises PaymentCompletedEvent (event in memory)
```

**Phase 4: xmin Concurrency Detection (TOO LATE)**
```
Request 1: SaveChangesAsync() SUCCEEDS
Request 2: SaveChangesAsync() throws DbUpdateConcurrencyException
```

**Phase 5: Domain Event Dispatch**
```
Request 1: Domain events dispatched -> Email sent
Request 2: Exception caught in CommitAsync catch block BUT...
```

### The Critical Oversight

Looking at `AppDbContext.CommitAsync()` lines 459-470:

```csharp
try
{
    await _publisher.Publish(notification, cancellationToken);
}
catch (Exception handlerException)
{
    // Phase 6A.52: Log handler exceptions but don't re-throw
    _logger.LogError(handlerException, "[Phase 6A.52] [HANDLER-EXCEPTION] ...");
}
```

**The issue**: The outer try-catch for MediatR.Publish prevents handler exceptions from propagating, but this doesn't affect the concurrency exception scenario because:

1. `SaveChangesAsync()` is called BEFORE domain event dispatch
2. If SaveChangesAsync() fails, the code exits early

**Re-checking the code flow**... Actually, I see the issue now!

The domain events are collected BEFORE SaveChangesAsync(), but dispatched AFTER. So if SaveChangesAsync() throws, the dispatch should NOT happen.

### FINAL ROOT CAUSE IDENTIFICATION

After exhaustive analysis, the ONLY way two emails could be sent is if:

**Stripe is sending TWO DIFFERENT webhook events** with different Event IDs that both trigger PaymentCompletedEvent.

Common scenarios:
1. `checkout.session.completed` (evt_ABC) - triggers email
2. `payment_intent.succeeded` (evt_XYZ) - ALSO triggers email via different handler?

Looking at PaymentsController webhook handler, it only handles:
- `checkout.session.completed`
- `checkout.session.expired`
- `charge.refunded`

It does NOT handle `payment_intent.succeeded`. So this isn't the cause.

### THE ACTUAL ROOT CAUSE: Stripe Webhook Retry with Race Condition

**Scenario**:
1. Stripe sends `checkout.session.completed` (evt_ABC)
2. Request 1 starts processing
3. Before Request 1 marks event as processed, Stripe timeout + retry sends the SAME event again
4. Request 2 starts processing (same evt_ABC, but row exists with Processed=false)
5. Both requests load Registration with Preliminary status
6. Both requests call CompletePayment() - idempotency guard fails because StripePaymentIntentId is NULL for both
7. **Request 1 SaveChanges succeeds, dispatches events, sends email**
8. **Request 2 SaveChanges fails with DbUpdateConcurrencyException**
9. **BUT Request 2's domain events were already in the handler's notification variable!**

Wait, no. Let me trace this more carefully...

In `AppDbContext.CommitAsync()`:
1. Domain events collected into `domainEvents` list (line 418-421)
2. SaveChangesAsync() called (line 437)
3. IF SaveChangesAsync() throws, the method exits (exception propagates)
4. Domain events dispatched only if SaveChanges succeeded (line 441)

So if SaveChanges fails, events should NOT be dispatched.

### BREAKTHROUGH: TicketService Nested Commit DOES Dispatch Events!

Looking at `TicketService.GenerateTicketAsync()` line 197:
```csharp
var changeCount = await _unitOfWork.CommitAsync(cancellationToken);
```

This calls `AppDbContext.CommitAsync()` which:
1. Collects domain events from ALL tracked entities (including Registration!)
2. Calls SaveChangesAsync()
3. Dispatches domain events

**If Registration is still tracked with undispatched events, the nested CommitAsync will try to dispatch them AGAIN!**

But wait - the outer CommitAsync clears events after dispatch (line 480-483):
```csharp
foreach (var entry in ChangeTracker.Entries<BaseEntity>())
{
    entry.Entity.ClearDomainEvents();
}
```

So the events should be cleared before the nested commit...

**WAIT - THE EVENTS ARE CLEARED AFTER THE FOREACH LOOP THAT DISPATCHES THEM!**

If TicketService.GenerateTicketAsync() is called INSIDE the foreach loop that dispatches events, the events haven't been cleared yet!

Let me trace this:
1. Outer CommitAsync: SaveChanges() succeeds
2. Outer CommitAsync: foreach (domainEvent in domainEvents) - starts iterating
3. PaymentCompletedEvent is dispatched -> PaymentCompletedEventHandler runs
4. Handler calls TicketService.GenerateTicketAsync()
5. TicketService calls _unitOfWork.CommitAsync() - NESTED
6. Nested CommitAsync: Collects domain events - PaymentCompletedEvent is STILL in the list!
7. Nested CommitAsync: SaveChangesAsync() - only saves Ticket (Registration already saved)
8. Nested CommitAsync: Dispatches PaymentCompletedEvent AGAIN!
9. SECOND EMAIL SENT!
10. Nested CommitAsync: Clears events
11. Outer CommitAsync: Continue foreach loop, then clears events (already cleared)

**THIS IS THE ROOT CAUSE!**

---

## THE TRUE ROOT CAUSE

**Nested CommitAsync in TicketService Causes Double Domain Event Dispatch**

The PaymentCompletedEvent is dispatched during the outer CommitAsync's domain event dispatch loop. The handler calls TicketService, which calls CommitAsync again. The nested CommitAsync collects and dispatches domain events AGAIN because they haven't been cleared yet (clearing happens after the dispatch loop completes).

### Why the Previous Fixes Didn't Work

1. **Domain-level idempotency guard**: Doesn't help because the SAME event object is dispatched twice. The guard prevents raising NEW events, not dispatching existing ones.

2. **xmin concurrency token**: Doesn't help because both dispatches happen from the SAME request. There's no concurrent modification - just nested calls.

---

## RECOMMENDED FIX

### Option 1: Clear Domain Events BEFORE Dispatch Loop (Quick Fix)

**File:** `src/LankaConnect/Infrastructure/Data/AppDbContext.cs`

**Change:** Move ClearDomainEvents() to happen IMMEDIATELY after collecting events:

```csharp
// Collect domain events before saving
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.Entity.DomainEvents.Any())
    .SelectMany(e => e.Entity.DomainEvents)
    .ToList();

// CLEAR EVENTS IMMEDIATELY AFTER COLLECTION
// This prevents nested CommitAsync from re-dispatching the same events
foreach (var entry in ChangeTracker.Entries<BaseEntity>())
{
    entry.Entity.ClearDomainEvents();
}

// Save changes to database
var result = await SaveChangesAsync(cancellationToken);

// Dispatch domain events after successful save
// (events are now in local variable, not on entities)
if (domainEvents.Any())
{
    foreach (var domainEvent in domainEvents)
    {
        await _publisher.Publish(...);
    }
}
// No need to clear again - already cleared above
```

### Option 2: Add Dispatch Guard to Prevent Re-entry (Robust Fix)

**File:** `src/LankaConnect/Infrastructure/Data/AppDbContext.cs`

Add a thread-local flag to prevent nested dispatch:

```csharp
private static AsyncLocal<bool> _isDispatching = new AsyncLocal<bool>();

public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // Prevent nested dispatch
    if (_isDispatching.Value)
    {
        _logger.LogDebug("[AppDbContext] Nested CommitAsync detected - skipping event dispatch");
        return await SaveChangesAsync(cancellationToken);
    }

    // ... existing collection logic ...

    var result = await SaveChangesAsync(cancellationToken);

    if (domainEvents.Any())
    {
        _isDispatching.Value = true;
        try
        {
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(...);
            }
        }
        finally
        {
            _isDispatching.Value = false;
        }
    }

    // Clear events
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        entry.Entity.ClearDomainEvents();
    }

    return result;
}
```

### Option 3: Remove Nested CommitAsync from TicketService (Best Long-term)

The ticket will be committed as part of the outer transaction. This is the cleanest solution but requires careful testing to ensure tickets are saved correctly.

---

## TEST PLAN

### Unit Tests
1. **Test nested CommitAsync doesn't dispatch events twice**
   - Mock MediatR publisher
   - Call CommitAsync with entity that has domain event
   - In event handler, call CommitAsync again
   - Assert: Publisher.Publish called exactly ONCE per event

2. **Test domain events cleared before nested call**
   - After collecting events, clear immediately
   - Nested CommitAsync should find no events

### Integration Tests
1. **Test payment webhook doesn't send duplicate emails**
   - Create paid event registration in Preliminary state
   - Simulate checkout.session.completed webhook
   - Assert: Email sent exactly once
   - Assert: Ticket created exactly once

2. **Test concurrent webhook handling**
   - Use Stripe CLI to send same webhook twice in rapid succession
   - Assert: Only one email sent
   - Assert: Second request returns 200 without processing

### Manual Verification
1. Complete a real payment on staging
2. Check Azure logs for:
   - `[Phase 6A.24] Dispatching domain event: PaymentCompletedEvent` - should appear ONCE
   - `PaymentCompleted COMPLETE: Email sent successfully` - should appear ONCE
3. Verify only one email received

---

## Template Placeholder Issue

The evidence shows one email with `{{EventDateTime}}` not replaced. This is likely caused by:

1. The second email dispatch happening with stale or incomplete data
2. The TicketService nested CommitAsync causing the handler to run again with different context
3. Template rendering failing silently in the second dispatch

This will be resolved by fixing the duplicate dispatch issue.

---

## Summary

| Aspect | Details |
|--------|---------|
| **Issue Category** | Backend - Domain Event Double Dispatch |
| **Root Cause** | Nested CommitAsync in TicketService dispatches domain events AGAIN before the outer dispatch loop clears them |
| **Why Fix 1 Failed** | Domain idempotency guard prevents NEW events, not re-dispatch of existing ones |
| **Why Fix 2 Failed** | xmin token only prevents concurrent DB writes, not nested calls from same request |
| **Recommended Fix** | Clear domain events IMMEDIATELY after collection, before dispatch loop |
| **Impact** | Duplicate confirmation emails, template rendering issues |
| **Priority** | High - User-facing duplicate communications |

---

## Document Information

- **Author**: Claude AI (Architecture Agent)
- **Date**: 2026-02-04
- **Related Issues**: #56
- **Previous RCAs**: RCA_PAYMENT_WEBHOOK_CONCURRENCY_ISSUE.md
- **Status**: Analysis Complete - Ready for Implementation
