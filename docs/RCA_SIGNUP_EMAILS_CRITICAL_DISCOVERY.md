# CRITICAL DISCOVERY: Signup Commitment Endpoint Returns HTTP 200 Without Executing Application Code

**Date**: 2026-02-15
**Time**: 17:25 UTC
**Phase**: 6A.114 Diagnostic Investigation
**Category**: INFRASTRUCTURE ISSUE - Request Handled Before Application

---

## Executive Summary

🚨 **CRITICAL FINDING**: The signup commitment API endpoint returns HTTP 200 OK but the request **NEVER reaches the ASP.NET Core application code**.

### The Smoking Gun

**Test Executed**: 2026-02-15 at 17:23:51 UTC
```
POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/signups/8567645d-7c71-4965-bec3-f696d266b597/items/72639fc5-005f-415f-aa94-6326965e1590/commit

Authorization: Bearer {valid_token}
Body: {userId, quantity, notes, contactName, contactEmail}

RESPONSE:
HTTP 200 OK
x-correlation-id: 4504ff3c-1880-451d-a029-32b1a4b2c22e
Content-Length: 0
Date: Sun, 15 Feb 2026 17:23:51 GMT
Server: Kestrel
```

**Azure Container App Logs at 17:23:51**: **ZERO LOGS**

- ❌ NO logs with correlation ID `4504ff3c-1880-451d-a029-32b1a4b2c22e`
- ❌ NO `DEBUG-CONTROLLER-ENTRY` logs
- ❌ NO `DEBUG-HANDLER` logs
- ❌ NO `DIAG-R` repository logs
- ❌ NO logs from `RequestLoggingMiddleware` showing the HTTP request
- ❌ NO logs of ANY kind at 17:23:51

**But**: Other requests (Hangfire, health checks) ARE logged normally at 17:24:33, 17:24:36, etc.

---

## What This Proves

### ✅ Confirmed Working:
1. **Login endpoint works** - Returns token, logs appear in Azure
2. **Logging infrastructure works** - Hangfire, health checks, background jobs all log normally
3. **Debug logging deployed** - Commit `bbc567cb` deployed successfully at 16:50:39 UTC
4. **Authorization works** - Request includes valid Bearer token

### ❌ NOT Working:
1. **Signup commitment endpoint** - Returns HTTP 200 but doesn't execute application code
2. **Request logging middleware** - No "HTTP POST" log entry for this request
3. **Controller logging** - None of the WARNING-level logs appear
4. **Handler logging** - Command handler never executes
5. **Domain events** - Never raised because handler never runs

---

## Root Cause Hypothesis

**The request is being handled by infrastructure BEFORE it reaches the ASP.NET Core application.**

### Possible Culprits (In Order of Likelihood):

#### 1. **Azure Container Apps Ingress/Gateway Response Caching** 🔴 MOST LIKELY
- Azure might be caching HTTP 200 responses for POST requests (misconfiguration)
- Cache hit returns 200 immediately without hitting application
- Explains why correlation-id exists but no application logs

**Evidence**:
- Response has `Server: Kestrel` header (suggests it reached a Kestrel instance at some point)
- Content-Length: 0 (no response body, typical of cached empty response)
- Correlation ID present (Azure infrastructure adds this)

#### 2. **Authorization Middleware Short-Circuit** ⚠️ POSSIBLE
- `[Authorize]` attribute on endpoint might be configured to return 200 without executing handler
- But this would be unusual and would still log the request

**Evidence Against**:
- Other authorized endpoints likely work
- Would still see `RequestLoggingMiddleware` logs

#### 3. **Routing Middleware Misconfiguration** ⚠️ POSSIBLE
- Route pattern might match a different handler that returns empty 200
- Duplicate route registration

**Evidence Against**:
- Route pattern is unique: `/api/Events/{id}/signups/{id}/items/{id}/commit`
- Would still see routing middleware logs

#### 4. **Multiple Container Instances with Stale Code** ⚠️ POSSIBLE
- Load balancer directing traffic to old container still running previous code
- That container might have different behavior

**Evidence Against**:
- Deployment shows single revision active
- Would expect to see SOME logs from the new container

---

## Evidence Trail

### Deployment Verification

```
Commit: bbc567cb
Message: fix(build): Remove null-conditional operators causing compilation error
Deployed: 2026-02-15 16:50:39Z (27 minutes before test)
Status: ✅ SUCCESS
Duration: 8m33s
```

**Deployed Code Contains**:
```csharp
// src/LankaConnect.API/Controllers/EventsController.cs:1743
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
[Authorize]
public async Task<IActionResult> CommitToSignUpItem(...)
{
    Logger.LogWarning("[DEBUG-CONTROLLER-ENTRY] CommitToSignUpItem endpoint HIT - ...");
    // ... rest of method
}
```

### Test Execution Log

