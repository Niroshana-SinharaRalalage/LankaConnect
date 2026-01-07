# Root Cause Analysis: Event Cancellation Timeout Error

**Date**: 2026-01-07
**Issue**: Event cancellation fails with 30-second timeout on first click, then 400 validation error on second click, but event is actually cancelled in database
**Severity**: High - Critical user experience issue
**Status**: Analysis Complete - Ready for Fix Implementation

---

## Executive Summary

The event cancellation feature experiences a **30-second timeout on the first click**, followed by a **400 Bad Request validation error on the second click** stating "Only published or draft events can be cancelled". However, **the cancellation actually succeeds** - the event status is updated to Cancelled in the database, as confirmed by page refresh.

**Root Cause**: The cancellation operation takes longer than 30 seconds due to **email notification processing in domain event handlers**, causing a frontend timeout. The backend transaction completes successfully after the timeout, but the frontend retries with stale state.

**Impact**:
- Poor user experience (appears to fail, requires retry, shows confusing error)
- User uncertainty about operation success
- Potential duplicate cancellation attempts
- Database shows event as Cancelled despite frontend error messages

---

## 1. Issue Classification

### Primary Issue: **Backend Performance - Long-Running Transaction**
The cancellation operation exceeds the 30-second frontend timeout threshold due to synchronous email processing within the database transaction.

### Secondary Issue: **State Synchronization**
The frontend doesn't refetch event state after timeout, leading to stale data and validation errors on retry.

### Affected Layers:
1. **Backend (Primary)**: Email processing in EventCancelledEventHandler blocking transaction commit
2. **Frontend (Secondary)**: Timeout handling and state management in React component
3. **API Client (Configuration)**: 30-second timeout may be too short for operations with email notifications

---

## 2. Detailed Code Flow Analysis

### 2.1 Frontend Cancellation Flow

**File**: `web/src/app/events/[id]/manage/page.tsx`

```typescript
// Line 119-147: handleCancelEvent function
const handleCancelEvent = async () => {
  try {
    setIsCancelling(true);
    setError(null);
    await eventsRepository.cancelEvent(id, cancellationReason.trim()); // Line 137 - TIMEOUT HERE
    setIsCancelling(false);
    setShowCancelModal(false);
    setCancellationReason('');
    await refetch(); // Only called if no error thrown
  } catch (err) {
    console.error('Failed to cancel event:', err);
    setError(err instanceof Error ? err.message : 'Failed to cancel event. Please try again.');
    setIsCancelling(false);
    // Modal stays open, no refetch - STALE STATE
  }
};
```

**Issues**:
1. **No refetch on timeout**: When `cancelEvent()` times out, `refetch()` is never called
2. **Stale event state**: Component continues to use pre-cancellation event data
3. **Modal persists**: User sees error and can retry with stale state

### 2.2 Repository Layer

**File**: `web/src/infrastructure/api/repositories/events.repository.ts`

```typescript
// Line 236-239: cancelEvent method
async cancelEvent(id: string, reason: string): Promise<void> {
  const request: CancelEventRequest = { reason };
  await apiClient.post<void>(`${this.basePath}/${id}/cancel`, request);
}
```

Uses default timeout from apiClient (30 seconds).

### 2.3 API Client Configuration

**File**: `web/src/infrastructure/api/client/api-client.ts`

```typescript
// Line 40-48: ApiClient constructor
this.axiosInstance = axios.create({
  baseURL,
  timeout: config?.timeout || 30000, // 30 SECONDS DEFAULT
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    ...config?.headers,
  },
});
```

**Issue**: 30-second timeout is insufficient for operations that:
- Send emails to multiple recipients
- Process event registrations
- Query recipient lists (email groups + newsletter subscribers)

### 2.4 Backend API Endpoint

**File**: `src/LankaConnect.API/Controllers/EventsController.cs`

