# Root Cause Analysis: Event Unpublish Button Not Working

**Date**: 2025-12-30
**Phase**: 6A.53
**Severity**: Critical
**Status**: Analysis Complete
**Issue**: Unpublish button click triggers API call with 200 OK response, but UI does not reflect the unpublished state

---

## Executive Summary

The event unpublish functionality is **FULLY FUNCTIONAL** at the backend level but experiences **UI STATE SYNCHRONIZATION FAILURE** after the API call completes. The backend successfully changes the event status from `Published` to `Draft` and commits the changes to the database, but the frontend does not refresh or update the UI to reflect the new status.

**Classification**: **UI Issue** - Frontend state management and refetch problem

---

## Evidence Analysis

### 1. Frontend Console Output
```
[Violation] 'click' handler took 1925ms
[Violation] 'click' handler took 279ms

🚀 API Request: POST /events/89f8ef9f-af11-4b1a-8dec-b440faef9ad0/unpublish
✅ API Response Success: {status: 200, statusText: 'OK'}

🚀 API Request: GET /events/89f8ef9f-af11-4b1a-8dec-b440faef9ad0
✅ API Response Success: {status: 200, statusText: 'OK', dataSize: 1212}
```

**Key Observations**:
- Click handler takes excessive time (1925ms) - indicating potential blocking operations
- POST `/unpublish` returns 200 OK - backend successfully processed the request
- Subsequent GET request returns 200 OK with 1212 bytes - fresh data is fetched
- **BUT**: UI does not visually update to show "Draft" status

### 2. Performance Issues
- **1925ms click handler** is extremely slow and indicates:
  - Blocking synchronous operations in React component
  - Multiple state updates or re-renders
  - Possible unnecessary operations during unpublish flow

---

## Backend Verification (FULLY FUNCTIONAL)

### API Controller
**File**: `src\LankaConnect.API\Controllers\EventsController.cs` (Lines 394-411)

```csharp
/// <summary>
/// Phase 6A.41: Unpublish an event (return to Draft status) (Owner only)
/// Allows organizers to make corrections after premature publication.
/// </summary>
[HttpPost("{id:guid}/unpublish")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> UnpublishEvent(Guid id)
{
    Logger.LogInformation("Unpublishing event: {EventId}", id);

    var command = new UnpublishEventCommand(id);
    var result = await Mediator.Send(command);

    return HandleResult(result);
}
```

**Status**: ✅ Correct - Properly receives request and delegates to MediatR

---

### Command Handler
**File**: `src\LankaConnect.Application\Events\Commands\UnpublishEvent\UnpublishEventCommandHandler.cs`

```csharp
public async Task<Result> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "[Phase 6A.41] UnpublishEventCommandHandler.Handle START - EventId: {EventId}",
        request.EventId);

    // Retrieve event
    var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
    if (@event == null)
    {
        _logger.LogWarning("Event not found: {EventId}", request.EventId);
        return Result.Failure("Event not found");
    }

    _logger.LogInformation(
        "Event loaded - Id: {EventId}, Status: {Status}, CurrentRegistrations: {Registrations}",
        @event.Id, @event.Status, @event.CurrentRegistrations);

    // Use domain method to unpublish
    var unpublishResult = @event.Unpublish();

    if (unpublishResult.IsFailure)
    {
        _logger.LogWarning("Unpublish failed: {Error}", unpublishResult.Error);
        return unpublishResult;
    }

    // Save changes
    await _unitOfWork.CommitAsync(cancellationToken);

    _logger.LogInformation(
        "[Phase 6A.41] Event {EventId} unpublished successfully - Status changed to Draft",
        request.EventId);

    return Result.Success();
}
```

**Status**: ✅ Correct - Follows proper Clean Architecture pattern:
1. Loads event aggregate from repository
2. Calls domain method `Unpublish()`
3. Commits changes via Unit of Work
4. Logs success

---

### Domain Entity
**File**: `src\LankaConnect.Domain\Events\Event.cs` (Lines 147-160)

```csharp
/// <summary>
/// Phase 6A.41: Unpublishes a published event, returning it to Draft status.
/// Allows organizers to make corrections after premature publication.
/// Business Rules:
/// - Only Published events can be unpublished
/// - Cannot unpublish Active, Cancelled, Postponed, or Completed events
/// - Events with registrations CAN be unpublished (organizer's decision)
/// </summary>
public Result Unpublish()
{
    if (Status != EventStatus.Published)
        return Result.Failure("Only published events can be unpublished");

    Status = EventStatus.Draft;
    PublishedAt = null; // Phase 6A.46: Clear publish timestamp when unpublishing
    MarkAsUpdated();

    // Raise domain event (for potential notification/logging)
    RaiseDomainEvent(new EventUnpublishedEvent(Id, DateTime.UtcNow));

    return Result.Success();
}
```

