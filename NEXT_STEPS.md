# Next Steps: Payment Flow Diagnosis

## Immediate Actions Required

### Action 1: Test Webhook Endpoint Reachability

**What to do:**
```powershell
# Windows (PowerShell)
cd c:\Work\LankaConnect
.\scripts\Test-WebhookEndpoint.ps1
```

**Or using bash:**
```bash
# Linux/Mac/WSL
cd /c/Work/LankaConnect
chmod +x scripts/test-api-simple.sh
./scripts/test-api-simple.sh
```

**Expected outcomes:**
- ✅ **400 Bad Request** → Endpoint is working, proceed to Action 2
- ⚠️ **500 Internal Server Error** → Application error, check logs (Action 4)
- ❌ **404 Not Found** → Deployment issue, redeploy API
- ❌ **Connection refused** → Azure App Service not running

---

### Action 2: Run Database Diagnosis

**What to do:**
```bash
# Connect to Azure PostgreSQL database
psql "postgresql://username@host:5432/database?sslmode=require"

# Run diagnosis queries
\i c:/Work/LankaConnect/scripts/diagnose-payment-flow.sql
```

**Key queries to focus on:**
1. **Step 2**: Check if ANY webhook events exist in last 24 hours
2. **Step 5**: Check registrations stuck in Pending status
3. **Step 6**: Complete payment flow trace (JOIN all tables)

**Analyze results:**

**Scenario A:** No webhook events found
- **Root Cause**: Stripe webhook not configured or signature failing
- **Next Action**: Action 3 (Check Stripe Configuration)

**Scenario B:** Webhook events exist but registrations still Pending
- **Root Cause**: Exception in payment processing logic
- **Next Action**: Action 4 (Check Application Logs)

**Scenario C:** Registrations Completed but no tickets
- **Root Cause**: Exception in PaymentCompletedEventHandler ticket generation
- **Next Action**: Action 4 (Check Application Logs for "Failed to generate ticket")

**Scenario D:** Tickets exist but no emails
- **Root Cause**: Email service configuration issue
- **Next Action**: Action 5 (Verify Email Configuration)

---

### Action 3: Verify Stripe Configuration

**What to check:**

1. **Stripe Dashboard → Developers → Webhooks**
   - URL configured: `https://lankaconnect-api.azurewebsites.net/api/payments/webhook`
   - Events enabled: `checkout.session.completed`
   - Check "Recent deliveries" tab for failed attempts

2. **Compare Webhook Signing Secret**
   ```bash
   # Get current Azure configuration
   az webapp config appsettings list \
     --resource-group <your-resource-group> \
     --name lankaconnect-api \
     --query "[?name=='STRIPE_WEBHOOK_SECRET'].value" \
     --output tsv
   ```

   Compare with Stripe Dashboard → Webhooks → Signing Secret

3. **Verify Mode Consistency**
   - If testing: Use Stripe Test Mode keys and Test Mode webhook secret
   - If production: Use Stripe Live Mode keys and Live Mode webhook secret
   - Don't mix test/live secrets!

**If secret doesn't match:**
```bash
# Update Azure App Service configuration
az webapp config appsettings set \
  --resource-group <your-resource-group> \
  --name lankaconnect-api \
  --settings STRIPE_WEBHOOK_SECRET="whsec_xxxxxxxxx"

# Restart app service to apply changes
az webapp restart \
  --resource-group <your-resource-group> \
  --name lankaconnect-api
```

---

### Action 4: Check Azure Container Logs

**What to do:**
```bash
# Option A: Stream live logs
az webapp log tail \
  --resource-group <your-resource-group> \
  --name lankaconnect-api

# Option B: Download log file
az webapp log download \
  --resource-group <your-resource-group> \
  --name lankaconnect-api \
  --log-file azure_logs.zip

# Unzip and search
unzip azure_logs.zip
grep -i "webhook\|payment.*complet\|ticket\|email" *.log > filtered_logs.txt
```

**What to search for:**

1. **Webhook Received**
   ```bash
   grep "Processing webhook event" logs.txt
   ```
   - ❌ Not found → Webhook not reaching server (Action 3)
   - ✅ Found → Continue to #2

2. **Signature Verification**
   ```bash
   grep "Stripe webhook signature verification failed" logs.txt
   ```
   - ✅ Found → Wrong webhook secret (Action 3)
   - ❌ Not found → Continue to #3

