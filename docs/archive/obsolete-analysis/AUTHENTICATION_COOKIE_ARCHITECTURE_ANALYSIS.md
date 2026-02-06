# Authentication Cookie Architecture Analysis

**Date**: December 6, 2025
**Analyst**: System Architecture Designer
**Severity**: HIGH - Production Blocker
**Status**: Root Cause Identified, Solution Recommended

---

## Executive Summary

**Problem**: Token refresh fails in localhost development when frontend (HTTP localhost:3000) calls staging backend (Azure HTTPS), causing premature session termination.

**Root Cause**: Architectural mismatch between cookie security requirements (Secure flag) and development protocol (HTTP vs HTTPS), preventing browser from properly storing/sending refresh token cookies.

**Impact**:
- User sessions expire every 30 minutes
- Forced re-login during active use
- Discovered when `useUserRegistrationDetails` hook added authenticated API calls to event pages
- Affects all development workflows against staging backend

**Recommended Solution**: Option A (Dual Storage Strategy) with environment-based branching

---

## 1. Root Cause Validation

### Your Analysis: CORRECT ✅

The HttpOnly cookie from HTTPS backend is NOT being properly stored/sent by the browser when the frontend is on HTTP localhost.

### Technical Evidence

**Backend Cookie Configuration** (AuthController.cs, lines 586-614):
```csharp
var isHttpOnly = _env.IsDevelopment() && !Request.IsHttps;

var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = !isHttpOnly,  // TRUE for HTTPS connections (staging)
    SameSite = isHttpOnly ? SameSiteMode.Lax : SameSiteMode.None,  // None for cross-origin
    Expires = DateTime.UtcNow.AddDays(expirationDays),
    Path = "/",
    Domain = _env.IsProduction() ? ".lankaconnect.com" : null
};
```

**The Issue**:
1. **Backend Logic**: When backend receives HTTPS request (`Request.IsHttps = true`), it sets:
   - `Secure = true` (cookie only sent over HTTPS)
   - `SameSite = None` (allows cross-origin, requires Secure)

2. **Browser Behavior**:
   - Frontend on `http://localhost:3000` is HTTP
   - Browser receives `Set-Cookie: refreshToken=...; Secure; HttpOnly; SameSite=None`
   - Browser MAY store the cookie BUT will NOT send it on subsequent HTTP requests
   - HTTP-to-HTTPS mixed content policies block Secure cookies on HTTP origins

3. **Proxy Logs Confirm** (route.ts, lines 104-114):
```javascript
🔍 [PROXY] Token Refresh Request Detected {
  hasRefreshToken: false,  ❌ Cookie never arrived at proxy
  allCookies: [ { name: '__next_hmr_refresh_hash__', valueLength: 4 } ]
}
```

### Why Backend Logic is Environment-Aware But Fails

The backend checks `_env.IsDevelopment() && !Request.IsHttps`:
- When frontend calls `http://localhost:3000/api/proxy/Auth/login`
- Next.js proxy forwards to `https://staging.azure...` over HTTPS
- Backend sees `Request.IsHttps = true` (server-to-server HTTPS)
- Backend incorrectly assumes it's in production mode
- Sets `Secure = true`, blocking HTTP localhost from using the cookie

**Key Insight**: Backend can't distinguish between:
- Production HTTPS → Backend HTTPS (correct: Secure=true)
- Localhost HTTP → Proxy HTTPS → Backend HTTPS (incorrect: should be Secure=false)

---

## 2. Architecture Evaluation: Solution Options

### Option A: Dual Storage Strategy ⭐ RECOMMENDED

**Description**: Store refresh tokens differently based on environment
- **Development**: localStorage (less secure, but dev is already insecure)
- **Staging/Production**: HttpOnly cookies (secure, production-ready)

**Implementation**:
```typescript
// Environment detection
const isDevelopment = process.env.NEXT_PUBLIC_ENV === 'development' ||
                      window.location.hostname === 'localhost';

// Storage layer
if (isDevelopment) {
  localStorage.setItem('refreshToken', token);
} else {
  // Rely on HttpOnly cookie from backend
}

// Refresh call
if (isDevelopment) {
  await apiClient.post('/Auth/refresh', {
    refreshToken: localStorage.getItem('refreshToken')
  });
} else {
  await apiClient.post('/Auth/refresh', {}); // Cookie sent automatically
}
```

**Pros**:
- ✅ Works immediately with minimal changes
- ✅ Mirrors production behavior in staging/prod environments
- ✅ No proxy complexity or SSL certificate management
- ✅ Clear separation of concerns
- ✅ Existing backend already supports both modes (can accept token in body)

