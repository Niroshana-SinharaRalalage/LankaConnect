# Root Cause Analysis: Unpublished Events Not Visible in Event Management

**Date:** 2026-01-27
**Analyst:** System Architect Agent
**Phase:** 6A.88 - Event Management Visibility Fix
**Severity:** MEDIUM - Impacts event organizer workflow

---

## 1. Executive Summary

After creating a new event and navigating back to the Event Management tab without publishing it, the organizer cannot see the newly created event. The event is correctly saved to the database with `Status = Draft`, but the Event Management page does not display it.

### Issue Reproduction Steps
1. User creates a new event
2. User navigates back to Event Management tab WITHOUT publishing the event
3. **Expected:** The newly created event appears in the list with Draft status
4. **Actual:** The event is NOT visible in the Event Management page

---

## 2. Issue Categorization

| Category | Applicable | Explanation |
|----------|------------|-------------|
| UI Issue | No | Frontend correctly calls API and displays returned data |
| Auth Issue | No | User authentication and authorization work correctly |
| Backend API Issue | **YES** | `GetEventsByOrganizerQueryHandler` incorrectly filters out Draft events |
| Database Issue | No | Events are correctly saved with Draft status |
| Missing Feature | No | This is a bug, not missing functionality |

---

## 3. Data Flow Analysis

### 3.1 Expected Flow (Happy Path)
```
1. User creates event -> Event saved with Status=Draft
2. User navigates to Event Management page
3. Frontend calls GET /api/events/my-events
4. Backend retrieves ALL organizer's events (including Draft)
5. Draft events displayed in Event Management page
```

### 3.2 Actual Flow (Bug Path)
```
1. User creates event -> Event saved with Status=Draft (CORRECT)
2. User navigates to Event Management page
3. Frontend calls GET /api/events/my-events (CORRECT)
4. GetEventsByOrganizerQueryHandler delegates to GetEventsQuery
5. GetEventsQuery filters: "Status != Draft && Status != UnderReview" (BUG)
6. Draft events are filtered out before reaching frontend
7. Event Management page shows no Draft events
```

---

## 4. Root Cause Analysis

### 4.1 PRIMARY ROOT CAUSE: GetEventsByOrganizerQueryHandler Delegates to GetEventsQuery

**File:** `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEventsByOrganizer\GetEventsByOrganizerQueryHandler.cs`

**Problem:** The handler for organizer's "My Events" delegates to `GetEventsQuery`, which was designed for **public event listings** and intentionally filters out Draft and UnderReview events.

**Why This is Wrong:**
- `GetEventsQuery` is for public listings where Draft events should NOT be visible
- `GetEventsByOrganizerQuery` is for the organizer's management view where Draft events MUST be visible
- The delegation causes organizer-specific logic to use public-facing filters

### 4.2 The Filtering Code (GetEventsQueryHandler)

**File:** `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEvents\GetEventsQueryHandler.cs`
**Lines:** 161-167

```csharp
// Phase 6A.59: Get ALL events except Draft and UnderReview
// This includes Published, Active, Cancelled, Completed, Archived, Postponed
// Cancelled events will show with CANCELLED badge in UI
var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
return allEvents
    .Where(e => e.Status != EventStatus.Draft && e.Status != EventStatus.UnderReview)
    .ToList();
```

**Purpose:** This filter is CORRECT for public event listings - users should NOT see unpublished/draft events.

**Bug:** The `GetEventsByOrganizerQueryHandler` delegates to this query, inheriting the filter when it shouldn't.

### 4.3 The Delegation Bug (GetEventsByOrganizerQueryHandler)

**File:** `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEventsByOrganizer\GetEventsByOrganizerQueryHandler.cs`
**Lines:** 89-97 and 151-168

```csharp
// Phase 6A.47: If filters provided, use GetEventsQuery for search/filter support
if (HasFilters(request))
{
    var organizerEventIds = (await _eventRepository.GetByOrganizerAsync(request.OrganizerId, cancellationToken))
        .Select(e => e.Id)
        .ToHashSet();

    // Use GetEventsQuery with filters  <-- PROBLEM: Delegates to GetEventsQuery
    var getEventsQuery = new GetEventsQuery(...);
    var eventsResult = await _mediator.Send(getEventsQuery, cancellationToken);

    var filteredEvents = eventsResult.Value
        .Where(e => organizerEventIds.Contains(e.Id))
        .ToList();
}

// Original path without filters - ALSO delegates to GetEventsQuery
var getAllQuery = new GetEventsQuery();
var allEventsResult = await _mediator.Send(getAllQuery, cancellationToken);
```

**Impact:** Both paths (with filters and without filters) delegate to `GetEventsQuery`, which filters out Draft events.

