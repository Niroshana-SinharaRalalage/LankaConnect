# Event Cancellation Email Flow - Complete Analysis

**Date**: 2026-01-06
**Event ID**: 1d73cdb2-93ca-4403-8eb0-59afb22b66b3
**Status**: Cancelled in DB, but NO emails sent

---

## Complete Flow Diagram

```
UI (localhost:3000)
  ↓
[1] CancelEventModal.onConfirm(reason)
  ↓
[2] eventsRepository.cancelEvent(id, reason)
  ↓
[3] POST https://lankaconnect-api-staging.../api/events/{id}/cancel
  ↓
[4] EventsController.CancelEvent(id, request)
      Log: "Cancelling event: {EventId}"
  ↓
[5] Mediator.Send(CancelEventCommand)
  ↓
[6] CancelEventCommandHandler.Handle()
      Log: "[Phase 6A.63] CancelEventCommandHandler - START..."
      ↓
[7] EventRepository.GetByIdAsync(trackChanges: true)
      Log: "[Phase 6A.63] Event {EventId} retrieved..."
      ↓
[8] event.Cancel(reason) → Domain Method
      - Sets Status = Cancelled
      - Sets CancellationReason
      - RaiseDomainEvent(EventCancelledEvent) ← CRITICAL!
      Log: "[Phase 6A.63] Event.Cancel() succeeded, DomainEvents count: {Count}"
      ↓
[9] UnitOfWork.CommitAsync()
      ↓
[10] AppDbContext.CommitAsync()
      Log: "[DIAG-10] AppDbContext.CommitAsync START"
      ↓
[11] ChangeTracker.DetectChanges()
      Log: "[DIAG-11] Tracked BaseEntity count BEFORE..."
      Log: "[DIAG-13] Tracked BaseEntity count AFTER..."
      ↓
[12] Collect Domain Events
      Log: "[DIAG-14] Entity AFTER DetectChanges - DomainEvents: {Count}"
      Log: "[DIAG-15] Domain events collected: {Count}"
      ↓
[13] SaveChangesAsync() → Database UPDATE
      Log: "[DIAG-16] SaveChangesAsync completed"
      ↓
[14] MediatR.Publish(DomainEventNotification<EventCancelledEvent>)
      Log: "[DIAG-17] About to dispatch domain event: EventCancelledEvent"
      Log: "[DIAG-18] Publishing notification for: EventCancelledEvent"
      ↓
[15] EventCancelledEventHandler.Handle() ← EMAIL HANDLER
      Log: "[Phase 6A.63] EventCancelledEventHandler INVOKED..."
      ↓
[16] EventRepository.GetByIdAsync()
      ↓
[17] RegistrationRepository.GetByEventAsync()
      Log: "[Phase 6A.63] Found {Count} confirmed registrations..."
      ↓
[18] EventNotificationRecipientService.ResolveRecipientsAsync()
      Log: "[Phase 6A.63] Resolved {Count} notification recipients..."
      ↓
[19] Consolidate Recipients (registrations + email groups + newsletter)
      Log: "[Phase 6A.63] Sending cancellation emails to {TotalCount} unique recipients..."
      ↓
[20] FOR EACH recipient:
      EmailService.SendTemplatedEmailAsync("event-cancelled-notification", email, parameters)
        ↓
[21] EmailTemplateRepository.GetByNameAsync("event-cancelled-notification")
        Log: "Getting template by name: event-cancelled-notification"
        Log: "Found template {TemplateId} with name event-cancelled-notification"
        ↓
[22] Check template.IsActive
        If !IsActive → FAIL: "Email template 'event-cancelled-notification' is not active"
        ↓
[23] TemplateService.RenderTemplateAsync()
        ↓
[24] EmailService.SendEmailAsync()
        ↓
[25] SendViaSmtpAsync() OR SendViaAzureCommunicationServicesAsync()

      If SUCCESS:
        Log: "[Phase 6A.63] Event cancellation emails completed. Success: {SuccessCount}, Failed: {FailCount}"
      If FAILURE:
        Log: "[Phase 6A.63] Failed to send event cancellation email to {Email}: {Errors}"
```