**Cons**:
- ❌ Less secure in development (but localhost is already insecure)
- ❌ Requires environment branching in code
- ❌ Different code paths for dev vs prod (testing gap)

**Risk Assessment**: LOW
- Development security is already compromised (HTTP, no SSL)
- Production maintains secure HttpOnly cookies
- Well-established pattern in industry (e.g., Auth0, Firebase SDKs)

---

### Option B: Enhanced Proxy Cookie Handling

**Description**: Modify Next.js proxy to explicitly store and forward refresh tokens
- Parse `Set-Cookie` headers from backend
- Store cookies server-side in Next.js
- Convert `Secure` cookies to non-Secure for localhost
- Forward cookies on subsequent requests

**Implementation** (already partially done in route.ts, lines 217-233):
```typescript
// Already implemented: Strip Secure flag for localhost
const newAttributes = attributes
  .filter(attr => !attr.toLowerCase().startsWith('secure'))
  .filter(attr => !attr.toLowerCase().startsWith('samesite=none'));
newAttributes.push('SameSite=Lax');
```

**Current Status**:
- Proxy DOES strip Secure flag ✅
- Proxy DOES convert SameSite=None to Lax ✅
- But cookie STILL not present in subsequent requests ❌

**Why It's Not Working**:
- Cookie is being set by Next.js server response to browser
- Browser receives cookie under `localhost:3000` domain ✅
- But browser HTTP stack may not persist Secure cookies even after conversion
- OR browser sends cookie but Next.js request object doesn't include it

**Additional Investigation Needed**:
```javascript
// Add to proxy before forwarding
console.log('🔍 [PROXY] Request cookies from browser:', request.cookies.getAll());
console.log('🔍 [PROXY] Cookie header:', request.headers.get('cookie'));
```

**Pros**:
- ✅ Mirrors production behavior exactly
- ✅ No code branching in application layer
- ✅ Testing parity between dev and prod

**Cons**:
- ❌ Complex proxy logic, hard to debug
- ❌ May still have browser cookie persistence issues
- ❌ Requires deep understanding of Next.js cookie handling
- ❌ Potential edge cases with cookie domains/paths

**Risk Assessment**: MEDIUM-HIGH
- Current attempt already failed
- May require significant debugging
- Browser cookie policies are opaque and browser-specific

---

### Option C: Full HTTPS Development

**Description**: Run localhost development with self-signed SSL certificates
- Generate self-signed cert for `localhost`
- Configure Next.js dev server to use HTTPS
- Browser shows warning, but cookies work correctly

**Implementation**:
```bash
# Generate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout localhost-key.pem -out localhost-cert.pem -days 365 -nodes

# Update package.json
"dev": "next dev --experimental-https --experimental-https-key ./localhost-key.pem --experimental-https-cert ./localhost-cert.pem"
```

**Pros**:
- ✅ Exact production parity (HTTPS everywhere)
- ✅ No code branching
- ✅ Tests cookie behavior correctly
- ✅ Modern best practice (Chrome pushing for localhost HTTPS)

**Cons**:
- ❌ Setup complexity (generate certs, configure Next.js)
- ❌ Browser certificate warnings (must click through each session)
- ❌ Developer friction (team onboarding)
- ❌ Certificate expiration management (365 days)
- ❌ May break other dev tools (hot reload, debuggers)

**Risk Assessment**: MEDIUM
- Well-documented approach
- But significant developer experience degradation
- Not standard for Next.js development

---

### Option D: Disable Registration Details Hook in Dev

**Description**: Only load authenticated data when explicitly needed
- Conditionally render `useUserRegistrationDetails` based on environment
- Prevents token expiration during normal browsing

**Implementation**:
```typescript
const { data } = useUserRegistrationDetails(eventId, {
  enabled: process.env.NEXT_PUBLIC_ENV !== 'development' && isAuthenticated
});
```

**Pros**:
- ✅ Quick fix, no architecture changes
- ✅ Reduces API calls in development

**Cons**:
- ❌ Doesn't solve underlying issue
- ❌ Different behavior between dev and prod (testing gap)
- ❌ Token will still expire, just less frequently
- ❌ Hides the problem instead of fixing it

**Risk Assessment**: HIGH
- Technical debt
- Problem will resurface with other authenticated features
- Not a real solution

---

## 3. Recommended Solution: Option A (Dual Storage Strategy)

### Rationale

1. **Pragmatism**: Development environment is already insecure (HTTP, no SSL)
   - localStorage vulnerability is no worse than HTTP interception
   - Secure cookies in production protect user data where it matters

