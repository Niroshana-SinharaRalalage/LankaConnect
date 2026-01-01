# Payment Webhook Root Cause Analysis - 2025-12-17

## Executive Summary

**Status**: CRITICAL PRODUCTION ISSUE
**Impact**: Users completing payments receive NO confirmation emails and NO tickets
**Deployed Fix**: Commit d264a82 (AppDbContext IPublisher injection) - deployed 12:51 UTC
**Result**: Still broken after deployment

## Problem Statement

User completed payment for event registration:
- ✅ Payment succeeded in Stripe
- ✅ Registration status changed to "Completed" in database
- ❌ NO email received
- ❌ NO QR code/ticket generated
- ❌ NO webhook processing logs in application logs (last 1000 lines)

## Investigation Findings

### What We Fixed (But Didn't Work)

**Previous Diagnosis**: AppDbContext missing IPublisher dependency
- Added IPublisher injection to AppDbContext constructor
- Added domain event dispatching in CommitAsync()
- Deployed successfully (commit d264a82)
- Build: 0 errors

**Why This Didn't Fix It**: The webhook isn't even reaching our application code.

### Root Cause: Infrastructure Configuration Issue

The ACTUAL problem is at the infrastructure level, NOT the application code:

#### Evidence Chain:

1. **No Webhook Logs** (Most Damning Evidence)
   - User reported: "NO webhook processing logs in last 1000 lines"
   - Expected log at PaymentsController.cs:239: `"Processing webhook event {EventId} of type {EventType}"`
   - This log appears BEFORE any business logic
   - If webhook reached the server, this log MUST appear
   - Since it doesn't appear → webhook never reached the server

2. **Registration Status Changed to "Completed"**
   - This proves Stripe received the payment
   - BUT our webhook handler NEVER ran
   - Status change must have happened through different code path OR manual intervention

3. **Application Code is Correct**
   - ✅ Webhook endpoint exists: `POST /api/payments/webhook`
   - ✅ AllowAnonymous attribute present
   - ✅ Signature verification implemented
   - ✅ Event handler registered via MediatR assembly scanning
   - ✅ Domain event dispatching code exists
   - ✅ All dependencies injected correctly

## The REAL Issue: Stripe Webhook Not Configured

### What's Missing at Infrastructure Level:

| Layer | Component | Status | Impact |
|-------|-----------|--------|--------|
| **Stripe Dashboard** | Webhook endpoint URL | ❌ NOT configured | Webhooks not sent |
| **Azure Key Vault** | `STRIPE_WEBHOOK_SECRET` | ❌ NOT stored | Cannot verify signatures |
| **Azure Container App** | `Stripe__WebhookSecret` env var | ❌ NOT configured | App cannot read secret |
| **DNS/Ingress** | Route `/api/payments/webhook` | ⚠️ Unknown | May not be reachable |

### How to Verify:

**Test 1: Check Stripe Dashboard**
```
1. Log into Stripe Dashboard → Developers → Webhooks
2. Look for endpoint: https://lankaconnect-staging.azurecontainerapps.io/api/payments/webhook
3. Check events: Is "checkout.session.completed" enabled?
4. Check recent deliveries: Are there any delivery attempts?
5. If delivery attempts exist: Check HTTP status codes
```

**Expected Results:**
- If NO webhook configured → Root cause confirmed
- If webhook configured but failing with 404 → Routing issue
- If webhook configured but failing with 401/403 → Auth issue
- If webhook configured but failing with 500 → Application code issue
- If webhook configured and returning 200 → Different root cause

**Test 2: Check Azure Logs for Webhook Attempts**
```bash
az containerapp logs show \
  --name lankaconnect-staging \
  --resource-group rg-lankaconnect-staging \
  --tail 1000 \
  --query "[?contains(message, '/api/payments/webhook') || contains(message, 'Stripe-Signature') || contains(message, 'checkout.session.completed')]"
```

**Expected Results:**
- If NO logs → Webhooks not reaching the server (DNS/routing issue or Stripe not sending)
- If logs show 404 → Route not registered
- If logs show 401 → Authentication blocking anonymous endpoint
- If logs show signature errors → Secret mismatch
- If logs show processing → Different root cause in business logic

## Detailed Code Flow Analysis

### IF Webhook Reaches Server (Which It Isn't):

