# Refresh Token Cookie Flow Diagrams

## Problem: Cross-Origin Cookie Blocking

### Broken Flow (Before Fix)

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: User Login Request                                  │
└─────────────────────────────────────────────────────────────┘

Frontend (HTTP localhost:3000)
    │
    │ POST /api/Auth/login
    │ { email, password }
    │ Origin: http://localhost:3000
    ▼
Backend (HTTPS *.azurecontainerapps.io)
    │
    │ ✅ CORS allows origin
    │ ✅ Credentials valid
    │ ✅ Generate access token + refresh token
    │
    │ ❌ MISTAKE: isLocalDevelopment = true (because Staging)
    │ ❌ Sets: Secure=false (wrong for HTTPS!)
    │
    │ Set-Cookie: refreshToken=abc123;
    │             HttpOnly; Secure=false; ← WRONG!
    │             SameSite=Lax; Path=/
    ▼
Browser
    │
    │ 🚫 REJECTED: "Cookie has Secure=false but
    │              came from HTTPS - dropping it"
    │
    │ ✅ Accepts JSON response (200 OK)
    │ ❌ Silently drops cookie
    ▼
Frontend receives:
    ✅ { accessToken, user } → Login appears successful
    ❌ No cookie stored → But refresh will fail later


┌─────────────────────────────────────────────────────────────┐
│ Step 2: Token Expires (10 minutes later)                    │
└─────────────────────────────────────────────────────────────┘

Frontend detects 401 Unauthorized
    │
    │ POST /api/Auth/refresh
    │ (No cookies sent - never had one!)
    ▼
Backend
    │
    │ var refreshToken = Request.Cookies["refreshToken"];
    │ → NULL (no cookie received)
    │
    │ return BadRequest("Refresh token is required");
    ▼
Frontend receives: 400 Bad Request
    │
    │ Refresh failed → Logout user
    ▼
User Experience: "Why am I logged out?"
```

---

## Solution: Protocol-Based Cookie Settings

### Working Flow (After Fix)

```
┌─────────────────────────────────────────────────────────────┐
│ Option A: Local Backend (Same-Origin)                       │
└─────────────────────────────────────────────────────────────┘

Frontend (HTTP localhost:3000)
    │
    │ POST /api/Auth/login
    │ Origin: http://localhost:3000
    ▼
Backend (HTTP localhost:5000)
    │
    │ ✅ Check: Request.IsHttps = false
    │ ✅ Check: _env.IsDevelopment() = true
    │ ✅ Result: isHttpOnly = true
    │
    │ Set-Cookie: refreshToken=abc123;
    │             HttpOnly; Secure=false; ✅ Correct!
    │             SameSite=Lax; Path=/
    ▼
Browser
    │
    │ ✅ ACCEPTED: HTTP cookie from HTTP backend
    │ ✅ Same-origin (localhost → localhost)
    │ ✅ Cookie stored
    ▼
Frontend: ✅ Login successful + ✅ Cookie set


--- 10 minutes later ---

Frontend detects 401
    │
    │ POST /api/Auth/refresh
    │ Cookie: refreshToken=abc123 ✅ Sent!
    ▼
Backend
    │
    │ var refreshToken = Request.Cookies["refreshToken"];
    │ → "abc123" ✅ Found!
    │
    │ Validate and generate new access token
    │ return Ok({ accessToken: "new_token" })
    ▼
Frontend: ✅ Token refreshed → ✅ User stays logged in

User Experience: Seamless (no logout)


┌─────────────────────────────────────────────────────────────┐
│ Option B: Next.js Proxy (Same-Origin via Proxy)             │
└─────────────────────────────────────────────────────────────┘

Browser (localhost:3000)
    │
    │ POST /api/Auth/login
    │ Origin: http://localhost:3000
    ▼
Next.js Server (localhost:3000)
    │
    │ Proxy rewrites request →
    ▼
