# Phase 6A.10: Authorization 500 Error Fix - Deployment Instructions

**Date:** 2025-11-28
**Issue:** Event creation fails with 500 error - CORS headers missing on error responses
**Phase:** 6A.10 - Authorization Policy Cache & CORS Error Handling

---

## Changes Summary

### Files Modified
1. **Program.cs** - Added CORS error handling middleware to ensure headers on ALL responses
2. **AuthenticationExtensions.cs** - Registered diagnostic authorization logging handler
3. **LoggingAuthorizationHandler.cs** (NEW) - Created diagnostic handler for troubleshooting

### Root Cause
**Authorization policy cache issue in Azure Container App:**
- Policy definitions updated in code (included `EventOrganizerAndBusinessOwner`)
- Container app did not restart after deployment
- Old cached policy (without new role) still in memory
- Authorization fails → Pipeline short-circuits → CORS headers not added

**Secondary issue - CORS on error responses:**
- Standard `UseCors()` middleware conditionally adds headers
- When authorization throws 500 error, pipeline short-circuits
- Response returns without CORS headers
- Browser shows "CORS policy" error instead of real 500 error

---

## Solution Implemented

### Fix #1: CORS Error Handling Middleware
**Added custom middleware BEFORE standard UseCors() to guarantee headers on ALL responses.**

**Location:** `Program.cs` line 218-249

**What it does:**
- Runs BEFORE all other middleware (except routing)
- Checks request Origin header against allowed origins
- ALWAYS adds CORS headers to response (success or error)
- Handles OPTIONS preflight immediately with 204 No Content
- Ensures CORS headers present even when authorization fails with 500

**Benefits:**
- Browser can see actual error (500) instead of CORS error
- Frontend error handling can work properly
- Debugging becomes much easier

### Fix #2: Diagnostic Authorization Handler
**Added logging handler to diagnose authorization failures.**

**Location:** `LoggingAuthorizationHandler.cs` (new file)

**What it logs:**
- User ID, email, authentication status
- All user claims from JWT token
- Authorization requirements being checked
- Required roles vs actual user role
- Pending (unsatisfied) requirements

**Benefits:**
- Immediate visibility into authorization failures
- Can see if role claim is present in JWT
- Can see if policy requirements match
- Helps diagnose cache issues

---

## Deployment Steps

### Prerequisites
- Azure CLI installed and authenticated
- GitHub repository access
- Access to Azure Portal

### Step 1: Restart Azure Container App (CRITICAL)
**This clears the cached authorization policies.**

```bash
# Azure CLI command
az containerapp restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg

# Wait for restart to complete (check health)
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --query "properties.runningStatus"
```

**Expected Output:** `"Running"`

**Why this is critical:**
- ASP.NET Core caches authorization policies in memory at startup
- Code was deployed but container may not have restarted
- Restart forces reload of updated policy definitions

### Step 2: Deploy Code Changes

#### Option A: GitHub Actions (Recommended)
```bash
# Commit changes
git add .
git commit -m "fix(auth): Add CORS error handling middleware and diagnostic logging

- Add custom CORS middleware to ensure headers on ALL responses
- Create LoggingAuthorizationHandler for authorization diagnostics
- Fix authorization 500 error masquerading as CORS error
- Phase 6A.10"

# Push to trigger deployment
git push origin develop
```

**Wait for deployment:**
1. Go to GitHub Actions tab
2. Watch "Deploy to Azure Container Apps" workflow
3. Verify completion (green checkmark)

#### Option B: Manual Deployment
```bash
# Build and push Docker image
cd src/LankaConnect.API
docker build -t lankaconnect-api:staging .
docker tag lankaconnect-api:staging <your-registry>.azurecr.io/lankaconnect-api:staging
docker push <your-registry>.azurecr.io/lankaconnect-api:staging

# Update container app
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --image <your-registry>.azurecr.io/lankaconnect-api:staging
```

### Step 3: Verify Deployment
```bash
# Check container app status
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --query "properties.{Status:runningStatus,Replicas:template.scale.minReplicas,Image:template.containers[0].image}"

# Check health endpoint
curl https://lankaconnect-api-staging.azurecontainerapps.io/health
```

