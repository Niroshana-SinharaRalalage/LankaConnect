# Fix Validation Plan: Phase 6A.44 Anonymous Sign-Up Issues

**Date:** 2024-12-24
**Status:** Ready for Testing
**Estimated Testing Time:** 2-3 hours
**Environment:** Azure Staging

---

## Quick Reference

| Issue | Root Cause | Fix Status | Test Priority |
|-------|-----------|------------|---------------|
| **Issue 1:** Email validation error for mandatory items | CheckEventRegistration doesn't filter cancelled registrations | ✅ Fixed | HIGH |
| **Issue 2:** Token refresh error for Open Items | Missing anonymous endpoint | ✅ Fixed | HIGH |

---

## Pre-Test Requirements

### Environment Setup
- ✅ Backend deployed to Azure staging: `https://lankaconnect-api-staging.azurewebsites.net`
- ✅ Frontend running locally: `http://localhost:3000`
- ✅ Database: Azure staging PostgreSQL
- ✅ Stripe: Test mode keys configured

### Test Accounts Needed
```
Anonymous Test User 1:
  Email: test-anon@example.com
  Status: Active registration (Confirmed)

Anonymous Test User 2:
  Email: cancelled@example.com
  Status: Cancelled registration

Member Test User:
  Email: member@example.com
  Status: Has LankaConnect account
```

### Test Event Requirements
- Event must be PAID (Stripe checkout enabled)
- Event must have sign-up list with `HasOpenItems = true`
- Event must have mandatory/preferred/suggested items

---

## Testing Sequence

### Phase 1: Backend API Validation (1 hour)

#### Test 1.1: CheckEventRegistration - Active Registration
**Endpoint:** `POST /api/Events/{eventId}/check-registration`

**Request:**
```http
POST https://lankaconnect-api-staging.azurewebsites.net/api/Events/{{eventId}}/check-registration
Content-Type: application/json

{
  "email": "test-anon@example.com"
}
```

**Expected Response:** HTTP 200
```json
{
  "hasUserAccount": false,
  "isRegisteredForEvent": true,
  "canCommitAnonymously": true
}
```

**Pass Criteria:**
- [x] `isRegisteredForEvent` = true
- [x] `canCommitAnonymously` = true
- [x] Response time < 100ms

---

#### Test 1.2: CheckEventRegistration - Cancelled Registration
**Setup:** Update registration status to Cancelled in database

**Request:** Same as 1.1 with `cancelled@example.com`

**Expected Response:** HTTP 200
```json
{
  "isRegisteredForEvent": false,
  "needsEventRegistration": true
}
```

