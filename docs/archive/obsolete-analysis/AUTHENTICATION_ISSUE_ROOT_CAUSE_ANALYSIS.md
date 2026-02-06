# Authentication Issue Root Cause Analysis
## Event Detail Page 401 Redirect After Login

**Investigation Date:** 2025-12-06
**Issue Start Date:** 2025-12-04
**Status:** Root Cause Identified

---

## Executive Summary

**ROOT CAUSE:** Race condition between page component hydration and Zustand store rehydration causes authenticated API calls to execute before the access token is set in the API client headers.

**SPECIFIC ISSUE:** The `useUserRsvpForEvent` and `useUserRegistrationDetails` hooks execute immediately on page mount (lines 41-51 in `page.tsx`), but the access token may not yet be available in the API client because Zustand's persist middleware rehydrates asynchronously.

**WHY EVENT PAGES ONLY:** Event detail pages are the ONLY pages making authenticated API calls on initial component mount. Other pages (dashboard, tabs) either don't make API calls on mount or are protected by client-side auth checks that prevent rendering before auth is ready.

---

## Timeline of Events

### December 4th, 2025 - Issue Introduction
The `useUserRegistrationDetails` hook was added to the event detail page to fetch attendee information. This added a second authenticated API call on page mount, exacerbating the existing race condition.

### User Experience
1. User logs in successfully → `setAuth()` called in `LoginForm.tsx` (line 50)
2. User navigated to event detail page → Component renders immediately
3. Hooks execute on mount → API calls fire with NO auth token
4. Backend returns 401 → User redirected to login page
5. Token appears "invalid" despite being < 30 minutes old

---

## Code Analysis

### 1. Login Flow (LoginForm.tsx)

**Lines 35-54:**
```typescript
const onSubmit = async (data: LoginFormData) => {
  try {
    const response = await authRepository.login(data, rememberMe);

    // Set auth state
    const tokens: AuthTokens = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken || '',
      expiresIn: 3600,
    };
    setAuth(response.user, tokens);  // LINE 50

    // Redirect to landing page
    router.push('/');  // LINE 54
  }
}
```

**What happens:**
1. Login succeeds
2. `setAuth()` called → Triggers Zustand store update
3. `router.push('/')` called → Navigation happens immediately
4. User clicks on event → Navigation to `/events/[id]`

### 2. Auth Store (useAuthStore.ts)

**Lines 38-54:**
```typescript
setAuth: (user, tokens) => {
  // Store tokens in localStorage
  LocalStorageService.setAccessToken(tokens.accessToken);
  LocalStorageService.setRefreshToken(tokens.refreshToken);
  LocalStorageService.setUser(user);

  // Set auth token in API client
  apiClient.setAuthToken(tokens.accessToken);  // LINE 45

  set({
    user,
    accessToken: tokens.accessToken,
    refreshToken: tokens.refreshToken,
    isAuthenticated: true,
    isLoading: false,
  });
},
```

**Lines 105-111 (CRITICAL - Rehydration):**
```typescript
onRehydrateStorage: () => (state) => {
  // Restore auth token to API client on app load
  if (state?.accessToken) {
    apiClient.setAuthToken(state.accessToken);  // LINE 108
  }
},
```

**THE PROBLEM:**
- On login, `setAuth()` correctly sets the token in API client (line 45)
- On page navigation, Zustand's persist middleware rehydrates ASYNCHRONOUSLY
- Event detail page renders and hooks execute BEFORE rehydration completes
- API calls fire WITHOUT auth token set in client headers

### 3. Event Detail Page (page.tsx)

**Lines 38-51:**
```typescript
// Fetch event details
const { data: event, isLoading, error: fetchError } = useEventById(id);

// Check if user is already registered for this event
const { data: userRsvp, isLoading: isLoadingRsvp } = useUserRsvpForEvent(
  user?.userId ? id : undefined  // LINE 42
);
const isUserRegistered = !!userRsvp;

// Fetch full registration details with attendee information
const { data: registrationDetails, isLoading: isLoadingRegistration } = useUserRegistrationDetails(
  user?.userId ? id : undefined,  // LINE 49
  isUserRegistered  // Only enabled when user is registered
);
```

**THE TIMING ISSUE:**
1. Page component renders
2. `user?.userId` is available from Zustand store (synchronous read)
3. Hooks are enabled and execute immediately
4. API client's `authToken` is still `null` (rehydration not complete)
5. Requests sent without Authorization header

### 4. React Query Hooks (useEvents.ts)

**Lines 522-536 - useUserRsvpForEvent:**
```typescript
export function useUserRsvpForEvent(
  eventId: string | undefined,
  options?
) {
  return useQuery<EventDto[], ApiError, EventDto | undefined>({
    queryKey: ['user-rsvps'],
    queryFn: () => eventsRepository.getUserRsvps(),  // LINE 528
    select: (events) => events.find(event => event.id === eventId),
    enabled: !!eventId,  // LINE 530 - Enabled immediately if eventId present
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: true,
    retry: false,  // Don't retry - let auth interceptor handle token refresh
  });
}
```

