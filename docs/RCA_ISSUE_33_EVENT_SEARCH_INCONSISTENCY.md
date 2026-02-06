# Root Cause Analysis: Issue #33 - Event Search Inconsistent Results

## Issue Summary

**GitHub Issue**: #33 - Event search and Event management search do not return correct results

**Problem Statement**: Two different search behaviors observed:
1. Event Search in Dashboard page (Event Management tab)
2. Event Search in Events page (/events)

Both searches appear to return inconsistent results when searching for events.

---

## Investigation Scope

### Files Analyzed

**Frontend Components:**
- `c:\Work\LankaConnect\web\src\app\(dashboard)\dashboard\page.tsx` - Dashboard page
- `c:\Work\LankaConnect\web\src\app\events\page.tsx` - Events listing page
- `c:\Work\LankaConnect\web\src\components\events\filters\EventFilters.tsx` - Shared filter component
- `c:\Work\LankaConnect\web\src\infrastructure\api\repositories\events.repository.ts` - Frontend API repository
- `c:\Work\LankaConnect\web\src\presentation\hooks\useEvents.ts` - React Query hooks

**Backend Endpoints:**
- `c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EventsController.cs` - API controller
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEvents\GetEventsQuery.cs` - GetEvents query
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEvents\GetEventsQueryHandler.cs` - GetEvents handler
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEventsByOrganizer\GetEventsByOrganizerQuery.cs` - GetEventsByOrganizer query
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEventsByOrganizer\GetEventsByOrganizerQueryHandler.cs` - GetEventsByOrganizer handler
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetMyRegisteredEvents\GetMyRegisteredEventsQuery.cs` - GetMyRegisteredEvents query
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetMyRegisteredEvents\GetMyRegisteredEventsQueryHandler.cs` - GetMyRegisteredEvents handler
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\SearchEvents\SearchEventsQuery.cs` - SearchEvents query
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\SearchEvents\SearchEventsQueryHandler.cs` - SearchEvents handler
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs` - Event repository with SearchAsync

---

## Architecture Overview

### Search Flow Comparison

#### 1. Events Page (/events) - Public Event Search

```
User Input -> SearchInput (500ms debounce)
    -> useEvents hook (filters object)
    -> eventsRepository.getEvents(filters)
    -> GET /api/events?searchTerm=X&...
    -> GetEventsQuery (with SearchTerm)
    -> GetEventsQueryHandler
        -> If SearchTerm provided: eventRepository.SearchAsync()
        -> Else: GetFilteredEventsAsync() with traditional filters
    -> Return EventDto[]
```

**Key Characteristics:**
- Uses `useEvents` hook with `GetEventsRequest` filters
- Calls `GET /api/events` endpoint
- Backend uses `GetEventsQuery` which delegates to `SearchAsync()` for text search
- Default date filter: `'upcoming'` (future events only)
- Excludes `Draft` and `UnderReview` events (default behavior)
- No user-specific filtering (shows all public events)

#### 2. Dashboard - Event Management Tab (Organizer's Events)

```
User Input -> EventFilters (300ms debounce)
    -> filtersToApiParams(createdFilters)
    -> eventsRepository.getUserCreatedEvents(apiParams)
    -> GET /api/events/my-events?searchTerm=X&...
    -> GetEventsByOrganizerQuery (with SearchTerm)
    -> GetEventsByOrganizerQueryHandler
        -> Get all organizer's event IDs first
        -> If filters: delegate to GetEventsQuery with IncludeAllStatuses=true
        -> Filter results to only organizer's events
    -> Return EventDto[]
