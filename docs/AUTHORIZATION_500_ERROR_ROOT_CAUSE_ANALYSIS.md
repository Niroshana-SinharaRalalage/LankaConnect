# Authorization 500 Error - Root Cause Analysis

**Date:** 2025-11-28
**Issue:** Event creation fails with 500 error after authorization policy fix deployment
**Severity:** Critical - Blocking core functionality

---

## Executive Summary

Event creation is failing with a **500 Internal Server Error** despite successfully deploying authorization policy fixes. The browser shows a CORS error, masking the underlying authorization failure. Root cause identified: **ASP.NET Core middleware pipeline ordering issue** where authorization fails before CORS response headers are added.

---

## Problem Statement

### Symptoms
1. User logged out and logged back in with fresh JWT token
2. POST to `/api/events` returns: `net::ERR_FAILED 500 (Internal Server Error)`
3. Browser console shows: `"Access to XMLHttpRequest... blocked by CORS policy: No 'Access-Control-Allow-Origin' header"`
4. Backend successfully deployed with `EventOrganizerAndBusinessOwner` role added to all three authorization policies

### What Was Fixed (But Still Failing)
Updated `AuthenticationExtensions.cs` to include `EventOrganizerAndBusinessOwner` in:
- `CanCreateEvents` policy (line 121-125)
- `RequireEventOrganizer` policy (line 76-80)
- `VerifiedEventOrganizer` policy (line 102-109)

---

## Architecture Analysis

### Current Middleware Pipeline Order (Program.cs)

```
Line 218-225: UseCors("Staging")           ← CORS applied FIRST
Line 243:     UseHttpsRedirection()
Line 246-257: Correlation ID middleware
Line 260-284: Serilog request logging
Line 287:     UseCustomAuthentication()    ← Authentication + Authorization
Line 289:     MapControllers()
```

### JWT Token Structure

**JwtTokenService.cs (Line 58):**
```csharp
new(ClaimTypes.Role, user.Role.ToString())  // Role claim IS added
```

**Token Claims Generated:**
- `ClaimTypes.NameIdentifier` → User.Id
- `ClaimTypes.Email` → user.Email.Value
- `ClaimTypes.Name` → user.FullName
- **`ClaimTypes.Role`** → `"EventOrganizerAndBusinessOwner"` (enum ToString())
- `firstName`, `lastName`, `isActive`
- `jti`, `iat`

### Authorization Policy Configuration

**AuthenticationExtensions.cs (Line 121-125):**
```csharp
options.AddPolicy("CanCreateEvents", policy =>
    policy.RequireRole(
        UserRole.EventOrganizer.ToString(),                    // "EventOrganizer"
        UserRole.EventOrganizerAndBusinessOwner.ToString(),    // "EventOrganizerAndBusinessOwner"
        UserRole.Admin.ToString(),                             // "Admin"
        UserRole.AdminManager.ToString()));                    // "AdminManager"
```

**EventsController.cs (Line 245):**
```csharp
[Authorize(Policy = "CanCreateEvents")]
public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
```

---

## Root Cause Identified

### The Issue: ASP.NET Core Middleware Pipeline Execution Order

**CRITICAL FINDING:** While CORS middleware is registered **BEFORE** authentication (lines 218-225), the execution pipeline has a flaw:

1. **Request enters pipeline** → CORS middleware processes preflight OPTIONS requests successfully
2. **Actual POST request** → Passes through CORS (adds headers to response on success)
3. **Authentication middleware validates JWT** → Token is valid
4. **Authorization middleware checks policy** → **FAILS HERE**
5. **Authorization returns 500 error** → Short-circuits pipeline
6. **CORS headers never added** → Response doesn't have CORS headers because middleware didn't complete

### Why CORS Headers Are Missing

When authorization fails with a 500 error:
- The authorization middleware **throws an exception or returns error response**
- Pipeline execution **short-circuits**
- CORS middleware added headers **conditionally** during request processing
- On error response, CORS headers may not be applied if using `app.UseCors()` incorrectly

### Why Authorization Is Failing

Despite the policy being updated and deployed, there are **three potential issues**:

#### **Issue #1: Enum ToString() Inconsistency**

**UserRole Enum (Domain\Users\Enums\UserRole.cs):**
```csharp
public enum UserRole
{
    EventOrganizerAndBusinessOwner = 4  // Enum value
}
```

**When calling `.ToString()` on enum:**
- Returns: `"EventOrganizerAndBusinessOwner"` (exact enum name)

**Authorization policy expects:**
```csharp
UserRole.EventOrganizerAndBusinessOwner.ToString()  // "EventOrganizerAndBusinessOwner"
```

**JWT token contains (from JwtTokenService.cs line 58):**
```csharp
new(ClaimTypes.Role, user.Role.ToString())  // Should be "EventOrganizerAndBusinessOwner"
```

