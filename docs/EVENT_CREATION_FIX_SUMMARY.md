# Event Creation 500 Error - Fix Summary

**Date:** 2025-11-27
**Issue:** HTTP 500 Internal Server Error when creating events
**Root Cause:** Missing `EventOrganizerAndBusinessOwner` role in authorization policies
**Status:** FIXED

---

## Problem Statement

Users with the `EventOrganizerAndBusinessOwner` role were unable to create events, receiving HTTP 500 Internal Server Errors. The error manifested as a CORS issue in the browser, masking the underlying authorization failure.

---

## Root Cause

Three ASP.NET Core authorization policies were missing the `EventOrganizerAndBusinessOwner` role:

1. `CanCreateEvents` - Used by `[Authorize(Policy = "CanCreateEvents")]` on event creation endpoint
2. `RequireEventOrganizer` - Used by other event-related endpoints
3. `VerifiedEventOrganizer` - Used by endpoints requiring active account + organizer role

The domain logic correctly included this role in `UserRole.CanCreateEvents()` extension method, but the API layer policies diverged from this business rule.

---

## Solution Applied

**File:** `src/LankaConnect.API/Extensions/AuthenticationExtensions.cs`

### Changes Made

#### 1. CanCreateEvents Policy (Line 115-119)

**BEFORE:**
```csharp
options.AddPolicy("CanCreateEvents", policy =>
    policy.RequireRole(UserRole.EventOrganizer.ToString(),
                     UserRole.Admin.ToString(),
                     UserRole.AdminManager.ToString()));
```

**AFTER:**
```csharp
options.AddPolicy("CanCreateEvents", policy =>
    policy.RequireRole(UserRole.EventOrganizer.ToString(),
                     UserRole.EventOrganizerAndBusinessOwner.ToString(), // ADDED
                     UserRole.Admin.ToString(),
                     UserRole.AdminManager.ToString()));
```

#### 2. RequireEventOrganizer Policy (Line 76-80)

**BEFORE:**
```csharp
options.AddPolicy("RequireEventOrganizer", policy =>
    policy.RequireRole(UserRole.EventOrganizer.ToString(),
                     UserRole.Admin.ToString(),
                     UserRole.AdminManager.ToString()));
```

**AFTER:**
```csharp
options.AddPolicy("RequireEventOrganizer", policy =>
    policy.RequireRole(UserRole.EventOrganizer.ToString(),
                     UserRole.EventOrganizerAndBusinessOwner.ToString(), // ADDED
                     UserRole.Admin.ToString(),
                     UserRole.AdminManager.ToString()));
```

#### 3. VerifiedEventOrganizer Policy (Line 102-109)

**BEFORE:**
```csharp
options.AddPolicy("VerifiedEventOrganizer", policy =>
{
    policy.RequireRole(UserRole.EventOrganizer.ToString(),
                     UserRole.Admin.ToString(),
                     UserRole.AdminManager.ToString());
    policy.RequireClaim("isActive", "true");
});
```

**AFTER:**
```csharp
options.AddPolicy("VerifiedEventOrganizer", policy =>
{
    policy.RequireRole(UserRole.EventOrganizer.ToString(),
                     UserRole.EventOrganizerAndBusinessOwner.ToString(), // ADDED
                     UserRole.Admin.ToString(),
                     UserRole.AdminManager.ToString());
    policy.RequireClaim("isActive", "true");
});
```

---

## Impact Analysis

### What This Fixes

1. **Event Creation** - `EventOrganizerAndBusinessOwner` users can now create events
2. **Event Management** - All event-related operations now work for dual-role users
3. **Authorization Consistency** - API policies now align with domain business rules

### What Still Works

1. **EventOrganizer Role** - Continues to work as before
2. **Admin/AdminManager Roles** - Unaffected by changes
3. **GeneralUser/BusinessOwner Roles** - Still correctly blocked from event operations

### Potential Side Effects