3. **Payment Completion**
   ```bash
   grep "Successfully completed payment for Event" logs.txt
   ```
   - ❌ Not found → Exception in HandleCheckoutSessionCompletedAsync (check for errors)
   - ✅ Found → Continue to #4

4. **Domain Event Dispatched**
   ```bash
   grep "Dispatching domain event: PaymentCompletedEvent" logs.txt
   ```
   - ❌ Not found → IPublisher not injected (check startup logs)
   - ✅ Found → Continue to #5

5. **Handler Invoked**
   ```bash
   grep "PaymentCompletedEventHandler INVOKED" logs.txt
   ```
   - ❌ Not found → Handler not registered in MediatR
   - ✅ Found → Continue to #6

6. **Ticket Generated**
   ```bash
   grep "Ticket generated successfully" logs.txt
   ```
   - ❌ Not found → Check for "Failed to generate ticket"
   - ✅ Found → Continue to #7

7. **Email Sent**
   ```bash
   grep "Payment confirmation email sent successfully" logs.txt
   ```
   - ❌ Not found → Email service issue (Action 5)
   - ✅ Found → Success! Check user's spam folder

**If you find exceptions:**
```bash
grep -E "Error|Exception|Failed" logs.txt | grep -i "payment\|webhook\|ticket\|email"
```

Document the exact error message and stack trace.

---

### Action 5: Verify Email Service Configuration

**Check environment variables:**
```bash
az webapp config appsettings list \
  --resource-group <your-resource-group> \
  --name lankaconnect-api \
  --output table
```

**Required variables:**
- `AZURE_EMAIL_CONNECTION_STRING` - Azure Communication Services connection string
- `AZURE_EMAIL_SENDER_ADDRESS` - Verified sender email address

**Test email service independently:**
1. Check if Azure Communication Services is provisioned
2. Verify sender email is verified in Azure portal
3. Check if domain is verified (for custom domains)
4. Check Azure Communication Services quotas/limits

**Common issues:**
- Connection string expired or invalid
- Sender email not verified
- Domain not configured for sending
- Service quota exceeded
- Service disabled/suspended

---

### Action 6: Manual Recovery (If Needed)

If you have registrations stuck in Pending with confirmed Stripe payments:

**Step 1: Identify stuck registrations**
```sql
SELECT
    r.id AS registration_id,
    r.event_id,
    r.stripe_checkout_session_id,
    r.payment_status,
    r.status,
    r.created_at
FROM events.registrations r
WHERE r.payment_status = 'Pending'
  AND r.stripe_checkout_session_id IS NOT NULL
  AND r.created_at > NOW() - INTERVAL '24 hours'
ORDER BY r.created_at DESC;
```

**Step 2: Verify payment in Stripe Dashboard**
1. Copy `stripe_checkout_session_id` from query result
2. Go to Stripe Dashboard → Payments
3. Search for session ID
4. Verify payment_status = "paid"

**Step 3: Check if webhook event exists**
```sql
SELECT * FROM stripe_webhook_events
WHERE stripe_event_id LIKE '%cs_xxxxx%'  -- Replace with your session ID
ORDER BY created_at DESC;
```

**Step 4A: If webhook event exists but not processed**
```sql
-- Reset processing flag to allow retry
UPDATE stripe_webhook_events
SET is_processed = false, processed_at = NULL
WHERE stripe_event_id = 'evt_xxxxx';  -- Replace with event ID
```

Then manually trigger webhook processing by resending from Stripe Dashboard:
- Stripe Dashboard → Developers → Webhooks
- Find the webhook endpoint
- Click on failed event
- Click "Resend"

**Step 4B: If no webhook event exists**
Stripe never sent the webhook. Options:
1. Manually send webhook from Stripe Dashboard
2. Use Stripe CLI to replay event:
   ```bash
   stripe events resend evt_xxxxx
   ```
3. Last resort: Manual database update (not recommended, skips domain logic)

---

## Decision Tree

