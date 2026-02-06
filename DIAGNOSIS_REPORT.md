# Payment Flow Diagnosis Report

## Problem Statement
User reports: "No email, no QR code" after completing payment registration for event.

## Complete System Flow Mapping

### Payment Completion Flow (Expected Behavior)

```
1. User completes Stripe checkout
   ↓
2. Stripe sends webhook POST to /api/payments/webhook
   Location: PaymentsController.cs:221-284
   ↓
3. Webhook signature verification
   Location: PaymentsController.cs:232-237
   ↓
4. Idempotency check (prevents duplicate processing)
   Location: PaymentsController.cs:242-246
   ↓
5. Record webhook event
   Location: PaymentsController.cs:249
   ↓
6. Route to HandleCheckoutSessionCompletedAsync()
   Location: PaymentsController.cs:255 → 289-375
   ↓
7. Validate payment_status = "paid"
   Location: PaymentsController.cs:306-310
   ↓
8. Extract metadata (registration_id, event_id)
   Location: PaymentsController.cs:313-326
   ↓
9. Load Event aggregate with registrations
   Location: PaymentsController.cs:333-338
   ↓
10. Find Registration in Event.Registrations
    Location: PaymentsController.cs:341-346
    ↓
11. Call registration.CompletePayment(paymentIntentId)
    Location: Registration.cs:235-264
    ↓
12. Domain state changes:
    - PaymentStatus: Pending → Completed
    - Status: Pending → Confirmed
    - Stores StripePaymentIntentId
    Location: Registration.cs:243-246
    ↓
13. RaiseDomainEvent(new PaymentCompletedEvent(...))
    Location: Registration.cs:253-261
    ↓
14. await _unitOfWork.CommitAsync()
    Location: PaymentsController.cs:362
    ↓
15. AppDbContext.CommitAsync() collects domain events
    Location: AppDbContext.cs:294-366
    ↓
16. SaveChangesAsync() persists to database
    Location: AppDbContext.cs:325
    ↓
17. Dispatch domain events via MediatR IPublisher
    Location: AppDbContext.cs:329-359
    ↓
18. MediatR routes to PaymentCompletedEventHandler
    Location: PaymentCompletedEventHandler.cs:43-242
    ↓
19. Handler generates ticket with QR code
    Location: PaymentCompletedEventHandler.cs:146-176
    ↓
20. Handler renders email templates
    Location: PaymentCompletedEventHandler.cs:179-196
    ↓
21. Handler sends email with PDF attachment
    Location: PaymentCompletedEventHandler.cs:220
    ↓
22. Log success
    Location: PaymentCompletedEventHandler.cs:230-232
```

## Potential Failure Points (Categorized)

### Category 1: Infrastructure (Webhook Not Reaching Server)

**Symptoms:**
- No webhook events in `stripe_webhook_events` table
- No logs showing "Processing webhook event" (PaymentsController.cs:239)

**Root Causes:**
1. Stripe webhook not configured correctly in Stripe Dashboard
2. Webhook URL incorrect or inaccessible
3. Azure App Service firewall blocking Stripe IPs
4. Webhook endpoint not deployed/registered

**Verification Steps:**
```bash
# 1. Check if webhook endpoint is reachable
curl -X POST https://lankaconnect-api.azurewebsites.net/api/payments/webhook \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'

# 2. Check Stripe Dashboard → Developers → Webhooks
# Expected URL: https://lankaconnect-api.azurewebsites.net/api/payments/webhook
# Expected Events: checkout.session.completed

# 3. Check webhook delivery attempts in Stripe Dashboard
```

**Expected Logs If Webhook Is Reaching:**
```
Processing webhook event {EventId} of type {EventType}
```

---

### Category 2: Authentication (Signature Verification Failing)

**Symptoms:**
- Webhook events received but return 400 Bad Request
- Logs showing "Stripe webhook signature verification failed"

**Root Causes:**
1. Incorrect STRIPE_WEBHOOK_SECRET environment variable
2. Webhook secret from test mode used in production (or vice versa)
3. Stripe library signature mismatch

**Verification Steps:**
```bash
# Check environment variable matches Stripe Dashboard secret
# Azure Portal → App Service → Configuration → STRIPE_WEBHOOK_SECRET

# Compare with Stripe Dashboard → Developers → Webhooks → Signing secret
```

**Expected Logs If Signature Fails:**
```
Stripe webhook signature verification failed
```

**Fix:**
```bash
# Update Azure App Service configuration
az webapp config appsettings set \
  --resource-group <resource-group> \
  --name lankaconnect-api \
  --settings STRIPE_WEBHOOK_SECRET="whsec_xxx..."
```

---

### Category 3: Database (Idempotency Blocking)

