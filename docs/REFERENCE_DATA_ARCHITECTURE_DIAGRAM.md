# Reference Data Architecture - Visual Diagrams

**Companion Document**: See `REFERENCE_DATA_ARCHITECTURE.md` for detailed specifications

---

## 1. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         React Application                           │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────┐    │
│  │                   Component Layer                          │    │
│  │                                                            │    │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │    │
│  │  │EventCreation │  │CategoryFilter│  │EventDetails  │   │    │
│  │  │Form          │  │              │  │Tab           │   │    │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘   │    │
│  │         │                 │                 │            │    │
│  │         └─────────────────┼─────────────────┘            │    │
│  │                          │                               │    │
│  └──────────────────────────┼───────────────────────────────┘    │
│                             │                                     │
│  ┌──────────────────────────┼───────────────────────────────┐    │
│  │                   Hook Layer                             │    │
│  │                          │                               │    │
│  │         ┌────────────────┴────────────────┐             │    │
│  │         │                                  │             │    │
│  │  ┌──────▼──────────┐           ┌──────────▼────────┐   │    │
│  │  │useEventCategories│          │useEventStatuses   │   │    │
│  │  │                 │           │                   │   │    │
│  │  │ - Data caching  │           │ - Data caching    │   │    │
│  │  │ - Error handling│           │ - Error handling  │   │    │
│  │  │ - Auto retry    │           │ - Auto retry      │   │    │
│  │  └──────┬──────────┘           └──────────┬────────┘   │    │
│  │         │                                  │            │    │
│  └─────────┼──────────────────────────────────┼────────────┘    │
│            │                                  │                 │
│  ┌─────────┼──────────────────────────────────┼────────────┐    │
│  │  Utility│Layer                            │            │    │
│  │         │                                  │            │    │
│  │  ┌──────▼──────────────────────────────────▼────────┐  │    │
│  │  │         enum-utils.ts                           │  │    │
│  │  │                                                  │  │    │
│  │  │  - getLabelFromIntValue()                      │  │    │
│  │  │  - getCodeFromIntValue()                       │  │    │
│  │  │  - buildIntToLabelMap()                        │  │    │
│  │  │  - buildCodeToIntMap()                         │  │    │
│  │  └──────┬───────────────────────────────────────────┘  │    │
│  │         │                                              │    │
│  └─────────┼──────────────────────────────────────────────┘    │
│            │                                                    │
│  ┌─────────┼────────────────────────────────────────────────┐  │
│  │  Service│Layer                                           │  │
│  │         │                                                │  │
│  │  ┌──────▼──────────────────────────────────────────────┐│  │
│  │  │    referenceData.service.ts                        ││  │
│  │  │                                                     ││  │
│  │  │    getReferenceDataByTypes(types, activeOnly)     ││  │
│  │  │                                                     ││  │
│  │  │    ┌─────────────────────────────────────┐        ││  │
│  │  │    │ API Client (apiClient.get())        │        ││  │
│  │  │    └─────────────────────────────────────┘        ││  │
│  │  └──────┬──────────────────────────────────────────────┘│  │
│  │         │                                                │  │
│  └─────────┼────────────────────────────────────────────────┘  │
└────────────┼───────────────────────────────────────────────────┘
             │
             │ HTTP GET /api/reference-data?types=EventCategory,EventStatus
             │
┌────────────▼───────────────────────────────────────────────────────┐
│                      ASP.NET Core Backend                          │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────┐    │
│  │               ReferenceDataController                      │    │
│  │                                                            │    │
│  │    GET /api/reference-data                                │    │
│  │    - Parse query params (types, activeOnly)               │    │
│  │    - Call repository                                      │    │
│  │    - Return JSON                                          │    │
│  └──────────────────────────┬─────────────────────────────────┘    │
│                             │                                     │
│  ┌──────────────────────────▼─────────────────────────────────┐    │
│  │           ReferenceDataRepository                          │    │
│  │                                                            │    │
│  │    - Memory cache (1 hour)                                │    │
│  │    - Query database                                       │    │
│  │    - Filter by type and isActive                          │    │
│  └──────────────────────────┬─────────────────────────────────┘    │
│                             │                                     │
└─────────────────────────────┼──────────────────────────────────────┘
                              │
                              │ SQL Query
                              │