**NONE** - This is a pure additive change. No existing functionality is removed or modified.

---

## Testing Strategy

### Manual Testing Checklist

#### Prerequisites
- [ ] Deploy updated code to staging environment
- [ ] Have test accounts for each role:
  - GeneralUser
  - EventOrganizer
  - EventOrganizerAndBusinessOwner
  - BusinessOwner
  - Admin
  - AdminManager

#### Test Cases

**Test 1: EventOrganizerAndBusinessOwner Can Create Events**
- [ ] Login as EventOrganizerAndBusinessOwner
- [ ] Navigate to event creation form
- [ ] Fill in all required fields:
  - Title: "Test Event"
  - Description: (at least 20 characters)
  - Category: Community
  - Start Date: (future date)
  - End Date: (after start date)
  - Capacity: 50
  - Location: Optional but valid if provided
  - Pricing: Free or valid paid event
- [ ] Submit form
- [ ] **Expected:** Event created successfully (HTTP 201)
- [ ] **Expected:** Redirected to event detail page
- [ ] **Expected:** Event appears in "My Events" list

**Test 2: EventOrganizer Role Still Works (Regression)**
- [ ] Login as EventOrganizer
- [ ] Create event with same data as Test 1
- [ ] **Expected:** Event created successfully

**Test 3: Admin/AdminManager Roles Still Work (Regression)**
- [ ] Login as Admin
- [ ] Create event with same data as Test 1
- [ ] **Expected:** Event created successfully

**Test 4: GeneralUser Blocked (Security)**
- [ ] Login as GeneralUser
- [ ] Attempt to access event creation endpoint directly
- [ ] **Expected:** HTTP 403 Forbidden
- [ ] **Expected:** Error message indicates insufficient permissions

**Test 5: BusinessOwner Blocked (Security)**
- [ ] Login as BusinessOwner
- [ ] Attempt to access event creation endpoint directly
- [ ] **Expected:** HTTP 403 Forbidden
- [ ] **Expected:** Error message indicates insufficient permissions

**Test 6: Unauthenticated Access Blocked (Security)**
- [ ] Logout
- [ ] Attempt to access event creation endpoint
- [ ] **Expected:** HTTP 401 Unauthorized

### Automated Testing

**Recommended Unit Tests** (to prevent regression):

```csharp
// src/LankaConnect.API.Tests/Extensions/AuthorizationPolicyTests.cs
[Theory]
[InlineData(UserRole.EventOrganizer, true)]
[InlineData(UserRole.EventOrganizerAndBusinessOwner, true)]
[InlineData(UserRole.Admin, true)]
[InlineData(UserRole.AdminManager, true)]
[InlineData(UserRole.GeneralUser, false)]
[InlineData(UserRole.BusinessOwner, false)]
public void CanCreateEvents_Policy_Allows_Correct_Roles(UserRole role, bool shouldAllow)
{
    // Test implementation to validate policy behavior
}
```

---

## Deployment Instructions

### Prerequisites

1. All changes committed to `develop` branch
2. Build succeeds with 0 errors
3. All existing tests pass

### Staging Deployment

```bash
# 1. Ensure you're on develop branch with latest changes
git checkout develop
git pull origin develop

# 2. Build the solution
dotnet build src/LankaConnect.API/LankaConnect.API.csproj --configuration Release

# 3. Run tests
dotnet test src/LankaConnect.Tests/LankaConnect.Tests.csproj

# 4. Deploy to staging (Azure Container Apps)
# This happens automatically via GitHub Actions on push to develop
git push origin develop

# 5. Verify deployment
# Check Azure Container Apps staging environment is running
# URL: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
```

### Production Deployment

```bash
# 1. Merge develop to master (after staging validation)
git checkout master
git merge develop
git push origin master

# 2. Deployment happens automatically via GitHub Actions

# 3. Monitor production logs for errors
# Check Azure Application Insights for any authorization failures
```

