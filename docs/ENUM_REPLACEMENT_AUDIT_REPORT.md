# Enum Replacement Audit Report
**Date**: 2025-12-28
**Phase**: 6A.47 Follow-up
**Status**: CRITICAL ISSUES FOUND

## Executive Summary

A comprehensive audit of the frontend codebase has revealed **CRITICAL VIOLATIONS** where hardcoded enum values are still being used instead of the database-driven reference data API. This directly contradicts Phase 6A.47's objective to eliminate hardcoded enums.

## Critical Issues Found

### 1. **CategoryFilter.tsx** - MUST FIX ❌
**File**: `web/src/components/events/filters/CategoryFilter.tsx`
**Lines**: 30
**Issue**: Uses `getEventCategoryOptions()` helper function which reads directly from EventCategory enum

**Current Code**:
```typescript
const categories = getEventCategoryOptions(); // Line 30
```

**Problem**:
- NOT using reference data API
- Hardcoded enum iteration
- Violates Phase 6A.47 requirements

**Fix Required**:
- Replace with `useEventInterests()` hook
- Map API response to dropdown options
- Handle loading states

---

### 2. **enum-utils.ts** - MUST FIX ❌
**File**: `web/src/lib/enum-utils.ts`
**Lines**: 13-20
**Issue**: Helper function that iterates over EventCategory enum keys

**Current Code**:
```typescript
export function getEventCategoryOptions(): Array<{ value: EventCategory; label: string }> {
  return Object.entries(EventCategory)
    .filter(([key, value]) => typeof value === 'number')
    .map(([key, value]) => ({
      value: value as EventCategory,
      label: key,
    }));
}
```

**Problem**:
- Reads from hardcoded EventCategory enum
- NOT using database
- Used by CategoryFilter.tsx (cascading issue)

**Fix Required**:
- DEPRECATE this entire file
- Move functionality to specialized hooks
- Or refactor to accept API data as parameter

---

### 3. **EventEditForm.tsx** - MUST FIX ❌
**File**: `web/src/presentation/components/features/events/EventEditForm.tsx`
**Lines**: 55-64, 387-396
**Issue**: Hardcoded categoryMap and categoryOptions

**Current Code (Line 55-64)**:
```typescript
const categoryMap: Record<string, EventCategory> = {
  'Religious': EventCategory.Religious,
  'Cultural': EventCategory.Cultural,
  'Community': EventCategory.Community,
  'Educational': EventCategory.Educational,
  'Social': EventCategory.Social,
  'Business': EventCategory.Business,
  'Charity': EventCategory.Charity,
  'Entertainment': EventCategory.Entertainment,
};
```

**Current Code (Line 387-396)**:
```typescript
const categoryOptions = [
  { value: EventCategory.Religious, label: 'Religious' },
  { value: EventCategory.Cultural, label: 'Cultural' },
  { value: EventCategory.Community, label: 'Community' },
  { value: EventCategory.Educational, label: 'Educational' },
  { value: EventCategory.Social, label: 'Social' },
  { value: EventCategory.Business, label: 'Business' },
  { value: EventCategory.Charity, label: 'Charity' },
  { value: EventCategory.Entertainment, label: 'Entertainment' },
];
```

**Problem**:
- Manually hardcoded array
- NOT using reference data API
- Duplicate violation in same file

**Fix Required**:
- Use `useEventInterests()` hook
- Build categoryMap from API response
- Handle loading states in form

---

### 4. **EventCreationForm.tsx** - PARTIAL VIOLATION ⚠️
**File**: `web/src/presentation/components/features/events/EventCreationForm.tsx`
**Lines**: 68, 253-275
**Issue**: Uses `useEventInterests()` BUT has fallback hardcoded categoryOptions

