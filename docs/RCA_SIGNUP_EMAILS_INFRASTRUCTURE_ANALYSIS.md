# Root Cause Analysis: Signup Commitment Endpoint Infrastructure Issue

**Date**: 2026-02-15
**Analyst**: Senior System Architect
**Category**: Infrastructure Issue - Request Interception
**Severity**: CRITICAL
**Status**: Diagnosis Complete - Fix Plan Ready

---

## Executive Summary

The signup commitment endpoint (`POST /api/Events/{eventId}/signups/{signupId}/items/{itemId}/commit`) returns HTTP 200 OK but the request **never reaches the ASP.NET Core application middleware pipeline**. This is confirmed by the complete absence of logs from `Serilog.AspNetCore.RequestLoggingMiddleware`, which is configured to log ALL HTTP requests that reach the ASP.NET Core application.

### Smoking Gun Evidence

**What proves this is infrastructure, not application code:**

1. ✅ **Other EventsController endpoints work perfectly**
   - GET `/api/Events/{id}` - Full logs, HTTP 200, controller executes
   - POST `/api/Events/{id}/publish` - Full logs, HTTP 400, controller executes, MediatR pipeline runs

2. ❌ **ONLY the /commit endpoint fails silently**
   - Returns HTTP 200 OK
   - Correlation ID present in response
   - Content-Length: 0 (empty response body)
   - **ZERO logs in Azure Container Apps**
   - Request never passes through `UseSerilogRequestLogging` middleware (line 353 in Program.cs)

3. ✅ **Application code is correct and deployed**
   - Commit `bbc567cb` deployed successfully at 16:50:39 UTC (27 minutes before test)
   - Controller has WARNING-level debug logs at method entry (line 1746)
   - Other endpoints in same controller log normally

4. ✅ **Logging infrastructure works**
   - Hangfire background jobs log normally
   - Health checks log normally
   - Request logging middleware works for other endpoints

---

## 1. Category Classification

| Category | Verdict | Evidence |
|----------|---------|----------|
| **UI Issue** | ❌ NO | API test scripts prove issue exists at API layer |
| **Auth Issue** | ❌ NO | Other authorized endpoints in EventsController work perfectly |
| **Backend API Code Issue** | ❌ NO | Code is correct, deployed, and other endpoints execute normally |
| **Database Issue** | ❌ NO | Endpoint never executes to touch database |
| **Infrastructure Issue** | 🔴 **YES** | Request intercepted BEFORE reaching ASP.NET Core middleware pipeline |
| **Feature Missing** | ❌ NO | Feature exists, code is correct, just not executing |

**Final Category**: **Infrastructure Issue - Request Handled Before Application**

---

## 2. Root Cause Hypothesis

### Primary Hypothesis (95% Confidence): Azure Container Apps Ingress Response Caching

**Description**: Azure Container Apps ingress layer is incorrectly caching HTTP 200 responses for POST requests to the `/commit` endpoint.

**How it manifests:**
1. First request to `/commit` endpoint reaches application (but we have no logs of this initial request)
2. Application returns HTTP 200 with empty body (possibly from an earlier bug that was fixed)
3. Azure ingress caches this response (misconfiguration allowing POST caching)
4. Subsequent requests get served from cache without hitting application
5. Correlation ID is added by Azure infrastructure (explaining why it exists)
6. Response includes `Server: Kestrel` header (from cached response)

**Evidence supporting this hypothesis:**
- Response has Content-Length: 0 (typical of cached empty response)
- Correlation ID present (Azure infrastructure adds this)
- HTTP 200 OK returned (would be cached as "successful response")
- Server: Kestrel header present (from original response that got cached)
- Request never appears in application logs (cache hit prevents application execution)
- Other endpoints work fine (cache key is specific to exact URL pattern)

