# Root Cause Analysis: Resend Confirmation Endpoint Investigation
## Phase 6A.X - January 26, 2026

---

## Executive Summary

The resend confirmation endpoint returns **200 OK** but **does not create tickets or send emails**. Investigation reveals a silent failure with **no execution logs**, indicating the endpoint code may not be deployed to Azure staging despite commits being present in origin/develop.

---

## Investigation Timeline

### 1. Initial Problem
- **Symptom**: Attendees API returns `ticketCode: null`, `qrCodeData: null`, `hasTicket: false`
- **User Request**: Test resend confirmation endpoint to generate missing tickets

### 2. Database Fix (PaymentStatus Enum Correction)
- **Issue**: Registration had `PaymentStatus = 2` (Failed) instead of `1` (Completed)
- **Root Cause**: Enum value confusion - Completed = 1, not 2
- **Fix**: Updated database: `UPDATE events.registrations SET "PaymentStatus" = 1`
- **Result**: Registration now shows `PaymentStatus = 1` (Completed) ✅

### 3. API Testing
```bash
POST /api/Events/d543629f-a5ba-4475-b124-3d0fc5200f2f/attendees/18422a29-61f7-4575-87d2-72ac0b1581d1/resend-confirmation
Authorization: Bearer <valid_organizer_token>

Response: 200 OK
Body: {"message": "Confirmation email resent successfully"}
```
- ✅ Endpoint returns success
- ❌ No ticket created
- ❌ No email sent

### 4. Database Investigation
**Query Results:**
```sql
-- Registration Check
SELECT "Id", "Status", "PaymentStatus", "UpdatedAt"
FROM events.registrations
WHERE "Id" = '18422a29-61f7-4575-87d2-72ac0b1581d1';

Result:
- PaymentStatus: 1 (Completed) ✅
- Status: Confirmed ✅
- UpdatedAt: 2026-01-26 14:06:28 UTC

-- Ticket Check
SELECT * FROM events.tickets
WHERE "RegistrationId" = '18422a29-61f7-4575-87d2-72ac0b1581d1';

Result: NO ROWS ❌

-- Email Check
SELECT * FROM communications.email_messages
WHERE template_data::text LIKE '%18422a29-61f7-4575-87d2-72ac0b1581d1%';

Result: NO ROWS ❌
```

### 5. Azure Container Logs Investigation
**Commands Executed:**
```bash
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 1000
```

**Search Filters:**
- `grep -i "resend"`
- `grep -i "Phase 6A.X"`
- `grep -i "18422a29-61f7-4575-87d2-72ac0b1581d1"`
- `grep -i "ResendAttendeeConfirmation"`

**Result**: NO LOGS FOUND ❌

**Expected Logs (from ResendAttendeeConfirmationCommandHandler.cs):**
```csharp
// Line 2077-2079: Controller entry log
Logger.LogInformation("[Phase 6A.X] API: Resending attendee confirmation - EventId={EventId}...");

// Line 135-138: Handler start log
_logger.LogInformation("ResendAttendeeConfirmation START: EventId={EventId}, RegistrationId={RegistrationId}...");

// Line 142-145: Registration loaded log
_logger.LogInformation("ResendAttendeeConfirmation: Registration loaded - Status={Status}, PaymentStatus={PaymentStatus}");

// ... and 10+ more logging statements throughout execution
```

---

## Root Cause Analysis

### Evidence

| Evidence Type | Finding | Implication |
|---------------|---------|-------------|
| **API Response** | 200 OK with success message | Endpoint exists and responds |
| **Database - Tickets** | No ticket record | Ticket generation never executed |
| **Database - Emails** | No email record | Email sending never executed |
| **Azure Logs** | Zero logs for operation | Handler code never executed |
| **Code Review** | Extensive logging present | Logs SHOULD appear if code runs |

### Conclusion

**The handler code is NOT executing despite the endpoint returning 200 OK.**

### Possible Explanations

#### 1. **Deployment Issue** (Most Likely - 80% probability)
**Hypothesis**: Azure container running old code without resend feature

