# Root Cause Analysis: Issue #81 - Newsletter Event Dropdown Shows All Events

**Date**: 2026-02-15
**Issue**: GitHub Issue #81
**Status**: ✅ ROOT CAUSE IDENTIFIED
**Severity**: 🔴 HIGH (Security/Authorization Issue)

---

## Executive Summary

When an Event Organizer creates/updates a reminder newsletter from the Communications tab, the "select event" dropdown shows **ALL events in the system** instead of only events created by the logged-in organizer. This is a **security and UX issue** that allows organizers to see and potentially link to events they don't own.

### Root Cause
**UI is calling the wrong API endpoint**. The `NewsletterForm.tsx` component uses `useEvents({})` which calls the **public** `GET /api/Events` endpoint without any organizer filtering.

### Security Impact
- ⚠️ **Information Disclosure**: Organizers can see titles of all events (including private/draft events from other organizers)
- ⚠️ **Potential Data Leak**: If an organizer links a newsletter to someone else's event, the newsletter system may incorrectly send emails to that event's attendees
- ⚠️ **Authorization Bypass**: Organizers can access event information they shouldn't have permission to view

---

## Issue Details

### Reported Behavior
1. Login as Event Organizer
2. Create an event from Dashboard → Event Management
3. Publish the event
4. Go to 'Communications' tab of the event
5. Click 'Create/Update Reminder Newsletter' button
6. **PROBLEM**: The 'select event' dropdown shows ALL events in the system

### Expected Behavior
The dropdown should **ONLY show events created by/belonging to the logged-in Event Organizer**.

---

## Technical Analysis

### 1. Frontend Code Review

**File**: `web/src/presentation/components/features/newsletters/NewsletterForm.tsx`

**Line 59**:
```typescript
const { data: events = [], isLoading: isLoadingEvents } = useEvents({});
```

**Problem**:
- Calls `useEvents({})` with **empty filters**
- This fetches ALL events using the public `GET /api/Events` endpoint
- No organizer filtering applied

**Line 304-308** (Event Dropdown):
```tsx
<option value="">No event linkage</option>
{events.map((event) => (
  <option key={event.id} value={event.id}>
    {event.title}
  </option>
))}
```

**Problem**:
- Displays all fetched events without any client-side filtering
- No check for event ownership

---

### 2. Backend API Analysis

#### Current Implementation (WRONG)

**Endpoint Called**: `GET /api/Events`
**Controller**: `EventsController.cs` (Line 122)
**Handler**: `GetEventsQueryHandler.cs`

**Code Flow**:
```csharp
[HttpGet]
public async Task<IActionResult> GetEvents(
    [FromQuery] EventStatus? status = null,
    [FromQuery] EventCategory? category = null,
    // ... other filters ...
    [FromQuery] Guid? userId = null, // ⚠️ NOT used for organizer filtering
    // ...
)
{
    var authenticatedUserId = User.Identity?.IsAuthenticated == true
        ? User.GetUserId()
        : (Guid?)null;

    var query = new GetEventsQuery(...); // ⚠️ No OrganizerId filter!
    var result = await Mediator.Send(query);
    return HandleResult(result);
}
```

**Analysis**:
- `userId` parameter is used for **location-based sorting**, NOT organizer filtering
- `authenticatedUserId` is used to populate `UserRegistrationStatus`, NOT to filter events
- **No organizer filtering logic exists** in this endpoint

---

#### Correct Endpoint (EXISTS BUT NOT USED)

**Endpoint**: `GET /api/Events/my-events`
**Controller**: `EventsController.cs` (Line 874-909)
**Handler**: `GetEventsByOrganizerQueryHandler.cs`

**Code Flow**:
```csharp
/// <summary>
/// Get events created by current user (Authenticated Event Organizers/Admins)
/// Phase 6A.47: Added filtering support (search, category, dates, location)
/// Issue #36: Added StatusFilter for user-friendly status filtering
/// </summary>
[HttpGet("my-events")]
[Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
public async Task<IActionResult> GetMyEvents(
    [FromQuery] string? searchTerm = null,
    [FromQuery] EventCategory? category = null,
    [FromQuery] DateTime? startDateFrom = null,
    [FromQuery] DateTime? startDateTo = null,
    [FromQuery] string? state = null,
    [FromQuery] List<Guid>? metroAreaIds = null,
    [FromQuery] EventStatusFilter? statusFilter = null)
{
    var userId = User.GetUserId();

    // ✅ CORRECT: Filters by OrganizerId from JWT token
    var query = new GetEventsByOrganizerQuery(
        userId,
        searchTerm,
        category,
        startDateFrom,
        startDateTo,
        state,
        metroAreaIds,
        statusFilter);

    var result = await Mediator.Send(query);
    return HandleResult(result);
}
```

