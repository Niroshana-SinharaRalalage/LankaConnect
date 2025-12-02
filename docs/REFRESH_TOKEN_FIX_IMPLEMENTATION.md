# Refresh Token Cookie Fix - Implementation Guide

**Date:** 2025-12-01
**Status:** Ready for Testing

---

## Quick Summary

Fixed automatic logout issue caused by refresh token cookies not being set correctly. Backend cookie logic now checks actual connection protocol instead of just environment name.

---

## What Was Fixed

### Backend Changes (AuthController.cs)

**Before:**
```csharp
// ❌ WRONG: Treats staging as local development
var isLocalDevelopment = _env.IsDevelopment() || _env.IsStaging();
Secure = !isLocalDevelopment;  // FALSE in staging (but Azure uses HTTPS!)
```

**After:**
```csharp
// ✅ CORRECT: Checks actual connection protocol
var isHttpOnly = _env.IsDevelopment() && !Request.IsHttps;
Secure = !isHttpOnly;  // TRUE for all HTTPS (staging/prod), FALSE only for local HTTP
```

**Files Modified:**
- `src/LankaConnect.API/Controllers/AuthController.cs`
  - `SetRefreshTokenCookie()` method
  - `ClearRefreshTokenCookie()` method

---

## Testing Instructions

### Option A: Local Backend (Recommended)

1. **Update frontend configuration:**
   ```bash
   # Edit web/.env.local
   NEXT_PUBLIC_API_URL=http://localhost:5000/api
   ```

2. **Start backend:**
   ```bash
   cd src/LankaConnect.Api
   dotnet run
   ```

3. **Start frontend (new terminal):**
   ```bash
   cd web
   npm run dev
   ```

4. **Test login flow:**
   - Open http://localhost:3000
   - Login with your credentials
   - Open DevTools → Application → Cookies → http://localhost:3000
   - **Verify:** `refreshToken` cookie should be visible

5. **Test refresh flow:**
   - Stay logged in for 10+ minutes
   - Perform any authenticated action (view profile, etc.)
   - **Verify:** Should NOT be logged out
   - Check browser console for "🔄 Token refreshed successfully"

6. **Check backend logs:**
   ```
   Setting refresh token cookie: Secure=False, SameSite=Lax,
   Environment=Development, IsHttps=False
   ```

---

### Option B: Next.js Proxy to Staging

If you prefer to keep backend on Azure staging:

1. **Update next.config.js:**
   ```javascript
   // Add to next.config.js
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

2. **Update frontend configuration:**
   ```bash
   # Edit web/.env.local
   NEXT_PUBLIC_API_URL=/api
   ```

3. **Restart Next.js dev server:**
   ```bash
   cd web
   npm run dev
   ```

4. **Test same as Option A**

---

## Expected Behavior

### ✅ Success Indicators

1. **Cookie is set:**
   - DevTools → Application → Cookies → `refreshToken` appears
   - Cookie attributes: `HttpOnly`, `Secure` (if HTTPS), `SameSite`

2. **Login works:**
   - Successful login redirects to dashboard
   - User stays logged in

3. **Refresh works:**
   - After 10+ minutes, user is NOT logged out
   - Authenticated requests continue to work
   - Console shows: "🔄 Token refreshed successfully"

4. **Backend logs show:**
   ```
   Setting refresh token cookie: Secure=True/False, SameSite=Lax/None,
   IsHttps=True/False, Environment=Development/Staging
   ```

### ❌ Failure Indicators

1. **No cookie in DevTools**
   → Cookie is being blocked by browser
   → Check frontend URL (HTTP vs HTTPS mismatch)

2. **400 error on refresh endpoint**
   → Backend not receiving cookie
   → Check `withCredentials: true` in API client

3. **Still logged out after 10 minutes**
   → Refresh token flow not working
   → Check browser console for errors
   → Check backend logs for refresh attempt

---

## Environment-Specific Cookie Settings

| Environment | Secure | SameSite | Expected Protocol |
|-------------|--------|----------|-------------------|
| **Local Dev** | `false` | `Lax` | HTTP localhost |
| **Staging** | `true` | `None` | HTTPS Azure |
| **Production** | `true` | `None` | HTTPS Azure |

---

## Troubleshooting

### Issue: Cookie not visible in DevTools

**Possible causes:**
1. Frontend is HTTP, backend is HTTPS → Browser blocks cookie
2. Cookie has wrong `Domain` attribute
3. Cookie has wrong `SameSite` attribute

**Solution:**
- Use **Option A** (local backend) for development
- OR use **Option B** (Next.js proxy) to create same-origin

### Issue: 400 error on /api/Auth/refresh

**Cause:** Backend not receiving `refreshToken` cookie

**Check:**
1. Cookie is in DevTools under correct origin
2. Frontend API client has `withCredentials: true` ✅ (already set)
3. CORS allows credentials ✅ (already configured)

**Solution:**
- Verify cookie is set on correct domain
- Check browser Network tab → Request → Cookies sent

### Issue: Backend logs show wrong Secure/SameSite

**Check backend logs:**
```
Setting refresh token cookie: Secure=?, SameSite=?, IsHttps=?
```

**Expected values:**
- Local HTTP: `Secure=false, SameSite=Lax, IsHttps=false`
- Staging HTTPS: `Secure=true, SameSite=None, IsHttps=true`

**If incorrect:**
- Verify backend code was updated correctly
- Restart backend application
- Check `ASPNETCORE_ENVIRONMENT` variable

---

## Deployment Checklist

### Before Deploying to Staging

- [ ] Backend changes committed to git
- [ ] Tested locally with Option A or B
- [ ] Cookie appears in DevTools
- [ ] Refresh flow works (no logout after 10 minutes)
- [ ] Backend logs show correct cookie settings

### Deploy to Staging

```bash
# Commit changes
git add src/LankaConnect.API/Controllers/AuthController.cs
git add docs/architecture/RefreshTokenCookieArchitecture.md
git add docs/REFRESH_TOKEN_FIX_IMPLEMENTATION.md
git commit -m "fix(auth): Fix refresh token cookie for cross-origin requests

- Check Request.IsHttps instead of just environment name
- Azure staging uses HTTPS, requires Secure=true
- Enhanced logging for debugging cookie settings
- Updated ClearRefreshTokenCookie for consistency"

git push origin develop
```

### Test on Staging

1. Wait for deployment to complete
2. Clear browser cookies for staging domain
3. Login to staging application
4. Check backend logs in Azure:
   ```
   Setting refresh token cookie: Secure=true, SameSite=None, IsHttps=true
   ```
5. Verify refresh flow works

---

## Next Steps

### For Development (Choose One)

**Recommended:**
- [ ] Use Option A (local backend) for daily development
- [ ] Update team documentation with local setup instructions

**Alternative:**
- [ ] Use Option B (Next.js proxy) if local backend is difficult
- [ ] Document proxy configuration for team

### For Production

- [ ] Test on staging with real usage patterns
- [ ] Monitor staging logs for cookie-related issues
- [ ] Deploy to production after 24-48 hours of successful staging
- [ ] Update production documentation

---

## Success Metrics

Track these after deployment:

1. **User Session Duration**
   - Should increase from ~10 minutes to intended duration
   - Monitor logout frequency

2. **Refresh Token API Calls**
   - Should see successful 200 responses
   - Should happen automatically every ~10 minutes

3. **User Complaints**
   - Should eliminate "logged out unexpectedly" reports
   - Monitor support tickets

4. **Backend Logs**
   - All cookie operations should log clearly
   - No 400 errors on /api/Auth/refresh

---

## Rollback Plan

If issues occur after deployment:

1. **Immediate rollback:**
   ```bash
   git revert HEAD
   git push origin develop
   ```

2. **Temporary workaround:**
   - Increase access token lifetime in appsettings.json
   - This gives more time to fix cookie issues
   - NOT a permanent solution (security concern)

3. **Alternative approach:**
   - Consider storing refresh token in localStorage (less secure)
   - Document security trade-offs
   - Implement additional XSS protections

---

## Questions?

Refer to the full architecture document:
- `docs/architecture/RefreshTokenCookieArchitecture.md`

For urgent issues:
- Check backend logs for cookie settings
- Verify browser DevTools shows cookie
- Test with curl to isolate frontend vs backend issues