### 4.4 Repository Method is Correct

**File:** `c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EventRepository.cs`
**Lines:** 280-328

```csharp
public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(
    Guid organizerId, CancellationToken cancellationToken = default)
{
    var result = await _dbSet
        .AsNoTracking()
        .Include(e => e.Images)
        .Include(e => e.Registrations)
        .Where(e => e.OrganizerId == organizerId)  // NO STATUS FILTER - Returns ALL
        .OrderByDescending(e => e.StartDate)
        .ToListAsync(cancellationToken);
    return result;
}
```

**Status:** CORRECT - Returns all events regardless of status.

---

## 5. Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           DATA FLOW DIAGRAM                                      │
└─────────────────────────────────────────────────────────────────────────────────┘

 ┌──────────────┐
 │   Frontend   │
 │  Dashboard   │   Location: web/src/app/(dashboard)/dashboard/page.tsx
 │   page.tsx   │
 └──────┬───────┘
        │ 1. getUserCreatedEvents()
        ▼
 ┌──────────────┐
 │   events.    │   Location: web/src/infrastructure/api/events.repository.ts
 │ repository   │
 │     .ts      │
 └──────┬───────┘
        │ 2. GET /api/events/my-events
        ▼
 ┌──────────────┐
 │   Events     │   Location: src/LankaConnect.API/Controllers/EventsController.cs
 │ Controller   │
 │    .cs       │
 └──────┬───────┘
        │ 3. GetEventsByOrganizerQuery
        ▼
 ┌──────────────────────────────┐
 │ GetEventsByOrganizerQuery    │   Location: src/LankaConnect.Application/Events/
 │         Handler              │             Queries/GetEventsByOrganizer/
 │                              │
 │  ┌───────────────────────┐   │
 │  │ GetByOrganizerAsync() │◄──┼─── Gets ALL events (Draft included)
 │  │    (Repository)       │   │    ✅ Works correctly
 │  └───────────┬───────────┘   │
 │              │               │
 │              ▼               │
 │  ┌───────────────────────┐   │
 │  │   GetEventsQuery      │◄──┼─── ❌ BUG: Filters out Draft events
 │  │      (Delegated)      │   │    "Status != Draft && Status != UnderReview"
 │  └───────────┬───────────┘   │
 │              │               │
 │              ▼               │
 │  ┌───────────────────────┐   │
 │  │    Filter by IDs      │   │    Draft events already filtered out
 │  │  (organizerEventIds)  │   │    before this step
 │  └───────────────────────┘   │
 └──────────────────────────────┘
                │
                ▼
         ❌ Draft events are MISSING
            from response to frontend