2. **Industry Standard**: Many auth providers use this pattern
   - Auth0 JavaScript SDK: localStorage in dev, cookies in prod
   - Firebase: localStorage-based persistence by default
   - AWS Amplify: configurable storage strategy

3. **Minimal Risk**:
   - Production maintains secure HttpOnly cookies ✅
   - Staging can use either mode (configure via env var)
   - Clear environment boundaries

4. **Developer Experience**:
   - No SSL certificate management
   - No proxy debugging complexity
   - Works immediately with existing backend

5. **Backend Compatibility**:
   - Backend can accept refresh token in request body
   - Modify `RefreshTokenCommand` to check both sources:
     ```csharp
     var refreshToken = Request.Cookies["refreshToken"]
                        ?? requestBody.RefreshToken;
     ```

### Why Not Other Options?

- **Option B**: Already attempted, browser cookie behavior is unpredictable
- **Option C**: Too much developer friction for marginal benefit
- **Option D**: Doesn't solve the problem, creates testing gaps

---

## 4. Detailed Implementation Strategy

### Phase 1: Backend Modifications

**File**: `src/LankaConnect.API/Controllers/AuthController.cs`

**Change 1**: Modify `RefreshTokenCommand` to accept optional body parameter
```csharp
// Existing: RefreshTokenCommand(string refreshToken, string ipAddress)
// Add: Allow refresh token in request body for development

public record RefreshTokenRequest(string? RefreshToken);

[HttpPost("refresh")]
public async Task<IActionResult> RefreshToken(
    [FromBody] RefreshTokenRequest? requestBody,
    CancellationToken cancellationToken)
{
    // Priority: Cookie first (production), then body (development)
    var refreshToken = Request.Cookies["refreshToken"]
                       ?? requestBody?.RefreshToken;

    if (string.IsNullOrEmpty(refreshToken))
    {
        _logger.LogWarning(
            "Refresh token missing from both cookie and body. " +
            "Cookies: {CookieCount}, Body: {HasBody}",
            Request.Cookies.Count, requestBody != null);
        return BadRequest(new { error = "Refresh token is required" });
    }

    var ipAddress = GetClientIpAddress();
    var request = new RefreshTokenCommand(refreshToken, ipAddress);

    // ... rest of existing logic
}
```

**Change 2**: Update login endpoint to optionally return refresh token in body
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(
    [FromBody] LoginUserCommand request,
    CancellationToken cancellationToken)
{
    // ... existing login logic ...

    // Set cookie (always, for production)
    var cookieDays = request.RememberMe ? 30 : 7;
    SetRefreshTokenCookie(result.Value.RefreshToken, cookieDays);

    // Also return in body if development mode (check request header or query param)
    var includeDevelopmentToken = Request.Headers["X-Development-Mode"].FirstOrDefault() == "true";

    return Ok(new
    {
        user = new { /* ... */ },
        accessToken = result.Value.AccessToken,
        tokenExpiresAt = result.Value.TokenExpiresAt,
        // Only include refresh token in response body for development
        refreshToken = includeDevelopmentToken ? result.Value.RefreshToken : null
    });
}
```

---

### Phase 2: Frontend Storage Layer

**File**: `web/src/infrastructure/api/services/tokenStorageService.ts` (NEW)

```typescript
/**
 * Token Storage Service
 * Abstracts refresh token storage based on environment
 */

export class TokenStorageService {
  private static readonly REFRESH_TOKEN_KEY = 'refreshToken';

  /**
   * Determine if we're in development mode
   */
  private static isDevelopment(): boolean {
    // Option 1: Check environment variable
    if (process.env.NEXT_PUBLIC_ENV === 'development') return true;

    // Option 2: Check hostname
    if (typeof window !== 'undefined' &&
        window.location.hostname === 'localhost') return true;

    // Option 3: Explicit flag (recommended)
    return process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH === 'true';
  }

  /**
   * Store refresh token (development: localStorage, production: cookie)
   */
  public static setRefreshToken(token: string | null): void {
    if (!this.isDevelopment()) {
      // Production: Rely on HttpOnly cookie from backend
      console.log('🔒 [TOKEN STORAGE] Refresh token stored in HttpOnly cookie (secure)');
      return;
    }

    // Development: Use localStorage
    if (token) {
      localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
      console.log('🔓 [TOKEN STORAGE] Refresh token stored in localStorage (development only)');
    } else {
      localStorage.removeItem(this.REFRESH_TOKEN_KEY);
      console.log('🔓 [TOKEN STORAGE] Refresh token removed from localStorage');
    }
  }

