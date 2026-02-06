# Quick Reference: Payment Flow Diagnosis

## 🚨 Emergency Checklist (5 Minutes)

### Step 1: Test Endpoint (30 seconds)
```powershell
# Windows
.\scripts\Test-WebhookEndpoint.ps1

# Expected: 400 Bad Request = GOOD
```

### Step 2: Check Database (2 minutes)
```sql
-- Are webhooks being received?
SELECT COUNT(*) FROM stripe_webhook_events
WHERE created_at > NOW() - INTERVAL '1 hour';

-- Are registrations stuck?
SELECT COUNT(*) FROM events.registrations
WHERE payment_status = 'Pending'
AND created_at > NOW() - INTERVAL '1 hour';

-- Complete flow check
SELECT
    r.payment_status,
    r.status,
    COUNT(t.id) as ticket_count,
    COUNT(em.id) as email_count
FROM events.registrations r
LEFT JOIN events.tickets t ON t.registration_id = r.id
LEFT JOIN communications.email_messages em ON em.created_at > r.updated_at
WHERE r.created_at > NOW() - INTERVAL '1 hour'
GROUP BY r.id, r.payment_status, r.status;
```

### Step 3: Check Logs (2 minutes)
```bash
az webapp log tail --resource-group <rg> --name lankaconnect-api | grep -i "payment\|webhook\|ticket\|email"
```

**Look for these log messages:**
- ✅ "Processing webhook event" → Webhook received
- ✅ "Successfully completed payment" → Domain updated
- ✅ "PaymentCompletedEventHandler INVOKED" → Handler called
- ✅ "Ticket generated successfully" → Ticket created
- ✅ "Payment confirmation email sent" → Email sent

**Or these error messages:**
- ❌ "Stripe webhook signature verification failed" → Wrong secret
- ❌ "Failed to complete payment" → Payment logic error
- ❌ "Failed to generate ticket" → Ticket service error
- ❌ "Failed to send payment confirmation email" → Email service error

---

## 🎯 Most Common Issues (90% of cases)

### Issue 1: Stripe Webhook Not Configured
**Symptoms:** No webhook events in database, no logs

**Fix:**
1. Stripe Dashboard → Developers → Webhooks → Add Endpoint
2. URL: `https://lankaconnect-api.azurewebsites.net/api/payments/webhook`
3. Events: Select `checkout.session.completed`
4. Copy signing secret
5. Add to Azure:
   ```bash
   az webapp config appsettings set \
     --settings STRIPE_WEBHOOK_SECRET="whsec_xxx"
   ```

### Issue 2: Wrong Webhook Secret
**Symptoms:** Logs show "signature verification failed"

**Fix:**
```bash
# Get secret from Stripe Dashboard → Webhooks → Signing Secret
# Update Azure config
az webapp config appsettings set \
  --resource-group <rg> \
  --name lankaconnect-api \
  --settings STRIPE_WEBHOOK_SECRET="whsec_xxx"

# Restart app
az webapp restart --resource-group <rg> --name lankaconnect-api
```

### Issue 3: Email Service Not Configured
**Symptoms:** Tickets created, but no emails sent

**Fix:**
```bash
# Check required settings
az webapp config appsettings list \
  --resource-group <rg> \
  --name lankaconnect-api \
  --query "[?name=='AZURE_EMAIL_CONNECTION_STRING' || name=='AZURE_EMAIL_SENDER_ADDRESS']"

# If missing, add them
az webapp config appsettings set \
  --settings \
    AZURE_EMAIL_CONNECTION_STRING="endpoint=xxx" \
    AZURE_EMAIL_SENDER_ADDRESS="noreply@yourdomain.com"
```

---

## 📋 File Locations (Code References)

| Component | File | Line Range |
|-----------|------|------------|
| Webhook Endpoint | `src/LankaConnect.API/Controllers/PaymentsController.cs` | 221-284 |
| Webhook Handler | `src/LankaConnect.API/Controllers/PaymentsController.cs` | 289-375 |
| Payment Completion | `src/LankaConnect.Domain/Events/Registration.cs` | 235-264 |
| Event Dispatch | `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` | 294-366 |
| Payment Event Handler | `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs` | 43-242 |

---

## 🔍 Key Log Search Commands

```bash
# Download logs
az webapp log download --resource-group <rg> --name lankaconnect-api --log-file logs.zip
unzip logs.zip

# Search patterns
grep "Processing webhook event" *.log
grep "signature verification failed" *.log
grep "Successfully completed payment" *.log
grep "PaymentCompletedEventHandler INVOKED" *.log
grep "Ticket generated successfully" *.log
grep "Payment confirmation email sent" *.log
grep -i "error\|exception" *.log | grep -i "payment\|webhook"
```

---

## 🗃️ Database Quick Queries

