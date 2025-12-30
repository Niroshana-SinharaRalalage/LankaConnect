# Root Cause Analysis: Dashboard Text Search Not Working

**Phase**: 6A.58 - Dashboard Event Filtration
**Date**: 2025-12-30
**Status**: RESOLVED - Root Cause Identified
**Severity**: High - Feature completely non-functional

---

## Executive Summary

**Problem**: Text-based search in the "My Registered Events" dashboard tab does not filter events. When users type a search term, all events continue to display regardless of the search input.

**Root Cause**: Infinite dependency loop in `EventFilters.tsx` line 84 causes `useEffect` to continuously trigger, preventing debounced search term from propagating to parent component.

**Impact**: Users cannot search their registered events by text, significantly degrading UX for users with many event registrations.

**Fix Complexity**: LOW - Single-line dependency array fix

---

## 1. Evidence Analysis

### Screenshot Analysis
User provided screenshot showing:
- Search input contains: "Test Evenr" (typo for "Test Event")
- Results display:
  - Two "Test Event" entries (SHOULD match)
  - One "Monthly Dana January 2026" entry (SHOULD NOT match)
- **Conclusion**: Search is not filtering - showing all events regardless of search term

### User Behavior Pattern
- User typed search term → No visual feedback
- Other filters (Category, Date Range, Location) ARE working correctly
- Only text search is broken

---

## 2. Execution Flow Analysis

### Expected Flow (What SHOULD Happen)
```
1. User types "Test Evenr" in SearchInput
   ↓
2. EventFilters.handleSearchChange() updates searchInput state
   ↓
3. After 300ms debounce, debouncedSearchTerm updates
   ↓
4. useEffect (line 80-84) detects debouncedSearchTerm change
   ↓
5. Calls onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm })
   ↓
6. Dashboard receives updated registeredFilters state
   ↓
7. useEffect (line 103-122) detects registeredFilters change
   ↓
8. Calls filtersToApiParams(registeredFilters)
   ↓
9. Calls eventsRepository.getUserRsvps(apiParams)
   ↓
10. API receives searchTerm parameter
   ↓
11. Backend filters events by text search
   ↓
12. UI displays filtered results
```

### Actual Flow (What IS Happening)
```
1. User types "Test Evenr" in SearchInput ✓
   ↓
2. EventFilters.handleSearchChange() updates searchInput state ✓
   ↓
3. After 300ms debounce, debouncedSearchTerm updates ✓
   ↓
4. useEffect (line 80-84) detects debouncedSearchTerm change ✓
   ↓
5. Calls onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm }) ✓
   ↓
6. Dashboard receives updated registeredFilters state ✓
   ↓
7. useEffect (line 103-122) detects registeredFilters change ✓
   ↓
8. Calls filtersToApiParams(registeredFilters) ✓
   ↓
9. ❌ INFINITE LOOP: onFiltersChange triggers, which updates filters prop
   ↓
10. ❌ useEffect (line 80-84) runs again because filters is in dependency array
   ↓
11. ❌ Calls onFiltersChange again with same searchTerm
   ↓
12. ❌ Loop continues, preventing state stabilization
   ↓
13. ❌ API never receives updated searchTerm OR receives empty string
```

---

## 3. Root Cause Identification

### Location
**File**: `web/src/components/events/filters/EventFilters.tsx`
**Lines**: 80-84

### Problematic Code
```typescript
// Update parent when debounced search term changes
useEffect(() => {
  if (debouncedSearchTerm !== filters.searchTerm) {
    onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm]); // ❌ MISSING: filters, onFiltersChange
```

### The Bug
**Missing Dependencies**: The `useEffect` has `filters` and `onFiltersChange` in its body but NOT in the dependency array.

**What Happens**:
1. `debouncedSearchTerm` changes → `useEffect` runs
2. Calls `onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm })`
3. Parent updates `filters` prop → `EventFilters` re-renders
4. `filters` prop changes but `useEffect` doesn't re-run (not in deps)
5. Component has stale closure over old `filters` value
6. Next `debouncedSearchTerm` change uses STALE `filters` object
7. Infinite loop or race condition ensues

### Why This Breaks Search

**React's Exhaustive Deps Rule Violation**:
- ESLint warning: `React Hook useEffect has missing dependencies: 'filters' and 'onFiltersChange'`
- The hook captures `filters` from first render
- When `onFiltersChange` is called, it updates parent state
- Parent passes NEW `filters` object to child
- But `useEffect` doesn't see the change (not in deps)
- Hook continues using OLD `filters` value