┌─────────────────────────────▼──────────────────────────────────────┐
│                      PostgreSQL Database                           │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────┐    │
│  │           ReferenceData Table                              │    │
│  │                                                            │    │
│  │  id         | enumType      | code      | intValue | name│    │
│  │  ───────────┼───────────────┼───────────┼──────────┼─────│    │
│  │  uuid-1     | EventCategory | Religious | 0        | ... │    │
│  │  uuid-2     | EventCategory | Cultural  | 1        | ... │    │
│  │  uuid-3     | EventStatus   | Draft     | 0        | ... │    │
│  │  uuid-4     | Currency      | USD       | 1        | ... │    │
│  │                                                            │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Data Flow Sequence Diagram

```
┌─────────┐        ┌──────────┐      ┌─────────┐      ┌─────────┐      ┌──────────┐
│Component│        │   Hook   │      │ Service │      │ Backend │      │ Database │
└────┬────┘        └────┬─────┘      └────┬────┘      └────┬────┘      └────┬─────┘
     │                  │                  │                │                │
     │ useEventCategories()                │                │                │
     ├─────────────────>│                  │                │                │
     │                  │                  │                │                │
     │                  │ Check React Query Cache           │                │
     │                  ├──────────────────┤                │                │
     │                  │                  │                │                │
     │                  │ CACHE MISS       │                │                │
     │                  │                  │                │                │
     │                  │ getReferenceDataByTypes(['EventCategory'])        │
     │                  ├─────────────────>│                │                │
     │                  │                  │                │                │
     │                  │                  │ GET /api/reference-data        │
     │                  │                  ├───────────────>│                │
     │                  │                  │                │                │
     │                  │                  │                │ Check Memory Cache
     │                  │                  │                ├───────────────┤
     │                  │                  │                │                │
     │                  │                  │                │ CACHE MISS    │
     │                  │                  │                │                │
     │                  │                  │                │ SELECT * FROM ReferenceData
     │                  │                  │                │ WHERE enumType = 'EventCategory'
     │                  │                  │                ├───────────────>│
     │                  │                  │                │                │
     │                  │                  │                │ ReferenceValue[]
     │                  │                  │                │<───────────────┤
     │                  │                  │                │                │
     │                  │                  │                │ Store in cache (1 hour)
     │                  │                  │                ├───────────────┤
     │                  │                  │                │                │
     │                  │                  │ JSON Response  │                │
     │                  │                  │<───────────────┤                │
     │                  │                  │                │                │
     │                  │ ReferenceValue[] │                │                │
     │                  │<─────────────────┤                │                │
     │                  │                  │                │                │
     │                  │ Transform to EnumOption[]         │                │
     │                  ├──────────────────┤                │                │
     │                  │                  │                │                │
     │                  │ Store in React Query Cache (1 hour)              │
     │                  ├──────────────────┤                │                │
     │                  │                  │                │                │
     │ EnumOption[]     │                  │                │                │
     │<─────────────────┤                  │                │                │
     │                  │                  │                │                │
     │ Render dropdown  │                  │                │                │
     ├─────────────────┤                  │                │                │
     │                  │                  │                │                │
     │                  │                  │                │                │
     │ (5 minutes later)│                  │                │                │
     │ useEventCategories() - SAME COMPONENT               │                │
     ├─────────────────>│                  │                │                │
     │                  │                  │                │                │
     │                  │ Check React Query Cache           │                │
     │                  ├──────────────────┤                │                │
     │                  │                  │                │                │
     │                  │ CACHE HIT ✓      │                │                │
     │                  │                  │                │                │
     │ EnumOption[] (cached)               │                │                │
     │<─────────────────┤                  │                │                │
     │                  │                  │                │                │
     │ Instant render ✓ │                  │                │                │
     └──────────────────┘──────────────────┘────────────────┘────────────────┘
```

---

## 3. Component Dependency Graph

