# Root Cause Analysis: Orphaned Stripe Payment
**Date**: 2025-12-18
**Incident**: Stripe payment succeeded but registration never existed in database
**Severity**: CRITICAL - Customer paid but received nothing
**Webhook ID**: `evt_1SfZoMLvfbr023L1xcB9paik`
**Checkout Session**: `cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk`

---

## Executive Summary

A user successfully paid for event registration through Stripe, but the registration **never existed** in the database. The webhook was received and marked as processed, but the handler silently failed to complete the payment flow because no registration could be found. This is a **critical customer service issue** requiring immediate action.

---

## Evidence Chain

### 1. Webhook Event (CONFIRMED)
```sql
SELECT * FROM stripe_webhook_events WHERE event_id = 'evt_1SfZoMLvfbr023L1xcB9paik';
```
**Result**:
- `event_type`: checkout.session.completed
- `processed`: TRUE
- `processed_at`: 2025-12-18 05:31:38.743625+00
- `error_message`: NULL

**Conclusion**: Webhook received and marked as processed successfully.

### 2. Registration Search (NOT FOUND)
```sql
SELECT * FROM events.registrations
WHERE "StripeCheckoutSessionId" = 'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk';
```
**Result**: ZERO ROWS

**Conclusion**: Registration with this checkout session ID **never existed** in database.

### 3. Recent Registrations Review
```sql
SELECT * FROM events.registrations
WHERE "CreatedAt" >= NOW() - INTERVAL '7 days'
ORDER BY "CreatedAt" DESC
LIMIT 20;
```
**Result**: 20 registrations found, NONE matching the webhook's checkout session ID.

---

## Expected Flow vs. Actual Flow

### Expected (Correct) Flow
```
1. User fills registration form
2. POST /api/events/{id}/rsvp → RsvpToEventCommandHandler
3. Registration.CreateWithAttendees() → Registration created (Status: Pending, PaymentStatus: Pending)
4. Event.Registrations.Add(registration) → Registration added to Event aggregate
5. UnitOfWork.CommitAsync() → Registration persisted to database
6. StripePaymentService.CreateEventCheckoutSessionAsync() → Checkout session created
7. registration.SetStripeCheckoutSession(sessionId) → StripeCheckoutSessionId saved
8. UnitOfWork.CommitAsync() → Checkout session ID persisted
9. User redirected to Stripe → Payment completed
10. Webhook received → checkout.session.completed
11. PaymentsController.HandleCheckoutSessionCompletedAsync()
    - Extract metadata.registration_id
    - Load registration from database
    - registration.CompletePayment(paymentIntentId)
    - UnitOfWork.CommitAsync()
    - PaymentCompletedEvent raised → Email + Ticket generated
```

### Actual (Broken) Flow
```
1. User filled registration form (??)
2. POST /api/events/{id}/rsvp (?)
3. ??? Something failed here ???
4. Stripe checkout session created → cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk
5. User redirected to Stripe
6. User paid successfully
7. Webhook received → evt_1SfZoMLvfbr023L1xcB9paik
8. PaymentsController.HandleCheckoutSessionCompletedAsync()
    - Extracted metadata.registration_id (???)
    - Attempted to load registration
    - Registration NOT FOUND
    - Logged error (line 355)
    - SILENTLY RETURNED (no exception thrown)
9. Webhook marked as processed
10. User received NOTHING
```

---

## Root Cause Analysis

### Most Likely Scenario: Transaction Rollback After Stripe Session Creation

**Code Path**: `RsvpToEventCommandHandler.HandleMultiAttendeeRsvp()`

**The Problem** (Lines 91-144):
```csharp
// Line 91-101: Registration created and added to aggregate
var registerResult = @event.RegisterWithAttendees(...);
_eventRepository.Update(@event);

// Line 104: Get just-created registration
var registration = @event.Registrations.Last();

// Line 108-133: Create Stripe checkout session
if (!@event.IsFree())
{
    var checkoutRequest = new CreateEventCheckoutSessionRequest { ... };
    var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);

    if (checkoutResult.IsFailure)
        return Result<string?>.Failure(...); // ← ROLLBACK TRIGGERED HERE

    // Line 136-138: Save checkout session ID
    var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value);
    if (setSessionResult.IsFailure)
        return Result<string?>.Failure(...); // ← OR ROLLBACK HERE

    // Line 141: Commit transaction
    await _unitOfWork.CommitAsync(cancellationToken);

    // Line 144: Return checkout URL
    return Result<string?>.Success(checkoutResult.Value);
}
```