**Symptoms:**
- Webhook received successfully
- Log: "Event {EventId} already processed, skipping" (PaymentsController.cs:244)
- Registration still in Pending status

**Root Causes:**
1. Stripe retried webhook after initial failure
2. First attempt recorded event but threw exception before completion
3. `is_processed` flag set to true prematurely

**Verification Steps:**
```sql
-- Check webhook events
SELECT * FROM stripe_webhook_events
WHERE created_at > NOW() - INTERVAL '1 hour'
ORDER BY created_at DESC;

-- Check if event processed but registration not updated
SELECT
    swe.stripe_event_id,
    swe.is_processed,
    r.payment_status
FROM stripe_webhook_events swe
JOIN events.registrations r ON r.stripe_checkout_session_id LIKE '%' || swe.stripe_event_id || '%'
WHERE swe.event_type = 'checkout.session.completed'
  AND swe.is_processed = true
  AND r.payment_status = 'Pending';
```

**Fix:**
```sql
-- Reset webhook processing flag to allow retry
UPDATE stripe_webhook_events
SET is_processed = false, processed_at = NULL
WHERE stripe_event_id = 'evt_xxx';
```

---

### Category 4: Application Logic (Silent Exception in Handler)

**Symptoms:**
- Webhook processed successfully (200 OK)
- Registration updated to Completed/Confirmed
- But no ticket or email generated

**Root Causes:**
1. Exception thrown in PaymentCompletedEventHandler after payment completion
2. Fail-silent pattern catching exception (PaymentCompletedEventHandler.cs:238-241)
3. Ticket generation failing
4. Email service throwing exception

**Verification Steps:**
```bash
# Check logs for event handler invocation
grep "PaymentCompletedEventHandler INVOKED" azure_logs.txt

# Check logs for ticket generation
grep "Ticket generated successfully" azure_logs.txt

# Check logs for email sending
grep "Payment confirmation email sent successfully" azure_logs.txt

# Check for silent failures
grep "Error handling PaymentCompletedEvent" azure_logs.txt
```

**Database Checks:**
```sql
-- Check if registrations completed but no tickets
SELECT r.id, r.payment_status, r.status, r.updated_at
FROM events.registrations r
LEFT JOIN events.tickets t ON t.registration_id = r.id
WHERE r.payment_status = 'Completed'
  AND t.id IS NULL
  AND r.updated_at > NOW() - INTERVAL '1 hour';

-- Check if tickets created but emails not sent
SELECT
    t.registration_id,
    t.ticket_code,
    t.created_at AS ticket_created,
    em.id AS email_id,
    em.status AS email_status
FROM events.tickets t
LEFT JOIN communications.email_messages em
    ON em.created_at > t.created_at
    AND em.created_at < t.created_at + INTERVAL '5 minutes'
WHERE t.created_at > NOW() - INTERVAL '1 hour';
```

**Expected Logs (Successful Flow):**
```
[Phase 6A.24] PaymentCompletedEventHandler INVOKED - Event {EventId}, Registration {RegistrationId}
Ticket generated successfully: {TicketCode}
Ticket PDF retrieved successfully, size: {Size} bytes
Payment confirmation email sent successfully to {Email}
```

---

### Category 5: Configuration (Missing Environment Variables)

**Symptoms:**
- Application starts but services fail silently
- Dependency injection errors
- Email service not configured

**Root Causes:**
1. Missing AZURE_EMAIL_CONNECTION_STRING
2. Missing STRIPE_WEBHOOK_SECRET
3. Missing DATABASE_CONNECTION_STRING

**Verification Steps:**
```bash
# Check Azure App Service configuration
az webapp config appsettings list \
  --resource-group <resource-group> \
  --name lankaconnect-api \
  --query "[].{Name:name, Value:value}" \
  --output table

# Required variables:
# - DATABASE_CONNECTION_STRING
# - STRIPE_PUBLISHABLE_KEY
# - STRIPE_SECRET_KEY
# - STRIPE_WEBHOOK_SECRET
# - AZURE_EMAIL_CONNECTION_STRING
# - AZURE_EMAIL_SENDER_ADDRESS
```

**Expected in appsettings.Production.json:**
```json
{
  "Stripe": {
    "PublishableKey": "${STRIPE_PUBLISHABLE_KEY}",
    "SecretKey": "${STRIPE_SECRET_KEY}",
    "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
  },
  "EmailSettings": {
    "Provider": "Azure",
    "AzureConnectionString": "${AZURE_EMAIL_CONNECTION_STRING}",
    "AzureSenderAddress": "${AZURE_EMAIL_SENDER_ADDRESS}"
  }
}
```

---

## Diagnostic Test Scripts

### 1. Test Webhook Endpoint Reachability

**File:** `c:\Work\LankaConnect\scripts\test-webhook-endpoint.sh`

