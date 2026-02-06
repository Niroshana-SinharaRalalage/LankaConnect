# Reference Data Architecture - Documentation Index

**Phase**: 6A.47 Enhancement - Database-Driven Enum Replacement
**Date**: 2025-12-28
**Status**: Architecture Design Complete

---

## Overview

This documentation set provides a complete architecture for replacing 11 hardcoded enum locations with database-driven reference data API calls.

**Total Documentation**: 4 documents, 120KB of specifications, diagrams, and implementation guides

---

## Document Suite

### 1. Executive Summary (START HERE)
**File**: [REFERENCE_DATA_MIGRATION_SUMMARY.md](./REFERENCE_DATA_MIGRATION_SUMMARY.md)
**Size**: 9.5KB
**Reading Time**: 10 minutes

**Contents**:
- Problem statement and recommended solution
- Implementation plan (10 days, 5 phases)
- Architecture decision records
- Performance expectations
- Risk assessment and rollback plan
- Success metrics

**Audience**: Team leads, project managers, architects

---

### 2. Detailed Architecture Specification
**File**: [REFERENCE_DATA_ARCHITECTURE.md](./REFERENCE_DATA_ARCHITECTURE.md)
**Size**: 29KB
**Reading Time**: 30 minutes

**Contents**:
- Hook architecture design (3 options analyzed, Option A recommended)
- Mapping utilities design (6 utility functions)
- Loading state strategy (4 options analyzed, Option D recommended)
- Error handling strategy (4 options analyzed, Option B+D recommended)
- Implementation order (11 components prioritized)
- Code reuse patterns (3 patterns with examples)
- Validation strategy (3-layer testing)
- Performance considerations (multi-layer caching)
- Migration checklist (25 items)
- Future enhancements (3 phases)

**Audience**: Developers, architects

---

### 3. Visual Architecture Diagrams
**File**: [REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md](./REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md)
**Size**: 61KB
**Reading Time**: 20 minutes

**Contents**:
1. System architecture overview (component → API → database)
2. Data flow sequence diagram (cache layers, timing)
3. Component dependency graph (11 components mapped)
4. Hook architecture pattern (specialized hooks)
5. Utility function call graph (4 usage patterns)
6. Caching strategy diagram (multi-layer cache timeline)
7. Error handling flow (3 scenarios)
8. Migration phases timeline (2-week schedule)
9. Testing strategy diagram (3-layer verification)

**Audience**: Visual learners, architects, developers

---

### 4. Implementation Guide (MOST PRACTICAL)
**File**: [REFERENCE_DATA_IMPLEMENTATION_GUIDE.md](./REFERENCE_DATA_IMPLEMENTATION_GUIDE.md)
**Size**: 21KB
**Reading Time**: 15 minutes (copy-paste mode)

**Contents**:
- Step 1: Update hook file (complete code)
- Step 2: Update utility file (complete code)
- Step 3: Component migration examples (5 patterns)
  - CategoryFilter.tsx (dropdown)
  - EventDetailsTab.tsx (label mapping)
  - EventsList.tsx (switch statement replacement)
  - EventCreationForm.tsx (form dropdown)
  - Error handling component
- Step 4: Prefetcher component (optional optimization)
- Step 5: Verification commands (grep tests)
- Step 6: Testing checklist
- Common patterns cheat sheet
- Troubleshooting guide

**Audience**: Developers (implementation phase)

---

## Reading Roadmap

### For Team Leads / Project Managers
1. Read: **REFERENCE_DATA_MIGRATION_SUMMARY.md** (10 min)
2. Review: Implementation plan and timeline
3. Approve: Architecture decisions
4. Next: Assign to developer

### For Architects
1. Read: **REFERENCE_DATA_MIGRATION_SUMMARY.md** (10 min)
2. Read: **REFERENCE_DATA_ARCHITECTURE.md** (30 min)
3. Review: **REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md** (20 min)
4. Validate: Architecture decision records
5. Next: Approve for implementation