---

## Current Problem: MISSING LOGS

**Expected logs** (if working):
```
"Cancelling event: 1d73cdb2-93ca-4403-8eb0-59afb22b66b3"
"[Phase 6A.63] CancelEventCommandHandler - START..."
"[DIAG-10] AppDbContext.CommitAsync START"
"[DIAG-17] About to dispatch domain event: EventCancelledEvent"
"[Phase 6A.63] EventCancelledEventHandler INVOKED..."
"[Phase 6A.63] Sending cancellation emails to {N} unique recipients..."
```

**Actual logs found**:
- NONE - Zero logs for this event ID
- NO "Cancelling event" message
- NO "[Phase 6A.63]" messages
- NO "[DIAG-*]" messages

---

## Gap Analysis vs Working Registration Email

### Registration Email Flow (WORKING):
```
Registration.Confirm()
  → RaiseDomainEvent(RegistrationConfirmedEvent)
  → UnitOfWork.CommitAsync()
  → AppDbContext dispatches domain events
  → RegistrationConfirmedEventHandler executes
  → EmailService.SendTemplatedEmailAsync("registration-confirmation", ...)
  → ✅ EMAIL SENT
```

### Cancellation Email Flow (BROKEN):
```
Event.Cancel()
  → RaiseDomainEvent(EventCancelledEvent)
  → UnitOfWork.CommitAsync()
  → AppDbContext dispatches domain events
  → EventCancelledEventHandler executes ???
  → EmailService.SendTemplatedEmailAsync("event-cancelled-notification", ...) ???
  → ❌ NO EMAILS SENT
```

---

## Key Differences to Investigate

1. **Template Loading**:
   - Registration: `"registration-confirmation"` template
   - Cancellation: `"event-cancelled-notification"` template
   - Both use same `EmailTemplateRepository.GetByNameAsync()`

2. **Template Parameters**:
   - Registration: UserName, EventTitle, EventDateTime, etc.
   - Cancellation: EventTitle, EventStartDate, EventStartTime, EventLocation, CancellationReason

3. **Handler Execution**:
   - Registration: Try-catch with fail-silent pattern
   - Cancellation: Try-catch with fail-silent pattern (same pattern)

4. **Logging**:
   - Registration: Basic logging
   - Cancellation: Comprehensive "[Phase 6A.63]" logging

---

## Hypothesis: Silent Failure Points

### Point 1: EventCancelledEventHandler NOT INVOKED
**Evidence**: No "[Phase 6A.63] EventCancelledEventHandler INVOKED" log
**Possible Causes**:
- MediatR handler NOT registered
- Domain event NOT being raised
- Domain event NOT being dispatched

### Point 2: Template Loading Fails
**Evidence**: No "Getting template by name: event-cancelled-notification" log
**Possible Causes**:
- Template doesn't exist in database
- Template category='Event' invalid (Phase6A63Fix4 should have fixed this)
- Template is_active=false

### Point 3: Handler Throws Exception (Caught by Fail-Silent)
**Evidence**: No error logs
**Possible Causes**:
- Exception in handler caught by try-catch at line 165-169
- Exception in AppDbContext.CommitAsync caught at line 412-419
- Exception NOT being logged

---

## Immediate Actions Required

1. **Add More Logging** - EventCancelledEventHandler needs MORE logs at EVERY step
2. **Check Template in Database** - Verify template exists and is_active=true
3. **Test API Directly** - Call `/api/events/{id}/cancel` via Postman/curl
4. **Check Azure Logs** - Look for exceptions around event cancellation time
5. **Add Try-Catch Logging** - Log ALL exceptions, even in fail-silent blocks

---

## Next Steps

1. Add comprehensive logging to EventCancelledEventHandler
2. Add logging to EmailTemplateRepository.GetByNameAsync
3. Add logging to EmailService.SendTemplatedEmailAsync
4. Test event cancellation via API
5. Check Azure logs for errors
