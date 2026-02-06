# Complete Enum Usage Audit - All Locations

**Generated**: 2025-12-28
**Context**: Phase 6A.47 - Comprehensive enum replacement verification
**User Request**: "Show me the list of location we use enums and them how many are replaced with reference data"

---

## Executive Summary

| Enum Type | Total Usages | ✅ Database API | ❌ Hardcoded | ⚠️ Type/Logic Only | 📋 Status |
|-----------|-------------|----------------|--------------|-------------------|-----------|
| **EventCategory** | 73 locations | 2 | 8 | 63 | **89% Needs Fix** |
| **EventStatus** | 28 locations | 0 | 2 | 26 | **93% Type-Only** |
| **UserRole** | 34 locations | 0 | 0 | 34 | **100% Type-Only** |
| **Currency** | 38 locations | 0 | 38 | 0 | **100% Hardcoded** |
| **TOTAL** | **173 locations** | **2 (1%)** | **48 (28%)** | **123 (71%)** | **Only 1% Fixed!** |

---

## ❌ CRITICAL FINDINGS

### EventCategory - 8 Hardcoded UI Locations (MUST FIX)

1. **CategoryFilter.tsx** (Line 30) ❌
   - Uses: `getEventCategoryOptions()` helper
   - Purpose: Event filter dropdown
   - **MUST replace with API**

2. **enum-utils.ts** (Lines 13-20) ❌
   - Function: `getEventCategoryOptions()`
   - Used by: CategoryFilter.tsx, events/page.tsx
   - **MUST deprecate and replace**

3. **EventEditForm.tsx** (Lines 56-63, 388-395) ❌
   - Hardcoded: `categoryMap` object + `categoryOptions` array
   - Purpose: Event edit form dropdown
   - **MUST replace with API**

4. **EventCreationForm.tsx** (Lines 253-275) ⚠️
   - Uses API: `useEventInterests()` ✅
   - BUT: Has hardcoded fallback array ❌
   - **MUST remove fallback**

5. **events/page.tsx** (Lines 155, 250) ❌
   - Uses: `getEventCategoryOptions()` in event listing
   - Purpose: Statistics display + category badges
   - **MUST replace with API**

6. **templates/page.tsx** (Lines 32-39) ❌
   - Hardcoded: Category options array
   - Purpose: Template filter dropdown
   - **MUST replace with API**

7. **events/[id]/page.tsx** (Lines 106-113) ❌
   - Hardcoded: Category name mapping object
   - Purpose: Display event category name
   - **MUST replace with API**

8. **EventDetailsTab.tsx** (Lines 58-65) ❌
   - Hardcoded: Category name mapping object
   - Purpose: Display event details
   - **MUST replace with API**

### EventStatus - 2 Hardcoded UI Locations (MUST FIX)

1. **events/[id]/manage/page.tsx** (Lines 55-62) ❌
   - Hardcoded: `statusLabels` mapping object
   - Purpose: Display event status badge
   - **MUST replace with API**

2. **EventsList.tsx** (Lines 76-94) ❌
   - Hardcoded: Status badge color mapping (switch statement)
   - Purpose: Status badge rendering
   - **MUST replace with API**

### Currency - 38 Hardcoded Locations (CONSIDER FOR FUTURE)

All Currency enum usages are hardcoded in dropdowns:
- EventCreationForm.tsx: 10 locations
- EventEditForm.tsx: 15 locations
- GroupPricingTierBuilder.tsx: 3 locations

**Note**: Currency is less critical (only 2 values: USD, LKR), but should eventually use reference data for consistency.

---

## ✅ Files Using Database API (Only 2!)

### 1. CulturalInterestsSection.tsx ✅
```typescript
const { data: eventInterests, isLoading } = useEventInterests();
```
- **Status**: ✅ Correct
- **Purpose**: User profile event interests selection
- **API Call**: `GET /api/reference-data?types=EventCategory`

### 2. EventCreationForm.tsx ⚠️ (Partial)
```typescript
const { data: eventInterests, isLoading } = useEventInterests();
const categoryOptions = eventInterests?.map(...) ?? [
  // ❌ BAD: Hardcoded fallback array
  { value: EventCategory.Religious, label: 'Religious' },
  // ... 7 more hardcoded values
];
```
- **Status**: ⚠️ Partial (uses API but has bad fallback)
- **Fix Needed**: Remove hardcoded fallback, show loading state instead

---

## ⚠️ Type-Only Usages (Correct - Keep These)

These files use enums for **type safety, logic, and validation** (NOT UI data):

### UserRole (34 locations - All Type-Only) ✅
- **role-helpers.ts**: Type guards (`isAdmin()`, `canCreateEvent()`, etc.) ✅
- **Page auth checks**: `user.role === UserRole.Admin` ✅
- **RegisterForm.tsx**: Mapping user selection to enum value ✅

### EventCategory (63 locations - Type-Only) ✅
- **EventsList.tsx**: Switch statement for badge colors ✅
- **Dashboard logic**: Role-based UI rendering ✅
- **Default values**: `category: EventCategory.Community` ✅

### EventStatus (26 locations - Type-Only) ✅
- **Logic checks**: `event.status === EventStatus.Draft` ✅
- **Conditional rendering**: `status === EventStatus.Published` ✅
- **Type safety**: Function parameters and return types ✅

---

## 📊 Breakdown by File Purpose

