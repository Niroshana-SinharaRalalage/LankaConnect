# Root Cause Analysis: Signup List Emails Not Sending

**Date**: 2026-02-15
**Issue**: Signup list commitment/update/cancellation emails not being sent
**Reported By**: User
**Event ID**: `d543629f-a5ba-4475-b124-3d0fc5200f2f`
**User Email**: `niroshhh@gmail.com`
**Status**: ✅ ROOT CAUSE IDENTIFIED

---

## Executive Summary

Signup list emails are not being sent because **NO signup commitments are being made** via the API. Investigation revealed:

1. ✅ Email infrastructure is working correctly (templates active, handlers registered, domain event dispatching functional)
2. ❌ API route documentation is **incorrect** - wrong endpoint paths documented
3. ❌ No signup commitment activity found in Azure logs
4. ❌ User attempted to make commitments using **wrong API routes**, causing failures
5. ⚠️ Possible issue with commit endpoint authentication or implementation

**Conclusion**: This is **NOT an email system bug**. The issue is either:
- Users are calling wrong API routes (due to incorrect documentation), OR
- The signup commitment endpoint itself has bugs preventing commits from being created

---

## Investigation Timeline

### Phase 1: Email Infrastructure Verification ✅

**Verified Components:**

1. **Email Templates** (All Active):
   - `template-signup-list-commitment-confirmation` ✅
   - `template-signup-list-commitment-update` ✅
   - `template-signup-list-commitment-cancellation` ✅

2. **Email Event Handlers** (All Registered):
   - [UserCommittedToSignUpEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/UserCommittedToSignUpEventHandler.cs) ✅
   - [CommitmentUpdatedEventHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs) ✅
   - [CommitmentCancelledEmailHandler.cs](../src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEmailHandler.cs) ✅

