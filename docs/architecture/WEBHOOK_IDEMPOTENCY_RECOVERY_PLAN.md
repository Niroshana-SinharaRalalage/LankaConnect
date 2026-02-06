# Webhook Idempotency Recovery Plan

## Executive Summary

**Problem**: User completed payment at 9:59 PM UTC, old Container App revision processed webhook successfully (HTTP 200) but failed to send email/ticket due to IPublisher NULL bug. Current revision cannot reprocess due to idempotency check.

**Solution**: Manual domain event trigger via temporary admin endpoint to generate email and ticket without modifying payment state.

**Registration ID**: `219dd972-2309-4898-a972-4e0b6a7224fb`
**Webhook Event ID**: `evt_1SfSktLvfbr023L1qB78D1CR`

---

## Architectural Decision

### Option Selected: **Domain Event Manual Trigger (Option 4)**

#### Rationale

| Criteria | Evaluation |
|----------|------------|
| **Data Integrity** | ✅ Excellent - No modification to payment/registration state |
| **Audit Trail** | ✅ Excellent - Preserves original webhook processing record |
| **Simplicity** | ✅ Good - Single API call, no database modification |
| **Risk** | ✅ Low - Idempotent operation, can be retried safely |
| **Deployment** | ✅ Code already deployed, just need to call endpoint |

#### Why NOT Other Options

**Option 1 (Reset Idempotency Flag)**:
- ❌ Violates idempotency guarantees
- ❌ Risk of duplicate processing if webhook fires again
- ❌ Could cause double-charging or duplicate records

**Option 2 (Manual Service Call)**:
- ❌ Bypasses domain event architecture
- ❌ Creates inconsistency in how tickets are generated
- ❌ May miss future business logic in event handler

**Option 3 (New Payment)**:
- ❌ Creates accounting complexity (refund + new charge)
- ❌ Poor user experience
- ❌ Requires business process changes

---

## Architecture Overview

### Normal Flow (What Should Have Happened)

```
Stripe Webhook (9:59 PM)
    ↓
PaymentsController.Webhook() - Line 225
    ↓
HandleCheckoutSessionCompletedAsync() - Line 300
    ↓
registration.CompletePayment() - Registration.cs:235
    ↓  Raises PaymentCompletedEvent
    ↓
_unitOfWork.CommitAsync()
    ↓
AppDbContext.CommitAsync() - Line 294
    ↓  Collects domain events (Line 311)
    ↓  Saves to database (Line 325)
    ↓  Dispatches via _publisher.Publish() - Line 343
    ↓
PaymentCompletedEventHandler.Handle() - Line 43
    ↓
├─ Generate Ticket (Line 146-176)
└─ Send Email with PDF (Line 220)
```

### What Actually Happened (Old Revision Bug)

```
Stripe Webhook (9:59 PM)
    ↓
PaymentsController.Webhook() - OLD REVISION
    ↓
HandleCheckoutSessionCompletedAsync()
    ↓
registration.CompletePayment() ✅
    ↓  Raises PaymentCompletedEvent ✅
    ↓
_unitOfWork.CommitAsync() ✅
    ↓
AppDbContext.CommitAsync()
    ↓  Collects domain events ✅
    ↓  Saves to database ✅
    ↓  Dispatches via _publisher.Publish() ❌ _publisher was NULL
    ↓
    X STOPPED HERE - Event never dispatched
    X PaymentCompletedEventHandler NEVER invoked
    X No ticket generated
    X No email sent
```

### Recovery Flow (This Solution)

```
Admin triggers recovery endpoint
    ↓
POST /api/admin/recovery/trigger-payment-event
    ↓
AdminRecoveryController.TriggerPaymentCompletedEvent()
    ↓
├─ Verify registration exists ✅
├─ Verify PaymentStatus = Completed ✅
├─ Verify RegistrationStatus = Confirmed ✅
├─ Retrieve event & user details ✅
├─ Construct PaymentCompletedEvent manually ✅
└─ Publish via _publisher.Publish()
    ↓
PaymentCompletedEventHandler.Handle() - Line 43
    ↓
├─ Generate Ticket (Line 146-176) ✅
└─ Send Email with PDF (Line 220) ✅
```

---

## Implementation Steps

### Step 1: Pre-Deployment Verification

Run SQL script to verify current state:

```bash
# Execute verification script
sqlcmd -S <server> -d LankaConnect -i scripts/recover-payment-completed-event.sql
```