---

## Verification Steps (Post-Deployment)

### Staging Environment

1. **Check API Health**
   ```bash
   curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health
   ```
   **Expected:** HTTP 200 OK

2. **Test Event Creation** (via frontend)
   - URL: Your Next.js staging frontend
   - Login with EventOrganizerAndBusinessOwner test account
   - Create test event
   - **Expected:** Success (no 500 error)

3. **Check Logs** (Azure Container Apps)
   ```bash
   az containerapp logs show \
     --name lankaconnect-api-staging \
     --resource-group LankaConnect-RG \
     --follow
   ```
   **Expected:** No authorization failures for EventOrganizerAndBusinessOwner

### Production Environment

Same steps as staging, but use production URLs.

---

## Rollback Plan

**If issues are detected in staging:**

1. **Revert commit:**
   ```bash
   git revert <commit-hash>
   git push origin develop
   ```

2. **Wait for automatic redeployment**

3. **Alternative:** Manually rollback in Azure Portal
   - Navigate to Container App
   - Select "Revisions"
   - Activate previous revision

**Risk Level:** LOW - Changes are additive only, no data or schema changes

---

## Related Documentation

- **Architecture Analysis:** `docs/EVENT_CREATION_500_ERROR_ARCHITECTURE_ANALYSIS.md`
- **Phase 6A Master Index:** `docs/PHASE_6A_MASTER_INDEX.md`
- **Role System Specification:** `src/LankaConnect.Domain/Users/Enums/UserRole.cs`
- **JWT Role Claims Fix:** Commit `c0d457c`

---

## Long-Term Recommendations

### 1. Prevent Policy-Domain Drift

**Problem:** Authorization policies can diverge from domain business rules

**Solution:** Create abstraction that delegates to domain logic

```csharp
// Proposed pattern for future policies
options.AddPolicy("CanCreateEvents", policy =>
    policy.RequireAssertion(context =>
    {
        var roleClaimValue = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<UserRole>(roleClaimValue, out var role))
            return false;

        return role.CanCreateEvents(); // Single source of truth
    }));
```

**Benefits:**
- Eliminates duplicate logic
- Guarantees policy-domain consistency
- Self-documenting code

### 2. Add Policy Consistency Tests

**Create unit tests that validate:**
- Every authorization policy matches corresponding domain logic
- Policy changes trigger test failures if domain logic is unchanged
- Prevents similar issues in future

### 3. Improve Error Handling

**Current Issue:** 500 errors don't include actionable error messages

**Recommendation:** Add global exception handler with correlation IDs

```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var correlationId = Guid.NewGuid();
        var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();

        logger.LogError(exceptionHandler?.Error,
            "Unhandled exception. CorrelationId: {CorrelationId}",
            correlationId);

        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Internal Server Error",
            correlationId = correlationId.ToString(),
            // In development only:
            detail = exceptionHandler?.Error?.Message
        });
    });
});
```

---

## Success Criteria

- [x] Fix applied to all three authorization policies
- [ ] Build succeeds with 0 errors (in progress)
- [ ] All existing tests pass
- [ ] Manual testing with EventOrganizerAndBusinessOwner succeeds
- [ ] No regression in existing roles
- [ ] Deployed to staging successfully
- [ ] Validated in staging environment
- [ ] Deployed to production (after staging validation)

---

**Estimated Time to Production:** ~2 hours (including testing and validation)

**Risk Assessment:** LOW - Additive change with no breaking modifications

**Recommended Timeline:**
1. Build verification: 10 minutes
2. Deploy to staging: 10 minutes
3. Manual testing: 30 minutes
4. Soak time in staging: 1 hour
5. Deploy to production: 10 minutes
6. Production validation: 20 minutes

**Total:** ~2.5 hours from commit to production

---

**Document Version:** 1.0
**Last Updated:** 2025-11-27
**Author:** System Architecture Designer