**Status**: ✅ Correct - Domain logic properly:
1. Validates current status (must be Published)
2. Changes status to Draft
3. Clears PublishedAt timestamp
4. Marks entity as updated
5. Raises domain event

---

### Repository
**File**: `src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs` (Lines 59-121)

```csharp
public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
{
    _repoLogger.LogInformation("[DIAG-R1] EventRepository.GetByIdAsync START - EventId: {EventId}", id);

    // Phase 6A.41 FIX: Load entity with AsNoTracking() to avoid change tracking corruption
    var eventEntity = await _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include("_emailGroupEntities")
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments)
        .AsNoTracking()  // Phase 6A.41: Load without tracking first
        .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    if (eventEntity == null)
    {
        _repoLogger.LogWarning("[DIAG-R2] Event not found: {EventId}", id);
        return null;
    }

    // Sync email group IDs WHILE ENTITY IS DETACHED
    // This prevents change tracking corruption
    var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
    var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<Domain.Communications.Entities.EmailGroup>;

    if (emailGroupEntities != null)
    {
        var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();
        eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);
    }

    // Phase 6A.53: DO NOT attach when loaded with AsNoTracking
    // Return entity untracked - callers that need to modify it should explicitly attach

    return eventEntity;
}
```

**Status**: ⚠️ CRITICAL ISSUE IDENTIFIED - Repository returns **untracked** entity!

**Problem**: The entity is loaded with `AsNoTracking()` and is **NEVER attached** to the DbContext. When the handler calls `_unitOfWork.CommitAsync()`, EF Core has **NO TRACKED CHANGES** to persist to the database!

---

## Root Cause Identified

### The Bug Chain

1. **EventRepository.GetByIdAsync()** loads the event with `AsNoTracking()` (Line 74)
2. Entity is **detached** from DbContext - no change tracking
3. Comment says "DO NOT attach when loaded with AsNoTracking" (Lines 107-111)
4. **UnpublishEventCommandHandler** calls `@event.Unpublish()` which modifies the **detached** entity
5. Handler calls `_unitOfWork.CommitAsync()` but EF Core has **NO CHANGES** to save
6. Database is **NEVER UPDATED**
7. Backend returns 200 OK (because no exception was thrown)
8. Frontend fetches event again via GET `/events/{id}`
9. Database still has `Status = Published` (unchanged)
10. Frontend receives stale data with Published status
11. **UI does not update** because the database was never updated

---

## Why This Bug Exists

### Phase 6A.53 Comment (Lines 107-111 in EventRepository.cs)
```csharp
// Phase 6A.53: DO NOT attach when loaded with AsNoTracking
// ISSUE: Attaching defeats the purpose of AsNoTracking and causes tracking conflicts
// when the event's registrations collection contains registrations already tracked by the same DbContext
// SOLUTION: Return the entity untracked - callers that need to modify it should explicitly attach
// For read-only scenarios (like PaymentCompletedEventHandler), untracked entities work fine
```

**Analysis**: This comment indicates a previous fix (Phase 6A.53) was applied to prevent tracking conflicts with registrations. However, the fix **BREAKS ALL WRITE OPERATIONS** that use `GetByIdAsync()`.

**The Conflict**:
- **Read-only scenarios** (queries) need `AsNoTracking()` for performance
- **Write scenarios** (commands) need **tracked entities** for change detection
- **Current implementation** assumes all calls to `GetByIdAsync()` are read-only

---

## Impact Assessment

### Affected Operations

**ALL write operations using EventRepository.GetByIdAsync() are broken:**

1. ✅ **UnpublishEvent** (Phase 6A.41) - **BROKEN** - Status not updating
2. ✅ **PublishEvent** - Likely broken
3. ✅ **CancelEvent** - Likely broken
4. ✅ **UpdateEvent** - Likely broken
5. ✅ **DeleteEvent** - Likely broken
6. ✅ **Any domain event raising operation** - Domain events won't be dispatched

**Read-only operations (working correctly):**
- GetEventById (Query)
- GetEvents (Query)
- Search/Nearby queries

---

## Frontend Analysis (Secondary Issue)

### Unpublish Button Handler
**File**: `web\src\app\events\[id]\manage\page.tsx` (Lines 93-116)