```
┌──────────────────────────────────────────────────────────────────────┐
│                      Component Dependencies                          │
└──────────────────────────────────────────────────────────────────────┘

                    ┌────────────────────┐
                    │   App Layout       │
                    │  (Prefetcher)      │
                    └─────────┬──────────┘
                              │ Prefetch all enums
                              │ on app initialization
                              ▼
                    ┌────────────────────┐
                    │  React Query Cache │
                    │                    │
                    │  EventCategories   │
                    │  EventStatuses     │
                    │  Currencies        │
                    └─────────┬──────────┘
                              │
                ┌─────────────┼─────────────┐
                │             │             │
                ▼             ▼             ▼
     ┌──────────────┐  ┌──────────┐  ┌────────────┐
     │EventCreation │  │Category  │  │EventDetails│
     │Form          │  │Filter    │  │Tab         │
     └──────┬───────┘  └─────┬────┘  └──────┬─────┘
            │                │               │
            │ useEventCategories()          │
            ├────────────────┼───────────────┤
            │                │               │
            ▼                ▼               ▼
     ┌──────────────────────────────────────────┐
     │      useEventCategories() Hook           │
     │                                          │
     │  - Returns: EnumOption[]                 │
     │  - Caches: 1 hour                        │
     │  - Retries: 3x exponential backoff       │
     └──────────────┬───────────────────────────┘
                    │
                    ▼
     ┌──────────────────────────────────────────┐
     │      getReferenceDataByTypes()           │
     │                                          │
     │  - API: /api/reference-data              │
     │  - Params: types, activeOnly             │
     └──────────────┬───────────────────────────┘
                    │
                    ▼
            Backend API (ASP.NET Core)

┌──────────────────────────────────────────────────────────────────────┐
│                   11 Components to Migrate                           │
└──────────────────────────────────────────────────────────────────────┘

HIGH PRIORITY (Core functionality)
┌─────────────────────────────────────────────────────────────────────┐
│ 1. CategoryFilter.tsx          - Filter dropdown                    │
│ 2. EventCreationForm.tsx       - Category selection dropdown        │
│ 3. events/page.tsx             - 3 category filter usages           │
└─────────────────────────────────────────────────────────────────────┘

MEDIUM PRIORITY (Display components)
┌─────────────────────────────────────────────────────────────────────┐
│ 4. EventDetailsTab.tsx         - Category label mapping             │
│ 5. EventsList.tsx              - 2 switch statements                │
│ 6. events/[id]/page.tsx        - Category display                   │
│ 7. EventEditForm.tsx           - 2 label mappings                   │
└─────────────────────────────────────────────────────────────────────┘

LOW PRIORITY (Edge cases)
┌─────────────────────────────────────────────────────────────────────┐
│ 8. templates/page.tsx          - Category lookup                    │
│ 9. events/[id]/manage/page.tsx - Status/category labels             │
│ 10. lib/enum-utils.ts          - Add new utilities                  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 4. Hook Architecture Pattern

```
┌──────────────────────────────────────────────────────────────────────┐
│                    Specialized Hook Pattern                          │
└──────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  useEventCategories()                                               │
│                                                                     │
│  Input:  None (or activeOnly: boolean)                             │
│  Output: UseQueryResult<EnumOption[], Error>                       │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐ │
│  │ Responsibilities:                                             │ │
│  │                                                               │ │
│  │  ✓ Fetch from API (getReferenceDataByTypes)                  │ │
│  │  ✓ Transform ReferenceValue[] → EnumOption[]                 │ │
│  │  ✓ Sort by displayOrder                                      │ │
│  │  ✓ Cache for 1 hour (staleTime)                              │ │
│  │  ✓ Retry 3x on failure (exponential backoff)                 │ │
│  │  ✓ Show toast notification on error                          │ │
│  │  ✓ Return { data, isLoading, isError, refetch }              │ │
│  └───────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  useEventStatuses()                                                 │
│                                                                     │
│  Input:  None (or activeOnly: boolean)                             │
│  Output: UseQueryResult<EnumOption[], Error>                       │
│                                                                     │
│  (Same pattern as useEventCategories)                              │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  useCurrencies()                                                    │
│                                                                     │
│  Input:  None (or activeOnly: boolean)                             │
│  Output: UseQueryResult<EnumOption[], Error>                       │
│                                                                     │
│  (Same pattern as useEventCategories)                              │
└─────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────┐
│                      EnumOption Interface                            │
│                                                                      │
│  {                                                                   │
│    value: number;           // Enum int value (e.g., 0, 1, 2)       │
│    code: string;            // Database code (e.g., "Religious")    │
│    label: string;           // Display name (e.g., "Religious")     │
│    description?: string;    // Optional description                 │
│    displayOrder: number;    // Sort order                           │
│  }                                                                   │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 5. Utility Function Call Graph