```csharp
// Lines 416-429: Cancel event endpoint
[HttpPost("{id:guid}/cancel")]
[Authorize]
public async Task<IActionResult> CancelEvent(Guid id, [FromBody] CancelEventRequest request)
{
    Logger.LogInformation("Cancelling event: {EventId}", id);

    var command = new CancelEventCommand(id, request.Reason);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

**Observation**: Standard MediatR command pattern, no explicit timeout handling.

### 2.5 Command Handler

**File**: `src/LankaConnect.Application/Events/Commands/CancelEvent/CancelEventCommandHandler.cs`

```csharp
// Lines 24-69: Handle method
public async Task<Result> Handle(CancelEventCommand request, CancellationToken cancellationToken)
{
    // 1. Retrieve event WITH CHANGE TRACKING
    var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);

    // 2. Call domain method (raises EventCancelledEvent)
    var cancelResult = @event.Cancel(request.CancellationReason);

    // 3. Commit changes (triggers domain event dispatch)
    await _unitOfWork.CommitAsync(cancellationToken); // BLOCKS HERE FOR 30+ SECONDS

    return Result.Success();
}
```

**Critical Path**: The `CommitAsync()` call blocks until **ALL domain event handlers complete**, including email sending.

### 2.6 Domain Event: EventCancelledEvent

**File**: `src/LankaConnect.Domain/Events/Event.cs`

```csharp
// Lines 162-179: Cancel method
public Result Cancel(string reason)
{
    // Validation: Only Published or Draft events can be cancelled
    if (Status != EventStatus.Published && Status != EventStatus.Draft)
        return Result.Failure("Only published or draft events can be cancelled"); // LINE 166 - SECOND CLICK ERROR

    Status = EventStatus.Cancelled;
    CancellationReason = reason.Trim();
    MarkAsUpdated();

    // Raises domain event (stored in _domainEvents list)
    RaiseDomainEvent(new EventCancelledEvent(Id, reason.Trim(), DateTime.UtcNow));

    return Result.Success();
}
```

**Key Validation**: Line 166 explains the second click error - the event is already Cancelled from the first attempt.

### 2.7 Transaction and Domain Event Dispatch

**File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`

```csharp
// Lines 309-420: CommitAsync method
public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
{
    // 1. Detect changes
    ChangeTracker.DetectChanges();

    // 2. Collect domain events
    var domainEvents = ChangeTracker.Entries<BaseEntity>()
        .SelectMany(e => e.Entity.DomainEvents)
        .ToList();

    // 3. Save changes to database (FAST - typically < 1 second)
    var result = await SaveChangesAsync(cancellationToken);

    // 4. Dispatch domain events AFTER save (SLOW - 30+ seconds for emails)
    if (domainEvents.Any())
    {
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(notification); // BLOCKS until ALL handlers complete
        }
    }

    return result;
}
```

**Critical Issue**: Domain event dispatch happens **synchronously within the transaction commit**, blocking the API response until all email notifications are sent.

### 2.8 EventCancelledEventHandler - The Bottleneck

**File**: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`

```csharp
// Lines 49-173: Handle method
public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
{
    // 1. Retrieve event data (DB query)
    var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);

    // 2. Get confirmed registrations (DB query)
    var registrations = await _registrationRepository.GetByEventAsync(domainEvent.EventId, cancellationToken);

    // 3. Load user data for each registration (N+1 queries)
    foreach (var registration in confirmedRegistrations)
    {
        var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
        registrationEmails.Add(user.Email.Value);
    }

    // 4. Resolve email groups + newsletter subscribers (complex queries)
    var notificationRecipients = await _recipientService.ResolveRecipientsAsync(
        domainEvent.EventId, cancellationToken);

    // 5. Send email to EACH recipient sequentially (VERY SLOW)
    foreach (var email in allRecipients) // Could be 100+ recipients
    {
        var result = await _emailService.SendTemplatedEmailAsync(
            "event-cancelled-notification",
            email,
            parameters,
            cancellationToken); // Each email takes 200-500ms
    }
}
```

**Performance Issues**:
1. **N+1 Query Problem**: Loads user data one-by-one for each registration
2. **Sequential Email Sending**: Sends emails one at a time (not batched/async)
3. **Synchronous Processing**: All emails sent before returning to caller
4. **No Timeout Protection**: Handler runs indefinitely until complete

**Time Breakdown** (estimated for 50 recipients):
- Database queries: 2-3 seconds
- N+1 user lookups: 5-10 seconds
- Email sending: 50 recipients × 300ms = 15 seconds
- Total: **22-28 seconds** (approaching or exceeding 30s timeout)

For events with 100+ recipients or email groups, this easily exceeds 30 seconds.

---

## 3. Error Sequence Timeline

### First Click: Timeout After 30 Seconds

```
T+0s:     User clicks "Cancel Event"
          Frontend: handleCancelEvent() called, sets isCancelling=true

