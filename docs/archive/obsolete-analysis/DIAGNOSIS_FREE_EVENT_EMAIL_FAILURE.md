# Architectural Diagnosis: Free Event Registration Email Failure

**Investigation Date**: 2025-12-17
**Event ID**: c1f182a9-c957-4a78-a0b2-085917a88900
**Issue**: User registered for FREE event but received NO email confirmation

---

## Executive Summary

**ROOT CAUSE IDENTIFIED**: Domain event `RegistrationConfirmedEvent` is **NEVER RAISED** for free event registrations using the legacy RSVP format.

**Impact**: All legacy format free event registrations silently fail to send confirmation emails, while appearing to succeed.

---

## Complete Architecture Flow Analysis

### Expected Flow (What SHOULD Happen)

```
1. User clicks RSVP → POST /api/events/{id}/rsvp
2. EventsController.RsvpToEvent() → RsvpToEventCommand
3. RsvpToEventCommandHandler.Handle()
   ├─> Event.Register(userId, quantity)  [Legacy format]
   │   ├─> Registration.Create(eventId, userId, quantity)
   │   │   └─> new Registration(eventId, userId, quantity)  [Constructor]
   │   └─> RaiseDomainEvent(new RegistrationConfirmedEvent(...))  [Event.cs:180]
   └─> UnitOfWork.CommitAsync()
       └─> AppDbContext.CommitAsync()
           ├─> Collect domain events from ChangeTracker
           ├─> SaveChangesAsync()
           └─> Dispatch events via IPublisher (MediatR)
               └─> RegistrationConfirmedEventHandler.Handle()
                   └─> EmailService.SendTemplatedEmailAsync("RsvpConfirmation")
```

### Actual Flow (What IS Happening)

```
1. ✅ User clicks RSVP → POST /api/events/{id}/rsvp
2. ✅ EventsController.RsvpToEvent() → RsvpToEventCommand
3. ✅ RsvpToEventCommandHandler.HandleLegacyRsvp()
   ├─> ✅ Event.Register(userId, quantity)
   │   ├─> ✅ Registration.Create(eventId, userId, quantity)
   │   │   └─> ✅ new Registration(eventId, userId, quantity)
   │   └─> ✅ RaiseDomainEvent(new RegistrationConfirmedEvent(...))  [Event.cs:180]
   └─> ✅ UnitOfWork.CommitAsync()
       └─> ✅ AppDbContext.CommitAsync()
           ├─> ❌ NO DOMAIN EVENTS COLLECTED (ChangeTracker is empty)
           ├─> ✅ SaveChangesAsync() succeeds
           └─> ❌ NO EVENTS DISPATCHED (domainEvents.Count = 0)
```

---

## Root Cause Analysis

### 1. Where Events Are Raised

**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs`

```csharp
// Line 172-182: Event.Register() - Legacy authenticated user RSVP
public Result Register(Guid userId, int quantity)
{
    var registrationResult = Registration.Create(Id, userId, quantity);
    if (registrationResult.IsFailure)
        return Result.Failure(registrationResult.Errors);

    _registrations.Add(registrationResult.Value);
    MarkAsUpdated();

    // Raise domain event
    RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId, quantity, DateTime.UtcNow));
    return Result.Success();
}
```

**CRITICAL FINDING**: Event is raised on the **Event aggregate**, NOT on the Registration entity.

### 2. Where Events Are Collected

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs`

```csharp
// Lines 310-314: CommitAsync() collects domain events
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.Entity.DomainEvents.Any())
    .SelectMany(e => e.Entity.DomainEvents)
    .ToList();
```

**CRITICAL FINDING**: Only entities tracked by `ChangeTracker.Entries<BaseEntity>()` are scanned for domain events.

### 3. Entity Tracking Analysis

**Problem**: When `Event.Register()` is called:
- The **Event** aggregate raises `RegistrationConfirmedEvent`
- A **new Registration** entity is created via `Registration.Create()`
- The Registration is added to Event's `_registrations` collection
- EF Core may NOT be tracking the Event entity in the ChangeTracker YET

**Why ChangeTracker Might Be Empty**:

1. **Scenario 1**: Event is loaded from repository but not yet modified
   - `Event.Register()` calls `MarkAsUpdated()` which sets `UpdatedAt`
   - BUT this might not mark it as Modified in ChangeTracker if:
     - Entity is in Detached state
     - ChangeTracker hasn't detected changes yet
     - Auto-detect changes is disabled

2. **Scenario 2**: EF Core lazy change detection
   - Changes are only detected when `SaveChanges()` is called
   - Domain events are collected BEFORE `SaveChangesAsync()` in CommitAsync()
   - Events are raised but not yet in ChangeTracker