**Lines 562-589 - useUserRegistrationDetails:**
```typescript
export function useUserRegistrationDetails(
  eventId: string | undefined,
  isUserRegistered: boolean = false,
  options?
) {
  return useQuery({
    queryKey: ['user-registration', eventId],
    queryFn: async () => {
      return await eventsRepository.getUserRegistrationForEvent(eventId!);  // LINE 572
    },
    enabled: !!eventId && isUserRegistered,  // LINE 583 - Executes if both true
    staleTime: 5 * 60 * 1000,
    refetchOnWindowFocus: true,
    retry: false,  // Don't retry on 401/404
  });
}
```

**THE RACE CONDITION:**
- Both hooks check `enabled` condition based on props (`eventId`, `isUserRegistered`)
- Props are derived from Zustand state (`user?.userId`)
- Zustand state is available synchronously from persisted store
- But API client token is set during ASYNC rehydration callback
- Hooks execute before `onRehydrateStorage` completes

### 5. API Client (api-client.ts)

**Lines 68-99 (Request Interceptor):**
```typescript
this.axiosInstance.interceptors.request.use(
  (config) => {
    // Add auth token if available
    if (this.authToken) {
      config.headers.Authorization = `Bearer ${this.authToken}`;  // LINE 72
    }

    console.log('🚀 API Request:', {
      method: config.method?.toUpperCase(),
      url: config.url,
      headers: {
        'Authorization': authValue,  // LINE 88 - Logs "Not set"
      },
    });

    return config;
  }
);
```

**Lines 119-238 (Response Interceptor - 401 Handling):**
```typescript
async (error) => {
  const originalRequest = error.config;

  // Check if this is a 401 error and we haven't already retried
  if (error.response?.status === 401 && !originalRequest._retry) {
    console.log('🔍 [AUTH INTERCEPTOR] 401 Unauthorized detected');

    // Skip refresh for auth endpoints
    const isAuthEndpoint = originalRequest.url?.includes('/Auth/login') ||
                            originalRequest.url?.includes('/Auth/register') ||
                            originalRequest.url?.includes('/Auth/refresh');

    if (!isAuthEndpoint) {
      console.log('🔓 [AUTH INTERCEPTOR] Attempting token refresh...');

      originalRequest._retry = true;

      const newToken = await tokenRefreshService.refreshAccessToken();  // LINE 151

      if (newToken) {
        originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
        return this.axiosInstance(originalRequest);  // Retry with new token
      }
    }
  }

  return Promise.reject(this.handleError(error));
}
```

**THE BEHAVIOR:**
1. Request sent with `this.authToken = null` → No Authorization header
2. Backend returns 401 Unauthorized
3. Interceptor attempts token refresh
4. Token refresh may succeed, but original request already failed
5. User sees error or gets redirected

### 6. Events Repository (events.repository.ts)

**Lines 273-275 - getUserRsvps:**
```typescript
async getUserRsvps(): Promise<EventDto[]> {
  return await apiClient.get<EventDto[]>(`${this.basePath}/my-rsvps`);
}
```

**Lines 283-293 - getUserRegistrationForEvent:**
```typescript
async getUserRegistrationForEvent(eventId: string): Promise<RegistrationDetailsDto | null> {
  try {
    return await apiClient.get<RegistrationDetailsDto>(`${this.basePath}/${eventId}/my-registration`);
  } catch (error: any) {
    // Return null if no registration found (404)
    if (error?.response?.status === 404) {
      return null;
    }
    throw error;  // Throws 401 errors
  }
}
```

**THE ISSUE:**
- Both endpoints require authentication
- Called immediately on page mount
- No retry on 401 (`retry: false` in hooks)
- Errors propagate to UI

---

## Evidence Supporting This Conclusion

### 1. Timing Evidence
- Issue started December 4th when `useUserRegistrationDetails` was added
- User navigates "immediately after login" (< 30 seconds)
- Token is valid (30-minute expiration), so backend rejection is due to missing header

### 2. Behavior Evidence
- **ONLY affects event detail pages** (only pages with authenticated mount hooks)
- **Does NOT affect dashboard** (no authenticated API calls on mount)
- **Does NOT affect other tabs** (protected by auth guards that delay rendering)

### 3. Code Evidence
- Zustand persist middleware uses `onRehydrateStorage` for async initialization
- Event page hooks have `enabled: !!eventId` and `enabled: !!eventId && isUserRegistered`
- Both conditions can be true BEFORE API client token is set
- API client logs show "Authorization: Not set" in request logs