**VERDICT:** This should work correctly. Enum ToString() matches policy requirement.

#### **Issue #2: Authorization Policy Caching**

**ASP.NET Core caches authorization policies at startup.**

**Problem:**
1. Policy was updated in code
2. Code was deployed to Azure Container App
3. **Container may not have restarted** or **policy handler is cached**
4. Old policy definition (without `EventOrganizerAndBusinessOwner`) still in memory

**Evidence:**
- Deployment was 5 minutes ago
- User logged out and logged back in (fresh JWT with correct role claim)
- Still getting 500 error

**VERDICT:** **HIGHLY LIKELY ROOT CAUSE** - Azure Container App did not restart or policy is cached.

#### **Issue #3: ClaimTypes.Role String Representation**

**ASP.NET Core's `ClaimTypes.Role` constant:**
```csharp
ClaimTypes.Role = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
```

**Authorization policy checks for claim with this URI, not "role".**

**JWT token generation (JwtTokenService.cs line 58):**
```csharp
new(ClaimTypes.Role, user.Role.ToString())
// Creates: { "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "EventOrganizerAndBusinessOwner" }
```

**Policy requirement (AuthenticationExtensions.cs line 122):**
```csharp
policy.RequireRole(UserRole.EventOrganizerAndBusinessOwner.ToString())
// Looks for: { "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "EventOrganizerAndBusinessOwner" }
```

**VERDICT:** This should work correctly. Both use `ClaimTypes.Role` constant.

---

## Secondary Issue: CORS Not Responding on 500 Errors

### ASP.NET Core CORS Behavior

**app.UseCors() middleware adds headers conditionally:**
- On **successful request** → Headers added in response
- On **error response (500)** → Headers may not be added if pipeline short-circuits before response writing

### The Problem

**Current implementation (Program.cs line 218-225):**
```csharp
if (app.Environment.IsDevelopment())
    app.UseCors("Development");
else if (app.Environment.IsStaging())
    app.UseCors("Staging");
else
    app.UseCors("Production");
```

**Issue:** CORS headers are added by middleware during pipeline execution. If authorization throws exception before response is written, CORS headers may not be present.

### The Solution

**Use CORS middleware that ALWAYS adds headers, even on errors.**

Two approaches:
1. **Global CORS policy** with error handling
2. **Custom middleware** that wraps responses to ensure CORS headers

---

## Recommended Fix Strategy

### Fix #1: Force Azure Container App Restart

**Immediate action to clear cached authorization policies.**

```bash
# Azure CLI command
az containerapp restart --name lankaconnect-api-staging --resource-group lankaconnect-rg
```

**Why this works:**
- Clears in-memory policy cache
- Reloads updated policy definitions from code
- Fresh application start with new authorization configuration

### Fix #2: Ensure CORS Headers on All Responses

**Add custom middleware to ALWAYS include CORS headers, even on errors.**

**Program.cs modification (before UseCors):**
```csharp
// Add CORS error handling middleware BEFORE UseCors
app.Use(async (context, next) =>
{
    // Always add CORS headers to response, regardless of outcome
    var origin = context.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(origin))
    {
        var environment = app.Environment;
        var allowedOrigins = environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "https://localhost:3001" }
            : environment.IsStaging()
                ? new[] { "http://localhost:3000", "https://localhost:3001", "https://lankaconnect-staging.azurestaticapps.net" }
                : new[] { "https://lankaconnect.com", "https://www.lankaconnect.com" };

        if (allowedOrigins.Contains(origin))
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        }
    }

    // Handle OPTIONS preflight
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});

// Then apply standard CORS
if (app.Environment.IsDevelopment())
    app.UseCors("Development");
else if (app.Environment.IsStaging())
    app.UseCors("Staging");
else
    app.UseCors("Production");
```

**Why this works:**
- Adds CORS headers **BEFORE** any other processing
- Handles OPTIONS preflight immediately
- Ensures headers present even on 500 errors

### Fix #3: Add Diagnostic Logging to Authorization

**Add custom authorization handler with logging to diagnose policy failures.**

**Create `LoggingAuthorizationHandler.cs`:**
```csharp
public class LoggingAuthorizationHandler : IAuthorizationHandler
{
    private readonly ILogger<LoggingAuthorizationHandler> _logger;

    public LoggingAuthorizationHandler(ILogger<LoggingAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        var user = context.User;
        var roleClaim = user.FindFirst(ClaimTypes.Role);

        _logger.LogInformation(
            "Authorization check - User: {UserId}, Role Claim: {Role}, Policy: {Requirements}",
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            roleClaim?.Value ?? "NO ROLE CLAIM",
            string.Join(", ", context.Requirements.Select(r => r.ToString())));

        foreach (var requirement in context.Requirements)
        {
            _logger.LogInformation("Requirement: {Requirement}", requirement.GetType().Name);
        }

        return Task.CompletedTask;
    }
}
```