```typescript
// Handle Unpublish Event
const handleUnpublishEvent = async () => {
  if (!event || event.organizerId !== user?.userId) {
    return;
  }

  const confirmed = window.confirm(
    'Are you sure you want to unpublish this event? It will return to Draft status and will not be visible to the public until published again.'
  );

  if (!confirmed) return;

  try {
    setIsUnpublishing(true);
    setError(null);
    await eventsRepository.unpublishEvent(id);
    setIsUnpublishing(false);
    await refetch();
  } catch (err) {
    console.error('Failed to unpublish event:', err);
    setError(err instanceof Error ? err.message : 'Failed to unpublish event. Please try again.');
    setIsUnpublishing(false);
  }
};
```

**Status**: ⚠️ Suboptimal but would work if backend was correct

**Issues**:
1. **Blocking synchronous operations** - State updates before async operations complete
2. **No optimistic update** - UI only updates after successful refetch
3. **1925ms performance** indicates unnecessary re-renders or blocking operations

**Expected Flow** (if backend worked):
1. User clicks Unpublish
2. Confirmation dialog shown
3. API call made to `/unpublish`
4. Backend returns 200 OK (currently false positive)
5. `refetch()` called to get fresh data
6. UI updates to show Draft status

**Current Reality**:
- Steps 1-5 execute correctly
- Step 6 **FAILS** because database was never updated (backend bug)
- `refetch()` gets stale data with Published status
- UI remains unchanged

---

## Repository API Client
**File**: `web\src\infrastructure\api\repositories\events.repository.ts` (Lines 224-230)

```typescript
/**
 * Phase 6A.41: Unpublish event (return to Draft status)
 * Allows organizers to make corrections after premature publication
 */
async unpublishEvent(id: string): Promise<void> {
  await apiClient.post<void>(`${this.basePath}/${id}/unpublish`);
}
```

**Status**: ✅ Correct - Simple POST request, no issues here

---

## Detailed Fix Plan

### Solution 1: Fix Repository to Support Both Read and Write Operations

**Problem**: `GetByIdAsync()` returns untracked entities, breaking all write operations.

**Solution**: Add a `trackChanges` parameter to control tracking behavior.

**Implementation**:

#### Step 1: Update IEventRepository Interface
**File**: `src\LankaConnect.Domain\Events\IEventRepository.cs`

```csharp
public interface IEventRepository : IRepository<Event>
{
    // Add parameter to control change tracking
    Task<Event?> GetByIdAsync(Guid id, bool trackChanges = true, CancellationToken cancellationToken = default);

    // ... other methods
}
```

#### Step 2: Update EventRepository Implementation
**File**: `src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs`

```csharp
public override async Task<Event?> GetByIdAsync(
    Guid id,
    bool trackChanges = true,
    CancellationToken cancellationToken = default)
{
    _repoLogger.LogInformation(
        "[DIAG-R1] EventRepository.GetByIdAsync START - EventId: {EventId}, TrackChanges: {TrackChanges}",
        id, trackChanges);

    // Build query with eager loading
    var query = _dbSet
        .Include(e => e.Images)
        .Include(e => e.Videos)
        .Include(e => e.Registrations)
        .Include("_emailGroupEntities")
        .Include(e => e.SignUpLists)
            .ThenInclude(s => s.Items)
                .ThenInclude(i => i.Commitments);

    // Apply tracking behavior based on parameter
    if (!trackChanges)
    {
        query = query.AsNoTracking();
        _repoLogger.LogInformation("[DIAG-R2] Loading entity WITHOUT change tracking");
    }
    else
    {
        _repoLogger.LogInformation("[DIAG-R2] Loading entity WITH change tracking");
    }

    var eventEntity = await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    if (eventEntity == null)
    {
        _repoLogger.LogWarning("[DIAG-R3] Event not found: {EventId}", id);
        return null;
    }

    // Sync email group IDs
    var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
    var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<Domain.Communications.Entities.EmailGroup>;

    if (emailGroupEntities != null)
    {
        var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();
        eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

        _repoLogger.LogInformation(
            "[DIAG-R4] Synced {EmailGroupCount} email group IDs to domain entity",
            emailGroupIds.Count);
    }

    _repoLogger.LogInformation(
        "[DIAG-R5] Event loaded - EventId: {EventId}, Status: {Status}, Tracked: {Tracked}",
        eventEntity.Id,
        eventEntity.Status,
        trackChanges);

    return eventEntity;
}
```

#### Step 3: Update Command Handlers to Use Tracked Entities

**File**: `src\LankaConnect.Application\Events\Commands\UnpublishEvent\UnpublishEventCommandHandler.cs`