### For Developers (Implementation)
1. Read: **REFERENCE_DATA_MIGRATION_SUMMARY.md** (10 min)
2. Skim: **REFERENCE_DATA_ARCHITECTURE.md** (15 min - focus on Section 1-7)
3. **Use**: **REFERENCE_DATA_IMPLEMENTATION_GUIDE.md** (active reference during coding)
4. Reference: **REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md** (when stuck)
5. Next: Begin Phase 1 implementation

### For QA / Testers
1. Read: **REFERENCE_DATA_MIGRATION_SUMMARY.md** (10 min - focus on Testing Strategy)
2. Review: **REFERENCE_DATA_ARCHITECTURE.md** Section 7 (Validation)
3. Use: **REFERENCE_DATA_IMPLEMENTATION_GUIDE.md** Steps 5-6 (verification checklist)
4. Next: Create test plan

---

## Quick Reference

### Key Decisions

| Decision | Chosen Option | Rationale |
|----------|---------------|-----------|
| **Hook Architecture** | Specialized hooks per enum | Type safety, clear API |
| **Loading Strategy** | Prefetch on app load | Instant UX, no spinners |
| **Error Handling** | Retry + toast + retry button | Resilient, non-blocking |
| **Implementation Order** | Bottom-up (utilities first) | Stable foundation |

### Key Components

| Component | Purpose | Location |
|-----------|---------|----------|
| `useEventCategories()` | Fetch categories | `useReferenceData.ts` |
| `useEventStatuses()` | Fetch statuses | `useReferenceData.ts` |
| `useCurrencies()` | Fetch currencies | `useReferenceData.ts` |
| `getLabelFromIntValue()` | Lookup label | `enum-utils.ts` |
| `buildIntToLabelMap()` | Performance map | `enum-utils.ts` |

### Timeline Summary

| Phase | Duration | Components | Risk |
|-------|----------|------------|------|
| **Phase 1: Foundation** | 2 days | Hooks, utilities | Low |
| **Phase 2: Core** | 3 days | 3 components | Medium |
| **Phase 3: Display** | 3 days | 4 components | Low |
| **Phase 4: Edge Cases** | 1 day | 3 components | Low |
| **Phase 5: Verification** | 1 day | Testing | Low |
| **Total** | **10 days** | **11 components** | **Medium** |

---

## File Locations

### Documentation (Created)
```
docs/
├── REFERENCE_DATA_INDEX.md                    (this file)
├── REFERENCE_DATA_MIGRATION_SUMMARY.md        (executive summary)
├── REFERENCE_DATA_ARCHITECTURE.md             (detailed spec)
├── REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md     (visual diagrams)
└── REFERENCE_DATA_IMPLEMENTATION_GUIDE.md     (code examples)
```

### Files to Create
```
web/src/components/ReferenceDataPrefetcher.tsx (NEW)
```

### Files to Update
```
web/src/infrastructure/api/hooks/useReferenceData.ts (UPDATE)
web/src/lib/enum-utils.ts (UPDATE)
web/src/app/layout.tsx (UPDATE - add prefetcher)
web/src/components/events/filters/CategoryFilter.tsx (UPDATE)
web/src/presentation/components/features/events/EventCreationForm.tsx (UPDATE)
web/src/presentation/components/features/events/EventDetailsTab.tsx (UPDATE)
web/src/presentation/components/features/events/EventsList.tsx (UPDATE)
web/src/presentation/components/features/events/EventEditForm.tsx (UPDATE)
web/src/app/events/page.tsx (UPDATE)
web/src/app/templates/page.tsx (UPDATE)
web/src/app/events/[id]/page.tsx (UPDATE)
web/src/app/events/[id]/manage/page.tsx (UPDATE)
```

---

## Implementation Checklist

### Pre-Implementation
- [ ] Read REFERENCE_DATA_MIGRATION_SUMMARY.md
- [ ] Review architecture with team
- [ ] Approve implementation plan
- [ ] Verify backend API is ready (`/api/reference-data`)
- [ ] Verify database is seeded with reference data

