# Event Filtration System Architecture

**Date**: 2025-12-29
**Related**: [ARCHITECTURE_DECISION_EVENT_FILTRATION.md](./ARCHITECTURE_DECISION_EVENT_FILTRATION.md)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           PRESENTATION LAYER                            │
│                         (React Components + Hooks)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐    │
│  │  Events Page     │  │ Dashboard Page   │  │ Dashboard Page   │    │
│  │  (/events)       │  │ (My Registered)  │  │ (Event Mgmt)     │    │
│  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘    │
│           │                     │                      │               │
│           │        ┌────────────┴───────────┬──────────┘               │
│           │        │                        │                          │
│           ▼        ▼                        ▼                          │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │            EventFilters Component (Reusable)                │      │
│  │  ┌───────────┐ ┌──────────┐ ┌────────────┐ ┌─────────────┐ │      │
│  │  │  Search   │ │ Category │ │ Date Range │ │  Location   │ │      │
│  │  │  Input    │ │ Dropdown │ │  Dropdown  │ │ TreeDropdown│ │      │
│  │  └───────────┘ └──────────┘ └────────────┘ └─────────────┘ │      │
│  │  Props: { onFiltersChange, enabledFilters, layout }        │      │
│  └───────────────────────┬─────────────────────────────────────┘      │
│                          │                                             │
│                          ▼                                             │
│                    EventFilters {}                                     │
│                    {                                                   │
│                      searchTerm?: string                               │
│                      category?: EventCategory                          │
│                      startDateFrom?: string                            │
│                      startDateTo?: string                              │
│                      state?: string                                    │
│                      metroAreaIds?: string[]                           │
│                    }                                                   │
│                          │                                             │
└──────────────────────────┼─────────────────────────────────────────────┘
                           │
┌──────────────────────────┼─────────────────────────────────────────────┐
│                          ▼             REACT QUERY LAYER               │
│  ┌────────────────────────────────────────────────────────────┐       │
│  │  React Query Hooks (Caching + State Management)            │       │
│  │                                                             │       │
│  │  useEvents(filters)          ─── queryKey: ['events', {...}]      │
│  │  useUserRsvps(filters)       ─── queryKey: ['rsvps', {...}]       │
│  │  useUserCreatedEvents(filters) ─ queryKey: ['created', {...}]     │
│  │                                                             │       │
│  │  Features:                                                  │       │
│  │  • Automatic caching (5 min stale time)                     │       │
│  │  • Request deduplication                                    │       │
│  │  • Background refetching                                    │       │
│  │  • Cache invalidation                                       │       │
│  └────────────────────────┬───────────────────────────────────┘       │
└──────────────────────────┼─────────────────────────────────────────────┘
                           │
┌──────────────────────────┼─────────────────────────────────────────────┐
│                          ▼          REPOSITORY LAYER                   │
│  ┌────────────────────────────────────────────────────────────┐       │
│  │  EventsRepository (Infrastructure Layer)                   │       │
│  │                                                             │       │
│  │  getEvents(filters?: EventFilters)                         │       │
│  │  getUserRsvps(filters?: EventFilters)        ◄─── NEW      │       │
│  │  getUserCreatedEvents(filters?: EventFilters) ◄─── NEW     │       │
│  │                                                             │       │
│  │  Pattern:                                                   │       │
│  │  1. Build URLSearchParams from filters                     │       │
│  │  2. Append query parameters                                │       │
│  │  3. Call apiClient.get<EventDto[]>(url)                    │       │
│  └────────────────────────┬───────────────────────────────────┘       │
└──────────────────────────┼─────────────────────────────────────────────┘
                           │
┌──────────────────────────┼─────────────────────────────────────────────┐
│                          ▼              API CLIENT                     │
│  ┌────────────────────────────────────────────────────────────┐       │
│  │  HTTP Client (Fetch Wrapper)                               │       │
│  │  • Authentication (JWT tokens)                             │       │
│  │  • Error handling                                          │       │
│  │  • Request/response interceptors                           │       │
│  └────────────────────────┬───────────────────────────────────┘       │
└──────────────────────────┼─────────────────────────────────────────────┘
                           │
                           │ HTTP GET with query params
                           │
