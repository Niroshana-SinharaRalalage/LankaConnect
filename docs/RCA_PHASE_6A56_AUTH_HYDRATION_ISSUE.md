# Root Cause Analysis: Authentication State Inconsistency Between Pages

**Phase**: 6A.56
**Date**: 2025-12-30
**Issue**: User appears logged in on events list page but logged out on event detail page
**Severity**: HIGH - Critical UX issue affecting user registration flow
**Category**: UI + Auth Issue (Not Backend, Not Database)

---

## Problem Statement

Users experience inconsistent authentication state when navigating from events list page to event detail page:

1. **Events List Page** (`/events`): User shows as logged in, sees "You are registered" badges
2. **Event Detail Page** (`/events/{id}`): User appears logged out, shows "Register for this Event" form
3. **Behavior**: Registration status "flips" on page navigation/refresh

This breaks user trust and creates confusion about their registration status.

---

## User Report

> "Cannot verify a different issue:
> 1. Logged in as niroshhh@gmail.com user and I can see I have already registered for /events/0458806b-8672-4ad5-a7cb-f5346f1b282a.
> 2. When navigate to that event, it says I am not registered mainly because I was logged out
>
> Please check whether this is an UI issue, Auth Issue, Backend API issue, a Database issue or a feature missing case."

---

## Root Cause Analysis

### Issue Category: **UI + Auth Hydration Timing Issue**

This is **NOT**:
- ❌ Backend API issue (APIs work correctly)
- ❌ Database issue (data is correct)
- ❌ Feature missing (features exist)

This **IS**:
- ✅ **Auth hydration timing issue** in UI components
- ✅ **Inconsistent auth check patterns** between pages
- ✅ **Race condition** between Zustand hydration and React Query execution

---

## Technical Deep Dive

### Component Behavior Comparison

#### Events List Page (`web/src/app/events/page.tsx`)

```typescript
// Line 40: Simple auth check
const { user } = useAuthStore();

// Line 82: Registration check - NO HYDRATION DEPENDENCY
const { data: userRsvps } = useUserRsvps({ enabled: !!user });

// Line 85-88: O(1) lookup using Set
const registeredEventIds = useMemo(
  () => new Set(userRsvps?.map(e => e.id) || []),
  [userRsvps]
);
```

**Behavior**: Query executes immediately if `user` exists in store (even if not fully hydrated).

---

#### Event Detail Page (`web/src/app/events/[id]/page.tsx`)

```typescript
// Line 62: Auth check WITH HYDRATION FLAG
const { user, _hasHydrated } = useAuthStore();

// Line 83-85: Registration check - DEPENDS ON HYDRATION
const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
  (user?.userId && _hasHydrated) ? id : undefined  // ⚠️ CRITICAL CONDITION
);

// Line 90-93: Registration details check - DEPENDS ON HYDRATION
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
  (user?.userId && _hasHydrated) ? id : undefined,
  !!userRsvp
);

// Line 96: Final registration check
const isUserRegistered = !!userRsvp && registrationDetails?.status !== RegistrationStatus.Cancelled;
```

**Behavior**: Queries are **DISABLED** until both `user?.userId` AND `_hasHydrated` are true.

---

### Auth Store Hydration Process

From `web/src/presentation/store/useAuthStore.ts`:

```typescript
// Lines 40, 132-143: Hydration lifecycle
_hasHydrated: false,  // Initial state

onRehydrateStorage: () => (state) => {
  console.log('🔄 [AUTH STORE] Rehydration complete');

  // Restore auth token to API client
  if (state?.accessToken) {
    console.log('✅ [AUTH STORE] Restoring auth token to API client');
    apiClient.setAuthToken(state.accessToken);
  }

  // Mark as hydrated AFTER token restoration
  state?.setHasHydrated(true);
},
```

**Timeline**:
1. Page loads → `_hasHydrated = false`
2. Zustand reads from localStorage (async)
3. Zustand sets `user`, `accessToken`, `refreshToken`
4. Zustand calls `onRehydrateStorage` callback
5. API client gets auth token
6. **FINALLY**: `_hasHydrated = true`

---

### The Race Condition

**Scenario**: User navigates from `/events` to `/events/{id}`

1. React renders event detail page
2. `useAuthStore()` returns:
   - `user` = `{ userId: "...", ... }` ✅ (exists in localStorage)
   - `_hasHydrated` = `false` ⚠️ (still hydrating)
3. Conditional check: `(user?.userId && _hasHydrated)` = `false`
4. React Query queries are **DISABLED**
5. Page shows "not registered" state
6. 50-200ms later: `_hasHydrated` becomes `true`
7. Queries activate, but **React may not re-render immediately**