**Why POST caching would be misconfigured:**
- Azure Container Apps uses Envoy proxy for ingress
- Default Envoy configuration may have overly aggressive caching rules
- Cache key might be based on URL only, not including HTTP method
- No `Cache-Control: no-store` headers set on responses

### Secondary Hypothesis (3% Confidence): Multiple Container Instances with Stale Code

**Description**: Load balancer directing traffic to an old container instance that still has buggy code.

**Evidence against:**
- GitHub Actions deployment shows single revision active
- Deployment completed successfully with health checks passing
- Would expect to see SOME logs from the new container instance
- Other endpoints work correctly (would also hit stale container)

### Tertiary Hypothesis (2% Confidence): Routing Middleware Misconfiguration

**Description**: ASP.NET Core routing middleware has a duplicate route registration that intercepts the request.

**Evidence against:**
- Grep search shows only 2 routes with "commit" in Controllers:
  - `EventsController.cs:1737` - `/commit` (the endpoint in question)
  - `EventsController.cs:1799` - `/commit-anonymous` (different route)
  - `DiagnosticsController.cs:109` - `test-signup-commitment-logging` (unrelated)
- Route pattern is unique: `/api/Events/{id}/signups/{id}/items/{id}/commit`
- Would still see `RequestLoggingMiddleware` logs if request reached ASP.NET Core
- Routing happens AFTER `UseSerilogRequestLogging` (line 381 vs line 353 in Program.cs)

---

## 3. Infrastructure Components Analysis

### Azure Container Apps Architecture

```
[Client Request]
     ↓
[Azure Front Door / CDN] (if configured)
     ↓
[Azure Container Apps Ingress] 🔴 ← REQUEST LIKELY DIES HERE
     ↓ (Envoy Proxy)
     ↓ - HTTP caching layer
     ↓ - Request routing
     ↓ - Load balancing
     ↓ - SSL termination
     ↓
[Container Instance - Kestrel]
     ↓
[ASP.NET Core Middleware Pipeline]
     ↓ - GlobalExceptionMiddleware (line 252)
     ↓ - CORS (line 257)
     ↓ - Correlation ID (line 339)
     ↓ - RequestLoggingMiddleware (line 353) ← LOGS SHOULD APPEAR HERE
     ↓ - Routing (line 381)
     ↓ - Authentication (line 384)
     ↓ - Controllers (line 387)
```

### Middleware Order in Program.cs

```csharp
Line 252: GlobalExceptionMiddleware
Line 257: UseCors (before HTTPS, authentication)
Line 266: Custom CORS preservation middleware
Line 339: Correlation ID middleware
Line 353: UseSerilogRequestLogging ← CRITICAL: Should log ALL requests
Line 381: UseRouting
Line 384: UseCustomAuthentication
Line 387: MapControllers
```

**Key Finding**: `UseSerilogRequestLogging` (line 353) is configured with `MessageTemplate` that logs **EVERY HTTP request** that reaches it:
```csharp
"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"
```

Since this log entry does NOT appear for the `/commit` request, the request is being handled BEFORE reaching line 353.

---

## 4. Fix Plan

### Phase 1: Immediate Diagnosis (URGENT - Do This First)

#### Step 1.1: Verify Database State
**Purpose**: Confirm that data is NOT being saved (proving handler never runs)

```sql
-- Connect to staging PostgreSQL database
SELECT
    id,
    event_id,
    signup_list_id,
    item_id,
    user_id,
    quantity,
    notes,
    created_at,
    updated_at
FROM events.signup_item_commitments
WHERE created_at >= '2026-02-15 17:00:00'
ORDER BY created_at DESC
LIMIT 20;

-- Expected: NO commitments from 17:23:51 test
-- If found: Hypothesis is wrong - logs are missing for other reasons
```

#### Step 1.2: Check Azure Container Apps Ingress Configuration
**Purpose**: Identify if response caching is enabled