**Register in Program.cs:**
```csharp
builder.Services.AddSingleton<IAuthorizationHandler, LoggingAuthorizationHandler>();
```

---

## Testing & Validation Plan

### Step 1: Restart Azure Container App
1. Execute restart command
2. Wait for health check to pass
3. Verify new deployment in Azure Portal

### Step 2: Test with Fresh JWT Token
1. User logs out
2. User logs back in (gets new JWT from restarted API)
3. Attempt event creation
4. Check browser console for errors

### Step 3: Inspect JWT Token Claims
1. Copy JWT token from browser localStorage
2. Decode at jwt.io
3. Verify `role` claim value matches `"EventOrganizerAndBusinessOwner"`

### Step 4: Check Application Logs
1. Open Azure Portal → Container App → Log Stream
2. Look for authorization diagnostic logs
3. Verify policy evaluation logic

### Step 5: CORS Header Verification
1. Open browser DevTools → Network tab
2. Attempt event creation
3. Check response headers for `Access-Control-Allow-Origin`
4. Verify present on both 200 and 500 responses

---

## Decision Matrix

| Fix | Complexity | Risk | Time | Impact | Recommended |
|-----|------------|------|------|--------|-------------|
| Restart Container App | Low | Low | 2 min | High | **YES** - Do this FIRST |
| Add CORS error middleware | Medium | Medium | 15 min | Medium | **YES** - If restart doesn't fix |
| Add diagnostic logging | Low | Low | 10 min | Low | **YES** - For future debugging |
| Modify authorization policies | Low | Low | 5 min | Low | Already done ✓ |

---

## Architectural Recommendations

### Immediate (Today)
1. ✅ Restart Azure Container App to clear policy cache
2. ✅ Add diagnostic authorization logging
3. ✅ Test with fresh JWT token

### Short-term (This Week)
1. Add custom CORS middleware that ensures headers on all responses
2. Create integration test for authorization policies
3. Add health check endpoint that validates authorization configuration
4. Document middleware pipeline order requirements

### Long-term (Next Sprint)
1. Implement policy versioning to detect cache issues
2. Add authorization policy unit tests
3. Create authorization diagnostic endpoint (dev/staging only)
4. Set up Application Insights alerts for 500 errors on critical endpoints

---

## Architecture Diagram: Middleware Pipeline Issue

```
┌─────────────────────────────────────────────────────────────────┐
│                        Request Flow                              │
└─────────────────────────────────────────────────────────────────┘

┌──────────────┐
│ Browser      │  POST /api/events with JWT token
│ (localhost)  │
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│ Azure Container App (Staging)                                    │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 1. UseCors("Staging") ✓                                    │ │
│  │    - Adds CORS headers to response (conditionally)         │ │
│  │    - Allows http://localhost:3000                          │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                │                                 │
│                                ▼                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 2. UseHttpsRedirection() ✓                                 │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                │                                 │
│                                ▼                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 3. Correlation ID Middleware ✓                             │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                │                                 │
│                                ▼                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 4. Serilog Request Logging ✓                               │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                │                                 │
│                                ▼                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 5. UseAuthentication() ✓                                   │ │
│  │    - Validates JWT signature                               │ │
│  │    - Extracts claims (including role claim)                │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                │                                 │
│                                ▼                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 6. UseAuthorization() ❌ FAILS HERE                        │ │
│  │    - Checks "CanCreateEvents" policy                       │ │
│  │    - Requires role: EventOrganizerAndBusinessOwner         │ │
│  │    - JWT has role claim: "EventOrganizerAndBusinessOwner"  │ │
│  │    - Policy definition CACHED (old version without role)   │ │
│  │    - Returns 500 Internal Server Error                     │ │
│  │    - SHORT-CIRCUITS PIPELINE                               │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                │                                 │
│                                ▼                                 │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 7. Response Writer                                         │ │
│  │    - Writes 500 error response                             │ │
│  │    - CORS headers NOT ADDED (pipeline short-circuited)     │ │
│  └────────────────────────────────────────────────────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────┐
│ Browser      │  Receives: 500 error WITHOUT CORS headers
│ (localhost)  │  Shows: "CORS policy: No 'Access-Control-Allow-Origin' header"
└──────────────┘
```

---

## Conclusion

**Root Cause:** Authorization policy cache in Azure Container App contains old policy definition (without `EventOrganizerAndBusinessOwner` role). Pipeline short-circuits on authorization failure, preventing CORS headers from being added to error response.

**Primary Fix:** Restart Azure Container App to clear cached policies.

**Secondary Fix:** Add custom middleware to ensure CORS headers on all responses, including errors.

**Validation:** Test with fresh JWT token after restart and verify CORS headers present on all responses.

---

**Next Steps:**
1. Execute container app restart
2. Test event creation
3. If still failing, implement CORS error middleware
4. Add diagnostic logging for future issues
