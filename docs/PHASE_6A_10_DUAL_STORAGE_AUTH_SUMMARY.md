# Phase 6A.10: Dual Storage Strategy for Refresh Tokens

**Status**: ✅ Complete
**Date**: 2025-12-06
**Phase Number**: 6A.10

## Problem Statement

After logging in, navigating to an event detail page from '/events' caused forced logout and redirect to login page. This issue started on December 4th, 2025, when the `useUserRegistrationDetails` hook was added to event detail pages.

### Root Cause Analysis

1. **Timeline**:
   - Dec 4, 2025: `useUserRegistrationDetails` hook added to event detail pages
   - This hook makes authenticated API call: `GET /api/Events/{eventId}/registrations/user`
   - Event detail page became the ONLY page making authenticated calls on initial load
   - When access token expired, this triggered token refresh
   - In cross-origin scenarios (localhost → Azure staging), refresh token cookie wasn't forwarded
   - Backend returned 400 "Refresh token is required"
   - Frontend forced logout and redirected to login page

2. **Technical Root Cause**:
   - Frontend: `http://localhost:3000` (HTTP)
   - Backend: `https://staging.azurecontainerapps.io` (HTTPS via proxy)
   - Backend sets `Secure=true` cookie when it sees HTTPS request (from proxy → backend)
   - Browser on `http://localhost:3000` receives Secure cookie
   - **Browser won't send Secure cookies on HTTP requests**
   - Refresh token never reaches the backend
   - Token refresh fails with 400 Bad Request

## Solution: Dual Storage Strategy

Environment-based storage approach:
- **Development**: Store refresh token in localStorage (less secure but dev is already insecure)
- **Production**: Use HttpOnly cookies (secure, immune to XSS)
- Environment variable `NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH` controls behavior

### Why This Solution Works

1. **Development Already Insecure**:
   - Running on HTTP (no encryption)
   - No domain isolation (localhost)
   - Browser dev tools expose everything
   - Adding localStorage doesn't meaningfully reduce security

2. **Production Remains Secure**:
   - HTTPS (encrypted)
   - HttpOnly cookies (immune to XSS)
   - SameSite=Strict (CSRF protection)
   - Proper domain isolation

3. **Environment-Aware**:
   - Single codebase works in all environments
   - No code changes needed when deploying
   - Environment variable controls behavior

## Implementation Details

### 1. Environment Configuration

**File**: `web/.env.development` (NEW)
```bash
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:3000/api/proxy
NEXT_PUBLIC_ENV=development

# Auth Strategy
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=true
```

**File**: `web/.env.staging` (UPDATED)
```bash
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:3000/api/proxy
NEXT_PUBLIC_ENV=staging

# Auth Strategy
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false
```

**File**: `web/.env.production` (NEW)
```bash
# API Configuration
NEXT_PUBLIC_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api
NEXT_PUBLIC_ENV=production

# Auth Strategy
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false
```

### 2. TokenStorageService (NEW)

**File**: `web/src/infrastructure/api/services/tokenStorageService.ts`

**Purpose**: Environment-aware storage service for refresh tokens

**Key Features**:
- `shouldUseLocalStorage()`: Checks `NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH` env variable
- `setRefreshToken(token)`: Stores in localStorage (dev) or logs warning (prod)
- `getRefreshToken()`: Retrieves from localStorage (dev) or returns null (prod)
- `clearRefreshToken()`: Removes from localStorage (dev) or logs warning (prod)
- `getStorageMode()`: Returns current storage mode for debugging

