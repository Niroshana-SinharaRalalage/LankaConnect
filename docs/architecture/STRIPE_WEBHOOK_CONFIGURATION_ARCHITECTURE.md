# Stripe Webhook Configuration Architecture
## Phase 6A.24 - Azure Container App Staging Environment

**Date**: 2025-12-15
**Status**: Implementation Plan
**Environment**: Azure Container Apps (Staging)
**Affected Components**: Payment Processing, Email Delivery, Ticket Generation

---

## Executive Summary

This document provides a comprehensive architectural solution for configuring Stripe webhooks in the Azure Container App staging environment. All code for the payment completion workflow exists and is correct. The issue is purely **infrastructure and configuration**.

**Problem Statement**: Stripe webhooks are not reaching the Azure Container App endpoint (`POST /api/payments/webhook`), preventing ticket emails from being sent after successful payment.

**Root Cause**: Missing configuration at three layers:
1. Stripe Dashboard - webhook endpoint not registered
2. Azure Key Vault - webhook secret not stored
3. Azure Container App - environment variable not configured

---

## Current State Analysis

### What EXISTS (Code Complete)

| Component | Status | Location |
|-----------|--------|----------|
| Webhook endpoint | ✅ Complete | `PaymentsController.cs:221-282` |
| Signature verification | ✅ Complete | Line 232-236 using `EventUtility.ConstructEvent()` |
| Event processing | ✅ Complete | Line 250-265 with idempotency checks |
| Payment completion logic | ✅ Complete | Line 287-356 `HandleCheckoutSessionCompletedAsync()` |
| Domain event raised | ✅ Complete | `Registration.CompletePayment()` line 253-259 |
| Event handler | ✅ Complete | `PaymentCompletedEventHandler.cs:1-244` |
| Ticket generation | ✅ Complete | Line 147-199 of event handler |
| Email sending | ✅ Complete | Line 84-134 of event handler |
| Email templates | ✅ Complete | `src/LankaConnect.Infrastructure/Templates/Email/` |

### What's MISSING (Configuration)

| Component | Status | Impact |
|-----------|--------|--------|
| Stripe webhook URL | ❌ Not configured | Webhooks not delivered |
| `STRIPE_WEBHOOK_SECRET` in Key Vault | ❌ Not stored | Cannot verify signatures |
| `Stripe__WebhookSecret` env var | ❌ Not configured | App cannot read secret |
| Ingress path whitelisting | ⚠️ Unknown | May need explicit allow |

### Evidence from Azure Logs

```
✅ Payment succeeds in Stripe Dashboard
✅ Registration status changes to "Completed"
❌ NO "Processing webhook event" logs (PaymentsController:238)
❌ NO "Handling PaymentCompletedEvent" logs (PaymentCompletedEventHandler:47)
❌ NO email sending attempts
❌ NO ticket generation
```

**Conclusion**: Payment succeeds, status updates, but webhook never reaches our endpoint.

---

## Architecture Overview

### Webhook Flow (Complete Stack)

