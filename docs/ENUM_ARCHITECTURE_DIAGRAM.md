# Enum Replacement Architecture Diagram
**Phase 6A.47 Follow-up**
**Date**: 2025-12-28

## Current Architecture (BROKEN)

```
┌─────────────────────────────────────────────────────────────┐
│                      UI Components                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐    ┌──────────────────┐              │
│  │ CategoryFilter   │    │ EventEditForm    │              │
│  │                  │    │                  │              │
│  │ ❌ HARDCODED     │    │ ❌ HARDCODED     │              │
│  │ enum dropdown    │    │ enum dropdown    │              │
│  └────────┬─────────┘    └────────┬─────────┘              │
│           │                       │                         │
│           │                       │                         │
└───────────┼───────────────────────┼─────────────────────────┘
            │                       │
            │                       │
            ▼                       ▼
    ┌───────────────┐      ┌────────────────┐
    │ enum-utils.ts │      │ categoryMap    │
    │               │      │ (inline code)  │
    │ ❌ Reads enum │      │ ❌ Hardcoded   │
    │ keys directly │      │ object literal │
    └───────┬───────┘      └────────────────┘
            │
            │
            ▼
    ┌───────────────────────┐
    │ EventCategory enum    │
    │                       │
    │ Religious = 0         │
    │ Cultural = 1          │
    │ Community = 2         │
    │ ...                   │
    │                       │
    │ ❌ HARDCODED VALUES   │
    └───────────────────────┘

┌────────────────────────────────────────────┐
│          Database (IGNORED)                │
├────────────────────────────────────────────┤
│  ReferenceData Table                       │
│  ┌──────────┬─────────┬──────────────┐    │
│  │ EnumType │ Code    │ IntValue     │    │
│  ├──────────┼─────────┼──────────────┤    │
│  │ EventCat │ RELIG   │ 0            │    │
│  │ EventCat │ CULT    │ 1            │    │
│  │ EventCat │ COMM    │ 2            │    │
│  └──────────┴─────────┴──────────────┘    │
│                                            │
│  ✅ Single source of truth                │
│  ⚠️ BUT NOT BEING USED BY UI              │
└────────────────────────────────────────────┘
```

## Target Architecture (CORRECT)

```
┌─────────────────────────────────────────────────────────────┐
│                      UI Components                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐    ┌──────────────────┐              │
│  │ CategoryFilter   │    │ EventEditForm    │              │
│  │                  │    │                  │              │
│  │ ✅ useEventCat() │    │ ✅ useEventCat() │              │
│  │ + loading state  │    │ + loading state  │              │
│  └────────┬─────────┘    └────────┬─────────┘              │
│           │                       │                         │
│           │                       │                         │
└───────────┼───────────────────────┼─────────────────────────┘
            │                       │
            │                       │
            ▼                       ▼
    ┌─────────────────────────────────────┐
    │  Specialized Hooks (NEW)            │
    ├─────────────────────────────────────┤
    │  useEventCategories()               │
    │  useEventStatuses()                 │
    │  useUserRoles()                     │
    │  useCurrencies()                    │
    │                                     │
    │  ✅ Formats API data for dropdowns  │
    │  ✅ Handles loading states          │
    │  ✅ Returns { value, label }[]      │
    └────────┬────────────────────────────┘
             │
             │
             ▼
    ┌─────────────────────────────────────┐
    │  Base Hook: useReferenceData()      │
    │                                     │
    │  ✅ React Query caching (1 hour)    │
    │  ✅ Fetches from API                │
    └────────┬────────────────────────────┘
             │
             │
             ▼
    ┌─────────────────────────────────────┐
    │  API Client                         │
    │                                     │
    │  GET /api/reference-data?types=...  │
    └────────┬────────────────────────────┘
             │
             │
             ▼
┌─────────────────────────────────────────────────┐
│          Backend API (C#)                       │
├─────────────────────────────────────────────────┤
│  ReferenceDataController.cs                     │
│                                                 │
│  ✅ Cached in memory (1 hour)                   │
│  ✅ Returns ReferenceValueDto[]                 │
└────────┬────────────────────────────────────────┘
         │
         │
         ▼
┌─────────────────────────────────────────────────┐
│          Database (SOURCE OF TRUTH)             │
├─────────────────────────────────────────────────┤
│  ReferenceData Table                            │
│  ┌──────────┬─────────┬──────────┬──────────┐  │
│  │ EnumType │ Code    │ IntValue │ Name     │  │
│  ├──────────┼─────────┼──────────┼──────────┤  │
│  │ EventCat │ RELIG   │ 0        │Religious │  │
│  │ EventCat │ CULT    │ 1        │Cultural  │  │
│  │ EventCat │ COMM    │ 2        │Community │  │
│  │ EventCat │ EDU     │ 3        │Education │  │
│  │ EventCat │ SOC     │ 4        │Social    │  │
│  └──────────┴─────────┴──────────┴──────────┘  │
│                                                 │
│  ✅ Single source of truth                     │
│  ✅ ACTUALLY USED BY UI                         │
└─────────────────────────────────────────────────┘
```

