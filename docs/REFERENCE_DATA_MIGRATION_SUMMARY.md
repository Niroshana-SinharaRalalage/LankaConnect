# Reference Data Architecture - Executive Summary

**Phase**: 6A.47 Enhancement
**Date**: 2025-12-28
**Status**: Architecture Design Complete
**Next**: Begin Implementation

---

## Problem Statement

Currently, 11 frontend locations use hardcoded enum arrays and switch statements for EventCategory, EventStatus, and Currency. This violates DRY principles and makes the codebase brittle to database changes.

**Current Issues**:
- Hardcoded category labels in 8 components
- 2 switch statements mapping enums to display values
- No single source of truth for label mappings
- Backend database has reference data API but frontend doesn't use it

---

## Recommended Solution

**Architecture**: Specialized React Query hooks with utility functions

### Key Components

1. **Specialized Hooks** (Option A):
   - `useEventCategories()` - Returns EnumOption[]
   - `useEventStatuses()` - Returns EnumOption[]
   - `useCurrencies()` - Returns EnumOption[]

2. **Utility Functions**:
   - `getLabelFromIntValue()` - Single lookups
   - `buildIntToLabelMap()` - Performance-optimized loops
   - `getEventCategoryLabelFromData()` - Type-safe wrappers

3. **Loading Strategy** (Option D):
   - Prefetch on app initialization
   - 1-hour cache (React Query + Backend)
   - No loading spinners in dropdowns

4. **Error Handling** (Option B + D):
   - 3x auto-retry with exponential backoff
   - Toast notifications
   - Retry button fallback

---

## Implementation Plan

### Phase 1: Foundation (2 days)
1. Update `useReferenceData.ts` - Add specialized hooks
2. Update `enum-utils.ts` - Add utility functions
3. Create `ReferenceDataPrefetcher.tsx` - Add to app layout
4. Write unit tests

### Phase 2: Core Components (3 days)
5. Migrate `CategoryFilter.tsx` - Filter dropdown
6. Migrate `EventCreationForm.tsx` - Category selection
7. Migrate `events/page.tsx` - 3 filter usages

### Phase 3: Display Components (3 days)
8. Migrate `EventDetailsTab.tsx` - Category label mapping
9. Migrate `EventsList.tsx` - 2 switch statements
10. Migrate `events/[id]/page.tsx` - Category display
11. Migrate `EventEditForm.tsx` - 2 label mappings

### Phase 4: Edge Cases (1 day)
12. Migrate `templates/page.tsx` - Category lookup
13. Migrate `events/[id]/manage/page.tsx` - Status/category labels
14. Update `enum-utils.ts` - Deprecate old functions

### Phase 5: Verification (1 day)
15. Run grep tests - Verify no hardcoded arrays
16. Run typecheck and build - 0 errors
17. Manual testing - All 11 components
18. Documentation update

**Total Time**: 10 days

---

## Architecture Decisions

### Decision 1: Hook Architecture
**Chosen**: Option A - Specialized hooks per enum type
**Rationale**:
- Type safety (each hook returns specific EnumOption[])
- Developer experience (clear API, autocomplete-friendly)
- Independent caching per enum
- Easy to extend with enum-specific logic

**Rejected Alternatives**:
- Option B (Single parameterized hook) - Less type-safe
- Option C (Helper hooks only) - Requires too much boilerplate

### Decision 2: Loading Strategy
**Chosen**: Option D - Prefetch on app load
**Rationale**:
- Instant UX (no loading spinners)
- Backend cache lasts 1 hour (perfect for prefetch)
- Small payload (~5KB total)
- React Query memory cache

**Rejected Alternatives**:
- Option A (Disable dropdown) - Poor UX
- Option B (Loading option) - Unnecessary with prefetch
- Option C (Spinner overlay) - Over-engineered