```
┌─────────────────────────────────────────────────────────────────────┐
│                          STRIPE PLATFORM                            │
│                                                                     │
│  1. Payment succeeds in Checkout Session                          │
│  2. Stripe generates checkout.session.completed event             │
│  3. Stripe signs event with webhook secret                        │
│  4. Stripe POST to configured webhook URL ─────────┐              │
└─────────────────────────────────────────────────────┼──────────────┘
                                                      │
                                                      │ HTTPS POST
                                                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   AZURE CONTAINER APP (Ingress)                     │
│                                                                     │
│  5. Azure receives request at FQDN                                 │
│  6. Ingress routes to target port 5000                            │
│  7. Request reaches Kestrel HTTP endpoint ──────┐                 │
└─────────────────────────────────────────────────┼───────────────────┘
                                                  │
                                                  │ HTTP
                                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   ASP.NET CORE MIDDLEWARE PIPELINE                  │
│                                                                     │
│  8. CORS middleware (allows anonymous)                             │
│  9. Routing middleware                                             │
│ 10. Endpoint: POST /api/payments/webhook [AllowAnonymous] ───┐    │
└───────────────────────────────────────────────────────────────┼─────┘
                                                                │
                                                                │
                                                                ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   PAYMENTS CONTROLLER (Line 221)                    │
│                                                                     │
│ 11. Read request body and Stripe-Signature header                 │
│ 12. Verify signature using WebhookSecret ◄──────┐                 │
│ 13. Check idempotency (prevent duplicate)       │                 │
│ 14. Route to HandleCheckoutSessionCompletedAsync│                 │
│ 15. Call registration.CompletePayment()         │                 │
│ 16. Raise PaymentCompletedEvent ────────────────┼────┐            │
└─────────────────────────────────────────────────┼────┼─────────────┘
                                                  │    │
                              ┌───────────────────┘    │
                              │                        │
                              ▼                        │ MediatR
┌──────────────────────────────────────┐               │
│      AZURE KEY VAULT INTEGRATION     │               │
│                                      │               │
│ Secret: stripe-webhook-secret        │               │
│ Retrieved via: secretref pattern    │               │
│ Loaded into: StripeOptions instance  │               │
└──────────────────────────────────────┘               │
                                                       │
                                                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│              PAYMENT COMPLETED EVENT HANDLER (Line 1)               │
│                                                                     │
│ 17. Retrieve registration and event details                        │
│ 18. Call ticketGenerationService.GenerateTicketsAsync()           │
│ 19. Generate QR codes for each ticket                             │
│ 20. Store tickets in database                                     │
│ 21. Call emailService.SendTicketEmailAsync() ──────┐              │
└─────────────────────────────────────────────────────┼──────────────┘
                                                      │
                                                      ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        EMAIL SERVICE                                │
│                                                                     │
│ 22. Load email template (ticket-confirmation.html)                │
│ 23. Render template with ticket data                              │
│ 24. Attach ticket QR codes                                        │
│ 25. Send via SMTP (configured in Azure secrets)                   │
│ 26. ✅ Customer receives ticket email                             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Solution Design

### Layer 1: Stripe Dashboard Configuration

**Objective**: Register webhook endpoint and obtain webhook signing secret

**Steps**:

1. **Navigate to Stripe Dashboard**
   - URL: https://dashboard.stripe.com/webhooks
   - Use staging/test mode keys

2. **Create Webhook Endpoint**
   - Click "Add endpoint"
   - Webhook URL: `https://<CONTAINER_APP_FQDN>/api/payments/webhook`
   - Events to listen: `checkout.session.completed`
   - API version: Latest (currently 2024-11-20.acacia)

3. **Retrieve Webhook Signing Secret**
   - Format: `whsec_...` (starts with whsec_)
   - Copy to clipboard for Key Vault storage
   - Keep in secure password manager

**Azure CLI Command to Get FQDN**:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv
```

**Expected Output**: `lankaconnect-api-staging.proudpond-abc123.eastus.azurecontainerapps.io`

**Full Webhook URL**:
```
https://lankaconnect-api-staging.proudpond-abc123.eastus.azurecontainerapps.io/api/payments/webhook
```

---

### Layer 2: Azure Key Vault Secret Storage

**Objective**: Store webhook secret securely in Azure Key Vault with proper naming

**Secret Naming Convention**:
- Key Vault name: `stripe-webhook-secret` (lowercase with hyphens)
- Environment variable: `Stripe__WebhookSecret` (hierarchical config format)
- secretref value: `stripe-webhook-secret` (matches Key Vault name)

**Azure CLI Commands**:

```bash
# Set variables
RESOURCE_GROUP="lankaconnect-staging"
KEY_VAULT_NAME="lankaconnect-staging-kv"
WEBHOOK_SECRET="<paste whsec_... from Stripe Dashboard>"

# Store webhook secret in Key Vault
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name stripe-webhook-secret \
  --value "$WEBHOOK_SECRET" \
  --description "Stripe webhook signing secret for staging environment" \
  --tags Environment=Staging Component=Payments

# Verify secret was stored
az keyvault secret show \
  --vault-name $KEY_VAULT_NAME \
  --name stripe-webhook-secret \
  --query "value" -o tsv | head -c 20 && echo "..."

# List all Stripe-related secrets for verification
az keyvault secret list \
  --vault-name $KEY_VAULT_NAME \
  --query "[?contains(name, 'stripe')].{Name:name, Tags:tags}" -o table
```

**Expected Secrets** (after configuration):
- `stripe-secret-key` (already exists)
- `stripe-publishable-key` (already exists)
- `stripe-webhook-secret` (NEW - to be added)

---

### Layer 3: Azure Container App Environment Variable Configuration

**Objective**: Configure Container App to read webhook secret from Key Vault

**Current Configuration** (from `.github/workflows/deploy-staging.yml:154-155`):
```bash
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key
```

**Required Addition**:
```bash
Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Implementation Options**:

#### Option A: Update GitHub Actions Workflow (Recommended)

**File**: `.github/workflows/deploy-staging.yml`

**Location**: Lines 154-155 (after existing Stripe configuration)