```bash
chmod +x scripts/test-webhook-endpoint.sh
./scripts/test-webhook-endpoint.sh https://lankaconnect-api.azurewebsites.net/api/payments/webhook
```

**Expected Output:**
- 400 Bad Request with "Invalid signature" → Endpoint is reachable
- 500 Internal Server Error → Check application logs
- Connection refused → Infrastructure issue

### 2. Database Diagnosis Queries

**File:** `c:\Work\LankaConnect\scripts\diagnose-payment-flow.sql`

```bash
psql -h <host> -U <user> -d <database> -f scripts/diagnose-payment-flow.sql
```

**What It Checks:**
1. Recent registrations with payment status
2. Webhook events received and processed
3. Tickets generated
4. Email messages queued/sent
5. Registrations stuck in Pending
6. Complete payment flow trace (JOIN all tables)
7. Orphaned webhooks (received but not matched to registration)

### 3. Azure Container Log Analysis

```bash
# Download last 5000 lines of logs
az webapp log download \
  --resource-group <resource-group> \
  --name lankaconnect-api \
  --log-file azure_logs.zip

# Unzip and search for webhook activity
unzip azure_logs.zip
grep -i "webhook\|payment.*complet\|ticket\|email" *.log
```

**Key Log Patterns to Search:**
```bash
# Webhook received
grep "Processing webhook event" logs.txt

# Payment completed
grep "Successfully completed payment" logs.txt

# Domain event dispatched
grep "Dispatching domain event: PaymentCompletedEvent" logs.txt

# Handler invoked
grep "PaymentCompletedEventHandler INVOKED" logs.txt

# Ticket generated
grep "Ticket generated successfully" logs.txt

# Email sent
grep "Payment confirmation email sent" logs.txt

# Errors
grep -E "Error|Exception|Failed" logs.txt | grep -i "payment\|webhook\|ticket\|email"
```

---

## Systematic Diagnosis Procedure

Follow these steps in order:

### Step 1: Verify Infrastructure
```bash
# Test webhook endpoint reachability
curl -X POST https://lankaconnect-api.azurewebsites.net/api/payments/webhook \
  -H "Content-Type: application/json" \
  -d '{"test": "data"}'
```

**Decision:**
- ✅ Returns 400/500 → Endpoint reachable → Go to Step 2
- ❌ Connection refused → **INFRASTRUCTURE ISSUE** → Fix Azure deployment

---

### Step 2: Check Stripe Configuration
1. Go to Stripe Dashboard → Developers → Webhooks
2. Verify webhook URL: `https://lankaconnect-api.azurewebsites.net/api/payments/webhook`
3. Verify event enabled: `checkout.session.completed`
4. Check recent webhook delivery attempts
5. Compare webhook signing secret with Azure App Service config

**Decision:**
- ✅ Webhook configured and showing delivery attempts → Go to Step 3
- ❌ Webhook not configured or failing → **CONFIGURATION ISSUE** → Configure webhook

---

### Step 3: Check Database State
```sql
-- Run diagnosis queries
\i scripts/diagnose-payment-flow.sql
```

**Analyze Results:**

**Scenario A:** No webhook events in table
- **Root Cause:** Webhook not reaching server OR signature verification failing
- **Action:** Check Azure logs for signature verification errors

**Scenario B:** Webhook events exist, is_processed=true, registration still Pending
- **Root Cause:** Exception thrown after idempotency record but before payment completion
- **Action:** Check logs for exceptions between webhook receipt and payment completion
- **Fix:** Reset is_processed flag and retry

**Scenario C:** Registration Completed, but no tickets
- **Root Cause:** Exception in PaymentCompletedEventHandler ticket generation
- **Action:** Check logs for "Failed to generate ticket"

**Scenario D:** Tickets exist, but no emails
- **Root Cause:** Email service configuration issue
- **Action:** Check AZURE_EMAIL_CONNECTION_STRING configuration

---

### Step 4: Analyze Application Logs

```bash
# Download and search logs
az webapp log tail --resource-group <rg> --name lankaconnect-api | tee live_logs.txt

# Search for specific registration
grep "<registration-id>" live_logs.txt

# Trace complete flow
grep -E "webhook|PaymentCompleted|Ticket|Email" live_logs.txt | sort
```

**Expected Log Sequence:**
1. "Processing webhook event evt_xxx of type checkout.session.completed"
2. "Completing payment for Event {EventId}, Registration {RegistrationId}"
3. "Successfully completed payment"
4. "Found 1 domain events to dispatch: PaymentCompletedEvent"
5. "Dispatching domain event: PaymentCompletedEvent"
6. "PaymentCompletedEventHandler INVOKED"
7. "Ticket generated successfully"
8. "Payment confirmation email sent successfully"

