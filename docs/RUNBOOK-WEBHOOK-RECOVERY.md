# Webhook Recovery Runbook - Phase 6A.24

**Purpose**: Operational procedures for recovering from Stripe webhook processing failures
**Created**: 2025-12-18
**Status**: Active
**Owner**: Development Team

---

## Table of Contents
1. [Overview](#overview)
2. [Diagnostic Phase](#diagnostic-phase)
3. [Recovery Phase](#recovery-phase)
4. [Verification Phase](#verification-phase)
5. [Troubleshooting](#troubleshooting)
6. [Appendix](#appendix)

---

## Overview

### Problem Statement
Stripe webhooks may be delivered successfully (HTTP 200) but fail to complete ticket generation or email sending due to:
- Infrastructure issues (IPublisher NULL, logging misconfiguration)
- Service failures (TicketService, EmailService exceptions)
- Database transaction issues

### Recovery Approach
1. **Diagnose**: Run SQL queries to determine exact failure point
2. **Recover**: Use AdminRecoveryController to manually trigger PaymentCompletedEvent
3. **Verify**: Confirm ticket generated and email sent

### Safety Guarantees
- ✅ Idempotent: Safe to call multiple times
- ✅ Read-only: Doesn't modify payment or registration status
- ✅ Auditable: All operations logged
- ✅ Secure: Requires authentication (admin role in production)

---

## Diagnostic Phase

### Step 1: Run Diagnostic SQL Script

**File**: `scripts/diagnose-webhook-evt_1SfZoMLvfbr023L1xcB9paik.sql`

**How to Run**:
```bash
# Option A: Azure Data Studio
1. Open Azure Data Studio
2. Connect to staging database
3. Open scripts/diagnose-webhook-evt_1SfZoMLvfbr023L1xcB9paik.sql
4. Execute all queries

# Option B: psql command line
psql "$DATABASE_CONNECTION_STRING" -f scripts/diagnose-webhook-evt_1SfZoMLvfbr023L1xcB9paik.sql
```

### Step 2: Interpret Diagnostic Results

**Check the DIAGNOSTIC SUMMARY (last query)**:

#### Scenario A: ✅ COMPLETE - All steps succeeded
```
✅ Webhook | ✅ Registration | ✅ Ticket | ✅ Email
Overall Status: ✅ COMPLETE - All steps succeeded
Recommended Action: NO ACTION NEEDED
```
**Interpretation**: Payment flow worked correctly. No recovery needed.

#### Scenario B: ⚠️ PARTIAL - Ticket generation failed
```
✅ Webhook | ✅ Registration | ❌ Ticket | ❌ Email
Overall Status: ⚠️ PARTIAL - Handler failed at ticket generation
Recommended Action: RECOVERY NEEDED - Use AdminRecoveryController
```
**Interpretation**: PaymentCompletedEventHandler invoked but TicketService failed. Proceed to Recovery Phase.

#### Scenario C: ⚠️ PARTIAL - Email sending failed
```
✅ Webhook | ✅ Registration | ✅ Ticket | ❌ Email
Overall Status: ⚠️ PARTIAL - Handler failed at email sending
Recommended Action: RECOVERY NEEDED - Use AdminRecoveryController
```
**Interpretation**: Ticket generated but email failed. Proceed to Recovery Phase.

#### Scenario D: ❌ FAILED - Payment not completed
```
✅ Webhook | ❌ Registration | ❌ Ticket | ❌ Email
Overall Status: ❌ FAILED - Payment not completed
Recommended Action: INVESTIGATE - Check webhook handler
```
**Interpretation**: Webhook processed but payment status not updated. This is a **serious bug**. Consult system-architect immediately.

### Step 3: Collect Registration Information

From the diagnostic results, note:
- **Registration ID**: `registration_id` from REGISTRATION STATUS query
- **Event ID**: `event_id` from REGISTRATION STATUS query
- **Contact Email**: `contact_email` from CONTACT INFORMATION query
- **User ID**: `user_id` from REGISTRATION STATUS query

**Example**:
```
Registration ID: 12345678-1234-1234-1234-123456789012
Event ID: 87654321-4321-4321-4321-210987654321
Contact Email: niroshanaks@gmail.com
User ID: 5e782b4d-29ed-4e1d-9039-6c8f698aeea9
```

---

## Recovery Phase

### Prerequisites
- ✅ Diagnostic SQL script executed
- ✅ Scenario identified as "RECOVERY NEEDED"
- ✅ Registration ID collected
- ✅ Contact email verified

### Step 1: Authenticate to Staging API

**Get Access Token**:
```bash
# Login to staging
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "YOUR_EMAIL",
    "password": "YOUR_PASSWORD"
  }' \
  | jq -r '.accessToken'

# Save token
export ACCESS_TOKEN="your-token-here"
```

**Verify Token Works**:
```bash
curl -H "Authorization: Bearer $ACCESS_TOKEN" \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"registrationId": "test"}'

# Expected: 404 Not Found (registration doesn't exist, but auth works)
```

### Step 2: Trigger Recovery Endpoint

**Call AdminRecoveryController**:
```bash
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "registrationId": "PASTE_REGISTRATION_ID_HERE"
  }'
```

**Example with Real Data**:
```bash
curl -X POST https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/admin/recovery/trigger-payment-event \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "registrationId": "12345678-1234-1234-1234-123456789012"
  }'
```

### Step 3: Monitor Logs in Real-Time

**Open separate terminal and watch logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 \
  --follow \
  | grep -E "ADMIN RECOVERY|PaymentCompletedEvent|Ticket|Email"
```

### Step 4: Interpret Recovery Response

#### Success Response (HTTP 200):
```json
{
  "message": "PaymentCompletedEvent published successfully",
  "registrationId": "12345678-1234-1234-1234-123456789012",
  "eventId": "87654321-4321-4321-4321-210987654321",
  "contactEmail": "niroshanaks@gmail.com",
  "amountPaid": 50.00,
  "attendeeCount": 2,
  "note": "Email and ticket generation should complete within 30 seconds. Check application logs for PaymentCompletedEventHandler."
}
```
**Action**: Wait 30 seconds, then proceed to Verification Phase.

#### Validation Error (HTTP 400):
```json
{
  "error": "Payment not completed",
  "details": "Registration has payment status: Pending. Only Completed payments can trigger this event."
}
```
**Action**: Payment wasn't completed. Investigate webhook handler. Don't proceed with recovery.

#### Not Found Error (HTTP 404):
```json
{
  "error": "Registration not found"
}
```
**Action**: Double-check registration ID. Verify it exists in database.

#### Internal Error (HTTP 500):
```json
{
  "error": "Internal server error",
  "details": "Exception message here"
}
```
**Action**: Check application logs for exception details. May require system-architect consultation.

---

## Verification Phase

### Step 1: Check Application Logs

**Look for these log messages** (within 30 seconds of recovery):
```
[ADMIN RECOVERY] Publishing PaymentCompletedEvent - Event: {...}, Registration: {...}
[INF] PaymentCompletedEventHandler: Handling payment completed event for Registration: {...}
[INF] PaymentCompletedEventHandler: Generating ticket for event {...}
[INF] TicketService: Ticket generated successfully
[INF] PaymentCompletedEventHandler: Sending confirmation email to {...}
[INF] EmailService: Email queued for sending
```

**If logs don't appear**:
- Wait 60 seconds (background job may be delayed)
- Check for ERROR level logs showing exceptions
- If still no logs, consult Troubleshooting section

### Step 2: Re-run Diagnostic SQL

**Run diagnostic script again**:
```bash
psql "$DATABASE_CONNECTION_STRING" -f scripts/diagnose-webhook-evt_1SfZoMLvfbr023L1xcB9paik.sql
```

**Check DIAGNOSTIC SUMMARY**:
```
✅ Webhook | ✅ Registration | ✅ Ticket | ✅ Email
Overall Status: ✅ COMPLETE - All steps succeeded
```

**If still showing failures**: Proceed to Troubleshooting.

### Step 3: Verify User Received Email

**Check with user**:
1. Ask user to check inbox for email from "LankaConnect Staging"
2. Subject should be: "Your Ticket for [Event Name]"
3. Email should contain PDF attachment with QR code
4. PDF should show ticket code and event details

**If email not received**:
- Check spam folder
- Query email_messages table for status
- Check Azure Email Service configuration
- See Troubleshooting section

### Step 4: Verify Ticket in Database

**SQL Query**:
```sql
SELECT
    t.ticket_code,
    t.status,
    t.qr_code_data,
    t.created_at
FROM tickets.tickets t
WHERE t.registration_id = 'PASTE_REGISTRATION_ID_HERE';
```

**Expected Result**:
- 1 row returned
- `status` = 'Active'
- `qr_code_data` contains base64 PNG image
- `created_at` matches recovery timestamp

---

## Troubleshooting

### Issue 1: "Registration not found" (404)

**Cause**: Invalid registration ID or wrong database

**Solution**:
1. Verify registration ID from diagnostic SQL
2. Confirm you're querying staging database
3. Check if registration was deleted (unlikely)

### Issue 2: "Payment not completed" (400)

**Cause**: Payment status is not Completed

**Diagnosis**:
```sql
SELECT
    r.id,
    r.status,
    r.payment_status,
    r.stripe_payment_intent_id
FROM events.registrations r
WHERE r.id = 'PASTE_REGISTRATION_ID_HERE';
```

**Solution**:
- If `payment_status` = 'Pending': Payment didn't complete. Webhook handler failed.
- If `payment_status` = 'Failed': Payment failed. Cannot recover.
- If `payment_status` = 'Refunded': Payment was refunded. Cannot recover.

**Action**: Consult system-architect if payment status is unexpected.

### Issue 3: Recovery endpoint returns 200 but logs show errors

**Cause**: PaymentCompletedEventHandler invoked but TicketService or EmailService failed

**Diagnosis**:
Check logs for exceptions:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 \
  | grep -E "ERROR|Exception" -A 10
```

**Common Issues**:

#### A. TicketService Exception: "QR code generation failed"
**Solution**: Check QRCoder library is installed. Verify ticket template exists.

#### B. EmailService Exception: "Azure Email Service not configured"
**Solution**: Verify environment variables:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.template.containers[0].env[?starts_with(name, `AZURE_EMAIL`)]'
```

Expected:
- `AZURE_EMAIL_CONNECTION_STRING` = set
- `AZURE_EMAIL_SENDER_ADDRESS` = set

#### C. Database Exception: "Foreign key violation"
**Solution**: Check registration and event still exist. Verify database migration state.

### Issue 4: Email queued but never sent

**Diagnosis**:
```sql
SELECT
    em.id,
    em.status,
    em.created_at,
    em.sent_at,
    em.failed_at,
    em.error_message
FROM communications.email_messages em
WHERE em.to_emails::jsonb @> to_jsonb(ARRAY['niroshanaks@gmail.com'])
  AND em.created_at > NOW() - INTERVAL '1 hour'
ORDER BY em.created_at DESC;
```

**Status Meanings**:
- `Queued`: Waiting for background worker (should send within 30 seconds)
- `Sending`: Currently being sent
- `Sent`: Successfully sent
- `Failed`: Permanent failure (check `error_message`)

**Solution**:
- If `Queued` for > 2 minutes: Background worker may be down. Check application health.
- If `Failed`: Check `error_message` for details. May be Azure Email Service issue.

### Issue 5: Duplicate tickets created

**Cause**: Recovery endpoint called multiple times

**Impact**: User receives multiple emails with different ticket codes

**Solution**:
1. Mark old tickets as Cancelled:
```sql
UPDATE tickets.tickets
SET status = 'Cancelled',
    updated_at = NOW()
WHERE registration_id = 'PASTE_REGISTRATION_ID_HERE'
  AND id != (
      SELECT id FROM tickets.tickets
      WHERE registration_id = 'PASTE_REGISTRATION_ID_HERE'
      ORDER BY created_at DESC
      LIMIT 1
  );
```

2. Email user with correct ticket:
   - Subject: "Updated Ticket for [Event Name]"
   - Body: "We've issued you a new ticket. Please use the ticket in this email and discard any previous tickets."

---

## Appendix

### A. Recovery Endpoint API Specification

**Endpoint**: `POST /api/admin/recovery/trigger-payment-event`

**Authentication**: Bearer token (admin role in production)

**Request Body**:
```json
{
  "registrationId": "guid-format-uuid"
}
```

**Response Codes**:
- `200 OK`: Event published successfully
- `400 Bad Request`: Validation failed (payment not completed, registration not confirmed)
- `404 Not Found`: Registration or event not found
- `500 Internal Server Error`: Unexpected exception

**Response Body (200 OK)**:
```json
{
  "message": "PaymentCompletedEvent published successfully",
  "registrationId": "guid",
  "eventId": "guid",
  "contactEmail": "string",
  "amountPaid": 0.0,
  "attendeeCount": 0,
  "note": "Email and ticket generation should complete within 30 seconds..."
}
```

### B. PaymentCompletedEvent Flow Diagram

```
AdminRecoveryController.TriggerPaymentCompletedEvent()
  │
  ├─> [Validation] Registration exists? Payment completed?
  │
  ├─> [Construction] Build PaymentCompletedEvent with registration data
  │
  ├─> [Publish] IPublisher.Publish(DomainEventNotification<PaymentCompletedEvent>)
  │
  └─> [Handler] PaymentCompletedEventHandler.Handle()
        │
        ├─> TicketService.GenerateTicketAsync()
        │     ├─> Generate 8-character ticket code
        │     ├─> Generate QR code (PNG, base64)
        │     ├─> Create PDF with QuestPDF
        │     └─> Save ticket to database
        │
        └─> EmailService.SendEmailAsync()
              ├─> Load ticket-confirmation template
              ├─> Populate template data
              ├─> Attach ticket PDF
              ├─> Queue email in database
              └─> Background worker sends email via Azure Email Service
```

### C. Webhook Event Lifecycle

```
Stripe → Webhook Delivered
  │
  ├─> PaymentsController.Webhook()
  │     │
  │     ├─> Verify signature ✅
  │     │
  │     ├─> Check idempotency
  │     │     ├─> Already processed? → Return 200, skip
  │     │     └─> Not processed? → Continue
  │     │
  │     ├─> Record webhook event (Processed = false)
  │     │
  │     ├─> HandleCheckoutSessionCompletedAsync()
  │     │     ├─> Extract registration_id from metadata
  │     │     ├─> Call registration.CompletePayment()
  │     │     │     ├─> Set PaymentStatus = Completed
  │     │     │     ├─> Set Status = Confirmed
  │     │     │     └─> RaiseDomainEvent(PaymentCompletedEvent)
  │     │     └─> await _unitOfWork.CommitAsync()
  │     │           ├─> SaveChangesAsync()
  │     │           ├─> Dispatch domain events via IPublisher
  │     │           │     └─> PaymentCompletedEventHandler.Handle()
  │     │           │           ├─> Generate ticket
  │     │           │           └─> Send email
  │     │           └─> ClearDomainEvents()
  │     │
  │     ├─> Mark webhook as processed (Processed = true)
  │     │
  │     └─> Return HTTP 200
  │
  └─> Stripe marks webhook as delivered ✅
```

### D. Contact Information

**For Urgent Issues**:
- Email: dev-team@lankaconnect.com
- Slack: #phase-6a-24-webhooks

**Escalation Path**:
1. Check this runbook
2. Check application logs
3. Run diagnostic SQL
4. Consult system-architect via Task tool
5. Create GitHub issue with diagnostic results

### E. Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-18 | 1.0 | Initial runbook created | Claude Code |

---

## Quick Reference Card

**🚨 EMERGENCY RECOVERY (2 Minutes)**

1. **Get Registration ID**:
```sql
SELECT id FROM events.registrations
WHERE stripe_checkout_session_id = 'cs_test_xxx'
ORDER BY created_at DESC LIMIT 1;
```

2. **Login to API**:
```bash
export TOKEN=$(curl -s -X POST https://lankaconnect-api-staging.../api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"your-email","password":"your-password"}' \
  | jq -r '.accessToken')
```

3. **Trigger Recovery**:
```bash
curl -X POST https://lankaconnect-api-staging.../api/admin/recovery/trigger-payment-event \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"registrationId":"PASTE-HERE"}'
```

4. **Verify in Logs** (30 seconds):
```bash
az containerapp logs show --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging --tail 50 --follow \
  | grep -E "PaymentCompleted|Ticket|Email"
```

Done! ✅
