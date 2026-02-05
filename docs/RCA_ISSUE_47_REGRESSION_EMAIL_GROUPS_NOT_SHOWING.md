# Root Cause Analysis: Issue #47 REGRESSION - Email Groups Not Showing

**Date**: 2026-02-04
**Severity**: CRITICAL - Complete loss of email group visibility
**Status**: REGRESSION caused by Issue #47 fix

---

## Executive Summary

After deploying the fix for Issue #47 (cross-user email group visibility), **ALL email groups stopped appearing** for ALL users. This is a critical regression affecting:
- Dashboard → Email Groups tab (shows "No Email Groups Yet")
- Create/Edit Event → Email Groups dropdown (shows "No options available")

**Root Cause**: The fix used a Zustand getter property (`isHydrated`) which **does not work** with Zustand selectors, causing the query to never execute.

**Data Status**: ✅ **NO DATA LOSS** - Backend API confirms 3 email groups exist and are returned correctly.

---

## 1. Root Cause Analysis

### What Was Changed (Issue #47 Fix)

File: `web/src/presentation/hooks/useEmailGroups.ts`

```typescript
// Line 73 - ADDED THIS LINE:
const isHydrated = useAuthStore((state) => state.isHydrated);

// Line 83 - CHANGED enabled condition:
enabled: !!userId && isHydrated,  // Added "&& isHydrated"
```

### Why It Broke

In `useAuthStore.ts`, `isHydrated` is defined as a **JavaScript getter**, not a regular property:

```typescript
// Lines 43-45 in useAuthStore.ts
get isHydrated() {
  return get()._hasHydrated;
}
```

**PROBLEM**: Zustand selectors receive a **plain object snapshot** of the state, NOT the store instance with getters.

When you do:
```typescript
const isHydrated = useAuthStore((state) => state.isHydrated);
```

What happens:
1. Zustand creates a snapshot of the state object
2. Getters are **NOT copied** to the snapshot (they exist only on the store instance)
3. `state.isHydrated` returns **`undefined`**
4. `enabled: !!userId && undefined` → evaluates to `false`
5. Query NEVER executes
6. **No email groups are fetched**

### Evidence

| Test | Result |
|------|--------|
| Backend API `GET /api/emailgroups` | ✅ Returns 3 email groups correctly |
| Database data | ✅ All email groups exist |
| Frontend with current fix | ❌ Shows "No Email Groups" |
| `state.isHydrated` in selector | Returns `undefined` (getter not accessible) |

---

## 2. Issue Classification

| Issue Type | Applies? | Explanation |
|------------|----------|-------------|
| **UI Issue** | ✅ YES | Email groups not rendering |
| **Auth Issue** | ✅ YES | Accessing auth store getter incorrectly |
| **Backend API Issue** | ❌ NO | API works correctly |
| **Database Issue** | ❌ NO | Data exists |
| **Feature Missing** | ❌ NO | Feature works, just broken by regression |

**Classification: Auth/State Management Issue causing UI Regression**

---

## 3. Recommended Fix

### Use the Existing `useHasHydrated` Selector

The codebase already has a working helper for this exact purpose:

```typescript
// Line 249 in useAuthStore.ts - EXISTING HELPER
export const useHasHydrated = () => useAuthStore((state) => state._hasHydrated);
```

This is used correctly in `ProtectedRoute.tsx`:
```typescript
import { useAuthStore, useHasHydrated } from '@/presentation/store/useAuthStore';
const hasHydrated = useHasHydrated();  // Works correctly!
```

### Fix Implementation

File: `web/src/presentation/hooks/useEmailGroups.ts`

**Change 1 - Update import (line 27):**
```typescript
// BEFORE:
import { useAuthStore } from '@/presentation/store/useAuthStore';

// AFTER:
import { useAuthStore, useHasHydrated } from '@/presentation/store/useAuthStore';
```

**Change 2 - Replace broken selector (line 71-73):**
```typescript
// BEFORE (BROKEN - getter doesn't work with selectors):
const isHydrated = useAuthStore((state) => state.isHydrated);

// AFTER (FIXED - uses the existing helper that accesses _hasHydrated):
const isHydrated = useHasHydrated();
```

---

## 4. Why This Fix Is Safe

| Consideration | Analysis |
|--------------|----------|
| **Pattern already proven** | `ProtectedRoute.tsx` uses `useHasHydrated()` successfully |
| **No store changes needed** | Uses existing helper, no modifications to auth store |
| **Backward compatible** | No interface changes |
| **Issue #47 still fixed** | The `enabled: !!userId && isHydrated` logic remains intact |

---

## 5. Test Plan

### 5.1 Verify Fix Works
1. Log in as test user
2. Navigate to Dashboard → Email Groups
3. Verify all 3 email groups appear
4. Navigate to Events → Create Event
5. Verify Email Groups dropdown shows the 3 groups

### 5.2 Verify Issue #47 Is Still Fixed (Cross-User Visibility)
1. Log in as User A (has email groups)
2. Verify User A sees their groups
3. Log out
4. Log in as User B (has no email groups OR different groups)
5. Verify User B does NOT see User A's groups

---

## 6. Files to Change

| File | Change |
|------|--------|
| `web/src/presentation/hooks/useEmailGroups.ts` | Update import, replace getter selector with `useHasHydrated()` |

---

## 7. Lessons Learned

1. **JavaScript getters and Zustand selectors do not mix** - Selectors receive plain object snapshots
2. **Follow existing patterns** - `ProtectedRoute` had the correct implementation
3. **Test after deployment** - The regression would have been caught with a quick manual test
4. **Code review should check for getter usage** - The original fix should have been compared against existing hydration patterns

---

## 8. Summary

| Item | Details |
|------|---------|
| **Root Cause** | Zustand selectors cannot access JavaScript getters; `state.isHydrated` returns `undefined` |
| **Fix** | Use existing `useHasHydrated()` helper instead of broken getter selector |
| **Files to Change** | 1 file: `useEmailGroups.ts` |
| **Risk Level** | Low - using an already-tested pattern |
| **Data Impact** | None - data was never lost |