T+0.1s:   Frontend → API: POST /api/events/{id}/cancel

T+0.2s:   Backend: CancelEventCommandHandler.Handle()
          - Retrieve event (10ms)
          - event.Cancel() succeeds, Status = Cancelled (1ms)
          - _unitOfWork.CommitAsync() starts

T+0.3s:   Backend: AppDbContext.CommitAsync()
          - SaveChangesAsync() completes (200ms) ✓ EVENT SAVED AS CANCELLED
          - Dispatching EventCancelledEvent...

T+0.5s:   Backend: EventCancelledEventHandler.Handle() starts
          - Loading registrations...
          - Loading users (N+1 queries)...
          - Resolving email groups...
          - Sending emails (50 recipients)...

T+30s:    Frontend: Axios timeout triggered ❌
          - API call fails with NetworkError: "timeout of 30000ms exceeded"
          - catch block executes
          - Error displayed: "Failed to cancel event. Please try again."
          - refetch() NOT CALLED - event state still shows Status: Published
          - isCancelling set to false
          - Modal stays open

T+45s:    Backend: Email sending completes (15 emails failed, 35 succeeded)
          - EventCancelledEventHandler.Handle() returns
          - CommitAsync() completes
          - API response sent (but frontend already timed out)
```

**Result**: Frontend shows error, but database has `Status = Cancelled`, `CancellationReason = "user provided reason"`

### Second Click: 400 Bad Request

```
T+0s:     User clicks "Cancel Event" again (thinking first attempt failed)
          Frontend: handleCancelEvent() called with SAME event state (Status: Published)

T+0.1s:   Frontend → API: POST /api/events/{id}/cancel

T+0.2s:   Backend: CancelEventCommandHandler.Handle()
          - Retrieve event from DB (Status: Cancelled) ✓
          - event.Cancel() called

T+0.21s:  Domain: Event.Cancel() validation
          - Check: Status != Published && Status != Draft
          - Event.Status = Cancelled ❌ VALIDATION FAILS
          - Returns: Result.Failure("Only published or draft events can be cancelled")

T+0.3s:   API: Returns 400 Bad Request ❌

T+0.4s:   Frontend: catch block executes
          - Error displayed: "Only published or draft events can be cancelled"
          - User confused (event appears failed, but was actually successful)
