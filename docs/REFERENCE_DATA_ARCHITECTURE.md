# Reference Data Architecture: Database-Driven Enum Replacement

**Author**: System Architecture Designer
**Date**: 2025-12-28
**Phase**: 6A.47 Enhancement
**Status**: Architecture Design

---

## Executive Summary

This document defines the architecture for replacing all 11 hardcoded enum locations with database-driven reference data API calls. The design prioritizes reusability, type safety, performance, and maintainability while ensuring a seamless user experience.

**Key Decisions**:
- **Hook Architecture**: Specialized hooks per enum type (Option A)
- **Mapping Utilities**: Shared utility module with typed helpers
- **Loading Strategy**: Prefetch on app initialization (Option D)
- **Error Handling**: Retry with toast notifications (Option B + D)
- **Implementation Order**: Bottom-up (utilities → hooks → components)

---

## 1. Hook Architecture Design

### Recommendation: Option A - Specialized Hooks Per Enum Type

**Rationale**:
- **Type Safety**: Each hook returns strongly-typed data specific to its enum
- **Developer Experience**: Clear, discoverable API (autocomplete-friendly)
- **Performance**: Independent caching per enum type
- **Extensibility**: Easy to add enum-specific logic (formatting, validation)

### Implementation

```typescript
// web/src/infrastructure/api/hooks/useReferenceData.ts

import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { getReferenceDataByTypes } from '../services/referenceData.service';
import type { ReferenceValue } from '../services/referenceData.service';
import { EventCategory, EventStatus, Currency } from '../types/events.types';

/**
 * Query key factory for reference data
 */
export const referenceDataKeys = {
  all: ['referenceData'] as const,
  byTypes: (types: string[], activeOnly?: boolean) =>
    [...referenceDataKeys.all, 'byTypes', types, activeOnly] as const,
  eventCategories: () => [...referenceDataKeys.all, 'eventCategories'] as const,
  eventStatuses: () => [...referenceDataKeys.all, 'eventStatuses'] as const,
  currencies: () => [...referenceDataKeys.all, 'currencies'] as const,
};

/**
 * Common query configuration for reference data
 */
const REFERENCE_DATA_QUERY_CONFIG = {
  staleTime: 1000 * 60 * 60, // 1 hour
  gcTime: 1000 * 60 * 60 * 24, // 24 hours
  retry: 3,
  retryDelay: (attemptIndex: number) => Math.min(1000 * 2 ** attemptIndex, 30000),
} as const;

/**
 * Option type for dropdown selections
 */
export interface EnumOption {
  /** Enum integer value (e.g., EventCategory.Religious = 0) */
  value: number;
  /** Database code (e.g., "Religious") */
  code: string;
  /** Display label (e.g., "Religious Events") */
  label: string;
  /** Optional description */
  description?: string;
  /** Display order for sorting */
  displayOrder: number;
}

/**
 * Hook to fetch Event Categories
 *
 * @example
 * ```tsx
 * const { data: categories, isLoading } = useEventCategories();
 *
 * <select>
 *   {categories?.map(cat => (
 *     <option key={cat.value} value={cat.value}>{cat.label}</option>
 *   ))}
 * </select>
 * ```
 */
export function useEventCategories(): UseQueryResult<EnumOption[], Error> {
  return useQuery<EnumOption[], Error>({
    queryKey: referenceDataKeys.eventCategories(),
    queryFn: async () => {
      const data = await getReferenceDataByTypes(['EventCategory']);
      return data
        .map(item => ({
          value: item.intValue ?? 0,
          code: item.code,
          label: item.name,
          description: item.description ?? undefined,
          displayOrder: item.displayOrder,
        }))
        .sort((a, b) => a.displayOrder - b.displayOrder);
    },
    ...REFERENCE_DATA_QUERY_CONFIG,
  });
}

/**
 * Hook to fetch Event Statuses
 *
 * @example
 * ```tsx
 * const { data: statuses } = useEventStatuses();
 * const statusLabel = statuses?.find(s => s.value === event.status)?.label;
 * ```
 */
export function useEventStatuses(): UseQueryResult<EnumOption[], Error> {
  return useQuery<EnumOption[], Error>({
    queryKey: referenceDataKeys.eventStatuses(),
    queryFn: async () => {
      const data = await getReferenceDataByTypes(['EventStatus']);
      return data
        .map(item => ({
          value: item.intValue ?? 0,
          code: item.code,
          label: item.name,
          description: item.description ?? undefined,
          displayOrder: item.displayOrder,
        }))
        .sort((a, b) => a.displayOrder - b.displayOrder);
    },
    ...REFERENCE_DATA_QUERY_CONFIG,
  });
}

/**
 * Hook to fetch Currencies
 *
 * @example
 * ```tsx
 * const { data: currencies } = useCurrencies();
 * <select>
 *   {currencies?.map(curr => (
 *     <option key={curr.value} value={curr.value}>{curr.label}</option>
 *   ))}
 * </select>
 * ```
 */
export function useCurrencies(): UseQueryResult<EnumOption[], Error> {
  return useQuery<EnumOption[], Error>({
    queryKey: referenceDataKeys.currencies(),
    queryFn: async () => {
      const data = await getReferenceDataByTypes(['Currency']);
      return data
        .map(item => ({
          value: item.intValue ?? 1,
          code: item.code,
          label: item.name,
          description: item.description ?? undefined,
          displayOrder: item.displayOrder,
        }))
        .sort((a, b) => a.displayOrder - b.displayOrder);
    },
    ...REFERENCE_DATA_QUERY_CONFIG,
  });
}

/**
 * Generic hook to fetch multiple enum types at once
 * Use when you need multiple enums in one component
 *
 * @example
 * ```tsx
 * const { data: refData } = useReferenceData(['EventCategory', 'EventStatus']);
 * const categories = refData?.filter(r => r.enumType === 'EventCategory');
 * ```
 */
export function useReferenceData(
  types: string[],
  activeOnly: boolean = true
): UseQueryResult<ReferenceValue[], Error> {
  return useQuery<ReferenceValue[], Error>({
    queryKey: referenceDataKeys.byTypes(types, activeOnly),
    queryFn: () => getReferenceDataByTypes(types, activeOnly),
    ...REFERENCE_DATA_QUERY_CONFIG,
  });
}

/**
 * Keep existing useEventInterests for backward compatibility
 * Maps EventCategory to profile interests format
 */
export interface EventInterestOption {
  code: string;
  name: string;
  description?: string;
}

export function useEventInterests(): UseQueryResult<EventInterestOption[], Error> {
  return useQuery<EventInterestOption[], Error>({
    queryKey: referenceDataKeys.eventCategories(),
    queryFn: async () => {
      const data = await getReferenceDataByTypes(['EventCategory']);
      return data
        .map(item => ({
          code: item.code,
          name: item.name,
          description: item.description ?? undefined,
        }))
        .sort((a, b) => a.name.localeCompare(b.name));
    },
    ...REFERENCE_DATA_QUERY_CONFIG,
  });
}
```

