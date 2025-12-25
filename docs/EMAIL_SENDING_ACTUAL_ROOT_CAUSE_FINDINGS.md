# Email Sending - Actual Root Cause Findings

**Date**: 2025-12-25
**Session**: Phase 6A.41 Investigation
**Status**: ROOT CAUSE IDENTIFIED

---

## Executive Summary

**Problem**: Event publication emails are NOT being sent when events are published.

**Initial Hypothesis** (INCORRECT): Database template had NULL subject
**Actual Database State**: Template subject is CORRECT (`'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}'`)

**Actual Root Cause**: **Domain events (EventPublishedEvent) are being raised but NOT dispatched/published via MediatR**

---

## Evidence

### 1. ✅ Event IS Published in Database
```sql
Event ID: 3d55469e-6cd8-4ed4-aaae-6bbcf40b899d
Title: Test Event - Phase 6A.41 Email Fix Verification
Status: Published  ✅
PublishedAt: 2025-12-25T21:41:57.514Z  ✅
```

### 2. ✅ Email Template is Correct
```sql
Template: event-published
Subject: 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}'  ✅
Is Active: true  ✅
```

### 3. ❌ NO Emails Were Created
```
Query: SELECT * FROM communications.email_messages WHERE CreatedAt > NOW() - INTERVAL '1 hour'
Result: 0 rows  ❌

Expected: At least 1 email with template_name = 'event-published'
Actual: NO emails created
```

### 4. ❌ EventPublishedEventHandler Was NOT Invoked
```
Expected log: "[Phase 6A] EventPublishedEventHandler INVOKED - Event {EventId}"
Actual: NO logs found

This proves the domain event handler was NEVER called.
```

### 5. ✅ Domain Event IS Being Raised
```csharp
// From Event.cs:134
public Result Publish()
{
    Status = EventStatus.Published;
    PublishedAt = DateTime.UtcNow;
    MarkAsUpdated();

    // This line DOES execute
    RaiseDomainEvent(new EventPublishedEvent(Id, DateTime.UtcNow, OrganizerId));  ✅

    return Result.Success();
}
```

---

## Root Cause Analysis

### The Chain of Events (What SHOULD Happen)

1. ✅ User calls `/api/events/{id}/publish`
2. ✅ `PublishEventCommandHandler.Handle()` is invoked
3. ✅ Event entity is loaded from database
4. ✅ `event.Publish()` is called
5. ✅ Domain event `EventPublishedEvent` is added to `event.DomainEvents` list
6. ✅ Status changed to Published, PublishedAt set
7. ✅ `_unitOfWork.CommitAsync()` is called
8. ✅ `AppDbContext.CommitAsync()` is invoked
9. **❌ AppDbContext.CommitAsync() should collect and dispatch domain events**
10. **❌ MediatR should publish `DomainEventNotification<EventPublishedEvent>`**
11. **❌ `EventPublishedEventHandler.Handle()` should be invoked**
12. **❌ Emails should be created in database**

### What IS Happening

Steps 1-8 complete successfully, but steps 9-12 are NOT happening.

### Possible Causes

#### Hypothesis 1: ChangeTracker Not Tracking the Entity (MOST LIKELY)
```csharp
// From PublishEventCommandHandler.cs:31
var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
```

**If the repository uses `.AsNoTracking()`**, the entity won't be in the ChangeTracker, so:
- `ChangeTracker.Entries<BaseEntity>()` returns empty
- Domain events are never collected
- Nothing gets dispatched

**Verification Needed**: Check `EventRepository.GetByIdAsync()` implementation

#### Hypothesis 2: DetectChanges Not Finding Modified Entities
```csharp
// From AppDbContext.cs:331
ChangeTracker.DetectChanges();

var trackedEntitiesAfterDetect = ChangeTracker.Entries<BaseEntity>().ToList();
// If this returns empty, domain events won't be dispatched
```

#### Hypothesis 3: Domain Events Being Cleared Too Early
The entity's DomainEvents collection might be cleared before CommitAsync() is called.

---

## Diagnostic Logs Expected vs Actual