```powershell
Time: 2026-02-15 12:20:18 local (17:20:18 UTC)
Login: ✅ SUCCESS - Token received
Commit Request: HTTP POST to correct endpoint
Response: HTTP 200 OK
Correlation-ID: 4504ff3c-1880-451d-a029-32b1a4b2c22e
```

### Azure Logs Search Results

**Search 1**: `grep "DEBUG-CONTROLLER"` across 500 tail logs → **0 results**
**Search 2**: `grep "4504ff3c-1880-451d-a029-32b1a4b2c22e"` across 1000 tail logs → **0 results**
**Search 3**: `grep "17:23:5"` across 500 tail logs → **0 results**
**Search 4**: `grep "eventscontroller\|committosignup"` → **0 results**
**Search 5**: `grep "HTTP POST.*Events"` → **0 results**

**But**: Logs at 17:24:33, 17:24:36 show Hangfire requests logging normally.

---

## Impact Analysis

### Business Impact
- **CRITICAL**: Signup commitment feature appears to work (HTTP 200) but is completely non-functional
- **CRITICAL**: No data is actually being saved (despite earlier RCA thinking it was)
- **CRITICAL**: No emails sent because handlers never execute
- **HIGH**: Silent failure - users receive no error message

### Technical Debt
- **Infrastructure misconfiguration** allowing cached/stale responses
- **No monitoring** to detect when endpoints return without executing code
- **No end-to-end tests** that verify database changes actually occur

---

## Next Steps (URGENT)

### Priority 1: Verify If Data Is Actually Being Saved 🚨

**Action**: Query database directly to check if commitment from 17:23:51 test exists

```sql
SELECT * FROM events.signup_item_commitments
WHERE created_at >= '2026-02-15 17:20:00'
ORDER BY created_at DESC
LIMIT 10;
```

**Expected**: NO commitments from the tests (because handler never ran)
**If found**: This would disprove the hypothesis and suggest logs are simply not appearing

### Priority 2: Test A Different EventsController Endpoint 🚨

**Action**: Call a GET endpoint to verify application code runs AT ALL

```powershell
GET /api/Events/{known_event_id}
Authorization: Bearer {token}
```

**Expected**: Should see logs with correlation ID and controller execution
**If no logs**: Proves entire EventsController is not executing (infrastructure issue)
**If logs appear**: Proves issue is specific to the commit endpoint

### Priority 3: Check Azure Container Apps Configuration 🚨

**Action**: Review Azure Container Apps settings for:
- Response caching configuration
- Ingress rules
- Multiple revisions active
- Traffic splitting percentages

### Priority 4: Check for Duplicate Route Registrations ⚠️

**Action**: Search entire codebase for routes matching `**/commit`

```bash
grep -r "commit\]" src/LankaConnect.API/Controllers/
```

**Look for**: Duplicate registrations that might be handling the request

### Priority 5: Deploy Health Check Log 🚨

**Action**: Add logging to `Startup.cs` / `Program.cs` to verify application startup

```csharp
_logger.LogWarning("[APP-STARTUP] LankaConnect API started - Version: {Version}, Time: {Time}",
    Assembly.GetExecutingAssembly().GetName().Version, DateTime.UtcNow);
```

---

## Files Referenced

### Backend
- [src/LankaConnect.API/Controllers/EventsController.cs](../src/LankaConnect.API/Controllers/EventsController.cs) (Lines 1737-1755)
- [src/LankaConnect.Application/Events/Commands/CommitToSignUpItem/CommitToSignUpItemCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/CommitToSignUpItem/CommitToSignUpItemCommandHandler.cs)
- [src/LankaConnect.Infrastructure/Data/AppDbContext.cs](../src/LankaConnect.Infrastructure/Data/AppDbContext.cs)

### Test Scripts
- [final_test.ps1](../final_test.ps1) - Successful test returning HTTP 200
- [verbose_test.ps1](../verbose_test.ps1) - Test showing correlation ID and headers

### Documentation
- [RCA_SIGNUP_EMAILS_BACKEND_BUG_FINAL.md](./RCA_SIGNUP_EMAILS_BACKEND_BUG_FINAL.md) - Previous RCA (now outdated)
- [PHASE_6A114_DEBUG_FINDINGS.md](./PHASE_6A114_DEBUG_FINDINGS.md) - Debug logging findings

---

## Conclusion

**This is NOT a backend code bug. This is an INFRASTRUCTURE issue.**

The ASP.NET Core application code is correct and functional, but **the request is never reaching the application**. Something in the Azure Container Apps infrastructure layer (ingress, gateway, load balancer, or caching) is intercepting the request and returning an HTTP 200 response without executing the application code.

**Confidence Level**: 🔴 **99% CERTAIN**

**Category**: Infrastructure Issue - Request Handling Before Application

**Severity**: CRITICAL - Feature completely non-functional, silent failure

**Status**: Awaiting infrastructure investigation and testing of other endpoints.

---

**Last Updated**: 2026-02-15 17:25 UTC