┌──────────────────────────┼─────────────────────────────────────────────┐
│                          ▼           BACKEND API                       │
│  ┌────────────────────────────────────────────────────────────┐       │
│  │  EventsController.cs                                       │       │
│  │                                                             │       │
│  │  GET /api/events?category=1&state=CA&...                   │       │
│  │  GET /api/events/my-registered-events?...  ◄─── READY      │       │
│  │  GET /api/events/my-events?...             ◄─── READY      │       │
│  └────────────────────────┬───────────────────────────────────┘       │
│                           │                                             │
│                           ▼                                             │
│  ┌────────────────────────────────────────────────────────────┐       │
│  │  CQRS Handlers (Application Layer)                         │       │
│  │                                                             │       │
│  │  GetEventsQuery                                             │       │
│  │  GetMyRegisteredEventsQuery      ◄─── ALL FILTERS READY    │       │
│  │  GetEventsByOrganizerQuery       ◄─── ALL FILTERS READY    │       │
│  │                                                             │       │
│  │  Parameters:                                                │       │
│  │  • searchTerm (FTS)                                         │       │
│  │  • category (enum filter)                                   │       │
│  │  • startDateFrom/To (date range)                            │       │
│  │  • state (location filter)                                  │       │
│  │  • metroAreaIds (location filter)                           │       │
│  └────────────────────────┬───────────────────────────────────┘       │
│                           │                                             │
│                           ▼                                             │
│  ┌────────────────────────────────────────────────────────────┐       │
│  │  PostgreSQL Database                                       │       │
│  │  • Events table with indexes                               │       │
│  │  • Full-text search (FTS) on title/description             │       │
│  │  • Indexed columns: Category, State, MetroAreaIds          │       │
│  │  • Query performance: < 100ms                              │       │
│  └────────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Component Interaction Flow

### Scenario 1: User Applies Category Filter

```
┌──────────────┐
│     User     │
└──────┬───────┘
       │ 1. Selects "Cultural" from category dropdown
       ▼
┌──────────────────────┐
│  EventFilters Comp   │
│  [Category Dropdown] │◄─── State: selectedCategory = EventCategory.Cultural
└──────┬───────────────┘
       │ 2. onFiltersChange({ category: EventCategory.Cultural })
       ▼
┌──────────────────────┐
│  Dashboard Page      │
│  [useState]          │◄─── setRegisteredFilters({ category: 1 })
└──────┬───────────────┘
       │ 3. useEffect detects filter change
       ▼
┌──────────────────────┐
│  React Query Hook    │
│  useUserRsvps()      │◄─── queryKey: ['rsvps', { category: 1 }]
└──────┬───────────────┘
       │ 4. Check cache for this queryKey
       │    Cache MISS → Make API call
       ▼
┌──────────────────────┐
│  EventsRepository    │
│  getUserRsvps()      │◄─── Build URL: /my-registered-events?category=1
└──────┬───────────────┘
       │ 5. HTTP GET request
       ▼
┌──────────────────────┐
│  Backend API         │
│  EventsController    │◄─── GetMyRegisteredEventsQuery(category: Cultural)
└──────┬───────────────┘
       │ 6. Query database with WHERE category = 1
       ▼
┌──────────────────────┐
│  PostgreSQL          │◄─── SELECT * FROM Events WHERE category = 1 AND ...
└──────┬───────────────┘
       │ 7. Return filtered EventDto[]
       ▼
┌──────────────────────┐
│  Dashboard Page      │
│  setRegisteredEvents │◄─── Update UI with filtered events
└──────────────────────┘
```

---

## Data Flow: Multi-Filter Combination