**Expected Response:**
```json
{
  "Status": "Healthy",
  "Checks": [
    {"Name": "PostgreSQL Database", "Status": "Healthy"},
    {"Name": "Redis Cache", "Status": "Healthy"},
    {"Name": "EF Core DbContext", "Status": "Healthy"}
  ]
}
```

---

## Testing Procedure

### Test 1: User Login (Get Fresh JWT)
**This ensures JWT has role claim from restarted API.**

1. Open browser (Chrome/Edge with DevTools)
2. Navigate to `http://localhost:3000`
3. Open DevTools → Application → Local Storage
4. Clear `accessToken` and `refreshToken`
5. Log out if logged in
6. Log in with Event Organizer account
7. Copy new `accessToken` from Local Storage

**Verify JWT contents:**
1. Go to https://jwt.io
2. Paste access token
3. Check payload for:
   ```json
   {
     "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "EventOrganizerAndBusinessOwner",
     "http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier": "...",
     "http://schemas.microsoft.com/ws/2008/06/identity/claims/emailaddress": "..."
   }
   ```

### Test 2: Event Creation
**This tests authorization policy evaluation.**

1. Navigate to Events page
2. Click "Create Event" button
3. Fill out event form:
   - Title: "Test Event Authorization Fix"
   - Description: "Testing Phase 6A.10 fix"
   - Category: Community
   - Start Date: Tomorrow
   - End Date: Tomorrow + 1 day
   - Location: Test Location
4. Click "Create Event"

**Expected Result:** Event created successfully (200 OK)

### Test 3: Check CORS Headers (Success Case)
**This verifies CORS headers present on successful requests.**

1. Open DevTools → Network tab
2. Filter: "events"
3. Create event (as above)
4. Click on POST request to `/api/events`
5. Check Response Headers:
   ```
   Access-Control-Allow-Origin: http://localhost:3000
   Access-Control-Allow-Credentials: true
   Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS, PATCH
   Access-Control-Allow-Headers: Content-Type, Authorization, X-Correlation-ID, X-Request-ID
   ```

### Test 4: Check CORS Headers (Error Case)
**This verifies CORS headers present even on 500 errors.**

**Simulate authorization failure:**
1. Open DevTools → Application → Local Storage
2. Modify `accessToken` (corrupt it by changing last character)
3. Try to create event
4. Check Network tab → Response Headers

**Expected Result:**
- Status: 401 Unauthorized (or 500 if different error)
- **CORS headers STILL PRESENT:**
  ```
  Access-Control-Allow-Origin: http://localhost:3000
  Access-Control-Allow-Credentials: true
  ```

### Test 5: Check Authorization Logs
**This verifies diagnostic logging is working.**

**Azure Portal:**
1. Go to Azure Portal → Container Apps → lankaconnect-api-staging
2. Click "Log stream" (left menu)
3. Filter by "Authorization"
4. Create event while watching logs

**Expected Log Output:**
```
[INFO] Authorization evaluation - UserId: xxx, Email: test@example.com, Role: EventOrganizerAndBusinessOwner, IsAuthenticated: True
[INFO] Authorization requirement: RolesAuthorizationRequirement - Microsoft.AspNetCore.Authorization.Infrastructure.RolesAuthorizationRequirement
[INFO] Required roles: EventOrganizer, EventOrganizerAndBusinessOwner, Admin, AdminManager, User has role: EventOrganizerAndBusinessOwner
```

**If authorization FAILS, you'll see:**
```
[WARN] Pending (unsatisfied) requirements: RolesAuthorizationRequirement
```

---

## Troubleshooting

### Issue: Still getting 500 error after restart

**Check #1: Container actually restarted?**
```bash
# Check restart time
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --query "[0].{Name:name,Created:properties.createdTime,Active:properties.active}"
```

**Check #2: New code deployed?**
```bash
# Check image tag
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --query "properties.template.containers[0].image"
```