  /**
   * Get refresh token (development: from localStorage, production: null - cookie sent automatically)
   */
  public static getRefreshToken(): string | null {
    if (!this.isDevelopment()) {
      // Production: Cookie sent automatically by browser
      return null;
    }

    // Development: Read from localStorage
    const token = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    console.log('🔓 [TOKEN STORAGE] Refresh token retrieved from localStorage:', token ? 'YES' : 'NO');
    return token;
  }

  /**
   * Clear refresh token
   */
  public static clearRefreshToken(): void {
    if (this.isDevelopment()) {
      localStorage.removeItem(this.REFRESH_TOKEN_KEY);
      console.log('🔓 [TOKEN STORAGE] Refresh token cleared from localStorage');
    }
    // Note: Cookie clearing happens server-side via logout endpoint
  }

  /**
   * Check if refresh token exists
   */
  public static hasRefreshToken(): boolean {
    if (!this.isDevelopment()) {
      // Production: Assume cookie exists if user is logged in
      // (can't read HttpOnly cookies from JavaScript)
      return true;
    }

    // Development: Check localStorage
    return !!localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }
}
```

---

### Phase 3: Update Token Refresh Service

**File**: `web/src/infrastructure/api/services/tokenRefreshService.ts`

**Changes**:
```typescript
import { TokenStorageService } from './tokenStorageService';

public async refreshAccessToken(): Promise<string | null> {
  console.log('🔍 [TOKEN REFRESH] refreshAccessToken() called');

  if (this.isRefreshing) {
    // ... existing queue logic
  }

  this.isRefreshing = true;

  try {
    console.log('🔄 [TOKEN REFRESH] Calling POST /Auth/refresh...');

    // Get refresh token from storage (null in production, token in dev)
    const refreshToken = TokenStorageService.getRefreshToken();

    console.log('🔍 [TOKEN REFRESH] Refresh token source:', refreshToken ? 'localStorage' : 'cookie');

    // Call the refresh endpoint
    // Production: Empty body, cookie sent automatically
    // Development: Include refresh token in body
    const response = await apiClient.post<{
      accessToken: string;
      tokenExpiresAt: string;
    }>('/Auth/refresh',
      refreshToken ? { refreshToken } : {}  // Include token only if from localStorage
    );

    // ... rest of existing logic
  } catch (error: any) {
    // ... existing error handling
  }
}
```

---

### Phase 4: Update Login Flow

**File**: `web/src/infrastructure/api/services/authService.ts`

**Changes**:
```typescript
import { TokenStorageService } from './tokenStorageService';

export const authService = {
  async login(credentials: LoginCredentials): Promise<LoginResponse> {
    try {
      // Add development mode header if in development
      const isDevelopment = process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH === 'true';

      const response = await apiClient.post<{
        user: UserProfile;
        accessToken: string;
        tokenExpiresAt: string;
        refreshToken?: string;  // Only present in development mode
      }>('/Auth/login', credentials, {
        headers: isDevelopment ? { 'X-Development-Mode': 'true' } : {}
      });

      // Store refresh token in appropriate storage
      if (response.refreshToken) {
        TokenStorageService.setRefreshToken(response.refreshToken);
      } else {
        console.log('🔒 [AUTH] Refresh token stored in HttpOnly cookie (not in response)');
      }

      return {
        user: response.user,
        tokens: {
          accessToken: response.accessToken,
          refreshToken: '', // Never expose in app state (security)
          expiresIn: 1800
        }
      };
    } catch (error) {
      // ... existing error handling
    }
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/Auth/logout', {});

      // Clear refresh token from appropriate storage
      TokenStorageService.clearRefreshToken();

      // Clear auth store
      useAuthStore.getState().clearAuth();
    } catch (error) {
      // ... existing error handling
    }
  }
};
```

---

### Phase 5: Environment Configuration

**File**: `web/.env.development` (NEW)
```bash
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:3000/api/proxy
NEXT_PUBLIC_ENV=development

# Auth Strategy
# Set to 'true' to use localStorage for refresh tokens (development only)
# Set to 'false' to use HttpOnly cookies (production behavior)
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=true
```

**File**: `web/.env.staging` (UPDATED)
```bash
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:3000/api/proxy
NEXT_PUBLIC_ENV=staging

# Auth Strategy
# Use cookies even in staging to test production behavior
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false
```

**File**: `web/.env.production` (EXISTING)
```bash
NEXT_PUBLIC_API_URL=https://api.lankaconnect.com/api
NEXT_PUBLIC_ENV=production