```
┌──────────────────────────────────────────────────────────────────────┐
│                  Utility Function Usage Patterns                     │
└──────────────────────────────────────────────────────────────────────┘

PATTERN 1: Single Label Lookup
┌─────────────────────────────────────────────────────────────────────┐
│ Component                                                           │
│   ↓                                                                 │
│ useEventCategories() → categories: EnumOption[]                     │
│   ↓                                                                 │
│ getLabelFromIntValue(categories, EventCategory.Religious)           │
│   ↓                                                                 │
│ "Religious Events" (string)                                         │
└─────────────────────────────────────────────────────────────────────┘

PATTERN 2: Code to Int Mapping
┌─────────────────────────────────────────────────────────────────────┐
│ Component                                                           │
│   ↓                                                                 │
│ useEventCategories() → categories: EnumOption[]                     │
│   ↓                                                                 │
│ getIntValueFromCode(categories, "Religious")                        │
│   ↓                                                                 │
│ 0 (EventCategory.Religious)                                         │
└─────────────────────────────────────────────────────────────────────┘

PATTERN 3: Multiple Lookups in Loop (Optimized)
┌─────────────────────────────────────────────────────────────────────┐
│ Component                                                           │
│   ↓                                                                 │
│ useEventCategories() → categories: EnumOption[]                     │
│   ↓                                                                 │
│ useMemo(() => buildIntToLabelMap(categories), [categories])         │
│   ↓                                                                 │
│ Map<number, string> { 0 → "Religious", 1 → "Cultural", ... }        │
│   ↓                                                                 │
│ events.map(e => labelMap.get(e.category))  // O(1) lookup           │
└─────────────────────────────────────────────────────────────────────┘

PATTERN 4: Type-Safe Wrappers
┌─────────────────────────────────────────────────────────────────────┐
│ Component                                                           │
│   ↓                                                                 │
│ useEventCategories() → categories: EnumOption[]                     │
│   ↓                                                                 │
│ getEventCategoryLabelFromData(categories, event.category)           │
│   ↓                                                                 │
│ "Religious Events" (with type safety)                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 6. Caching Strategy Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                        Multi-Layer Cache                             │
└──────────────────────────────────────────────────────────────────────┘

Time: T0 (First Load)
┌─────────────────────────────────────────────────────────────────────┐
│ Browser (React Query Cache)                                         │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: EMPTY                                  │       │
│   └─────────────────────────────────────────────────────────┘       │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ CACHE MISS → Fetch from backend
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Backend (ASP.NET Memory Cache)                                      │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: EMPTY                                  │       │
│   └─────────────────────────────────────────────────────────┘       │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ CACHE MISS → Query database
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│ PostgreSQL Database                                                 │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ SELECT * FROM ReferenceData                             │       │
│   │ WHERE EnumType = 'EventCategory'                        │       │
│   └─────────────────────────────────────────────────────────┘       │
│   Response: 8 rows (~200ms)                                         │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ Store in backend cache
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Backend (ASP.NET Memory Cache) - UPDATED                            │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: [8 items] (expires in 1 hour)          │       │
│   └─────────────────────────────────────────────────────────┘       │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ Return to frontend
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Browser (React Query Cache) - UPDATED                               │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: [8 items] (stale in 1 hour)            │       │
│   └─────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────┘

─────────────────────────────────────────────────────────────────────────

Time: T0 + 5 minutes (Second Load - Same Session)
┌─────────────────────────────────────────────────────────────────────┐
│ Browser (React Query Cache)                                         │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: [8 items] ✓ FRESH                      │       │
│   └─────────────────────────────────────────────────────────┘       │
│   Response: Instant (~10ms) - NO API CALL                           │
└─────────────────────────────────────────────────────────────────────┘

─────────────────────────────────────────────────────────────────────────

Time: T0 + 65 minutes (After 1 hour - Cache Expired)
┌─────────────────────────────────────────────────────────────────────┐
│ Browser (React Query Cache)                                         │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: [8 items] ⚠ STALE                      │       │
│   └─────────────────────────────────────────────────────────┘       │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ Background refetch
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Backend (ASP.NET Memory Cache)                                      │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: [8 items] ✓ FRESH                      │       │
│   └─────────────────────────────────────────────────────────┘       │
│   Response: Fast (~50ms) - FROM BACKEND CACHE                       │
└───────────────────────────────┬─────────────────────────────────────┘
                                │ Return to frontend
                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│ Browser (React Query Cache) - REFRESHED                             │
│   ┌─────────────────────────────────────────────────────────┐       │
│   │ EventCategories: [8 items] ✓ FRESH (stale in 1 hour)    │       │
│   └─────────────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 7. Error Handling Flow

```
┌──────────────────────────────────────────────────────────────────────┐
│                      Error Handling Strategy                         │
└──────────────────────────────────────────────────────────────────────┘