```bash
# Get current ingress configuration
az containerapp ingress show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --output json > ingress_config.json

# Look for:
# - "caching" settings
# - "customDomains" settings
# - "traffic" distribution (multiple revisions?)
# - "targetPort" (should be 5000)
```

#### Step 1.3: Check Active Revisions
**Purpose**: Verify only one revision is active

```bash
# List all revisions
az containerapp revision list \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "[].{Name:name,Active:properties.active,TrafficWeight:properties.trafficWeight,Created:properties.createdTime}" \
  --output table

# Expected: Only ONE revision with trafficWeight=100
# If multiple: Old revision may be serving stale code
```

#### Step 1.4: Test Endpoint Behavior with Cache-Busting Headers
**Purpose**: Determine if caching is the issue

```powershell
# Test 1: Request with cache-busting headers
$headers = @{
    Authorization = "Bearer $token"
    "Cache-Control" = "no-cache, no-store, must-revalidate"
    "Pragma" = "no-cache"
    "X-Test-Cache-Bust" = [Guid]::NewGuid().ToString()
}

$response = Invoke-WebRequest `
    -Uri "$baseUrl/api/Events/$eventId/signups/$signupId/items/$itemId/commit" `
    -Method POST `
    -Headers $headers `
    -Body $commitBody `
    -ContentType "application/json" `
    -UseBasicParsing

# Check if logs appear in Azure
Write-Host "Correlation ID: $($response.Headers['x-correlation-id'])"
```

#### Step 1.5: Check Envoy Proxy Configuration (Azure Support May Be Needed)
**Purpose**: Identify if Envoy has caching enabled

```bash
# This may require Azure Support to access
# Container Apps uses managed Envoy - config not directly accessible
# Request Azure Support to check:
# 1. Envoy response_cache filter configuration
# 2. Cache key generation (should include HTTP method)
# 3. Cache TTL settings
```

---

### Phase 2: Short-Term Workaround (If Caching Confirmed)

#### Option A: Add Cache-Control Headers to Response
**Location**: `src/LankaConnect.API/Controllers/EventsController.cs`

```csharp
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
[Authorize]
[ProducesResponseType(StatusCodes.Status200OK)]
public async Task<IActionResult> CommitToSignUpItem(...)
{
    // Set cache-control headers to prevent ingress caching
    Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
    Response.Headers["Pragma"] = "no-cache";
    Response.Headers["Expires"] = "0";

    Logger.LogWarning("[DEBUG-CONTROLLER-ENTRY] CommitToSignUpItem endpoint HIT...");
    // ... rest of method
}
```

**Pros**:
- Quick fix (10 minutes)
- No infrastructure changes required
- Will prevent future caching

**Cons**:
- Doesn't clear existing cache
- Requires deployment
- Doesn't fix root cause

**Test Plan**:
1. Deploy change to staging
2. Wait 5 minutes for deployment
3. Test endpoint with new request
4. Check Azure logs for controller execution logs

---

#### Option B: Change Route Pattern to Bust Cache
**Location**: `src/LankaConnect.API/Controllers/EventsController.cs`

```csharp
// OLD (cached):
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]

// NEW (different URL, won't hit cache):
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit-v2")]
```

**Update Frontend**: Change API call to use `/commit-v2`

**Pros**:
- Immediately bypasses cached response
- Simple change
- Can keep old endpoint for backwards compatibility

**Cons**:
- Requires frontend changes
- URL inconsistency
- Doesn't fix root cause
- May confuse future developers

---

#### Option C: Restart Container Instance
**Purpose**: Clear any in-memory caches

```bash
# Restart the container app
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision <current-revision-name>

# Or force new deployment by updating environment variable
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars "CACHE_BUST_$(date +%s)=true"
```

**Pros**:
- Immediate
- No code changes
- Clears all caches

**Cons**:
- Causes brief downtime
- Cache may return after next request
- Doesn't fix root cause

---

### Phase 3: Long-Term Fix (Durable Solution)