**Current Code (Line 253-275)**:
```typescript
const categoryCodeToEnumValue: Record<string, EventCategory> = {
  'Religious': EventCategory.Religious,
  'Cultural': EventCategory.Cultural,
  'Community': EventCategory.Community,
  'Educational': EventCategory.Educational,
  'Social': EventCategory.Social,
  'Business': EventCategory.Business,
  'Charity': EventCategory.Charity,
  'Entertainment': EventCategory.Entertainment,
};

const categoryOptions = eventInterests?.map(interest => ({
  value: categoryCodeToEnumValue[interest.code] ?? EventCategory.Community,
  label: interest.name,
})) ?? [
  { value: EventCategory.Religious, label: 'Religious' },
  { value: EventCategory.Cultural, label: 'Cultural' },
  { value: EventCategory.Community, label: 'Community' },
  { value: EventCategory.Educational, label: 'Educational' },
  { value: EventCategory.Social, label: 'Social' },
  { value: EventCategory.Business, label: 'Business' },
  { value: EventCategory.Charity, label: 'Charity' },
  { value: EventCategory.Entertainment, label: 'Entertainment' },
];
```

**Problem**:
- Fallback array is hardcoded
- categoryCodeToEnumValue mapper is hardcoded
- Should show loading state instead of fallback

**Fix Required**:
- Remove hardcoded fallback array
- Show loading spinner while API loads
- Build mapping dynamically from API response

---

## Files Using Enums (Type Safety) - KEEP ✅

These files use enums for **type definitions and validators only**, NOT for UI dropdowns. These are acceptable:

1. **events.types.ts** - Enum type definitions (matches backend)
2. **auth.types.ts** - UserRole enum definition
3. **event.schemas.ts** - Zod validators using enum types
4. **role-helpers.ts** - Type guards and role validation
5. **eventMapper.ts** - DTO mapping with type safety

**Rationale**: These provide TypeScript type safety and compile-time validation. The enums here serve as contracts between frontend and backend, NOT as UI data sources.

---

## Files Already Fixed - NO ACTION ✅

1. **CulturalInterestsSection.tsx** - Uses `useEventInterests()` hook correctly
2. **Profile constants** - Reference data integration complete

---

## Architecture Analysis

### Current State
```
CategoryFilter.tsx
    ↓
getEventCategoryOptions() (enum-utils.ts)
    ↓
EventCategory enum (hardcoded)
```

### Target State
```
CategoryFilter.tsx
    ↓
useEventCategories() hook (NEW)
    ↓
Reference Data API
    ↓
Database
```

---

## Proposed Architecture

### Phase 1: Create Specialized Hooks

**File**: `web/src/infrastructure/api/hooks/useReferenceData.ts`

Add these new hooks:

```typescript
/**
 * Hook to fetch Event Categories for dropdowns
 * Returns: { value: number, label: string }[]
 */
export function useEventCategories() {
  const { data: eventInterests, isLoading, error } = useEventInterests();

  return {
    data: eventInterests?.map(interest => ({
      value: getIntValueFromCode(interest.code), // Map to enum int value
      label: interest.name,
    })),
    isLoading,
    error,
  };
}

/**
 * Hook to fetch Event Statuses for dropdowns
 */
export function useEventStatuses() {
  const { data, isLoading, error } = useReferenceData(['EventStatus']);

  return {
    data: data?.map(item => ({
      value: item.intValue,
      label: item.name,
    })),
    isLoading,
    error,
  };
}

/**
 * Hook to fetch User Roles for dropdowns
 */
export function useUserRoles() {
  const { data, isLoading, error } = useReferenceData(['UserRole']);

  return {
    data: data?.map(item => ({
      value: item.intValue,
      label: item.name,
    })),
    isLoading,
    error,
  };
}

/**
 * Hook to fetch Currency options for dropdowns
 */
export function useCurrencies() {
  const { data, isLoading, error } = useReferenceData(['Currency']);

  return {
    data: data?.map(item => ({
      value: item.intValue,
      label: item.name,
    })),
    isLoading,
    error,
  };
}
```

### Phase 2: Add Mapping Utilities