Backend (HTTPS *.azurecontainerapps.io)
    │
    │ ✅ Check: Request.IsHttps = true
    │ ✅ Check: _env.IsDevelopment() = false (Staging)
    │ ✅ Result: isHttpOnly = false
    │
    │ Set-Cookie: refreshToken=abc123;
    │             HttpOnly; Secure=true; ✅ Correct!
    │             SameSite=None; Path=/
    ▼
Next.js Server
    │
    │ Proxies response back with cookie →
    ▼
Browser
    │
    │ ✅ ACCEPTED: Sees response from same-origin (localhost:3000)
    │ ✅ Cookie stored under localhost:3000
    ▼
Frontend: ✅ Login successful + ✅ Cookie set


--- 10 minutes later ---

Browser
    │
    │ POST /api/Auth/refresh
    │ Cookie: refreshToken=abc123 (to localhost:3000)
    ▼
Next.js Server
    │
    │ Proxy forwards with cookie →
    ▼
Backend
    │
    │ ✅ Receives cookie via proxy
    │ Validate and refresh
    │ return Ok({ accessToken })
    ▼
Next.js Server → Browser: ✅ New token

User Experience: Seamless


┌─────────────────────────────────────────────────────────────┐
│ Staging/Production (Same-Origin)                            │
└─────────────────────────────────────────────────────────────┘

Frontend (HTTPS app.lankaconnect.com)
    │
    │ POST /api/Auth/login
    │ Origin: https://app.lankaconnect.com
    ▼
Backend (HTTPS api.lankaconnect.com)
    │
    │ ✅ Check: Request.IsHttps = true
    │ ✅ Check: _env.IsProduction() = true
    │ ✅ Result: isHttpOnly = false
    │
    │ Set-Cookie: refreshToken=abc123;
    │             HttpOnly; Secure=true; ✅ Correct!
    │             SameSite=None; Path=/;
    │             Domain=.lankaconnect.com ✅ Share across subdomains
    ▼
Browser
    │
    │ ✅ ACCEPTED: HTTPS cookie from HTTPS backend
    │ ✅ Stored for .lankaconnect.com domain
    │ ✅ Sent with requests to api.lankaconnect.com
    ▼
Frontend: ✅ Login successful + ✅ Cookie set


--- Token refresh flow ---

Browser automatically includes cookie with all
requests to *.lankaconnect.com

✅ Refresh works seamlessly
```

---

## Cookie Decision Logic

### New Implementation (Correct)

```
┌─────────────────────────────────────────────┐
│ SetRefreshTokenCookie() Decision Tree       │
└─────────────────────────────────────────────┘

                    Start
                      │
                      ▼
            Is Development Environment?
                      │
        ┌─────────────┴─────────────┐
       No                          Yes
        │                            │
        ▼                            ▼
   isHttpOnly = false      Is Request HTTPS?
        │                            │
        │                  ┌─────────┴─────────┐
        │                 Yes                  No
        │                  │                    │
        │                  ▼                    ▼
        │         isHttpOnly = false   isHttpOnly = true
        │                  │                    │
        └──────────────────┴────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │ Set Cookie Options:    │
              │                        │
              │ Secure = !isHttpOnly   │
              │                        │
              │ SameSite = isHttpOnly  │
              │   ? Lax : None         │
              │                        │
              │ HttpOnly = true        │
              │ Path = /               │
              │ Domain = Production?   │
              │   .domain : null       │
              └────────────────────────┘
                           │
                           ▼
              Response.Cookies.Append()


Examples:

┌────────────────┬──────────┬────────┬──────────┬──────────┐
│ Environment    │ Protocol │ Result │ Secure   │ SameSite │
├────────────────┼──────────┼────────┼──────────┼──────────┤
│ Development    │ HTTP     │ true   │ false    │ Lax      │
│ Development    │ HTTPS    │ false  │ true     │ None     │
│ Staging        │ HTTP     │ false  │ true     │ None     │
│ Staging        │ HTTPS    │ false  │ true     │ None     │
│ Production     │ HTTPS    │ false  │ true     │ None     │
└────────────────┴──────────┴────────┴──────────┴──────────┘
```

---

## Browser Cookie Acceptance Rules

```
┌─────────────────────────────────────────────────────────────┐
│ Browser Cookie Decision Matrix                              │
└─────────────────────────────────────────────────────────────┘

