# ADR-008: Webhook Idempotency Recovery Strategy

## Status
**Accepted** - 2025-12-17

## Context

### The Problem
A user completed payment for event registration at 9:59 PM UTC. The Stripe webhook `evt_1SfSktLvfbr023L1qB78D1CR` was successfully received and processed by an old Azure Container App revision, which:

1. ✅ Verified webhook signature
2. ✅ Updated registration status to Confirmed
3. ✅ Updated payment status to Completed
4. ✅ Stored StripePaymentIntentId
5. ✅ Saved to database successfully
6. ✅ Returned HTTP 200 to Stripe
7. ❌ **Failed to dispatch PaymentCompletedEvent** (IPublisher was NULL due to DI bug)
8. ❌ **Failed to generate ticket**
9. ❌ **Failed to send confirmation email with ticket PDF**

At 11:12 PM UTC, a fixed revision was deployed with IPublisher correctly configured. However, when the user manually resent the webhook, the system correctly skipped processing due to idempotency check:

```
Event evt_1SfSktLvfbr023L1qB78D1CR already processed, skipping
```

This left the user with a completed payment but no ticket or confirmation email.

### Architecture Constraints

Our system uses **Clean Architecture with Domain-Driven Design**:

1. **Domain Events**: `Registration.CompletePayment()` raises `PaymentCompletedEvent`
2. **Event Dispatching**: `AppDbContext.CommitAsync()` dispatches domain events via MediatR
3. **Side Effects**: `PaymentCompletedEventHandler` generates tickets and sends emails
4. **Idempotency**: Webhook events are tracked in `stripe_webhook_events` table to prevent duplicate processing

The idempotency mechanism is **critical** for payment integrity:
- Prevents double-charging users
- Prevents duplicate registration confirmations
- Ensures Stripe webhook retries are handled safely

### Business Impact

**User Impact**: High severity
- User paid $250 for event registration
- No confirmation email received
- No ticket PDF with QR code received
- Cannot attend event without ticket

**System Impact**: Low severity
- Database state is correct (payment recorded, registration confirmed)
- Only side effects (email/ticket) are missing
- No data corruption or integrity issues

## Decision Drivers

1. **Data Integrity**: Must not compromise payment or registration state
2. **Audit Trail**: Must preserve complete record of webhook processing
3. **Idempotency**: Must not break idempotency guarantees for future webhooks
4. **User Experience**: User must receive ticket and email as soon as possible
5. **Risk**: Solution must minimize risk of duplicate tickets or emails
6. **Simplicity**: Prefer simple solution that can be executed quickly
7. **Reversibility**: Solution should be reversible if errors occur

## Options Considered

### Option 1: Reset Idempotency Flag (Database Modification)

**Approach**: Mark webhook event as unprocessed, resend webhook from Stripe

```sql
UPDATE stripe_webhook_events
SET Processed = 0, ProcessedAt = NULL
WHERE EventId = 'evt_1SfSktLvfbr023L1qB78D1CR';
```

**Pros**:
- Webhook flow reprocesses naturally
- No code changes required
- Uses existing business logic

**Cons**:
- ❌ **Violates idempotency principle** - webhook is processed twice
- ❌ **Risk of duplicate processing** if webhook fires again before manual retry
- ❌ **Could cause data corruption** - `CompletePayment()` expects Pending status
- ❌ **Audit trail shows incorrect state** - event marked as "not processed" when it was
- ❌ **Race condition risk** - Stripe might retry webhook during modification

**Verdict**: ❌ **Rejected** - Too risky, violates architectural principles

---

### Option 2: Manual Ticket Generation (Application Service Call)

**Approach**: Call ticket service directly, bypass domain events

```csharp
var ticket = await _ticketService.GenerateTicketAsync(registrationId, eventId);
await _emailService.SendEmailAsync(email, ticket);
```

**Pros**:
- Simple and direct
- No domain event complexity
- Can be executed via admin endpoint

**Cons**:
- ❌ **Bypasses domain event architecture** - inconsistent with normal flow
- ❌ **Duplicates business logic** - email/ticket generation logic in two places
- ❌ **May miss future enhancements** - if PaymentCompletedEventHandler is updated, this won't benefit
- ❌ **No guarantee of consistency** - manual call might use different parameters
- ❌ **Creates technical debt** - two paths for same operation