**Analysis**:
- ✅ Requires authentication (`[Authorize]`)
- ✅ Extracts organizer ID from JWT token (`User.GetUserId()`)
- ✅ Filters events by `OrganizerId` in query handler
- ✅ Returns ONLY events created by the logged-in organizer

---

### 3. Query Handler Analysis

**File**: `GetEventsByOrganizerQueryHandler.cs`

**Key Logic** (Line ~60-70):
```csharp
public async Task<Result<IReadOnlyList<EventDto>>> Handle(
    GetEventsByOrganizerQuery request,
    CancellationToken cancellationToken)
{
    // ✅ CRITICAL: Filters by OrganizerId
    var events = await _eventRepository.GetEventsByOrganizerAsync(
        request.OrganizerId,
        cancellationToken);

    // Additional filtering (search, category, dates, etc.)
    // ...

    return Result<IReadOnlyList<EventDto>>.Success(filteredEvents);
}
```

**Repository Method** (EventRepository.cs):
```csharp
public async Task<IReadOnlyList<Event>> GetEventsByOrganizerAsync(
    Guid organizerId,
    CancellationToken cancellationToken = default)
{
    return await _dbContext.Events
        .Where(e => e.OrganizerId == organizerId) // ✅ CRITICAL FILTER
        .Include(e => e.Location)
        .Include(e => e.Images)
        .ToListAsync(cancellationToken);
}
```

---

## Root Cause Summary

| Component | Current Behavior | Correct Behavior |
|-----------|-----------------|------------------|
| **Frontend** | Calls `useEvents({})` | Should call organizer-specific hook |
| **API Endpoint** | `GET /api/Events` (public) | Should use `GET /api/Events/my-events` |
| **Filtering** | ❌ None (returns all events) | ✅ Filter by `OrganizerId` from JWT |
| **Authorization** | ❌ Anonymous access allowed | ✅ `[Authorize]` attribute required |

---

## Impact Analysis

### Security Implications

1. **Information Disclosure** (Medium Risk)
   - Event titles and IDs are exposed to unauthorized organizers
   - Draft/private events from other organizers are visible
   - Mitigation: Events are not fully detailed, only titles shown

2. **Potential Authorization Bypass** (High Risk)
   - If an organizer selects another organizer's event:
     - Newsletter gets linked to wrong event
     - Email recipients may include attendees from the wrong event
     - This could spam users who didn't register for the organizer's event
   - Mitigation: Backend should validate event ownership when creating newsletter

3. **Data Integrity** (Medium Risk)
   - Incorrect event associations in newsletter data
   - Confusing analytics and reporting

### UX Implications

1. **Confusion**: Organizers see hundreds of irrelevant events
2. **Poor Performance**: Dropdown loads ALL events (potentially thousands)
3. **Accidental Errors**: Easy to select wrong event

---

## Fix Plan

### Phase 1: Immediate Frontend Fix (HIGH PRIORITY)

**File**: `web/src/presentation/components/features/newsletters/NewsletterForm.tsx`

**Change Required**:
```typescript
// ❌ BEFORE (Line 59)
const { data: events = [], isLoading: isLoadingEvents } = useEvents({});

// ✅ AFTER
const { data: events = [], isLoading: isLoadingEvents } = useMyEvents();
```

**Create New Hook**: `web/src/presentation/hooks/useEvents.ts`

Add the following hook:

```typescript
/**
 * useMyEvents Hook
 *
 * Fetches events created by the authenticated organizer
 * Used for newsletter creation, event management dashboard, etc.
 *
 * Features:
 * - Automatic caching with 5-minute stale time
 * - Requires authentication
 * - Filters by organizer ID (from JWT token)
 *
 * @param filters - Optional filters (category, date range, search term)
 *
 * @example
 * ```tsx
 * const { data: myEvents } = useMyEvents({ category: 'Cultural' });
 * ```
 */
export function useMyEvents(
  filters?: {
    searchTerm?: string;
    category?: EventCategory;
    startDateFrom?: string;
    startDateTo?: string;
    state?: string;
    metroAreaIds?: string[];
    statusFilter?: EventStatusFilter;
  },
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: ['my-events', filters || {}] as const,
    queryFn: async () => {
      const result = await eventsRepository.getMyEvents(filters);
      return result;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1,
    ...options,
  });
}
```

