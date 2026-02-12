# Root Cause Analysis: Production Stripe Webhook 404 Error

**Date**: 2026-02-11
**Severity**: CRITICAL
**Impact**: All production payments succeed at Stripe but fail to confirm registrations
**Status**: RESOLVED - Configuration fixed

---

## Executive Summary

Production Stripe webhooks were returning **HTTP 404 errors** after successful payment processing, causing registrations to remain in "Preliminary" status indefinitely. Users were charged but did not receive tickets or confirmation emails.

**Root Cause**: Webhook endpoint URL mismatch between Stripe Dashboard configuration and actual backend API endpoint.

**Resolution**: Updated Stripe webhook URL from `/api/webhooks/stripe` to `/api/payments/webhook`.

---

## Incident Details

### Timeline
- **2026-02-11 ~6:30 PM EST**: User paid $2.00 for event registration (Event ID: `aeffb10e-ff84-4497-a86e-8b2fbac09cf2`)
- **2026-02-11 ~6:30 PM EST**: Stripe successfully processed payment (Payment Intent: `pi_3SzmrdRqh3VBExQm2s...`)
- **2026-02-11 ~6:30 PM EST**: Stripe webhook delivery failed with HTTP 404
- **2026-02-11 ~6:35 PM EST**: User stuck on "Payment Pending" page despite successful payment
- **2026-02-11 ~8:00 PM EST**: Issue discovered via Stripe Dashboard showing "404 ERR" status

### Evidence
1. **Stripe Dashboard**:
   - Event: `payment_intent.succeeded`
   - Event ID: `evt_3SzmrdRqh3VBExQm2sIXKAnuz`
   - Delivery Status: "Failed" with HTTP 404
   - Retry attempts: Multiple failures with "Next retry in 56 minutes"

2. **User Experience**:
   - Payment page redirected to Stripe checkout
   - Stripe showed "You're all done here" (payment completed)
   - $2.00 charged to credit card
   - Events listing showed green "You are registered" badge
   - But registration status was "Preliminary" (not "Confirmed")

3. **Missing Confirmation**:
   - No ticket generated
   - No confirmation email sent
   - User unable to check-in at event

---

## Root Cause Analysis

### Primary Root Cause: Webhook URL Path Mismatch

**Stripe Dashboard Configuration**:
```
Endpoint URL: https://api.lankaconnect.app/api/webhooks/stripe
                                                    ^^^^^^^^
                                                    WRONG PATH
```

**Actual Backend Endpoint** (PaymentsController.cs:236-340):
```csharp
[Route("api/[controller]")]  // controller = "payments"
public class PaymentsController : ControllerBase
{
    [HttpPost("webhook")]  // Route: api/payments/webhook
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        // Webhook handler implementation
    }
}
```

**Correct URL**:
```
https://api.lankaconnect.app/api/payments/webhook
                                    ^^^^^^^^
                                    CORRECT PATH
```

### Contributing Factors

1. **No Webhook Health Monitoring**
   - No alerting when webhook deliveries fail
   - Issue only discovered when user complained
   - No dashboard showing webhook failure rate

2. **Misleading Success Page**
   - Payment success page shows "Payment Successful!" based on Stripe redirect
   - Does NOT wait for webhook confirmation
   - Users assume registration is complete when it's not

3. **Misleading "You are registered" Badge**
   - Events listing shows badge for ANY registration status
   - Does not distinguish between "Preliminary" and "Confirmed"
   - Users believe they are registered when payment is still pending

---

## Technical Details

### Webhook Flow (Normal)
```
1. User submits payment → Stripe Checkout
2. Stripe processes payment → payment_intent.succeeded event
3. Stripe POSTs webhook to backend → /api/payments/webhook
4. Backend verifies signature → Handles checkout.session.completed
5. Registration.CompletePayment() → Status: Preliminary → Confirmed
6. PaymentCompletedEvent dispatched → Ticket generated + Email sent
```

### Webhook Flow (Failure - 404)
```
1. User submits payment → Stripe Checkout ✅
2. Stripe processes payment → payment_intent.succeeded event ✅
3. Stripe POSTs to WRONG URL → /api/webhooks/stripe ❌ (404 Not Found)
4. Backend never receives webhook ❌
5. Registration stays Preliminary ❌
6. No ticket, no email ❌
```

### Event Types Confusion

**Important Note**: The Stripe Dashboard screenshot showed `payment_intent.succeeded` event, but the code handles `checkout.session.completed`. These are **NOT different workflows** - they are **sequential events in the SAME Checkout Session**:

- `payment_intent.succeeded` fires when charge is authorized
- `checkout.session.completed` fires when entire session completes (includes payment intent ID)

The current code correctly uses `checkout.session.completed` as the canonical event.

---

## Impact Assessment

### User Impact
- **Affected Users**: All production payments between [unknown start date] and 2026-02-11
- **User Experience**: Paid but no ticket received
- **Financial Impact**: Users charged but service not delivered
- **Trust Impact**: Users may request chargebacks or refunds

### Data Impact
- **Orphaned Registrations**: All registrations stuck in "Preliminary" status
- **Missing Tickets**: No PDF tickets generated for affected users
- **Missing Emails**: No confirmation emails sent
- **Revenue Tracking**: Payment recorded at Stripe but not in backend analytics

---

## Resolution

### Immediate Fix (Completed)
1. Updated Stripe Dashboard webhook URL:
   - **Old**: `https://api.lankaconnect.app/api/webhooks/stripe`
   - **New**: `https://api.lankaconnect.app/api/payments/webhook`

2. Verified endpoint is reachable:
   ```bash
   curl -X POST https://api.lankaconnect.app/api/payments/webhook
   # Expected: HTTP 400 (signature invalid)
   # Confirms endpoint exists
   ```