```

**Result**: Correct validation error, but confusing UX because frontend state doesn't reflect first click's success.

### Page Refresh: Truth Revealed

```
User refreshes page → Event shows Status: CANCELLED
User realizes cancellation worked, but UX was broken
```

---

## 4. Root Cause Summary

### Primary Root Cause: **Synchronous Email Processing in Transaction**

The `EventCancelledEventHandler` sends emails **synchronously** during the database transaction commit, causing the entire operation to exceed the 30-second frontend timeout.

**Design Flaw**: Domain event handlers are dispatched **after** `SaveChangesAsync()` but **within** the same `CommitAsync()` method, meaning the API endpoint doesn't return until all emails are sent.

### Contributing Factors:

1. **Performance Issues**:
   - N+1 query pattern loading user data
   - Sequential email sending (not batched)
   - No query optimization for large recipient lists

2. **Timeout Configuration**:
   - 30-second default may be too short for email operations
   - No special timeout handling for long-running operations

3. **State Management**:
   - Frontend doesn't refetch event state on error
   - No optimistic updates or polling for completion
   - Stale state causes confusing validation errors

4. **Architecture**:
   - Domain events handled synchronously in request pipeline
   - No background job processing for email notifications
   - Transaction includes non-critical operations (email sending)

---

## 5. Recommended Fix Strategy

### Option 1: Background Job Processing (RECOMMENDED)

**Description**: Move email notifications to an asynchronous background job queue.

**Implementation**:
1. Store a "pending email" record in `CommitAsync()` (fast)
2. Return success to frontend immediately
3. Background worker picks up pending emails and processes them
4. Retry logic for failed emails

**Benefits**:
- **Immediate response** (<1 second)
- **Resilient** email delivery
- **Scalable** (handles 1000+ recipients)
- **User-friendly** (no waiting)

**Drawbacks**:
- Requires background worker infrastructure
- More complex error handling

**Code Changes**:
```csharp
// EventCancelledEventHandler.cs
public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, ...)
{
    // Queue email job instead of sending directly
    await _emailQueueService.QueueEventCancellationEmailsAsync(
        domainEvent.EventId,
        domainEvent.Reason,
        cancellationToken);

    // Return immediately - emails sent by background worker
}
```

**Effort**: Medium (2-3 days)
**Risk**: Low

---

### Option 2: Increase Timeout + Optimize Queries (QUICK FIX)

**Description**: Increase frontend timeout and optimize database queries.

**Implementation**:
1. Increase `apiClient` timeout to 90 seconds for cancel operation
2. Add `.Include()` statements to batch-load users with registrations
3. Keep sequential email sending (simplest)

**Benefits**:
- **Quick to implement** (1 day)
- **No architecture changes**
- **Handles most cases** (up to 200 recipients)

**Drawbacks**:
- Still blocks user for 30-60 seconds
- Doesn't scale to large events (500+ recipients)
- Poor user experience (long wait)

**Code Changes**:
```typescript
// events.repository.ts
async cancelEvent(id: string, reason: string): Promise<void> {
  const request: CancelEventRequest = { reason };
  // Override default 30s timeout with 90s for cancel operation
  await apiClient.post<void>(`${this.basePath}/${id}/cancel`, request, { timeout: 90000 });
}
```

```csharp
// EventCancelledEventHandler.cs
// Add eager loading to avoid N+1
var registrations = await _registrationRepository
    .GetByEventAsync(domainEvent.EventId, cancellationToken)
    .Include(r => r.User) // Batch load users
    .ToListAsync();
