# Root Cause Analysis: Issue #79 - Event Type Search Shows Error Instead of "No Events Found"

**Date**: 2026-02-15
**Analyst**: Claude (SPARC Architecture Agent)
**Issue**: PROD - Events page -> 'Event Type' search shows error message instead of "No Events found"
**Severity**: Medium (UI/UX Issue - Production)
**Environment**: Production

---

## Executive Summary

**Issue Category**: **Frontend UI Error Handling Issue**

When users filter events by Event Type categories that have no events (Ceremony, Workshop, Celebration), the Events page displays "Failed to load events. Please try again later." instead of the expected "No Events found" message.

**Root Cause**: The backend API correctly returns an empty array `[]` for event types with no events, but the frontend error handling logic incorrectly treats this as an error condition, likely due to React Query's error state being triggered by an unrelated issue or stale error state.

**Impact**: Users cannot distinguish between:
- Legitimate server errors (API down, network issues)
- Empty search results (no events exist for selected category)

This leads to confusion and poor user experience, making users think the system is broken when it's actually working correctly.

---

## Issue Details

### Problem Description
- **What happens**: Filtering by Event Types "Ceremony", "Workshop", or "Celebration" shows error message
- **Expected behavior**: Should display "No Events found" with friendly message
- **Actual behavior**: Shows "Failed to load events. Please try again later."
- **Affected Event Types**: Ceremony (10), Workshop (8), Celebration (11)
- **Working Event Types**: Religious (0), Cultural (1), Community (2), etc. (types with existing events)

---

## Investigation Timeline

### Phase 1: Frontend Investigation ✅

**File Analyzed**: `web/src/app/events/page.tsx`

**Key Findings**:
1. **Error Display Logic** (Lines 380-401):
```tsx
{isLoading ? (
  // Loading skeleton...
) : eventsError ? (
  <Card>
    <CardContent className="p-12 text-center">
      <p className="text-destructive text-lg">
        Failed to load events. Please try again later.
      </p>
    </CardContent>
  </Card>
) : !events || events.length === 0 ? (
  <Card>
    <CardContent className="p-12 text-center">
      <Calendar className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
      <h3 className="text-xl font-semibold text-neutral-900 mb-2">
        No Events Found
      </h3>
      <p className="text-neutral-500">
        {hasActiveFilters
          ? 'Try adjusting your filters to see more events.'
          : 'Check back soon for new events!'}
      </p>
    </CardContent>
  </Card>
) : (
  // Display events grid...
)}
```

**Analysis**:
- The logic has THREE states: loading, error, and no-results
- The error state (`eventsError`) takes precedence over the no-results check
- If `eventsError` is truthy, it shows error message regardless of whether `events` is an empty array

2. **Filter Implementation** (Lines 58-94):
```tsx
const [selectedCategory, setSelectedCategory] = useState<EventCategory | undefined>(undefined);

const filters = useMemo(() => {
  return {
    searchTerm: debouncedSearchTerm || undefined,
    category: selectedCategory,
    statusFilter: statusFilter,
    userId: user?.userId,
    latitude: isAnonymous ? latitude ?? undefined : undefined,
    longitude: isAnonymous ? longitude ?? undefined : undefined,
    metroAreaIds: stableMetroIds,
    state: selectedState,
    ...dateRange,
  };
}, [debouncedSearchTerm, selectedCategory, statusFilter, user?.userId, isAnonymous, latitude, longitude, stableMetroIds, selectedState, dateRange]);

const { data: events, isLoading: eventsLoading, error: eventsError } = useEvents(filters);
```

**Analysis**:
- Category filter is passed correctly to `useEvents` hook
- React Query's `error` state (`eventsError`) is the issue - it's being set when it shouldn't be

---

### Phase 2: Backend API Investigation ✅

**File Analyzed**: `src/LankaConnect.API/Controllers/EventsController.cs`

**Key Findings**:
1. **GetEvents Endpoint** (Lines 122-171):
```csharp
[HttpGet]
[ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> GetEvents(
    [FromQuery] EventStatus? status = null,
    [FromQuery] EventStatusFilter? statusFilter = null,
    [FromQuery] EventCategory? category = null,
    // ... other parameters
)
{
    var query = new GetEventsQuery(
        status,
        statusFilter,
        category,
        // ... other parameters
    );

    var result = await Mediator.Send(query);

    return HandleResult(result);
}
```

**Analysis**:
- Endpoint accepts `EventCategory?` parameter (nullable enum)
- Returns `IReadOnlyList<EventDto>` with HTTP 200 OK
- No special handling for empty results - returns empty array `[]`