**The Bug**:
1. Registration is created in memory (Lines 91-95)
2. Stripe checkout session created (Line 131) **BEFORE first commit**
3. If Stripe call succeeds BUT something fails afterward (validation, etc.)
4. Return failure → **Transaction rollback**
5. Registration deleted from database
6. Stripe checkout session PERSISTS (external system)
7. User redirected to Stripe
8. User pays
9. Webhook arrives
10. Registration not found (was rolled back)

---

## Why Silent Failure Occurred

**PaymentsController.cs Lines 323-357**:
```csharp
// Extract metadata
if (!session.Metadata.TryGetValue("registration_id", out var registrationIdStr) ||
    !Guid.TryParse(registrationIdStr, out var registrationId))
{
    _logger.LogWarning("Checkout session {SessionId} missing registration_id in metadata", session.Id);
    return; // ← SILENT RETURN (webhook still marked as processed)
}

// Get event with registrations
var @event = await _eventRepository.GetByIdAsync(eventId);
if (@event == null)
{
    _logger.LogError("Event {EventId} not found for checkout session {SessionId}", eventId, session.Id);
    return; // ← SILENT RETURN
}

// Find the registration
var registration = @event.Registrations.FirstOrDefault(r => r.Id == registrationId);
if (registration == null)
{
    _logger.LogError("Registration {RegistrationId} not found in Event {EventId}", registrationId, eventId);
    return; // ← SILENT RETURN - THIS IS WHERE IT FAILED
}
```

**The Issue**:
- Webhook handler logs error but **DOES NOT THROW EXCEPTION**
- Returns normally → Webhook marked as processed
- No retry mechanism
- No alerting
- **Payment orphaned permanently**

---

## Supporting Evidence

### Code Analysis

**1. Transaction Boundaries**:
- First `UnitOfWork.CommitAsync()` at line 141 (AFTER Stripe call)
- If any failure before line 141 → Rollback
- Stripe call succeeds → Session created externally
- Local transaction rolls back → Registration lost

**2. Metadata Population**:
```csharp
// RsvpToEventCommandHandler.cs Line 123-128
Metadata = new Dictionary<string, string>
{
    { "event_id", @event.Id.ToString() },
    { "registration_id", registration.Id.ToString() },  // ← This ID was never persisted
    { "user_id", request.UserId.ToString() }
}
```

**3. No Compensating Transaction**:
- If Stripe session created but commit fails → No rollback of Stripe session
- No mechanism to cancel checkout session
- Orphaned payment guaranteed

---

## Impact Assessment

### Immediate Impact
- **1 confirmed customer** paid but received nothing
- Customer experience: SEVERE
- Refund required or manual registration creation

### Potential Scale
To determine if this is systemic or one-off, run:

```sql
-- Find webhooks that were processed but had errors
SELECT
    w.event_id,
    w.event_type,
    w.processed_at,
    w.error_message
FROM stripe_webhook_events w
WHERE w.event_type = 'checkout.session.completed'
  AND w.processed = true
  AND (w.error_message IS NOT NULL OR w.processed_at < NOW() - INTERVAL '1 hour')
ORDER BY w.created_at DESC
LIMIT 50;

-- Find registrations with pending payment status older than 1 hour
SELECT
    r."Id",
    r."EventId",
    r."Status",
    r."PaymentStatus",
    r."StripeCheckoutSessionId",
    r."CreatedAt"
FROM events.registrations r
WHERE r."PaymentStatus" = 1  -- PaymentStatus.Pending
  AND r."CreatedAt" < NOW() - INTERVAL '1 hour'
ORDER BY r."CreatedAt" DESC;

-- Find checkout sessions in Stripe metadata that don't exist in database
-- (This requires Stripe API call - cannot be done via SQL)
```

### Business Risk
- **High**: Every paid event registration could fail this way
- **Customer trust**: Damaged if not resolved quickly
- **Financial liability**: Potential refunds + customer service costs