**Verdict**: ❌ **Rejected** - Violates architectural consistency

---

### Option 3: Create New Payment (Business Process Change)

**Approach**: Refund original payment, create new registration

1. Refund Stripe payment
2. Cancel original registration
3. User re-registers with new payment
4. New webhook processes normally

**Pros**:
- Clean slate - uses normal flow end-to-end
- No technical hacks or workarounds
- Fully auditable

**Cons**:
- ❌ **Poor user experience** - user must re-register
- ❌ **Accounting complexity** - refund + new charge creates reconciliation issues
- ❌ **Time consuming** - requires user action, Stripe processing time
- ❌ **Risk of lost revenue** - user might not re-register
- ❌ **Doesn't solve technical problem** - could happen again

**Verdict**: ❌ **Rejected** - Unnecessary complexity, poor UX

---

### Option 4: Manual Domain Event Trigger (Selected) ✅

**Approach**: Create temporary admin endpoint that manually publishes `PaymentCompletedEvent`

```csharp
// Verify payment is completed
if (registration.PaymentStatus != PaymentStatus.Completed)
    return BadRequest("Payment not completed");

// Construct domain event manually
var event = new PaymentCompletedEvent(
    registration.EventId,
    registration.Id,
    registration.UserId,
    contactEmail,
    registration.StripePaymentIntentId,
    registration.TotalPrice.Amount,
    registration.GetAttendeeCount(),
    registration.UpdatedAt
);

// Publish via MediatR (same as normal flow)
await _publisher.Publish(new DomainEventNotification<PaymentCompletedEvent>(event));
```

**Pros**:
- ✅ **Preserves data integrity** - No modification to payment/registration state
- ✅ **Maintains audit trail** - Webhook processing record unchanged
- ✅ **Follows architecture** - Uses normal domain event dispatch mechanism
- ✅ **Idempotent** - Can be called multiple times safely (ticket service has duplicate checks)
- ✅ **Simple execution** - Single API call
- ✅ **Low risk** - Read-only operation, comprehensive validation
- ✅ **Reversible** - No permanent changes, can retry if errors
- ✅ **Consistent logic** - Same handler processes event as normal flow

**Cons**:
- ⚠️ **Requires code deployment** - Admin controller must be deployed
- ⚠️ **Manual intervention** - Not fully automated
- ⚠️ **Temporary code** - Controller should be removed after recovery
- ⚠️ **Potential duplicate emails** - If called multiple times, user gets multiple emails

**Mitigation**:
- Code already deployed to current revision
- Comprehensive validation prevents incorrect usage
- Clear documentation for cleanup
- Duplicate email risk is acceptable (user can ignore duplicates)

**Verdict**: ✅ **Accepted** - Best balance of safety, simplicity, and architectural integrity

## Decision

**We will implement Option 4: Manual Domain Event Trigger**

### Implementation Components

1. **SQL Verification Script**: `scripts/recover-payment-completed-event.sql`
   - Verifies registration state
   - Checks for existing tickets
   - Provides data for manual event construction

2. **Admin Recovery Controller**: `src/LankaConnect.API/Controllers/AdminRecoveryController.cs`
   - Endpoint: `POST /api/admin/recovery/trigger-payment-event`
   - Validates registration state
   - Constructs PaymentCompletedEvent manually
   - Publishes via IPublisher (MediatR)
   - Comprehensive logging for audit

3. **Recovery Documentation**:
   - Full plan: `docs/architecture/WEBHOOK_IDEMPOTENCY_RECOVERY_PLAN.md`
   - Quick start: `scripts/RECOVERY_QUICK_START.md`
   - This ADR: `docs/architecture/ADR-008-WEBHOOK-IDEMPOTENCY-RECOVERY-STRATEGY.md`

### Safety Mechanisms

**Pre-Execution Validation**:
- Registration must exist
- PaymentStatus must be Completed
- RegistrationStatus must be Confirmed
- StripePaymentIntentId must be populated
- Contact email must exist

**Post-Execution Verification**:
- Check logs for event handler invocation
- Verify ticket created in database
- Confirm email sent via Azure Communication Services
- User confirms receipt of email and ticket PDF

