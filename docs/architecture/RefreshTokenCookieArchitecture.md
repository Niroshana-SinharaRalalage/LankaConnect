# Architecture Decision Record: Refresh Token Cookie Implementation

**Date:** 2025-12-01
**Status:** Implemented
**Decision Maker:** System Architect

---

## Problem Statement

Users were being automatically logged out after 10 minutes despite implementing JWT refresh token functionality. Investigation revealed that refresh token cookies were not being set or transmitted correctly between frontend and backend.

## Root Cause Analysis

### Issue 1: Incorrect Cookie Security Configuration

**Original Code (INCORRECT):**
```csharp
var isLocalDevelopment = _env.IsDevelopment() || _env.IsStaging();

var cookieOptions = new CookieOptions
{
    HttpOnly = true,
    Secure = !isLocalDevelopment,  // ❌ FALSE in staging
    SameSite = isLocalDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
    ...
};
```

**The Problem:**
- Code treated "Staging" environment as equivalent to "local development"
- Azure Container Apps (staging) **always use HTTPS**, not HTTP
- Setting `Secure=false` for HTTPS connections causes browsers to reject cookies
- OR if Azure forces `Secure=true`, HTTP frontend cannot receive the cookie

### Issue 2: Cross-Origin Cookie Blocking

**Architecture Mismatch:**
```
Frontend:  HTTP  localhost:3000
Backend:   HTTPS *.azurecontainerapps.io (staging)

Browser Security Policy:
- Blocks cookies from HTTPS → HTTP (mixed content)
- Even with CORS configured and withCredentials: true
- Login response (JSON) succeeds, but cookie is silently dropped
```

**Why Login Appeared to Work:**
- Login API call returns 200 OK with JSON access token → Frontend sees success
- Cookie set in response headers → **Silently blocked by browser**
- No visible error, just missing cookie
- Refresh attempt fails because cookie was never set

---

## Decision

### Part 1: Fix Backend Cookie Logic (IMPLEMENTED)

**New Implementation:**
```csharp
private void SetRefreshTokenCookie(string refreshToken, int expirationDays = 7)
{
    // Check actual connection protocol, not just environment name
    var isHttpOnly = _env.IsDevelopment() && !Request.IsHttps;

    var cookieOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = !isHttpOnly,  // ✅ true for HTTPS (staging/prod), false only for local HTTP
        SameSite = isHttpOnly ? SameSiteMode.Lax : SameSiteMode.None,
        Expires = DateTime.UtcNow.AddDays(expirationDays),
        Path = "/",
        Domain = _env.IsProduction() ? ".lankaconnect.com" : null
    };

    _logger.LogDebug(
        "Setting refresh token cookie: Secure={Secure}, SameSite={SameSite}, " +
        "Expires={Expires}, Environment={Environment}, IsHttps={IsHttps}",
        cookieOptions.Secure, cookieOptions.SameSite, cookieOptions.Expires,
        _env.EnvironmentName, Request.IsHttps);

    Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
}
```

**Key Improvements:**
1. ✅ Check `Request.IsHttps` instead of just environment name
2. ✅ Azure staging (HTTPS) correctly sets `Secure=true, SameSite=None`
3. ✅ Local development (HTTP) correctly sets `Secure=false, SameSite=Lax`
4. ✅ Enhanced logging includes protocol information
5. ✅ Production can set Domain for subdomain sharing

### Part 2: Choose Development Architecture

## Option A: Local Backend Development (RECOMMENDED)

**Architecture:**
```
Frontend:  HTTP localhost:3000
Backend:   HTTP localhost:5000
Cookie:    Secure=false, SameSite=Lax
Result:    Same-origin, cookies work perfectly
```

**Implementation:**
```bash
# web/.env.local
NEXT_PUBLIC_API_URL=http://localhost:5000/api

# Run backend locally
cd src/LankaConnect.Api
dotnet run

# Run frontend
cd web
npm run dev
```

**Benefits:**
- ✅ Same-origin architecture (no cross-origin issues)
- ✅ Realistic testing environment
- ✅ Faster development iteration
- ✅ No special proxy configuration
- ✅ Matches production architecture pattern
- ✅ Easy debugging with local breakpoints

**Trade-offs:**
- Requires local database setup
- Must run both frontend and backend locally
- Different environment than staging

---

## Option B: Next.js Proxy (ALTERNATIVE)

**Architecture:**
```
Browser → localhost:3000/api → Next.js Server → Azure Backend (HTTPS)
Cookie flows back through proxy → Browser sees same-origin
```

**Implementation:**
```javascript
// next.config.js
async rewrites() {
  if (process.env.NODE_ENV === 'development') {
    return [
      {
        source: '/api/:path*',
        destination: 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/:path*',
      },
    ];
  }
  return [];
}
```

```bash
# web/.env.local
NEXT_PUBLIC_API_URL=/api  # Same-origin from browser perspective
```