SCENARIO 1: Network Timeout
┌─────────────────────────────────────────────────────────────────────┐
│ Component renders                                                   │
│   ↓                                                                 │
│ useEventCategories() - API call                                     │
│   ↓                                                                 │
│ ❌ Network timeout (30 seconds)                                     │
│   ↓                                                                 │
│ Retry #1 (after 1 second)                                           │
│   ↓                                                                 │
│ ❌ Network timeout (30 seconds)                                     │
│   ↓                                                                 │
│ Retry #2 (after 2 seconds)                                          │
│   ↓                                                                 │
│ ❌ Network timeout (30 seconds)                                     │
│   ↓                                                                 │
│ Retry #3 (after 4 seconds)                                          │
│   ↓                                                                 │
│ ❌ Network timeout (30 seconds)                                     │
│   ↓                                                                 │
│ Give up - isError = true                                            │
│   ↓                                                                 │
│ Show toast: "Failed to load categories. Retrying..."               │
│   ↓                                                                 │
│ Component shows error state with retry button                       │
└─────────────────────────────────────────────────────────────────────┘

SCENARIO 2: Transient Backend Error (503)
┌─────────────────────────────────────────────────────────────────────┐
│ Component renders                                                   │
│   ↓                                                                 │
│ useEventCategories() - API call                                     │
│   ↓                                                                 │
│ ❌ 503 Service Unavailable                                          │
│   ↓                                                                 │
│ Retry #1 (after 1 second)                                           │
│   ↓                                                                 │
│ ✓ 200 OK - Success!                                                 │
│   ↓                                                                 │
│ Component renders normally                                          │
│   ↓                                                                 │
│ User never sees error (seamless recovery)                           │
└─────────────────────────────────────────────────────────────────────┘

SCENARIO 3: Database Error (500)
┌─────────────────────────────────────────────────────────────────────┐
│ Component renders                                                   │
│   ↓                                                                 │
│ useEventCategories() - API call                                     │
│   ↓                                                                 │
│ ❌ 500 Internal Server Error (database down)                        │
│   ↓                                                                 │
│ Retry #1, #2, #3 - All fail                                         │
│   ↓                                                                 │
│ Show toast: "Failed to load categories."                            │
│   ↓                                                                 │
│ Component Error State:                                              │
│   ┌─────────────────────────────────────────────────────┐           │
│   │ ⚠ Failed to load categories.                       │           │
│   │                                                     │           │
│   │ [Retry Button]                                     │           │
│   └─────────────────────────────────────────────────────┘           │
│   ↓                                                                 │
│ User clicks "Retry"                                                 │
│   ↓                                                                 │
│ refetch() - Retry API call                                          │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 8. Migration Phases Timeline

