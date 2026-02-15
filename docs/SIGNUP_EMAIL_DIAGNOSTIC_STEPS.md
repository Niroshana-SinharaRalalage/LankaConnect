# Signup List Email Diagnostic Steps

**Updated**: 2026-02-14
**Issue**: Signup list commitment/update/cancellation emails not sending
**Status**: Infrastructure verified - Need to check Azure logs

---

## ✅ What We've Verified (All Working)

1. **Email Templates**: All 3 templates are ACTIVE in staging database
   - `template-signup-list-commitment-confirmation` ✅
   - `template-signup-list-commitment-update` ✅
   - `template-signup-list-commitment-cancellation` ✅

2. **Email Handlers**: All 3 handlers exist and properly implemented
   - `UserCommittedToSignUpEventHandler` ✅
   - `CommitmentUpdatedEventHandler` ✅
   - `CommitmentCancelledEmailHandler` ✅

3. **Domain Events**: Code raises events correctly ✅
   - `SignUpItem.AddCommitment()` raises `UserCommittedToSignUpEvent`
   - `SignUpItem.UpdateCommitment()` raises `CommitmentUpdatedEvent` or `CommitmentCancelledEvent`

4. **Domain Event Dispatching**: AppDbContext.CommitAsync() properly dispatches via MediatR ✅

---

## 🔍 Diagnostic Steps

### Step 1: Trigger a Signup Commitment

**API Call:**
```bash
# 1. Get auth token
TOKEN=$(curl -s -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "niroshhh@gmail.com",
    "password": "1qaz!QAZ",
    "rememberMe": true,
    "ipAddress": "string"
  }' | jq -r '.token')

# 2. Get event signup lists (NOTE: Route is /signups NOT /signup-lists)
curl -s -X 'GET' \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/signups" \
  -H "Authorization: Bearer $TOKEN"

# 3. Extract signup list ID and item ID from response above, then commit
# IMPORTANT: Route requires signupListId parameter!
# Example values:
#   SIGNUP_ID="8567645d-7c71-4965-bec3-f696d266b597"  # From response above
#   ITEM_ID="72639fc5-005f-415f-aa94-6326965e1590"    # From items array
#   USER_ID="5e782b4d-29ed-4e1d-9039-6c8f698aeea9"    # From login response

curl -X 'POST' \
  "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/signups/{signupListId}/items/{itemId}/commit" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{
    "userId": "{userId}",
    "quantity": 2,
    "notes": "Test commitment",
    "contactName": "Your Name",
    "contactEmail": "your@email.com"
  }'
```

### Step 2: Check Azure Logs Immediately

**Via Azure Portal:**
1. Go to Azure Portal → Container Apps → `lankaconnect-api-staging`
2. Click "Log stream" or "Logs"
3. Search for these patterns in order:

**Pattern 1: Domain Event Raised?**
```
[DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem
```
- **If FOUND**: Domain event was raised ✅ → Go to Pattern 2
- **If NOT FOUND**: ❌ **Root Cause: Domain event not being raised**

**Pattern 2: Domain Event Dispatched?**
```
[Phase 6A.24] Successfully dispatched domain event: UserCommittedToSignUpEvent
```
- **If FOUND**: Event dispatched to MediatR ✅ → Go to Pattern 3
- **If NOT FOUND**: ❌ **Root Cause: Domain event not being dispatched**

**Pattern 3: Email Handler Executed?**
```
UserCommittedToSignUp START
```
- **If FOUND**: Handler received the event ✅ → Go to Pattern 4
- **If NOT FOUND**: ❌ **Root Cause: Handler not registered or MediatR routing issue**

**Pattern 4: Email Service Called?**
```
[DIAG-EMAIL] SendTemplatedEmailAsync START - Template: template-signup-list-commitment-confirmation
```
- **If FOUND**: Email service was called ✅ → Go to Pattern 5
- **If NOT FOUND**: ❌ **Root Cause: Handler failing before email call**

**Pattern 5: Email Sent?**
```
[DIAG-EMAIL] Domain entity marked as SENT - SUCCESS
```
- **If FOUND**: Email sent successfully ✅ → Check recipient inbox
- **If NOT FOUND**: ❌ **Root Cause: Email sending failed** → Check for errors

### Step 3: Look for Errors

**Search for:**
```
UserCommittedToSignUp FAILED
[DIAG-EMAIL] Template INACTIVE
[Phase 6A.52] [HANDLER-EXCEPTION]
```