---

## Recovery Strategy

### Immediate Actions (Customer Service)

**Option A: Manual Registration Creation** (RECOMMENDED if event hasn't occurred)
```sql
-- 1. Get checkout session details from Stripe API
-- stripe.checkout.sessions.retrieve('cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk')

-- 2. Extract metadata (event_id, user_id, amount_paid)

-- 3. Manually create registration in database
INSERT INTO events.registrations (
    "Id",
    "EventId",
    "UserId",
    "Status",
    "PaymentStatus",
    "StripeCheckoutSessionId",
    "StripePaymentIntentId",
    "Quantity",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    gen_random_uuid(),  -- New registration ID
    '<event_id_from_metadata>',
    '<user_id_from_metadata>',
    2,  -- RegistrationStatus.Confirmed
    2,  -- PaymentStatus.Completed
    'cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk',
    '<payment_intent_id_from_stripe>',
    1,  -- Or actual quantity
    NOW(),
    NOW()
);

-- 4. Manually trigger email and ticket generation
-- (Call ResendTicketEmailCommand or manually generate via PaymentCompletedEvent)
```

**Option B: Refund Payment** (If event already occurred or data incomplete)
```bash
# Refund via Stripe API
stripe refunds create --payment-intent=<payment_intent_id> --reason=duplicate
```

### Monitoring Query
```sql
-- Run this hourly to catch future occurrences
SELECT
    w.event_id as webhook_id,
    w.created_at as webhook_time,
    w.processed,
    COALESCE(r."Id"::text, 'NOT FOUND') as registration_id,
    r."Status" as reg_status,
    r."PaymentStatus" as payment_status
FROM stripe_webhook_events w
LEFT JOIN events.registrations r ON r."StripeCheckoutSessionId" =
    (SELECT metadata->>'checkout_session_id' FROM stripe.webhook_events WHERE event_id = w.event_id)
WHERE w.event_type = 'checkout.session.completed'
  AND w.created_at > NOW() - INTERVAL '1 hour'
ORDER BY w.created_at DESC;
```

---

## Prevention: Fix Plan

### Fix 1: Reorder Stripe Call AFTER First Commit (HIGHEST PRIORITY)

**File**: `RsvpToEventCommandHandler.cs` Line 60-150

**Change**:
```csharp
// CURRENT (BROKEN):
var registerResult = @event.RegisterWithAttendees(...);
_eventRepository.Update(@event);
var registration = @event.Registrations.Last();

if (!@event.IsFree())
{
    var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);
    registration.SetStripeCheckoutSession(checkoutResult.Value);
    await _unitOfWork.CommitAsync(cancellationToken); // ← First commit
    return Result<string?>.Success(checkoutResult.Value);
}

// FIXED (CORRECT):
var registerResult = @event.RegisterWithAttendees(...);
_eventRepository.Update(@event);
var registration = @event.Registrations.Last();

// COMMIT REGISTRATION FIRST (before external Stripe call)
await _unitOfWork.CommitAsync(cancellationToken);

if (!@event.IsFree())
{
    var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);
    if (checkoutResult.IsFailure)
    {
        // Registration already persisted - cancel it
        registration.Cancel();
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<string?>.Failure($"Failed to create payment session: {checkoutResult.Error}");
    }

    registration.SetStripeCheckoutSession(checkoutResult.Value);
    await _unitOfWork.CommitAsync(cancellationToken); // ← Second commit (just session ID)
    return Result<string?>.Success(checkoutResult.Value);
}
```

**Why This Fixes It**:
- Registration persisted BEFORE Stripe call
- If Stripe fails → Registration exists but status=Pending
- Can be cleaned up later (expired pending registrations)
- No orphaned payments

---

### Fix 2: Add Compensating Transaction for Stripe Session

**File**: `RsvpToEventCommandHandler.cs`

**Add**:
```csharp
private async Task<Result<string?>> HandleMultiAttendeeRsvp(...)
{
    // ... existing code ...

    string? checkoutSessionId = null;

    try
    {
        var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);
        if (checkoutResult.IsFailure)
            return Result<string?>.Failure(...);

        checkoutSessionId = checkoutResult.Value;

        // Set session ID on registration
        var setSessionResult = registration.SetStripeCheckoutSession(checkoutSessionId);
        if (setSessionResult.IsFailure)
        {
            // Compensating transaction: Cancel Stripe session
            await CancelStripeCheckoutSessionAsync(checkoutSessionId);
            return Result<string?>.Failure(...);
        }

        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<string?>.Success(checkoutSessionId);
    }
    catch (Exception ex)
    {
        // Compensating transaction: Cancel Stripe session if it was created
        if (checkoutSessionId != null)
            await CancelStripeCheckoutSessionAsync(checkoutSessionId);

        throw;
    }
}

private async Task CancelStripeCheckoutSessionAsync(string sessionId)
{
    try
    {
        var sessionService = new Stripe.Checkout.SessionService(_stripeClient);
        await sessionService.ExpireAsync(sessionId); // Mark session as expired
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to cancel Stripe checkout session {SessionId}", sessionId);
    }
}
```

---

### Fix 3: Add Webhook Retry Mechanism

**File**: `PaymentsController.cs` Lines 300-386

**Change**:
```csharp
private async Task HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent)
{
    try
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogWarning("Checkout session data is null for event {EventId}", stripeEvent.Id);
            throw new InvalidOperationException("Checkout session data is null"); // ← THROW instead of return
        }

        // ... existing metadata extraction ...

        if (!session.Metadata.TryGetValue("registration_id", out var registrationIdStr) ||
            !Guid.TryParse(registrationIdStr, out var registrationId))
        {
            _logger.LogError("Checkout session {SessionId} missing registration_id - ORPHANED PAYMENT", session.Id);

            // ALERT: Send notification to admin/ops team
            await _alertService.SendCriticalAlertAsync(
                "Orphaned Stripe Payment Detected",
                $"Checkout session {session.Id} missing registration_id. Manual intervention required.");

            throw new InvalidOperationException($"Missing registration_id for session {session.Id}"); // ← THROW
        }

        // ... load event ...

        var registration = @event.Registrations.FirstOrDefault(r => r.Id == registrationId);
        if (registration == null)
        {
            _logger.LogError("Registration {RegistrationId} not found - ORPHANED PAYMENT. Session: {SessionId}",
                registrationId, session.Id);

            // ALERT: Send notification
            await _alertService.SendCriticalAlertAsync(
                "Orphaned Stripe Payment - Registration Not Found",
                $"Registration {registrationId} not found for session {session.Id}. User paid but has no registration.");

            throw new InvalidOperationException($"Registration {registrationId} not found"); // ← THROW
        }

        // ... complete payment ...
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling checkout.session.completed webhook");

        // Re-throw to trigger HTTP 500 → Stripe retries webhook
        throw;
    }
}
```

**Why This Helps**:
- Throws exception → HTTP 500 response
- Stripe retries webhook (up to 3 days)
- Gives time to manually fix registration
- Alerts team immediately

---

### Fix 4: Add Monitoring and Alerting

**New File**: `OrphanedPaymentDetectionService.cs`

```csharp
public interface IOrphanedPaymentDetectionService
{
    Task<List<OrphanedPayment>> DetectOrphanedPaymentsAsync(CancellationToken cancellationToken = default);
}

public class OrphanedPaymentDetectionService : IOrphanedPaymentDetectionService
{
    private readonly IStripeClient _stripeClient;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<OrphanedPaymentDetectionService> _logger;

    public async Task<List<OrphanedPayment>> DetectOrphanedPaymentsAsync(CancellationToken cancellationToken)
    {
        var orphanedPayments = new List<OrphanedPayment>();

        // Get all checkout sessions from last 7 days
        var sessionService = new Stripe.Checkout.SessionService(_stripeClient);
        var options = new SessionListOptions
        {
            Created = new DateRangeOptions
            {
                GreaterThanOrEqual = DateTime.UtcNow.AddDays(-7)
            },
            Limit = 100
        };

        var sessions = await sessionService.ListAsync(options, cancellationToken: cancellationToken);

        foreach (var session in sessions.Data)
        {
            // Skip unpaid sessions
            if (session.PaymentStatus != "paid")
                continue;

            // Check if registration exists
            if (!session.Metadata.TryGetValue("registration_id", out var regIdStr) ||
                !Guid.TryParse(regIdStr, out var regId))
            {
                orphanedPayments.Add(new OrphanedPayment
                {
                    SessionId = session.Id,
                    Amount = session.AmountTotal ?? 0,
                    Currency = session.Currency,
                    Reason = "Missing registration_id in metadata"
                });
                continue;
            }

            // Check if registration exists in database
            if (!session.Metadata.TryGetValue("event_id", out var eventIdStr) ||
                !Guid.TryParse(eventIdStr, out var eventId))
                continue;

            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            var registration = @event?.Registrations.FirstOrDefault(r => r.Id == regId);

            if (registration == null)
            {
                orphanedPayments.Add(new OrphanedPayment
                {
                    SessionId = session.Id,
                    RegistrationId = regId,
                    EventId = eventId,
                    Amount = session.AmountTotal ?? 0,
                    Currency = session.Currency,
                    Reason = "Registration not found in database"
                });
            }
        }

        return orphanedPayments;
    }
}

// Run this as a scheduled background job every hour
public class OrphanedPaymentDetectionJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var orphaned = await _detectionService.DetectOrphanedPaymentsAsync(stoppingToken);

            if (orphaned.Any())
            {
                await _alertService.SendCriticalAlertAsync(
                    "Orphaned Payments Detected",
                    $"{orphaned.Count} orphaned payments found: {string.Join(", ", orphaned.Select(o => o.SessionId))}");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

---

### Fix 5: Add Registration Expiry Cleanup

**Purpose**: Clean up Pending registrations that never completed payment

```csharp
public class ExpiredPendingRegistrationCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Find registrations pending payment for more than 24 hours
            var expiredRegistrations = await _registrationRepository
                .GetExpiredPendingRegistrationsAsync(TimeSpan.FromHours(24), stoppingToken);

            foreach (var registration in expiredRegistrations)
            {
                _logger.LogWarning("Cancelling expired pending registration {RegistrationId}", registration.Id);
                registration.Cancel();
            }

            await _unitOfWork.CommitAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
```

---

## Testing Plan

### Unit Tests

**File**: `RsvpToEventCommandHandlerTests.cs`

```csharp
[Fact]
public async Task Handle_StripeCallFailsAfterRegistrationCreated_ShouldCancelRegistration()
{
    // Arrange
    var command = new RsvpToEventCommand { /* paid event */ };
    _stripePaymentService
        .Setup(s => s.CreateEventCheckoutSessionAsync(It.IsAny<CreateEventCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result<string>.Failure("Stripe error"));

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsFailure.Should().BeTrue();

    // Verify registration was cancelled (not orphaned)
    var registration = _event.Registrations.Last();
    registration.Status.Should().Be(RegistrationStatus.Cancelled);
}

[Fact]
public async Task Handle_DatabaseCommitFailsAfterStripeCall_ShouldCancelStripeSession()
{
    // Arrange
    var command = new RsvpToEventCommand { /* paid event */ };
    string? capturedSessionId = null;

    _stripePaymentService
        .Setup(s => s.CreateEventCheckoutSessionAsync(It.IsAny<CreateEventCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result<string>.Success("cs_test_123"))
        .Callback<CreateEventCheckoutSessionRequest, CancellationToken>((req, ct) => capturedSessionId = "cs_test_123");

    _unitOfWork
        .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Database error"));

    // Act
    await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));

    // Assert
    _stripePaymentService.Verify(
        s => s.CancelCheckoutSessionAsync("cs_test_123", It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Integration Tests

**File**: `PaymentFlowIntegrationTests.cs`

```csharp
[Fact]
public async Task EndToEnd_UserRegistersForPaidEvent_ShouldCompleteSuccessfully()
{
    // Arrange
    var eventId = await CreatePaidEventAsync();
    var userId = await CreateUserAsync();

    // Act
    var rsvpResponse = await _client.PostAsJsonAsync($"/api/events/{eventId}/rsvp", new
    {
        UserId = userId,
        Attendees = new[] { new { Name = "John Doe", Age = 30 } },
        Email = "john@example.com",
        PhoneNumber = "+1234567890",
        SuccessUrl = "http://localhost/success",
        CancelUrl = "http://localhost/cancel"
    });

    // Assert registration created
    rsvpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var checkoutUrl = await rsvpResponse.Content.ReadAsStringAsync();
    checkoutUrl.Should().StartWith("https://checkout.stripe.com");

    // Verify registration exists in database
    var registration = await _dbContext.Registrations
        .Where(r => r.UserId == userId && r.EventId == eventId)
        .FirstOrDefaultAsync();

    registration.Should().NotBeNull();
    registration.Status.Should().Be(RegistrationStatus.Pending);
    registration.PaymentStatus.Should().Be(PaymentStatus.Pending);
    registration.StripeCheckoutSessionId.Should().NotBeNullOrEmpty();

    // Simulate Stripe webhook
    var webhookPayload = CreateCheckoutSessionCompletedWebhook(registration.StripeCheckoutSessionId);
    var webhookResponse = await _client.PostAsync("/api/payments/webhook", webhookPayload);

    webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify payment completed
    await _dbContext.Entry(registration).ReloadAsync();
    registration.Status.Should().Be(RegistrationStatus.Confirmed);
    registration.PaymentStatus.Should().Be(PaymentStatus.Completed);
}
```

---

## Deployment Strategy

### Phase 1: Immediate Hotfix (Deploy within 24 hours)
1. **Fix 1**: Reorder Stripe call AFTER first commit
2. **Fix 3**: Webhook throws exceptions instead of silent returns
3. Deploy to production with zero-downtime deployment
4. Monitor logs for 48 hours

### Phase 2: Monitoring (Deploy within 1 week)
1. **Fix 4**: Orphaned payment detection service
2. **Fix 5**: Expired registration cleanup
3. Add alerting to Slack/email
4. Deploy as background services

### Phase 3: Robustness (Deploy within 2 weeks)
1. **Fix 2**: Compensating transactions
2. Add circuit breaker for Stripe calls
3. Add retry policies with exponential backoff
4. Comprehensive integration tests

---

## Success Criteria

### Immediate
- ✅ No new orphaned payments occur
- ✅ All webhooks either succeed or retry (no silent failures)
- ✅ Affected customer receives refund or registration

### Long-term
- ✅ Orphaned payment detection running hourly
- ✅ Zero orphaned payments detected for 30 days
- ✅ Test coverage >90% for payment flows
- ✅ Alerting configured for all payment failures

---

## Lessons Learned

1. **Never make external API calls inside database transactions**
   - Commit local state first
   - Then call external APIs
   - Use compensating transactions if external call fails

2. **Always throw exceptions on critical failures**
   - Silent returns hide problems
   - Exceptions trigger retries
   - Alerts must be sent for customer-impacting errors

3. **Add monitoring for critical business flows**
   - Payment flows need real-time monitoring
   - Automated detection of orphaned payments
   - Alerting to ops team

4. **Test failure scenarios**
   - Happy path tests are not enough
   - Test what happens when Stripe fails
   - Test what happens when database fails
   - Test transaction rollback scenarios

---

## Appendix: Files to Modify

### Application Layer
- `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs`
- `src/LankaConnect.Application/Events/Commands/RegisterAnonymousAttendee/RegisterAnonymousAttendeeCommandHandler.cs`

### API Layer
- `src/LankaConnect.API/Controllers/PaymentsController.cs`

### Infrastructure Layer
- `src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs` (add CancelCheckoutSessionAsync)

### New Files (Monitoring)
- `src/LankaConnect.Application/Common/Services/OrphanedPaymentDetectionService.cs`
- `src/LankaConnect.Infrastructure/BackgroundJobs/OrphanedPaymentDetectionJob.cs`
- `src/LankaConnect.Infrastructure/BackgroundJobs/ExpiredRegistrationCleanupJob.cs`

### Tests
- `tests/LankaConnect.Application.Tests/Events/Commands/RsvpToEventCommandHandlerTests.cs`
- `tests/LankaConnect.API.Tests/Integration/PaymentFlowIntegrationTests.cs`

---

**Next Steps**: Implement Fix 1 and Fix 3 immediately as hotfix, then proceed with monitoring and robustness improvements.