**File**: `web/src/infrastructure/api/utils/enum-mappers.ts` (NEW)

```typescript
/**
 * Utility to map reference data code to enum integer value
 * Uses the intValue from database reference data
 */
export function getIntValueFromCode(
  code: string,
  referenceData: ReferenceValue[]
): number | undefined {
  return referenceData.find(item => item.code === code)?.intValue ?? undefined;
}

/**
 * Utility to map enum integer value to display name
 */
export function getNameFromIntValue(
  intValue: number,
  referenceData: ReferenceValue[]
): string | undefined {
  return referenceData.find(item => item.intValue === intValue)?.name ?? undefined;
}

/**
 * Build code-to-intValue map from reference data
 * Used for form submissions
 */
export function buildCodeToIntMap(
  referenceData: ReferenceValue[]
): Record<string, number> {
  return referenceData.reduce((acc, item) => {
    if (item.intValue !== null) {
      acc[item.code] = item.intValue;
    }
    return acc;
  }, {} as Record<string, number>);
}
```

### Phase 3: Update Components

#### CategoryFilter.tsx
```typescript
'use client';

import { Tag, Loader2 } from 'lucide-react';
import { useEventCategories } from '@/infrastructure/api/hooks/useReferenceData';

export function CategoryFilter({ value, onChange, className = '' }) {
  const { data: categories, isLoading } = useEventCategories();

  if (isLoading) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <Loader2 className="h-5 w-5 animate-spin" />
        <span className="text-sm text-gray-500">Loading categories...</span>
      </div>
    );
  }

  return (
    <div className={`relative ${className}`}>
      <Tag className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
      <select
        value={value ?? ''}
        onChange={(e) => onChange(e.target.value !== '' ? parseInt(e.target.value, 10) : null)}
        className="block w-full pl-10 pr-10 py-2.5 border rounded-lg"
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

#### EventEditForm.tsx
```typescript
// Add hook
const { data: eventCategories, isLoading: isLoadingCategories } = useEventCategories();
const { data: referenceData } = useReferenceData(['EventCategory']);

// Build mapper dynamically
const categoryCodeToIntMap = useMemo(() =>
  referenceData ? buildCodeToIntMap(referenceData) : {},
  [referenceData]
);

// Use in conversion
const convertCategoryToNumber = useCallback((category: any): number => {
  if (typeof category === 'number') return category;
  return categoryCodeToIntMap[category] ?? EventCategory.Community;
}, [categoryCodeToIntMap]);

// Use in dropdown
{isLoadingCategories ? (
  <Loader2 className="h-5 w-5 animate-spin" />
) : (
  <select {...register('category', { valueAsNumber: true })}>
    {eventCategories?.map((option) => (
      <option key={option.value} value={option.value}>
        {option.label}
      </option>
    ))}
  </select>
)}
```

#### EventCreationForm.tsx
```typescript
// Replace hardcoded fallback
const { data: eventCategories, isLoading: isLoadingCategories } = useEventCategories();
const { data: referenceData } = useReferenceData(['EventCategory']);

const categoryCodeToIntMap = useMemo(() =>
  referenceData ? buildCodeToIntMap(referenceData) : {},
  [referenceData]
);

// Remove hardcoded fallback array entirely
// Show loading state instead
```

---

## Testing Strategy

### 1. Unit Tests
- Test specialized hooks return correct format
- Test mapping utilities handle edge cases
- Test loading states render correctly

### 2. Integration Tests
- Verify API calls fetch correct reference data
- Verify dropdowns populate from database
- Verify form submissions use correct intValues

### 3. Manual Testing Checklist
- [ ] CategoryFilter dropdown shows database categories
- [ ] EventEditForm loads with correct category selected
- [ ] EventCreationForm shows all categories from DB
- [ ] Loading states display during API fetch
- [ ] Form submissions send correct integer values
- [ ] Build succeeds with 0 errors
- [ ] No console errors about missing data

### 4. Verification Commands
```bash
# Search for any remaining hardcoded enum arrays
grep -r "EventCategory\." web/src/components
grep -r "EventCategory\." web/src/presentation