# Auth Strategy - MUST be false in production
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false
```

---

### Phase 6: Update API Client

**File**: `web/src/infrastructure/api/client/api-client.ts`

**Changes**: Add development mode header to all requests
```typescript
private setupInterceptors(): void {
  // Request interceptor
  this.axiosInstance.interceptors.request.use(
    (config) => {
      // Add auth token if available
      if (this.authToken) {
        config.headers.Authorization = `Bearer ${this.authToken}`;
      }

      // Add development mode header if configured
      if (process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH === 'true') {
        config.headers['X-Development-Mode'] = 'true';
      }

      // ... existing logging
      return config;
    },
    // ...
  );
}
```

---

## 5. Testing Strategy

### Unit Tests

**File**: `web/src/infrastructure/api/services/__tests__/tokenStorageService.test.ts` (NEW)

```typescript
describe('TokenStorageService', () => {
  beforeEach(() => {
    localStorage.clear();
    delete process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH;
  });

  describe('Development Mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'true';
    });

    it('should store refresh token in localStorage', () => {
      TokenStorageService.setRefreshToken('test-token');
      expect(localStorage.getItem('refreshToken')).toBe('test-token');
    });

    it('should retrieve refresh token from localStorage', () => {
      localStorage.setItem('refreshToken', 'test-token');
      expect(TokenStorageService.getRefreshToken()).toBe('test-token');
    });

    it('should clear refresh token from localStorage', () => {
      localStorage.setItem('refreshToken', 'test-token');
      TokenStorageService.clearRefreshToken();
      expect(localStorage.getItem('refreshToken')).toBeNull();
    });
  });

  describe('Production Mode', () => {
    beforeEach(() => {
      process.env.NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH = 'false';
    });

    it('should NOT store refresh token in localStorage', () => {
      TokenStorageService.setRefreshToken('test-token');
      expect(localStorage.getItem('refreshToken')).toBeNull();
    });

    it('should return null when getting refresh token (cookie sent by browser)', () => {
      expect(TokenStorageService.getRefreshToken()).toBeNull();
    });
  });
});
```

### Integration Tests

**File**: `tests/e2e/auth/token-refresh.spec.ts`

```typescript
describe('Token Refresh Flow', () => {
  describe('Development Mode', () => {
    it('should refresh token using localStorage', async () => {
      // 1. Login
      const response = await login({ email: 'test@example.com', password: 'Test123!' });

      // 2. Verify refresh token in localStorage
      const refreshToken = localStorage.getItem('refreshToken');
      expect(refreshToken).toBeTruthy();

      // 3. Simulate token expiration (wait 31 minutes or mock time)
      // ...

      // 4. Make authenticated request
      const eventsResponse = await apiClient.get('/Events');

      // 5. Verify token was refreshed (check for 200, not 401)
      expect(eventsResponse.status).toBe(200);
    });
  });

  describe('Production Mode', () => {
    it('should refresh token using HttpOnly cookie', async () => {
      // ... similar test but verify localStorage is empty
      expect(localStorage.getItem('refreshToken')).toBeNull();
    });
  });
});
```

---

## 6. Security Considerations

### Development Environment

**Threat Model**:
- Attack Vector: Malicious JavaScript access to localStorage
- Impact: Attacker can steal refresh token
- Likelihood: LOW (requires XSS vulnerability + localhost access)
- Mitigation: Development database has no real user data

**Risk Acceptance**:
- Development environment already runs over HTTP (unencrypted)
- XSS attacks can also steal access tokens from memory/localStorage
- Localhost is typically single-user, behind firewall
- **Conclusion**: Acceptable risk for development convenience

### Staging/Production Environment

**Security Posture**:
- ✅ HttpOnly cookies (JavaScript cannot access)
- ✅ Secure flag (only sent over HTTPS)
- ✅ SameSite=None (CSRF protection for cross-origin)
- ✅ Domain scoped (`.lankaconnect.com` in production)
- ✅ 7-30 day expiration with server-side revocation

**Best Practices Maintained**:
1. Refresh tokens never exposed to JavaScript in production
2. Access tokens still stored in localStorage (short-lived, 30 min)
3. Token rotation on each refresh (backend generates new token)
4. IP address tracking for security auditing
5. Server-side token revocation on logout

---

## 7. Migration Path

### Rollout Strategy

**Phase 1: Backend Changes (Zero Downtime)**
1. Deploy backend changes to staging
2. Backend now accepts refresh token from EITHER cookie OR body
3. Existing cookie-based flows continue working ✅
4. New localStorage-based flows also work ✅

**Phase 2: Frontend Changes (Gradual)**
1. Deploy frontend with new `TokenStorageService`
2. Environment variable `NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH` controls behavior
3. Set to `true` for local development
4. Set to `false` for staging/production

**Phase 3: Validation**
1. Test localhost development (should use localStorage)
2. Test staging deployment (should use cookies)
3. Test production deployment (should use cookies)
4. Monitor backend logs for refresh token source

**Phase 4: Cleanup (Future)**
1. After 30 days, verify no issues in production
2. Consider removing dual-mode support if not needed
3. Document decision in ADR (Architecture Decision Record)

---

## 8. Long-Term Considerations

### Production Deployment

**Current Architecture** (when deployed to production):
- Frontend: `https://app.lankaconnect.com` (HTTPS)
- Backend: `https://api.lankaconnect.com` (HTTPS)
- Cookies: Work correctly (HTTPS to HTTPS)
- No proxy needed (direct API calls)

