# Reference Data Implementation Guide

**Phase**: 6A.47 Enhancement - Replace Hardcoded Enums
**Architecture**: See `REFERENCE_DATA_ARCHITECTURE.md`
**Diagrams**: See `REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md`

---

## Quick Start: Copy-Paste Code Examples

This guide provides **exact code** you can copy and paste for each implementation step.

---

## Step 1: Update Hook File

**File**: `web/src/infrastructure/api/hooks/useReferenceData.ts`

Replace the entire file with:

```typescript
/**
 * React Query hooks for Reference Data
 * Phase 6A.47: Database-driven reference data with React Query caching
 */

import { useQuery, UseQueryResult } from '@tanstack/react-query';
import { getReferenceDataByTypes } from '../services/referenceData.service';
import type { ReferenceValue } from '../services/referenceData.service';

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

## Step 2: Update Utility File

**File**: `web/src/lib/enum-utils.ts`

Replace the entire file with:

```typescript
/**
 * Utility functions for working with TypeScript enums
 * Phase 6A.47: Database-driven enum utilities
 */

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

// ============================================================================
// DEPRECATED FUNCTIONS (backward compatibility - remove in Phase 6A.48)
// ============================================================================

/**
 * Get event category label (backward compatibility)
 * @deprecated Use getLabelFromIntValue with useEventCategories instead
 */
export function getEventCategoryLabel(category: EventCategory): string {
  console.warn('getEventCategoryLabel is deprecated. Use getLabelFromIntValue with useEventCategories hook.');
  return EventCategory[category] || 'Unknown';
}