---

## 2. Mapping Utilities Design

### Purpose
Provide reusable functions for converting between:
- Database codes → Enum int values
- Enum int values → Display labels
- Building lookup maps for performance

### Implementation

```typescript
// web/src/lib/enum-utils.ts

import type { EnumOption } from '@/infrastructure/api/hooks/useReferenceData';
import { EventCategory, EventStatus, Currency } from '@/infrastructure/api/types/events.types';

/**
 * Get enum integer value from database code
 *
 * @example
 * getIntValueFromCode(categories, 'Religious') // Returns 0
 */
export function getIntValueFromCode(
  options: EnumOption[] | undefined,
  code: string
): number | undefined {
  return options?.find(opt => opt.code === code)?.value;
}

/**
 * Get display label from enum integer value
 *
 * @example
 * getLabelFromIntValue(categories, EventCategory.Religious) // Returns "Religious Events"
 */
export function getLabelFromIntValue(
  options: EnumOption[] | undefined,
  value: number
): string | undefined {
  return options?.find(opt => opt.value === value)?.label;
}

/**
 * Get database code from enum integer value
 *
 * @example
 * getCodeFromIntValue(categories, EventCategory.Religious) // Returns "Religious"
 */
export function getCodeFromIntValue(
  options: EnumOption[] | undefined,
  value: number
): string | undefined {
  return options?.find(opt => opt.value === value)?.code;
}

/**
 * Build a Map for O(1) lookups from code to int value
 * Use when you need frequent lookups in a loop
 *
 * @example
 * const codeMap = buildCodeToIntMap(categories);
 * const value = codeMap.get('Religious'); // Fast O(1) lookup
 */
export function buildCodeToIntMap(
  options: EnumOption[] | undefined
): Map<string, number> {
  const map = new Map<string, number>();
  options?.forEach(opt => map.set(opt.code, opt.value));
  return map;
}

/**
 * Build a Map for O(1) lookups from int value to label
 * Use when you need frequent label lookups in a loop
 *
 * @example
 * const labelMap = buildIntToLabelMap(categories);
 * const label = labelMap.get(EventCategory.Religious); // Fast O(1) lookup
 */
export function buildIntToLabelMap(
  options: EnumOption[] | undefined
): Map<number, string> {
  const map = new Map<number, string>();
  options?.forEach(opt => map.set(opt.value, opt.label));
  return map;
}

/**
 * Get event category label (backward compatibility)
 * DEPRECATED: Use getLabelFromIntValue with useEventCategories instead
 */
export function getEventCategoryLabel(category: EventCategory): string {
  console.warn('getEventCategoryLabel is deprecated. Use getLabelFromIntValue with useEventCategories hook.');
  return EventCategory[category] || 'Unknown';
}

/**
 * Get event category options (backward compatibility)
 * DEPRECATED: Use useEventCategories hook instead
 */
export function getEventCategoryOptions(): Array<{ value: EventCategory; label: string }> {
  console.warn('getEventCategoryOptions is deprecated. Use useEventCategories hook instead.');
  return Object.entries(EventCategory)
    .filter(([key, value]) => typeof value === 'number')
    .map(([key, value]) => ({
      value: value as EventCategory,
      label: key,
    }));
}

/**
 * Type-safe wrapper for EventCategory lookups
 */
export function getEventCategoryLabelFromData(
  categories: EnumOption[] | undefined,
  category: EventCategory
): string {
  return getLabelFromIntValue(categories, category) ?? 'Unknown Category';
}

/**
 * Type-safe wrapper for EventStatus lookups
 */
export function getEventStatusLabelFromData(
  statuses: EnumOption[] | undefined,
  status: EventStatus
): string {
  return getLabelFromIntValue(statuses, status) ?? 'Unknown Status';
}

/**
 * Type-safe wrapper for Currency lookups
 */
export function getCurrencyLabelFromData(
  currencies: EnumOption[] | undefined,
  currency: Currency
): string {
  return getLabelFromIntValue(currencies, currency) ?? 'Unknown Currency';
}
```