**Check #3: Authorization logs show role claim?**
```bash
# Stream logs
az containerapp logs tail \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --follow
```

Look for: `Role: EventOrganizerAndBusinessOwner` in logs

### Issue: CORS headers missing on error responses

**Check #1: Origin header in request?**
- Open DevTools → Network → Request Headers
- Verify `Origin: http://localhost:3000` is present

**Check #2: Custom middleware registered?**
- Check Program.cs line 218-249
- Ensure middleware is BEFORE `UseCors()`

**Check #3: Allowed origins match?**
- Custom middleware: `http://localhost:3000`
- Request Origin: `http://localhost:3000` (must match exactly)

### Issue: Authorization still failing

**Check #1: JWT has role claim?**
1. Copy access token from browser
2. Decode at jwt.io
3. Look for claim with type: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`
4. Value should be: `EventOrganizerAndBusinessOwner`

**Check #2: Policy definition correct?**
```bash
# Check code in AuthenticationExtensions.cs line 121-125
# Should include: UserRole.EventOrganizerAndBusinessOwner.ToString()
```

**Check #3: User actually has correct role in database?**
```sql
-- Connect to PostgreSQL
SELECT id, email, role, is_active
FROM users
WHERE email = 'your-test-email@example.com';
```

Expected: `role = 4` (EventOrganizerAndBusinessOwner enum value)

---

## Rollback Procedure

**If deployment causes issues:**

### Step 1: Revert code changes
```bash
git revert HEAD
git push origin develop
```

### Step 2: Redeploy previous version
```bash
# List previous revisions
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg

# Activate previous revision
az containerapp revision activate \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-rg \
  --revision <previous-revision-name>
```

### Step 3: Verify rollback
```bash
curl https://lankaconnect-api-staging.azurecontainerapps.io/health
```

---

## Success Criteria

- [ ] Azure Container App restarted successfully
- [ ] New code deployed to staging environment
- [ ] Health check endpoint returns 200 OK
- [ ] User can log in and get fresh JWT token
- [ ] JWT token contains role claim: `EventOrganizerAndBusinessOwner`
- [ ] Event creation succeeds with 200 OK
- [ ] CORS headers present on successful requests
- [ ] CORS headers present on error responses (test with corrupted JWT)
- [ ] Authorization logs show user role and requirements
- [ ] No CORS errors in browser console
- [ ] Browser shows actual HTTP status codes (500, 401, etc.) instead of CORS errors

---

## Post-Deployment Tasks

### Update Documentation
1. Update PROGRESS_TRACKER.md with Phase 6A.10 completion
2. Update STREAMLINED_ACTION_PLAN.md with fix details
3. Create PHASE_6A_10_SUMMARY.md

### Monitor Production
1. Set up Azure Application Insights alerts for:
   - 500 errors on `/api/events` endpoint
   - Authorization failures (log pattern: "Pending (unsatisfied) requirements")
   - CORS errors (log pattern: "CORS policy")

2. Monitor for 24 hours:
   - Check error rates in Azure Portal
   - Review authorization logs for anomalies
   - Verify no increase in failed requests

### Technical Debt
1. Create integration tests for authorization policies
2. Add policy version checking to health endpoint
3. Implement authorization policy cache invalidation strategy
4. Document middleware ordering requirements in architecture guide

---

## Related Documents

- [Root Cause Analysis](./AUTHORIZATION_500_ERROR_ROOT_CAUSE_ANALYSIS.md)
- [Phase 6A Master Index](./PHASE_6A_MASTER_INDEX.md)
- [Progress Tracker](./PROGRESS_TRACKER.md)
- [Streamlined Action Plan](./STREAMLINED_ACTION_PLAN.md)

---

## Contact & Support

**If issues persist:**
1. Check Azure Portal logs
2. Review root cause analysis document
3. Contact development team with:
   - JWT token (redacted sensitive info)
   - Network request/response headers
   - Azure log stream output
   - User account role from database

---

**Deployment Prepared By:** System Architecture Designer (Claude Code)
**Review Status:** Ready for deployment
**Risk Level:** Low (backward compatible, adds diagnostic logging)