**Change**:
```yaml
# BEFORE (current)
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key

# AFTER (add webhook secret)
Stripe__SecretKey=secretref:stripe-secret-key \
Stripe__PublishableKey=secretref:stripe-publishable-key \
Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Deployment**: Automatic on next push to `develop` branch

**Pros**:
- ✅ Consistent with existing pattern
- ✅ Version controlled
- ✅ Auditable in Git history
- ✅ Repeatable on every deployment

**Cons**:
- ⚠️ Requires code push to deploy
- ⚠️ Not immediate

#### Option B: Manual Azure CLI Update (Immediate)

**Command**:
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

**Deployment**: Immediate (30-60 seconds)

**Pros**:
- ✅ Immediate effect
- ✅ Good for testing

**Cons**:
- ❌ Not version controlled
- ❌ Will be overwritten on next GitHub Actions deploy
- ❌ Must also update workflow file to persist

#### Option C: Hybrid Approach (RECOMMENDED)

1. **Immediate Fix**: Use Azure CLI for immediate testing
2. **Permanent Fix**: Update GitHub Actions workflow
3. **Verification**: Redeploy via GitHub Actions to confirm

**Commands**:
```bash
# Step 1: Immediate fix for testing
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret

# Step 2: Verify environment variable is set
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']" -o table

# Step 3: Restart container to load new config
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging

# Step 4: Wait for container to be ready
sleep 30

# Step 5: Check logs for startup
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 20
```

---

### Layer 4: Azure Container App Ingress Verification

**Objective**: Ensure ingress allows unauthenticated POST requests to webhook endpoint

**Current Configuration**:
```yaml
# From deploy-staging.yml:158-161
az containerapp ingress update \
  --name ${{ env.CONTAINER_APP_NAME }} \
  --resource-group ${{ env.RESOURCE_GROUP }} \
  --target-port 5000
```

**Verification Commands**:

```bash
# Check ingress configuration
az containerapp ingress show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --output json

# Expected output (key fields):
# {
#   "external": true,
#   "targetPort": 5000,
#   "transport": "http",
#   "allowInsecure": false,
#   "fqdn": "lankaconnect-api-staging.proudpond-abc123.eastus.azurecontainerapps.io"
# }

# Test webhook endpoint from external network
FQDN=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

curl -X POST "https://$FQDN/api/payments/webhook" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: invalid" \
  -d '{"test": "data"}' \
  -w "\nHTTP Status: %{http_code}\n"

# Expected: HTTP 400 (signature verification failed) = endpoint reachable
# NOT expected: 404 (endpoint not found) or timeout
```

**Analysis**:
- Endpoint has `[AllowAnonymous]` attribute (line 222 of PaymentsController.cs)
- CORS middleware allows all origins in staging (line 146-154 of Program.cs)
- No additional ingress configuration required

---

## Implementation Plan

### Phase 1: Preparation (5 minutes)

**Checklist**:
- [ ] Login to Azure CLI: `az login`
- [ ] Set subscription: `az account set --subscription "<subscription-id>"`
- [ ] Verify Key Vault access: `az keyvault secret list --vault-name lankaconnect-staging-kv`
- [ ] Login to Stripe Dashboard (test mode)
- [ ] Have text editor ready for copying secrets

### Phase 2: Stripe Dashboard Configuration (10 minutes)

**Steps**:

1. **Get Container App FQDN**:
```bash
FQDN=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

echo "Webhook URL: https://$FQDN/api/payments/webhook"
```

2. **Navigate to Stripe Dashboard**:
   - URL: https://dashboard.stripe.com/test/webhooks
   - Click "Add endpoint"

3. **Configure Webhook**:
   - **Endpoint URL**: `https://<FQDN>/api/payments/webhook`
   - **Description**: "LankaConnect Staging - Event Ticket Payments"
   - **Events to send**:
     - ✅ `checkout.session.completed`
   - **API Version**: Latest
   - Click "Add endpoint"

4. **Retrieve Signing Secret**:
   - Click on newly created endpoint
   - Click "Reveal" next to "Signing secret"
   - Copy secret (format: `whsec_...`)
   - Store in password manager

**Verification**:
- [ ] Endpoint appears in webhook list
- [ ] Status is "Enabled"
- [ ] Signing secret copied to clipboard

### Phase 3: Azure Key Vault Secret Storage (5 minutes)

**Steps**:

1. **Store webhook secret**:
```bash
# Replace <paste-secret-here> with actual whsec_... value
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --value "<paste-secret-here>" \
  --description "Stripe webhook signing secret for staging" \
  --tags Environment=Staging Component=Payments
```

2. **Verify storage**:
```bash
# Should output first 20 characters of secret (whsec_...)
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query "value" -o tsv | head -c 30 && echo "..."

# List all Stripe secrets
az keyvault secret list \
  --vault-name lankaconnect-staging-kv \
  --query "[?contains(name, 'stripe')].name" -o tsv
```