### Phase 1: Foundation
- [ ] Update `useReferenceData.ts` (add specialized hooks)
- [ ] Update `enum-utils.ts` (add utility functions)
- [ ] Create `ReferenceDataPrefetcher.tsx`
- [ ] Add prefetcher to `layout.tsx`
- [ ] Write unit tests
- [ ] Test hooks in isolation

### Phase 2-4: Component Migration
- [ ] Migrate 11 components (see implementation guide)
- [ ] Run grep tests after each component
- [ ] Test each component thoroughly
- [ ] Update component tests

### Phase 5: Verification
- [ ] Run all grep tests (no violations)
- [ ] Run `npm run typecheck` (0 errors)
- [ ] Run `npm run build` (0 errors)
- [ ] Complete manual testing checklist
- [ ] Performance testing (cache behavior)

### Post-Implementation
- [ ] Create Phase 6A.47 summary document
- [ ] Update PROGRESS_TRACKER.md
- [ ] Update PHASE_6A_MASTER_INDEX.md
- [ ] Remove deprecated functions (after warning period)
- [ ] Document lessons learned

---

## API Contract Reference

### Endpoint
```
GET /api/reference-data?types=EventCategory,EventStatus&activeOnly=true
```

### Response Structure
```typescript
interface ReferenceValue {
  id: string;
  enumType: string;        // "EventCategory", "EventStatus", etc.
  code: string;            // "Religious", "Cultural", etc.
  intValue: number | null; // 0, 1, 2, etc.
  name: string;            // "Religious Events", "Cultural Events", etc.
  description: string | null;
  displayOrder: number;
  isActive: boolean;
  metadata: Record<string, unknown> | null;
}
```

### Transformed to UI
```typescript
interface EnumOption {
  value: number;          // intValue
  code: string;           // code
  label: string;          // name
  description?: string;   // description
  displayOrder: number;   // displayOrder
}
```

---

## Performance Targets

| Metric | Target | Measurement |
|--------|--------|-------------|
| **First Load** | < 200ms | Backend cache miss |
| **Cached Load** | < 10ms | React Query memory |
| **Cache Duration** | 1 hour | Matches backend |
| **Bundle Size Increase** | < 5KB | All hooks + utils |
| **API Call Reduction** | 100% | No duplicate calls per session |

---

## Success Criteria

### Technical
- Zero hardcoded enum arrays (grep test passes)
- Zero TypeScript errors
- Zero build errors
- 90%+ test coverage maintained

### UX
- Dropdowns load instantly (< 100ms)
- No "Unknown" labels in production
- Error rate < 0.1%

### Maintenance
- Single source of truth (database)
- Easy to add new categories (no frontend changes)
- Reduced code duplication (11 locations → 3 hooks)

---

## Support

### Questions During Implementation
1. Check **REFERENCE_DATA_IMPLEMENTATION_GUIDE.md** troubleshooting section
2. Review **REFERENCE_DATA_ARCHITECTURE_DIAGRAM.md** for visual reference
3. Refer to **REFERENCE_DATA_ARCHITECTURE.md** for detailed specs

### Issues or Blockers
- Create GitHub issue with label `phase-6a47`
- Tag: `@system-architect`
- Include: Component name, error message, expected vs actual behavior

---

## Related Documentation

- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md) - Phase 6A overview
- [PROGRESS_TRACKER.md](./PROGRESS_TRACKER.md) - Implementation tracking
- [STREAMLINED_ACTION_PLAN.md](./STREAMLINED_ACTION_PLAN.md) - Project roadmap
- [Master Requirements Specification.md](./Master%20Requirements%20Specification.md) - System requirements

---

## Version History

| Version | Date | Changes | Author |
|---------|------|---------|--------|
| 1.0 | 2025-12-28 | Initial architecture design | System Architecture Designer |

---

## Next Steps

1. **Review** - Team reviews this index and all 4 documents
2. **Approve** - Sign off on architecture decisions
3. **Plan** - Schedule 10-day implementation window
4. **Implement** - Begin Phase 1 (Foundation)
5. **Track** - Update PROGRESS_TRACKER.md daily

**Estimated Start Date**: TBD
**Estimated Completion**: TBD + 10 days
**Phase Number**: 6A.47
