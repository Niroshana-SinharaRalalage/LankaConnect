# Root Cause Analysis: Issue #47 - Email Groups Cross-User Visibility

**Issue:** One event organizer can see email groups of other event organizers
**Condition:** This only happens when the organizer has NO email groups of their own
**Behavior:** When trying to USE those email groups, they get an error (correctly blocked)

---

## Investigation Summary

### Files Investigated

#### Backend (API Layer)
1. **`c:\Work\LankaConnect\src\LankaConnect.API\Controllers\EmailGroupsController.cs`**
   - Endpoint: `GET /api/emailgroups?includeAll={false|true}`
   - Authorization: `[Authorize(Roles = "EventOrganizer,Admin,AdminManager")]`
   - Controller passes `includeAll` parameter to query

2. **`c:\Work\LankaConnect\src\LankaConnect.Application\Communications\Queries\GetEmailGroups\GetEmailGroupsQueryHandler.cs`**
   - Line 54: `if (isAdmin && request.IncludeAll)` - Guards `GetAllActiveAsync()`
   - Line 65: `GetByOwnerAsync(userId)` - Filters by current user's ID
   - Uses `ICurrentUserService.UserId` for filtering

3. **`c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Data\Repositories\EmailGroupRepository.cs`**
   - Line 42-44: `WHERE g.OwnerId == ownerId AND g.IsActive`
   - Correctly filters email groups by owner ID

4. **`c:\Work\LankaConnect\src\LankaConnect.Infrastructure\Security\Services\CurrentUserService.cs`**
   - Line 24: Uses `ClaimTypes.NameIdentifier` to get user ID from JWT
   - Line 47-54: `IsAdmin` checks for "Admin" or "AdminManager" roles

5. **`c:\Work\LankaConnect\src\LankaConnect.Application\Events\Commands\CreateEvent\CreateEventCommandHandler.cs`**
   - Line 346-358: Validates email group ownership before assignment
   - Returns error if organizer tries to use another organizer's email group

#### Frontend (React/Next.js)
6. **`c:\Work\LankaConnect\web\src\presentation\components\features\events\EventCreationForm.tsx`**
   - Line 42: `useEmailGroups()` - Fetches email groups for dropdown
   - Line 915-927: Renders MultiSelect with email groups