---

## 3. Loading State Strategy

### Recommendation: Option D - Prefetch on App Load

**Rationale**:
- **Instant UX**: Reference data available immediately when needed
- **Reduced Complexity**: No loading spinners in every dropdown
- **Server Cache**: Backend caches for 1 hour, perfect for prefetch
- **Client Cache**: React Query keeps data in memory

### Implementation

```typescript
// web/src/app/layout.tsx (or app providers)

'use client';

import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { getReferenceDataByTypes } from '@/infrastructure/api/services/referenceData.service';
import { referenceDataKeys } from '@/infrastructure/api/hooks/useReferenceData';

/**
 * Prefetch reference data on app initialization
 * This ensures dropdowns load instantly without spinners
 */
export function ReferenceDataPrefetcher() {
  const queryClient = useQueryClient();

  useEffect(() => {
    // Prefetch all common enum types
    const enumTypes = ['EventCategory', 'EventStatus', 'Currency'];

    queryClient.prefetchQuery({
      queryKey: referenceDataKeys.byTypes(enumTypes, true),
      queryFn: () => getReferenceDataByTypes(enumTypes, true),
      staleTime: 1000 * 60 * 60, // 1 hour
    });
  }, [queryClient]);

  return null; // No UI
}

// Add to root layout:
// <ReferenceDataPrefetcher />
```

### Fallback for Loading States

Even with prefetch, gracefully handle loading for edge cases:

```tsx
// Pattern for dropdowns
const { data: categories, isLoading } = useEventCategories();

return (
  <select disabled={isLoading}>
    {isLoading ? (
      <option>Loading categories...</option>
    ) : (
      <>
        <option value="">Select Category</option>
        {categories?.map(cat => (
          <option key={cat.value} value={cat.value}>{cat.label}</option>
        ))}
      </>
    )}
  </select>
);
```

---

## 4. Error Handling Strategy

### Recommendation: Option B + D - Retry with Toast + Empty State