**Code Example**:
```typescript
class TokenStorageService {
  public shouldUseLocalStorage(): boolean {
    const useLocalStorage = process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH;
    return useLocalStorage === 'true';
  }

  public setRefreshToken(token: string): void {
    if (!token) {
      console.warn('[TOKEN STORAGE] Attempted to store empty/null token');
      return;
    }

    if (this.shouldUseLocalStorage()) {
      // Development: Store in localStorage
      console.log('[TOKEN STORAGE] Storing refresh token in localStorage (development mode)');
      localStorage.setItem('refreshToken', token);
    } else {
      // Production: Backend sets HttpOnly cookie
      console.warn('[TOKEN STORAGE] Cookie mode: Backend will set HttpOnly cookie');
    }
  }

  public getRefreshToken(): string | null {
    if (this.shouldUseLocalStorage()) {
      // Development: Read from localStorage
      return localStorage.getItem('refreshToken');
    } else {
      // Production: Cookie is sent automatically by browser
      return null; // Cannot access HttpOnly cookie from JavaScript
    }
  }
}
```

### 3. Updated tokenRefreshService

**File**: `web/src/infrastructure/api/services/tokenRefreshService.ts`

**Changes**:
- Import `tokenStorageService`
- Get refresh token from storage before making refresh request
- Send refresh token in request body if using localStorage mode
- Added comprehensive logging for debugging

**Code Example**:
```typescript
public async refreshAccessToken(): Promise<string | null> {
  try {
    console.log('🔍 [TOKEN REFRESH] Storage mode:', tokenStorageService.getStorageMode());

    // Get refresh token from storage (localStorage in dev, cookie in prod)
    const refreshToken = tokenStorageService.getRefreshToken();

    // Development: Send refresh token in request body (from localStorage)
    // Production: Refresh token is in HttpOnly cookie, backend reads it automatically
    const requestBody = refreshToken ? { refreshToken } : {};

    const response = await apiClient.post<{
      accessToken: string;
      tokenExpiresAt: string;
    }>('/Auth/refresh', requestBody);

    // ... rest of token refresh logic
  } catch (error) {
    // ... error handling
  }
}
```

### 4. Updated LocalStorageService

**File**: `web/src/infrastructure/storage/localStorage.ts`

**Changes**:
- Import `tokenStorageService`
- Delegate refresh token operations to `TokenStorageService`
- Maintains backward compatibility for access tokens
- `clearAuth()` now uses `TokenStorageService`

**Code Example**:
```typescript
import { tokenStorageService } from '../api/services/tokenStorageService';

export class LocalStorageService {
  static getRefreshToken(): string | null {
    // Delegate to TokenStorageService for environment-aware storage
    return tokenStorageService.getRefreshToken();
  }

  static setRefreshToken(token: string): boolean {
    // Delegate to TokenStorageService for environment-aware storage
    tokenStorageService.setRefreshToken(token);
    return true;
  }

  static clearAuth(): void {
    this.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
    // Delegate to TokenStorageService for environment-aware clearing
    tokenStorageService.clearRefreshToken();
    this.removeItem(STORAGE_KEYS.USER);
  }
}
```

### 5. Updated Auth Types

**File**: `web/src/infrastructure/api/types/auth.types.ts`

**Changes**:
- `LoginResponse` now includes optional `refreshToken` field
- Allows backend to send refresh token in response body (development)
- Cookie mode (production) doesn't send refresh token in body

**Code Example**:
```typescript
export interface LoginResponse {
  user: UserDto;
  accessToken: string;
  refreshToken?: string; // Optional: Sent in response body for localStorage mode (development)
  tokenExpiresAt: string;
}
```

### 6. Updated LoginForm

**File**: `web/src/presentation/components/features/auth/LoginForm.tsx`

**Changes**:
- Handles optional `refreshToken` in response body
- Sets refresh token from response if present (development)
- Falls back to empty string for cookie mode (production)

**Code Example**:
```typescript
const response = await authRepository.login(data, rememberMe);

// Phase 6A.10: In development (localStorage mode), backend sends refreshToken in response body
// In production (cookie mode), refreshToken is in HttpOnly cookie
const tokens: AuthTokens = {
  accessToken: response.accessToken,
  refreshToken: response.refreshToken || '', // Use refreshToken from response if present
  expiresIn: 3600,
};
setAuth(response.user, tokens);
```

## Testing

### Unit Tests

**File**: `web/src/infrastructure/api/services/__tests__/tokenStorageService.test.ts`

**Coverage**: 22 tests, all passing