7. **`c:\Work\LankaConnect\web\src\presentation\hooks\useEmailGroups.ts`**
   - Lines 33-43: Query keys include `userId` for cache isolation (Issue #47 fix attempt)
   - Line 73: `queryKey: emailGroupKeys.list(includeAll, userId)`
   - Line 74: `queryFn: () => emailGroupsRepository.getEmailGroups(includeAll)`
   - Line 78: `enabled: !!userId` - Disabled when user not authenticated

8. **`c:\Work\LankaConnect\web\src\infrastructure\api\repositories\email-groups.repository.ts`**
   - Line 21-25: API call to `/emailgroups?includeAll={false}`

9. **`c:\Work\LankaConnect\web\src\presentation\store\useAuthStore.ts`**
   - Zustand store with persistence for auth state
   - Complex hydration logic (lines 132-241)

10. **`c:\Work\LankaConnect\web\src\presentation\components\layout\Header.tsx`**
    - Lines 283-285: `queryClient.clear()` on logout (Issue #47 fix attempt)

---

## Root Cause Analysis

### Backend Analysis: CORRECT
The backend implementation is **correctly** filtering email groups by owner:

```csharp
// GetEmailGroupsQueryHandler.cs - Line 54-65
if (isAdmin && request.IncludeAll)
{
    // Admin-only path - BOTH conditions required
    emailGroups = await _emailGroupRepository.GetAllActiveAsync(cancellationToken);
}
else
{
    // All other users - filtered by their own ID
    emailGroups = await _emailGroupRepository.GetByOwnerAsync(userId, cancellationToken);
}
```

The `isAdmin` flag is correctly derived from JWT role claims, and `GetByOwnerAsync` properly filters by `OwnerId`.

### Frontend Analysis: POTENTIAL ISSUE IDENTIFIED

The frontend has TWO fixes attempted for Issue #47:

1. **Cache clearing on logout** (Header.tsx line 285):
   ```typescript
   queryClient.clear();
   ```

2. **User ID in query key** (useEmailGroups.ts lines 39-40, 73):
   ```typescript
   list: (includeAll: boolean, userId: string | null) =>
     [...emailGroupKeys.lists(), { includeAll, userId }] as const,

   queryKey: emailGroupKeys.list(includeAll, userId),
   ```

### Identified Issue: Race Condition During Hydration

The bug likely occurs due to a **race condition** during Zustand store hydration:

1. **Scenario:**
   - User A logs in and views email groups (their data is fetched)
   - User A logs out (cache is cleared)
   - User B logs in
   - **During hydration**, before the auth store fully loads User B's data:
     - `userId` in `useEmailGroups` hook is temporarily `null`
     - When `userId` becomes available, React Query might use stale backend response

2. **The "No Groups" Condition:**
   - When an organizer has NO groups, the backend returns `[]`
   - If User B (no groups) logs in after User A (has groups), and there's a hydration timing issue:
     - The query might fire before the correct JWT token is set in API client
     - Or cached responses from a previous session leak through

3. **Why the fix is incomplete:**
   - The `enabled: !!userId` check only prevents fetching when `userId` is `null`
   - But `userId` can be set before the auth token is fully propagated to the API client
   - The `queryClient.clear()` on logout helps, but doesn't handle all edge cases

---

## Evidence of Attempted Fix

The code contains comments referencing "Phase 6A.X Issue #47":

```typescript
// useEmailGroups.ts - Lines 33, 37-38, 67-68
/**
 * Phase 6A.X Issue #47: Added userId to list query key to prevent cache collision between users
 */
// Phase 6A.X Issue #47: Include userId in query key to ensure each user gets their own cache
// This prevents User B from seeing User A's cached email groups after login/logout
```

```typescript
// Header.tsx - Line 283
// Phase 6A.X Issue #47: Clear React Query cache on logout to prevent
// data leakage between users (e.g., email groups visibility)
```

These fixes address **frontend caching** but may not fully resolve the issue if the problem is **hydration timing**.

---

## Recommended Fix Approach

### Option 1: Wait for Full Hydration (Recommended)
Ensure the query only runs after both:
1. Auth store is hydrated (`isHydrated === true`)
2. User ID is available (`userId !== null`)
3. Auth token is set in API client

```typescript
// In useEmailGroups.ts
export function useEmailGroups(includeAll: boolean = false, options?: ...) {
  const user = useAuthStore((state) => state.user);
  const isHydrated = useAuthStore((state) => state._hasHydrated);
  const userId = user?.userId ?? null;

  return useQuery({
    queryKey: emailGroupKeys.list(includeAll, userId),
    queryFn: () => emailGroupsRepository.getEmailGroups(includeAll),
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: false,
    // CRITICAL: Wait for FULL hydration before fetching
    enabled: !!userId && isHydrated,
    ...options,
  });
}
```

### Option 2: Clear Cache on Login (Additional Safety)
Add `queryClient.clear()` when a user logs in, not just logout:

```typescript
// In LoginForm.tsx - after successful login
const queryClient = useQueryClient();

const onSubmit = async (data: LoginFormData) => {
  try {
    // ... login logic ...

    // Clear any stale cache from previous sessions
    queryClient.clear();

    setAuth(response.user, tokens);
    router.push('/');
  } catch (error) {
    // ...
  }
};
```

### Option 3: Backend Validation (Defense in Depth)
The backend ALREADY validates email group ownership in `CreateEventCommandHandler` (lines 346-358). This is correct and should remain as defense-in-depth.

---

## Affected Components

| Component | File Path | Impact |
|-----------|-----------|--------|
| API Controller | `src/LankaConnect.API/Controllers/EmailGroupsController.cs` | Low (correctly implemented) |
| Query Handler | `src/LankaConnect.Application/Communications/Queries/GetEmailGroups/GetEmailGroupsQueryHandler.cs` | Low (correctly implemented) |
| Repository | `src/LankaConnect.Infrastructure/Data/Repositories/EmailGroupRepository.cs` | Low (correctly implemented) |
| React Hook | `web/src/presentation/hooks/useEmailGroups.ts` | **HIGH - needs fix** |
| Event Form | `web/src/presentation/components/features/events/EventCreationForm.tsx` | Medium (consumer of hook) |
| Auth Store | `web/src/presentation/store/useAuthStore.ts` | Medium (hydration timing) |
| Login Form | `web/src/app/(auth)/login/page.tsx` | Medium (may need cache clear) |

---

## Conclusion

**Root Cause:** React Query cache timing issue during Zustand auth store hydration. The existing fix (Issue #47) partially addresses the problem by adding `userId` to query keys and clearing cache on logout, but doesn't fully account for hydration race conditions.

**Fix Type:** Frontend (React/React Query)

**Severity:** Medium - Users see incorrect data but cannot USE it (backend correctly blocks unauthorized access)

**Recommended Action:**
1. Add `isHydrated` check to `useEmailGroups` hook `enabled` condition
2. Clear React Query cache on login (in addition to logout)
3. No backend changes required (already correct)