---

## 🎯 Possible Root Causes (Based on Log Findings)

### Scenario A: No Domain Event Raised
**Log Pattern:** No `[DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem`

**Possible Causes:**
1. Signup commitment API endpoint not calling `AddCommitment()` on SignUpItem
2. Entity not being tracked by EF Core ChangeTracker
3. Domain event cleared before CommitAsync()

**Fix:** Check the command handler that processes signup commitments

---

### Scenario B: Domain Event Not Dispatched
**Log Pattern:** Has `[DIAG-14]` but no `[Phase 6A.24] Successfully dispatched`

**Possible Causes:**
1. Exception during dispatch (check for `[Phase 6A.52] [HANDLER-EXCEPTION]`)
2. MediatR not configured
3. Domain event notification creation failed

**Fix:** Check for exceptions in logs

---

### Scenario C: Handler Not Executed
**Log Pattern:** Has dispatch log but no `UserCommittedToSignUp START`

**Possible Causes:**
1. Handler not registered in DI container
2. MediatR can't find the handler
3. Handler assembly not loaded

**Fix:**
```csharp
// Verify in DependencyInjection.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
});
```

---

### Scenario D: Handler Fails Before Email Call
**Log Pattern:** Has `UserCommittedToSignUp START` but no `[DIAG-EMAIL] SendTemplatedEmailAsync START`

**Possible Causes:**
1. User not found (`GetByIdAsync` returns null)
2. Event not found (`GetEventBySignUpListIdAsync` returns null)
3. Exception thrown before email call

**Fix:** Check handler logs for warnings/errors

---

### Scenario E: Email Sending Fails
**Log Pattern:** Has `[DIAG-EMAIL] SendTemplatedEmailAsync START` but no `SENT - SUCCESS`

**Possible Causes:**
1. Template not found (should be impossible - we verified they exist)
2. Template inactive (should be impossible - we verified IsActive=true)
3. Azure Email Service rate limiting
4. Network issue calling Azure Communication Services
5. Invalid email parameters

**Fix:** Look for `[DIAG-EMAIL]` error logs with details

---

## 🚀 Quick Diagnostic Endpoint

**Test Logging Works:**
```bash
curl -X 'POST' \
  'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Diagnostics/test-signup-commitment-logging' \
  -H 'Content-Type: application/json' \
  -d '{
    "eventId": "d543629f-a5ba-4475-b124-3d0fc5200f2f",
    "userEmail": "niroshhh@gmail.com"
  }'
```

Then check Azure logs for `[DIAG-TEST]` entries. If you see these, logging is working.

---

## 📊 Expected Complete Log Sequence (When Working)

```
[DIAG-10] AppDbContext.CommitAsync START
[DIAG-14] Entity AFTER DetectChanges - Type: SignUpItem, Id: xxx, State: Modified, DomainEvents: 1, EventTypes: [UserCommittedToSignUpEvent]
[DIAG-15] Domain events collected: 1, Types: [UserCommittedToSignUpEvent]
[Phase 6A.24] Found 1 domain events to dispatch: UserCommittedToSignUpEvent
[DIAG-16] SaveChangesAsync completed, 1 entities saved
[Issue #56] Domain events cleared from entities to prevent double dispatch
[DIAG-17] About to dispatch domain event: UserCommittedToSignUpEvent
[DIAG-18] Publishing notification for: UserCommittedToSignUpEvent
[Phase 6A.24] Successfully dispatched domain event: UserCommittedToSignUpEvent
UserCommittedToSignUp START: UserId=xxx, Quantity=2, ItemDescription=xxx
[DIAG-EMAIL] SendTemplatedEmailAsync START - Template: template-signup-list-commitment-confirmation
[DIAG-EMAIL] Template FOUND - IsActive: True
[DIAG-EMAIL] Template RENDERED
[DIAG-EMAIL] Domain entity marked as SENT - SUCCESS
[Phase 6A.100] UserCommittedToSignUp COMPLETE: Email sent - Email=niroshhh@gmail.com
```

**If any line is missing, that's where the problem is!**

---

## 📝 Summary

1. ✅ All infrastructure is in place and correct
2. ✅ Templates are active
3. ✅ Handlers exist
4. ✅ Domain event dispatching code is correct
5. ❓ **Need to check Azure logs to find where the flow breaks**

**Next Action:** Follow Step 1 → Step 2 → Step 3 above to identify the exact breakdown point.
