# Payment Orphaning Bug - Fix Implementation Plan

**Status**: CRITICAL - Immediate Action Required
**Created**: 2025-12-18
**Priority**: P0 (Customer Impact)

---

## Problem Summary

User paid for event registration via Stripe, but registration never existed in database. Root cause: **Database transaction rollback AFTER Stripe checkout session creation**.

**See Full Analysis**: [RCA_ORPHANED_STRIPE_PAYMENT.md](./RCA_ORPHANED_STRIPE_PAYMENT.md)

---

## Immediate Actions (Before Code Fix)

### 1. Recover Affected Customer

**Option A: Manual Registration (If event hasn't occurred)**
```bash
# Step 1: Get checkout session details from Stripe CLI
stripe checkout sessions retrieve cs_test_a1GJDLaIOJeGp1IUiA54Yv2uXmBZrou3bor1iANb25zissulk

# Step 2: Extract metadata
# - event_id
# - user_id (if present)
# - amount_paid
# - payment_intent_id

# Step 3: Run database script to create registration
# See RCA document Section: "Recovery Strategy - Option A"

# Step 4: Manually trigger ticket email
# Call ResendTicketEmailCommand API endpoint
```

**Option B: Issue Refund (If event occurred or data incomplete)**
```bash
stripe refunds create \
  --payment-intent=<payment_intent_id> \
  --reason=requested_by_customer \
  --metadata[reason]="Registration failed - system error"
```

---

## Code Fixes

### Fix 1: Transaction Order Fix (HIGHEST PRIORITY)
**File**: `src/LankaConnect.Application/Events/Commands/RsvpToEvent/RsvpToEventCommandHandler.cs`
**Lines**: 60-150
**Estimated Time**: 2 hours (including testing)

**Current (Broken) Flow**:
```
1. Create registration in memory
2. Call Stripe API (EXTERNAL - can fail)
3. Commit database transaction
4. If #2 fails → rollback → orphaned payment
```

**Fixed Flow**:
```
1. Create registration in memory
2. Commit database transaction (registration persisted)
3. Call Stripe API
4. If #3 fails → cancel registration + return error (no orphan)
5. Update registration with checkout session ID
6. Commit again (just session ID)
```

**Implementation**:
```csharp
// BEFORE:
var registerResult = @event.RegisterWithAttendees(userId, attendeeDetailsList, contactResult.Value);
_eventRepository.Update(@event);
var registration = @event.Registrations.Last();

if (!@event.IsFree())
{
    var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);
    if (checkoutResult.IsFailure)
        return Result<string?>.Failure(...);

    registration.SetStripeCheckoutSession(checkoutResult.Value);
    await _unitOfWork.CommitAsync(cancellationToken); // ← First commit
}

// AFTER:
var registerResult = @event.RegisterWithAttendees(userId, attendeeDetailsList, contactResult.Value);
_eventRepository.Update(@event);
var registration = @event.Registrations.Last();

// COMMIT FIRST (before external Stripe call)
await _unitOfWork.CommitAsync(cancellationToken);

if (!@event.IsFree())
{
    var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);
    if (checkoutResult.IsFailure)
    {
        // Registration already persisted - cancel it
        registration.Cancel();
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<string?>.Failure($"Payment session creation failed: {checkoutResult.Error}");
    }

    registration.SetStripeCheckoutSession(checkoutResult.Value);
    await _unitOfWork.CommitAsync(cancellationToken); // ← Second commit (session ID only)
    return Result<string?>.Success(checkoutResult.Value);
}
```

**Testing**:
- Unit test: Stripe call fails → registration should be cancelled (not orphaned)
- Integration test: End-to-end flow with Stripe test mode
- Manual test: Register for paid event, verify registration exists before Stripe redirect

---

### Fix 2: Webhook Error Handling (HIGHEST PRIORITY)
**File**: `src/LankaConnect.API/Controllers/PaymentsController.cs`
**Lines**: 300-386
**Estimated Time**: 1 hour

**Current (Broken) Behavior**:
```csharp
if (registration == null)
{
    _logger.LogError("Registration {RegistrationId} not found", registrationId);
    return; // ← SILENT FAILURE - webhook marked as processed
}
```

**Fixed Behavior**:
```csharp
if (registration == null)
{
    _logger.LogError("CRITICAL: Registration {RegistrationId} not found for session {SessionId} - ORPHANED PAYMENT",
        registrationId, session.Id);

    // TODO: Send alert to ops team
    // await _alertService.SendCriticalAlertAsync("Orphaned Payment Detected", ...);

    // Throw exception to trigger Stripe retry
    throw new InvalidOperationException(
        $"Registration {registrationId} not found for checkout session {session.Id}. " +
        $"This is an orphaned payment requiring manual intervention.");
}
```

**Why This Helps**:
- HTTP 500 response → Stripe retries webhook (up to 3 days)
- Gives time to manually create registration
- Logs visible error for investigation

**Testing**:
- Unit test: Missing registration → should throw exception
- Integration test: Webhook with invalid registration ID → HTTP 500
- Manual test: Send test webhook with fake registration ID

---

### Fix 3: Add Cleanup Job for Expired Pending Registrations
**New File**: `src/LankaConnect.Infrastructure/BackgroundJobs/ExpiredRegistrationCleanupJob.cs`
**Estimated Time**: 2 hours

**Purpose**: Cancel registrations stuck in Pending status for >24 hours

```csharp
public class ExpiredRegistrationCleanupJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredRegistrationCleanupJob> _logger;

    public ExpiredRegistrationCleanupJob(
        IServiceProvider serviceProvider,
        ILogger<ExpiredRegistrationCleanupJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Find registrations pending payment for more than 24 hours
                var cutoffTime = DateTime.UtcNow.AddHours(-24);
                var expiredRegistrations = await context.Registrations
                    .Where(r => r.Status == RegistrationStatus.Pending &&
                               r.PaymentStatus == PaymentStatus.Pending &&
                               r.CreatedAt < cutoffTime)
                    .ToListAsync(stoppingToken);

                if (expiredRegistrations.Any())
                {
                    _logger.LogWarning("Found {Count} expired pending registrations", expiredRegistrations.Count);

                    foreach (var registration in expiredRegistrations)
                    {
                        _logger.LogInformation("Cancelling expired registration {RegistrationId}", registration.Id);
                        registration.Cancel();
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }

                // Run every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in expired registration cleanup job");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 min on error
            }
        }
    }
}
```

**Register in Program.cs**:
```csharp
builder.Services.AddHostedService<ExpiredRegistrationCleanupJob>();
```

**Testing**:
- Create registration with CreatedAt = 25 hours ago, Status = Pending
- Run job
- Verify registration status changed to Cancelled

---

### Fix 4: Add Orphaned Payment Detection (MEDIUM PRIORITY)
**New File**: `src/LankaConnect.Infrastructure/BackgroundJobs/OrphanedPaymentDetectionJob.cs`
**Estimated Time**: 4 hours

**Purpose**: Detect payments in Stripe that have no corresponding registration in database

```csharp
public class OrphanedPaymentDetectionJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IStripeClient _stripeClient;
    private readonly ILogger<OrphanedPaymentDetectionJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get paid checkout sessions from last 7 days
                var sessionService = new Stripe.Checkout.SessionService(_stripeClient);
                var sessions = await sessionService.ListAsync(new SessionListOptions
                {
                    Created = new DateRangeOptions
                    {
                        GreaterThanOrEqual = DateTime.UtcNow.AddDays(-7)
                    },
                    Limit = 100
                }, cancellationToken: stoppingToken);

                var orphanedCount = 0;

                foreach (var session in sessions.Data)
                {
                    // Skip unpaid sessions
                    if (session.PaymentStatus != "paid")
                        continue;

                    // Check if registration exists
                    if (!session.Metadata.TryGetValue("registration_id", out var regIdStr) ||
                        !Guid.TryParse(regIdStr, out var regId))
                    {
                        _logger.LogWarning("ORPHANED PAYMENT: Session {SessionId} missing registration_id", session.Id);
                        orphanedCount++;
                        continue;
                    }

                    // Check database
                    var registrationExists = await context.Registrations
                        .AnyAsync(r => r.Id == regId, stoppingToken);

                    if (!registrationExists)
                    {
                        _logger.LogError(
                            "ORPHANED PAYMENT: Session {SessionId}, Registration {RegistrationId} not found. Amount: {Amount} {Currency}",
                            session.Id, regId, session.AmountTotal, session.Currency);
                        orphanedCount++;

                        // TODO: Send alert
                        // await _alertService.SendCriticalAlertAsync(...);
                    }
                }

                if (orphanedCount > 0)
                {
                    _logger.LogError("CRITICAL: {Count} orphaned payments detected", orphanedCount);
                }

                // Run every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in orphaned payment detection job");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
```

---

### Fix 5: Add Compensating Transaction (LOW PRIORITY - FUTURE)
**File**: `src/LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs`
**Estimated Time**: 3 hours

**Purpose**: Cancel Stripe checkout session if database commit fails after session creation

**Add Method**:
```csharp
public async Task<Result> CancelCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default)
{
    try
    {
        var sessionService = new SessionService(_stripeClient);
        await sessionService.ExpireAsync(sessionId, cancellationToken: cancellationToken);

        _logger.LogInformation("Expired Stripe checkout session {SessionId}", sessionId);
        return Result.Success();
    }
    catch (StripeException ex)
    {
        _logger.LogError(ex, "Failed to expire Stripe checkout session {SessionId}", sessionId);
        return Result.Failure($"Failed to cancel checkout session: {ex.Message}");
    }
}
```

**Use in RsvpToEventCommandHandler**:
```csharp
string? checkoutSessionId = null;

try
{
    var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(...);
    checkoutSessionId = checkoutResult.Value;

    registration.SetStripeCheckoutSession(checkoutSessionId);
    await _unitOfWork.CommitAsync(cancellationToken);

    return Result<string?>.Success(checkoutSessionId);
}
catch (Exception)
{
    // Compensating transaction: Cancel Stripe session
    if (checkoutSessionId != null)
        await _stripePaymentService.CancelCheckoutSessionAsync(checkoutSessionId);

    throw;
}
```

---

## Testing Strategy

### Unit Tests (Required for Hotfix)
**File**: `tests/LankaConnect.Application.Tests/Events/Commands/RsvpToEventCommandHandlerTests.cs`

1. **Test: Stripe call fails after registration created**
   - Verify registration is cancelled (not orphaned)
   - Verify error returned to user

2. **Test: Database commit fails after Stripe call**
   - Verify Stripe session is cancelled
   - Verify exception is thrown

3. **Test: Happy path - paid event registration**
   - Verify registration committed before Stripe call
   - Verify checkout URL returned

### Integration Tests (Required for Hotfix)
**File**: `tests/LankaConnect.API.Tests/Integration/PaymentFlowIntegrationTests.cs`

1. **Test: End-to-end paid event registration**
   - Register for event
   - Verify registration exists in DB with Status=Pending
   - Simulate webhook
   - Verify registration updated to Confirmed

2. **Test: Webhook with missing registration**
   - Send webhook for non-existent registration
   - Verify HTTP 500 response
   - Verify error logged

### Manual Testing (Required Before Deploy)

1. **Scenario 1: Successful paid registration**
   - Register for paid event on staging
   - Verify registration exists in DB before Stripe redirect
   - Complete payment on Stripe
   - Verify webhook updates registration
   - Verify email sent

2. **Scenario 2: Stripe API failure**
   - Mock Stripe API to return error
   - Register for paid event
   - Verify registration cancelled
   - Verify error message shown to user

3. **Scenario 3: Webhook retry**
   - Delete registration from DB after creation
   - Send webhook
   - Verify HTTP 500 response
   - Check Stripe dashboard for retry attempts

---

## Deployment Plan

### Phase 1: Immediate Hotfix (Deploy Today)
**Duration**: 4 hours (code + test)
**Deployment Window**: Off-peak hours (2 AM - 4 AM)

**Files Changed**:
1. `RsvpToEventCommandHandler.cs` (Fix 1)
2. `PaymentsController.cs` (Fix 2)

**Deployment Steps**:
```bash
# 1. Create hotfix branch
git checkout -b hotfix/payment-orphaning-fix develop

# 2. Implement Fix 1 and Fix 2
# ... code changes ...

# 3. Run tests
dotnet test

# 4. Build
dotnet build --configuration Release

# 5. Deploy to staging
# ... deployment commands ...

# 6. Manual testing on staging
# ... test scenarios ...

# 7. Deploy to production
# ... deployment commands ...

# 8. Monitor logs for 2 hours
tail -f /var/log/lankaconnect/api.log | grep -i payment

# 9. Merge to develop and master
git checkout develop
git merge hotfix/payment-orphaning-fix
git checkout master
git merge hotfix/payment-orphaning-fix
git push origin develop master
```

**Rollback Plan**:
```bash
# If issues detected, rollback to previous version
# ... rollback commands ...
```

### Phase 2: Monitoring (Deploy Week of 2025-12-23)
**Duration**: 1 day (code + test)

**Files Added**:
1. `ExpiredRegistrationCleanupJob.cs` (Fix 3)
2. `OrphanedPaymentDetectionJob.cs` (Fix 4)

**Deployment**: Normal deployment process

### Phase 3: Robustness (Deploy Week of 2026-01-06)
**Duration**: 2 days (code + test)

**Files Changed**:
1. `StripePaymentService.cs` (Fix 5)
2. `RsvpToEventCommandHandler.cs` (Use Fix 5)

**Deployment**: Normal deployment process

---

## Monitoring & Alerting

### Log Queries

**Check for orphaned payments** (run hourly):
```bash
# Search application logs for orphaned payment errors
grep -i "ORPHANED PAYMENT" /var/log/lankaconnect/api.log
```

**Database query** (run daily):
```sql
-- Find registrations stuck in Pending for >24 hours
SELECT
    r."Id",
    r."EventId",
    r."CreatedAt",
    r."Status",
    r."PaymentStatus",
    r."StripeCheckoutSessionId"
FROM events.registrations r
WHERE r."PaymentStatus" = 1  -- Pending
  AND r."CreatedAt" < NOW() - INTERVAL '24 hours'
ORDER BY r."CreatedAt" DESC;
```

### Alerts to Add (Future)

1. **Critical Alert**: Orphaned payment detected
   - Trigger: OrphanedPaymentDetectionJob finds orphaned payment
   - Channel: Email + Slack
   - Recipients: Engineering team + Customer support

2. **Warning Alert**: Stripe API failure rate >5%
   - Trigger: Multiple Stripe API failures in 1 hour
   - Channel: Slack
   - Recipients: Engineering team

3. **Info Alert**: Expired registrations cleaned up
   - Trigger: ExpiredRegistrationCleanupJob cancels registrations
   - Channel: Slack
   - Recipients: Engineering team

---

## Success Metrics

### Immediate (After Hotfix)
- ✅ Zero new orphaned payments detected
- ✅ All webhooks either succeed or return HTTP 500 (triggering retry)
- ✅ Affected customer receives refund or registration

### Short-term (1 week after hotfix)
- ✅ Monitoring jobs running without errors
- ✅ No orphaned payments detected in last 7 days
- ✅ Expired registration cleanup running successfully

### Long-term (1 month after hotfix)
- ✅ Zero orphaned payments for 30 days
- ✅ Test coverage >90% for payment flows
- ✅ Alerting configured and tested
- ✅ Documentation updated

---

## Risk Assessment

### Deployment Risks
- **Low Risk**: Transaction order fix is straightforward
- **Low Risk**: Webhook error handling is non-breaking change
- **Medium Risk**: Background jobs could have performance impact

### Mitigation
- Deploy hotfix during off-peak hours
- Monitor CPU/memory after background job deployment
- Have rollback plan ready

### Customer Impact
- **Current**: Customers can lose money (pay but get nothing)
- **After Hotfix**: Extremely low risk of payment issues
- **After Monitoring**: Proactive detection of any issues

---

## Next Steps

1. **Immediate** (Today):
   - [ ] Recover affected customer (refund or manual registration)
   - [ ] Implement Fix 1 and Fix 2
   - [ ] Write unit tests
   - [ ] Deploy to staging
   - [ ] Manual testing

2. **This Week**:
   - [ ] Deploy hotfix to production
   - [ ] Monitor logs for 48 hours
   - [ ] Implement Fix 3 and Fix 4
   - [ ] Deploy monitoring jobs

3. **Next 2 Weeks**:
   - [ ] Implement Fix 5 (compensating transactions)
   - [ ] Add alerting infrastructure
   - [ ] Write integration tests
   - [ ] Update documentation

---

## Documentation

- **Full RCA**: [RCA_ORPHANED_STRIPE_PAYMENT.md](./RCA_ORPHANED_STRIPE_PAYMENT.md)
- **Architecture Decision**: Commit local state before external API calls
- **Runbook**: How to handle orphaned payments manually

---

**Owner**: Engineering Team
**Reviewers**: Tech Lead, Product Manager
**Status**: Ready for Implementation