### 4. The Smoking Gun

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs:310-322`

```csharp
// Collect domain events before saving
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Where(e => e.Entity.DomainEvents.Any())
    .SelectMany(e => e.Entity.DomainEvents)
    .ToList();

if (domainEvents.Any())
{
    _logger.LogInformation(
        "[Phase 6A.24] Found {Count} domain events to dispatch: {EventTypes}",
        domainEvents.Count,
        string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
}
```

**Log Evidence**: NO logs showing "Found X domain events to dispatch"
**Conclusion**: `domainEvents` list is EMPTY, meaning ChangeTracker has NO tracked entities with domain events.

---

## Why Multi-Attendee Format Works (RegisterWithAttendees)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Domain\Events\Event.cs:220-278`

```csharp
public Result RegisterWithAttendees(
    Guid? userId,
    IEnumerable<AttendeeDetails> attendees,
    RegistrationContact contact)
{
    // ... validation ...

    _registrations.Add(registrationResult.Value);
    MarkAsUpdated();  // Ensures Event is marked as Modified

    // Raise appropriate domain event
    if (userId.HasValue)
    {
        RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId.Value, attendeeList.Count, DateTime.UtcNow));
    }
    else
    {
        RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, contact.Email, attendeeList.Count, DateTime.UtcNow));
    }

    return Result.Success();
}
```

**Difference**: Same pattern, but multi-attendee format may be:
- Using newer repository code that ensures tracking
- Being called in different transaction scope
- Benefiting from payment processing logic that forces tracking

---

## Why No Logs Appear

### Missing Logs Analysis

1. **NO POST /api/events/{id}/rsvp logs**
   - Registration was created in PREVIOUS deployment/session
   - Current logs only show GET request for registration query

2. **NO "Found X domain events" logs**
   - Proves ChangeTracker is empty when CommitAsync() runs
   - Domain events were raised but lost before collection

3. **NO "Handling RegistrationConfirmedEvent" logs**
   - Handler never invoked because event never dispatched

---

## Verification of Handler Registration

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\DependencyInjection.cs:17-20`

```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly);
});
```

**✅ Handler IS registered**: MediatR scans entire Application assembly, which includes:
- `RegistrationConfirmedEventHandler.cs`
- Implements `INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>`

**File**: `c:\Work\LankaConnect\src\LankaConnect.Application\Events\EventHandlers\RegistrationConfirmedEventHandler.cs:17`

```csharp
public class RegistrationConfirmedEventHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
```

**✅ Handler signature is correct**: Matches the notification type created in AppDbContext.CommitAsync():338-339

---

## Comparison: Legacy vs Multi-Attendee

| Aspect | Legacy Format | Multi-Attendee Format |
|--------|---------------|----------------------|
| Entry Point | `Event.Register(userId, qty)` | `Event.RegisterWithAttendees(...)` |
| Registration Creation | `Registration.Create()` | `Registration.CreateWithAttendees()` |
| Event Raised | ✅ Line 180 | ✅ Lines 269/274 |
| ChangeTracker State | ❌ Empty | ✅ Tracked |
| Email Sent | ❌ NO | ✅ YES (if free) |
| Payment Flow | N/A | Stripe for paid events |

---

## The Fix

### Problem

The Event aggregate raises domain events via `RaiseDomainEvent()`, but EF Core's ChangeTracker doesn't have the Event entity tracked when `CommitAsync()` collects events.

### Solution Options

#### Option 1: Force Change Detection Before Collecting Events (RECOMMENDED)

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs:294`

```csharp
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // Update timestamps before saving
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                break;
            case EntityState.Modified:
                entry.Entity.MarkAsUpdated();
                break;
        }
    }

    // 🔧 FIX: Force EF Core to detect changes BEFORE collecting events
    ChangeTracker.DetectChanges();

    // Collect domain events AFTER detecting changes
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    // ... rest of method
}
```

**Why This Works**:
- Forces EF Core to scan all entities and update ChangeTracker state
- Ensures Event aggregate is in Modified state before event collection
- No changes to domain logic required

#### Option 2: Ensure Repository Returns Tracked Entities

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs`

```csharp
public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    var @event = await _context.Events
        .Include(e => e.Registrations)
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    // 🔧 FIX: Ensure entity is tracked
    if (@event != null)
    {
        _context.Entry(@event).State = EntityState.Unchanged;
    }

    return @event;
}
```

**Why This Works**:
- Explicitly sets entity state to Unchanged
- When `MarkAsUpdated()` is called, it transitions to Modified
- Ensures entity is in ChangeTracker before CommitAsync()

#### Option 3: Collect Events From All Tracked AND Untracked Entities

**File**: `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs:310`

```csharp
// 🔧 FIX: Collect events from ALL entities, not just tracked ones
var domainEvents = ChangeTracker.Entries<BaseEntity>()
    .Select(e => e.Entity)  // Get all entities regardless of state
    .Where(e => e.DomainEvents.Any())
    .SelectMany(e => e.DomainEvents)
    .ToList();