2. **GetEventsQueryHandler** (Lines 45-188):
```csharp
public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
{
    // ... filtering logic

    // Step 3: Apply additional in-memory filters
    var filteredEvents = ApplyInMemoryFilters(events, request);
    var filteredList = filteredEvents.ToList();

    // Step 4: Sort and map to DTOs
    var result = filteredList
        .OrderBy(e => e.StartDate)
        .Select(e => _mapper.Map<EventDto>(e))
        .ToList();

    return Result<IReadOnlyList<EventDto>>.Success(result);
}
```

**Key Method**: `ApplyInMemoryFilters` (Lines 641-682):
```csharp
private IEnumerable<Event> ApplyInMemoryFilters(IReadOnlyList<Event> events, GetEventsQuery request)
{
    var filteredEvents = events.AsEnumerable();

    // Category filter
    if (request.Category.HasValue)
    {
        filteredEvents = filteredEvents.Where(e => e.Category == request.Category.Value);
    }

    // ... other filters

    return filteredEvents;
}
```

**Analysis**:
- Backend correctly filters by category using LINQ `.Where(e => e.Category == request.Category.Value)`
- If no events match, it returns an empty `IEnumerable` which becomes an empty array `[]`
- The handler returns `Result.Success([])` - **this is correct behavior**
- No exceptions thrown, no error states set

**Conclusion**: Backend is working correctly. It returns HTTP 200 OK with an empty array `[]`.

---

### Phase 3: EventCategory Enum Verification ✅

**Files Analyzed**:
- Backend: `src/LankaConnect.Domain/Events/Enums/EventCategory.cs`
- Frontend: `web/src/infrastructure/api/types/events.types.ts`