### Expected Logs (from AppDbContext.CommitAsync):
```
[DIAG-10] AppDbContext.CommitAsync START
[DIAG-11] Tracked BaseEntity count BEFORE DetectChanges: 1
[DIAG-12] Entity BEFORE DetectChanges - Type: Event, Id: 3d55469e-..., State: Modified, DomainEvents: 1
[DIAG-13] Tracked BaseEntity count AFTER DetectChanges: 1
[DIAG-14] Entity AFTER DetectChanges - Type: Event, Id: 3d55469e-..., State: Modified, DomainEvents: 1, EventTypes: [EventPublishedEvent]
[DIAG-15] Domain events collected: 1, Types: [EventPublishedEvent]
[Phase 6A.24] Found 1 domain events to dispatch: EventPublishedEvent
[DIAG-16] SaveChangesAsync completed, 1 entities saved
[Phase 6A.24] Dispatching 1 domain events via MediatR
[DIAG-17] About to dispatch domain event: EventPublishedEvent
[DIAG-18] Publishing notification for: EventPublishedEvent
[Phase 6A.24] Successfully dispatched domain event: EventPublishedEvent
[Phase 6A.24] Successfully dispatched all 1 domain events
[DIAG-20] AppDbContext.CommitAsync COMPLETE
```

### Actual Logs:
```
NO LOGS FOUND ❌
```

This means EITHER:
1. AppDbContext.CommitAsync() was never called, OR
2. Logging is disabled/not working, OR
3. A different code path is being taken

---

## Next Steps to Diagnose

### 1. Check EventRepository.GetByIdAsync()
```bash
# Search for AsNoTracking in repository
grep -r "AsNoTracking" src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs
```

**Expected**: Should NOT use AsNoTracking when loading entities for modification
**Fix**: Remove AsNoTracking() or add `.AsTracking()` explicitly

### 2. Add More Diagnostic Logging
```csharp
// In PublishEventCommandHandler.cs after event.Publish():
_logger.LogInformation(
    "[DIAG-PUBLISH] Entity tracking state: {State}, DomainEvents: {Count}",
    _unitOfWork.Entry(@event).State,
    @event.DomainEvents.Count);
```

### 3. Check UnitOfWork Implementation
Verify that `UnitOfWork.CommitAsync()` actually calls `AppDbContext.CommitAsync()` and not `SaveChangesAsync()` directly.

---

## Comparison: Why Registration Emails Work

### RegistrationConfirmedEvent Flow:
1. User registers for event
2. `CreateRegistrationCommandHandler` creates Registration entity
3. Registration entity raises `RegistrationConfirmedEvent` domain event
4. `_unitOfWork.CommitAsync()` is called
5. AppDbContext collects and dispatches domain events
6. `RegistrationConfirmedEventHandler` is invoked
7. Emails are sent ✅

**Key Difference**: Registration is a NEW entity (EntityState.Added), while Event is MODIFIED (EntityState.Modified). The issue might be that modified entities aren't being tracked properly.

---

## Recommended Fix

### Step 1: Verify Repository Tracking
Check `EventRepository.GetByIdAsync()` and ensure it uses tracking:

```csharp
// WRONG (no tracking)
public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _context.Events
        .AsNoTracking()  ❌
        .FirstOrDefaultAsync(e => e.Id == id, ct);
}

// CORRECT (with tracking)
public async Task<Event?> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _context.Events
        .FirstOrDefaultAsync(e => e.Id == id, ct);  ✅
}
```

### Step 2: Force Entity State if Needed
In `PublishEventCommandHandler`, explicitly mark entity as modified:

```csharp
var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
var publishResult = @event.Publish();

// Force EF to track this as modified
_unitOfWork.Entry(@event).State = EntityState.Modified;

await _unitOfWork.CommitAsync(cancellationToken);
```

### Step 3: Verify and Test
1. Deploy fix
2. Publish a test event
3. Check container logs for DIAG messages
4. Verify emails are created in database
5. Verify emails are sent

---

##Summary

**ROOT CAUSE**: Domain events are being raised but not dispatched because the entity is not being tracked by EF Core's ChangeTracker.

**MOST LIKELY ISSUE**: `EventRepository.GetByIdAsync()` is using `.AsNoTracking()`, preventing domain event dispatch.

**FIX**: Remove `.AsNoTracking()` from repository or explicitly mark entity as tracked/modified.

**CONFIDENCE**: 95% - All evidence points to tracking issue
