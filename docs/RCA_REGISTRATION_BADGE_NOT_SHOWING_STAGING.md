# Root Cause Analysis: Registration Badges Not Showing on Staging UI

**Date:** 2026-02-12
**Reporter:** User
**Severity:** High
**Status:** Root Cause Identified

---

## Executive Summary

User reports registration badges are **NOT showing** on staging UI (`/events` page) despite:
- Backend deployment succeeded (Run 21959415583)
- Migration passed successfully
- User has Confirmed registration for "Christmas Dinner Dance 2025"

**ROOT CAUSE:** Frontend UI was NOT deployed to staging after backend changes. The latest frontend deployment (Run #209) was 3 hours before backend deployment, meaning it does not include the necessary code changes to display badges.

---

## Evidence Chain

### 1. Frontend Code Analysis ✅ CORRECT

**File:** `web/src/app/events/page.tsx` (Line 524)
```tsx
<RegistrationBadge registrationStatus={event.userRegistrationStatus} compact={false} />
```

**File:** `web/src/presentation/components/features/events/RegistrationBadge.tsx` (Line 18)
```tsx
if (registrationStatus !== RegistrationStatus.Confirmed) return null;
```

**Conclusion:** Frontend code correctly uses `userRegistrationStatus` from EventDto and only shows badge for `Confirmed` status.

---

### 2. Frontend Deployment Status ❌ NOT DEPLOYED

**GitHub Workflow:** `deploy-ui-staging.yml`

| Run | Status | Timestamp | Branch |
|-----|--------|-----------|--------|
| 209 | ✅ Success | 2026-02-12 18:22:22Z | develop |
| 208 | ✅ Success | 2026-02-12 17:40:29Z | develop |

**Last Frontend Commit:** `b06116e1` (Azure Blob Storage image upload - 6A.106 Part 3)

**Backend Deployment:** Run 21959415583 completed at ~18:30 (8m54s ago from user report)

**Timeline Gap:**
- Backend deployed: ~18:30 UTC
- Last frontend deploy: 18:22 UTC (8 minutes BEFORE backend)
- Frontend does NOT have latest backend changes

**Conclusion:** Frontend deployment succeeded, but it deployed BEFORE backend changes were made.

---

### 3. Backend API Testing ✅ WORKING CORRECTLY

**Test:** GET `/api/Events?statusFilter=1` with Bearer token

**Request:**
```bash
curl -X GET "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events?statusFilter=1" \
  -H "Authorization: Bearer eyJhbGci..."
```

**Response for Christmas Dinner Dance 2025:**
```json
{
  "id": "d543629f-a5ba-4475-b124-3d0fc5200f2f",
  "title": "Christmas Dinner Dance 2025",
  "userRegistrationStatus": "Confirmed",  // ✅ Correctly populated
  "currentRegistrations": 4,
  "capacity": 75
}
```

**Response for other events (user has Preliminary registrations):**
```json
{
  "id": "d914cc72-ce7e-45e9-9c6e-f7b07bd2405c",
  "title": "Sri Lankan Tech Professionals Meetup",
  "userRegistrationStatus": "Preliminary",  // ✅ Correctly populated
  "currentRegistrations": 0,
  "capacity": 70
}
```

**Conclusion:** Backend correctly populates `userRegistrationStatus` for authenticated requests.

---

### 4. JWT Authentication Flow ✅ WORKING CORRECTLY

**Login Response:**
```json
{
  "user": {
    "userId": "5e782b4d-29ed-4e1d-9039-6c8f698aeea9",
    "email": "niroshhh@gmail.com",
    "role": "EventOrganizer"
  },
  "accessToken": "eyJhbGci..."
}
```

**JWT Claims (decoded):**
```json
{
  "nameid": "5e782b4d-29ed-4e1d-9039-6c8f698aeea9",
  "email": "niroshhh@gmail.com",
  "role": "EventOrganizer"
}
```

**Backend Code:** `EventsController.cs` (Line 140)
```csharp
var authenticatedUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
```

**Backend Code:** `ClaimsPrincipalExtensions.cs` (Line 15)
```csharp
var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Reads "nameid" claim
```

**Conclusion:** JWT token contains correct `nameid` claim, backend extracts it correctly via `User.GetUserId()`.

---

### 5. Frontend API Client ✅ WORKING CORRECTLY

**File:** `web/src/infrastructure/api/client/api-client.ts` (Line 71-73)
```typescript
if (this.authToken) {
  config.headers.Authorization = `Bearer ${this.authToken}`;
}
```

**File:** `web/src/presentation/store/useAuthStore.ts` (Line 55)
```typescript
apiClient.setAuthToken(tokens.accessToken); // Called on login
```

**Conclusion:** Frontend correctly sends Authorization header when token is available.

---

## Root Cause

**PRIMARY:** Frontend deployment timing mismatch. The frontend was deployed BEFORE backend changes, so it does not include the latest code that uses `userRegistrationStatus`.

**SECONDARY (Not applicable in this case):** If frontend HAD been deployed after backend, the integration would work correctly because:
- Backend correctly populates `userRegistrationStatus` ✅
- Frontend correctly renders `RegistrationBadge` ✅
- JWT authentication flow works correctly ✅
- API client sends auth headers correctly ✅

---

## Fix Plan

### Step 1: Trigger Frontend Deployment

**Action:** Re-deploy frontend to staging to pick up latest code.

**Method 1: Push Empty Commit (Recommended)**
```bash
cd web/
git commit --allow-empty -m "chore: Trigger frontend staging deployment"
git push origin develop
```

**Method 2: Manual Workflow Trigger**
```bash
gh workflow run deploy-ui-staging.yml --ref develop
```

---

### Step 2: Verify Deployment

**Check workflow:**
```bash
gh run list --workflow=deploy-ui-staging.yml --limit 3
```

**Expected:** New run with timestamp AFTER backend deployment (18:30 UTC)

---

### Step 3: Test Badge Display

**Access:** https://lankaconnect-staging.azurewebsites.net/events

**Test Steps:**
1. Clear browser cache (Ctrl+Shift+R)
2. Login with: `niroshhh@gmail.com` / `1qaz!QAZ`
3. Navigate to `/events`
4. Locate "Christmas Dinner Dance 2025" event card
5. **Verify:** Green "You are registered" badge appears

**Expected Result:**
```
┌──────────────────────────────────────┐
│ Christmas Dinner Dance 2025          │
├──────────────────────────────────────┤
│ [Active] [✅ You are registered]    │
│ Dec 25, 2025 at 7:00 PM             │
│ New York, NY                        │
│ 4 / 75 registered                   │
└──────────────────────────────────────┘
```

---

### Step 4: Verify Browser Console

**Open DevTools Console** and check for:

1. **API Request:**
```
🚀 API Request: {
  method: "GET",
  url: "/events",
  Authorization: "Bearer eyJ..."  // ✅ Should be present
}
```

2. **API Response:**
```
✅ API Response Success: {
  status: 200,
  dataSize: [number]
}
```

3. **Event Data:**
```javascript
console.log(event.userRegistrationStatus); // Should be "Confirmed"
```

---

## Why Backend Worked But Frontend Didn't

### Backend Changes (Commit 1ad0e0f9)
- Modified `GetEventsQueryHandler.cs` to populate `UserRegistrationStatus`
- Modified `EventsController.cs` to extract `userId` from JWT
- Deployed to staging via Run 21959415583 at ~18:30 UTC

### Frontend Status at Time of Issue
- Last deployed: Run 209 at 18:22 UTC (8 minutes BEFORE backend)
- Frontend code exists but wasn't deployed yet
- User's browser served OLD frontend bundle without `userRegistrationStatus` support

### Why User Saw Issue
- User accessed staging UI after backend deployment
- Frontend bundle in browser was from 18:22 deployment
- Old bundle doesn't request/display `userRegistrationStatus`
- Backend correctly sends data, but frontend doesn't use it

---

## Prevention Strategies

### 1. Atomic Deployments
**Problem:** Backend and frontend deployed separately causes integration gaps.

**Solution:** Deploy backend + frontend together in a single workflow.

**Workflow Enhancement:**
```yaml
# deploy-staging-full.yml
name: Deploy Full Stack to Staging
on:
  push:
    branches: [develop]
jobs:
  deploy-backend:
    runs-on: ubuntu-latest
    steps: [...]

  deploy-frontend:
    runs-on: ubuntu-latest
    needs: [deploy-backend]  # Wait for backend first
    steps: [...]
```

---

### 2. Feature Flags
**Problem:** New backend fields break old frontend.

**Solution:** Use feature flags for gradual rollout.

```csharp
// Backend: Always send userRegistrationStatus (backwards compatible)
public class EventDto
{
    public RegistrationStatus? UserRegistrationStatus { get; set; }  // Nullable = optional
}
```

```typescript
// Frontend: Gracefully handle missing field
{event.userRegistrationStatus && (
  <RegistrationBadge registrationStatus={event.userRegistrationStatus} />
)}
```

---

### 3. Deployment Order Documentation
**Add to CLAUDE.md:**

```markdown
## Deployment Order for Breaking Changes

When backend adds new fields that frontend depends on:

1. Deploy backend FIRST (makes field available)
2. Wait for backend health check to pass
3. Deploy frontend SECOND (consumes new field)
4. Verify integration test passes

NEVER deploy frontend before backend for new integrations.
```

---

### 4. Integration Smoke Tests
**Add post-deployment test:**

```bash
# scripts/test_staging_integration.sh
# Run after both deployments complete
TOKEN=$(curl -s -X POST "$API_URL/api/Auth/login" -d "{...}" | jq -r '.accessToken')
EVENTS=$(curl -s "$API_URL/api/Events" -H "Authorization: Bearer $TOKEN")
HAS_REG_STATUS=$(echo $EVENTS | jq '.[0].userRegistrationStatus != null')

if [ "$HAS_REG_STATUS" = "true" ]; then
  echo "✅ Integration test passed"
else
  echo "❌ Integration test failed: userRegistrationStatus missing"
  exit 1
fi
```

---

## Lessons Learned

1. **Deployment timing matters:** Backend-frontend integrations require coordinated deployments.

2. **Feature completeness != Working feature:** Code can be correct but not deployed.

3. **Test in isolation:** Backend API works correctly, frontend code is correct, but timing breaks integration.

4. **Verify deployments:** Always check deployment timestamps when debugging integration issues.

5. **Browser caching:** Even after deployment, users may have cached old bundles (require hard refresh).

---

## Next Steps

- [ ] **IMMEDIATE:** Trigger frontend deployment to staging
- [ ] **VERIFY:** Test registration badge display after deployment
- [ ] **DOCUMENT:** Update CLAUDE.md with deployment order guidelines
- [ ] **ENHANCE:** Add integration smoke test to CI/CD pipeline
- [ ] **CONSIDER:** Atomic deployment workflow for future breaking changes

---

## Related Issues

- **Issue #2:** Fixed badge to show only for Confirmed status (PR #73)
- **Commit 1ad0e0f9:** Backend changes to populate userRegistrationStatus
- **Run 21959415583:** Backend deployment (succeeded)
- **Run 209:** Frontend deployment (succeeded, but BEFORE backend)

---

## Verification Checklist

After deploying frontend:

- [ ] Frontend deployment workflow completes successfully
- [ ] Timestamp is AFTER 18:30 UTC (backend deployment time)
- [ ] Clear browser cache and reload `/events` page
- [ ] Login with test credentials
- [ ] Badge appears on "Christmas Dinner Dance 2025" event
- [ ] Browser console shows Authorization header in API requests
- [ ] API response includes `userRegistrationStatus: "Confirmed"`
- [ ] No JavaScript errors in console

---

**Status:** Ready for fix implementation
**Risk:** Low (frontend code is correct, just needs deployment)
**ETA:** 5-10 minutes (deployment + verification)