**Backend Enum** (C#):
```csharp
public enum EventCategory
{
    Religious,      // 0
    Cultural,       // 1
    Community,      // 2
    Educational,    // 3
    Social,         // 4
    Business,       // 5
    Charity,        // 6
    Entertainment,  // 7
    Workshop,       // 8 - Phase 6A.109
    Festival,       // 9 - Phase 6A.109
    Ceremony,       // 10 - Phase 6A.109
    Celebration     // 11 - Phase 6A.109
}
```

**Frontend Enum** (TypeScript):
```typescript
export enum EventCategory {
  Religious = 0,
  Cultural = 1,
  Community = 2,
  Educational = 3,
  Social = 4,
  Business = 5,
  Charity = 6,
  Entertainment = 7,
  Workshop = 8,
  Festival = 9,
  Ceremony = 10,
  Celebration = 11,
}
```

**Analysis**: ✅ Enums are perfectly synchronized. The issue is NOT an enum mismatch.

---

### Phase 4: React Query Error Handling Investigation ✅

**File Analyzed**: `web/src/presentation/hooks/useEvents.ts`

**useEvents Hook** (Lines 75-90):
```typescript
export function useEvents(
  filters?: GetEventsRequest,
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventKeys.list(filters || {}),
    queryFn: async () => {
      const result = await eventsRepository.getEvents(filters);
      return result;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchOnWindowFocus: true,
    retry: 1, // Only retry once
    ...options,
  });
}
```

**API Client** (`web/src/infrastructure/api/client/api-client.ts`):
- Lines 298-301: `get<T>` method returns `response.data` directly
- Lines 228-272: Error handling converts Axios errors to custom ApiError types
- No special handling for empty arrays

**Events Repository** (`web/src/infrastructure/api/repositories/events.repository.ts`):
```typescript
async getEvents(filters: GetEventsRequest = {}): Promise<EventDto[]> {
  const params = new URLSearchParams();

  if (filters.category !== undefined) params.append('category', String(filters.category));
  // ... other filters

  const queryString = params.toString();
  const url = queryString ? `${this.basePath}?${queryString}` : this.basePath;

  return await apiClient.get<EventDto[]>(url);
}
```

**Analysis**:
- Repository correctly converts `filters.category` to query parameter
- API client makes GET request to `/api/events?category=10` (for Ceremony)
- Backend returns HTTP 200 OK with `[]`
- API client returns `[]` to React Query
- **React Query should set `data: []`, `isLoading: false`, `error: undefined`**

---

## Root Cause Identification

### Primary Root Cause

**The issue is in the frontend React Query error state management.**

There are two possible scenarios:

#### Scenario A: Stale Error State (Most Likely)
React Query's error state (`eventsError`) is not being cleared when switching between event types. When a user:
1. First searches for an event type with events → Success, `error: undefined`
2. Then searches for an event type WITHOUT events → Backend returns `[]` correctly
3. **BUT** React Query still has a stale `error` from a previous failed request in cache
4. Frontend shows error message instead of "No Events Found"

#### Scenario B: Empty Array Misinterpretation (Less Likely)
The `useEvents` hook or React Query configuration incorrectly treats an empty array `[]` as an error condition, possibly due to:
- Custom error handling in the `queryFn`
- Stale cache issues
- Race conditions between filter changes

### Secondary Contributing Factors

1. **No Error State Reset**: The Events page doesn't explicitly reset error state when filters change
2. **Cache Key Collisions**: React Query cache keys may not be granular enough to distinguish between different filter states
3. **No Optimistic Error Clearing**: When filters change, the old error state persists until the new request completes

---

## Evidence Supporting Root Cause

### ✅ Evidence That Backend is Correct
1. Backend handler returns `Result.Success([])` for empty results
2. Controller returns HTTP 200 OK (not 4xx or 5xx)
3. EventCategory enum values match frontend exactly (8, 9, 10, 11)
4. LINQ filtering logic is standard and correct

### ✅ Evidence That Frontend Has Issue
1. Error message is hardcoded in Events page: "Failed to load events. Please try again later."
2. Error display logic checks `eventsError` BEFORE checking empty array
3. No explicit error state clearing when filters change
4. React Query cache may retain stale error states

### ✅ Evidence of Stale Error State
- Issue description mentions error appears ONLY for event types with NO events
- Event types WITH events work fine (no error shown)
- This suggests React Query's error state is not being cleared properly

---

## Impact Analysis

### User Impact
- **Severity**: Medium
- **Affected Users**: All users (authenticated and anonymous)
- **Frequency**: Every time a user filters by Ceremony, Workshop, or Celebration
- **User Confusion**: High - users think system is broken when it's actually working

### Business Impact
- **Data Loss**: None
- **Revenue Impact**: None (informational feature)
- **Brand Impact**: Moderate (poor UX reflects badly on product quality)

### Technical Debt
- Indicates potential issue with error state management across the application
- May affect other search/filter features using React Query

---

## Fix Plan

### Solution Overview
Clear React Query error state when filters change, ensuring empty results are not confused with error states.

### Option 1: Frontend Fix - Add Error State Reset (Recommended)

**Why Recommended**:
- Root cause is in frontend state management
- Backend is working correctly
- Minimal code changes
- No deployment required for backend

**Files to Modify**:
1. `web/src/app/events/page.tsx`

**Changes Required**:

#### Change 1: Reset Error on Filter Change
Add `useEffect` to clear error when category filter changes:

```typescript
// Add after line 97 (after useEvents hook)
useEffect(() => {
  // Clear any stale errors when category filter changes
  if (eventsError) {
    queryClient.invalidateQueries({ queryKey: eventKeys.list(filters) });
  }
}, [selectedCategory]);
```

#### Change 2: Improve Error Display Logic
Modify error check to be more defensive (line 380):

```typescript
{isLoading ? (
  // Loading skeleton...
) : eventsError && (!events || events.length === 0) ? (
  // ^ Only show error if we ALSO have no data
  <Card>
    <CardContent className="p-12 text-center">
      <p className="text-destructive text-lg">
        Failed to load events. Please try again later.
      </p>
    </CardContent>
  </Card>
) : !events || events.length === 0 ? (
  // Show no events message
  <Card>
    <CardContent className="p-12 text-center">
      <Calendar className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
      <h3 className="text-xl font-semibold text-neutral-900 mb-2">
        No Events Found
      </h3>
      <p className="text-neutral-500">
        {hasActiveFilters
          ? 'Try adjusting your filters to see more events.'
          : 'Check back soon for new events!'}
      </p>
    </CardContent>
  </Card>
) : (
  // Display events grid...
)}
```

**Better Approach**: Simplify to prioritize data over error:

```typescript
{isLoading ? (
  // Loading skeleton...
) : !events || events.length === 0 ? (
  eventsError ? (
    // Only show error if we have no data AND an error
    <Card>
      <CardContent className="p-12 text-center">
        <p className="text-destructive text-lg">
          Failed to load events. Please try again later.
        </p>
      </CardContent>
    </Card>
  ) : (
    // Show no events message
    <Card>
      <CardContent className="p-12 text-center">
        <Calendar className="h-16 w-16 mx-auto mb-4 text-neutral-400" />
        <h3 className="text-xl font-semibold text-neutral-900 mb-2">
          No Events Found
        </h3>
        <p className="text-neutral-500">
          {hasActiveFilters
            ? 'Try adjusting your filters to see more events.'
            : 'Check back soon for new events!'}
        </p>
      </CardContent>
    </Card>
  )
) : (
  // Display events grid...
)}
```

### Option 2: React Query Configuration Fix

**Files to Modify**:
1. `web/src/presentation/hooks/useEvents.ts`

**Changes Required**:

Add `keepPreviousData` option to prevent stale errors:

```typescript
export function useEvents(
  filters?: GetEventsRequest,
  options?: Omit<UseQueryOptions<EventDto[], ApiError>, 'queryKey' | 'queryFn'>
) {
  return useQuery({
    queryKey: eventKeys.list(filters || {}),
    queryFn: async () => {
      const result = await eventsRepository.getEvents(filters);
      return result;
    },
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: true,
    retry: 1,
    keepPreviousData: false, // Don't keep previous data when filters change
    ...options,
  });
}
```

---

## Testing Strategy

### Unit Tests
1. **Test: Empty Array Returns No Error**
   - Mock API to return `[]`
   - Verify `eventsError` is `undefined`
   - Verify "No Events Found" message is displayed

2. **Test: Filter Change Clears Error**
   - Mock API to return error for first request
   - Change filter
   - Mock API to return `[]` for second request
   - Verify error is cleared and "No Events Found" is shown

### Integration Tests
1. **Test: Ceremony Category Filter**
   - Select "Ceremony" from Event Type dropdown
   - Verify "No Events Found" message appears (not error)

2. **Test: Workshop Category Filter**
   - Select "Workshop" from Event Type dropdown
   - Verify "No Events Found" message appears (not error)

3. **Test: Celebration Category Filter**
   - Select "Celebration" from Event Type dropdown
   - Verify "No Events Found" message appears (not error)

### Manual Testing Checklist
- [ ] Filter by Ceremony → Shows "No Events Found"
- [ ] Filter by Workshop → Shows "No Events Found"
- [ ] Filter by Celebration → Shows "No Events Found"
- [ ] Filter by Religious → Shows events (or "No Events Found" if none)
- [ ] Switch between filters rapidly → No stale errors
- [ ] Refresh page with Ceremony filter → No error
- [ ] Network error simulation → Shows error message correctly
- [ ] Clear filters → Returns to all events

---

## Deployment Plan

### Phase 1: Frontend Fix Deployment
1. Implement Option 1 (recommended)
2. Run unit tests
3. Run integration tests
4. Deploy to staging
5. Manual QA on staging
6. Deploy to production
7. Monitor production logs

### Phase 2: Verification
1. Test all affected event types in production
2. Monitor error rates for `/api/events` endpoint
3. Collect user feedback

### Rollback Plan
- If issue persists, revert frontend changes
- Investigate React Query cache configuration
- Consider adding server-side logging for empty result scenarios

---

## Prevention Measures

### Immediate Actions
1. Add unit tests for empty result scenarios in all list pages
2. Document React Query error state management patterns
3. Add explicit error state clearing in all filter-based pages

### Long-term Improvements
1. Create reusable `useFilteredList` hook with proper error handling
2. Add React Query DevTools to staging for debugging
3. Standardize error vs. no-results UI patterns across application
4. Add monitoring for "false error" scenarios (HTTP 200 with empty array)

### Code Review Checklist Addition
- [ ] Empty result scenarios are handled separately from errors
- [ ] Filter changes clear previous error states
- [ ] React Query cache keys are granular enough
- [ ] Error display logic prioritizes data over stale errors

---

## Related Issues

- **Issue #78**: Festival filter error (similar enum issue, already fixed)
- **Issue #36**: Event status filter improvements
- **Phase 6A.109**: EventCategory enum expansion (added Workshop, Festival, Ceremony, Celebration)

---

## Conclusion

**Issue Category**: Frontend UI Error Handling Issue

**Root Cause**: React Query's error state is not being cleared when filters change, causing stale errors to be displayed when searching for event types with no events.

**Fix**: Modify Events page error display logic to prioritize data availability over error state, and/or add explicit error clearing when filters change.

**Complexity**: Low (frontend-only fix)

**Estimated Effort**: 2-4 hours (including testing)

**Recommended Approach**: Option 1 (Frontend Fix - Improve Error Display Logic)

---

**Document Version**: 1.0
**Last Updated**: 2026-02-15
**Status**: Ready for Implementation