# Verify no enum-utils imports in UI components
grep -r "from.*enum-utils" web/src/components
grep -r "from.*enum-utils" web/src/presentation

# Check build succeeds
cd web && npm run build
```

---

## Implementation Order

1. **Phase 1**: Create specialized hooks (30 min)
   - Add `useEventCategories()`
   - Add `useEventStatuses()`
   - Add `useUserRoles()`
   - Add `useCurrencies()`

2. **Phase 2**: Create mapping utilities (20 min)
   - Create `enum-mappers.ts`
   - Add `getIntValueFromCode()`
   - Add `buildCodeToIntMap()`
   - Add `getNameFromIntValue()`

3. **Phase 3**: Fix CategoryFilter.tsx (15 min)
   - Replace `getEventCategoryOptions()` with `useEventCategories()`
   - Add loading state
   - Test dropdown functionality

4. **Phase 4**: Fix EventEditForm.tsx (30 min)
   - Add `useEventCategories()` hook
   - Build dynamic categoryCodeToIntMap
   - Remove hardcoded categoryOptions array
   - Add loading state for category dropdown
   - Test form submission

5. **Phase 5**: Fix EventCreationForm.tsx (20 min)
   - Remove hardcoded fallback array
   - Build dynamic categoryCodeToIntMap
   - Add proper loading state
   - Test form creation

6. **Phase 6**: Deprecate enum-utils.ts (10 min)
   - Add deprecation warning comment
   - Update documentation
   - Plan removal in next phase

7. **Phase 7**: Verification (15 min)
   - Run test suite
   - Manual testing checklist
   - Build verification
   - Update tracking docs

**Total Estimated Time**: 2 hours 20 minutes

---

## Files to Fix (Summary)

| File | Priority | Estimated Time | Status |
|------|----------|----------------|--------|
| `web/src/infrastructure/api/hooks/useReferenceData.ts` | HIGH | 30 min | Add hooks |
| `web/src/infrastructure/api/utils/enum-mappers.ts` | HIGH | 20 min | CREATE NEW |
| `web/src/components/events/filters/CategoryFilter.tsx` | HIGH | 15 min | FIX |
| `web/src/presentation/components/features/events/EventEditForm.tsx` | HIGH | 30 min | FIX |
| `web/src/presentation/components/features/events/EventCreationForm.tsx` | MEDIUM | 20 min | FIX |
| `web/src/lib/enum-utils.ts` | LOW | 10 min | DEPRECATE |

---

## Risk Assessment

### High Risk
- **CategoryFilter.tsx**: Used in public event listing page - high visibility
- **EventEditForm.tsx**: Used by organizers - business critical

### Medium Risk
- **EventCreationForm.tsx**: Already has API hook, just needs cleanup

### Low Risk
- **enum-utils.ts**: Only used by CategoryFilter, safe to deprecate

---

## Success Criteria

- [ ] All UI components use reference data API
- [ ] No hardcoded enum arrays in components
- [ ] Loading states shown during API fetch
- [ ] Form submissions send correct intValues
- [ ] Build completes with 0 errors
- [ ] All tests pass
- [ ] Manual testing checklist complete
- [ ] Documentation updated

---

## Next Steps

1. Review this audit report
2. Approve implementation plan
3. Execute fixes in order
4. Verify with testing strategy
5. Update PROGRESS_TRACKER.md
6. Create phase summary document

---

## Conclusion

This audit has identified **3 critical violations** and **1 partial violation** where hardcoded enums are still being used in UI components. The proposed architecture provides a clean, database-driven solution that maintains type safety while eliminating hardcoded values.

**Estimated Total Implementation Time**: 2 hours 20 minutes

**Recommended Action**: Proceed with implementation immediately to maintain system integrity and prevent future technical debt.