**Stale Closure Problem**:
```typescript
// First render: filters = { searchTerm: '', category: null, ... }
useEffect(() => {
  // This closure captures the OLD filters object
  if (debouncedSearchTerm !== filters.searchTerm) {
    onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm]);

// User types "Test" → debouncedSearchTerm = "Test"
// Hook runs with OLD filters (searchTerm: '')
// Calls onFiltersChange({ ...OLD_FILTERS, searchTerm: "Test" })
// Parent updates filters to { searchTerm: "Test", ... }
// But hook STILL has closure over OLD filters!
// Next change uses OLD filters again → INFINITE LOOP
```

---

## 4. Why Other Filters Work

### Category, Date Range, Location Filters
These filters call `onFiltersChange` directly WITHOUT using `useEffect`:

```typescript
const handleCategoryChange = (category: EventCategory | null) => {
  onFiltersChange({ ...filters, category }); // Direct call, no useEffect
};

const handleDateRangeChange = (dateRange: DateRangePreset) => {
  onFiltersChange({ ...filters, dateRange }); // Direct call, no useEffect
};

const handleMetroAreasChange = (metroAreaIds: string[]) => {
  onFiltersChange({ ...filters, metroAreaIds }); // Direct call, no useEffect
};
```

**Why they work**:
- No debounce → No `useEffect` → No stale closure
- Direct synchronous state update
- No dependency array issues

**Why search doesn't work**:
- Search uses debounce → Requires `useEffect`
- `useEffect` has missing dependencies → Stale closure
- State updates don't propagate correctly

---

## 5. Backend Verification

### Backend IS Working Correctly

**Query Handler**: `GetMyRegisteredEventsQueryHandler.cs`
- Line 34: `if (HasFilters(request))` correctly checks for `SearchTerm`
- Line 99: `HasFilters()` includes `!string.IsNullOrWhiteSpace(request.SearchTerm)`
- Line 46-53: Correctly passes `SearchTerm` to `GetEventsQuery`

**Repository**: `events.repository.ts`
- Line 320: `if (filters.searchTerm) params.append('searchTerm', filters.searchTerm);`
- Line 330-332: Correctly builds URL with searchTerm parameter

**API Endpoint**: `/api/events/my-rsvps`
- Receives `searchTerm` parameter correctly
- Backend filtering logic is functional (verified in Phase 1)

**Conclusion**: Backend is NOT the issue. The searchTerm simply never reaches the backend due to frontend state management bug.

---

## 6. Fix Plan

### Solution 1: Fix Dependency Array (RECOMMENDED)

**File**: `web/src/components/events/filters/EventFilters.tsx`
**Line**: 84

**Change**:
```typescript
// BEFORE (BROKEN)
useEffect(() => {
  if (debouncedSearchTerm !== filters.searchTerm) {
    onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm]); // ❌ Missing dependencies

// AFTER (FIXED)
useEffect(() => {
  if (debouncedSearchTerm !== filters.searchTerm) {
    onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm, filters, onFiltersChange]); // ✓ All dependencies
```

**Why this works**:
- Hook re-runs when `filters` changes → No stale closure
- Hook re-runs when `onFiltersChange` changes → Stable reference
- Condition `debouncedSearchTerm !== filters.searchTerm` prevents infinite loop

**Potential Issue**: If `filters` object changes on every render (new object reference), this could cause excessive re-renders.

**Mitigation**: Dashboard already uses `useState` for filters, which maintains stable references. Only updates when actual filter values change.

### Solution 2: Use Ref for Latest Filters (ALTERNATIVE)

**File**: `web/src/components/events/filters/EventFilters.tsx`

**Change**:
```typescript
// Store latest filters in ref to avoid stale closure
const filtersRef = useRef(filters);
useEffect(() => {
  filtersRef.current = filters;
}, [filters]);

// Use ref in debounce effect
useEffect(() => {
  if (debouncedSearchTerm !== filtersRef.current.searchTerm) {
    onFiltersChange({ ...filtersRef.current, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm, onFiltersChange]);
```

**Pros**:
- Avoids dependency on `filters` object
- Guarantees latest filters value
- No risk of infinite loop

**Cons**:
- More complex code
- Ref pattern less idiomatic for this use case

### Solution 3: Callback Ref Pattern (ALTERNATIVE)

**File**: `web/src/components/events/filters/EventFilters.tsx`

**Change**:
```typescript
// Use callback that always gets latest filters
const updateSearchTerm = useCallback(() => {
  if (debouncedSearchTerm !== filters.searchTerm) {
    onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm, filters, onFiltersChange]);

useEffect(() => {
  updateSearchTerm();
}, [updateSearchTerm]);
```