### User Applies: Category + Date Range + Location + Search

```
EventFilters State:
{
  searchTerm: "festival",
  category: EventCategory.Cultural,
  startDateFrom: "2025-01-01T00:00:00Z",
  startDateTo: "2025-12-31T23:59:59Z",
  state: "CA",
  metroAreaIds: ["metro-1", "metro-2"]
}

                   │
                   ▼
        URLSearchParams Builder
                   │
                   ├─► searchTerm=festival
                   ├─► category=1
                   ├─► startDateFrom=2025-01-01T00:00:00Z
                   ├─► startDateTo=2025-12-31T23:59:59Z
                   ├─► state=CA
                   ├─► metroAreaIds=metro-1
                   └─► metroAreaIds=metro-2

                   │
                   ▼
    /api/events/my-registered-events?searchTerm=festival&category=1&startDateFrom=2025-01-01T00:00:00Z&startDateTo=2025-12-31T23:59:59Z&state=CA&metroAreaIds=metro-1&metroAreaIds=metro-2

                   │
                   ▼
        Backend Query Handler
                   │
                   ▼
    PostgreSQL WHERE Clause:
    WHERE
      (title ILIKE '%festival%' OR description ILIKE '%festival%')
      AND category = 1
      AND start_date BETWEEN '2025-01-01' AND '2025-12-31'
      AND state = 'CA'
      AND metro_area_id IN ('metro-1', 'metro-2')
      AND registration.user_id = @userId

                   │
                   ▼
        Filtered EventDto[]
```

---

## State Management Architecture

### Dashboard Page: Dual Filter State

```
Dashboard Page Component
├── State: registeredFilters (EventFilters)
│   ├── Used by: My Registered Events tab
│   ├── Updated by: EventFilters component #1
│   └── Triggers: useEffect → loadRegisteredEvents()
│
├── State: createdFilters (EventFilters)
│   ├── Used by: Event Management tab
│   ├── Updated by: EventFilters component #2
│   └── Triggers: useEffect → loadCreatedEvents()
│
├── Tab 1: My Registered Events
│   ├── Renders: EventFilters component
│   │   └── Props: { onFiltersChange: setRegisteredFilters }
│   │
│   └── Renders: EventsList component
│       └── Props: { events: registeredEvents }
│
└── Tab 2: Event Management
    ├── Renders: EventFilters component
    │   └── Props: { onFiltersChange: setCreatedFilters }
    │
    └── Renders: EventsList component
        └── Props: { events: createdEvents }
```

**Key Design Decision**: Filter state is isolated per tab
- Switching tabs preserves filter state
- Each tab maintains independent filter values
- No cross-tab filter interference

---

## Performance Optimization Strategy

### React Query Caching Behavior

```
User applies filter: { category: EventCategory.Cultural }

┌─────────────────────────────────────────────────────────────┐
│                    React Query Cache                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  queryKey: ['rsvps', {}]                                    │
│  data: [Event1, Event2, Event3, ...]                        │
│  staleTime: 5 minutes                                       │
│  cacheTime: 10 minutes                                      │
│  status: STALE (older data, can serve immediately)          │
│                                                             │
│  queryKey: ['rsvps', { category: 1 }]   ◄─── NEW KEY       │
│  data: [Event1, Event3] (filtered)                          │
│  staleTime: 5 minutes                                       │
│  cacheTime: 10 minutes                                      │
│  status: FRESH (just fetched)                               │
│                                                             │
└─────────────────────────────────────────────────────────────┘

User removes filter (back to no filters):

React Query serves cached data from queryKey: ['rsvps', {}]
→ Instant UI update (no API call)
→ Background refetch updates data (stale-while-revalidate)
```

### Debounce Strategy for Search