**Rationale**:
- **Resilience**: Auto-retry handles transient network errors
- **UX**: Toast notification keeps user informed without blocking
- **Graceful Degradation**: Empty dropdown with retry button if all retries fail

### Implementation

```typescript
// web/src/infrastructure/api/hooks/useReferenceData.ts

import { toast } from 'sonner'; // or your toast library

export function useEventCategories(): UseQueryResult<EnumOption[], Error> {
  return useQuery<EnumOption[], Error>({
    queryKey: referenceDataKeys.eventCategories(),
    queryFn: async () => {
      const data = await getReferenceDataByTypes(['EventCategory']);
      return data
        .map(item => ({
          value: item.intValue ?? 0,
          code: item.code,
          label: item.name,
          description: item.description ?? undefined,
          displayOrder: item.displayOrder,
        }))
        .sort((a, b) => a.displayOrder - b.displayOrder);
    },
    ...REFERENCE_DATA_QUERY_CONFIG,
    onError: (error) => {
      console.error('Failed to load event categories:', error);
      toast.error('Failed to load categories. Retrying...', {
        duration: 3000,
      });
    },
  });
}
```

### Component Error Handling

```tsx
// Pattern for components
const { data: categories, isLoading, isError, refetch } = useEventCategories();

if (isError) {
  return (
    <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
      <p className="text-sm text-red-700">Failed to load categories.</p>
      <button
        onClick={() => refetch()}
        className="mt-2 text-sm text-red-600 underline"
      >
        Retry
      </button>
    </div>
  );
}
```

---

## 5. Implementation Order

### Recommendation: Bottom-Up Dependency Order

**Phase 1: Foundation** (Low Risk)
1. `enum-utils.ts` - Add new utility functions (keep old ones for compatibility)
2. `useReferenceData.ts` - Add specialized hooks (useEventCategories, useEventStatuses, useCurrencies)
3. `layout.tsx` - Add ReferenceDataPrefetcher component

**Phase 2: Core Components** (Medium Risk - Most Used)
4. `CategoryFilter.tsx` - Replace getEventCategoryOptions() with useEventCategories()
5. `EventCreationForm.tsx` - Replace useEventInterests with useEventCategories (dropdown)
6. `EventEditForm.tsx` - Add category/status label lookups (2 mappings)

**Phase 3: Display Components** (Low Risk - Read-Only)
7. `EventDetailsTab.tsx` - Replace hardcoded categoryLabels with hook + utility
8. `EventsList.tsx` - Replace 2 switch statements with utility function calls
9. `events/[id]/page.tsx` - Replace category label lookup

**Phase 4: Page Components** (High Risk - Entry Points)
10. `events/page.tsx` - Replace filter mapping (3 usages)
11. `templates/page.tsx` - Replace category label lookup
12. `events/[id]/manage/page.tsx` - Add status/category label lookups

**Phase 5: Cleanup** (Final Step)
13. Remove deprecated functions from enum-utils.ts (add console.warn first)
14. Run grep to verify no hardcoded arrays remain
15. Update tests

### Rationale for Order:
- **Utilities first**: Foundation must be stable before components use it
- **Least used first**: CategoryFilter is isolated, safe to start with
- **Forms second**: Critical path but well-tested
- **Display components**: Low risk, just label lookups
- **Pages last**: Entry points, test thoroughly

---

## 6. Code Reuse Pattern

### Recommendation: Option B + C - Hook + Helper Function

**Rationale**:
- **Hook for data fetching**: `useEventCategories()` handles caching, loading, errors
- **Helper for lookups**: `getLabelFromIntValue()` for one-off conversions
- **No inline logic**: Keeps components clean and testable

### Pattern Examples

#### Pattern 1: Dropdown with Options
```tsx
// Use hook directly for dropdowns
function CategoryDropdown() {
  const { data: categories, isLoading } = useEventCategories();

  return (
    <select disabled={isLoading}>
      {categories?.map(cat => (
        <option key={cat.value} value={cat.value}>{cat.label}</option>
      ))}
    </select>
  );
}
```

#### Pattern 2: Label Lookup (Single Value)
```tsx
// Use hook + helper for label display
function EventCard({ event }: { event: EventDto }) {
  const { data: categories } = useEventCategories();

  const categoryLabel = getLabelFromIntValue(categories, event.category);

  return <div>{categoryLabel}</div>;
}
```