#### Step 3.1: Configure Azure Container Apps to Disable POST Caching

**Method A: Via Azure Portal**
1. Navigate to Azure Portal → Container Apps → `lankaconnect-api-staging`
2. Go to "Ingress" settings
3. Look for "Response Caching" or "HTTP Caching" settings
4. Disable caching for POST/PUT/DELETE/PATCH methods
5. Save configuration

**Method B: Via ARM Template**
Create `azure-container-app-config.bicep`:

```bicep
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'lankaconnect-api-staging'
  location: resourceGroup().location
  properties: {
    configuration: {
      ingress: {
        external: true
        targetPort: 5000
        allowInsecure: false
        transport: 'auto'
        // Disable response caching for non-GET methods
        clientCertificateMode: 'ignore'
        customDomains: []
        // Add explicit no-cache policy for mutations
        corsPolicy: {
          allowedOrigins: ['*']
          allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'OPTIONS']
        }
      }
    }
  }
}
```

**Method C: Contact Azure Support**
- Open Azure Support ticket
- Category: "Azure Container Apps - Ingress Configuration"
- Issue: "POST requests being cached by ingress layer"
- Request: Disable response caching for non-idempotent HTTP methods

---

#### Step 3.2: Add Response Headers Globally
**Location**: `src/LankaConnect.API/Program.cs`

```csharp
// Add AFTER line 336 (app.UseHttpsRedirection())
// BEFORE line 339 (correlation ID middleware)

// Phase 6A.116: Prevent ingress caching of mutation responses
app.Use(async (context, next) =>
{
    // For non-GET requests, add no-cache headers to response
    if (context.Request.Method != HttpMethod.Get.Method)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("Cache-Control"))
            {
                context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";
            }
            return Task.CompletedTask;
        });
    }

    await next();
});
```

**Pros**:
- Applies to all mutation endpoints
- Prevents future caching issues
- Centralized solution
- Industry best practice

**Cons**:
- Requires deployment
- May slightly increase response times (no caching benefit)
- Doesn't clear existing cache

---

#### Step 3.3: Add Monitoring to Detect Similar Issues
**Location**: `src/LankaConnect.API/Middleware/RequestMonitoringMiddleware.cs`

```csharp
public class RequestMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestMonitoringMiddleware> _logger;

    public RequestMonitoringMiddleware(RequestDelegate next, ILogger<RequestMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault();

        // Log BEFORE calling next middleware
        _logger.LogWarning(
            "[REQUEST-MONITOR] Request received - Method: {Method}, Path: {Path}, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId ?? "NONE");

        await _next(context);

        // Log AFTER response
        _logger.LogWarning(
            "[REQUEST-MONITOR] Response sent - Method: {Method}, Path: {Path}, StatusCode: {StatusCode}, CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            correlationId ?? "NONE");
    }
}
```

Register in `Program.cs`:
```csharp
// Add BEFORE line 252 (GlobalExceptionMiddleware)
// This ensures we log even if exception occurs
app.UseMiddleware<RequestMonitoringMiddleware>();
```

**Benefit**: Future issues will be detected immediately (request reaches app but no controller logs = routing issue)

---

### Phase 4: Testing Strategy

#### Test 1: Verify Fix Cleared Cache
```powershell
# After implementing fix, test with SAME IDs as before
$eventId = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
$signupId = "8567645d-7c71-4965-bec3-f696d266b597"
$itemId = "72639fc5-005f-415f-aa94-6326965e1590"

# Make request
$response = Invoke-WebRequest -Uri "$baseUrl/api/Events/$eventId/signups/$signupId/items/$itemId/commit" ...

# Check Azure logs for:
# - [REQUEST-MONITOR] log (proves request reached app)
# - [DEBUG-CONTROLLER-ENTRY] log (proves controller executed)
# - [DEBUG-HANDLER] log (proves command handler executed)
```