```
┌──────────────────────────────────────────────────────────────────────┐
│                     Implementation Timeline                          │
└──────────────────────────────────────────────────────────────────────┘

WEEK 1: Foundation
┌─────────────────────────────────────────────────────────────────────┐
│ Day 1-2: Infrastructure                                             │
│   ✓ Create useEventCategories() hook                                │
│   ✓ Create useEventStatuses() hook                                  │
│   ✓ Create useCurrencies() hook                                     │
│   ✓ Add utility functions to enum-utils.ts                          │
│   ✓ Write unit tests                                                │
│                                                                     │
│ Day 3: Prefetching                                                  │
│   ✓ Create ReferenceDataPrefetcher component                        │
│   ✓ Add to app layout                                               │
│   ✓ Test cache behavior                                             │
│                                                                     │
│ Day 4-5: Core Components (3)                                        │
│   ✓ Migrate CategoryFilter.tsx                                      │
│   ✓ Migrate EventCreationForm.tsx                                   │
│   ✓ Migrate events/page.tsx (3 usages)                              │
└─────────────────────────────────────────────────────────────────────┘

WEEK 2: Display Components
┌─────────────────────────────────────────────────────────────────────┐
│ Day 1-2: Display Components (4)                                     │
│   ✓ Migrate EventDetailsTab.tsx                                     │
│   ✓ Migrate EventsList.tsx (2 switch statements)                    │
│   ✓ Migrate events/[id]/page.tsx                                    │
│   ✓ Migrate EventEditForm.tsx (2 mappings)                          │
│                                                                     │
│ Day 3: Edge Cases (3)                                               │
│   ✓ Migrate templates/page.tsx                                      │
│   ✓ Migrate events/[id]/manage/page.tsx                             │
│   ✓ Update enum-utils.ts (deprecate old functions)                  │
│                                                                     │
│ Day 4: Testing                                                      │
│   ✓ Run all grep commands (verify no violations)                    │
│   ✓ Run typecheck (0 errors)                                        │
│   ✓ Run build (0 errors)                                            │
│   ✓ Complete manual testing checklist                               │
│                                                                     │
│ Day 5: Documentation & Cleanup                                      │
│   ✓ Update PROGRESS_TRACKER.md                                      │
│   ✓ Create Phase summary document                                   │
│   ✓ Remove deprecated functions (after warning period)              │
└─────────────────────────────────────────────────────────────────────┘

Total: 10 days
Risk: Medium (breaking changes possible)
Rollback: Git revert or feature flag
```

---

## 9. Testing Strategy Diagram

```
┌──────────────────────────────────────────────────────────────────────┐
│                      Three-Layer Testing                             │
└──────────────────────────────────────────────────────────────────────┘

LAYER 1: Automated Grep Tests
┌─────────────────────────────────────────────────────────────────────┐
│ grep -r "EventCategory\.(Religious|Cultural)" web/src               │
│   ↓                                                                 │
│ ✓ No matches (all hardcoded enums replaced)                         │
│                                                                     │
│ grep -r "switch.*EventCategory" web/src                             │
│   ↓                                                                 │
│ ✓ No matches (all switch statements replaced)                       │
│                                                                     │
│ grep -r "const.*CATEGORY_OPTIONS" web/src                           │
│   ↓                                                                 │
│ ✓ No matches (no hardcoded arrays)                                  │
└─────────────────────────────────────────────────────────────────────┘

LAYER 2: TypeScript Compilation
┌─────────────────────────────────────────────────────────────────────┐
│ npm run typecheck                                                   │
│   ↓                                                                 │
│ ✓ 0 errors (all types correct)                                      │
│                                                                     │
│ npm run build                                                       │
│   ↓                                                                 │
│ ✓ Build succeeded (no runtime errors)                               │
└─────────────────────────────────────────────────────────────────────┘

LAYER 3: Manual Testing
┌─────────────────────────────────────────────────────────────────────┐
│ Test Case 1: Category Dropdown                                     │
│   ✓ Loads instantly (no spinner)                                    │
│   ✓ Shows 8 categories                                              │
│   ✓ Labels are correct                                              │
│   ✓ Selection saves integer value                                   │
│                                                                     │
│ Test Case 2: Category Filter                                       │
│   ✓ Filter works correctly                                          │
│   ✓ "All Categories" shows all events                               │
│   ✓ URL params update                                               │
│                                                                     │
│ Test Case 3: Event Display                                         │
│   ✓ Category badge shows correct label                              │
│   ✓ Status badge shows correct label                                │
│   ✓ No "Unknown" labels                                             │
│                                                                     │
│ Test Case 4: Error Handling                                        │
│   ✓ Network disconnect shows retry button                           │
│   ✓ Toast notification appears                                      │
│   ✓ Auto-retry works                                                │
│                                                                     │
│ Test Case 5: Performance                                           │
│   ✓ Initial load < 2 seconds                                        │
│   ✓ Reference data cached (Network tab)                             │
│   ✓ No duplicate API calls                                          │
│   ✓ Dropdowns respond instantly                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Document Metadata

- **Created**: 2025-12-28
- **Version**: 1.0
- **Author**: System Architecture Designer
- **Related**: `REFERENCE_DATA_ARCHITECTURE.md`
- **Status**: Design Complete

**Next Steps**:
1. Review diagrams with team
2. Validate architecture decisions
3. Begin implementation (Phase 1)