/**
 * Get event category options (backward compatibility)
 * @deprecated Use useEventCategories hook instead
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
```

---

## Step 3: Component Migration Examples

### Example 1: CategoryFilter.tsx (Dropdown)

**File**: `web/src/components/events/filters/CategoryFilter.tsx`

**BEFORE**:
```typescript
const categories = getEventCategoryOptions();
```

**AFTER**:
```typescript
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';

export function CategoryFilter({ value, onChange, className = '' }: CategoryFilterProps) {
  const { data: categories, isLoading } = useEventCategories();

  return (
    <div className={`relative ${className}`}>
      <select
        id="category-filter"
        value={value !== null ? value : ''}
        onChange={(e) =>
          onChange(e.target.value !== '' ? parseInt(e.target.value, 10) as EventCategory : null)
        }
        disabled={isLoading}
        className="block w-full pl-10 pr-10 py-2.5 border border-gray-300 rounded-lg"
      >
        <option value="">All Categories</option>
        {categories?.map((category) => (
          <option key={category.value} value={category.value}>
            {category.label}
          </option>
        ))}
      </select>
    </div>
  );
}
```

### Example 2: EventDetailsTab.tsx (Label Mapping)

**File**: `web/src/presentation/components/features/events/EventDetailsTab.tsx`

**BEFORE**:
```typescript
const categoryLabels: Record<EventCategory, string> = {
  [EventCategory.Religious]: 'Religious',
  [EventCategory.Cultural]: 'Cultural',
  [EventCategory.Community]: 'Community',
  [EventCategory.Educational]: 'Educational',
  [EventCategory.Social]: 'Social',
  [EventCategory.Business]: 'Business',
  [EventCategory.Charity]: 'Charity',
  [EventCategory.Entertainment]: 'Entertainment',
};

const categoryLabel = categoryLabels[event.category];
```

**AFTER**:
```typescript
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';
import { getLabelFromIntValue } from '@/lib/enum-utils';

export function EventDetailsTab({ event, ... }: EventDetailsTabProps) {
  const { data: categories } = useEventCategories();

  const categoryLabel = getLabelFromIntValue(categories, event.category) ?? 'Unknown';

  return (
    // ... rest of component
    <span>{categoryLabel}</span>
  );
}
```

### Example 3: EventsList.tsx (Switch Statement Replacement)

**File**: `web/src/presentation/components/features/dashboard/EventsList.tsx`

**BEFORE**:
```typescript
function getCategoryColor(category: EventCategory): string {
  switch (category) {
    case EventCategory.Religious: return 'purple';
    case EventCategory.Cultural: return 'blue';
    case EventCategory.Community: return 'green';
    case EventCategory.Educational: return 'yellow';
    case EventCategory.Social: return 'pink';
    case EventCategory.Business: return 'indigo';
    case EventCategory.Charity: return 'red';
    case EventCategory.Entertainment: return 'orange';
    default: return 'gray';
  }
}

function getCategoryLabel(category: EventCategory): string {
  switch (category) {
    case EventCategory.Religious: return 'Religious';
    case EventCategory.Cultural: return 'Cultural';
    case EventCategory.Community: return 'Community';
    case EventCategory.Educational: return 'Educational';
    case EventCategory.Social: return 'Social';
    case EventCategory.Business: return 'Business';
    case EventCategory.Charity: return 'Charity';
    case EventCategory.Entertainment: return 'Entertainment';
    default: return 'Other';
  }
}
```

**AFTER**:
```typescript
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';
import { buildIntToLabelMap, getLabelFromIntValue } from '@/lib/enum-utils';
import { useMemo } from 'react';

export function EventsList({ events }: EventsListProps) {
  const { data: categories } = useEventCategories();

  // Build Map once for O(1) lookups (performance optimization)
  const categoryLabelMap = useMemo(
    () => buildIntToLabelMap(categories),
    [categories]
  );

  // Category color mapping (keep hardcoded for now - can move to metadata later)
  const getCategoryColor = (category: EventCategory): string => {
    const colorMap: Record<number, string> = {
      [EventCategory.Religious]: 'purple',
      [EventCategory.Cultural]: 'blue',
      [EventCategory.Community]: 'green',
      [EventCategory.Educational]: 'yellow',
      [EventCategory.Social]: 'pink',
      [EventCategory.Business]: 'indigo',
      [EventCategory.Charity]: 'red',
      [EventCategory.Entertainment]: 'orange',
    };
    return colorMap[category] ?? 'gray';
  };

  return (
    <div>
      {events.map(event => {
        const categoryLabel = categoryLabelMap.get(event.category) ?? 'Other';
        const categoryColor = getCategoryColor(event.category);

        return (
          <div key={event.id}>
            <Badge color={categoryColor}>{categoryLabel}</Badge>
          </div>
        );
      })}
    </div>
  );
}
```

### Example 4: EventCreationForm.tsx (Dropdown in Form)

**File**: `web/src/presentation/components/features/events/EventCreationForm.tsx`

**BEFORE**:
```typescript
const { data: eventInterests, isLoading: isLoadingEventInterests } = useEventInterests();
```

**AFTER**:
```typescript
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';

export function EventCreationForm() {
  const { data: categories, isLoading: isLoadingCategories } = useEventCategories();

  return (
    <form>
      <select
        {...register('category')}
        disabled={isLoadingCategories}
        className="block w-full rounded-lg border-gray-300"
      >
        <option value="">Select Category</option>
        {categories?.map((category) => (
          <option key={category.value} value={category.value}>
            {category.label}
          </option>
        ))}
      </select>
    </form>
  );
}
```

### Example 5: Error Handling Component

**Pattern for all components**:

```typescript
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';

export function SomeComponent() {
  const { data: categories, isLoading, isError, refetch } = useEventCategories();

  // Error state
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

  // Loading state (optional - prefetch makes this rare)
  if (isLoading) {
    return <div>Loading categories...</div>;
  }

  // Normal rendering
  return (
    <select>
      {categories?.map(cat => (
        <option key={cat.value} value={cat.value}>{cat.label}</option>
      ))}
    </select>
  );
}
```

---

## Step 4: Prefetcher Component (Optional but Recommended)

**File**: `web/src/components/ReferenceDataPrefetcher.tsx` (NEW)

```typescript
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
```

**File**: `web/src/app/layout.tsx` (UPDATE)

Add to your root layout:

```typescript
import { ReferenceDataPrefetcher } from '@/components/ReferenceDataPrefetcher';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <QueryClientProvider client={queryClient}>
          <ReferenceDataPrefetcher />
          {children}
        </QueryClientProvider>
      </body>
    </html>
  );
}
```

---

## Step 5: Verification Commands

Run these after each component migration:

```bash
# Check for hardcoded category arrays
grep -r "Religious.*Cultural.*Community" web/src --include="*.tsx" --include="*.ts"

# Check for hardcoded category enum usage
grep -r "EventCategory\.(Religious|Cultural|Community)" web/src --include="*.tsx" --include="*.ts"

# Check for switch statements
grep -r "switch.*EventCategory" web/src --include="*.tsx" --include="*.ts"

# Check TypeScript compilation
npm run typecheck

# Build project
npm run build
```

**Expected Output**: No matches for grep, 0 errors for typecheck/build

---

## Step 6: Testing Checklist

After each component:

- [ ] Component renders without errors
- [ ] Dropdown loads instantly (if prefetch enabled)
- [ ] Correct labels display
- [ ] Form submission works (correct integer values)
- [ ] Error state shows retry button
- [ ] Network tab shows cached API calls
- [ ] No console warnings

---

## Common Patterns Cheat Sheet

### Pattern 1: Simple Dropdown
```typescript
const { data: categories } = useEventCategories();

<select>
  {categories?.map(cat => (
    <option key={cat.value} value={cat.value}>{cat.label}</option>
  ))}
</select>
```

### Pattern 2: Single Label Lookup
```typescript
const { data: categories } = useEventCategories();
const label = getLabelFromIntValue(categories, event.category);
```

### Pattern 3: Multiple Lookups (Optimized)
```typescript
const { data: categories } = useEventCategories();
const labelMap = useMemo(() => buildIntToLabelMap(categories), [categories]);

events.map(e => labelMap.get(e.category))
```

### Pattern 4: Type-Safe Wrapper
```typescript
const { data: categories } = useEventCategories();
const label = getEventCategoryLabelFromData(categories, event.category);
```

### Pattern 5: Error Handling
```typescript
const { data, isError, refetch } = useEventCategories();

if (isError) {
  return <ErrorState onRetry={refetch} />;
}
```

---

## Troubleshooting

### Issue: "categories is undefined"
**Solution**: Add optional chaining: `categories?.map()`

### Issue: "Dropdown shows 'Loading...' forever"
**Solution**: Check Network tab for API errors, verify backend is running

### Issue: "Labels show 'Unknown'"
**Solution**: Check `intValue` in database matches enum integer values

### Issue: "Build fails with type errors"
**Solution**: Ensure `EnumOption` interface is imported correctly

### Issue: "API returns empty array"
**Solution**: Verify database is seeded with reference data

---

## Next Steps

1. Copy `useReferenceData.ts` code (Step 1)
2. Copy `enum-utils.ts` code (Step 2)
3. Migrate CategoryFilter.tsx (Example 1)
4. Test thoroughly
5. Continue with remaining 10 components

**Estimated Time**: 2 days for all 11 components

---

## Document Metadata

- **Created**: 2025-12-28
- **Version**: 1.0
- **Author**: System Architecture Designer
- **Related**: `REFERENCE_DATA_ARCHITECTURE.md`, `REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md`