```
START: User reports "No email, no QR code"
  │
  ├─▶ Action 1: Test webhook endpoint
  │    │
  │    ├─▶ 400 Bad Request → Endpoint working ────────┐
  │    ├─▶ 500 Server Error → Action 4 (check logs)   │
  │    ├─▶ 404 Not Found → Redeploy API               │
  │    └─▶ Connection refused → Start Azure App       │
  │                                                     │
  ├─▶ Action 2: Run database diagnosis ◀──────────────┘
  │    │
  │    ├─▶ No webhook events → Action 3 (Stripe config)
  │    ├─▶ Webhooks but no completion → Action 4 (logs)
  │    ├─▶ Completed but no tickets → Action 4 (logs)
  │    └─▶ Tickets but no emails → Action 5 (email config)
  │
  ├─▶ Action 3: Verify Stripe configuration
  │    │
  │    ├─▶ Webhook URL wrong → Update in Stripe Dashboard
  │    ├─▶ Events not enabled → Enable checkout.session.completed
  │    └─▶ Secret mismatch → Update Azure config, restart app
  │
  ├─▶ Action 4: Check application logs
  │    │
  │    ├─▶ Signature failed → Action 3
  │    ├─▶ Idempotency blocking → Action 6 (manual recovery)
  │    ├─▶ Payment completion failed → Fix code bug
  │    ├─▶ Domain event not dispatched → Check IPublisher injection
  │    ├─▶ Handler not invoked → Check MediatR registration
  │    ├─▶ Ticket generation failed → Check TicketService logs
  │    └─▶ Email sending failed → Action 5
  │
  ├─▶ Action 5: Verify email configuration
  │    │
  │    ├─▶ Missing env vars → Add to Azure config
  │    ├─▶ Connection string invalid → Update from Azure portal
  │    ├─▶ Sender not verified → Verify in Azure Communication Services
  │    └─▶ Service quota exceeded → Upgrade plan or wait
  │
  └─▶ Action 6: Manual recovery
       │
       ├─▶ Reset webhook processing flag → Resend from Stripe
       ├─▶ Manually replay event → Use Stripe CLI
       └─▶ Last resort → Manual DB update (not recommended)
```

---

## Priority Order

Follow this order for fastest diagnosis:

1. **Action 1** (2 minutes) - Quick test to verify endpoint is reachable
2. **Action 2** (5 minutes) - Database queries reveal exact failure point
3. **Action 3** (10 minutes) - Stripe configuration is most common issue
4. **Action 4** (15 minutes) - Log analysis provides detailed error context
5. **Action 5** (10 minutes) - Email config if logs show email sending failure
6. **Action 6** (30 minutes) - Manual recovery only if payment is confirmed but processing failed

---

## Report Back With

After completing Actions 1-4, provide:

1. **Endpoint Test Result**
   ```
   Status Code: [400/500/404/etc]
   Response Body: [if any]
   ```

2. **Database Query Results**
   - Number of webhook events in last 24 hours
   - Number of registrations stuck in Pending
   - Output from "Step 6: Complete Payment Flow Trace"

3. **Stripe Configuration**
   - Webhook URL configured: [Yes/No]
   - Events enabled: [list]
   - Recent delivery attempts: [Success/Failure]

4. **Log Excerpts**
   - Any "Processing webhook event" lines
   - Any "Stripe webhook signature verification failed" lines
   - Any "Error" or "Exception" lines related to payment/webhook

5. **Identified Failure Point**
   - Which step in the sequence diagram is failing?
   - Error message and stack trace (if any)

---

## Reference Documents

- **Complete Flow Mapping**: `c:\Work\LankaConnect\DIAGNOSIS_REPORT.md`
- **Architecture Diagrams**: `c:\Work\LankaConnect\docs\PAYMENT_FLOW_ARCHITECTURE.md`
- **Test Scripts**: `c:\Work\LankaConnect\scripts\`
  - `Test-WebhookEndpoint.ps1` (Windows)
  - `test-api-simple.sh` (Linux/Mac)
  - `test-webhook-endpoint.sh` (Full Stripe payload)
- **Database Queries**: `c:\Work\LankaConnect\scripts\diagnose-payment-flow.sql`

---

## Contact Points for Help

If stuck at any step:

1. **Stripe Issues**: Check Stripe Dashboard → Events → Webhook Deliveries
2. **Azure Issues**: Azure Portal → App Service → Log Stream
3. **Database Issues**: Query error logs table if exists
4. **Email Issues**: Azure Communication Services → Metrics → Failed Sends

---

## Success Criteria

You'll know it's working when:
1. ✅ Webhook endpoint returns 400 for invalid signatures (Action 1)
2. ✅ Database shows webhook events with is_processed = true (Action 2)
3. ✅ Registrations move from Pending → Completed (Action 2)
4. ✅ Tickets generated with QR codes (Action 2)
5. ✅ Email messages in Sent status (Action 2)
6. ✅ Logs show complete flow without errors (Action 4)
7. ✅ User receives email with PDF ticket (end-to-end test)