```

**Key Characteristics:**
- Uses `filtersToApiParams()` utility for parameter conversion
- Calls `GET /api/events/my-events` endpoint
- Backend uses `GetEventsByOrganizerQuery`
- Default date filter: `'all'` (shows all events regardless of date)
- Includes `Draft` and `UnderReview` events (via `IncludeAllStatuses=true`)
- Filters to only events created by current user

#### 3. Dashboard - My Registered Events Tab (User's RSVPs)

```
User Input -> EventFilters (300ms debounce)
    -> filtersToApiParams(registeredFilters)
    -> eventsRepository.getUserRsvps(apiParams)
    -> GET /api/events/my-rsvps?searchTerm=X&...
    -> GetMyRegisteredEventsQuery (with SearchTerm)
    -> GetMyRegisteredEventsQueryHandler
        -> Get all user's registered event IDs first
        -> If filters: delegate to GetEventsQuery (default status filter)
        -> Filter results to only registered events
    -> Return EventDto[]
```

**Key Characteristics:**
- Uses `filtersToApiParams()` utility for parameter conversion
- Calls `GET /api/events/my-rsvps` endpoint
- Backend uses `GetMyRegisteredEventsQuery`
- Default date filter: `'upcoming'` (future events only)
- Does NOT include Draft/UnderReview (uses default GetEventsQuery behavior)
- Filters to only events user has registered for

---

## Root Cause Analysis

### Identified Differences Causing Inconsistent Results

#### 1. **Different API Endpoints**

| Context | Endpoint | Query Handler |
|---------|----------|---------------|
| Events Page | `GET /api/events` | `GetEventsQueryHandler` |
| Dashboard - Event Management | `GET /api/events/my-events` | `GetEventsByOrganizerQueryHandler` |
| Dashboard - My Registered Events | `GET /api/events/my-rsvps` | `GetMyRegisteredEventsQueryHandler` |

**Impact**: Each endpoint uses a different query handler with slightly different filtering logic.

#### 2. **Different Status Filtering**

| Context | Status Filter | Rationale |
|---------|---------------|-----------|
| Events Page | Excludes `Draft`, `UnderReview` | Public should not see unpublished events |
| Dashboard - Event Management | Includes ALL statuses | Organizer needs to see their draft events |
| Dashboard - My Registered Events | Excludes `Draft`, `UnderReview` | User shouldn't see events that became unpublished |

**Impact**: The same search term returns different event counts because of status filtering.

#### 3. **Different Default Date Range Filters**

| Context | Default Date Range | Effect |
|---------|-------------------|--------|
| Events Page | `'upcoming'` | `StartDateFrom = now` (future events only) |
| Dashboard - Event Management | `'all'` | No date filter (past + future events) |
| Dashboard - My Registered Events | `'upcoming'` | `StartDateFrom = now` (future events only) |

**Impact**: Dashboard Event Management shows more events because it includes past events by default.

#### 4. **Different Search Debounce Times**

| Context | Debounce Time |
|---------|---------------|
| Events Page | 500ms |
| Dashboard (EventFilters) | 300ms |

**Impact**: Minor UX difference; searches trigger at different times during typing.

#### 5. **Two-Stage Filtering in Dashboard Handlers**

The `GetEventsByOrganizerQueryHandler` and `GetMyRegisteredEventsQueryHandler` use a two-stage approach:

```csharp
// Stage 1: Get all IDs relevant to user
var organizerEvents = await _eventRepository.GetByOrganizerAsync(request.OrganizerId);
var organizerEventIds = organizerEvents.Select(e => e.Id).ToHashSet();

// Stage 2: Get filtered events from GetEventsQuery
var eventsResult = await _mediator.Send(getEventsQuery);

// Stage 3: Intersect results
var filteredEvents = eventsResult.Value
    .Where(e => organizerEventIds.Contains(e.Id))
    .ToList();