**Identify Missing Step:**
- Missing #1 → Webhook not reaching server
- Missing #2-3 → Exception in HandleCheckoutSessionCompletedAsync
- Missing #4-6 → Domain event not dispatched (IPublisher issue)
- Missing #7 → Ticket service issue
- Missing #8 → Email service issue

---

## Quick Reference: File Locations

| Component | File Path | Line Numbers |
|-----------|-----------|--------------|
| Webhook Endpoint | `src/LankaConnect.API/Controllers/PaymentsController.cs` | 221-284 |
| Webhook Handler | `src/LankaConnect.API/Controllers/PaymentsController.cs` | 289-375 |
| Registration.CompletePayment | `src/LankaConnect.Domain/Events/Registration.cs` | 235-264 |
| PaymentCompletedEvent | `src/LankaConnect.Domain/Events/DomainEvents/PaymentCompletedEvent.cs` | N/A |
| AppDbContext Event Dispatch | `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` | 294-366 |
| PaymentCompletedEventHandler | `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs` | 43-242 |
| Ticket Generation | `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs` | 146-176 |
| Email Sending | `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs` | 220 |

---

## Configuration Checklist

### Stripe Dashboard Configuration
- [ ] Webhook URL: `https://lankaconnect-api.azurewebsites.net/api/payments/webhook`
- [ ] Event types enabled: `checkout.session.completed`
- [ ] Webhook mode matches API mode (test/live)
- [ ] Signing secret copied to Azure config

### Azure App Service Configuration
- [ ] `STRIPE_PUBLISHABLE_KEY` set
- [ ] `STRIPE_SECRET_KEY` set
- [ ] `STRIPE_WEBHOOK_SECRET` set (matches Stripe Dashboard)
- [ ] `AZURE_EMAIL_CONNECTION_STRING` set
- [ ] `AZURE_EMAIL_SENDER_ADDRESS` set
- [ ] `DATABASE_CONNECTION_STRING` set

### Application Configuration
- [ ] PaymentsController registered in DI
- [ ] IPublisher injected into AppDbContext
- [ ] PaymentCompletedEventHandler registered in MediatR
- [ ] TicketService registered in DI
- [ ] EmailService registered in DI

---

## Next Steps After Diagnosis

Once you identify the failure point from the steps above:

1. **Infrastructure Issue (Step 1 fails)**
   - Fix: Redeploy API to Azure
   - Verify: Run endpoint test script again

2. **Configuration Issue (Step 2 fails)**
   - Fix: Update Stripe webhook configuration
   - Fix: Update Azure App Service environment variables
   - Verify: Check webhook delivery in Stripe Dashboard

3. **Database Issue (Step 3 finds stuck records)**
   - Fix: Reset is_processed flags
   - Fix: Manually trigger payment completion (TODO: create admin tool)
   - Verify: Re-run diagnosis queries

4. **Application Logic Issue (Step 4 finds exceptions)**
   - Fix: Address specific exception (ticket service, email service, etc.)
   - Fix: Add missing dependencies
   - Verify: Test with new payment

---

## Manual Recovery Procedure

If you need to manually complete a stuck payment:

```sql
-- 1. Find the stuck registration
SELECT * FROM events.registrations
WHERE id = '<registration-id>';

-- 2. Update payment status
UPDATE events.registrations
SET
    payment_status = 'Completed',
    status = 'Confirmed',
    stripe_payment_intent_id = '<payment-intent-id>',
    updated_at = NOW()
WHERE id = '<registration-id>';

-- 3. Manually generate ticket (requires running application code)
-- TODO: Create admin endpoint for manual ticket generation

-- 4. Manually send email (requires running application code)
-- TODO: Create admin endpoint for resending confirmation emails
```

**Note:** Domain events won't be raised for manual SQL updates. You'll need to call application endpoints to trigger ticket/email generation.

---

## Recommended Monitoring Additions

To prevent this in the future:

1. **Add Webhook Delivery Monitoring**
   - Alert if no webhooks received in 1 hour during business hours
   - Dashboard showing webhook delivery success rate

2. **Add Payment Completion Metrics**
   - Track time from registration creation to payment completion
   - Alert if registrations stuck in Pending > 30 minutes

3. **Add Ticket Generation Monitoring**
   - Alert if payment completed but no ticket generated
   - Track ticket generation success rate

4. **Add Email Delivery Monitoring**
   - Alert if ticket generated but no email sent
   - Track email delivery success rate

---

## Conclusion

Run the diagnostic scripts and follow the systematic procedure. Report back with:
1. Output from webhook endpoint test
2. Results from database queries (especially Step 6 - Complete Flow Trace)
3. Relevant log excerpts showing the failure point
4. Which category the issue falls into (1-5)

This will pinpoint the exact failure point and provide an actionable fix.