**Evidence:**
- Commits ARE in origin/develop:
  - `d8c60f10` - feat(phase-6a-resend-email): Add organizer resend confirmation
  - `5f78f084` - feat(phase-6a-qr-code): Add ticket code to attendees table
- GitHub Actions workflow #21362301457 completed successfully
- But no execution logs appear in container

**Possible Causes:**
- Container image cached/not rebuilt
- Deploy step succeeded but container not restarted
- Configuration issue preventing new code deployment

#### 2. **Endpoint Collision/Override** (Medium Probability - 15%)
**Hypothesis**: Another endpoint matching the same route returns 200 OK

**Evidence Against:**
- Specific route: `{id:guid}/attendees/{registrationId:guid}/resend-confirmation`
- Unlikely to have duplicate

#### 3. **Silent Exception Before Logging** (Low Probability - 5%)
**Hypothesis**: Exception thrown before any logging occurs

**Evidence Against:**
- Controller has immediate logging (line 2077)
- Try-catch blocks with logging present
- Would see error response, not 200 OK

---

## Verification Steps Performed

### ✅ Code Verification
- [x] Confirmed ResendAttendeeConfirmationCommandHandler.cs exists and has logging
- [x] Confirmed EventsController.cs has endpoint at line 2060-2093
- [x] Confirmed commits are in origin/develop
- [x] Verified extensive logging throughout handler execution path

### ✅ Database Verification
- [x] Confirmed registration PaymentStatus = Completed
- [x] Confirmed no ticket exists for registration
- [x] Confirmed no email records exist for registration

### ✅ Deployment Verification
- [x] Checked GitHub Actions workflow status
- [x] Last successful deploy: workflow #21362301457 at 14:59:13 UTC
- [x] Commits BEFORE this workflow, so SHOULD be deployed

### ⏳ Outstanding Verification
- [ ] Check actual deployed DLL files in Azure container
- [ ] Verify container restart after deployment
- [ ] Test endpoint with invalid data to confirm error handling

---

## Recommended Actions

### 🔴 IMMEDIATE (Critical Path)

**Option A: Force Redeploy** (Recommended)
```bash
# 1. Create empty commit to trigger deployment
git commit --allow-empty -m "chore: force Azure staging redeploy for Phase 6A.X features"

# 2. Push to trigger GitHub Actions
git push origin develop

# 3. Wait for deployment (#21364735172 currently queued)
gh run watch 21364735172

# 4. Test endpoint again
python scripts/test_resend_confirmation.py

# 5. Check for logs
az containerapp logs show --name lankaconnect-api-staging --tail 200 | grep "Phase 6A.X"
```

**Option B: Manual Container Restart**
```bash
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --revision <revision-name>
```

### 🟡 FOLLOW-UP (After Deployment)

1. **Verify Deployment Completed**:
   - Check GitHub Actions logs for successful build + deploy
   - Confirm Azure container restart timestamp

2. **Test Endpoint Again**:
   ```bash
   python scripts/test_resend_confirmation.py
   ```

3. **Verify Expected Behavior**:
   - Check Azure logs for `[Phase 6A.X]` entries
   - Query database for created ticket
   - Query database for queued email
   - Verify attendees API returns ticket data

4. **Document Results**:
   - Update PROGRESS_TRACKER.md
   - Update STREAMLINED_ACTION_PLAN.md

---

## Test Scripts Created

### 1. `scripts/fix_payment_status.py`
- **Purpose**: Update registration PaymentStatus from Pending to Completed
- **Status**: ✅ Executed successfully
- **Result**: Registration PaymentStatus = 1 (Completed)

### 2. `scripts/test_resend_confirmation.py`
- **Purpose**: Test resend confirmation endpoint
- **Status**: ✅ Returns 200 OK (but no ticket/email created)

### 3. `scripts/test_attendees_api.py`
- **Purpose**: Verify attendees API returns ticket fields
- **Status**: ✅ API working, LEFT JOIN working, but ticket fields are null (expected)