```
User types: "c" → "cu" → "cul" → "cult" → "cultu" → "cultur" → "cultural"

Without Debounce:
API calls: 7 requests
Time: 0ms, 100ms, 200ms, 300ms, 400ms, 500ms, 600ms
Problems: Server overload, wasted bandwidth, race conditions

With Debounce (500ms):
┌───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┬───┐
│ c │cu │cul│...│cultural                       │
└───┴───┴───┴───┴───┴───┴───┴───┴───┴───┴───┴───┘
    ▲                           ▲
    │ User typing               │ 500ms after last keystroke
    │                           │
    └───────────────────────────┴─► API call ONLY HERE

API calls: 1 request
Reduction: 85% fewer API calls
Bandwidth saved: 700KB → 100KB
```

**Implementation**:
```typescript
import { useDebouncedValue } from '@/presentation/hooks/useDebouncedValue';

const [searchInput, setSearchInput] = useState('');
const debouncedSearch = useDebouncedValue(searchInput, 500);

useEffect(() => {
  onFiltersChange({ ...filters, searchTerm: debouncedSearch });
}, [debouncedSearch]);
```

---

## Responsive Design Architecture

### Desktop Layout (≥ 768px)

```
┌───────────────────────────────────────────────────────────────────┐
│                    Dashboard - My Registered Events                │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  [Search: _________________________] [Category ▼] [Date ▼] [Location ▼] [Clear] │
│                                                                   │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │  Event Card 1   │  │  Event Card 2   │  │  Event Card 3   │  │
│  │  Cultural       │  │  Religious      │  │  Social         │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘

Layout: Horizontal, single row
Height: 60px
Spacing: 16px gap between controls
```

### Mobile Layout (< 768px)

```
┌───────────────────────────────────┐
│   Dashboard - My Registered       │
├───────────────────────────────────┤
│                                   │
│  [🔍 Filters (2 active)]          │ ◄─── Collapsible button
│                                   │
│  ┌───────────────────────────┐   │
│  │  Event Card 1             │   │
│  │  Cultural                 │   │
│  └───────────────────────────┘   │
│                                   │
│  ┌───────────────────────────┐   │
│  │  Event Card 2             │   │
│  │  Religious                │   │
│  └───────────────────────────┘   │
│                                   │
└───────────────────────────────────┘

When user taps "Filters":

┌───────────────────────────────────┐
│  ╔═══════════════════════════╗   │ ◄─── Bottom Drawer
│  ║ Filters                   ║   │
│  ║                           ║   │
│  ║ Search:                   ║   │
│  ║ [___________________]     ║   │
│  ║                           ║   │
│  ║ Category:                 ║   │
│  ║ [Select... ▼]             ║   │
│  ║                           ║   │
│  ║ Date Range:               ║   │
│  ║ [Upcoming ▼]              ║   │
│  ║                           ║   │
│  ║ Location:                 ║   │
│  ║ [Select states... ▼]      ║   │
│  ║                           ║   │
│  ║ [Clear Filters] [Apply]   ║   │
│  ╚═══════════════════════════╝   │
└───────────────────────────────────┘
```

---

## Type System Architecture

### TypeScript Interface Hierarchy

```
┌─────────────────────────────────────────────────────────────┐
│  EventFilters (New Interface)                               │
├─────────────────────────────────────────────────────────────┤
│  searchTerm?: string                                        │
│  category?: EventCategory                                   │
│  startDateFrom?: string                                     │
│  startDateTo?: string                                       │
│  state?: string                                             │
│  metroAreaIds?: string[]                                    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Used by
                     │
    ┌────────────────┼────────────────┐
    │                │                │
    ▼                ▼                ▼
┌─────────┐  ┌─────────────┐  ┌──────────────┐
│ Events  │  │  Dashboard  │  │  Repository  │
│  Page   │  │    Page     │  │   Methods    │
└─────────┘  └─────────────┘  └──────────────┘

Existing Interfaces (Extend, don't replace):

GetEventsRequest extends EventFilters {
  status?: EventStatus
  isFreeOnly?: boolean
  userId?: string
  latitude?: number
  longitude?: number
}

EventFiltersProps {
  onFiltersChange: (filters: EventFilters) => void
  initialFilters?: Partial<EventFilters>
  enabledFilters?: FilterConfig
  layout?: 'horizontal' | 'vertical' | 'compact'
  className?: string
}
```