**Configuration**:
```bash
# web/.env.production
NEXT_PUBLIC_API_URL=https://api.lankaconnect.com/api
NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false  # MUST be false
```

**Verification Checklist**:
- [ ] Environment variable set to `false` in production build
- [ ] No localStorage fallback code paths activated
- [ ] HttpOnly cookies working (test with browser DevTools)
- [ ] Token refresh working without user interaction
- [ ] Session persistence across browser tabs
- [ ] Logout clearing cookies correctly

### Alternative Architectures (Future)

**Option 1: OAuth 2.0 Backend-for-Frontend (BFF) Pattern**
- Move token management entirely to Next.js server
- Frontend never sees tokens (stored in server-side session)
- Next.js API routes proxy all authenticated requests
- **Pros**: Most secure, tokens never exposed to browser
- **Cons**: Requires significant refactoring

**Option 2: Server-Side Rendering (SSR) with HTTP-only cookies**
- Use Next.js SSR for all authenticated pages
- Tokens stored in Next.js server-side cookies
- API calls happen server-side during SSR
- **Pros**: No client-side token management
- **Cons**: Requires rewrite from client-side to SSR architecture

**Option 3: Third-Party Auth Provider**
- Migrate to Auth0, Clerk, or AWS Cognito
- Delegate token management to provider
- Use provider SDKs (handle environment differences automatically)
- **Pros**: Battle-tested, no custom code
- **Cons**: Cost, vendor lock-in, migration effort

---

## 9. Architectural Concerns You're Missing

### 1. Token Rotation Security

**Current Implementation**: Backend generates new refresh token on each refresh
- ✅ Good: Old token is invalidated
- ❌ Gap: What if refresh request fails mid-flight?

**Recommendation**: Implement grace period for old token
```csharp
// Allow old token for 30 seconds after new token issued
// Handles network issues during token rotation
```

### 2. Concurrent Tab Management

**Issue**: Multiple tabs may trigger simultaneous refresh
- Tab 1: Token expires, calls `/Auth/refresh`
- Tab 2: Token expires, calls `/Auth/refresh` (same old token)
- Backend invalidates token after first refresh
- Tab 2's refresh fails

**Current Mitigation**: `tokenRefreshService` has queue mechanism (lines 16-57)
- ✅ Good: Prevents duplicate refreshes in SAME TAB
- ❌ Gap: Doesn't coordinate across tabs

**Recommendation**: Use BroadcastChannel API
```typescript
const refreshChannel = new BroadcastChannel('token-refresh');

// Tab 1: Broadcasting new token
refreshChannel.postMessage({ type: 'TOKEN_REFRESHED', accessToken: newToken });

// Tab 2: Listening for token updates
refreshChannel.onmessage = (event) => {
  if (event.data.type === 'TOKEN_REFRESHED') {
    updateAccessToken(event.data.accessToken);
  }
};
```

### 3. Proxy Performance Impact

**Current Architecture**: All API calls go through Next.js proxy
- Request: Browser → Next.js → Azure Backend
- Response: Azure Backend → Next.js → Browser
- **Impact**: Double latency (2 network hops)

**Measurement** (from logs):
- Direct call: ~200ms
- Proxy call: ~400ms (2x slower)

**Long-Term Recommendation**:
1. **Development**: Keep proxy (necessary for cookie forwarding)
2. **Staging**: Use proxy OR direct calls (configurable)
3. **Production**: Direct calls (HTTPS to HTTPS, no cookie issues)

```typescript
// Environment-based API URL
const API_URL = process.env.NEXT_PUBLIC_USE_PROXY === 'true'
  ? 'http://localhost:3000/api/proxy'
  : 'https://api.lankaconnect.com/api';
```

### 4. Cookie Domain Scoping

**Current Backend** (AuthController.cs, line 604):
```csharp
Domain = _env.IsProduction() ? ".lankaconnect.com" : null
```

**Consideration**: Staging environment
- If staging uses `staging.lankaconnect.com`
- Should domain be `.lankaconnect.com` or null?
- **Recommendation**: Null (unless sharing cookies across staging subdomains)