## Data Flow Comparison

### Current (BROKEN)
```
User clicks dropdown
    ↓
CategoryFilter.tsx calls getEventCategoryOptions()
    ↓
enum-utils.ts reads EventCategory enum keys
    ↓
Returns hardcoded array [Religious, Cultural, ...]
    ↓
Dropdown shows hardcoded values
    ↓
❌ Database is completely bypassed
```

### Target (CORRECT)
```
User clicks dropdown
    ↓
CategoryFilter.tsx uses useEventCategories()
    ↓
useEventCategories() calls useReferenceData(['EventCategory'])
    ↓
useReferenceData() checks React Query cache
    ↓
Cache miss → API call to /reference-data
    ↓
Backend reads from ReferenceData table (cached 1 hour)
    ↓
Returns [{ code: 'RELIG', intValue: 0, name: 'Religious' }, ...]
    ↓
useEventCategories() maps to [{ value: 0, label: 'Religious' }, ...]
    ↓
Dropdown shows database values
    ↓
✅ Database is the source of truth
```

## Component Integration Examples

### Before (CategoryFilter.tsx)
```typescript
// ❌ WRONG - Uses hardcoded enum helper
import { getEventCategoryOptions } from '@/lib/enum-utils';

export function CategoryFilter({ value, onChange }) {
  const categories = getEventCategoryOptions(); // Hardcoded!

  return (
    <select value={value} onChange={onChange}>
      <option value="">All Categories</option>
      {categories.map((cat) => (
        <option key={cat.value} value={cat.value}>
          {cat.label}
        </option>
      ))}
    </select>
  );
}
```

### After (CategoryFilter.tsx)
```typescript
// ✅ CORRECT - Uses database API
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';

export function CategoryFilter({ value, onChange }) {
  const { data: categories, isLoading } = useEventCategories();

  if (isLoading) {
    return <Loader2 className="animate-spin" />;
  }

  return (
    <select value={value} onChange={onChange}>
      <option value="">All Categories</option>
      {categories?.map((cat) => (
        <option key={cat.value} value={cat.value}>
          {cat.label}
        </option>
      ))}
    </select>
  );
}
```

## Hook Architecture

```
┌─────────────────────────────────────────────────────────┐
│          Specialized Hooks (Consumer-Facing)            │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  useEventCategories()  →  Returns: { value, label }[]   │
│  useEventStatuses()    →  Returns: { value, label }[]   │
│  useUserRoles()        →  Returns: { value, label }[]   │
│  useCurrencies()       →  Returns: { value, label }[]   │
│                                                          │
│  Purpose: Format data for UI consumption                │
│  Benefits:                                               │
│    - Simple, dropdown-ready format                       │
│    - Loading state handling                              │
│    - Error handling                                      │
│    - Type-safe                                           │
└─────────────┬───────────────────────────────────────────┘
              │
              │ Calls
              │
              ▼
┌─────────────────────────────────────────────────────────┐
│          Base Hook (Data-Fetching)                      │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  useReferenceData(types: string[], activeOnly: bool)    │
│                                                          │
│  Purpose: Fetch raw reference data from API             │
│  Returns: ReferenceValue[] (database format)            │
│  Benefits:                                               │
│    - React Query caching (1 hour)                       │
│    - Automatic refetching                                │
│    - Error handling                                      │
│    - Stale-while-revalidate                             │
└─────────────┬───────────────────────────────────────────┘
              │
              │ Calls
              │
              ▼
┌─────────────────────────────────────────────────────────┐
│          Service Layer                                  │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  getReferenceDataByTypes(types, activeOnly)             │
│                                                          │
│  Purpose: API communication                             │
│  Benefits:                                               │
│    - Centralized error handling                          │
│    - Request/response interceptors                       │
│    - Type safety                                         │
└─────────────┬───────────────────────────────────────────┘
              │
              │ HTTP GET
              │
              ▼
          Backend API
```

## Type Safety Flow

```
┌────────────────────────────────────────────────────────┐
│              Database (Runtime)                        │
├────────────────────────────────────────────────────────┤
│  SELECT EnumType, Code, IntValue, Name                 │
│  FROM ReferenceData                                    │
│  WHERE EnumType IN ('EventCategory')                   │
│                                                        │
│  Returns: JSON array                                   │
└────────┬───────────────────────────────────────────────┘
         │
         │ API Response
         │
         ▼
┌────────────────────────────────────────────────────────┐
│              API Layer (TypeScript)                    │
├────────────────────────────────────────────────────────┤
│  interface ReferenceValue {                            │
│    id: string;                                         │
│    enumType: string;                                   │
│    code: string;                                       │
│    intValue: number | null;                            │
│    name: string;                                       │
│    description: string | null;                         │
│    displayOrder: number;                               │
│    isActive: boolean;                                  │
│  }                                                     │
│                                                        │
│  ✅ Validated at compile-time                         │
└────────┬───────────────────────────────────────────────┘
         │
         │ Transform
         │
         ▼
┌────────────────────────────────────────────────────────┐
│              UI Layer (TypeScript)                     │
├────────────────────────────────────────────────────────┤
│  interface DropdownOption {                            │
│    value: number;        // intValue from DB           │
│    label: string;        // name from DB               │
│  }                                                     │
│                                                        │
│  Specialized hooks map:                                │
│    ReferenceValue[] → DropdownOption[]                 │
│                                                        │
│  ✅ Type-safe transformation                           │
└────────────────────────────────────────────────────────┘
```

