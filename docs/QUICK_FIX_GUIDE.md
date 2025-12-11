# Quick Fix Guide - Authorization 500 Error

**TL;DR:** Restart Azure Container App + Deploy code + Test

---

## Immediate Actions (5 minutes)

### 1. Restart Container App
```bash
az containerapp restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg
```

**Wait 1 minute for restart to complete.**

### 2. Deploy Code
```bash
# From project root
git add .
git commit -m "fix(auth): Add CORS error handling and diagnostic logging - Phase 6A.10"
git push origin develop
```

**Wait 3-5 minutes for GitHub Actions to complete.**

### 3. Test Event Creation

1. Clear browser localStorage (accessToken, refreshToken)
2. Log out and log back in (gets fresh JWT from restarted API)
3. Navigate to Events page
4. Click "Create Event"
5. Fill form and submit

**Expected:** Event created successfully (200 OK)

---

## What Was Fixed

### Root Cause
- Authorization policy was cached in Azure Container App
- Old policy didn't include `EventOrganizerAndBusinessOwner` role
- CORS headers missing on 500 error responses

### Solution
1. **Custom CORS middleware** - Ensures headers on ALL responses (including errors)
2. **Diagnostic logging** - Shows authorization evaluation in real-time
3. **Container restart** - Clears cached authorization policies

---

## If Still Failing

### Check 1: JWT Token Has Role Claim
```
1. Copy accessToken from browser localStorage
2. Go to https://jwt.io
3. Paste token
4. Look for: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "EventOrganizerAndBusinessOwner"
```

**If missing:** User doesn't have correct role in database.

### Check 2: Authorization Logs
```bash
az containerapp logs tail \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --follow
```

**Look for:** `"Role: EventOrganizerAndBusinessOwner"` in logs

**If showing different role:** Database user role doesn't match expected value.

### Check 3: CORS Headers Present
```
1. Open DevTools → Network
2. Create event
3. Click POST request to /api/events
4. Check Response Headers
5. Verify: "Access-Control-Allow-Origin: http://localhost:3000"
```

**If missing:** Custom middleware not deployed or not running.

---

## Architecture Summary

```
Browser → Custom CORS Middleware (ALWAYS adds headers)
       → Standard CORS
       → Authentication (validates JWT)
       → Authorization (checks policy - MAY FAIL)
       → Controller
```

**Key:** Custom CORS middleware GUARANTEES headers even when authorization fails.

---

## Rollback

```bash
# Revert code
git revert HEAD
git push origin develop

# Or activate previous revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --revision <previous-revision-name>
```

---

## Detailed Documentation

- [Root Cause Analysis](./AUTHORIZATION_500_ERROR_ROOT_CAUSE_ANALYSIS.md)
- [Deployment Instructions](./PHASE_6A_10_DEPLOYMENT_INSTRUCTIONS.md)
- [Phase Summary](./PHASE_6A_10_SUMMARY.md)