```sql
-- Recent webhook activity
SELECT stripe_event_id, event_type, is_processed, created_at
FROM stripe_webhook_events
ORDER BY created_at DESC LIMIT 10;

-- Pending registrations
SELECT id, event_id, payment_status, status, created_at
FROM events.registrations
WHERE payment_status = 'Pending'
ORDER BY created_at DESC;

-- Registrations missing tickets
SELECT r.id, r.payment_status, r.status
FROM events.registrations r
LEFT JOIN events.tickets t ON t.registration_id = r.id
WHERE r.payment_status = 'Completed' AND t.id IS NULL;

-- Tickets missing emails
SELECT t.registration_id, t.ticket_code, t.created_at
FROM events.tickets t
WHERE NOT EXISTS (
    SELECT 1 FROM communications.email_messages em
    WHERE em.created_at > t.created_at
    AND em.created_at < t.created_at + INTERVAL '5 minutes'
);
```

---

## 🔧 Manual Recovery Commands

### Reset webhook processing flag
```sql
UPDATE stripe_webhook_events
SET is_processed = false, processed_at = NULL
WHERE stripe_event_id = 'evt_xxx';
```

### Resend webhook from Stripe
1. Stripe Dashboard → Developers → Webhooks
2. Click your webhook endpoint
3. Find the failed event
4. Click "Resend"

### Using Stripe CLI
```bash
stripe events resend evt_xxx
```

---

## 🎨 Flow Visualization

```
Stripe Payment → Webhook POST → Signature Check → Idempotency → Record Event
                                      ↓
                              Payment Completion
                                      ↓
                                Save to Database
                                      ↓
                              Dispatch Domain Event
                                      ↓
                              Event Handler Invoked
                                      ↓
                    ┌─────────────────┴─────────────────┐
                    ▼                                     ▼
            Generate Ticket                        Send Email
            (with QR code)                     (with PDF attachment)
```

---

## 🆘 Troubleshooting Decision Tree

```
User: "No email, no QR code"
  │
  ├─ Test endpoint → 400? → YES → Check database
  │                  └─ NO → Fix deployment
  │
  ├─ Webhooks in DB? → NO → Fix Stripe config
  │                  └─ YES → Check logs
  │
  ├─ Logs show "signature failed"? → YES → Update webhook secret
  │                                 └─ NO → Continue
  │
  ├─ Logs show "Successfully completed payment"? → NO → Code bug
  │                                               └─ YES → Continue
  │
  ├─ Tickets generated? → NO → Check ticket service
  │                      └─ YES → Continue
  │
  └─ Emails sent? → NO → Check email service
                   └─ YES → Check spam folder!
```

---

## 📞 Quick Commands Cheat Sheet

```bash
# Test endpoint
curl -X POST https://lankaconnect-api.azurewebsites.net/api/payments/webhook

# Stream logs
az webapp log tail --resource-group <rg> --name lankaconnect-api

# Check config
az webapp config appsettings list --resource-group <rg> --name lankaconnect-api

# Update config
az webapp config appsettings set --resource-group <rg> --name lankaconnect-api \
  --settings KEY="value"

# Restart app
az webapp restart --resource-group <rg> --name lankaconnect-api

# Connect to database
psql "postgresql://user@host:5432/db?sslmode=require"
```

---

## 📚 Full Documentation

- **Complete Diagnosis Report**: `c:\Work\LankaConnect\DIAGNOSIS_REPORT.md`
- **Architecture Diagrams**: `c:\Work\LankaConnect\docs\PAYMENT_FLOW_ARCHITECTURE.md`
- **Detailed Next Steps**: `c:\Work\LankaConnect\NEXT_STEPS.md`
- **Test Scripts**: `c:\Work\LankaConnect\scripts\`
- **SQL Queries**: `c:\Work\LankaConnect\scripts\diagnose-payment-flow.sql`

---

## ✅ Success Criteria

System is working when:
- [ ] Endpoint returns 400 for invalid signatures
- [ ] Webhooks recorded in database with is_processed = true
- [ ] Registrations change from Pending → Completed
- [ ] Tickets generated with unique codes
- [ ] Emails sent with PDF attachments
- [ ] User receives email in inbox (check spam!)

---

## 🔢 Environment Variables Required

```bash
# Stripe
STRIPE_PUBLISHABLE_KEY=pk_xxx
STRIPE_SECRET_KEY=sk_xxx
STRIPE_WEBHOOK_SECRET=whsec_xxx  # ← Most important for webhooks!

# Email
AZURE_EMAIL_CONNECTION_STRING=endpoint=xxx
AZURE_EMAIL_SENDER_ADDRESS=noreply@yourdomain.com

# Database
DATABASE_CONNECTION_STRING=Host=xxx;Database=xxx;Username=xxx;Password=xxx
```

---

## ⏱️ Typical Response Times

If system is healthy:
- Webhook received: < 1 second after Stripe payment
- Payment completed: < 500ms
- Domain event dispatched: < 100ms
- Ticket generated: < 2 seconds (includes QR code generation)
- Email sent: < 5 seconds (includes PDF generation)

**Total time from payment to email received: < 10 seconds**

If it's taking longer, there's likely an issue with:
- Network latency to Azure
- Database query performance
- PDF generation service
- Email service delivery