**Expected Output**:
```
stripe-publishable-key
stripe-secret-key
stripe-webhook-secret  ← NEW
```

**Verification**:
- [ ] Secret stored successfully
- [ ] Secret value starts with `whsec_`
- [ ] Secret appears in list

### Phase 4: Container App Environment Variable Configuration (10 minutes)

**Steps**:

1. **Update environment variable (immediate)**:
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

2. **Verify configuration**:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']" -o json
```

**Expected Output**:
```json
[
  {
    "name": "Stripe__WebhookSecret",
    "secretRef": "stripe-webhook-secret"
  }
]
```

3. **Restart container**:
```bash
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging
```

4. **Wait for startup**:
```bash
echo "Waiting 30 seconds for container restart..."
sleep 30
```

5. **Check startup logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 50 | grep -i "stripe\|webhook\|lankaconnect"
```

**Expected Output**:
- Should see "Starting LankaConnect API"
- Should NOT see errors about missing Stripe configuration
- Should see successful startup messages

**Verification**:
- [ ] Environment variable configured
- [ ] Container restarted successfully
- [ ] No startup errors in logs
- [ ] Health endpoint responding

### Phase 5: Webhook Endpoint Testing (15 minutes)

**Steps**:

1. **Test endpoint reachability**:
```bash
FQDN=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

curl -X POST "https://$FQDN/api/payments/webhook" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: test_invalid_signature" \
  -d '{"type": "test", "data": {}}' \
  -v 2>&1 | grep -E "HTTP/|Signature"
```

**Expected Output**:
```
> POST /api/payments/webhook HTTP/2
< HTTP/2 400
< content-type: application/json
{"Error":"Invalid signature"}
```

**Analysis**:
- ✅ HTTP 400 = Endpoint reached, signature verification working
- ❌ HTTP 404 = Endpoint not found (routing issue)
- ❌ HTTP 500 = Server error (check logs)
- ❌ Timeout = Ingress issue

2. **Stripe Dashboard Test**:
   - Go to https://dashboard.stripe.com/test/webhooks
   - Click on your webhook endpoint
   - Click "Send test webhook"
   - Select event: `checkout.session.completed`
   - Click "Send test webhook"

3. **Check Azure logs for test event**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow \
  --tail 20
```

**Expected Log Output**:
```
Processing webhook event evt_test_... of type checkout.session.completed
Checkout session missing registration_id in metadata
Event evt_test_... already processed, skipping
```

**Verification**:
- [ ] Endpoint returns 400 for invalid signature
- [ ] Stripe test webhook delivered successfully
- [ ] Logs show "Processing webhook event"
- [ ] Signature verification passed

### Phase 6: End-to-End Payment Test (20 minutes)

**Prerequisites**:
- Staging frontend running
- Test Stripe card: `4242 4242 4242 4242`
- Test event with paid sign-up list created

**Steps**:

1. **Create test registration**:
   - Navigate to staging frontend
   - Select event with paid sign-up list
   - Register for event (triggers Stripe Checkout)

2. **Complete test payment**:
   - Card number: `4242 4242 4242 4242`
   - Expiry: Any future date (e.g., 12/25)
   - CVC: Any 3 digits (e.g., 123)
   - ZIP: Any 5 digits (e.g., 12345)
   - Click "Pay"

3. **Monitor webhook delivery**:
```bash
# Open logs in real-time
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow
```

4. **Expected Log Sequence**:
```
[INFO] Processing webhook event evt_... of type checkout.session.completed
[INFO] Processing checkout.session.completed for session cs_test_...
[INFO] Payment status: paid
[INFO] Completing payment for registration <guid>
[INFO] Handling PaymentCompletedEvent for registration <guid>
[INFO] Generating 1 tickets for registration <guid>
[INFO] Generated ticket with QR code <ticket-id>
[INFO] Sending ticket confirmation email to <email>
[INFO] Email sent successfully via SMTP
```

5. **Verify ticket email**:
   - Check inbox for test email
   - Should contain:
     - ✅ Event details
     - ✅ QR code for ticket
     - ✅ Sign-up list assignment
     - ✅ "LankaConnect Staging" sender

**Verification**:
- [ ] Payment succeeds in Stripe
- [ ] Webhook delivered to Azure
- [ ] Signature verified successfully
- [ ] Event processed without errors
- [ ] Tickets generated in database
- [ ] Email sent successfully
- [ ] Customer receives ticket email with QR code

---

## Monitoring and Logging Strategy

### Application Insights Integration

**Current Setup**: Logs written to stdout/stderr (captured by Azure Container Apps)

**Log Queries** (via Azure Portal or CLI):

1. **Webhook event processing**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep "webhook event"
```