#### Pattern 3: Multiple Lookups in Loop
```tsx
// Use hook + Map for performance in loops
function EventList({ events }: { events: EventDto[] }) {
  const { data: categories } = useEventCategories();

  // Build Map once for O(1) lookups
  const labelMap = useMemo(
    () => buildIntToLabelMap(categories),
    [categories]
  );

  return (
    <ul>
      {events.map(event => (
        <li key={event.id}>
          {event.title} - {labelMap.get(event.category)}
        </li>
      ))}
    </ul>
  );
}
```

#### Pattern 4: Switch Statement Replacement
```tsx
// BEFORE (hardcoded):
function getBadgeColor(category: EventCategory): string {
  switch (category) {
    case EventCategory.Religious: return 'purple';
    case EventCategory.Cultural: return 'blue';
    case EventCategory.Community: return 'green';
    default: return 'gray';
  }
}

// AFTER (data-driven):
function CategoryBadge({ category }: { category: EventCategory }) {
  const { data: categories } = useEventCategories();

  const categoryData = categories?.find(c => c.value === category);
  const badgeColor = categoryData?.metadata?.badgeColor ?? 'gray';

  return <Badge color={badgeColor}>{categoryData?.label}</Badge>;
}
```

---

## 7. Validation Strategy

### Three-Layer Verification

#### Layer 1: Automated Grep Tests
```bash
# Run these commands to find violations

# Find hardcoded category arrays
grep -r "Religious.*Cultural.*Community" web/src --include="*.tsx" --include="*.ts"

# Find hardcoded category labels
grep -r "EventCategory\.(Religious|Cultural|Community)" web/src --include="*.tsx" --include="*.ts"

# Find switch statements on EventCategory
grep -r "switch.*EventCategory" web/src --include="*.tsx" --include="*.ts"

# Find hardcoded status labels
grep -r "EventStatus\.(Draft|Published|Active)" web/src --include="*.tsx" --include="*.ts"

# Find hardcoded currency arrays
grep -r "Currency\.(USD|LKR|GBP)" web/src --include="*.tsx" --include="*.ts"
```

#### Layer 2: TypeScript Compilation
```bash
# Ensure no type errors
npm run typecheck

# Expected output: 0 errors
```

#### Layer 3: Manual Testing Checklist

**Test Case 1: Category Dropdown (EventCreationForm)**
- [ ] Dropdown loads instantly (no spinner)
- [ ] All categories display correct labels
- [ ] Selection saves correct integer value
- [ ] Form validation works

**Test Case 2: Category Filter (events/page.tsx)**
- [ ] Filter dropdown loads instantly
- [ ] "All Categories" shows all events
- [ ] Each category filters correctly
- [ ] URL params update on selection

**Test Case 3: Event Display (EventDetailsTab)**
- [ ] Category badge shows correct label
- [ ] Status badge shows correct label
- [ ] Currency displays correctly

**Test Case 4: Event List (EventsList.tsx)**
- [ ] Category badges show correct colors
- [ ] Status badges show correct colors
- [ ] No "Unknown" labels appear

**Test Case 5: Error Handling**
- [ ] Disconnect network
- [ ] Dropdowns show "Loading..." briefly
- [ ] Toast notification appears
- [ ] Auto-retry works
- [ ] Retry button appears after 3 failures

**Test Case 6: Performance**
- [ ] Initial page load < 2 seconds
- [ ] Reference data cached (check Network tab)
- [ ] No duplicate API calls
- [ ] Dropdowns respond instantly

---

## 8. Migration Checklist

### Pre-Migration
- [x] Backend API endpoint ready (`/api/reference-data`)
- [x] Database seeded with EventCategory, EventStatus, Currency
- [ ] Create `useEventCategories()` hook
- [ ] Create `useEventStatuses()` hook
- [ ] Create `useCurrencies()` hook
- [ ] Add utility functions to `enum-utils.ts`
- [ ] Add `ReferenceDataPrefetcher` to app layout