**Update Repository**: `web/src/infrastructure/api/repositories/events.repository.ts`

Add method:

```typescript
/**
 * Get events created by the authenticated organizer
 * Maps to backend GET /api/Events/my-events
 */
async getMyEvents(filters?: {
  searchTerm?: string;
  category?: number;
  startDateFrom?: string;
  startDateTo?: string;
  state?: string;
  metroAreaIds?: string[];
  statusFilter?: number;
}): Promise<EventDto[]> {
  const params = new URLSearchParams();

  if (filters?.searchTerm) params.append('searchTerm', filters.searchTerm);
  if (filters?.category !== undefined) params.append('category', String(filters.category));
  if (filters?.startDateFrom) params.append('startDateFrom', filters.startDateFrom);
  if (filters?.startDateTo) params.append('startDateTo', filters.startDateTo);
  if (filters?.state) params.append('state', filters.state);
  if (filters?.metroAreaIds && filters.metroAreaIds.length > 0) {
    filters.metroAreaIds.forEach(id => params.append('metroAreaIds', id));
  }
  if (filters?.statusFilter !== undefined) params.append('statusFilter', String(filters.statusFilter));

  const queryString = params.toString();
  const url = queryString ? `${this.basePath}/my-events?${queryString}` : `${this.basePath}/my-events`;

  return await apiClient.get<EventDto[]>(url);
}
```

---

### Phase 2: Backend Validation (CRITICAL FOR SECURITY)

**File**: `src/LankaConnect.Application/Communications/Commands/CreateNewsletter/CreateNewsletterCommandHandler.cs`

**Add Validation** (Before creating newsletter):

```csharp
public async Task<Result<Guid>> Handle(
    CreateNewsletterCommand request,
    CancellationToken cancellationToken)
{
    // Get authenticated user ID (newsletter creator/organizer)
    var creatorId = _currentUserService.UserId;

    // PHASE 6A.114 ISSUE #81 FIX: Validate event ownership
    if (request.EventId.HasValue)
    {
        var linkedEvent = await _eventRepository.GetByIdAsync(
            request.EventId.Value,
            cancellationToken);

        if (linkedEvent == null)
        {
            return Result<Guid>.Failure(
                new Error("Newsletter.EventNotFound",
                "The selected event does not exist."));
        }

        // ✅ CRITICAL: Verify organizer owns the event
        if (linkedEvent.OrganizerId != creatorId)
        {
            _logger.LogWarning(
                "SECURITY: User {UserId} attempted to link newsletter to event {EventId} owned by {OwnerId}",
                creatorId, linkedEvent.Id, linkedEvent.OrganizerId);

            return Result<Guid>.Failure(
                new Error("Newsletter.UnauthorizedEventAccess",
                "You can only link newsletters to events you created."));
        }
    }

    // Continue with newsletter creation...
}
```

**Same validation needed in**: `UpdateNewsletterCommandHandler.cs`

---

### Phase 3: Testing Strategy

#### Unit Tests

**File**: `tests/LankaConnect.Application.Tests/Communications/Commands/CreateNewsletterCommandHandlerTests.cs`

Add test:

```csharp
[Fact]
public async Task Handle_WhenEventNotOwnedByUser_ShouldReturnFailure()
{
    // Arrange
    var organizerId = Guid.NewGuid();
    var otherOrganizerId = Guid.NewGuid();
    var eventId = Guid.NewGuid();

    _currentUserServiceMock
        .Setup(x => x.UserId)
        .Returns(organizerId);

    var otherOrganizerEvent = EventBuilder.Create()
        .WithId(eventId)
        .WithOrganizerId(otherOrganizerId) // Different organizer!
        .Build();

    _eventRepositoryMock
        .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(otherOrganizerEvent);

    var command = new CreateNewsletterCommand(
        Title: "Test Newsletter",
        Description: "Test",
        EmailGroupIds: new List<Guid>(),
        IncludeNewsletterSubscribers: true,
        EventId: eventId, // ⚠️ Trying to link to someone else's event
        MetroAreaIds: null,
        TargetAllLocations: true,
        IsAnnouncementOnly: false);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Code.Should().Be("Newsletter.UnauthorizedEventAccess");
    result.Error.Message.Should().Contain("You can only link newsletters to events you created");
}
```