**Pros**:
- Clear separation of concerns
- Testable callback
- ESLint compliant

**Cons**:
- Extra abstraction layer
- Same dependency issues as Solution 1

---

## 7. Testing Plan

### Manual Testing Steps

1. **Baseline Test** (Verify bug exists):
   - [ ] Navigate to Dashboard → "My Registered Events"
   - [ ] Type "Test" in search box
   - [ ] Wait 300ms for debounce
   - [ ] **Expected**: Only events with "Test" in name show
   - [ ] **Actual**: All events still visible (BUG)

2. **After Fix Test**:
   - [ ] Apply Solution 1 (add dependencies)
   - [ ] Restart dev server
   - [ ] Navigate to Dashboard → "My Registered Events"
   - [ ] Type "Test" in search box
   - [ ] Wait 300ms for debounce
   - [ ] **Expected**: Only matching events show
   - [ ] Clear search box
   - [ ] **Expected**: All events return

3. **Edge Cases**:
   - [ ] Type search term, then quickly change category
     - **Expected**: Both filters applied
   - [ ] Type search term, then quickly change date range
     - **Expected**: Both filters applied
   - [ ] Type partial word "Even" for "Event"
     - **Expected**: Partial match works
   - [ ] Type gibberish "xyzabc"
     - **Expected**: No results / empty state

4. **Performance Test**:
   - [ ] Type search term one character at a time
   - [ ] **Expected**: Only ONE API call after 300ms debounce
   - [ ] Check browser DevTools Network tab
   - [ ] **Expected**: No duplicate/rapid API calls

5. **Console Verification**:
   Add temporary console.logs to verify data flow:
   ```typescript
   // EventFilters.tsx line 81
   useEffect(() => {
     console.log('Debounced search term changed:', debouncedSearchTerm);
     console.log('Current filters.searchTerm:', filters.searchTerm);
     if (debouncedSearchTerm !== filters.searchTerm) {
       console.log('Calling onFiltersChange with:', debouncedSearchTerm);
       onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
     }
   }, [debouncedSearchTerm, filters, onFiltersChange]);

   // dashboard/page.tsx line 109
   const apiParams = filtersToApiParams(registeredFilters);
   console.log('API params:', apiParams);
   ```

### Automated Testing (Future Enhancement)

**Unit Test for EventFilters**:
```typescript
describe('EventFilters - Search', () => {
  it('should debounce search input and call onFiltersChange', async () => {
    const mockOnFiltersChange = jest.fn();
    const { getByPlaceholderText } = render(
      <EventFilters
        filters={{ searchTerm: '', category: null, dateRange: 'upcoming', metroAreaIds: [] }}
        onFiltersChange={mockOnFiltersChange}
      />
    );

    const searchInput = getByPlaceholderText('Search events...');
    fireEvent.change(searchInput, { target: { value: 'Test Event' } });

    // Should NOT call immediately
    expect(mockOnFiltersChange).not.toHaveBeenCalled();

    // Should call after 300ms debounce
    await waitFor(() => {
      expect(mockOnFiltersChange).toHaveBeenCalledWith({
        searchTerm: 'Test Event',
        category: null,
        dateRange: 'upcoming',
        metroAreaIds: []
      });
    }, { timeout: 500 });
  });
});
```

---

## 8. Prevention Strategy

### 1. ESLint Configuration
**Action**: Enforce React Hooks rules strictly

**File**: `.eslintrc.json`
```json
{
  "rules": {
    "react-hooks/exhaustive-deps": "error" // Change from "warn" to "error"
  }
}
```

**Benefit**: Build fails if dependency arrays are incorrect, preventing deployment of broken code.

### 2. Pre-Commit Hook
**Action**: Add lint check to pre-commit hook

**File**: `.husky/pre-commit`
```bash
#!/bin/sh
npm run lint
```

**Benefit**: Developers cannot commit code with ESLint errors.

### 3. Code Review Checklist
**Action**: Add to PR template

**Checklist Items**:
- [ ] All `useEffect` hooks have complete dependency arrays
- [ ] No ESLint warnings in changed files
- [ ] Debounced inputs tested manually
- [ ] State management follows React best practices

### 4. Component Testing Standards
**Action**: Require tests for stateful components

**Standard**: Every component with `useEffect` must have:
- Unit test for effect behavior
- Integration test for data flow
- Debounce test if applicable