## Mapping Strategy

### Code → IntValue Mapping
```typescript
// Reference Data from DB:
[
  { code: 'RELIG', intValue: 0, name: 'Religious' },
  { code: 'CULT', intValue: 1, name: 'Cultural' },
  { code: 'COMM', intValue: 2, name: 'Community' },
]

// Mapping Function:
function buildCodeToIntMap(refData: ReferenceValue[]): Record<string, number> {
  return refData.reduce((acc, item) => {
    if (item.intValue !== null) {
      acc[item.code] = item.intValue;
    }
    return acc;
  }, {});
}

// Result:
{
  'RELIG': 0,
  'CULT': 1,
  'COMM': 2,
}

// Usage in Forms:
const categoryInt = codeToIntMap[selectedCode]; // 'RELIG' → 0
```

### IntValue → Name Mapping
```typescript
// For Display (e.g., showing existing event category):
function getNameFromIntValue(intValue: number, refData: ReferenceValue[]): string {
  return refData.find(item => item.intValue === intValue)?.name ?? 'Unknown';
}

// Usage:
const displayName = getNameFromIntValue(0, categories); // 0 → 'Religious'
```

## Performance Considerations

### Caching Strategy
```
┌──────────────────────────────────────────────────┐
│           Multi-Layer Caching                    │
├──────────────────────────────────────────────────┤
│                                                  │
│  Layer 1: React Query (Frontend)                │
│    - staleTime: 1 hour                           │
│    - gcTime: 24 hours                            │
│    - Shared across all components               │
│    - Automatic background refetch                │
│                                                  │
│  Layer 2: Backend Memory Cache (C#)             │
│    - MemoryCache with 1 hour expiration          │
│    - Shared across all API requests             │
│    - Invalidated on reference data updates      │
│                                                  │
│  Layer 3: Database (PostgreSQL)                 │
│    - Infrequent reads (only on cache miss)       │
│    - Optimized with indexes                      │
│                                                  │
└──────────────────────────────────────────────────┘

Result: Near-instant dropdown rendering
        Database hit only once per hour
```

### Loading State Flow
```
Component mounts
    ↓
useEventCategories() called
    ↓
React Query checks cache
    ↓
┌─────────────┬─────────────┐
│  Cache Hit  │ Cache Miss  │
├─────────────┼─────────────┤
│ Instant     │ Loading...  │
│ Return data │ API call    │
│ isLoading:  │ isLoading:  │
│   false     │   true      │
│             │             │
│             │ ↓           │
│             │ Show        │
│             │ <Loader2/>  │
│             │             │
│             │ ↓           │
│             │ Data loaded │
│             │ Re-render   │
└─────────────┴─────────────┘
```

## Error Handling

```
API Call
    ↓
┌─────────────────────────────────┐
│  Success                        │
│  - Data cached                  │
│  - Component renders dropdown   │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  Network Error                  │
│  - Show error message           │
│  - Retry button                 │
│  - Fallback to cached data      │
└─────────────────────────────────┘
    ↓
┌─────────────────────────────────┐
│  API Error (500)                │
│  - Show error message           │
│  - Log to monitoring            │
│  - Contact support CTA          │
└─────────────────────────────────┘
```

## Migration Path

### Step 1: Add New Infrastructure
```
✅ Create useEventCategories() hook
✅ Create useEventStatuses() hook
✅ Create useUserRoles() hook
✅ Create useCurrencies() hook
✅ Create enum-mappers.ts utilities
```

### Step 2: Update Components
```
✅ CategoryFilter.tsx
✅ EventEditForm.tsx
✅ EventCreationForm.tsx
```

### Step 3: Deprecate Old Code
```
✅ Add deprecation warning to enum-utils.ts
✅ Document migration in CHANGELOG
```

### Step 4: Cleanup (Future)
```
⏳ Remove enum-utils.ts completely
⏳ Remove hardcoded fallback arrays
⏳ Remove unused enum helper functions
```

## Benefits Summary

| Aspect | Before (Hardcoded) | After (Database-Driven) |
|--------|-------------------|------------------------|
| **Source of Truth** | Enum in code | Database |
| **Updates** | Code change + deploy | Database update only |
| **Consistency** | Can drift | Always in sync |
| **Flexibility** | Fixed at compile | Runtime configurable |
| **Maintenance** | High (manual sync) | Low (automatic) |
| **Testing** | Mock enums | Mock API calls |
| **Performance** | Instant | Cached (near-instant) |
| **Type Safety** | ✅ Yes | ✅ Yes |
| **Scalability** | ❌ Limited | ✅ Unlimited |

---

**Conclusion**: The target architecture provides a robust, scalable, database-driven approach while maintaining type safety and excellent performance through multi-layer caching.