```

**Impact**: This intersection approach can cause unexpected results if:
- The organizer's events are filtered out by `GetEventsQuery` (e.g., due to status filter misconfig)
- Race conditions between the two queries

#### 6. **Search Algorithm Differences**

When `searchTerm` is provided:

**GetEventsQueryHandler (Events Page)**:
```csharp
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
{
    // Uses SearchAsync with excludeCancelled: false
    (events, _) = await _eventRepository.SearchAsync(
        request.SearchTerm,
        limit: 1000,
        offset: 0,
        request.Category,
        request.IsFreeOnly,
        request.StartDateFrom,
        excludeCancelled: false,  // Shows cancelled events
        cancellationToken);
}
```

**GetEventsByOrganizerQueryHandler (Dashboard)**:
- Delegates to `GetEventsQuery` with `SearchTerm` parameter
- Which then calls the same `SearchAsync` method
- But additional filtering happens in `GetEventsQuery` (status exclusion)

**Issue**: The `SearchAsync` method in `EventRepository` doesn't respect the `IncludeAllStatuses` flag from `GetEventsQuery`. The status filtering happens AFTER the search results are returned.

---

## Specific Issues Found

### Issue 1: Status Filtering Happens After Search

In `GetEventsQueryHandler`, when `SearchTerm` is provided, the code calls `SearchAsync()` first, then applies status filtering in `GetFilteredEventsAsync()`. However, when using search:

```csharp
// Line 60-81 in GetEventsQueryHandler.cs
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
{
    // SearchAsync is called...
    (events, _) = await _eventRepository.SearchAsync(...);
    // Results go directly to location-based sorting
    // Status filtering from GetFilteredEventsAsync is SKIPPED!
}
else
{
    // Only when no search term, status filtering is applied
    events = await GetFilteredEventsAsync(request, cancellationToken);
}
```

**Result**: When searching on Events page, Draft/UnderReview events might appear even though they shouldn't.

### Issue 2: Dashboard Event Management Missing Status Parameter

The `GetEventsByOrganizerQueryHandler` creates a `GetEventsQuery` without passing the explicit status filter:

```csharp
// Line 91-99 in GetEventsByOrganizerQueryHandler.cs
var getEventsQuery = new GetEventsQuery(
    SearchTerm: request.SearchTerm,
    Category: request.Category,
    StartDateFrom: request.StartDateFrom,
    StartDateTo: request.StartDateTo,
    State: request.State,
    MetroAreaIds: request.MetroAreaIds,
    IncludeAllStatuses: true  // This is correct
);
```

This looks correct, but the issue is `GetEventsQuery` with `IncludeAllStatuses=true` still calls `SearchAsync()` which doesn't know about this flag.

### Issue 3: In-Memory Filtering Applied After Search

The `ApplyInMemoryFilters` method in `GetEventsQueryHandler` applies additional filters like Category, State, Date range:

```csharp
// Line 515-547
private IEnumerable<Event> ApplyInMemoryFilters(IReadOnlyList<Event> events, GetEventsQuery request)
{
    // Category, State, StartDateFrom, StartDateTo, IsFreeOnly filters
}
```

This happens AFTER `SearchAsync()` returns, meaning:
- Search might return 1000 events
- Then in-memory filters reduce it
- But the search ranking from PostgreSQL FTS is not considered

---

## Recommended Fixes

### Fix 1: Consolidate Search Logic (High Priority)

Move all filtering logic into `SearchAsync()` method in `EventRepository` to ensure consistent behavior:

```csharp
public async Task<(IReadOnlyList<Event> Events, int TotalCount)> SearchAsync(
    string searchTerm,
    int limit,
    int offset,
    EventCategory? category = null,
    bool? isFreeOnly = null,
    DateTime? startDateFrom = null,
    DateTime? startDateTo = null,           // ADD
    bool excludeCancelled = false,
    bool includeAllStatuses = false,        // ADD
    List<Guid>? metroAreaIds = null,        // ADD
    CancellationToken cancellationToken = default)
```

### Fix 2: Apply Status Filter in SearchAsync (High Priority)

Add status filtering directly to the SQL query in `SearchAsync()`:

```sql
-- Current (no status filter)
WHERE e.search_vector @@ to_tsquery(...)