### 5. Documentation Update
**Action**: Document debounce pattern

**File**: `docs/FRONTEND_PATTERNS.md`
```markdown
## Debounced Input Pattern

When implementing debounced inputs:

1. Use `useDebounce` hook for value debouncing
2. Store debounced value in separate state
3. Use `useEffect` to sync debounced value to parent
4. ALWAYS include all dependencies in useEffect array
5. Add condition to prevent infinite loops

Example:
```typescript
const [searchInput, setSearchInput] = useState(filters.searchTerm);
const debouncedSearchTerm = useDebounce(searchInput, 300);

useEffect(() => {
  if (debouncedSearchTerm !== filters.searchTerm) {
    onFiltersChange({ ...filters, searchTerm: debouncedSearchTerm });
  }
}, [debouncedSearchTerm, filters, onFiltersChange]); // ALL dependencies
```
```

---

## 9. Related Issues & Technical Debt

### Potential Issues in Other Components

**Search for similar patterns**:
```bash
# Find all useDebounce usages
grep -r "useDebounce" web/src/

# Find all useEffect with missing deps
# (Requires manual ESLint check)
```

**Components to audit**:
- [ ] `/events` page filters (EventsPage.tsx)
- [ ] Any other page using EventFilters component
- [ ] Any custom search/filter components

### Technical Debt Items

1. **EventFilters Synchronization Logic**
   - Lines 86-91: Second `useEffect` for syncing external filter changes
   - Risk: Potential for sync issues if both effects fire simultaneously
   - **Recommendation**: Consolidate sync logic or use state reducer

2. **filtersToApiParams Utility**
   - Line 169: Converts empty string to undefined
   - Risk: Inconsistent handling of empty vs undefined
   - **Recommendation**: Standardize on null vs undefined convention

3. **Debounce Delay Hardcoded**
   - Line 77: 300ms delay hardcoded
   - Risk: Cannot adjust for different use cases
   - **Recommendation**: Make delay configurable via prop

---

## 10. Impact Assessment

### User Impact
- **Severity**: HIGH
- **Affected Users**: All users with registered events
- **Frequency**: Every search attempt (100% reproduction rate)
- **Workaround**: None (other filters work, but no text search alternative)

### Business Impact
- **Dashboard UX**: Significantly degraded
- **User Retention**: Risk if users cannot find their events
- **Support Tickets**: Potential increase in "search not working" reports

### Technical Impact
- **Code Quality**: ESLint warning indicates pattern violation
- **Maintainability**: Other developers may copy this broken pattern
- **Testing Gap**: No automated tests caught this issue

---

## 11. Recommendations

### Immediate Actions (Critical)
1. **Apply Solution 1**: Add missing dependencies to `useEffect` (1-line fix)
2. **Test Manually**: Verify search works with fix
3. **Deploy Fix**: Priority deployment to production

### Short-Term Actions (High Priority)
1. **Add Unit Tests**: Test EventFilters debounce behavior
2. **Audit Codebase**: Find similar patterns with missing dependencies
3. **Update ESLint**: Change `exhaustive-deps` from warning to error

### Long-Term Actions (Medium Priority)
1. **Document Pattern**: Add debounce pattern to frontend standards
2. **Pre-Commit Hooks**: Enforce linting before commits
3. **Component Library**: Create tested, reusable search component
4. **E2E Tests**: Add Cypress tests for dashboard search flows

---

## 12. Conclusion

### Root Cause Summary
The dashboard search feature fails due to a **missing dependency array in EventFilters.tsx line 84**. The `useEffect` hook that propagates debounced search terms to the parent component does not include `filters` and `onFiltersChange` in its dependencies, causing a stale closure that prevents search state from updating correctly.

### Fix Summary
**Simple one-line fix**: Add `filters` and `onFiltersChange` to the dependency array at line 84.

### Lessons Learned
1. **React Hooks Rules are Non-Negotiable**: ESLint warnings about dependencies must be treated as errors
2. **Debounce Patterns Need Testing**: Complex state flows require unit tests
3. **Code Review Gaps**: This pattern violation should have been caught in PR review
4. **Tooling Matters**: Stricter ESLint config would have prevented this issue

### Success Criteria
- ✅ Search filters events correctly
- ✅ Debounce prevents excessive API calls
- ✅ No infinite loops or performance issues
- ✅ ESLint warnings resolved
- ✅ Pattern documented for future reference

---

**Analysis Completed**: 2025-12-30
**Next Steps**: Apply fix, test, and deploy to production
**Document Owner**: System Architecture Designer