**Pass Criteria:**
- [x] `isRegisteredForEvent` = false (cancelled registrations don't count)
- [x] `needsEventRegistration` = true
- [x] `canCommitAnonymously` = false

---

#### Test 1.3: AddOpenSignUpItemAnonymous - Success
**Endpoint:** `POST /api/Events/{eventId}/signups/{signupId}/open-items-anonymous`

**Request:**
```http
POST https://lankaconnect-api-staging.azurewebsites.net/api/Events/{{eventId}}/signups/{{signupId}}/open-items-anonymous
Content-Type: application/json

{
  "contactEmail": "test-anon@example.com",
  "itemName": "Homemade Cookies",
  "quantity": 2,
  "notes": "Chocolate chip",
  "contactName": "Test User",
  "contactPhone": "+1-555-1234"
}
```

**Expected Response:** HTTP 200
```json
"{{itemId}}"  // GUID of created item
```

**Pass Criteria:**
- [x] HTTP 200 OK
- [x] Returns valid GUID
- [x] Response time < 500ms

**Database Verification:**
```sql
SELECT
    si."Id",
    si."ItemDescription",
    si."Quantity",
    c."UserId",
    c."ContactEmail"
FROM "SignUpItems" si
INNER JOIN "SignUpItemCommitments" c ON si."Id" = c."SignUpItemId"
WHERE si."ItemDescription" = 'Homemade Cookies'
```

**Expected Data:**
- [x] ItemDescription = "Homemade Cookies"
- [x] Quantity = 2
- [x] ContactEmail = "test-anon@example.com"
- [x] UserId is deterministic GUID (not in Users table)

---

#### Test 1.4: AddOpenSignUpItemAnonymous - Not Registered
**Request:** Same as 1.3 with unregistered email

**Expected Response:** HTTP 400
```json
{
  "detail": "NOT_REGISTERED:You must be registered for this event to add items. Please register for the event first."
}
```

**Pass Criteria:**
- [x] HTTP 400 Bad Request
- [x] Error message starts with "NOT_REGISTERED:"

---

#### Test 1.5: CommitToSignUpItemAnonymous - Active Registration
**Endpoint:** `POST /api/Events/{eventId}/signups/{signupId}/items/{itemId}/commit-anonymous`

**Request:**
```http
POST https://lankaconnect-api-staging.azurewebsites.net/api/Events/{{eventId}}/signups/{{signupId}}/items/{{itemId}}/commit-anonymous
Content-Type: application/json

{
  "contactEmail": "test-anon@example.com",
  "quantity": 1,
  "notes": "Will bring",
  "contactName": "Test User",
  "contactPhone": "+1-555-5678"
}
```

**Expected Response:** HTTP 200
```json
"{{commitmentId}}"  // GUID
```

**Pass Criteria:**
- [x] HTTP 200 OK
- [x] Returns commitment GUID
- [x] Item.RemainingQuantity decreases by 1

---

### Phase 2: Frontend Integration Testing (1 hour)

#### Test 2.1: Anonymous Open Item Creation Flow
**Prerequisites:**
- User NOT logged in
- User has completed paid event registration via Stripe
- Event has sign-up list with Open Items enabled

**Steps:**
1. Navigate to `http://localhost:3000/events/{{eventId}}`
2. Click "Sign-Up Lists" tab
3. Find sign-up list with "Open Items" section
4. Click "Add Your Own Item" button
5. Fill form:
   - Item Name: "Homemade Brownies"
   - Quantity: 3
   - Notes: "Chocolate brownies"
6. Click "Add Item"

**Expected Behavior:**
- [x] No "Token refresh failed" error
- [x] Success toast: "Item added successfully"
- [x] Item appears in list immediately
- [x] Shows "You: 3" (user's commitment)
- [x] No console errors
- [x] No 401 Unauthorized errors

**Failure Scenarios:**
- [x] User not registered → Shows "Please register for event first"
- [x] Email is member account → Shows "Please log in"

---

#### Test 2.2: Anonymous Mandatory Item Commitment
**Steps:**
1. Navigate to sign-up list with mandatory items
2. Click "Sign Up" on a mandatory item
3. Fill commitment form
4. Submit

**Expected Behavior:**
- [x] No "This email is not registered for the event" error
- [x] Commitment saves successfully
- [x] Remaining quantity decreases
- [x] User's commitment shown in list
- [x] No page refresh required

---

### Phase 3: Database Validation (30 minutes)

#### Validation 3.1: Anonymous Sign-Up Items
```sql
SELECT
    si."Id",
    si."ItemDescription",
    si."Quantity",
    c."UserId",
    c."ContactEmail",
    c."ContactName",
    u."Id" as RealUserId
FROM "SignUpItems" si
INNER JOIN "SignUpItemCommitments" c ON si."Id" = c."SignUpItemId"
LEFT JOIN "Users" u ON c."UserId" = u."Id"
WHERE si."IsOpenItem" = true
ORDER BY si."CreatedOn" DESC
LIMIT 10;
```

**Expected Results:**
- [x] Anonymous commitments have RealUserId = NULL
- [x] All anonymous commitments have ContactEmail populated
- [x] UserId is deterministic (same email = same UserId)

---

#### Validation 3.2: Deterministic UserId Consistency
```sql
SELECT
    c."ContactEmail",
    c."UserId",
    COUNT(*) as CommitmentCount,
    COUNT(DISTINCT c."UserId") as DistinctUserIds
FROM "SignUpItemCommitments" c
LEFT JOIN "Users" u ON c."UserId" = u."Id"
WHERE u."Id" IS NULL  -- Only anonymous commitments
GROUP BY c."ContactEmail", c."UserId"
HAVING COUNT(DISTINCT c."UserId") > 1;
```

**Expected Results:**
- [x] **EMPTY RESULT SET** (no duplicate UserIds for same email)
- If result set has rows → BUG in deterministic GUID generation

---

## Test Results Template

### Backend API Tests
| Test | Status | Response Time | Notes |
|------|--------|--------------|-------|
| 1.1 Active Registration | ⏳ Pending | | |
| 1.2 Cancelled Registration | ⏳ Pending | | |
| 1.3 Add Open Item (Success) | ⏳ Pending | | |
| 1.4 Add Open Item (Not Registered) | ⏳ Pending | | |
| 1.5 Commit Mandatory Item | ⏳ Pending | | |

### Frontend Tests
| Test | Status | Notes |
|------|--------|-------|
| 2.1 Anonymous Open Item Creation | ⏳ Pending | |
| 2.2 Anonymous Mandatory Commitment | ⏳ Pending | |

### Database Validation
| Test | Status | Notes |
|------|--------|-------|
| 3.1 Anonymous Items Query | ⏳ Pending | |
| 3.2 Deterministic UserId Check | ⏳ Pending | |

---

## Quick Curl Commands

### Check Registration (Active)
```bash
curl -X POST "https://lankaconnect-api-staging.azurewebsites.net/api/Events/{{eventId}}/check-registration" \
  -H "Content-Type: application/json" \
  -d '{"email":"test-anon@example.com"}'
```

### Add Open Item (Anonymous)
```bash
curl -X POST "https://lankaconnect-api-staging.azurewebsites.net/api/Events/{{eventId}}/signups/{{signupId}}/open-items-anonymous" \
  -H "Content-Type: application/json" \
  -d '{
    "contactEmail": "test-anon@example.com",
    "itemName": "Test Item",
    "quantity": 1,
    "notes": "Test note",
    "contactName": "Test User",
    "contactPhone": "+1-555-1234"
  }'
```

### Commit to Mandatory Item (Anonymous)
```bash
curl -X POST "https://lankaconnect-api-staging.azurewebsites.net/api/Events/{{eventId}}/signups/{{signupId}}/items/{{itemId}}/commit-anonymous" \
  -H "Content-Type: application/json" \
  -d '{
    "contactEmail": "test-anon@example.com",
    "quantity": 1,
    "contactName": "Test User",
    "contactPhone": "+1-555-5678"
  }'
```

---

## Issue Resolution Checklist

### Issue 1: Email Validation Error
- [x] Fix implemented: Added status filter to CheckEventRegistrationQueryHandler
- [ ] Backend test 1.1 passed (active registration)
- [ ] Backend test 1.2 passed (cancelled registration)
- [ ] Frontend test 2.2 passed (mandatory item commitment)
- [ ] No "not registered" errors for active registrations
- [ ] Database shows correct filtering behavior

### Issue 2: Token Refresh Error
- [x] Fix implemented: Created AddOpenSignUpItemAnonymous endpoint
- [ ] Backend test 1.3 passed (add Open item success)
- [ ] Backend test 1.4 passed (not registered error)
- [ ] Frontend test 2.1 passed (anonymous Open item creation)
- [ ] No 401 errors for anonymous endpoints
- [ ] No "token refresh failed" errors

---

## Success Criteria

### Functional
- ✅ Anonymous users can commit to mandatory items without validation errors
- ✅ Anonymous users can add Open items without authentication errors
- ✅ Cancelled registrations correctly excluded
- ✅ Member accounts prompted to log in

### Performance
- ✅ CheckEventRegistration query < 100ms
- ✅ AddOpenSignUpItemAnonymous endpoint < 500ms
- ✅ No database deadlocks or connection pool issues

### User Experience
- ✅ Zero friction for anonymous sign-ups
- ✅ Clear error messages for each scenario
- ✅ No confusing technical errors (token refresh, 401, etc.)

---

## Deployment Sign-Off

### Pre-Production Checklist
- [ ] All backend API tests passed
- [ ] All frontend integration tests passed
- [ ] Database validation queries executed successfully
- [ ] No console errors or warnings
- [ ] Application Insights showing no exceptions
- [ ] Performance within acceptable limits

### Production Deployment
- [ ] Backend deployed to production
- [ ] Frontend deployed to production
- [ ] Smoke tests executed on production
- [ ] Monitoring alerts configured
- [ ] Rollback plan documented and tested

### Post-Deployment
- [ ] Monitor for 24 hours
- [ ] Check Application Insights for errors
- [ ] Review user feedback
- [ ] Update Phase 6A.44 summary documentation

---

**Document Version:** 1.0
**Last Updated:** 2024-12-24
**Owner:** QA Lead
**Approvers:** Backend Lead, Frontend Lead, Product Owner