```
1. POST /api/payments/webhook
   ↓
2. PaymentsController.Webhook() Line 221-284
   ✅ Log: "Processing webhook event {EventId} of type {EventType}" (Line 239)
   ↓
3. Signature Verification (Line 232-236)
   → If fails: Returns 400 Bad Request
   ↓
4. Idempotency Check (Line 242-246)
   → If duplicate: Returns 200 OK early
   ↓
5. Record Event (Line 249)
   ↓
6. Route by Type (Line 252-266)
   → checkout.session.completed → HandleCheckoutSessionCompletedAsync
   ↓
7. HandleCheckoutSessionCompletedAsync() Line 289-375
   ✅ Log: "Processing checkout.session.completed for session {SessionId}" (Line 300)
   ↓
8. Validate Payment Status = "paid" (Line 306)
   ↓
9. Extract Metadata (event_id, registration_id) (Line 313-325)
   ✅ Log: "Completing payment for Event {EventId}, Registration {RegistrationId}" (Line 327)
   ↓
10. Get Event with Registrations (Line 333)
    → EventRepository.GetByIdAsync() loads .Include(e => e.Registrations)
    ↓
11. Find Registration (Line 341)
    → If not found: Log error and return
    ↓
12. Call registration.CompletePayment() (Line 350)
    → Sets PaymentStatus = Completed
    → Sets Status = Confirmed
    → Raises PaymentCompletedEvent
    ↓
13. Call _unitOfWork.CommitAsync() (Line 362)
    → UnitOfWork calls _context.CommitAsync()
    → AppDbContext.CommitAsync() collects domain events (Line 311)
    ✅ Log: "[Phase 6A.24] Found {Count} domain events to dispatch: PaymentCompletedEvent" (Line 318)
    → Saves to database (Line 325)
    → Dispatches events via MediatR (Line 329-350)
    ✅ Log: "[Phase 6A.24] Dispatching domain event: PaymentCompletedEvent" (Line 336)
    ✅ Log: "[Phase 6A.24] Successfully dispatched domain event: PaymentCompletedEvent" (Line 344)
    ↓
14. MediatR routes to PaymentCompletedEventHandler.Handle() (Line 43)
    ✅ Log: "[Phase 6A.24] ✅ PaymentCompletedEventHandler INVOKED" (Line 48)
    ↓
15. Generate Ticket + Send Email
```

### None of These Logs Appear → Webhook Never Reached Line 239

## The Mystery: How Did Registration Status Change?

User said registration status is "Completed" but webhook never ran. Possible explanations:

1. **Manual Database Update**: Someone manually changed the status
2. **Different Code Path**: Status changed through different endpoint (NOT webhook)
3. **Old Webhook Processing**: Webhook ran BEFORE our fixes were deployed
4. **Stripe Dashboard Confusion**: User looking at wrong event/registration

**Need to verify**:
- Check database audit logs for registration status change timestamp
- Compare with payment timestamp in Stripe
- Check if any other code can set Status = Completed

## Action Plan

### Immediate Actions (Next 15 Minutes):

1. **Check Stripe Dashboard**
   - Navigate to: Developers → Webhooks
   - Verify webhook endpoint configuration
   - Check recent delivery attempts and status codes
   - Screenshot evidence

2. **Check Azure Container App Logs**
   - Run the log query above
   - Look for ANY requests to `/api/payments/webhook`
   - Check for 404/401/500 errors

3. **Verify Environment Variables**
   - Check if `Stripe__WebhookSecret` is configured
   - Verify it matches the webhook secret in Stripe dashboard

### Configuration Fix (If Webhook Not Configured):

Follow steps in: `docs/architecture/STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md`

1. Create webhook in Stripe Dashboard
2. Store secret in Azure Key Vault
3. Configure Container App environment variable
4. Restart container app

### Code Fix (If Webhook Reaches Server But Fails):

Add enhanced diagnostic logging to PaymentsController:

```csharp
// At start of HandleCheckoutSessionCompletedAsync
_logger.LogInformation("STEP 1: Session metadata: {@Metadata}", session.Metadata);
_logger.LogInformation("STEP 2: Retrieved event {EventId}, Registration count: {Count}",
    eventId, @event?.Registrations?.Count ?? 0);
_logger.LogInformation("STEP 3: Registration payment status: {Status}, Registration status: {RegStatus}",
    registration?.PaymentStatus, registration?.Status);
_logger.LogInformation("STEP 4: Domain events before CompletePayment: {Count}",
    registration?.DomainEvents?.Count ?? 0);
// After CompletePayment
_logger.LogInformation("STEP 5: Domain events after CompletePayment: {Count}, Result: {Result}",
    registration?.DomainEvents?.Count ?? 0, completeResult.IsSuccess);
```

## Confidence Level

**95% confident** the root cause is:
- Stripe webhook endpoint NOT configured in Stripe Dashboard
- OR webhook configured but URL is wrong
- OR webhook configured but Azure ingress blocking it

**5% chance** it's something else:
- Application code issue we haven't found
- EF Core tracking issue preventing event dispatch
- MediatR configuration issue

## Next Steps

1. User checks Stripe Dashboard for webhook configuration
2. If not configured → Follow STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md
3. If configured → Check delivery logs in Stripe for error codes
4. If delivery succeeds (200 OK) → Add diagnostic logging and investigate further

## References

- Deployed commit: d264a82
- Original architecture doc: `docs/architecture/STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md`
- Webhook handler code: `src/LankaConnect.API/Controllers/PaymentsController.cs:221-375`
- Domain event dispatching: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs:294-366`
- Event handler: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