**Rollback Plan**:
- No rollback needed - operation is read-only
- If duplicate tickets created, mark old ones as Cancelled
- If duplicate emails sent, no action needed (user ignores duplicates)

## Consequences

### Positive

1. **User Gets Ticket**: Problem is resolved quickly (< 5 minutes execution time)
2. **Data Integrity Maintained**: No changes to payment or registration state
3. **Audit Trail Preserved**: Complete record of original webhook processing
4. **Architectural Consistency**: Uses same domain event handler as normal flow
5. **Low Risk**: Comprehensive validation and idempotent design
6. **Reusable**: Pattern can be used for future similar incidents

### Negative

1. **Manual Intervention Required**: Not automated, requires admin action
2. **Temporary Code**: Admin controller must be removed after recovery
3. **Duplicate Email Risk**: User may receive multiple emails if endpoint called repeatedly
4. **Doesn't Prevent Recurrence**: Original bug (IPublisher NULL) was fixed separately

### Neutral

1. **Documentation Overhead**: Comprehensive docs created for single-use recovery
2. **Learning Opportunity**: Team now has playbook for webhook idempotency issues
3. **Future Reference**: ADR serves as architectural guidance for similar scenarios

## Compliance and Security

### Authorization
- Endpoint requires `[Authorize]` attribute
- Recommended: Add admin role requirement in production
- Log all recovery operations for audit

### Data Privacy
- No exposure of sensitive payment data
- Email addresses logged for audit trail (acceptable under operational necessity)
- User notification of issue is recommended (transparency)

### Regulatory
- PCI DSS: No modification to payment data, maintains compliance
- GDPR: Operational necessity justifies email address logging
- SOX/Audit: Complete audit trail maintained, manual intervention documented

## Monitoring and Prevention

### Immediate Monitoring
- Alert on PaymentCompletedEvent dispatch failures
- Track email send success/failure rates
- Monitor ticket generation latency

### Long-Term Prevention
1. **Health Checks**: Add endpoint that verifies critical DI dependencies (IPublisher, IEmailService, ITicketService)
2. **Integration Tests**: Add test for webhook → event → handler flow with mocked dependencies
3. **Deployment Validation**: Fail deployment if health checks don't pass
4. **Observability**: Add distributed tracing for webhook → event → handler flow

### Future Architectural Improvements
1. **Idempotency Design Enhancement**: Track individual processing steps (payment, email, ticket) separately
2. **Retry Mechanism**: Allow selective retry of failed steps without reprocessing payment
3. **Event Sourcing**: Consider event sourcing pattern for payment flow to enable replay
4. **Circuit Breaker**: Implement circuit breaker for email/ticket services to prevent cascading failures

## References

### Related Documentation
- [PHASE_6A_24_TICKET_GENERATION_SUMMARY.md](../PHASE_6A_24_TICKET_GENERATION_SUMMARY.md) - Ticket generation implementation
- [PAYMENT_WEBHOOK_ROOT_CAUSE_ANALYSIS.md](./PAYMENT_WEBHOOK_ROOT_CAUSE_ANALYSIS.md) - Original IPublisher NULL bug analysis
- [STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md](./STRIPE_WEBHOOK_CONFIGURATION_ARCHITECTURE.md) - Webhook flow architecture

### Related Code
- **Domain Event**: `src/LankaConnect.Domain/Events/DomainEvents/PaymentCompletedEvent.cs`
- **Event Handler**: `src/LankaConnect.Application/Events/EventHandlers/PaymentCompletedEventHandler.cs`
- **Webhook Controller**: `src/LankaConnect.API/Controllers/PaymentsController.cs` (Line 225-295)
- **Event Dispatching**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs` (Line 294-366)
- **Registration Domain**: `src/LankaConnect.Domain/Events/Registration.cs` (Line 235-264)

### Stripe Documentation
- [Webhook Idempotency](https://stripe.com/docs/webhooks/best-practices#duplicate-events)
- [Event Types](https://stripe.com/docs/api/events/types)
- [Retry Behavior](https://stripe.com/docs/webhooks/best-practices#retry-logic)

## Review and Approval

**Author**: Claude Code (System Architecture Designer)
**Date**: 2025-12-17
**Reviewers**: [Your team members]
**Approval Status**: Ready for implementation

---

**Change Log**:
- 2025-12-17: Initial ADR created based on production incident analysis