**Benefits:**
- ✅ Test against staging backend
- ✅ No local backend setup needed
- ✅ Same-origin from browser perspective
- ✅ Cookies handled automatically
- ✅ Matches production data

**Trade-offs:**
- Requires Next.js restart to change proxy config
- Network dependency on staging
- Harder to debug backend issues
- Slightly slower than local development

---

## Cookie Settings by Environment

| Environment | Connection | Secure | SameSite | Domain | Use Case |
|-------------|-----------|--------|----------|--------|----------|
| **Local Dev (Option A)** | HTTP localhost | `false` | `Lax` | `null` | Same-origin development |
| **Local Dev (Option B)** | HTTP localhost (proxied) | `false` | `Lax` | `null` | Proxy to staging |
| **Staging (Azure)** | HTTPS Azure | `true` | `None` | `null` | Cross-origin testing |
| **Production** | HTTPS Azure | `true` | `None` | `.lankaconnect.com` | Production traffic |

---

## Security Considerations

### HttpOnly Cookie (IMPLEMENTED)
- ✅ Prevents JavaScript access to refresh token
- ✅ Mitigates XSS attacks
- ✅ Browser automatically includes in requests

### Secure Flag
- ✅ HTTPS-only transmission (staging/prod)
- ✅ Prevents man-in-the-middle attacks
- ✅ Only disabled for local HTTP development

### SameSite Attribute
- **Lax (Local)**: Allows same-origin requests only
- **None (Staging/Prod)**: Required for cross-origin with Secure flag
- ✅ Provides CSRF protection

### Domain Attribute
- **Development/Staging**: No domain (locked to exact host)
- **Production**: `.lankaconnect.com` (allows subdomain sharing if needed)

---

## Testing Strategy

### 1. Local Development (Option A)
```bash
# Start backend
cd src/LankaConnect.Api
dotnet run

# Start frontend (new terminal)
cd web
npm run dev

# Test flow
1. Login → Check DevTools → Cookie "refreshToken" should appear
2. Wait 10+ minutes OR manually expire token
3. Make authenticated request → Should auto-refresh
4. Check logs → Should see "Attempting token refresh" and success
```

### 2. Staging Testing
```bash
# Deploy backend with updated cookie logic
# Frontend points to staging API

# Test flow
1. Login → Check backend logs for cookie settings
2. Verify cookie attributes in logs: Secure=true, SameSite=None, IsHttps=true
3. Check if cookie appears in browser DevTools
4. Test refresh flow
```

### 3. Verification Checklist
- [ ] Cookie appears in DevTools → Application → Cookies
- [ ] Cookie has correct attributes (Secure, HttpOnly, SameSite)
- [ ] Login sets cookie
- [ ] Refresh endpoint receives cookie
- [ ] Token refresh succeeds
- [ ] Auto-logout disabled
- [ ] Manual logout clears cookie

---

## Implementation Status

### Completed
- ✅ Fixed `SetRefreshTokenCookie()` logic
- ✅ Fixed `ClearRefreshTokenCookie()` logic
- ✅ Enhanced logging for debugging
- ✅ Architecture decision documented

### Next Steps (Choose One)

**Recommended: Option A - Local Backend**
1. Update `web/.env.local` → `NEXT_PUBLIC_API_URL=http://localhost:5000/api`
2. Test login flow locally
3. Verify cookie is set and refresh works
4. Document in developer setup guide

**Alternative: Option B - Next.js Proxy**
1. Update `web/next.config.js` with rewrites
2. Update `web/.env.local` → `NEXT_PUBLIC_API_URL=/api`
3. Restart Next.js dev server
4. Test login flow through proxy
5. Verify cookie handling

### Deployment
1. Commit backend changes to git
2. Deploy to staging
3. Test with frontend (using chosen option)
4. Verify logs show correct cookie settings
5. Deploy to production after successful testing

---

## References

- **MDN Web Docs**: [HTTP Cookies](https://developer.mozilla.org/en-US/docs/Web/HTTP/Cookies)
- **OWASP**: [Session Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html)
- **ASP.NET Core**: [Cookie Policy](https://learn.microsoft.com/en-us/aspnet/core/security/gdpr)
- **Next.js**: [Rewrites](https://nextjs.org/docs/app/api-reference/next-config-js/rewrites)

---

## Lessons Learned

1. **Don't assume environment names match connection protocols**
   - Azure staging uses HTTPS despite being "non-production"
   - Always check `Request.IsHttps` for cookie security decisions

2. **Browser cookie blocking is silent**
   - Login can succeed (JSON response) while cookie is dropped
   - Always verify cookies in DevTools during development
   - Enhanced backend logging is critical for debugging

3. **Cross-origin cookies are complex**
   - Same-origin architecture is simpler and more secure
   - Proxy pattern is good alternative when remote backend needed
   - Production should use same-origin wherever possible

4. **Security vs. Convenience trade-offs**
   - HttpOnly cookies are more secure but harder to debug
   - Secure flag is essential but complicates local development
   - Documentation of each environment's configuration is crucial