2. **Payment completion events**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep "PaymentCompletedEvent"
```

3. **Email delivery**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep "email"
```

### Stripe Dashboard Monitoring

**Webhook Delivery Logs**:
- URL: https://dashboard.stripe.com/test/webhooks/<webhook-id>/logs
- Shows:
  - ✅ Successful deliveries (HTTP 200)
  - ❌ Failed deliveries with retry count
  - Response body from our endpoint
  - Response time

**Key Metrics**:
- Success rate (should be >99%)
- Average response time (<3 seconds)
- Failed deliveries (investigate if >1%)

### Alert Configuration

**Recommended Alerts** (future enhancement):

1. **Webhook Delivery Failures**:
   - Condition: >5 failed deliveries in 5 minutes
   - Action: Email to dev team
   - Priority: High

2. **Ticket Generation Failures**:
   - Condition: Log contains "Error generating tickets"
   - Action: Email + SMS
   - Priority: Critical

3. **Email Delivery Failures**:
   - Condition: Log contains "Failed to send email"
   - Action: Email to dev team
   - Priority: High

---

## Rollback Strategy

### Scenario 1: Webhook Secret Misconfigured

**Symptoms**:
- All webhook deliveries return HTTP 400
- Logs show "Stripe webhook signature verification failed"

**Rollback Steps**:

1. **Verify secret in Key Vault**:
```bash
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query "value" -o tsv | head -c 30
```

2. **Compare with Stripe Dashboard**:
   - Navigate to webhook settings
   - Reveal signing secret
   - Compare first 20 characters

3. **If mismatch, update Key Vault**:
```bash
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --value "<correct-secret-from-stripe>"
```

4. **Restart container**:
```bash
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging
```

**Recovery Time**: ~2 minutes

---

### Scenario 2: Webhook URL Changed

**Symptoms**:
- No webhook deliveries
- Stripe Dashboard shows 404 errors

**Rollback Steps**:

1. **Verify current FQDN**:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv
```

2. **Update Stripe Dashboard**:
   - Navigate to webhook settings
   - Update endpoint URL to correct FQDN
   - Test webhook delivery

**Recovery Time**: ~1 minute

---

### Scenario 3: Environment Variable Missing

**Symptoms**:
- Application startup errors
- Logs show "Object reference not set" in PaymentsController

**Rollback Steps**:

1. **Check environment variables**:
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']" -o table
```

2. **If missing, re-add**:
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

3. **Restart container**:
```bash
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging
```

**Recovery Time**: ~2 minutes

---

### Scenario 4: Catastrophic Failure

**Symptoms**:
- Container won't start
- Multiple configuration errors

**Rollback Steps**:

1. **Remove webhook environment variable**:
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --remove-env-vars Stripe__WebhookSecret
```

2. **Disable webhook in Stripe Dashboard**:
   - Navigate to webhook settings
   - Click "Disable endpoint"

3. **Investigate root cause**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 200 > rollback-logs.txt
```

4. **Restore from previous working deployment**:
```bash
# Trigger GitHub Actions redeployment
git push origin develop --force
```

**Recovery Time**: ~5 minutes

---

## Production Deployment Considerations

### Differences from Staging

| Component | Staging | Production |
|-----------|---------|------------|
| Stripe Mode | Test | Live |
| Webhook Secret | Test mode secret | Live mode secret |
| Key Vault | `lankaconnect-staging-kv` | `lankaconnect-production-kv` |
| Container App | `lankaconnect-api-staging` | `lankaconnect-api-production` |
| FQDN | `*.eastus.azurecontainerapps.io` | Custom domain |

### Production Deployment Checklist

**Before Deployment**:
- [ ] Test staging webhook thoroughly (10+ successful payments)
- [ ] Verify email delivery (100% success rate)
- [ ] Review Stripe Dashboard logs (no errors)
- [ ] Document staging configuration

**Production Steps**:

1. **Switch to Stripe Live Mode**:
   - Navigate to https://dashboard.stripe.com/webhooks
   - Create webhook endpoint for production FQDN
   - Use live mode webhook secret

2. **Store production secret**:
```bash
az keyvault secret set \
  --vault-name lankaconnect-production-kv \
  --name stripe-webhook-secret \
  --value "<live-mode-webhook-secret>" \
  --description "Stripe webhook signing secret for production" \
  --tags Environment=Production Component=Payments
```

3. **Update GitHub Actions workflow**:
   - File: `.github/workflows/deploy-production.yml`
   - Add same environment variable pattern
   - Ensure uses production Key Vault reference

