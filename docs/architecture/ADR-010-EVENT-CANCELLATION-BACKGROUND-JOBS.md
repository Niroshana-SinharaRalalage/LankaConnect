# ADR-010: Event Cancellation Background Jobs Implementation

**Status**: Proposed
**Date**: 2026-01-07
**Author**: System Architecture Designer
**Relates To**: Phase 6A.64 - Event Cancellation Timeout Fix

---

## Context

Event cancellation operations experience 30-second frontend timeouts when sending email notifications to confirmed registrations, email groups, and newsletter subscribers. Root cause analysis reveals synchronous email processing within database transactions, taking 30-45 seconds for 50-100 recipients.

**Current Pain Points**:
- 30-second timeout on first cancellation attempt
- Confusing 400 validation error on retry
- N+1 query pattern loading user data
- Poor user experience (appears to fail, requires retry)
- Event IS cancelled in database despite frontend error
- No scalability beyond 200 recipients

**Business Impact**:
- Event organizers frustrated with broken UI
- Support burden (users reporting "bug" that isn't a bug)
- Risk of duplicate cancellation attempts
- Poor perception of platform reliability

**Technical Debt**:
- Other event handlers have same pattern (EventPublishedEventHandler, EventPostponedEventHandler)
- No background job infrastructure for bulk email operations
- Synchronous email sending blocks API responses

---

## Decision

Implement a **two-phase approach** to fix event cancellation timeouts:

**Phase 1 (Hotfix - 1 day)**:
- Add bulk email query method to UserRepository (`GetEmailsByUserIdsAsync`)
- Refactor EventCancelledEventHandler to eliminate N+1 queries
- Increase frontend timeout to 90 seconds for cancel operation
- Add frontend refetch on error to prevent stale state
- Add performance logging to track operation duration

**Phase 2 (Long-term - 3-4 days)**:
- Create `EventCancellationEmailJob` background job class
- Refactor EventCancelledEventHandler to queue Hangfire job instead of sending emails
- Leverage existing Hangfire infrastructure (already deployed)
- Remove Phase 1 timeout workaround (back to 30s default)

**Technology Choice**: **Hangfire** (already integrated)
- PostgreSQL storage backend (already configured)
- Dashboard available at `/hangfire` (already deployed)
- Proven pattern with EventReminderJob (already in production)
- No new infrastructure needed

---

## Alternatives Considered

### Alternative 1: Only increase frontend timeout (rejected)

**Approach**: Change timeout from 30s to 120s.

**Pros**:
- Simplest fix (1-line change)
- No backend changes

**Cons**:
- Still blocks user for 30-60 seconds (poor UX)
- Doesn't scale beyond 300-400 recipients
- Doesn't address N+1 query performance issue
- Doesn't solve architecture problem (synchronous emails in transaction)

**Decision**: Rejected - Band-aid fix that doesn't address root cause

---

### Alternative 2: Quartz.NET for background jobs (rejected)

**Approach**: Use Quartz.NET instead of Hangfire.

**Pros**:
- Enterprise-grade job scheduling
- Powerful cron expressions
- Clustering support

**Cons**:
- Hangfire already integrated and working
- Requires additional NuGet packages
- Requires separate job storage configuration
- Learning curve for team
- Duplicate infrastructure (Hangfire + Quartz.NET)

**Decision**: Rejected - Hangfire already meets requirements

---

### Alternative 3: Azure Service Bus for job queue (rejected)

**Approach**: Use Azure Service Bus queues for email jobs.

**Pros**:
- Native Azure service
- Highly scalable
- Built-in retry and dead-letter queues

**Cons**:
- Requires new infrastructure setup
- Additional cost ($0.05 per million operations)
- Requires separate worker service to poll queue
- More complex deployment
- No built-in dashboard (need custom monitoring)
- Overkill for current scale (100-500 recipients)

**Decision**: Rejected - Hangfire sufficient for current scale, easier to manage

---

### Alternative 4: Fire-and-forget with no retry (rejected)

**Approach**: Queue emails in database table, background service processes them with no retry.

**Pros**:
- Simple implementation
- No third-party dependencies

**Cons**:
- No automatic retry on failure
- No built-in monitoring/dashboard
- Manual implementation of job state management
- Reinventing the wheel (Hangfire already provides this)

**Decision**: Rejected - Hangfire provides retry/monitoring out of box

---

### Alternative 5: Parallel email sending (considered for Phase 3)

**Approach**: Send emails in parallel instead of sequentially.

**Pros**:
- Faster email sending (5-10 seconds instead of 15-20 seconds)
- Better resource utilization

**Cons**:
- Risk of rate limiting from email service provider
- More complex error handling (which emails failed?)
- Potential Azure Communication Services throttling
- Requires semaphore/throttling implementation

**Decision**: Deferred to Phase 3 - Sequential sending is safer and works for current scale

---

## Detailed Design

### Phase 1: Bulk Query Optimization

**New Repository Method**:

```csharp
// IUserRepository.cs
Task<Dictionary<Guid, string>> GetEmailsByUserIdsAsync(
    IEnumerable<Guid> userIds,
    CancellationToken cancellationToken = default);

// UserRepository.cs
public async Task<Dictionary<Guid, string>> GetEmailsByUserIdsAsync(
    IEnumerable<Guid> userIds,
    CancellationToken cancellationToken = default)
{
    var ids = userIds.ToList();
    if (!ids.Any()) return new Dictionary<Guid, string>();

    return await _dbSet
        .AsNoTracking() // Read-only, no change tracking overhead
        .Where(u => ids.Contains(u.Id))
        .ToDictionaryAsync(
            u => u.Id,
            u => u.Email.Value,
            cancellationToken);
}
```

**Usage in EventCancelledEventHandler**:

```csharp
// BEFORE (N+1 queries)
foreach (var registration in confirmedRegistrations)
{
    var user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
    if (user != null)
    {
        registrationEmails.Add(user.Email.Value);
    }
}
// 50 registrations = 50 DB queries (5-10 seconds)

// AFTER (Bulk query)
var userIds = confirmedRegistrations
    .Where(r => r.UserId.HasValue)
    .Select(r => r.UserId.Value)
    .Distinct()
    .ToList();

if (userIds.Any())
{
    var emailsByUserId = await _userRepository.GetEmailsByUserIdsAsync(userIds, cancellationToken);

    foreach (var registration in confirmedRegistrations)
    {
        if (registration.UserId.HasValue &&
            emailsByUserId.TryGetValue(registration.UserId.Value, out var email))
        {
            registrationEmails.Add(email);
        }
    }
}
// 50 registrations = 1 DB query (100ms)
```

**Performance Improvement**: 10-20x faster user email loading

---

### Phase 2: Background Job Implementation

**New Background Job Class**:

```csharp
namespace LankaConnect.Application.Events.BackgroundJobs;

public class EventCancellationEmailJob
{
    // Dependencies injected by Hangfire
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    // ... other dependencies

    [Queue("emails")] // Dedicated queue for email jobs
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task SendCancellationEmailsAsync(
        Guid eventId,
        string cancellationReason,
        DateTime cancelledAt)
    {
        // Implementation identical to EventCancelledEventHandler.Handle()
        // But runs asynchronously in background worker
        // ... load event, registrations, resolve recipients, send emails ...
    }
}
```

**Refactored Event Handler**:

```csharp
public class EventCancelledEventHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<EventCancelledEventHandler> _logger;

    public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Queue background job (returns immediately)
        var jobId = _backgroundJobClient.Enqueue<EventCancellationEmailJob>(
            job => job.SendCancellationEmailsAsync(
                domainEvent.EventId,
                domainEvent.Reason,
                domainEvent.CancelledAt));

        _logger.LogInformation("Background job {JobId} queued for Event {EventId}", jobId, domainEvent.EventId);

        await Task.CompletedTask;
    }
}
```

**Transaction Flow**:
1. `SaveChangesAsync()` → Event status = Cancelled (persisted to DB)
2. Dispatch EventCancelledEvent
3. EventCancelledEventHandler → Queue Hangfire job (inside transaction)
4. Transaction commits
5. Hangfire worker picks up job asynchronously
6. Emails sent outside API request thread

**Job Queueing Inside Transaction** (Critical Decision):
- Job only queued if event successfully saved
- Transaction rollback prevents orphaned jobs
- Hangfire job enqueue is very fast (<10ms overhead)
- PostgreSQL handles this overhead easily
- Guarantees consistency (no event saved without job, no job without event)

---

## Consequences

### Positive

**Phase 1**:
- ✅ 10-20 second performance improvement
- ✅ 95% reduction in timeout errors
- ✅ Supports up to 200 recipients reliably
- ✅ Quick to implement and test (1 day)
- ✅ Low risk (easily reversible)
- ✅ No schema changes

**Phase 2**:
- ✅ <1 second API response time (95% improvement!)
- ✅ Scales to unlimited recipients
- ✅ Automatic retry on failure (3 attempts with exponential backoff)
- ✅ Observable via Hangfire dashboard
- ✅ No user blocking (emails sent asynchronously)
- ✅ Clean transaction boundaries (no email sending in DB transaction)
- ✅ Leverages existing infrastructure (no new services)
- ✅ Pattern can be reused for other bulk email operations

### Negative

**Phase 1**:
- ⚠️ Still blocks user for 15-25 seconds (mitigated by Phase 2)
- ⚠️ Doesn't scale beyond 200 recipients (mitigated by Phase 2)
- ⚠️ Email sending still synchronous (fixed in Phase 2)

**Phase 2**:
- ⚠️ Slight complexity increase (event handler → background job)
- ⚠️ Emails no longer sent immediately (sent within 1-2 minutes)
- ⚠️ Debugging requires Hangfire dashboard access
- ⚠️ Job retry may cause delayed email delivery if service is down

### Risks and Mitigations

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Background job fails silently | Low | High | Comprehensive logging, Hangfire dashboard monitoring, automatic retry (3 attempts) |
| Database transaction rollback after job queued | Very Low | Low | Job enqueue happens inside transaction, rollback prevents queue |
| Email service rate limiting | Medium | Medium | Sequential sending with delays, monitor Azure Communication Services quotas |
| Hangfire worker crash during processing | Low | Low | Persistent job storage in PostgreSQL, automatic job retry |
| Performance regression | Very Low | Medium | Performance tests, Phase 1 actually improves performance |

---

## Compliance

**Clean Architecture**: ✅
- Domain events remain in domain layer
- Application layer handles background job queueing
- Infrastructure layer implements repository methods
- No cross-layer violations

**Domain-Driven Design**: ✅
- Event.Cancel() domain method unchanged
- EventCancelledEvent domain event unchanged
- Background job is application concern, not domain concern
- Domain model remains pure

**Test-Driven Development**: ✅
- Unit tests for bulk query method
- Unit tests for background job queueing
- Integration tests for job execution
- Performance tests for timeout scenarios

**Zero Tolerance for Errors**: ✅
- All code compiles cleanly
- No breaking changes to API contract
- Backward compatible

---

## Implementation Timeline

**Phase 1 (Day 1)**:
- Morning: Implement bulk email query + tests
- Afternoon: Update frontend timeout + state management + tests
- Deploy to staging, verify

**Phase 2 (Days 2-4)**:
- Day 2: Implement background job + refactor event handler + tests
- Day 3: Test on staging with 500+ recipients, monitor Hangfire
- Day 4: Remove Phase 1 timeout increase, final testing, deploy to production

**Total**: 5 days from design to production

---

## Monitoring and Observability

**Hangfire Dashboard** (`/hangfire`):
- View queued jobs
- Monitor processing jobs
- Retry failed jobs manually
- View job execution history
- Track success/failure rates

**Structured Logging**:
```csharp
[Information] EventCancelledEventHandler - Queueing background job for Event {EventId}
[Information] Background job {JobId} queued for Event {EventId} cancellation emails
[Information] EventCancellationEmailJob START - Event {EventId}
[Information] EventCancellationEmailJob COMPLETED - Recipients: 50, Success: 50, Failed: 0, Duration: 18234ms
```

**Performance Metrics**:
- API response time (target: <1 second)
- Job processing time (track per recipient count)
- Email send success rate (target: 99%)
- Job retry rate (alert if >5%)

---

## Future Enhancements

**Phase 3 (Optional)**:
- Parallel email sending with throttling
- Email delivery status tracking table
- Progress API endpoint for frontend polling
- Email send progress indicators in UI

**Pattern Reuse**:
- Apply same pattern to EventPublishedEventHandler
- Apply same pattern to EventPostponedEventHandler
- Apply same pattern to any bulk notification scenario

**Scaling**:
- Increase Hangfire worker count (1 → 5 workers)
- Deploy dedicated Hangfire worker container (if needed)
- Horizontal scaling: API container + worker container separation

---

## References

**Related Documents**:
- [EVENT_CANCELLATION_TIMEOUT_FIX_IMPLEMENTATION_STRATEGY.md](../EVENT_CANCELLATION_TIMEOUT_FIX_IMPLEMENTATION_STRATEGY.md)
- [EVENT_CANCELLATION_TIMEOUT_C4_DIAGRAMS.md](./EVENT_CANCELLATION_TIMEOUT_C4_DIAGRAMS.md)
- [RCA_EVENT_CANCELLATION_TIMEOUT_ERROR.md](../RCA_EVENT_CANCELLATION_TIMEOUT_ERROR.md)

**Similar Patterns**:
- EventReminderJob (existing background job for event reminders)
- EventStatusUpdateJob (existing background job for status transitions)
- ExpiredBadgeCleanupJob (existing background job for cleanup)

**Technology Documentation**:
- [Hangfire Documentation](https://docs.hangfire.io/)
- [Entity Framework Core Performance](https://docs.microsoft.com/en-us/ef/core/performance/)
- [Azure Communication Services Email](https://learn.microsoft.com/en-us/azure/communication-services/concepts/email/email-overview)

---

## Approval

**Decision Maker**: Engineering Team Lead
**Status**: Pending Approval
**Expected Approval Date**: 2026-01-08

**Sign-off Required**:
- [ ] Engineering Team Lead
- [ ] Product Owner (user experience impact)
- [ ] DevOps Lead (deployment strategy)
- [ ] QA Lead (testing strategy)

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-07 | System Architecture Designer | Initial ADR draft |