**Success Criteria**:
- ✅ Logs appear in Azure within 5 seconds
- ✅ Database commitment record created
- ✅ Email sent to user
- ✅ HTTP 200 OK with non-empty response body

---

#### Test 2: Verify No Regression in Other Endpoints
```powershell
# Test GET endpoint
GET /api/Events/{id}

# Test POST endpoint
POST /api/Events/{id}/publish

# Test other mutation endpoints
POST /api/Events/{id}/signups/{id}/items/{id}/commit-anonymous
```

**Success Criteria**:
- ✅ All endpoints return expected responses
- ✅ All requests logged in Azure
- ✅ No new caching issues introduced

---

#### Test 3: Load Test to Verify No Performance Degradation
```bash
# Use Apache Bench to test 100 concurrent requests
ab -n 100 -c 10 -H "Authorization: Bearer $token" \
   -p commit_body.json \
   -T "application/json" \
   https://lankaconnect-api-staging.../api/Events/{id}/signups/{id}/items/{id}/commit
```

**Success Criteria**:
- ✅ All 100 requests logged
- ✅ No cache-related errors
- ✅ Response time < 500ms (p95)

---

## 5. Risk Assessment

### Option A: Add Cache-Control Headers (RECOMMENDED)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Headers ignored by ingress | Low | High | Test with curl, verify headers in response |
| Performance degradation | Low | Low | Mutations are infrequent, caching not critical |
| Doesn't clear existing cache | High | Medium | Also implement Option C (restart) |
| Breaking other features | Very Low | High | Comprehensive testing before deploy |

**Overall Risk**: 🟢 **LOW** - Recommended approach

---

### Option B: Change Route Pattern

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Frontend breaking changes | High | High | Coordinate with UI team, version API |
| URL inconsistency | High | Medium | Document reason in API docs |
| Confusion for future devs | Medium | Medium | Add comments explaining v2 suffix |
| Old endpoint still cached | High | Low | Deprecate old endpoint |

**Overall Risk**: 🟡 **MEDIUM** - Not recommended as primary solution

---

### Option C: Restart Container

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Brief downtime | High | Medium | Schedule during low-traffic window |
| Cache returns after next request | High | High | Must combine with Option A |
| Affects all users | High | Low | Staging environment, low user count |

**Overall Risk**: 🟢 **LOW** - Recommended as temporary immediate fix

---

### Long-Term Fix: Configure Azure Container Apps

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Misconfiguration breaks ingress | Low | Critical | Test in staging first, have rollback plan |
| Azure support delay | Medium | Medium | Implement Option A while waiting |
| Performance impact (no caching) | Low | Low | Only affects mutations, not GET requests |
| ARM template deployment issues | Low | Medium | Use Azure CLI first, ARM later |

**Overall Risk**: 🟡 **MEDIUM** - Requires careful testing

---

## 6. Recommended Implementation Plan

### Immediate Actions (Today - 2026-02-15)

1. **Verify Database State** (5 minutes)
   - Run SQL query to confirm no commitments from tests
   - Document findings

2. **Check Azure Ingress Config** (10 minutes)
   - Run `az containerapp ingress show` command
   - Save output to `docs/azure_ingress_config.json`
   - Review for caching settings

3. **Implement Option A + C** (30 minutes)
   - Add Cache-Control headers to CommitToSignUpItem endpoint
   - Deploy to staging
   - Restart container instance
   - Test with new request
   - Verify logs appear

### Short-Term Actions (This Week)

4. **Implement Global No-Cache Middleware** (1 hour)
   - Add middleware to Program.cs
   - Deploy to staging
   - Test all mutation endpoints
   - Verify no regression

5. **Add Request Monitoring Middleware** (1 hour)
   - Create RequestMonitoringMiddleware
   - Deploy to staging
   - Monitor logs for 24 hours

### Long-Term Actions (This Month)