4. **Deploy to production**:
   - Merge to `main` branch
   - Monitor GitHub Actions deployment
   - Verify health endpoint

5. **Test with real payment** (low amount):
   - Create test registration ($1 event)
   - Complete payment with real card
   - Verify webhook delivery
   - Verify ticket email received

**After Deployment**:
- [ ] Monitor webhook delivery logs (24 hours)
- [ ] Check email delivery rate (should be 100%)
- [ ] Set up alerts for failures
- [ ] Document production configuration

### Production Monitoring

**Key Metrics**:
- Webhook delivery success rate (target: >99.9%)
- Average webhook processing time (target: <2 seconds)
- Email delivery success rate (target: 100%)
- Ticket generation success rate (target: 100%)

**Logging**:
- Application Insights for structured logs
- Azure Monitor for container health
- Stripe Dashboard for webhook delivery
- Email service logs for delivery status

---

## Security Considerations

### Webhook Signature Verification

**Current Implementation** (PaymentsController.cs:232-236):
```csharp
var stripeEvent = EventUtility.ConstructEvent(
    json,
    signatureHeader,
    _stripeOptions.WebhookSecret
);
```

**Security Benefits**:
- ✅ Prevents replay attacks
- ✅ Ensures events from Stripe (not attackers)
- ✅ Validates event integrity

**Best Practices** (already implemented):
- ✅ Secret stored in Key Vault (not code)
- ✅ Secret loaded via environment variable
- ✅ [AllowAnonymous] required for Stripe to POST
- ✅ Signature verification before processing

### Key Vault Access

**Current Access Pattern**:
- Container App uses system-assigned managed identity
- Identity granted "Key Vault Secrets User" role
- Secrets loaded at runtime via `secretref` pattern

**Security Benefits**:
- ✅ No secrets in code or config files
- ✅ Secrets rotated without code changes
- ✅ Audit trail for secret access
- ✅ Least privilege access

### HTTPS/TLS

**Current Configuration**:
- Ingress enforces HTTPS (allowInsecure: false)
- Azure Container Apps provides TLS termination
- Stripe webhook requires HTTPS endpoint

**Security Benefits**:
- ✅ Encrypted in transit
- ✅ Prevents man-in-the-middle attacks
- ✅ Stripe validates SSL certificate

---

## Verification Checklist

### Pre-Implementation

- [ ] Azure CLI authenticated and subscription set
- [ ] Access to Stripe Dashboard (test mode)
- [ ] Access to Azure Key Vault
- [ ] Text editor for copying secrets

### Stripe Dashboard

- [ ] Webhook endpoint created
- [ ] Endpoint URL correct (matches Container App FQDN)
- [ ] Event `checkout.session.completed` selected
- [ ] Endpoint status "Enabled"
- [ ] Signing secret copied to clipboard

### Azure Key Vault

- [ ] Secret `stripe-webhook-secret` stored
- [ ] Secret value starts with `whsec_`
- [ ] Secret tagged with Environment=Staging
- [ ] Secret appears in secret list

### Azure Container App

- [ ] Environment variable `Stripe__WebhookSecret` configured
- [ ] secretref points to `stripe-webhook-secret`
- [ ] Container restarted successfully
- [ ] Health endpoint responding (HTTP 200)
- [ ] No startup errors in logs

### Webhook Endpoint

- [ ] Endpoint returns HTTP 400 for invalid signature
- [ ] Stripe test webhook delivered successfully
- [ ] Logs show "Processing webhook event"
- [ ] Signature verification passed

### End-to-End Flow

- [ ] Test payment completed successfully
- [ ] Webhook received within 5 seconds
- [ ] Event processed without errors
- [ ] Tickets generated in database
- [ ] Email sent successfully
- [ ] Customer receives ticket email with QR code

### Post-Implementation

- [ ] GitHub Actions workflow updated (permanent fix)
- [ ] Documentation updated
- [ ] Team notified of configuration
- [ ] Production deployment plan created

---

## Success Metrics

### Technical Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Webhook delivery success rate | >99% | Stripe Dashboard logs |
| Webhook processing time | <3 seconds | Application logs |
| Email delivery success rate | 100% | Email service logs |
| Ticket generation success rate | 100% | Application logs |

### Business Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Time from payment to email | <30 seconds | Customer feedback |
| Customer satisfaction | >95% | Support tickets |
| Payment completion rate | >90% | Stripe analytics |

---

## Troubleshooting Guide

### Issue: Webhook returns HTTP 404

**Cause**: Endpoint URL incorrect or routing misconfigured

**Diagnosis**:
```bash
# Verify FQDN matches Stripe configuration
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv

# Test endpoint directly
curl -X POST "https://<FQDN>/api/payments/webhook" -v
```