Result: User sees incorrect "logged out" state until a re-render triggers.

---

### Why Events List Page Works

The events list page **does NOT depend on `_hasHydrated`**:

```typescript
const { data: userRsvps } = useUserRsvps({ enabled: !!user });
```

This query activates as soon as `user` exists, even if hydration isn't complete. The API client already has the token from a previous page load, so the request succeeds.

---

## Impact Assessment

### User Impact: **HIGH**

- **Trust Issue**: Users think their registration disappeared
- **Confusion**: Registration status appears to "flip" randomly
- **Duplicate Registrations**: Users might re-register thinking they weren't registered
- **Support Burden**: Increased support tickets about "lost registrations"

### Affected User Journeys:

1. **Navigate from events list → event detail** (most common)
2. **Direct URL to event detail** (less common, works if already visited)
3. **Page refresh on event detail** (hydration race condition)

### Data Integrity: **SAFE**

- ✅ Database has correct registration data
- ✅ Backend APIs return correct responses
- ✅ Only UI display is affected

---

## Why This Wasn't Caught Earlier

1. **Inconsistent Patterns**: Two different auth check approaches in codebase
2. **Fast Hydration**: On fast machines, hydration completes before first render
3. **Testing Blind Spot**: Automated tests likely use `waitFor` which hides timing issues
4. **Developer Cache**: Developers frequently visit both pages, so token is already in API client

---

## Verification Test Plan

### Test 1: Reproduce the Issue

```bash
# 1. Clear all browser storage
localStorage.clear()
sessionStorage.clear()

# 2. Login as niroshhh@gmail.com

# 3. Navigate to /events
# ✅ Should see "You are registered" badges

# 4. Click on a registered event
# ❌ BUG: Shows "Register for this Event" (not registered)

# 5. Wait 1-2 seconds OR refresh page
# ✅ Now shows "You're Registered!"
```

### Test 2: Verify Auth State

```javascript
// In browser console on /events/{id}
const state = useAuthStore.getState()
console.log('user:', state.user)
console.log('_hasHydrated:', state._hasHydrated)
console.log('accessToken:', state.accessToken ? 'EXISTS' : 'NULL')
```

Expected on initial load:
- `user`: Object (exists)
- `_hasHydrated`: `false` ⚠️
- `accessToken`: String (exists)

After 200ms:
- `user`: Object (exists)
- `_hasHydrated`: `true` ✅
- `accessToken`: String (exists)

---

## Fix Strategy

### Option 1: Remove Hydration Dependency (RECOMMENDED)

**Approach**: Make event detail page consistent with events list page

**Changes**:
```typescript
// Before
const { data: userRsvp } = useUserRsvpForEvent(
  (user?.userId && _hasHydrated) ? id : undefined
);

// After
const { data: userRsvp } = useUserRsvpForEvent(
  user?.userId ? id : undefined
);
```

**Pros**:
- ✅ Simple, minimal code change
- ✅ Consistent with events list page pattern
- ✅ No breaking changes to hooks
- ✅ Fixes all affected pages

**Cons**:
- ⚠️ Query may execute before token is in API client (but axios interceptor handles this)

---

### Option 2: Add Hydration Dependency to Events List Page

**Approach**: Make events list page wait for hydration

**Changes**:
```typescript
// Before
const { data: userRsvps } = useUserRsvps({ enabled: !!user });

// After
const { data: userRsvps } = useUserRsvps({
  enabled: !!(user && _hasHydrated)
});
```

**Pros**:
- ✅ More "correct" - waits for full auth restoration

**Cons**:
- ❌ Adds unnecessary delay to working page
- ❌ Doesn't solve the UX problem, just makes it consistent everywhere

---

### Option 3: Fix at Hook Level

**Approach**: Make `useUserRsvpForEvent` handle hydration internally

**Changes** in `web/src/presentation/hooks/useEvents.ts`:
```typescript
export function useUserRsvpForEvent(
  eventId: string | undefined,
  options?: ...
) {
  const _hasHydrated = useAuthStore((state) => state._hasHydrated);

  return useQuery<EventDto[], ApiError, EventDto | undefined>({
    queryKey: ['user-rsvps'],
    queryFn: () => eventsRepository.getUserRsvps(),
    select: (events) => events.find(event => event.id === eventId),
    enabled: !!eventId && _hasHydrated,  // Add hydration check internally
    ...options,
  });
}
```

**Pros**:
- ✅ Centralizes hydration logic
- ✅ Prevents future inconsistencies