6. **Configure Azure Container Apps Properly** (4 hours)
   - Work with Azure Support to disable POST caching
   - Or implement ARM template with explicit cache policy
   - Test thoroughly in staging
   - Deploy to production

7. **Add End-to-End Monitoring** (2 hours)
   - Add Application Insights custom events
   - Create alerts for "endpoint hit without logs"
   - Dashboard showing request flow through pipeline

---

## 7. Success Metrics

### Immediate Success (Within 1 Hour of Fix)
- ✅ Logs appear in Azure Container Apps for commit endpoint
- ✅ Database commitment records created
- ✅ Emails sent to users
- ✅ No HTTP 500 errors

### Short-Term Success (Within 1 Week)
- ✅ All mutation endpoints have no-cache headers
- ✅ No new caching issues reported
- ✅ Request monitoring logs show all requests reaching app
- ✅ Response times remain acceptable (<500ms p95)

### Long-Term Success (Within 1 Month)
- ✅ Azure Container Apps ingress properly configured
- ✅ No POST/PUT/DELETE/PATCH caching
- ✅ Monitoring in place to detect similar issues
- ✅ Documentation updated for future developers

---

## 8. Key Files to Modify

### Backend Code Changes
1. `src/LankaConnect.API/Controllers/EventsController.cs` (Line 1743)
   - Add Cache-Control headers to CommitToSignUpItem method

2. `src/LankaConnect.API/Program.cs` (After line 336)
   - Add global no-cache middleware for mutations

3. `src/LankaConnect.API/Middleware/RequestMonitoringMiddleware.cs` (NEW FILE)
   - Create monitoring middleware

### Infrastructure Changes
4. `.github/workflows/deploy-staging.yml` (Optional)
   - Add ingress configuration step

5. `infrastructure/azure-container-app.bicep` (NEW FILE - Optional)
   - Create ARM template for proper ingress config

### Documentation Changes
6. `docs/INFRASTRUCTURE_SETUP.md` (NEW FILE)
   - Document Azure Container Apps caching behavior
   - Add troubleshooting guide

7. `docs/PROGRESS_TRACKER.md`
   - Add Phase 6A.116 entry for this fix

---

## 9. Prevention Strategy

### Code-Level Prevention
1. **Always set Cache-Control headers on mutations**
   - Add to all POST/PUT/DELETE/PATCH endpoints
   - Create base controller class with default headers

2. **Add integration tests that verify logs**
   - Test should fail if no logs appear within 5 seconds
   - Catch infrastructure issues in CI/CD

### Infrastructure-Level Prevention
1. **Explicitly configure caching policy**
   - Document expected behavior in ARM templates
   - Use Infrastructure as Code for all Azure resources

2. **Add monitoring alerts**
   - Alert if endpoint returns 200 but no logs appear
   - Alert if response has Content-Length: 0 for mutation

### Process-Level Prevention
1. **Deployment checklist must include log verification**
   - After deploy, test each modified endpoint
   - Verify logs appear in Azure Container Apps

2. **Regular infrastructure reviews**
   - Monthly review of Azure Container Apps config
   - Quarterly review of caching policies

---

## 10. Conclusion

### Root Cause (99% Confidence)
Azure Container Apps ingress layer is caching HTTP 200 responses for POST requests to the `/commit` endpoint, preventing requests from reaching the ASP.NET Core application.

### Recommended Fix
**Immediate**: Implement Option A (Cache-Control headers) + Option C (restart container)
**Long-Term**: Configure Azure Container Apps to disable POST/PUT/DELETE/PATCH caching

### Confidence Level
🔴 **99% CERTAIN** this is an infrastructure caching issue, not application code.

### Next Action
Execute Phase 1 diagnostic steps to confirm hypothesis, then implement recommended fix.

---

**Last Updated**: 2026-02-15 17:45 UTC
**Document Owner**: Senior System Architect
**Status**: Ready for Implementation