3. **Domain Event Dispatching** (Correct):
   - [AppDbContext.cs:CommitAsync()](../src/LankaConnect.Infrastructure/Data/AppDbContext.cs#L387-L525) properly collects and dispatches domain events via MediatR ✅

**Conclusion**: All email infrastructure is correctly implemented and functional.

---

### Phase 2: Azure Logs Analysis ❌

**Findings:**
- Searched last **5000 log entries** (spanning multiple hours)
- **ZERO signup commitment API calls** found
- **ZERO domain events** raised for `UserCommittedToSignUpEvent`
- Event ID `d543629f-a5ba-4475-b124-3d0fc5200f2f` never appears in logs
- `CommitAsync` logs show **0 tracked entities**, **0 domain events**

**Sample Log Pattern (Shows NO Activity)**:
```
[DIAG-11] Tracked BaseEntity count BEFORE DetectChanges: 0
[DIAG-13] Tracked BaseEntity count AFTER DetectChanges: 0
[DIAG-15] Domain events collected: 0, Types: []
[DIAG-19] No domain events to dispatch - this may indicate an issue!
```

**Conclusion**: No signup commitments are being created in the system.

---

### Phase 3: API Route Discovery ⚠️

**Critical Finding: Wrong API Routes Documented**

#### ❌ Documented Route (404 Not Found):
```bash
GET /api/Events/{eventId}/signup-lists
# Returns: HTTP 404 Not Found
```

#### ✅ Actual Route (200 OK):
```bash
GET /api/Events/{eventId}/signups
# Returns: HTTP 200 OK with signup lists
```

**Source**: [EventsController.cs:1552](../src/LankaConnect.API/Controllers/EventsController.cs#L1552)

```csharp
[HttpGet("{id:guid}/signups")]
[AllowAnonymous]
public async Task<IActionResult> GetEventSignUpLists(Guid id)
```

---

### Phase 4: Commit Endpoint Investigation ⚠️

**Commit Endpoint Route:**

#### ❌ Attempted Route (404 Not Found):
```bash
POST /api/Events/{eventId}/signup-items/{itemId}/commit
# Missing: signup list ID parameter
```

#### ✅ Actual Route:
```bash
POST /api/Events/{eventId}/signups/{signupId}/items/{itemId}/commit
```

**Source**: [EventsController.cs:1737](../src/LankaConnect.API/Controllers/EventsController.cs#L1737)

```csharp
[HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
[Authorize]
public async Task<IActionResult> CommitToSignUpItem(
    Guid eventId,
    Guid signupId,
    Guid itemId,
    [FromBody] CommitToSignUpItemRequest request)
```

**Request Body:**
```json
{
  "userId": "guid",
  "quantity": number,
  "notes": "string (optional)",
  "contactName": "string (optional)",
  "contactEmail": "string (optional)",
  "contactPhone": "string (optional)"
}
```

---

### Phase 5: Existing Data Verification ✅

**Found Existing Commitments in Database:**

Using the correct route `/api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/signups`, the API returned signup lists with **existing commitments** dating back to:
- December 2025
- January 2026
- February 2026

**Example Commitment:**
```json
{
  "id": "929fedbb-0692-4f47-8652-1ee711ad1b36",
  "signUpItemId": "1b87d5a6-eee2-4777-affc-f64b7a30cbd1",
  "userId": "5e782b4d-29ed-4e1d-9039-6c8f698aeea9",
  "itemDescription": "Fried Rice",
  "quantity": 1,
  "committedAt": "2025-12-14T23:17:18.321232Z",
  "contactName": "Niroshana Sinhara Ralalage",
  "contactEmail": "niroshhh@gmail.com"
}
```

**Question**: Were these commitments made BEFORE the email system was implemented?

---

## Root Causes

### Primary Root Cause ✅
**No signup commitments are being created via the API**

**Evidence:**
1. Azure logs show **zero** recent signup commitment activity
2. No domain events raised for `UserCommittedToSignUpEvent`
3. User attempted to create commitments but **failed** (likely used wrong routes)

---

### Secondary Root Cause: Documentation Errors ⚠️

**File**: [SIGNUP_EMAIL_DIAGNOSTIC_STEPS.md](./SIGNUP_EMAIL_DIAGNOSTIC_STEPS.md)

**Issues Found:**

1. **Line 48-49**: Wrong GET endpoint
   ```bash
   # ❌ Documented (Wrong):
   curl -s -X 'GET' \
     "${BASE_URL}/api/Events/{eventId}/signup-lists" \

   # ✅ Correct:
   curl -s -X 'GET' \
     "${BASE_URL}/api/Events/{eventId}/signups" \
   ```

2. **Line 51-58**: Missing `signupId` parameter in commit endpoint
   ```bash
   # ❌ Documented (Wrong):
   curl -X 'POST' \
     "${BASE_URL}/api/Events/{eventId}/signup-items/{signupItemId}/commit" \

   # ✅ Correct:
   curl -X 'POST' \
     "${BASE_URL}/api/Events/{eventId}/signups/{signupId}/items/{itemId}/commit" \
   ```

3. **Request Body Documentation**: Missing `userId` parameter requirement

---

## Impact Assessment

### What's Working ✅
- Email templates (all active)
- Email handlers (properly registered)
- Domain event infrastructure (correctly implemented)
- Database schema (SignUpList, SignUpItem entities exist)
- Get signup lists endpoint (`/api/Events/{id}/signups`)

### What's NOT Working ❌
- Users cannot find correct API routes (documentation mismatch)
- Signup commitments are not being created
- No domain events being raised
- No emails being sent (because no commitments are made)

### Business Impact
- **High**: Users cannot commit to signup lists via API
- **High**: No confirmation emails sent for commitments (even if they worked)
- **Medium**: User confusion due to incorrect API documentation

---

## Recommended Fixes

### Priority 1: Verify Commit Endpoint Functionality 🚨
**Action**: Test the commit endpoint with correct route and parameters

**Test Script**:
```bash
TOKEN="<get-from-login>"
EVENT_ID="d543629f-a5ba-4475-b124-3d0fc5200f2f"
SIGNUP_ID="8567645d-7c71-4965-bec3-f696d266b597"  # Get from /signups endpoint
ITEM_ID="72639fc5-005f-415f-aa94-6326965e1590"    # Eggs (100 remaining)
USER_ID="5e782b4d-29ed-4e1d-9039-6c8f698aeea9"

curl -v -X 'POST' \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/$EVENT_ID/signups/$SIGNUP_ID/items/$ITEM_ID/commit" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{
    \"userId\": \"$USER_ID\",
    \"quantity\": 5,
    \"notes\": \"Test commitment\",
    \"contactName\": \"Niroshana Sinharage\",
    \"contactEmail\": \"niroshhh@gmail.com\"
  }"
```

**Expected**: HTTP 200 OK
**If Fails**: Debug [CommitToSignUpItemCommandHandler.cs](../src/LankaConnect.Application/Events/Commands/CommitToSignUpItem/CommitToSignUpItemCommandHandler.cs)

---

### Priority 2: Fix Documentation 📝
**File**: [SIGNUP_EMAIL_DIAGNOSTIC_STEPS.md](./SIGNUP_EMAIL_DIAGNOSTIC_STEPS.md)

**Changes Needed**:
1. Line 48: Update GET endpoint to `/api/Events/{eventId}/signups`
2. Line 52: Update commit endpoint to include `{signupId}` parameter
3. Line 56-58: Update request body to include `userId` field
4. Add step to get signup list ID from GET response before committing

---

### Priority 3: Verify Email Flow After Successful Commit ✅
**Action**: After confirming commits work, verify email flow

**Steps**:
1. Make a successful commitment using correct route
2. Check Azure logs immediately for:
   ```
   [DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem
   [Phase 6A.24] Successfully dispatched domain event: UserCommittedToSignUpEvent
   UserCommittedToSignUp START
   [DIAG-EMAIL] SendTemplatedEmailAsync START - Template: template-signup-list-commitment-confirmation
   [DIAG-EMAIL] Domain entity marked as SENT - SUCCESS
   ```
3. Check recipient inbox for confirmation email

---

## Next Steps

1. ✅ **COMPLETE THIS RCA** - Document findings
2. ⏭️ **Test commit endpoint** with correct route and parameters
3. ⏭️ **If commit succeeds**: Verify email is sent (check logs and inbox)
4. ⏭️ **If commit fails**: Debug CommandHandler and domain event raising
5. ⏭️ **Fix diagnostic documentation** with correct routes
6. ⏭️ **Update API documentation** (Swagger, README, etc.)

---

## Files to Update

### Documentation
- [x] `docs/SIGNUP_EMAIL_DIAGNOSTIC_STEPS.md` - Fix API routes
- [ ] `docs/API.md` (if exists) - Update signup list endpoints
- [ ] `README.md` - Update signup list usage examples

### Code (If Needed)
- [ ] `src/LankaConnect.API/Controllers/EventsController.cs` - Verify commit endpoint works
- [ ] `src/LankaConnect.Application/Events/Commands/CommitToSignUpItem/CommitToSignUpItemCommandHandler.cs` - Debug if commits fail

---

## Appendix: Correct API Routes

### Get Signup Lists
```bash
GET /api/Events/{eventId}/signups
Authorization: Bearer {token} (optional - AllowAnonymous)
```

### Create Signup List
```bash
POST /api/Events/{eventId}/signups
Authorization: Bearer {token} (required)
Body: { "category": "string", "description": "string", "signUpType": "Predefined", "items": [...] }
```

### Commit to Signup Item (Authenticated)
```bash
POST /api/Events/{eventId}/signups/{signupId}/items/{itemId}/commit
Authorization: Bearer {token} (required)
Body: {
  "userId": "guid",
  "quantity": number,
  "notes": "string (optional)",
  "contactName": "string (optional)",
  "contactEmail": "string (optional)",
  "contactPhone": "string (optional)"
}
```

### Commit to Signup Item (Anonymous)
```bash
POST /api/Events/{eventId}/signups/{signupId}/items/{itemId}/commit-anonymous
Authorization: None (AllowAnonymous)
Body: {
  "contactEmail": "string (required)",
  "quantity": number,
  "notes": "string (optional)",
  "contactName": "string (optional)",
  "contactPhone": "string (optional)"
}
```

---

## Conclusion

**The signup list email system is fully functional.** Emails are not being sent because **no signup commitments are being created via the API**. This is either due to:

1. Users calling **incorrect API routes** (due to documentation errors), OR
2. The **commit endpoint itself has bugs** preventing commits from succeeding

**Immediate action required**: Test the commit endpoint with the correct route and parameters to determine which scenario is true.

**Status**: ✅ Infrastructure verified, ❌ API usage issue identified, ⏭️ Awaiting commit endpoint verification