**Expected Output**:
- ✅ Registration Status: Confirmed (enum 1)
- ✅ PaymentStatus: Completed (enum 2)
- ✅ StripePaymentIntentId: Populated
- ✅ Webhook event: Processed = true
- ❌ Ticket: Does NOT exist (this is what we're fixing)

**If Output Differs**:
- STOP and investigate before proceeding
- Ticket already exists → No action needed
- Payment not completed → Contact user for payment issue
- Registration not confirmed → Investigate webhook processing

---

### Step 2: Deploy Recovery Controller

The recovery controller has already been created at:
`src/LankaConnect.API/Controllers/AdminRecoveryController.cs`

**Deployment**:

```bash
# Build and deploy
dotnet build src/LankaConnect.API/LankaConnect.API.csproj
dotnet publish -c Release
# Deploy to Azure Container App (follow your deployment process)
```

**Security Note**: Controller requires `[Authorize]` attribute. In production, add admin role requirement.

---

### Step 3: Execute Recovery

#### Option A: Via API Tool (Postman/curl)

```bash
curl -X POST https://lankaconnect.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{
    "registrationId": "219dd972-2309-4898-a972-4e0b6a7224fb"
  }'
```

#### Option B: Via Swagger UI

1. Navigate to `https://lankaconnect.azurecontainerapps.io/swagger`
2. Authenticate using JWT token
3. Find `POST /api/admin/recovery/trigger-payment-event`
4. Execute with body:
   ```json
   {
     "registrationId": "219dd972-2309-4898-a972-4e0b6a7224fb"
   }
   ```

**Expected Response** (HTTP 200):

```json
{
  "message": "PaymentCompletedEvent published successfully",
  "registrationId": "219dd972-2309-4898-a972-4e0b6a7224fb",
  "eventId": "<event-guid>",
  "contactEmail": "<user-email>",
  "amountPaid": 250.00,
  "attendeeCount": 1,
  "note": "Email and ticket generation should complete within 30 seconds. Check application logs for PaymentCompletedEventHandler."
}
```

---

### Step 4: Verification Checklist

#### Immediate Verification (< 1 minute)

- [ ] API returns HTTP 200 with success message
- [ ] No error messages in response body
- [ ] Response contains expected registration/event IDs

#### Application Logs (< 2 minutes)

Check Azure Container App logs for:

```
[ADMIN RECOVERY] Manual PaymentCompletedEvent trigger requested for Registration 219dd972-2309-4898-a972-4e0b6a7224fb
[ADMIN RECOVERY] Publishing PaymentCompletedEvent - Event: {guid}, Registration: {guid}, Email: {email}, Amount: {amount}
[ADMIN RECOVERY] Successfully published PaymentCompletedEvent for Registration 219dd972-2309-4898-a972-4e0b6a7224fb
[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED - Event {guid}, Registration 219dd972-2309-4898-a972-4e0b6a7224fb
Ticket generated successfully: {ticket-code}
Ticket PDF retrieved successfully, size: {bytes} bytes
Payment confirmation email sent successfully to {email} for Registration 219dd972-2309-4898-a972-4e0b6a7224fb
```

**If Error Logs Appear**:
- Check specific error message
- Common issues:
  - Email service configuration
  - Ticket service PDF generation
  - Database connectivity
- Event handler uses fail-silent pattern, so errors won't rollback transaction

#### Database Verification (< 5 minutes)

```sql
-- Verify ticket created
SELECT * FROM Tickets WHERE RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb';

-- Should return 1 row with:
-- - TicketCode populated
-- - Status = Active
-- - QRCodeData populated
-- - GeneratedAt timestamp
```

#### User Verification (< 10 minutes)

- [ ] User receives email at contact address
- [ ] Email contains event details and attendee information
- [ ] Email has PDF attachment (ticket-{code}.pdf)
- [ ] PDF contains QR code and ticket details
- [ ] User can open PDF successfully

---

### Step 5: Post-Recovery Cleanup

#### Remove Temporary Controller

**IMPORTANT**: Delete the recovery controller after successful recovery:

```bash
# Delete the temporary controller
rm src/LankaConnect.API/Controllers/AdminRecoveryController.cs

# Rebuild and redeploy
dotnet build src/LankaConnect.API/LankaConnect.API.csproj
# Deploy to Azure Container App
```

#### Update Documentation

- [ ] Document incident in post-mortem
- [ ] Update runbook for future similar issues
- [ ] Archive recovery scripts in `scripts/archive/`

---

## Rollback Plan

### If Recovery Fails

**Scenario**: API call returns error or logs show failure

**Action**:
1. **DO NOT RETRY IMMEDIATELY** - Investigate root cause first
2. Check application logs for specific error
3. Verify database state hasn't changed (registration/payment status)
4. Common failure points:
   - Email service configuration (Azure Communication Services)
   - Ticket PDF generation (QRCoder library)
   - Template rendering (Liquid templates)

**Rollback Steps**:
- No rollback needed - operation is read-only and idempotent
- Repeated calls are safe and will not cause duplicate tickets/emails
- PaymentCompletedEventHandler has internal duplicate checks

### If Duplicate Tickets Generated

**Scenario**: Multiple tickets created for same registration

**Detection**:
```sql
SELECT RegistrationId, COUNT(*) as TicketCount
FROM Tickets
WHERE RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb'
GROUP BY RegistrationId
HAVING COUNT(*) > 1;
```

**Remediation**:
```sql
-- Keep the most recent ticket, mark others as Cancelled
UPDATE Tickets
SET Status = 4 -- Cancelled enum value
WHERE RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb'
  AND Id NOT IN (
    SELECT TOP 1 Id
    FROM Tickets
    WHERE RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb'
    ORDER BY GeneratedAt DESC
  );
```

### If Duplicate Emails Sent

**Scenario**: User receives multiple emails

**Action**:
- No rollback possible (emails already sent)
- Contact user to apologize and clarify
- Only latest ticket is valid
- Duplicate tickets will show as "Cancelled" in system

---

## Risk Assessment

### Low Risk ✅

- Operation is idempotent (can be retried safely)
- No modification to payment or registration state
- Only triggers email/ticket generation
- Fail-silent error handling prevents transaction rollback
- Comprehensive logging for audit trail

### Medium Risk ⚠️

- User could receive duplicate emails if called multiple times
  - **Mitigation**: Ticket service has duplicate check logic
  - **Mitigation**: Email logs will show multiple sends
  - **Mitigation**: User can ignore duplicates, only latest ticket valid

### High Risk ❌

- None identified

---

## Future Prevention

### Root Cause

The original issue was caused by `_publisher` (IPublisher) being NULL in the old Container App revision due to:
1. Dependency injection misconfiguration
2. Deployment of incomplete code
3. Missing integration tests for DI container

### Prevention Measures

1. **Pre-Deployment Validation**:
   - Add health check endpoint that verifies all critical dependencies
   - Fail deployment if health check fails
   - Test DI container resolution in startup

2. **Integration Tests**:
   - Add test for webhook → event → handler flow
   - Mock Stripe webhook and verify email/ticket generation
   - Test failure scenarios (NULL dependencies)

3. **Monitoring**:
   - Alert on PaymentCompletedEvent dispatch failures
   - Track email send success/failure rates
   - Monitor ticket generation latency

4. **Idempotency Design**:
   - Consider adding "processing state" to webhook events
   - Track individual processing steps (payment, email, ticket)
   - Allow selective retry of failed steps

---

## Contact Information

**For Questions or Issues**:
- Development Team: [Your team contact]
- On-Call Engineer: [On-call rotation]
- Database Admin: [DBA contact]

**Escalation Path**:
1. Check application logs in Azure Container App
2. Review database state using verification script
3. Contact development team with Registration ID
4. If payment integrity issue, escalate to finance team

---

## Appendix: SQL Queries

### Check Registration State
```sql
SELECT
    r.Id, r.EventId, r.UserId, r.Status, r.PaymentStatus,
    r.StripePaymentIntentId, r.Contact_Email, r.TotalPrice_Amount
FROM Registrations r
WHERE r.Id = '219dd972-2309-4898-a972-4e0b6a7224fb';
```

### Check Webhook Event
```sql
SELECT EventId, EventType, Processed, ProcessedAt, ErrorMessage
FROM stripe_webhook_events
WHERE EventId = 'evt_1SfSktLvfbr023L1qB78D1CR';
```

### Check Ticket Existence
```sql
SELECT t.Id, t.TicketCode, t.Status, t.GeneratedAt
FROM Tickets t
WHERE t.RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb';
```

### Check Email Logs (if email queue table exists)
```sql
SELECT * FROM EmailQueue
WHERE RegistrationId = '219dd972-2309-4898-a972-4e0b6a7224fb'
ORDER BY CreatedAt DESC;
```

---

**Document Version**: 1.0
**Created**: 2025-12-17
**Last Updated**: 2025-12-17
**Status**: Ready for execution