-- Proposed (with status filter)
WHERE e.search_vector @@ to_tsquery(...)
  AND (
    {includeAllStatuses} = true
    OR e."Status" NOT IN ('Draft', 'UnderReview')
  )
```

### Fix 3: Unify Date Filter Defaults (Medium Priority)

Consider making the default date filter consistent across all contexts, or make it explicit in the UI:

```typescript
// In EventFilters component, add visual indicator
{filters.dateRange === 'all' && (
  <span className="text-sm text-gray-500">
    (showing past and future events)
  </span>
)}
```

### Fix 4: Pass Full Filter Context to SearchAsync (Medium Priority)

Modify `GetEventsQueryHandler` to pass all filter context to `SearchAsync`:

```csharp
if (!string.IsNullOrWhiteSpace(request.SearchTerm))
{
    (events, _) = await _eventRepository.SearchAsync(
        request.SearchTerm,
        limit: 1000,
        offset: 0,
        request.Category,
        request.IsFreeOnly,
        request.StartDateFrom,
        request.StartDateTo,           // ADD
        excludeCancelled: false,
        includeAllStatuses: request.IncludeAllStatuses,  // ADD
        request.MetroAreaIds,          // ADD
        cancellationToken);

    // Skip GetFilteredEventsAsync since SearchAsync now handles all filters
}
```

### Fix 5: Add Logging for Filter State (Low Priority)

Add logging to help diagnose search issues:

```typescript
// In useEvents hook
console.log('[useEvents] Fetching with filters:', {
  endpoint: '/api/events',
  searchTerm: filters?.searchTerm,
  category: filters?.category,
  dateRange: { from: filters?.startDateFrom, to: filters?.startDateTo },
  metroAreaIds: filters?.metroAreaIds
});
```

---

## Summary Table

| Aspect | Events Page | Dashboard - Event Mgmt | Dashboard - My Registered |
|--------|-------------|----------------------|---------------------------|
| API Endpoint | `/api/events` | `/api/events/my-events` | `/api/events/my-rsvps` |
| Query Handler | `GetEventsQueryHandler` | `GetEventsByOrganizerQueryHandler` | `GetMyRegisteredEventsQueryHandler` |
| Default Date Filter | Upcoming only | All dates | Upcoming only |
| Status Filter | Excludes Draft/UnderReview | Includes All | Excludes Draft/UnderReview |
| User Filter | None (all public) | Organizer's events only | User's registered events |
| Search Debounce | 500ms | 300ms | 300ms |

---

## Testing Recommendations

1. **Test Case: Same Search Term Across All Contexts**
   - Search for "Test Event" on Events page
   - Search for "Test Event" on Dashboard Event Management
   - Search for "Test Event" on Dashboard My Registered
   - Compare results and verify expected differences

2. **Test Case: Draft Event Visibility**
   - Create a draft event as organizer
   - Verify it appears in Dashboard Event Management
   - Verify it does NOT appear on Events page
   - Verify it does NOT appear in My Registered (if registered)

3. **Test Case: Date Filter Behavior**
   - Create past and future events
   - Verify Events page only shows future events
   - Verify Dashboard Event Management shows all events
   - Toggle date filter and verify behavior

4. **Test Case: Search with Filters Combined**
   - Search with category filter applied
   - Verify results match both search term AND category
   - Compare across all three contexts

---

## Implementation Priority

1. **Critical**: Fix 1 & 2 - Consolidate search logic and apply status filter consistently
2. **High**: Fix 4 - Pass full filter context to SearchAsync
3. **Medium**: Fix 3 - Unify or clarify date filter defaults
4. **Low**: Fix 5 - Add debugging logs

---

## Related Files

- `c:\Work\LankaConnect\web\src\components\events\filters\EventFilters.tsx`
- `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEvents\GetEventsQueryHandler.cs`
- `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs`

---

**Document Created**: 2026-02-04
**Author**: Claude Code RCA Agent
**Issue Reference**: GitHub Issue #33