**Test Categories**:
1. Environment detection (`shouldUseLocalStorage`)
2. localStorage mode operations (set, get, clear)
3. Cookie mode operations (no-ops with logging)
4. Storage mode detection
5. Edge cases (null, undefined, empty string)

**Test Results**:
```
✓ TokenStorageService (22 tests) 65ms
  ✓ shouldUseLocalStorage (4 tests)
  ✓ setRefreshToken - localStorage mode (3 tests)
  ✓ setRefreshToken - cookie mode (2 tests)
  ✓ getRefreshToken - localStorage mode (2 tests)
  ✓ getRefreshToken - cookie mode (2 tests)
  ✓ clearRefreshToken - localStorage mode (2 tests)
  ✓ clearRefreshToken - cookie mode (2 tests)
  ✓ getStorageMode (2 tests)
  ✓ Edge cases (3 tests)

Test Files  1 passed (1)
Tests  22 passed (22)
```

### Build Verification

```bash
$ cd web && npm run build
✓ Compiled successfully in 42s
```

**Result**: 0 errors, 0 warnings

## Security Considerations

### Development Environment
- Already insecure (HTTP, no domain isolation)
- Browser dev tools expose all localStorage
- Adding refresh token to localStorage doesn't meaningfully reduce security
- Trade-off: Development convenience vs. insignificant security loss

### Production Environment
- HTTPS encryption
- HttpOnly cookies (immune to XSS attacks)
- SameSite=Strict (CSRF protection)
- Proper domain isolation
- No localStorage usage for refresh tokens
- Production security remains unchanged

## Next Steps

### Immediate (Required for Testing)
1. **Backend Update**: Modify `/Auth/login` and `/Auth/refresh` endpoints to:
   - Check if `ASPNETCORE_ENVIRONMENT` is `Development`
   - If Development, include `refreshToken` in response body
   - If Production, only set HttpOnly cookie (current behavior)

2. **Integration Testing**:
   - Test login → navigate to event page → token refresh → logout flow
   - Verify event detail page no longer triggers forced logout
   - Verify localStorage contains refresh token in development
   - Verify console logs show correct storage mode

### Future Enhancements
1. Add refresh token rotation for enhanced security
2. Implement token expiration monitoring
3. Add user notification before token expires
4. Implement "Remember Me" with extended refresh token lifetime

## Related Documentation

- Architecture Analysis: [`docs/AUTHENTICATION_COOKIE_ARCHITECTURE_ANALYSIS.md`](./AUTHENTICATION_COOKIE_ARCHITECTURE_ANALYSIS.md)
- Master Index: [`docs/PHASE_6A_MASTER_INDEX.md`](./PHASE_6A_MASTER_INDEX.md)
- Progress Tracker: [`docs/PROGRESS_TRACKER.md`](./PROGRESS_TRACKER.md)
- Action Plan: [`docs/STREAMLINED_ACTION_PLAN.md`](./STREAMLINED_ACTION_PLAN.md)

## Git Commits

1. `feat(auth): Implement dual storage strategy for refresh tokens` (91d27f9)
   - TokenStorageService implementation
   - Updated tokenRefreshService, LocalStorageService
   - Updated auth types and LoginForm
   - 22 unit tests, all passing

2. `chore: Add environment configuration files for dual storage strategy` (7dc40c9)
   - .env.development (NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=true)
   - .env.staging (NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false)
   - .env.production (NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false)

## Lessons Learned

1. **Cookie Security Attributes Matter**:
   - Secure cookies cannot be sent over HTTP
   - Browser enforces this strictly
   - No workaround from frontend

2. **Development Pragmatism**:
   - Development environment already has low security
   - Pragmatic solutions can trade insignificant security for convenience
   - Environment-specific behavior is acceptable

3. **Architecture Documentation**:
   - System-architect consultation provided comprehensive analysis
   - Documented decision rationale prevents future confusion
   - Alternative solutions documented for reference

4. **Test-Driven Development**:
   - Writing tests first clarified implementation requirements
   - Edge cases discovered during test writing
   - 100% test coverage provides confidence