**Solution**:
- Update Stripe webhook URL to correct FQDN
- Verify ingress configuration (target port 5000)

---

### Issue: Webhook returns HTTP 400 "Invalid signature"

**Cause**: Webhook secret mismatch or not configured

**Diagnosis**:
```bash
# Check if environment variable is configured
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query "properties.template.containers[0].env[?name=='Stripe__WebhookSecret']" -o json

# Verify secret in Key Vault matches Stripe Dashboard
az keyvault secret show \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --query "value" -o tsv | head -c 30
```

**Solution**:
1. Verify secret in Key Vault matches Stripe Dashboard
2. Ensure environment variable is configured correctly
3. Restart container to reload configuration

---

### Issue: Webhook received but no email sent

**Cause**: PaymentCompletedEvent handler failed or email configuration issue

**Diagnosis**:
```bash
# Check logs for event handler errors
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep -E "PaymentCompletedEvent|email|ticket"
```

**Possible Errors**:
- "Failed to send email": SMTP configuration issue
- "Error generating tickets": Database or QR code generation issue
- "Event not found": Registration data issue

**Solution**:
1. Verify SMTP configuration in Key Vault
2. Check registration data exists in database
3. Review full error stack trace in logs

---

### Issue: Webhook processed but tickets not generated

**Cause**: Ticket generation service failed

**Diagnosis**:
```bash
# Check logs for ticket generation errors
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 | grep "ticket"
```

**Possible Errors**:
- "QR code generation failed": Graphics library issue
- "Database error": Ticket table constraint violation
- "Sign-up list not found": Event configuration issue

**Solution**:
1. Verify event has valid sign-up list configuration
2. Check database constraints on Tickets table
3. Review QR code generation dependencies

---

## Architecture Decision Record

### ADR-001: Webhook Secret Storage

**Status**: Approved
**Date**: 2025-12-15
**Context**: Need to store Stripe webhook signing secret securely

**Decision**: Use Azure Key Vault with secretref pattern in Container App

**Rationale**:
- ✅ Consistent with existing Stripe secret storage pattern
- ✅ Secrets not in code or Git repository
- ✅ Supports secret rotation without redeployment
- ✅ Audit trail for compliance
- ✅ Least privilege access via managed identity

**Alternatives Considered**:
1. Environment variable with hardcoded secret
   - ❌ Secret visible in deployment logs
   - ❌ No audit trail
   - ❌ Requires redeployment to rotate

2. Azure App Configuration
   - ❌ Additional service to manage
   - ❌ More complex setup
   - ✅ Better for feature flags (not needed here)

3. GitHub Secrets
   - ❌ Only available at deployment time
   - ❌ Runtime rotation not possible
   - ✅ Good for CI/CD (not runtime config)

**Consequences**:
- ✅ Improved security posture
- ✅ Simplified secret rotation
- ⚠️ Requires Key Vault access for developers
- ⚠️ Managed identity must have correct permissions

---

### ADR-002: Webhook Endpoint Authentication

**Status**: Approved
**Date**: 2025-12-15
**Context**: Webhook endpoint must accept unauthenticated POST from Stripe

**Decision**: Use `[AllowAnonymous]` attribute with signature verification

**Rationale**:
- ✅ Stripe cannot provide JWT tokens
- ✅ Signature verification provides authentication
- ✅ Standard pattern for webhook endpoints
- ✅ Replay attack protection via signature timestamps

**Alternatives Considered**:
1. Custom authentication header
   - ❌ Stripe doesn't support custom headers
   - ❌ Not standard webhook pattern

2. IP allowlist
   - ❌ Stripe IPs change frequently
   - ❌ Not documented by Stripe
   - ⚠️ Could be used as additional layer

3. Shared secret in URL
   - ❌ Secret visible in logs
   - ❌ Not recommended by Stripe
   - ❌ Difficult to rotate

**Consequences**:
- ✅ Follows Stripe best practices
- ✅ Secure authentication via signatures
- ⚠️ Endpoint appears "open" in Swagger UI
- ⚠️ Must ensure signature verification is robust

---

### ADR-003: Webhook Event Idempotency

**Status**: Approved
**Date**: 2025-12-15
**Context**: Stripe may send duplicate webhook events

**Decision**: Use WebhookEventRepository for idempotency tracking

**Rationale**:
- ✅ Prevents duplicate ticket generation
- ✅ Prevents duplicate email sending
- ✅ Follows Stripe recommendations
- ✅ Database-backed for reliability

**Alternatives Considered**:
1. In-memory cache (Redis)
   - ⚠️ Could lose state on restart
   - ✅ Faster than database
   - ❌ Requires Redis availability