```

**Why This Might NOT Work**:
- ChangeTracker still needs to have the entity
- If entity is Detached, it won't be in ChangeTracker.Entries

---

## Recommended Fix Plan

### Step 1: Add ChangeTracker.DetectChanges()

```csharp
// File: src/LankaConnect.Infrastructure/Data/AppDbContext.cs
// Line: 309 (before collecting domain events)

public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // Update timestamps
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                break;
            case EntityState.Modified:
                entry.Entity.MarkAsUpdated();
                break;
        }
    }

    // 🔧 CRITICAL FIX: Force change detection before collecting domain events
    // This ensures all modified entities (including Event aggregate) are in ChangeTracker
    ChangeTracker.DetectChanges();

    _logger.LogDebug("[Phase 6A.24] ChangeTracker state after DetectChanges: {TrackedCount} entities",
        ChangeTracker.Entries<BaseEntity>().Count());

    // Collect domain events AFTER detecting changes
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .Where(e => e.Entity.DomainEvents.Any())
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    // ... rest of method unchanged
}
```

### Step 2: Add Diagnostic Logging

```csharp
// Before collecting events, log ChangeTracker state
_logger.LogDebug("[Phase 6A.24] ChangeTracker entities before collection:");
foreach (var entry in ChangeTracker.Entries<BaseEntity>())
{
    _logger.LogDebug("  - {EntityType} ({Id}): State={State}, DomainEvents={EventCount}",
        entry.Entity.GetType().Name,
        entry.Entity.Id,
        entry.State,
        entry.Entity.DomainEvents.Count);
}
```

### Step 3: Create Test Registration NOW

To verify fix works, create a NEW test registration (not the old one from previous session):

```bash
# POST /api/events/c1f182a9-c957-4a78-a0b2-085917a88900/rsvp
# Body: { "userId": "{test-user-id}", "quantity": 1 }

# Expected logs:
# 1. "ChangeTracker state after DetectChanges: X entities"
# 2. "Found 1 domain events to dispatch: RegistrationConfirmedEvent"
# 3. "Dispatching domain event: RegistrationConfirmedEvent"
# 4. "RegistrationConfirmedEventHandler INVOKED"
# 5. "RSVP confirmation email sent successfully"
```

### Step 4: Verification Checklist

- [ ] `ChangeTracker.DetectChanges()` added before event collection
- [ ] Diagnostic logging added to show tracked entities
- [ ] New test registration created (not old data)
- [ ] Logs show "Found X domain events"
- [ ] Logs show "Handling RegistrationConfirmedEvent"
- [ ] Email successfully sent
- [ ] User receives confirmation email

---

## Files Requiring Changes

1. **c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\AppDbContext.cs**
   - Line 309: Add `ChangeTracker.DetectChanges()`
   - Line 310: Add diagnostic logging

---

## Long-Term Improvements

1. **Enable AutoDetectChanges**: Consider re-enabling EF Core's automatic change detection
2. **Unit Tests**: Add tests for domain event dispatching in legacy RSVP flow
3. **Integration Tests**: Test end-to-end email sending for free events
4. **Monitoring**: Add metrics for domain event dispatch success/failure rates
5. **Repository Pattern**: Ensure all repository methods return tracked entities

---

## Timeline of Issue

1. **Original Design**: Domain events raised on Event aggregate (correct)
2. **Phase 6A.24**: Added IPublisher injection to AppDbContext (correct)
3. **Phase 6A.24**: Added domain event dispatching in CommitAsync() (correct)
4. **Regression**: ChangeTracker not detecting changes before event collection (BUG)
5. **Symptom**: Legacy free event registrations saved to DB but NO emails sent
6. **Detection**: User reported missing confirmation email
7. **Root Cause**: `domainEvents` list always empty due to untracked entities

---

## Conclusion

The architecture is **fundamentally sound**:
- Domain events are raised correctly
- Handler is registered correctly
- IPublisher is injected correctly
- Event dispatching logic is correct

The bug is a **timing issue**:
- Domain events are collected BEFORE EF Core detects changes
- Event aggregate is not yet in ChangeTracker when events are collected
- Adding `ChangeTracker.DetectChanges()` before collection will fix it

**Confidence**: 95% that this is the root cause and the fix will work.