```

---

## 6. Component-by-Component Analysis

### 6.1 Backend Components

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| Event.Create() | `src/LankaConnect.Domain/Events/Event.cs` | ✅ CORRECT | Creates with Status=Draft |
| CreateEventCommandHandler | `src/LankaConnect.Application/Events/Commands/CreateEvent/CreateEventCommandHandler.cs` | ✅ CORRECT | Saves event immediately |
| EventRepository.GetByOrganizerAsync | `src/LankaConnect.Infrastructure/Data/Repositories/EventRepository.cs` | ✅ CORRECT | Returns ALL events |
| **GetEventsByOrganizerQueryHandler** | `src/LankaConnect.Application/Events/Queries/GetEventsByOrganizer/GetEventsByOrganizerQueryHandler.cs` | ❌ **BUG** | Delegates to GetEventsQuery |
| GetEventsQueryHandler | `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs` | ✅ CORRECT* | Filters Draft for public use |

*Note: GetEventsQueryHandler is correct for its intended purpose (public listings), but should not be used for organizer's view.

### 6.2 Frontend Components

| Component | File | Status | Notes |
|-----------|------|--------|-------|
| Dashboard page | `web/src/app/(dashboard)/dashboard/page.tsx` | ✅ CORRECT | Displays what API returns |
| events.repository | `web/src/infrastructure/api/events.repository.ts` | ✅ CORRECT | Calls correct API |
| EventCard | `web/src/presentation/components/features/events/EventCard.tsx` | ✅ CORRECT | Can display any status |

---

## 7. Fix Plan

### 7.1 Recommended Fix: Direct Repository Mapping (Option A)

**File to Modify:** `c:\Work\LankaConnect\src\LankaConnect.Application\Events\Queries\GetEventsByOrganizer\GetEventsByOrganizerQueryHandler.cs`

**Change:** Instead of delegating to `GetEventsQuery`, directly map results from `GetByOrganizerAsync()` using AutoMapper.

**Implementation:**
1. Remove delegation to `GetEventsQuery`
2. Get events directly from `_eventRepository.GetByOrganizerAsync()`
3. Map to DTOs using AutoMapper
4. Apply filters locally if needed (search, category, date, location)

**Pseudo-code:**
```csharp
public async Task<Result<IReadOnlyList<EventDto>>> Handle(
    GetEventsByOrganizerQuery request, CancellationToken cancellationToken)
{
    // Get ALL organizer events (including Draft and UnderReview)
    var organizerEvents = await _eventRepository.GetByOrganizerAsync(
        request.OrganizerId, cancellationToken);

    // Map directly to DTOs (don't delegate to GetEventsQuery)
    var eventDtos = organizerEvents.Select(e => _mapper.Map<EventDto>(e)).ToList();

    // Apply local filters if provided
    if (HasFilters(request))
    {
        eventDtos = ApplyLocalFilters(eventDtos, request);
    }

    return Result<IReadOnlyList<EventDto>>.Success(eventDtos);
}
```

### 7.2 Why This Fix is Correct

1. **Organizer's View:** Organizers need to see ALL their events including:
   - Draft (unpublished, being edited)
   - UnderReview (submitted for moderation)
   - Published, Active, Cancelled, Completed, Archived, Postponed

2. **Public View Unchanged:** `GetEventsQuery` continues to filter Draft/UnderReview for public listings

3. **No Regression:** Public event browsing remains unaffected

### 7.3 Alternative Fix: Add IncludeAllStatuses Flag (Option B)

**Files to Modify:**
1. `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQuery.cs`
2. `src/LankaConnect.Application/Events/Queries/GetEvents/GetEventsQueryHandler.cs`

**Change:** Add `IncludeAllStatuses` parameter to bypass Draft/UnderReview filter.

**Risk:** Higher - modifies shared code used by multiple callers.

---

## 8. Test Cases Required

### 8.1 Unit Tests

1. **Test Draft Event Visibility**
   - Create event with Draft status
   - Call `GetEventsByOrganizerQuery` for organizer
   - **Assert:** Draft event is in results

2. **Test UnderReview Event Visibility**
   - Create event with UnderReview status
   - Call `GetEventsByOrganizerQuery` for organizer
   - **Assert:** UnderReview event is in results

3. **Test All Status Visibility**
   - Create events with all statuses
   - Call `GetEventsByOrganizerQuery`
   - **Assert:** All events are in results

4. **Test Filters Still Work**
   - Create multiple events with different categories
   - Apply category filter
   - **Assert:** Only matching events returned (including Draft)

### 8.2 Regression Tests

1. **Public Listings Unchanged**
   - Call `GetEventsQuery` (public)
   - **Assert:** Draft and UnderReview events are NOT in results

2. **Non-Organizer Cannot See Others' Drafts**
   - Call `GetEventsByOrganizerQuery` with different organizerId
   - **Assert:** Other organizers' Draft events not visible

---

## 9. Implementation Steps

1. **Read Current Handler:** Review `GetEventsByOrganizerQueryHandler.cs`
2. **Modify Handler:** Remove GetEventsQuery delegation, use direct mapping
3. **Write Tests:** Add unit tests for Draft/UnderReview visibility
4. **Test Locally:** Run all tests, verify Draft events visible
5. **Deploy to Staging:** Push changes, deploy via deploy-staging.yml
6. **Verify in Staging:** Create event, navigate away, check visibility
7. **Update Documentation:** PROGRESS_TRACKER.md, STREAMLINED_ACTION_PLAN.md

---

## 10. Files to Modify

| File | Action | Priority |
|------|--------|----------|
| `src/LankaConnect.Application/Events/Queries/GetEventsByOrganizer/GetEventsByOrganizerQueryHandler.cs` | **MODIFY** - Remove delegation to GetEventsQuery | HIGH |
| `tests/LankaConnect.Application.Tests/Events/Queries/GetEventsByOrganizer/GetEventsByOrganizerQueryHandlerTests.cs` | **ADD/MODIFY** - Add Draft visibility tests | HIGH |

---

## 11. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Public events show Drafts | Low | High | Don't modify GetEventsQuery |
| Filter functionality breaks | Medium | Medium | Implement local filters carefully |
| Performance degradation | Low | Low | Repository already fetches all needed data |

---

## 12. Summary

**Root Cause:** The `GetEventsByOrganizerQueryHandler` delegates to `GetEventsQuery` (designed for public listings) which filters out Draft and UnderReview events. This prevents organizers from seeing their unpublished events.

**Fix:** Modify `GetEventsByOrganizerQueryHandler` to directly map repository results instead of delegating to `GetEventsQuery`.

**Impact:** Organizers will see all their events including Draft status. Public event listings remain unchanged.

---

**Last Updated:** 2026-01-27