### Decision 3: Error Handling
**Chosen**: Option B + D - Retry with toast + Empty state
**Rationale**:
- Resilient to transient network errors
- Non-blocking UX (toast doesn't interrupt)
- Retry button for persistent errors
- No fallback to hardcoded values (prevents stale data)

**Rejected Alternatives**:
- Option A (Disable only) - Blocks user
- Option C (Fallback to hardcoded) - Defeats purpose of API

### Decision 4: Implementation Order
**Chosen**: Bottom-up (utilities → hooks → components)
**Rationale**:
- Stable foundation before component migration
- Test utilities in isolation
- Core components first (CategoryFilter)
- Pages last (entry points, test thoroughly)

---

## API Contract

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
    "description": "Religious ceremonies and celebrations",
    "displayOrder": 1,
    "isActive": true,
    "metadata": null
  }
]
```

### TypeScript Interface
```typescript
export interface EnumOption {
  value: number;          // Enum int (e.g., EventCategory.Religious = 0)
  code: string;           // DB code (e.g., "Religious")
  label: string;          // Display name (e.g., "Religious Events")
  description?: string;   // Optional description
  displayOrder: number;   // Sort order
}
```

---

## Performance Expectations

| Metric | Value | Notes |
|--------|-------|-------|
| **First Load** | ~200ms | Backend cache miss |
| **Subsequent Loads** | ~10ms | React Query memory |
| **After 1 Hour** | ~50ms | Backend cache hit |
| **Cache Size** | ~5KB | All enums combined |
| **Stale Time** | 1 hour | Matches backend cache |

---

## Testing Strategy

### Layer 1: Automated Grep Tests
```bash
grep -r "EventCategory\.(Religious|Cultural)" web/src
grep -r "switch.*EventCategory" web/src
```
**Expected**: No matches

### Layer 2: TypeScript Compilation
```bash
npm run typecheck
npm run build
```
**Expected**: 0 errors

### Layer 3: Manual Testing
- Category dropdown loads instantly
- All labels display correctly
- Form submission saves correct values
- Error state shows retry button
- Network tab shows cached API calls

---

## Rollback Plan

### Quick Rollback (5 minutes)
```bash
git revert HEAD
git push
```

### Partial Rollback (Component-Level)
Add feature flag to each component:
```typescript
const USE_REFERENCE_DATA = process.env.NEXT_PUBLIC_USE_REFERENCE_DATA === 'true';
```

### Gradual Migration
Keep deprecated functions during transition:
```typescript
export function getEventCategoryOptions() {
  console.warn('Deprecated. Use useEventCategories hook.');
  return Object.entries(EventCategory)...
}
```

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **API Downtime** | Low | High | 3x auto-retry, retry button |
| **Breaking Changes** | Medium | High | Comprehensive testing, gradual rollout |
| **Performance Degradation** | Low | Medium | Prefetch, 1-hour cache |
| **Type Errors** | Low | Medium | TypeScript compilation, unit tests |

---

## Success Metrics

### Technical Metrics
- [ ] 0 hardcoded enum arrays (grep test passes)
- [ ] 0 switch statements on EventCategory
- [ ] 0 TypeScript errors
- [ ] 0 build errors
- [ ] 90%+ test coverage

### UX Metrics
- [ ] Dropdowns load instantly (< 100ms)
- [ ] No "Unknown" labels in production
- [ ] Error rate < 0.1%
- [ ] User satisfaction maintained

### Maintenance Metrics
- [ ] Single source of truth (database)
- [ ] Easy to add new categories
- [ ] No frontend changes needed for DB updates
- [ ] Reduced code duplication (11 → 3 functions)

---

## Future Enhancements

### Phase 2: Additional Enums (Phase 6A.48)
Expand architecture to:
- DiscountType
- LanguageId
- PaymentType
- PriceType
- SponsorshipTier
- TicketType
- VisibilityLevel

### Phase 3: Metadata Support (Phase 6A.49)
Leverage `metadata` field:
```json
{
  "code": "Religious",
  "metadata": {
    "badgeColor": "purple",
    "icon": "church",
    "requiresApproval": true
  }
}
```

### Phase 4: Localization (Phase 6A.50)
Use metadata for translations:
```json
{
  "metadata": {
    "translations": {
      "si": "ආගමික උත්සව",
      "ta": "சமய நிகழ்வுகள்"
    }
  }
}
```

---

## Documentation Deliverables

1. **REFERENCE_DATA_ARCHITECTURE.md** - Detailed architecture specification
2. **REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md** - Visual diagrams and flows
3. **REFERENCE_DATA_IMPLEMENTATION_GUIDE.md** - Copy-paste code examples
4. **REFERENCE_DATA_MIGRATION_SUMMARY.md** - This document

---

## Approval Checklist

- [ ] Architecture reviewed by team
- [ ] Implementation order approved
- [ ] Performance expectations acceptable
- [ ] Rollback plan in place
- [ ] Test coverage adequate
- [ ] Documentation complete

---

## Next Steps

1. **Review** - Team reviews all 4 architecture documents
2. **Approve** - Get sign-off on implementation plan
3. **Implement** - Begin Phase 1 (Foundation)
4. **Track** - Update `PROGRESS_TRACKER.md` daily
5. **Test** - Run verification at each phase
6. **Document** - Create Phase summary on completion

---

## Questions for Review

1. Should we enable feature flags for gradual rollout?
2. Should we add Sentry error tracking for API failures?
3. Should we cache in localStorage for offline support?
4. Should we implement the prefetcher immediately or start without it?

---

## Document Metadata

- **Created**: 2025-12-28
- **Version**: 1.0
- **Author**: System Architecture Designer
- **Status**: Architecture Design Complete
- **Phase**: 6A.47 Enhancement

**Related Documents**:
- [REFERENCE_DATA_ARCHITECTURE.md](./REFERENCE_DATA_ARCHITECTURE.md) - Full specification
- [REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md](./REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md) - Visual diagrams
- [REFERENCE_DATA_IMPLEMENTATION_GUIDE.md](./REFERENCE_DATA_IMPLEMENTATION_GUIDE.md) - Code examples
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Implementation tracking
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) - Phase overview