### 4. `scripts/check_ticket_status.py`
- **Purpose**: Query database for ticket and email records
- **Status**: ✅ Confirmed no ticket or email records exist

---

## Expected Behavior After Proper Deployment

### When Endpoint Executes Correctly:

1. **Azure Container Logs** should show:
   ```
   [Phase 6A.X] API: Resending attendee confirmation - EventId=..., RegistrationId=...
   ResendAttendeeConfirmation START: EventId=..., RegistrationId=...
   ResendAttendeeConfirmation: Registration loaded - Status=Confirmed, PaymentStatus=Completed
   ResendAttendeeConfirmation: Existing ticket not found - generating new ticket
   ResendAttendeeConfirmation: Ticket generated successfully - TicketCode=LC-2026-XXXXXX
   ResendAttendeeConfirmation: Sending paid event confirmation - TicketCode=...
   ResendAttendeeConfirmation COMPLETE: Email sent successfully
   ```

2. **Database - events.tickets** should have:
   ```sql
   SELECT "TicketCode", "QrCodeData", "IsValid"
   FROM events.tickets
   WHERE "RegistrationId" = '18422a29-61f7-4575-87d2-72ac0b1581d1';

   -- Expected result: 1 row with ticket code and QR data
   ```

3. **Database - communications.email_messages** should have:
   ```sql
   SELECT template_name, status, "CreatedAt"
   FROM communications.email_messages
   WHERE template_data::text LIKE '%18422a29-61f7-4575-87d2-72ac0b1581d1%';

   -- Expected result: 1 row with status='Queued' or 'Sent'
   ```

4. **Attendees API** should return:
   ```json
   {
     "registrationId": "18422a29-61f7-4575-87d2-72ac0b1581d1",
     "hasTicket": true,
     "ticketCode": "LC-2026-XXXXXX",
     "qrCodeData": "base64encodedstring..."
   }
   ```

---

## Lessons Learned

### 1. Always Verify Deployment
- ✅ Check GitHub Actions success
- ✅ Check Azure container logs for recent activity
- ✅ Test endpoint behavior matches expected code path

### 2. Silent Failures Are Dangerous
- 200 OK response without execution = misleading success
- Always check database state after API calls
- Always verify logs show expected operations

### 3. Comprehensive Logging is Essential
- ResendAttendeeConfirmationCommandHandler has excellent logging
- This logging revealed the handler never executed
- Without it, debugging would have been much harder

---

## Status

**Current State**: ❌ Feature NOT working (suspected deployment issue)

**Next Action**: Wait for workflow #21364735172 to complete, then retest

**Owner**: Niroshana Sinharage

**Date**: 2026-01-26

---

## Appendix: Related Files

### Backend Feature Files
- `src/LankaConnect.Application/Common/Interfaces/IRegistrationEmailService.cs` (NEW)
- `src/LankaConnect.Infrastructure/Services/RegistrationEmailService.cs` (NEW)
- `src/LankaConnect.Application/Events/Commands/ResendAttendeeConfirmation/` (NEW)
  - `ResendAttendeeConfirmationCommand.cs`
  - `ResendAttendeeConfirmationCommandValidator.cs`
  - `ResendAttendeeConfirmationCommandHandler.cs`
- `src/LankaConnect.API/Controllers/EventsController.cs` (MODIFIED - lines 2060-2093)
- `src/LankaConnect.Infrastructure/DependencyInjection.cs` (MODIFIED - added service registration)

### Database Scripts
- `scripts/fix_payment_status.py`
- `scripts/check_ticket_status.py`
- `docs/phase-6a-x/fix_payment_status_for_testing.sql`

### Test Scripts
- `scripts/test_resend_confirmation.py`
- `scripts/test_attendees_api.py`

### Documentation
- `docs/phase-6a-x/TESTING_RESEND_CONFIRMATION.md`
- `docs/phase-6a-x/RCA_RESEND_ENDPOINT_INVESTIGATION.md` (THIS FILE)