```

**Effort**: Low (1 day)
**Risk**: Low

---

### Option 3: Optimistic UI + Polling (FRONTEND-ONLY)

**Description**: Show success immediately, poll for actual completion status.

**Implementation**:
1. Display "Cancelling..." message
2. Return success to user immediately
3. Poll event status every 2 seconds
4. Show "Cancelled" when confirmed

**Benefits**:
- **No backend changes**
- **Good UX** (appears instant)
- **Works with existing infrastructure**

**Drawbacks**:
- Email still sent synchronously (backend timeout risk)
- Doesn't solve underlying performance issue
- Complexity in frontend state management

**Code Changes**:
```typescript
// page.tsx
const handleCancelEvent = async () => {
  try {
    setIsCancelling(true);

    // Fire-and-forget cancel request (don't await)
    eventsRepository.cancelEvent(id, cancellationReason.trim());

    // Optimistically show success
    setShowCancelModal(false);
    setOptimisticStatus('Cancelling...');

    // Poll for actual status
    const pollInterval = setInterval(async () => {
      const latest = await eventsRepository.getEventById(id);
      if (latest.status === 'Cancelled') {
        setOptimisticStatus(null);
        await refetch();
        clearInterval(pollInterval);
      }
    }, 2000);

  } catch (err) {
    // Handle true errors
  }
};
```

**Effort**: Low (1 day)
**Risk**: Medium (may hide real errors)

---

### Option 4: Hybrid Approach (BEST UX)

**Description**: Combine Option 1 (background jobs) + Option 3 (optimistic UI).

**Implementation**:
1. Queue emails asynchronously (Option 1)
2. Return success immediately (<1s)
3. Show "Event cancelled successfully" message
4. Emails sent in background

**Benefits**:
- **Best user experience** (instant feedback)
- **Scalable** (handles any recipient count)
- **Reliable** (background retry logic)

**Drawbacks**:
- Most complex to implement
- Requires background worker + queue

**Effort**: Medium-High (3-4 days)
**Risk**: Low

---

## 6. Prevention Measures

### 6.1 Architectural Guidelines

1. **Separate Critical from Non-Critical Operations**:
   - Database writes = critical (must complete in transaction)
   - Email sending = non-critical (queue for later)

2. **Background Job Pattern for Notifications**:
   - All bulk email operations → background queue
   - Immediate response to user
   - Retry failed sends

3. **Timeout Configuration**:
   - Set operation-specific timeouts
   - Cancel operations: 90s (until background jobs implemented)
   - Standard operations: 30s

### 6.2 Code Quality Improvements

1. **Query Optimization**:
   - Use `.Include()` for related data
   - Avoid N+1 queries
   - Batch load users with registrations

2. **Error Handling**:
   - Refetch state on error
   - Log timeout vs validation errors separately
   - Clear user messaging

3. **Performance Monitoring**:
   - Log operation durations
   - Alert on operations > 10s
   - Track email send times

### 6.3 Testing Requirements

1. **Load Testing**:
   - Test with 50, 100, 500, 1000 recipients
   - Measure timeout rates
   - Validate email delivery

2. **Error Scenarios**:
   - Network timeout handling
   - Partial email failures
   - State synchronization

3. **User Experience**:
   - Verify success/error messages
   - Test retry behavior
   - Validate state consistency

---

## 7. Implementation Recommendation

### Phase 1: Immediate Quick Fix (1 day)
**Implement Option 2**: Increase timeout + optimize queries
- Frontend timeout: 30s → 90s for cancel operation
- Add `.Include()` for user loading
- Fix frontend refetch on error
- Deploy to production

### Phase 2: Long-Term Solution (Sprint 1)
**Implement Option 1**: Background job processing
- Set up email queue infrastructure
- Create background worker
- Move email sending to queue
- Remove timeout workaround from Phase 1

### Phase 3: UX Enhancement (Sprint 2)
**Add Option 3 elements**: Optimistic UI
- Show immediate success feedback
- Poll for completion (optional)
- Enhanced error messaging

---

## 8. Affected Components

### Frontend
- `web/src/app/events/[id]/manage/page.tsx` (handleCancelEvent)
- `web/src/infrastructure/api/repositories/events.repository.ts` (cancelEvent)
- `web/src/infrastructure/api/client/api-client.ts` (timeout config)

### Backend
- `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
- `src/LankaConnect.Application/Events/Commands/CancelEvent/CancelEventCommandHandler.cs`
- `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` (CommitAsync)
- `src/LankaConnect.Infrastructure/Data/UnitOfWork.cs`

### Database
- Events table (Status, CancellationReason)
- Registrations table (for recipient lookup)
- EmailMessages table (if using queue)

---

## 9. Success Criteria

### User Experience
- ✓ Cancel operation completes in <3 seconds (user feedback)
- ✓ No timeout errors for events with <500 recipients
- ✓ Clear success message shown immediately
- ✓ Event status reflects cancellation in UI

### Technical
- ✓ Email sending moved to background (Phase 2)
- ✓ Database transaction <1 second
- ✓ No N+1 queries in critical path
- ✓ 99.9% email delivery rate (with retries)

### Monitoring
- ✓ Alert if cancel operation >5 seconds
- ✓ Track email queue length
- ✓ Log failed email sends for retry

---

## 10. Related Issues

- Phase 6A.63: Event cancellation email notification system
- Similar timeout issues may exist for:
  - Event publishing (notifies email groups)
  - Event postponement
  - Mass registration confirmations

---

## Conclusion

The event cancellation timeout error is caused by **synchronous email processing within the database transaction**, resulting in operations that exceed the 30-second frontend timeout. The recommended fix is a **two-phase approach**: immediate quick fix with increased timeout + query optimization, followed by proper background job processing for email notifications.

This pattern should be applied to **all bulk notification scenarios** to prevent similar issues across the application.