**Cons**:
- ❌ Changes hook API contract
- ❌ May break other callers
- ❌ Still doesn't address the root problem

---

## Recommended Solution

### Hybrid Approach: Remove Dependency + Add Safety Net

**Step 1**: Remove `_hasHydrated` dependency from event detail page
**Step 2**: Add loading state handling while auth is settling
**Step 3**: Test auth token availability before query execution

**Implementation**:

```typescript
// web/src/app/events/[id]/page.tsx

const { user, _hasHydrated } = useAuthStore();

// Show loading state if auth is still hydrating
if (!_hasHydrated && user?.userId) {
  return <LoadingSpinner message="Verifying authentication..." />;
}

// Queries now execute immediately if user exists
const { data: userRsvp } = useUserRsvpForEvent(
  user?.userId ? id : undefined
);

const { data: registrationDetails } = useUserRegistrationDetails(
  user?.userId ? id : undefined,
  !!userRsvp
);
```

**Benefits**:
- ✅ Prevents "logged out" flash
- ✅ Shows intentional loading state
- ✅ Queries execute with proper auth token
- ✅ Consistent with events list page
- ✅ Clear UX during hydration

---

## Implementation Plan

### Phase 1: Fix Event Detail Page

1. ✅ Remove `_hasHydrated` dependency from `useUserRsvpForEvent` call
2. ✅ Remove `_hasHydrated` dependency from `useUserRegistrationDetails` call
3. ✅ Add loading state check at top of component
4. ✅ Test navigation from events list → event detail
5. ✅ Test direct URL access to event detail
6. ✅ Test page refresh on event detail

### Phase 2: Audit Other Pages

1. Search for all uses of `_hasHydrated` in codebase
2. Identify pages with similar inconsistencies
3. Apply same fix pattern
4. Document standard auth check pattern

### Phase 3: Testing

1. Manual testing on staging
2. Test with slow network (throttle to 3G)
3. Test with React DevTools to observe renders
4. Verify no auth token errors in console

### Phase 4: Documentation

1. Update PROGRESS_TRACKER.md
2. Create coding standard for auth checks
3. Add to component development checklist

---

## Files to Modify

### Primary Fix:
- `web/src/app/events/[id]/page.tsx` (lines 83-93, 96)

### Optional Improvements:
- `web/src/presentation/hooks/useEvents.ts` (add JSDoc warning about hydration)

### Documentation:
- `docs/PROGRESS_TRACKER.md` (add Phase 6A.56 entry)
- `docs/STREAMLINED_ACTION_PLAN.md` (update auth fix status)

---

## Acceptance Criteria

### Must Pass:

1. ✅ Navigate from `/events` to `/events/{id}` → Shows "You're Registered!" immediately
2. ✅ Direct URL to `/events/{id}` → Shows correct registration status
3. ✅ Refresh page on `/events/{id}` → No "flipping" between states
4. ✅ No console errors related to auth tokens
5. ✅ No API 401 errors during normal navigation
6. ✅ Loading state shows during hydration (if needed)
7. ✅ Works on slow networks (3G throttle)

### Should Pass:

1. ✅ Events list page still works correctly
2. ✅ Dashboard page shows correct auth state
3. ✅ Login/logout flow unaffected
4. ✅ Token refresh still works during API calls

---

## Rollback Plan

If fix causes regressions:

1. Revert commit
2. Add `_hasHydrated` back to affected queries
3. Investigate axios interceptor token handling
4. Consider Option 3 (hook-level fix) instead

---

## Future Prevention

### Code Review Checklist:
- [ ] Auth checks consistent across pages
- [ ] Hydration timing considered
- [ ] Loading states handle async auth
- [ ] No flash of incorrect auth state

### Linting Rule Ideas:
- Warn on `user?.userId && _hasHydrated` pattern
- Suggest `user?.userId` only (simpler)
- Flag inconsistent auth check patterns

---

## Related Issues

- Phase 6A.55: JSONB nullable enum materialization (backend issue - RESOLVED)
- Phase 6A.25: Free event registration not updating UI (cache invalidation - RESOLVED)
- Phase 6A.14: Edit registration modal (related to registration state)

---

## Conclusion

**This is a UI/Auth hydration timing issue**, not a backend or database problem. The fix is straightforward: remove the `_hasHydrated` dependency from event detail page auth checks to match the pattern used successfully in the events list page. This will resolve the "flipping" registration status and restore user trust in the application.

**Estimated Fix Time**: 30 minutes
**Estimated Test Time**: 1 hour
**Risk Level**: LOW (simple code change, well-understood problem)
**User Impact After Fix**: HIGH (immediate improvement in UX)