2. No idempotency checks
   - ❌ Risk of duplicate tickets
   - ❌ Poor customer experience
   - ❌ Violates Stripe best practices

3. Application-level deduplication
   - ⚠️ Complex to implement correctly
   - ⚠️ Race conditions possible
   - ❌ Not as reliable as database

**Consequences**:
- ✅ Guaranteed idempotency
- ✅ Audit trail of processed events
- ⚠️ Database dependency for webhook processing
- ⚠️ Additional database table to maintain

---

## Appendix A: Configuration Reference

### Environment Variables (Complete)

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Staging

# Database
ConnectionStrings__DefaultConnection=secretref:database-connection-string
ConnectionStrings__Redis=localhost:6379

# JWT Authentication
Jwt__Key=secretref:jwt-secret-key
Jwt__Issuer=secretref:jwt-issuer
Jwt__Audience=secretref:jwt-audience

# Entra External ID
EntraExternalId__IsEnabled=secretref:entra-enabled
EntraExternalId__TenantId=secretref:entra-tenant-id
EntraExternalId__ClientId=secretref:entra-client-id
EntraExternalId__Audience=secretref:entra-audience

# Email (SMTP)
EmailSettings__Provider=Smtp
EmailSettings__SmtpServer=secretref:smtp-host
EmailSettings__SmtpPort=secretref:smtp-port
EmailSettings__Username=secretref:smtp-username
EmailSettings__Password=secretref:smtp-password
EmailSettings__SenderEmail=secretref:email-from-address
EmailSettings__SenderName="LankaConnect Staging"
EmailSettings__EnableSsl=true
EmailSettings__TemplateBasePath=Templates/Email

# Azure Storage
AzureStorage__ConnectionString=secretref:azure-storage-connection-string

# Stripe (COMPLETE)
Stripe__SecretKey=secretref:stripe-secret-key
Stripe__PublishableKey=secretref:stripe-publishable-key
Stripe__WebhookSecret=secretref:stripe-webhook-secret  ← NEW
```

### Key Vault Secrets (Complete)

```bash
# Database
database-connection-string

# JWT
jwt-secret-key
jwt-issuer
jwt-audience

# Entra External ID
entra-enabled
entra-tenant-id
entra-client-id
entra-audience

# Email
smtp-host
smtp-port
smtp-username
smtp-password
email-from-address

# Azure Storage
azure-storage-connection-string

# Stripe (COMPLETE)
stripe-secret-key          # Existing
stripe-publishable-key     # Existing
stripe-webhook-secret      # NEW - to be added
```

---

## Appendix B: Quick Reference Commands

### Get Container App FQDN
```bash
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv
```

### Store Webhook Secret
```bash
az keyvault secret set \
  --vault-name lankaconnect-staging-kv \
  --name stripe-webhook-secret \
  --value "<webhook-secret>" \
  --description "Stripe webhook signing secret"
```

### Update Container App
```bash
az containerapp update \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --set-env-vars \
    Stripe__WebhookSecret=secretref:stripe-webhook-secret
```

### Restart Container
```bash
az containerapp revision restart \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging
```

### View Logs
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --follow
```

### Test Webhook Endpoint
```bash
FQDN=$(az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --query 'properties.configuration.ingress.fqdn' -o tsv)

curl -X POST "https://$FQDN/api/payments/webhook" \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: test" \
  -d '{"test": "data"}' \
  -w "\nHTTP: %{http_code}\n"
```

---

## Conclusion

This architecture document provides a complete solution for configuring Stripe webhooks in the Azure Container App staging environment. The solution focuses exclusively on **infrastructure and configuration** without modifying any existing code.

**Key Deliverables**:
1. ✅ Stripe Dashboard webhook configuration procedure
2. ✅ Azure Key Vault secret storage strategy
3. ✅ Container App environment variable configuration
4. ✅ Comprehensive testing and verification procedures
5. ✅ Monitoring and logging strategy
6. ✅ Rollback and disaster recovery plan
7. ✅ Production deployment considerations
8. ✅ Architecture Decision Records

**Expected Outcome**: After implementation, the complete payment-to-ticket-email workflow will function end-to-end, with customers receiving ticket emails within 30 seconds of successful payment.

**Next Steps**:
1. Follow implementation plan (Phases 1-6)
2. Verify end-to-end flow with test payment
3. Update GitHub Actions workflow for permanent fix
4. Monitor staging environment for 24-48 hours
5. Plan production deployment

---

**Document Version**: 1.0
**Last Updated**: 2025-12-15
**Author**: System Architecture Designer
**Review Status**: Ready for Implementation