Request   │ Cookie    │ Cookie      │ Browser
Protocol  │ Secure    │ SameSite    │ Decision
──────────┼───────────┼─────────────┼─────────────────────
HTTP      │ false     │ Lax         │ ✅ Accept (same-origin)
HTTP      │ false     │ None        │ ❌ Reject (None requires Secure)
HTTP      │ true      │ Any         │ ❌ Reject (can't send Secure over HTTP)
HTTPS     │ false     │ Any         │ ⚠️  Accept but won't send on future requests
HTTPS     │ true      │ Lax         │ ✅ Accept (same-origin)
HTTPS     │ true      │ None        │ ✅ Accept (cross-origin allowed)

Key Rules:
1. Secure=true cookies CANNOT be sent over HTTP
2. SameSite=None REQUIRES Secure=true
3. Cross-origin requests REQUIRE SameSite=None + Secure=true
4. HttpOnly prevents JavaScript access (security)
```

---

## Sequence Diagram: Full Authentication Flow

```
┌──────────┐         ┌──────────┐         ┌──────────┐         ┌─────────┐
│ Browser  │         │ Frontend │         │ Backend  │         │ Browser │
│          │         │   (App)  │         │   (API)  │         │ Storage │
└────┬─────┘         └────┬─────┘         └────┬─────┘         └────┬────┘
     │                    │                     │                    │
     │  1. Login Request  │                     │                    │
     │───────────────────>│                     │                    │
     │                    │                     │                    │
     │                    │ 2. POST /api/Auth/login                  │
     │                    │     { email, password }                  │
     │                    │────────────────────>│                    │
     │                    │                     │                    │
     │                    │                     │ 3. Validate user   │
     │                    │                     │    Generate tokens │
     │                    │                     │                    │
     │                    │                     │ 4. Check protocol  │
     │                    │                     │    Set cookie opts │
     │                    │                     │                    │
     │                    │ 5. 200 OK           │                    │
     │                    │    { accessToken }  │                    │
     │                    │    Set-Cookie: refreshToken             │
     │                    │<────────────────────│                    │
     │                    │                     │                    │
     │                    │ 6. Store accessToken│                    │
     │                    │────────────────────────────────────────>│
     │                    │                     │                    │
     │                    │ 7. Browser stores cookie                │
     │                    │<────────────────────────────────────────│
     │                    │                     │                    │
     │  8. Login Success  │                     │                    │
     │<───────────────────│                     │                    │
     │                    │                     │                    │
     │                    │                     │                    │
     │      ... 10 minutes pass ...             │                    │
     │                    │                     │                    │
     │                    │                     │                    │
     │ 9. API Request     │                     │                    │
     │───────────────────>│                     │                    │
     │                    │                     │                    │
     │                    │ 10. GET /api/some-resource              │
     │                    │     Authorization: Bearer <expired>     │
     │                    │────────────────────>│                    │
     │                    │                     │                    │
     │                    │ 11. 401 Unauthorized│                    │
     │                    │<────────────────────│                    │
     │                    │                     │                    │
     │                    │ 12. Detect 401      │                    │
     │                    │     Attempt refresh │                    │
     │                    │                     │                    │
     │                    │ 13. POST /api/Auth/refresh              │
     │                    │     Cookie: refreshToken ✅             │
     │                    │────────────────────>│                    │
     │                    │                     │                    │
     │                    │                     │ 14. Read cookie    │
     │                    │                     │     Validate token │
     │                    │                     │     Generate new   │
     │                    │                     │                    │
     │                    │ 15. 200 OK          │                    │
     │                    │     { accessToken } │                    │
     │                    │<────────────────────│                    │
     │                    │                     │                    │
     │                    │ 16. Update token    │                    │
     │                    │────────────────────────────────────────>│
     │                    │                     │                    │
     │                    │ 17. Retry original request              │
     │                    │     Authorization: Bearer <new>         │
     │                    │────────────────────>│                    │
     │                    │                     │                    │
     │                    │ 18. 200 OK          │                    │
     │                    │     { data }        │                    │
     │                    │<────────────────────│                    │
     │                    │                     │                    │
     │ 19. Show Data      │                     │                    │
     │<───────────────────│                     │                    │
     │                    │                     │                    │

User Experience: Seamless (no logout, no interruption)
```

---

## Why Previous Implementation Failed

```
┌─────────────────────────────────────────────────────────────┐
│ Problem: Environment-Based Logic                            │
└─────────────────────────────────────────────────────────────┘

Old Code:
    var isLocalDevelopment = _env.IsDevelopment() || _env.IsStaging();
    Secure = !isLocalDevelopment;

Environment Mapping:
    Development → isLocalDevelopment = true → Secure = false
    Staging     → isLocalDevelopment = true → Secure = false ❌
    Production  → isLocalDevelopment = false → Secure = true

Reality:
    Development (local) → HTTP  ✅ Secure=false is correct
    Staging (Azure)     → HTTPS ❌ Secure=false is WRONG!
    Production (Azure)  → HTTPS ✅ Secure=true is correct

Result:
    Staging cookies rejected by browser or not sent on subsequent requests


┌─────────────────────────────────────────────────────────────┐
│ Solution: Protocol-Based Logic                              │
└─────────────────────────────────────────────────────────────┘

New Code:
    var isHttpOnly = _env.IsDevelopment() && !Request.IsHttps;
    Secure = !isHttpOnly;

Protocol Detection:
    Development + HTTP  → isHttpOnly = true  → Secure = false ✅
    Development + HTTPS → isHttpOnly = false → Secure = true  ✅
    Staging + HTTPS     → isHttpOnly = false → Secure = true  ✅
    Production + HTTPS  → isHttpOnly = false → Secure = true  ✅

Result:
    All environments configure cookies correctly based on ACTUAL protocol
```

---

## Testing Checklist

### Visual Verification in Browser DevTools

```
1. Login to application

2. Open DevTools (F12) → Application tab → Cookies

3. Expand your domain (localhost:3000 or staging URL)

4. Look for cookie named "refreshToken"

   ✅ Success looks like:
   ┌────────────────────────────────────────────────────┐
   │ Name           │ Value                             │
   ├────────────────┼───────────────────────────────────┤
   │ refreshToken   │ eyJhbGci... (long token string)   │
   │                │                                   │
   │ Domain         │ localhost (or your domain)        │
   │ Path           │ /                                 │
   │ Expires        │ [7 or 30 days from now]           │
   │ Size           │ ~300-500 bytes                    │
   │ HttpOnly       │ ✓ (checkmark)                     │
   │ Secure         │ ✓ if HTTPS, blank if HTTP         │
   │ SameSite       │ Lax (HTTP) or None (HTTPS)        │
   │ Priority       │ Medium                            │
   └────────────────────────────────────────────────────┘

   ❌ Failure looks like:
   - No "refreshToken" cookie appears
   - Cookie appears then disappears
   - Cookie has wrong attributes

5. Test refresh: Wait 10+ minutes or manually expire token

6. Perform any authenticated action

7. Check Console tab:
   ✅ Success: "🔄 Token refreshed successfully"
   ❌ Failure: "❌ Token refresh failed" or 401 error

8. Check Network tab → Filter by "refresh":
   ✅ Success: POST /api/Auth/refresh → 200 OK
   ❌ Failure: POST /api/Auth/refresh → 400 Bad Request
```

This comprehensive diagram explains the entire flow, decision logic, and verification process for the refresh token cookie implementation.