### Component Migration (11 Total)
- [ ] 1. `lib/enum-utils.ts` - Add new utilities (keep old for compatibility)
- [ ] 2. `CategoryFilter.tsx` - Use `useEventCategories()`
- [ ] 3. `events/page.tsx` - Replace 3 category usages
- [ ] 4. `templates/page.tsx` - Replace category lookup
- [ ] 5. `events/[id]/page.tsx` - Replace category lookup
- [ ] 6. `EventDetailsTab.tsx` - Replace hardcoded categoryLabels
- [ ] 7. `EventsList.tsx` - Replace 2 switch statements
- [ ] 8. `EventCreationForm.tsx` - Replace useEventInterests (remove fallback)
- [ ] 9. `EventEditForm.tsx` - Add 2 label mappings
- [ ] 10. `events/[id]/manage/page.tsx` - Add label lookups

### Post-Migration
- [ ] Run all grep commands (verify no violations)
- [ ] Run `npm run typecheck` (0 errors)
- [ ] Run `npm run build` (0 errors)
- [ ] Complete manual testing checklist
- [ ] Remove deprecated functions (add warnings first)
- [ ] Update documentation
- [ ] Create Phase summary document

---

## 9. Performance Considerations

### Caching Strategy
```
┌─────────────┐
│   Browser   │
│   Memory    │ ← React Query Cache (1 hour)
└──────┬──────┘
       │ Cache MISS
       ↓
┌─────────────┐
│   Backend   │
│   Memory    │ ← ASP.NET Memory Cache (1 hour)
└──────┬──────┘
       │ Cache MISS
       ↓
┌─────────────┐
│  Database   │ ← PostgreSQL (source of truth)
└─────────────┘
```

### Expected Performance
- **First Load**: ~200ms (backend cache miss)
- **Subsequent Loads**: ~10ms (React Query memory)
- **After 1 Hour**: ~50ms (backend cache hit)
- **Cache Size**: ~5KB total (all enums)

### Optimization Tips
1. **Prefetch**: Load on app init (done in layout.tsx)
2. **Batch**: Use `useReferenceData(['EventCategory', 'EventStatus'])` for multiple enums
3. **Memoize**: Use `useMemo` for Map builders in loops
4. **Lazy Load**: Only load enums when needed (if prefetch disabled)

---

## 10. Testing Strategy

### Unit Tests
```typescript
// web/src/lib/__tests__/enum-utils.test.ts

import { getLabelFromIntValue, buildIntToLabelMap } from '../enum-utils';
import { EventCategory } from '@/infrastructure/api/types/events.types';

describe('enum-utils', () => {
  const mockCategories = [
    { value: 0, code: 'Religious', label: 'Religious Events', displayOrder: 1 },
    { value: 1, code: 'Cultural', label: 'Cultural Events', displayOrder: 2 },
  ];

  test('getLabelFromIntValue returns correct label', () => {
    expect(getLabelFromIntValue(mockCategories, 0)).toBe('Religious Events');
    expect(getLabelFromIntValue(mockCategories, 1)).toBe('Cultural Events');
  });

  test('getLabelFromIntValue returns undefined for unknown value', () => {
    expect(getLabelFromIntValue(mockCategories, 99)).toBeUndefined();
  });

  test('buildIntToLabelMap creates correct Map', () => {
    const map = buildIntToLabelMap(mockCategories);
    expect(map.get(0)).toBe('Religious Events');
    expect(map.get(1)).toBe('Cultural Events');
  });
});
```

### Integration Tests
```typescript
// web/src/infrastructure/api/hooks/__tests__/useReferenceData.test.ts

import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEventCategories } from '../useReferenceData';

describe('useEventCategories', () => {
  test('returns event categories from API', async () => {
    const queryClient = new QueryClient();
    const wrapper = ({ children }) => (
      <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const { result } = renderHook(() => useEventCategories(), { wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data).toHaveLength(8); // 8 categories
    expect(result.current.data?.[0]).toMatchObject({
      value: expect.any(Number),
      code: expect.any(String),
      label: expect.any(String),
      displayOrder: expect.any(Number),
    });
  });
});
```

---

## 11. Rollback Plan

If critical issues arise:

### Quick Rollback (5 minutes)
```bash
# Revert to previous commit
git revert HEAD
git push

# Or feature flag (if implemented)
# Set ENABLE_REFERENCE_DATA_API=false in .env
```

### Partial Rollback (Component-Level)
```tsx
// Add feature flag to each component
const USE_REFERENCE_DATA = process.env.NEXT_PUBLIC_USE_REFERENCE_DATA === 'true';

export function CategoryFilter() {
  if (!USE_REFERENCE_DATA) {
    // Fall back to old hardcoded version
    return <CategoryFilterOld />;
  }

  // New version with API
  const { data: categories } = useEventCategories();
  // ...
}
```