### Manual Cleanup Required
For the affected $2.00 registration (`aeffb10e-ff84-4497-a86e-8b2fbac09cf2`):

```sql
-- 1. Verify payment succeeded in Stripe Dashboard first
-- 2. Manually complete the registration:

UPDATE events.registrations
SET
    "Status" = 'Confirmed',
    "PaymentStatus" = 'Completed',
    "ConfirmedAt" = NOW(),
    "PaymentIntentId" = 'pi_3SzmrdRqh3VBExQm2s...'
WHERE "Id" = 'aeffb10e-ff84-4497-a86e-8b2fbac09cf2';

-- 3. Manually send confirmation email + generate ticket
-- (Requires custom admin endpoint - to be implemented)
```

---

## Prevention Measures

### Implemented
- [x] Corrected webhook URL in Stripe Dashboard
- [ ] Documented correct URL in deployment documentation

### Recommended (Not Yet Implemented)

#### 1. Webhook Health Monitoring (HIGH PRIORITY)
```csharp
// Add background service to detect orphaned registrations
public class WebhookHealthMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var orphaned = await _registrationRepo
                .Where(r => r.Status == RegistrationStatus.Preliminary
                         && r.CreatedAt < DateTime.UtcNow.AddMinutes(-10))
                .CountAsync();

            if (orphaned > 0)
            {
                _logger.LogCritical("[WEBHOOK HEALTH] {Count} orphaned registrations!", orphaned);
                // Send alert to Slack/PagerDuty
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

#### 2. Payment Success Page Polling (MEDIUM PRIORITY)
Replace immediate "Payment Successful!" message with polling:
```typescript
// Poll backend every 2 seconds until status = Confirmed
const [status, setStatus] = useState<'processing' | 'confirmed' | 'timeout'>('processing');

useEffect(() => {
  const pollInterval = setInterval(async () => {
    const reg = await api.get(`/api/registrations/${registrationId}`);
    if (reg.status === 'Confirmed') {
      setStatus('confirmed');
      clearInterval(pollInterval);
    }
  }, 2000);

  setTimeout(() => setStatus('timeout'), 60000); // 60s timeout
}, []);
```

#### 3. Fix "You are registered" Badge (HIGH PRIORITY)
Only show badge for `Confirmed` status, not `Preliminary`:
```typescript
// Current (WRONG): Shows badge for ANY status
isRegistered={registeredEventIds.has(event.id)}

// Fixed (CORRECT): Only show for Confirmed
isRegistered={userRegistrations.find(r =>
  r.eventId === event.id && r.status === RegistrationStatus.Confirmed
)}
```

#### 4. Automated Testing
Add integration test to verify webhook endpoint:
```csharp
[Fact]
public async Task WebhookEndpoint_ShouldReturn400_WhenSignatureInvalid()
{
    var response = await _client.PostAsync("/api/payments/webhook",
        new StringContent("{}", Encoding.UTF8, "application/json"));

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

#### 5. Deployment Verification Checklist
Add to deployment documentation:
- [ ] Verify Stripe webhook URL matches deployed API endpoint
- [ ] Test webhook delivery with `stripe trigger checkout.session.completed`
- [ ] Check webhook signing secret is configured in Azure Key Vault
- [ ] Monitor first 10 webhook deliveries post-deployment

---

## Lessons Learned

### What Went Well
- Webhook handler code was correct (signature verification, idempotency, logging)
- Routing configuration was correct (UseRouting before UseAuthentication)
- Issue was quickly identified using Stripe Dashboard delivery logs

### What Went Wrong
- No monitoring to detect webhook failures
- Misleading UX hid the problem from users
- No automated testing of webhook endpoint
- No deployment verification checklist

### Architectural Gaps
1. **Observability**: No webhook health metrics or alerting
2. **UX**: Success page shows success before backend confirmation
3. **Testing**: No end-to-end payment flow tests
4. **Documentation**: Webhook URL not documented in deployment guide

---

## Related Issues

- **RCA_STRIPE_WEBHOOK_NO_EVENT_DELIVERIES.md** (2026-01-30) - Webhook created AFTER payment, events not retroactively delivered
- **RCA_PAYMENT_WEBHOOK_CONCURRENCY_ISSUE.md** (2026-01-30) - DbUpdateConcurrencyException during webhook processing
- **Issue #2** (2026-02-11) - Misleading "You are registered" badge for Preliminary status

---

## Action Items

| Priority | Task | Assignee | Status |
|----------|------|----------|--------|
| P0 | Update Stripe webhook URL | DevOps | ✅ DONE |
| P0 | Manually fix affected $2 registration | Backend | ⏳ PENDING |
| P1 | Implement webhook health monitoring | Backend | 📋 PLANNED |
| P1 | Fix "You are registered" badge | Frontend | 📋 PLANNED |
| P2 | Add success page polling | Frontend | 📋 PLANNED |
| P2 | Add webhook endpoint integration test | Backend | 📋 PLANNED |
| P3 | Document webhook URL in deployment guide | DevOps | 📋 PLANNED |

---

## References

- **Stripe Dashboard**: Developers → Webhooks → LankaConnect Production Payments
- **Backend Code**: `src/LankaConnect.API/Controllers/PaymentsController.cs` (Lines 236-340)
- **Event Handler**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
- **Frontend Success Page**: `web/src/app/events/payment/success/page.tsx`
- **Registration Badge**: `web/src/presentation/components/features/events/RegistrationBadge.tsx`

---

**Prepared by**: Claude Sonnet 4.5 (AI Assistant)
**Reviewed by**: [Pending]
**Approved by**: [Pending]