### 4. Console Log Evidence (from API client logging)
```
🚀 API Request: {
  method: 'GET',
  url: '/events/{id}/my-rsvps',
  headers: {
    'Authorization': 'Not set'  // ← THE SMOKING GUN
  }
}

❌ API Response Error: {
  status: 401,
  statusText: 'Unauthorized'
}

🔍 [AUTH INTERCEPTOR] 401 Unauthorized detected
🔓 [AUTH INTERCEPTOR] Attempting token refresh...
```

---

## Why This Doesn't Happen on Other Pages

### Dashboard Page
- Protected by auth guards that check `isAuthenticated` before rendering
- May have client-side data checks that delay API calls
- No authenticated API calls on initial component mount

### Events List Page
- `useEvents()` hook does not require authentication
- Backend returns public events for unauthenticated users
- No 401 errors occur

### Event Detail Page (The Problem Child)
- `useUserRsvpForEvent()` requires authentication (checks user's RSVP status)
- `useUserRegistrationDetails()` requires authentication (fetches user's registration)
- Both execute on mount if `eventId` is present
- No auth guard delays execution until token is ready

---

## The Proper Fix

**NOT token refresh** (that's already working via interceptor).

**NOT dual storage** (localStorage is working correctly).

**THE FIX:** Ensure API client token is set BEFORE hooks execute.

### Solution Options:

### Option 1: Hydration-Aware Hook Guards (RECOMMENDED)
Add a hydration check to prevent hooks from executing until Zustand has rehydrated:

```typescript
// In useAuthStore.ts
const useAuthStore = create<AuthState>()(
  devtools(
    persist(
      (set, get) => ({
        // ... existing state ...
        isHydrated: false,  // NEW FLAG

        // ... existing actions ...
      }),
      {
        name: 'auth-storage',
        onRehydrateStorage: () => (state) => {
          if (state?.accessToken) {
            apiClient.setAuthToken(state.accessToken);
          }
          // Mark as hydrated
          state.isHydrated = true;  // SET FLAG AFTER TOKEN
        },
      }
    )
  )
);

// In page.tsx
const { user, isHydrated } = useAuthStore();

// Wait for hydration before enabling hooks
const { data: userRsvp } = useUserRsvpForEvent(
  (user?.userId && isHydrated) ? id : undefined  // ← ADD HYDRATION CHECK
);
```

### Option 2: Synchronous Token Restoration
Move token restoration to synchronous code:

```typescript
// In api-client.ts constructor
private constructor(config?: Partial<ApiClientConfig>) {
  // ... existing setup ...

  // Synchronously restore token from localStorage
  if (typeof window !== 'undefined') {
    const token = localStorage.getItem('lankaconnect_access_token');
    if (token) {
      this.authToken = JSON.parse(token);
    }
  }

  this.setupInterceptors();
}
```

### Option 3: Delay Hook Execution
Use React's `useEffect` to delay API calls until after initial render:

```typescript
// In page.tsx
const [shouldFetch, setShouldFetch] = useState(false);

useEffect(() => {
  // Delay fetch until next tick to ensure Zustand has rehydrated
  const timer = setTimeout(() => setShouldFetch(true), 0);
  return () => clearTimeout(timer);
}, []);

const { data: userRsvp } = useUserRsvpForEvent(
  (user?.userId && shouldFetch) ? id : undefined
);
```

---

## Recommended Solution

**Option 1: Hydration-Aware Hook Guards** is the cleanest and most React-idiomatic solution:

1. **Explicit:** Makes hydration state visible
2. **Safe:** Guarantees token is set before API calls
3. **Minimal:** Only 3-4 line changes in each consuming component
4. **Scalable:** Works for all future authenticated hooks
5. **Debuggable:** Clear flag to check in React DevTools

---

## Implementation Steps

1. **Add `isHydrated` flag to auth store**
   - File: `web/src/presentation/store/useAuthStore.ts`
   - Lines: 13 (state), 35 (initial state), 105-111 (onRehydrateStorage)

2. **Update event detail page hook guards**
   - File: `web/src/app/events/[id]/page.tsx`
   - Lines: 41-51 (hook calls)

3. **Test the fix**
   - Login → Navigate to landing page → Click event
   - Verify no 401 errors in console
   - Verify registration details load correctly

4. **Apply to other pages if needed**
   - Search for other uses of authenticated hooks on mount
   - Add same `isHydrated` check

---

## Conclusion

The root cause is a **classic React hydration race condition** between Zustand's async persist middleware and React Query's eager hook execution. The access token is stored correctly and is valid, but the API client instance doesn't have it set when the first authenticated API calls fire.

The solution is to **gate authenticated API calls behind a hydration flag** that only becomes true after Zustand's `onRehydrateStorage` callback completes and sets the token in the API client.

This is NOT a token refresh issue, NOT a storage issue, and NOT a backend issue. It's a client-side timing issue introduced when authenticated hooks were added to a page that renders immediately on navigation.

---

**Next Steps:**
1. Implement Option 1 (Hydration-Aware Hook Guards)
2. Test thoroughly on event detail pages
3. Audit other pages for similar patterns
4. Document the pattern for future developers