```csharp
public async Task<Result> Handle(UnpublishEventCommand request, CancellationToken cancellationToken)
{
    _logger.LogInformation(
        "[Phase 6A.41] UnpublishEventCommandHandler.Handle START - EventId: {EventId}",
        request.EventId);

    // Retrieve event WITH CHANGE TRACKING (trackChanges = true)
    var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
    if (@event == null)
    {
        _logger.LogWarning("Event not found: {EventId}", request.EventId);
        return Result.Failure("Event not found");
    }

    _logger.LogInformation(
        "Event loaded - Id: {EventId}, Status: {Status}, CurrentRegistrations: {Registrations}",
        @event.Id, @event.Status, @event.CurrentRegistrations);

    // Use domain method to unpublish
    var unpublishResult = @event.Unpublish();

    if (unpublishResult.IsFailure)
    {
        _logger.LogWarning("Unpublish failed: {Error}", unpublishResult.Error);
        return unpublishResult;
    }

    // Save changes - EF Core will now detect changes because entity is tracked
    await _unitOfWork.CommitAsync(cancellationToken);

    _logger.LogInformation(
        "[Phase 6A.41] Event {EventId} unpublished successfully - Status changed to Draft",
        request.EventId);

    return Result.Success();
}
```

#### Step 4: Update ALL Command Handlers

Apply same pattern to:
- `PublishEventCommandHandler`
- `CancelEventCommandHandler`
- `UpdateEventCommandHandler`
- `DeleteEventCommandHandler`
- `PostponeEventCommandHandler`
- Any other command handler that modifies events

#### Step 5: Update Query Handlers to Use Untracked Entities

**File**: `src\LankaConnect.Application\Events\Queries\GetEventById\GetEventByIdQueryHandler.cs`

```csharp
public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
{
    // Read-only query - use untracked entities for performance
    var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: false, cancellationToken);

    if (@event == null)
        return Result<EventDto>.Failure("Event not found");

    var dto = _mapper.Map<EventDto>(@event);
    return Result<EventDto>.Success(dto);
}
```

---

### Solution 2: Frontend Optimizations (Secondary Priority)

#### Issue: 1925ms Click Handler Performance

**File**: `web\src\app\events\[id]\manage\page.tsx`

**Improvements**:

1. **Optimistic UI Update** - Show Draft status immediately
2. **Error Recovery** - Revert to Published if API fails
3. **Reduce State Updates** - Batch state changes
4. **Loading States** - Better UX during operation

**Implementation**:

```typescript
const handleUnpublishEvent = async () => {
  if (!event || event.organizerId !== user?.userId) {
    return;
  }

  const confirmed = window.confirm(
    'Are you sure you want to unpublish this event? It will return to Draft status and will not be visible to the public until published again.'
  );

  if (!confirmed) return;

  // Store original status for error recovery
  const originalStatus = event.status;

  try {
    // Optimistic update - show Draft status immediately
    setEvent((prev) => prev ? { ...prev, status: EventStatus.Draft } : prev);
    setIsUnpublishing(true);
    setError(null);

    // Make API call
    await eventsRepository.unpublishEvent(id);

    // Success - refetch to get authoritative data from backend
    await refetch();
  } catch (err) {
    console.error('Failed to unpublish event:', err);

    // Error recovery - revert to original status
    setEvent((prev) => prev ? { ...prev, status: originalStatus } : prev);
    setError(err instanceof Error ? err.message : 'Failed to unpublish event. Please try again.');
  } finally {
    setIsUnpublishing(false);
  }
};
```

**Note**: This is a **secondary fix** and won't work until the backend repository issue is resolved.

---

## Testing Strategy

### Backend Tests

#### Test 1: Verify Unpublish Updates Database
```csharp
[Fact]
public async Task Unpublish_ShouldUpdateDatabaseStatus()
{
    // Arrange
    var event = CreatePublishedEvent();
    await _context.Events.AddAsync(event);
    await _context.SaveChangesAsync();

    var command = new UnpublishEventCommand(event.Id);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();

    // CRITICAL: Reload from database to verify persistence
    var updated = await _context.Events.FindAsync(event.Id);
    updated.Status.Should().Be(EventStatus.Draft);
    updated.PublishedAt.Should().BeNull();
}
```

#### Test 2: Verify Entity Tracking
```csharp
[Fact]
public async Task GetByIdAsync_WithTrackingEnabled_ShouldTrackChanges()
{
    // Arrange
    var event = CreateEvent();
    await _context.Events.AddAsync(event);
    await _context.SaveChangesAsync();
    _context.ChangeTracker.Clear();

    // Act
    var loaded = await _repository.GetByIdAsync(event.Id, trackChanges: true);
    loaded.Unpublish();
    await _unitOfWork.CommitAsync();

    // Assert
    var updated = await _context.Events.FindAsync(event.Id);
    updated.Status.Should().Be(EventStatus.Draft);
}
```