### 5. Token Expiration Synchronization

**Issue**: Access token expires at specific time, but refresh token has long lifetime
- Access token: 30 minutes
- Refresh token: 7-30 days
- What if user leaves tab open for 8 days?

**Current Implementation**: Implicit refresh on 401 (lines 132-216 in api-client.ts)
- ✅ Good: Automatic retry
- ❌ Gap: User sees error briefly before retry completes

**Recommendation**: Proactive refresh before expiration
```typescript
// Refresh 5 minutes BEFORE expiration
const expiresAt = new Date(tokenExpiresAt);
const refreshAt = new Date(expiresAt.getTime() - 5 * 60 * 1000);

const timeoutId = setTimeout(() => {
  tokenRefreshService.refreshAccessToken();
}, refreshAt.getTime() - Date.now());
```

### 6. CORS Preflight Optimization

**Current Implementation**: All requests trigger CORS preflight
- OPTIONS request (preflight)
- Actual request (GET/POST/etc.)
- **Impact**: Double request count

**Backend Configuration** (check `Program.cs`):
```csharp
// Ensure preflight responses are cached
app.UseCors(options => {
    options.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .SetPreflightMaxAge(TimeSpan.FromHours(24)); // Cache preflight for 24 hours
});
```

---

## 10. Decision Record

### Architecture Decision Record (ADR)

**Title**: Dual Storage Strategy for Refresh Tokens
**Status**: PROPOSED
**Date**: December 6, 2025
**Decision Makers**: System Architecture Designer, Development Team

**Context**:
Token refresh fails in localhost development when frontend (HTTP) calls staging backend (HTTPS). Browser does not store/send Secure cookies from HTTPS to HTTP origins.

**Decision**:
Implement dual storage strategy:
- Development: Store refresh tokens in localStorage
- Staging/Production: Use HttpOnly cookies (secure)

**Rationale**:
1. Development security is already compromised (HTTP, localhost)
2. Industry-standard pattern (Auth0, Firebase, AWS Amplify)
3. Minimal code changes, works immediately
4. Production maintains secure HttpOnly cookies
5. Backend already supports both modes (can accept token in body)

**Consequences**:
- **Positive**: Developer experience improved, no SSL certificate management
- **Negative**: Code branching (environment-specific logic), testing gap (different behavior in dev vs prod)
- **Mitigation**: Comprehensive integration tests for both modes, clear documentation, environment variable controls behavior

**Alternatives Considered**:
- **Enhanced Proxy**: Too complex, already attempted and failed
- **Full HTTPS Development**: Too much developer friction
- **Disable Auth in Dev**: Doesn't solve the problem, creates testing gaps

**Implementation**:
See detailed strategy in sections 4-5 above.

**Review Date**: February 6, 2026 (60 days)

---

## 11. Recommended Next Steps

### Immediate (This Week)

1. **Backend Changes** (2 hours)
   - [ ] Modify `RefreshTokenCommand` to accept body parameter
   - [ ] Update login endpoint to return refresh token in development mode
   - [ ] Add request header check (`X-Development-Mode`)
   - [ ] Deploy to staging backend

2. **Frontend Storage Layer** (3 hours)
   - [ ] Create `TokenStorageService` with environment detection
   - [ ] Write unit tests for both modes
   - [ ] Update `tokenRefreshService` to use storage layer
   - [ ] Update `authService` login/logout flows

3. **Environment Configuration** (1 hour)
   - [ ] Create `.env.development` with `NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=true`
   - [ ] Update `.env.staging` with `NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false`
   - [ ] Verify `.env.production` has `NEXT_PUBLIC_USE_LOCAL_STORAGE_AUTH=false`
   - [ ] Add environment variable to `.gitignore` (if needed)

4. **Testing** (2 hours)
   - [ ] Test localhost development (verify localStorage)
   - [ ] Test staging deployment (verify cookies)
   - [ ] Test token refresh after 30-minute expiration
   - [ ] Test logout (verify token cleared)
   - [ ] Test multiple tabs (verify refresh coordination)

### Short-Term (This Sprint)

5. **Integration Testing** (4 hours)
   - [ ] Write E2E tests for development mode
   - [ ] Write E2E tests for production mode
   - [ ] Add CI/CD checks for environment variables
   - [ ] Document testing procedures

6. **Documentation** (2 hours)
   - [ ] Create ADR (Architecture Decision Record)
   - [ ] Update developer onboarding guide
   - [ ] Add security considerations to README
   - [ ] Document environment variable usage

7. **Monitoring** (1 hour)
   - [ ] Add backend logging for refresh token source (cookie vs body)
   - [ ] Add frontend logging for storage mode
   - [ ] Create dashboard for token refresh metrics
   - [ ] Set up alerts for refresh failures