---

## Error Handling Architecture

### Error Flow Diagram

```
User applies filter → API call fails

┌──────────────────┐
│  EventFilters    │
│  Component       │
└────────┬─────────┘
         │ onFiltersChange({ category: 1 })
         ▼
┌──────────────────┐
│  Dashboard Page  │
│  [useState]      │◄─── setRegisteredFilters({ category: 1 })
└────────┬─────────┘
         │ useEffect triggers
         ▼
┌──────────────────┐
│  React Query     │
│  useUserRsvps()  │
└────────┬─────────┘
         │ API call
         ▼
┌──────────────────┐
│  Repository      │
│  getUserRsvps()  │
└────────┬─────────┘
         │ HTTP GET
         ▼
┌──────────────────┐
│  Backend API     │
│  500 Error       │◄─── Database error
└────────┬─────────┘
         │ Error response
         ▼
┌──────────────────┐
│  React Query     │
│  error state     │◄─── { isError: true, error: {...} }
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Dashboard Page  │
│  Render error    │◄─── Show error message to user
└──────────────────┘
    "Failed to load events. Please try again."
    [Retry Button]
```

**Error Handling Strategy**:
1. React Query handles retries (3 attempts)
2. Display user-friendly error message
3. Provide retry button
4. Log errors to monitoring service
5. Preserve filter state (don't reset filters on error)

---

## Testing Strategy

### Unit Tests

```
EventFilters Component:
├── Renders all filter controls
├── Calls onFiltersChange when search input changes
├── Calls onFiltersChange when category selected
├── Calls onFiltersChange when date range selected
├── Calls onFiltersChange when location selected
├── Clears all filters when Clear button clicked
├── Debounces search input (500ms delay)
└── Respects enabledFilters prop (hides disabled filters)

EventsRepository:
├── getUserRsvps() without filters returns all events
├── getUserRsvps() with category filter adds query param
├── getUserRsvps() with multiple filters builds correct URL
├── getUserCreatedEvents() follows same pattern
└── Handles API errors correctly

React Query Hooks:
├── useUserRsvps() fetches data on mount
├── useUserRsvps() refetches when filters change
├── useUserRsvps() caches results by queryKey
└── useUserRsvps() handles errors gracefully
```

### Integration Tests

```
Dashboard Page - My Registered Events:
├── User can apply category filter
├── User can apply date range filter
├── User can apply location filter
├── User can apply search filter
├── User can combine multiple filters
├── User can clear all filters
├── Filter state persists when switching tabs
├── Empty state shows when no events match
└── Loading state shows during data fetch

Events Page:
├── User can search events by title
├── Search combines with existing filters
└── Search is debounced (500ms)
```

---

## Deployment Architecture

### Build Pipeline

```
┌────────────────────────────────────────────────────────────┐
│  Phase 1: Repository Layer                                 │
│  ✅ No breaking changes (optional parameters)              │
│  ✅ Can deploy independently                               │
│  ✅ Backward compatible                                    │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│  Phase 2: EventFilters Component                           │
│  ✅ New component (no impact on existing code)             │
│  ✅ Can develop in Storybook isolation                     │
│  ✅ Unit tests verify behavior                             │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│  Phase 3: Dashboard Integration                            │
│  ⚠️  Updates existing component (Dashboard)                │
│  ⚠️  Requires integration testing                          │
│  ✅ Feature flag can control rollout (optional)            │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│  Phase 4: Events Page Enhancement                          │
│  ⚠️  Updates existing component (Events page)              │
│  ⚠️  Requires regression testing                           │
│  ✅ Search feature is additive (no removal)                │
└────────────────┬───────────────────────────────────────────┘
                 │
                 ▼
┌────────────────────────────────────────────────────────────┐
│  Phase 5: Testing & QA                                     │
│  ✅ All phases tested together                             │
│  ✅ Staging deployment                                     │
│  ✅ Production rollout                                     │
└────────────────────────────────────────────────────────────┘
```

### Rollback Plan

```
If issues found in production:

Phase 3/4 Issues (UI problems):
├── Quick fix: Hide EventFilters component (CSS display: none)
├── Repository methods still work (backward compatible)
└── Rollback: Revert Dashboard/Events page changes only

Phase 1 Issues (Repository problems):
├── Unlikely (optional parameters are safe)
├── If needed: Revert repository changes
└── All existing code continues to work (no filters passed)

Complete Rollback:
├── Revert all 4 phases
├── Zero data loss (no database changes)
├── Zero API contract changes (backend unchanged)
└── System returns to pre-implementation state
```

---

## Monitoring and Observability

### Metrics to Track

```
Performance Metrics:
├── API response time (p50, p95, p99)
├── React Query cache hit rate
├── Filter application latency (client-side)
├── Search debounce effectiveness
└── Page load time with filters

User Behavior Metrics:
├── Filter usage frequency (which filters are used most)
├── Filter combination patterns (common combinations)
├── Search query analytics (popular search terms)
├── Clear Filters button usage
└── Mobile vs Desktop filter usage

Error Metrics:
├── API error rate (4xx, 5xx)
├── React Query retry count
├── Failed filter applications
└── JavaScript errors in EventFilters component

Business Metrics:
├── Event discovery rate (filtered vs unfiltered)
├── Registration conversion (filtered events)
├── User engagement (time on page with filters)
└── Feature adoption (% of users using filters)
```

---

## Security Considerations

### Input Validation

```
Search Term:
├── Max length: 200 characters
├── Sanitize SQL injection attempts
├── Escape special characters
└── Rate limiting: 10 searches/minute

Category Filter:
├── Validate against EventCategory enum
├── Reject invalid values
└── Type-safe (TypeScript enforces)

Date Range:
├── Validate ISO date format
├── Reject future dates (if applicable)
└── Ensure startDateFrom ≤ startDateTo

Location Filter:
├── Validate metro IDs against database
├── Reject invalid GUIDs
└── Limit array size (max 50 metros)

State Filter:
├── Validate against US_STATES constant
├── 2-letter state code only
└── Case-insensitive comparison
```

### Authorization

```
GET /api/events/my-registered-events
├── Requires authentication (JWT token)
├── Returns only current user's events
└── Filters applied AFTER user check

GET /api/events/my-events
├── Requires EventOrganizer or Admin role
├── Returns only current user's created events
└── Filters applied AFTER ownership check

GET /api/events
├── Public endpoint (no auth required)
├── Returns only published events
└── Filters applied to public events only
```

---

## Conclusion

This architecture follows Clean Architecture principles:

1. **Separation of Concerns**: Presentation → Application → Infrastructure → Domain
2. **Dependency Inversion**: Components depend on abstractions (interfaces), not implementations
3. **Single Responsibility**: Each component has one clear purpose
4. **Open/Closed**: Open for extension (new filters), closed for modification
5. **Testability**: All layers can be tested independently

**Key Strengths**:
- Backward compatible (no breaking changes)
- Type-safe (TypeScript throughout)
- Performant (React Query caching, debouncing)
- Maintainable (single reusable component)
- Scalable (easy to add new filters)
- Accessible (WCAG 2.1 AA compliant)
- Responsive (desktop and mobile optimized)

**Next Steps**: Proceed with implementation following the 5-phase plan in the ADR.

---

**Document Version**: 1.0
**Last Updated**: 2025-12-29
**Related**: [ARCHITECTURE_DECISION_EVENT_FILTRATION.md](./ARCHITECTURE_DECISION_EVENT_FILTRATION.md)