### UI Components Needing Reference Data API

| File | Enum Type | Lines | Purpose | Fix Status |
|------|-----------|-------|---------|------------|
| CategoryFilter.tsx | EventCategory | 30 | Filter dropdown | ❌ Not Fixed |
| EventEditForm.tsx | EventCategory | 56-63, 388-395 | Form dropdown | ❌ Not Fixed |
| EventCreationForm.tsx | EventCategory | 253-275 | Form dropdown | ⚠️ Partial |
| events/page.tsx | EventCategory | 155, 250 | Statistics + badges | ❌ Not Fixed |
| templates/page.tsx | EventCategory | 32-39 | Template filter | ❌ Not Fixed |
| events/[id]/page.tsx | EventCategory | 106-113 | Display name | ❌ Not Fixed |
| EventDetailsTab.tsx | EventCategory | 58-65 | Display details | ❌ Not Fixed |
| events/[id]/manage/page.tsx | EventStatus | 55-62 | Status badge | ❌ Not Fixed |
| EventsList.tsx | EventStatus | 76-94 | Status badges | ❌ Not Fixed |

### Helper Files to Deprecate

| File | Function | Used By | Fix Status |
|------|----------|---------|------------|
| enum-utils.ts | `getEventCategoryOptions()` | CategoryFilter.tsx, events/page.tsx | ❌ Not Deprecated |
| enum-utils.ts | `getEventCategoryLabel()` | events/page.tsx | ❌ Not Deprecated |

---

## 🎯 What Needs to Be Fixed

### Priority 1: EventCategory Dropdowns (8 files)

1. ✅ CulturalInterestsSection.tsx - **Already fixed**
2. ⚠️ EventCreationForm.tsx - **Partial (remove fallback)**
3. ❌ EventEditForm.tsx - **Not fixed**
4. ❌ CategoryFilter.tsx - **Not fixed**
5. ❌ events/page.tsx - **Not fixed**
6. ❌ templates/page.tsx - **Not fixed**
7. ❌ events/[id]/page.tsx - **Not fixed**
8. ❌ EventDetailsTab.tsx - **Not fixed**

### Priority 2: EventStatus Display (2 files)

1. ❌ events/[id]/manage/page.tsx - **Not fixed**
2. ❌ EventsList.tsx - **Not fixed**

### Priority 3: Currency Dropdowns (3 files - Future)

1. ❌ EventCreationForm.tsx - **Not fixed**
2. ❌ EventEditForm.tsx - **Not fixed**
3. ❌ GroupPricingTierBuilder.tsx - **Not fixed**

### Deprecate Helper Files

1. ❌ enum-utils.ts - **Must deprecate**

---

## 📈 Progress Tracking

| Metric | Current | Target | Progress |
|--------|---------|--------|----------|
| EventCategory UI locations using API | 1.5 / 8 | 8 / 8 | **19%** |
| EventStatus UI locations using API | 0 / 2 | 2 / 2 | **0%** |
| Currency UI locations using API | 0 / 3 | 3 / 3 | **0%** |
| Helper files deprecated | 0 / 1 | 1 / 1 | **0%** |
| **OVERALL COMPLETION** | **1.5 / 14** | **14 / 14** | **11%** |

---

## ✅ Files That Are CORRECT (Do Not Change)

### Type Definitions (Keep Enum Imports)
- `events.types.ts` - EventCategory, EventStatus enum definitions ✅
- `auth.types.ts` - UserRole enum definition ✅

### Validators (Keep Enum Imports)
- `event.schemas.ts` - Zod schema validation ✅

### Type Guards & Helpers (Keep Enum Imports)
- `role-helpers.ts` - Role permission checks ✅
- `eventMapper.ts` - DTO mapping ✅

### Page Logic (Keep Enum Comparisons)
- All `user.role === UserRole.Admin` checks ✅
- All `event.status === EventStatus.Draft` checks ✅
- All `category === EventCategory.Religious` comparisons ✅

---

## 🔍 Search Patterns Used

```bash
# Pattern 1: Direct enum property access
(EventCategory|EventStatus|UserRole|Currency)\s*(\.|\[)

# Pattern 2: Helper function usage
(getEventCategoryOptions|getEventStatusOptions|getUserRoleOptions)

# Pattern 3: API hook usage
(useEventInterests|useReferenceData|useEventCategories)
```

---

## 📝 Notes

1. **Type-only usages are CORRECT**: 123 locations (71%) use enums for type safety and logic. These should NOT be changed.

2. **Only 1% actually using database**: Despite Phase 6A.47's goal, only 1 file (CulturalInterestsSection.tsx) correctly loads data from the database API without fallbacks.

3. **28% still hardcoded for UI**: 48 locations use hardcoded enum values for dropdown options, display names, and badges. These MUST be fixed.

4. **Currency is everywhere**: 38 hardcoded Currency usages. Consider adding to reference data in future phase.

5. **Helper functions cascade the problem**: `enum-utils.ts` is used by multiple files, so deprecating it requires updating all consumers.

---

## 🎓 Key Takeaway

**User was 100% correct to question the completion status.**

Out of 173 total enum usages:
- **Only 2 locations (1%)** are using the database API
- **48 locations (28%)** are still hardcoded for UI purposes
- **11% overall completion** for the reference data migration

The phase is **NOT complete** despite earlier claims.