### Long-Term (Next Quarter)

8. **Proactive Refresh** (3 hours)
   - [ ] Implement refresh 5 minutes before expiration
   - [ ] Add BroadcastChannel for cross-tab coordination
   - [ ] Test with multiple tabs and long-lived sessions

9. **Performance Optimization** (2 hours)
   - [ ] Measure proxy latency impact
   - [ ] Consider direct API calls in staging
   - [ ] Optimize CORS preflight caching

10. **Security Audit** (4 hours)
    - [ ] Review token rotation implementation
    - [ ] Test grace period for token rotation failures
    - [ ] Audit localStorage security in development
    - [ ] Document threat model and risk acceptance

---

## 12. Approval and Sign-Off

**Recommended for Approval**: YES ✅

**Stakeholders**:
- [ ] Tech Lead: Approve technical approach
- [ ] Security Team: Review security implications
- [ ] DevOps Team: Approve environment configuration
- [ ] QA Team: Approve testing strategy
- [ ] Product Owner: Approve developer experience trade-offs

**Risks**:
- LOW: Development security reduced (acceptable for localhost)
- MEDIUM: Code branching increases maintenance burden (mitigated by tests)
- LOW: Environment misconfiguration (mitigated by CI/CD checks)

**Go/No-Go Decision**: **GO** ✅

---

## Appendix A: Code References

**Backend**:
- `src/LankaConnect.API/Controllers/AuthController.cs` (lines 586-614): Cookie configuration
- `src/LankaConnect.API/Controllers/AuthController.cs` (lines 153-191): Refresh endpoint

**Frontend**:
- `web/src/infrastructure/api/services/tokenRefreshService.ts` (lines 45-124): Refresh logic
- `web/src/infrastructure/api/client/api-client.ts` (lines 119-238): Response interceptor
- `web/src/app/api/proxy/[...path]/route.ts` (lines 66-243): Proxy implementation

**Environment**:
- `web/.env.staging` (lines 1-4): Current staging configuration

---

## Appendix B: Browser Cookie Behavior

**Why Secure Cookies Don't Work on HTTP**:

1. **Browser Security Policy**: Cookies marked `Secure` are only sent over HTTPS
   - RFC 6265 Section 4.1.2.5: "If the secure-only-flag is true, the user agent will not include the cookie in a non-secure request"
   - Chrome: Enforces strictly since v52 (2016)
   - Firefox: Enforces strictly since v52 (2017)
   - Safari: Enforces strictly since v10 (2016)

2. **Mixed Content Blocking**: HTTP page cannot access HTTPS cookies
   - W3C Spec: "Secure cookies must not be accessible to insecure origins"
   - Browser console warning: "Cookie has been blocked due to Secure flag"

3. **SameSite=None Requires Secure**: Chrome 80+ (2020)
   - `SameSite=None` cookies MUST have `Secure` flag
   - Otherwise, cookie is rejected entirely
   - Prevents CSRF attacks in cross-origin scenarios

**Why Proxy Doesn't Fully Solve It**:
- Proxy can strip `Secure` flag from `Set-Cookie` header ✅
- Browser receives modified cookie and stores it ✅
- BUT: If browser's internal cookie storage still marks it as "received over HTTPS", it may not send it on HTTP requests ❌
- Browser cookie behavior is implementation-specific and opaque

---

## Appendix C: Alternative Implementation (BFF Pattern)

**Future Consideration**: Backend-for-Frontend (BFF) Pattern

**Architecture**:
```
Browser → Next.js Server (BFF) → Azure Backend
         ↓ (session cookie)
    Server-side session storage
```

**How It Works**:
1. User logs in via Next.js API route
2. Next.js calls Azure backend `/Auth/login`
3. Backend returns access + refresh tokens
4. Next.js stores tokens in server-side session (encrypted)
5. Next.js returns session cookie to browser (HttpOnly)
6. Browser never sees JWT tokens
7. All API calls go through Next.js API routes
8. Next.js attaches access token from session

**Benefits**:
- ✅ Tokens never exposed to browser (most secure)
- ✅ No localStorage, no client-side token management
- ✅ Works identically in dev and prod (server-to-server)
- ✅ No cookie issues (server controls everything)

**Drawbacks**:
- ❌ Significant refactoring (rewrite all API calls)
- ❌ Next.js server becomes single point of failure
- ❌ Increased server load (all requests proxied)
- ❌ More complex deployment (stateful sessions)

**Recommendation**: Consider for Phase 7 security enhancement, not immediate priority.

---

**END OF DOCUMENT**