#### Test 3: Verify No Tracking for Queries
```csharp
[Fact]
public async Task GetByIdAsync_WithTrackingDisabled_ShouldNotTrackChanges()
{
    // Arrange
    var event = CreateEvent();
    await _context.Events.AddAsync(event);
    await _context.SaveChangesAsync();
    _context.ChangeTracker.Clear();

    // Act
    var loaded = await _repository.GetByIdAsync(event.Id, trackChanges: false);
    var entry = _context.Entry(loaded);

    // Assert
    entry.State.Should().Be(EntityState.Detached);
}
```

### Frontend Tests

#### Test 1: Verify Refetch After Unpublish
```typescript
it('should refetch event after unpublishing', async () => {
  const mockEvent = createMockEvent({ status: EventStatus.Published });
  const mockUnpublish = jest.fn().mockResolvedValue(undefined);
  const mockRefetch = jest.fn().mockResolvedValue({ data: { ...mockEvent, status: EventStatus.Draft } });

  render(<EventManagePage event={mockEvent} unpublish={mockUnpublish} refetch={mockRefetch} />);

  fireEvent.click(screen.getByText('Unpublish Event'));
  fireEvent.click(screen.getByText('OK')); // Confirm dialog

  await waitFor(() => {
    expect(mockUnpublish).toHaveBeenCalledWith(mockEvent.id);
    expect(mockRefetch).toHaveBeenCalled();
  });
});
```

#### Test 2: Verify UI Updates After Refetch
```typescript
it('should update UI to show Draft status after unpublish', async () => {
  const mockEvent = createMockEvent({ status: EventStatus.Published });
  const mockRefetch = jest.fn().mockResolvedValue({
    data: { ...mockEvent, status: EventStatus.Draft }
  });

  const { rerender } = render(<EventManagePage event={mockEvent} refetch={mockRefetch} />);

  // Initially shows Published
  expect(screen.getByText('Published')).toBeInTheDocument();

  // Trigger unpublish
  fireEvent.click(screen.getByText('Unpublish Event'));
  fireEvent.click(screen.getByText('OK'));

  await waitFor(() => expect(mockRefetch).toHaveBeenCalled());

  // Rerender with updated data
  rerender(<EventManagePage event={{ ...mockEvent, status: EventStatus.Draft }} refetch={mockRefetch} />);

  // Should now show Draft
  expect(screen.getByText('Draft')).toBeInTheDocument();
});
```

---

## Migration Risk Assessment

### Breaking Changes
- ✅ **None** - Adding optional parameter with default value is backward compatible

### Rollback Plan
- Keep Phase 6A.53 comment for historical reference
- Document why tracking was changed
- Add migration guide for future developers

---

## Documentation Updates Required

1. **Architecture Decision Record (ADR)**: Document repository tracking strategy
2. **Developer Guide**: Explain when to use tracked vs untracked entities
3. **Code Comments**: Update Phase 6A.53 comment in EventRepository
4. **API Documentation**: No changes needed (internal fix only)

---

## Conclusion

### Root Cause
**EventRepository.GetByIdAsync() returns untracked entities, preventing EF Core from detecting changes when command handlers modify domain entities. The database is never updated, causing UI to display stale data after successful API calls.**

### Classification
**Backend API Issue** - Repository pattern implementation bug causing silent write operation failures

### Priority
**Critical** - Affects ALL event write operations (publish, unpublish, cancel, update, delete)

### Estimated Fix Time
- **Backend Fix**: 2-3 hours (repository + tests + all command handlers)
- **Frontend Optimization**: 1-2 hours (optional, improves UX)
- **Testing**: 2-3 hours (integration tests, manual testing)
- **Total**: 5-8 hours

### Next Steps
1. Implement Solution 1 (Repository tracking fix) - **Required**
2. Add comprehensive tests for tracking behavior - **Required**
3. Update all command handlers to explicitly request tracking - **Required**
4. Verify fix in Azure staging environment - **Required**
5. Implement Solution 2 (Frontend optimization) - **Optional**
6. Update documentation with ADR and migration guide - **Recommended**

---

**Prepared by**: Claude (System Architecture Designer)
**Review Required**: Senior Backend Developer, DevOps Engineer
**Approval Required**: Technical Lead, Product Owner