### Migration to Old Code
If API proves unreliable, keep old utility functions:
```typescript
// Keep both versions during migration
export function getEventCategoryOptionsLegacy() {
  return Object.entries(EventCategory)
    .filter(([key, value]) => typeof value === 'number')
    .map(([key, value]) => ({ value, label: key }));
}
```

---

## 12. Future Enhancements

### Phase 2: Additional Enums
After validating this architecture, expand to:
- `DiscountType`
- `LanguageId`
- `PaymentType`
- `PriceType`
- `SponsorshipTier`
- `TicketType`
- `VisibilityLevel`

### Phase 3: Metadata Support
Leverage `metadata` field for:
- Badge colors
- Icons
- Validation rules
- Feature flags

Example:
```json
{
  "code": "Religious",
  "name": "Religious Events",
  "metadata": {
    "badgeColor": "purple",
    "icon": "church",
    "requiresApproval": true
  }
}
```

### Phase 4: Localization
Use `metadata` for translations:
```json
{
  "code": "Religious",
  "name": "Religious Events",
  "metadata": {
    "translations": {
      "si": "ආගමික උත්සව",
      "ta": "சமய நிகழ்வுகள்"
    }
  }
}
```

---

## Appendix A: File Locations

### Files to Create
```
web/src/infrastructure/api/hooks/useReferenceData.ts (UPDATE)
web/src/lib/enum-utils.ts (UPDATE)
web/src/app/layout.tsx (UPDATE - add prefetcher)
web/src/components/ReferenceDataPrefetcher.tsx (NEW)
```

### Files to Update (11 Total)
```
1.  web/src/lib/enum-utils.ts
2.  web/src/components/events/filters/CategoryFilter.tsx
3.  web/src/app/events/page.tsx (3 usages)
4.  web/src/app/templates/page.tsx
5.  web/src/app/events/[id]/page.tsx
6.  web/src/presentation/components/features/events/EventDetailsTab.tsx
7.  web/src/presentation/components/features/dashboard/EventsList.tsx (2 switch)
8.  web/src/presentation/components/features/events/EventCreationForm.tsx
9.  web/src/presentation/components/features/events/EventEditForm.tsx (2 mappings)
10. web/src/app/events/[id]/manage/page.tsx
```

---

## Appendix B: API Contract

### Request
```
GET /api/reference-data?types=EventCategory,EventStatus&activeOnly=true
```

### Response
```json
[
  {
    "id": "uuid-1",
    "enumType": "EventCategory",
    "code": "Religious",
    "intValue": 0,
    "name": "Religious Events",
    "description": "Events related to religious ceremonies and celebrations",
    "displayOrder": 1,
    "isActive": true,
    "metadata": null
  },
  {
    "id": "uuid-2",
    "enumType": "EventStatus",
    "code": "Published",
    "intValue": 1,
    "name": "Published",
    "description": "Event is visible to the public",
    "displayOrder": 2,
    "isActive": true,
    "metadata": null
  }
]
```

---

## Appendix C: TypeScript Types

### Core Types
```typescript
// From referenceData.service.ts
export interface ReferenceValue {
  id: string;
  enumType: string;
  code: string;
  intValue: number | null;
  name: string;
  description: string | null;
  displayOrder: number;
  isActive: boolean;
  metadata: Record<string, unknown> | null;
}

// From useReferenceData.ts
export interface EnumOption {
  value: number;
  code: string;
  label: string;
  description?: string;
  displayOrder: number;
}

// Backward compatibility
export interface EventInterestOption {
  code: string;
  name: string;
  description?: string;
}
```

---

## Document Changelog

| Date       | Version | Changes                          | Author                   |
|------------|---------|----------------------------------|--------------------------|
| 2025-12-28 | 1.0     | Initial architecture design      | System Architect Designer|

---

**Next Steps**:
1. Review this architecture with team
2. Get approval for implementation order
3. Begin Phase 1 (Foundation)
4. Track progress in `PROGRESS_TRACKER.md`

**Questions for Review**:
- Should we enable feature flags for gradual rollout?
- Should we add Sentry error tracking for API failures?
- Should we prefetch on app load or lazy load on demand?
- Should we cache in localStorage for offline support?