#### Integration Tests

**Test Scenarios**:

1. ✅ **Organizer creates newsletter linked to their own event** → Success
2. ❌ **Organizer creates newsletter linked to another organizer's event** → 403 Forbidden
3. ✅ **Newsletter form dropdown only shows organizer's events** → Verify count and IDs
4. ✅ **Public events page shows all events** → Verify `GET /api/Events` still works for public

#### Manual Testing

**Test Script**:

```
1. Login as Organizer A (e.g., niroshhh@gmail.com)
2. Create Event "Test Event A"
3. Publish Event "Test Event A"
4. Login as Organizer B (different account)
5. Create Event "Test Event B"
6. Publish Event "Test Event B"
7. Go to Communications tab of "Test Event B"
8. Click "Create/Update Reminder Newsletter"
9. ✅ VERIFY: Event dropdown ONLY shows "Test Event B"
10. ❌ VERIFY: "Test Event A" is NOT in dropdown
11. Attempt to manually craft API request linking to "Test Event A"
12. ❌ VERIFY: Backend returns 403 Forbidden
```

---

### Phase 4: Additional Security Measures

1. **Authorization Check in Newsletter Queries**
   - `GetNewslettersByEventQuery`: Verify user owns the event
   - `GetNewsletterByIdQuery`: Verify user owns the linked event or is admin

2. **Audit Logging**
   - Log all attempts to link newsletters to events
   - Alert on unauthorized access attempts

3. **API Rate Limiting**
   - Prevent brute-force enumeration of event IDs

---

## Implementation Checklist

### Frontend Changes
- [ ] Create `useMyEvents()` hook in `useEvents.ts`
- [ ] Add `getMyEvents()` method to `events.repository.ts`
- [ ] Update `NewsletterForm.tsx` to use `useMyEvents({})`
- [ ] Test dropdown shows only organizer's events
- [ ] Verify loading states and error handling

### Backend Changes
- [ ] Add event ownership validation to `CreateNewsletterCommandHandler.cs`
- [ ] Add event ownership validation to `UpdateNewsletterCommandHandler.cs`
- [ ] Add unit tests for unauthorized event access
- [ ] Add integration tests for newsletter creation flow
- [ ] Add security audit logging

### Testing
- [ ] Unit tests pass (90%+ coverage)
- [ ] Integration tests pass
- [ ] Manual testing completed
- [ ] Security review completed
- [ ] Performance testing (dropdown load time)

### Documentation
- [ ] Update `PROGRESS_TRACKER.md`
- [ ] Update `STREAMLINED_ACTION_PLAN.md`
- [ ] Update API documentation (Swagger)
- [ ] Update security documentation

### Deployment
- [ ] Deploy to staging
- [ ] Test in staging environment
- [ ] Monitor logs for errors
- [ ] Deploy to production
- [ ] Post-deployment verification

---

## Related Issues

- None currently tracked

---

## Estimated Effort

- **Frontend Fix**: 2-3 hours
- **Backend Validation**: 2-3 hours
- **Testing**: 3-4 hours
- **Documentation**: 1 hour
- **Total**: 8-11 hours (1-1.5 days)

---

## Priority Justification

**Priority**: 🔴 HIGH

**Reasons**:
1. **Security Issue**: Potential information disclosure and authorization bypass
2. **User Impact**: Affects all Event Organizers using newsletter feature
3. **Data Integrity**: Risk of incorrect event-newsletter associations
4. **Quick Fix**: Solution is straightforward (use existing endpoint)

---

## Conclusion

This issue represents a **critical security and UX flaw** in the newsletter creation flow. The root cause is clear: **the frontend is calling the wrong API endpoint**. The fix is straightforward because the correct endpoint (`GET /api/Events/my-events`) already exists and is properly secured.

The recommended approach is:
1. **Immediate**: Switch frontend to use `useMyEvents()` hook
2. **Critical**: Add backend validation to prevent unauthorized event linking
3. **Follow-up**: Add comprehensive tests and security auditing

**Status**: Ready for implementation (Phase 6A.114)

---

**Document Version**: 1.0
**Last Updated**: 2026-02-15
**Author**: Claude (SPARC Architecture Agent)