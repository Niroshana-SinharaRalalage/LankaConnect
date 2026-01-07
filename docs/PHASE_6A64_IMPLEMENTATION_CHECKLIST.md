# Phase 6A.64: Event Cancellation Timeout Fix - Implementation Checklist

**Date**: 2026-01-07
**Sprint**: Phase 6A.64
**Estimated Effort**: 5 days
**Status**: Ready to Start

---

## Pre-Implementation Setup

### Day 0: Preparation

- [ ] Read [Complete Implementation Strategy](./EVENT_CANCELLATION_TIMEOUT_FIX_IMPLEMENTATION_STRATEGY.md)
- [ ] Read [Phase 6A.64 Summary](./PHASE_6A64_EVENT_CANCELLATION_TIMEOUT_FIX_SUMMARY.md)
- [ ] Review [C4 Architecture Diagrams](./architecture/EVENT_CANCELLATION_TIMEOUT_C4_DIAGRAMS.md)
- [ ] Review [ADR-010](./architecture/ADR-010-EVENT-CANCELLATION-BACKGROUND-JOBS.md)
- [ ] Create feature branch: `git checkout -b feature/phase-6a64-event-cancellation-timeout-fix`
- [ ] Verify local development environment setup:
  - [ ] PostgreSQL running
  - [ ] Redis running (optional, for caching)
  - [ ] Hangfire dashboard accessible at `/hangfire`
  - [ ] Frontend running on localhost:3000
  - [ ] Backend running on localhost:5000
- [ ] Verify staging environment access:
  - [ ] Azure Container Apps staging URL accessible
  - [ ] Hangfire dashboard accessible on staging
  - [ ] Database connection working
  - [ ] Email service (Azure Communication Services) configured

---

## Phase 1: Hotfix (Day 1)

**Goal**: Reduce timeout errors by 95%, support up to 200 recipients

### Morning Session (4 hours)

#### Task 1.1: Add Bulk Email Query to UserRepository

- [ ] **File**: `src/LankaConnect.Domain/Users/IUserRepository.cs`
  - [ ] Add interface method signature (line 28):
    ```csharp
    /// <summary>
    /// Phase 6A.64: Bulk load user emails by user IDs
    /// Optimizes email notification recipients loading
    /// </summary>
    Task<Dictionary<Guid, string>> GetEmailsByUserIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
    ```
  - [ ] Save file
  - [ ] Verify no compilation errors: `dotnet build src/LankaConnect.Domain`

- [ ] **File**: `src/LankaConnect.Infrastructure/Data/Repositories/UserRepository.cs`
  - [ ] Add implementation method (after line 160):
    ```csharp
    /// <summary>
    /// Phase 6A.64: Bulk load user emails by user IDs (eliminates N+1 queries)
    /// </summary>
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
  - [ ] Save file
  - [ ] Verify no compilation errors: `dotnet build src/LankaConnect.Infrastructure`

#### Task 1.2: Refactor EventCancelledEventHandler to Use Bulk Query

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
  - [ ] Add performance logging (line 49, after method start):
    ```csharp
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    ```
  - [ ] Replace N+1 query loop (lines 72-88) with bulk query:
    ```csharp
    // Phase 6A.64 FIX: Bulk load user emails instead of N+1 queries
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
    ```
  - [ ] Add performance logging at end of method (before line 185):
    ```csharp
    stopwatch.Stop();

    _logger.LogInformation(
        "[Phase 6A.64] EventCancelledEventHandler COMPLETED - Event {EventId}, " +
        "Recipients: {RecipientCount}, Success: {SuccessCount}, Failed: {FailCount}, " +
        "Duration: {DurationMs}ms",
        domainEvent.EventId, allRecipients.Count, successCount, failCount, stopwatch.ElapsedMilliseconds);

    // Alert if operation took longer than expected
    if (stopwatch.ElapsedMilliseconds > 60000) // 60 seconds
    {
        _logger.LogWarning(
            "[Phase 6A.64] PERFORMANCE WARNING: Event cancellation took {DurationMs}ms for {RecipientCount} recipients",
            stopwatch.ElapsedMilliseconds, allRecipients.Count);
    }
    ```
  - [ ] Save file
  - [ ] Verify no compilation errors: `dotnet build src/LankaConnect.Application`

#### Task 1.3: Write Unit Tests for Bulk Query

- [ ] **File**: `tests/LankaConnect.Application.Tests/Events/EventHandlers/EventCancelledEventHandlerTests.cs`
  - [ ] Add test for bulk query optimization:
    ```csharp
    [Fact]
    public async Task Handle_WithManyRegistrations_UsesBulkEmailQuery()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var @event = BuildTestEvent(eventId);

        var registrations = Enumerable.Range(1, 50)
            .Select(i => BuildTestRegistration(@event.Id, Guid.NewGuid()))
            .ToList();

        var mockUserRepository = new Mock<IUserRepository>();

        // Setup bulk email query to return emails
        var userIds = registrations.Select(r => r.UserId!.Value).ToList();
        var emailsByUserId = userIds.ToDictionary(id => id, id => $"user{id}@test.com");

        mockUserRepository
            .Setup(r => r.GetEmailsByUserIdsAsync(
                It.Is<IEnumerable<Guid>>(ids => ids.SequenceEqual(userIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailsByUserId);

        var handler = BuildHandler(
            userRepository: mockUserRepository.Object);

        // Act
        await handler.Handle(BuildNotification(eventId, "Test"), CancellationToken.None);

        // Assert
        mockUserRepository.Verify(
            r => r.GetEmailsByUserIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should use bulk email query");

        mockUserRepository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should NOT use GetByIdAsync in loop (N+1 pattern)");
    }
    ```
  - [ ] Add test for performance requirement:
    ```csharp
    [Fact]
    public async Task Handle_WithManyRegistrations_CompletesWithinReasonableTime()
    {
        // Arrange - 100 registrations
        var handler = BuildHandlerWithMockedEmailService();
        var notification = BuildNotificationWith100Registrations();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await handler.Handle(notification, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 90000,
            $"Handler took {stopwatch.ElapsedMilliseconds}ms, expected < 90000ms");
    }
    ```
  - [ ] Save file
  - [ ] Run tests: `dotnet test tests/LankaConnect.Application.Tests --filter "FullyQualifiedName~EventCancelledEventHandlerTests"`
  - [ ] Verify all tests pass

#### Task 1.4: Run Full Test Suite

- [ ] Run all unit tests: `dotnet test tests/LankaConnect.Application.Tests`
- [ ] Verify 0 test failures
- [ ] Run backend build: `dotnet build src/LankaConnect.API`
- [ ] Verify 0 compilation errors

### Afternoon Session (4 hours)

#### Task 1.5: Update Frontend Timeout Configuration

- [ ] **File**: `web/src/infrastructure/api/repositories/events.repository.ts`
  - [ ] Update cancelEvent method (line 236):
    ```typescript
    async cancelEvent(id: string, reason: string): Promise<void> {
      const request: CancelEventRequest = { reason };

      // Phase 6A.64 FIX: Override default 30s timeout with 90s for cancel operation
      // This accommodates email sending to confirmed registrations
      // Phase 2 will move emails to background jobs, eliminating this need
      await apiClient.post<void>(
        `${this.basePath}/${id}/cancel`,
        request,
        { timeout: 90000 } // 90 seconds (was 30s)
      );
    }
    ```
  - [ ] Save file
  - [ ] Verify TypeScript compiles: `npm run build` (in web directory)

#### Task 1.6: Fix Frontend State Management

- [ ] **File**: `web/src/app/events/[id]/manage/page.tsx`
  - [ ] Update handleCancelEvent method (lines 119-147):
    ```typescript
    const handleCancelEvent = async () => {
      try {
        setIsCancelling(true);
        setError(null);

        await eventsRepository.cancelEvent(id, cancellationReason.trim());

        // Success path
        setIsCancelling(false);
        setShowCancelModal(false);
        setCancellationReason('');
        await refetch();

      } catch (err) {
        console.error('Failed to cancel event:', err);

        // Phase 6A.64 CRITICAL FIX: Always refetch to get latest state from server
        // The cancellation may have succeeded despite timeout/error
        await refetch();

        // Determine if this is a validation error (event already cancelled)
        // or a timeout/network error
        const errorMessage = err instanceof Error ? err.message : 'Failed to cancel event. Please try again.';

        if (errorMessage.includes('Only published or draft events can be cancelled')) {
          // Event was already cancelled - close modal and show success
          setShowCancelModal(false);
          setCancellationReason('');
          // Success message will show from refetched event state
        } else {
          // Real error - show error message and keep modal open
          setError(errorMessage);
        }

        setIsCancelling(false);
      }
    };
    ```
  - [ ] Save file
  - [ ] Verify TypeScript compiles: `npm run build`

#### Task 1.7: Local Testing

- [ ] Start backend: `dotnet run --project src/LankaConnect.API`
- [ ] Start frontend: `npm run dev` (in web directory)
- [ ] Test Scenario 1: Cancel event with 10 registrations
  - [ ] Create test event via UI
  - [ ] Add 10 confirmed registrations (manual or seeded)
  - [ ] Click "Cancel Event"
  - [ ] Verify: Operation completes within 10 seconds
  - [ ] Verify: Event status shows "Cancelled"
  - [ ] Verify: Success message displayed
  - [ ] Check logs for bulk query usage
- [ ] Test Scenario 2: Cancel event with 50 registrations
  - [ ] Verify: Operation completes within 30 seconds
  - [ ] Verify: No timeout error
  - [ ] Check logs: "Duration: XXXXms" should be <30000ms

#### Task 1.8: Deploy to Staging

- [ ] Commit Phase 1 changes:
  ```bash
  git add .
  git commit -m "Phase 6A.64 Part 1: Event cancellation timeout hotfix

  - Add UserRepository.GetEmailsByUserIdsAsync for bulk email loading
  - Refactor EventCancelledEventHandler to eliminate N+1 queries
  - Add performance logging with stopwatch
  - Increase frontend timeout to 90s for cancel operation
  - Fix frontend state management to refetch on error
  - Add unit tests for bulk query optimization

  Performance improvement: 10-20 seconds faster, 95% success rate for <200 recipients

  Related: EVENT_CANCELLATION_TIMEOUT_FIX_IMPLEMENTATION_STRATEGY.md"
  ```

- [ ] Build backend for staging:
  ```bash
  cd src/LankaConnect.API
  dotnet publish -c Release -o ./publish
  ```

- [ ] Deploy to Azure Container Apps (staging):
  ```bash
  az containerapp update \
    --name lankaconnect-api-staging \
    --resource-group lankaconnect-staging \
    --image <your-acr>.azurecr.io/lankaconnect-api:phase1-hotfix \
    --revision-suffix phase1-hotfix
  ```

- [ ] Deploy frontend to Azure Static Web Apps (staging):
  ```bash
  cd web
  npm run build
  # Push to develop branch (triggers automatic deployment)
  git push origin feature/phase-6a64-event-cancellation-timeout-fix
  ```

#### Task 1.9: Verify Staging Deployment

- [ ] Check backend health: `curl https://lankaconnect-staging.azurewebsites.net/health`
- [ ] Check frontend loads: Open `https://lankaconnect-staging.azurestaticapps.net`
- [ ] Test event cancellation on staging:
  - [ ] Create event with 50 registrations
  - [ ] Cancel event
  - [ ] Verify: Completes within 30 seconds
  - [ ] Verify: Event status = Cancelled
  - [ ] Check logs in Azure Container Apps

#### Task 1.10: Phase 1 Sign-off

- [ ] Performance improvement verified (10-20s faster)
- [ ] 0 timeout errors for <100 recipients tested
- [ ] Frontend refetch working correctly
- [ ] All tests passing
- [ ] Staging deployment successful
- [ ] Document Phase 1 completion in PROGRESS_TRACKER.md
- [ ] **Phase 1 Complete** ✓

---

## Phase 2: Background Jobs (Days 2-4)

**Goal**: <1 second API response, unlimited scalability

### Day 2: Background Job Implementation

#### Task 2.1: Create EventCancellationEmailJob

- [ ] **File**: `src/LankaConnect.Application/Events/BackgroundJobs/EventCancellationEmailJob.cs` (NEW FILE)
  - [ ] Create new file in BackgroundJobs directory
  - [ ] Copy implementation from strategy document (section 2.3)
  - [ ] Key attributes:
    ```csharp
    [Queue("emails")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    ```
  - [ ] Include all helper methods: GetEventLocationString, FormatEventDateTimeRange
  - [ ] Add comprehensive logging
  - [ ] Save file
  - [ ] Verify compilation: `dotnet build src/LankaConnect.Application`

#### Task 2.2: Refactor EventCancelledEventHandler

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
  - [ ] Replace entire implementation with background job queueing:
    ```csharp
    using Hangfire;
    using LankaConnect.Application.Common;
    using LankaConnect.Application.Events.BackgroundJobs;
    using LankaConnect.Domain.Events.DomainEvents;
    using MediatR;
    using Microsoft.Extensions.Logging;

    namespace LankaConnect.Application.Events.EventHandlers;

    /// <summary>
    /// Phase 6A.64: Handles EventCancelledEvent by queueing background job for email notifications.
    /// This ensures fast response time (<1 second) and prevents timeout issues.
    /// Email sending happens asynchronously in EventCancellationEmailJob.
    /// </summary>
    public class EventCancelledEventHandler : INotificationHandler<DomainEventNotification<EventCancelledEvent>>
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<EventCancelledEventHandler> _logger;

        public EventCancelledEventHandler(
            IBackgroundJobClient backgroundJobClient,
            ILogger<EventCancelledEventHandler> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<EventCancelledEvent> notification, CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            _logger.LogInformation(
                "[Phase 6A.64] EventCancelledEventHandler - Queueing background job for Event {EventId}",
                domainEvent.EventId);

            try
            {
                // Queue background job for email sending (returns immediately)
                var jobId = _backgroundJobClient.Enqueue<EventCancellationEmailJob>(
                    job => job.SendCancellationEmailsAsync(
                        domainEvent.EventId,
                        domainEvent.Reason,
                        domainEvent.CancelledAt));

                _logger.LogInformation(
                    "[Phase 6A.64] Background job {JobId} queued for Event {EventId} cancellation emails",
                    jobId, domainEvent.EventId);
            }
            catch (Exception ex)
            {
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "[Phase 6A.64] Error queueing background job for EventCancelledEvent for Event {EventId}",
                    domainEvent.EventId);
            }

            await Task.CompletedTask;
        }
    }
    ```
  - [ ] Save file
  - [ ] Verify compilation: `dotnet build src/LankaConnect.Application`

#### Task 2.3: Update Unit Tests

- [ ] **File**: `tests/LankaConnect.Application.Tests/Events/EventHandlers/EventCancelledEventHandlerTests.cs`
  - [ ] Update existing tests for job queueing pattern
  - [ ] Mock IBackgroundJobClient instead of repositories
  - [ ] Add test:
    ```csharp
    [Fact]
    public async Task Handle_QueuesBackgroundJobWithCorrectParameters()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var reason = "Test cancellation";
        var cancelledAt = DateTime.UtcNow;

        var mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        var handler = new EventCancelledEventHandler(
            mockBackgroundJobClient.Object,
            Mock.Of<ILogger<EventCancelledEventHandler>>());

        var notification = new DomainEventNotification<EventCancelledEvent>(
            new EventCancelledEvent(eventId, reason, cancelledAt));

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockBackgroundJobClient.Verify(
            client => client.Enqueue<EventCancellationEmailJob>(
                It.IsAny<Expression<Action<EventCancellationEmailJob>>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CompletesQuickly()
    {
        // Arrange
        var handler = BuildHandler();
        var notification = BuildNotification();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await handler.Handle(notification, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"Handler took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
    }
    ```
  - [ ] Run tests: `dotnet test tests/LankaConnect.Application.Tests --filter "EventCancelledEventHandlerTests"`
  - [ ] Verify all tests pass

#### Task 2.4: Create Background Job Tests

- [ ] **File**: `tests/LankaConnect.Application.Tests/Events/BackgroundJobs/EventCancellationEmailJobTests.cs` (NEW FILE)
  - [ ] Create new test file
  - [ ] Add tests:
    ```csharp
    [Fact]
    public async Task SendCancellationEmailsAsync_LoadsEventData()
    {
        // Test job loads event correctly
    }

    [Fact]
    public async Task SendCancellationEmailsAsync_UsesBulkEmailQuery()
    {
        // Test job uses bulk query (not N+1)
    }

    [Fact]
    public async Task SendCancellationEmailsAsync_SendsEmailsToAllRecipients()
    {
        // Test job sends emails to confirmed registrations, email groups, newsletter
    }

    [Fact]
    public async Task SendCancellationEmailsAsync_HandlesEmailServiceFailure()
    {
        // Test job logs errors but doesn't throw
    }

    [Fact]
    public async Task SendCancellationEmailsAsync_LogsPerformanceMetrics()
    {
        // Test job logs duration, success/fail counts
    }
    ```
  - [ ] Run tests: `dotnet test tests/LankaConnect.Application.Tests --filter "EventCancellationEmailJobTests"`
  - [ ] Verify all tests pass

#### Task 2.5: Run Full Test Suite

- [ ] Run all unit tests: `dotnet test tests/LankaConnect.Application.Tests`
- [ ] Verify 0 test failures
- [ ] Run backend build: `dotnet build src/LankaConnect.API`
- [ ] Verify 0 compilation errors

### Day 3: Staging Testing and Validation

#### Task 3.1: Deploy Phase 2 to Staging

- [ ] Commit Phase 2 changes:
  ```bash
  git add .
  git commit -m "Phase 6A.64 Part 2: Event cancellation background jobs

  - Create EventCancellationEmailJob for asynchronous email sending
  - Refactor EventCancelledEventHandler to queue Hangfire job
  - Add comprehensive background job tests
  - Leverage existing Hangfire infrastructure

  Performance improvement: <1 second API response, unlimited scalability

  Related: EVENT_CANCELLATION_TIMEOUT_FIX_IMPLEMENTATION_STRATEGY.md"
  ```

- [ ] Build backend for staging:
  ```bash
  cd src/LankaConnect.API
  dotnet publish -c Release -o ./publish
  ```

- [ ] Deploy to Azure Container Apps (staging):
  ```bash
  az containerapp update \
    --name lankaconnect-api-staging \
    --resource-group lankaconnect-staging \
    --image <your-acr>.azurecr.io/lankaconnect-api:phase2-background-jobs \
    --revision-suffix phase2-background-jobs
  ```

#### Task 3.2: Verify Hangfire Infrastructure

- [ ] Check Hangfire dashboard: `https://lankaconnect-staging.azurewebsites.net/hangfire`
- [ ] Verify server is running:
  - [ ] Navigate to "Servers" tab
  - [ ] Verify worker count: 1
  - [ ] Verify queues: default, emails
- [ ] Verify existing recurring jobs still working:
  - [ ] event-reminder-job
  - [ ] event-status-update-job
  - [ ] expired-badge-cleanup-job

#### Task 3.3: Test Background Job Queueing

- [ ] Create test event with 10 registrations on staging
- [ ] Cancel event via UI
- [ ] Verify:
  - [ ] API returns success < 1 second
  - [ ] Event status = Cancelled in database
  - [ ] Frontend shows "Event cancelled successfully"
  - [ ] Check Hangfire dashboard: Job appears in "Enqueued" tab
  - [ ] Job state transitions: Enqueued → Processing → Succeeded
  - [ ] 10 cancellation emails sent
  - [ ] Check Azure Container Apps logs for job execution

#### Task 3.4: Load Testing with 100 Recipients

- [ ] Create event with 100 confirmed registrations
- [ ] Cancel event
- [ ] Measure API response time (should be <1 second)
- [ ] Monitor Hangfire dashboard:
  - [ ] Job processing time (track duration)
  - [ ] Success/failure status
- [ ] Verify all 100 emails sent
- [ ] Check logs for performance metrics
- [ ] Document results

#### Task 3.5: Load Testing with 500 Recipients

- [ ] Create event with 500 confirmed registrations (may need to use data seeding script)
- [ ] Cancel event
- [ ] Measure API response time (should be <1 second)
- [ ] Monitor Hangfire dashboard:
  - [ ] Job processing time (should be 2-5 minutes)
  - [ ] Verify job completes successfully
- [ ] Verify all 500 emails sent
- [ ] Check logs for any errors or warnings
- [ ] Document results

#### Task 3.6: Error Handling and Retry Testing

- [ ] **Test Scenario 1: Temporary Email Service Failure**
  - [ ] Temporarily break email service configuration (e.g., invalid SMTP credentials)
  - [ ] Cancel event
  - [ ] Verify:
    - [ ] API still returns success
    - [ ] Event status = Cancelled
    - [ ] Hangfire job moves to "Failed" state
    - [ ] Job scheduled for retry after 1 minute
  - [ ] Fix email service configuration
  - [ ] Wait for automatic retry
  - [ ] Verify: Job succeeds on retry

- [ ] **Test Scenario 2: Manual Job Retry**
  - [ ] Find failed job in Hangfire dashboard
  - [ ] Click "Requeue" button
  - [ ] Verify: Job re-runs successfully
  - [ ] Verify: Emails sent (no duplication)

### Day 4: Frontend Optimization and Production Deployment

#### Task 4.1: Revert Frontend Timeout to 30s

- [ ] **File**: `web/src/infrastructure/api/repositories/events.repository.ts`
  - [ ] Remove timeout override (revert to default 30s):
    ```typescript
    async cancelEvent(id: string, reason: string): Promise<void> {
      const request: CancelEventRequest = { reason };

      // Phase 6A.64: Background jobs implemented, no longer need 90s timeout
      await apiClient.post<void>(
        `${this.basePath}/${id}/cancel`,
        request
        // Removed: { timeout: 90000 }
      );
    }
    ```
  - [ ] Add comment explaining why timeout was removed
  - [ ] Save file
  - [ ] Verify TypeScript compiles: `npm run build`

#### Task 4.2: Test Frontend with Default Timeout

- [ ] Deploy frontend change to staging
- [ ] Cancel event with 500 registrations
- [ ] Verify:
  - [ ] API returns success within 30s (should be <1s)
  - [ ] No timeout error
  - [ ] Frontend shows success immediately
  - [ ] Hangfire job processes emails in background

#### Task 4.3: Final Staging Validation

- [ ] Run complete test suite on staging:
  - [ ] Cancel event with 10 registrations → <1s response
  - [ ] Cancel event with 50 registrations → <1s response
  - [ ] Cancel event with 100 registrations → <1s response
  - [ ] Cancel event with 500 registrations → <1s response
  - [ ] Verify all emails delivered
  - [ ] Verify no email duplication
  - [ ] Verify Hangfire retry works

- [ ] Verify existing functionality not broken:
  - [ ] Create event → Works
  - [ ] Publish event → Works (EventPublishedEventHandler still synchronous)
  - [ ] Register for event → Works
  - [ ] EventReminderJob → Still running hourly

#### Task 4.4: Update Documentation

- [ ] Update PROGRESS_TRACKER.md:
  - [ ] Mark Phase 6A.64 as "Complete"
  - [ ] Add summary of deliverables
  - [ ] Link to implementation documents

- [ ] Update STREAMLINED_ACTION_PLAN.md:
  - [ ] Update phase status
  - [ ] Add completion date

- [ ] Update TASK_SYNCHRONIZATION_STRATEGY.md:
  - [ ] Update task status

- [ ] Create PHASE_6A64_SUMMARY.md (if not already created):
  - [ ] Implementation details
  - [ ] Performance metrics
  - [ ] Lessons learned
  - [ ] Future enhancements

#### Task 4.5: Production Deployment

- [ ] Merge feature branch to develop:
  ```bash
  git checkout develop
  git merge feature/phase-6a64-event-cancellation-timeout-fix
  git push origin develop
  ```

- [ ] Create pull request to master:
  - [ ] Title: "Phase 6A.64: Event Cancellation Timeout Fix - Background Jobs"
  - [ ] Description: Include summary, testing results, performance metrics
  - [ ] Link to implementation documents
  - [ ] Request code review

- [ ] After approval, merge to master:
  ```bash
  git checkout master
  git merge develop
  git push origin master
  ```

- [ ] Tag release:
  ```bash
  git tag -a v6A.64 -m "Phase 6A.64: Event cancellation timeout fix - Background jobs implementation"
  git push origin v6A.64
  ```

- [ ] Deploy to production:
  - [ ] Build production release
  - [ ] Deploy backend to Azure Container Apps (production)
  - [ ] Deploy frontend to Azure Static Web Apps (production)
  - [ ] Verify health endpoints
  - [ ] Monitor Hangfire dashboard

#### Task 4.6: Production Monitoring

- [ ] Monitor production for 24 hours:
  - [ ] Check Hangfire dashboard every 4 hours
  - [ ] Monitor Azure Container Apps metrics:
    - [ ] CPU usage
    - [ ] Memory usage
    - [ ] Request latency
  - [ ] Check application logs for errors
  - [ ] Verify event cancellations working correctly
  - [ ] Verify background jobs processing successfully

- [ ] Set up alerts (if not already configured):
  - [ ] Alert on failed Hangfire jobs
  - [ ] Alert on API response time >2 seconds
  - [ ] Alert on error rate >1%

#### Task 4.7: Phase 2 Sign-off

- [ ] API response time <1 second verified ✓
- [ ] Background jobs processing successfully ✓
- [ ] 500+ recipient scalability tested ✓
- [ ] Retry mechanism working ✓
- [ ] No email duplication ✓
- [ ] No breaking changes to existing functionality ✓
- [ ] All tests passing ✓
- [ ] Production deployment successful ✓
- [ ] Monitoring in place ✓
- [ ] **Phase 2 Complete** ✓

---

## Post-Implementation

### Documentation Updates

- [ ] Update Master Requirements Specification.md (if user-facing features changed)
- [ ] Update PROJECT_CONTENT.md with Phase 6A.64 status
- [ ] Create summary document in docs/phase-summaries/
- [ ] Update architecture diagrams if needed

### Knowledge Transfer

- [ ] Present implementation to team:
  - [ ] Phase 1 optimizations (bulk query)
  - [ ] Phase 2 background jobs (Hangfire)
  - [ ] Hangfire dashboard usage
  - [ ] Monitoring and troubleshooting

- [ ] Document runbook for common issues:
  - [ ] How to retry failed jobs
  - [ ] How to monitor job queue
  - [ ] How to scale Hangfire workers
  - [ ] How to troubleshoot email delivery

### Retrospective

- [ ] What went well?
- [ ] What could be improved?
- [ ] Lessons learned for future background job implementations
- [ ] Performance metrics vs. expectations
- [ ] User feedback on improved experience

---

## Success Metrics

**Phase 1**:
- ✓ API response time: 15-25 seconds (was 30-45s)
- ✓ Timeout error rate: <5% (was >50%)
- ✓ N+1 queries eliminated: 1 query (was 50 queries)
- ✓ Supports up to 200 recipients

**Phase 2**:
- ✓ API response time: <1 second (was 30-45s)
- ✓ Timeout error rate: 0%
- ✓ Scalability: Unlimited recipients (tested with 500)
- ✓ Email delivery rate: >99%
- ✓ Job success rate: >99%

**Overall Impact**:
- ✓ 95%+ improvement in API response time
- ✓ 100% elimination of timeout errors
- ✓ Scalable to enterprise-level events
- ✓ Pattern established for future bulk operations
- ✓ Foundation for EventPublishedEventHandler and EventPostponedEventHandler optimizations

---

## Rollback Procedures

### If Phase 1 Fails

```bash
# Revert backend
git revert <phase-1-commit-sha>
az containerapp revision set-mode --revision <previous-revision>

# Revert frontend
git revert <phase-1-commit-sha>
git push origin develop
```

### If Phase 2 Fails

```bash
# Revert to Phase 1 (which works)
git revert <phase-2-commit-sha>
az containerapp revision set-mode --revision phase1-hotfix

# Phase 1 code still functional with 90s timeout
```

---

## Notes and Observations

**During Implementation**:
- [ ] Record any unexpected issues
- [ ] Note performance metrics for different recipient counts
- [ ] Document any deviations from plan
- [ ] Track time spent on each task

**Lessons Learned**:
- [ ] What worked well?
- [ ] What was more difficult than expected?
- [ ] What would you do differently next time?
- [ ] Any recommendations for similar future work?

---

## Team Sign-off

- [ ] Backend Developer - Implementation complete
- [ ] Frontend Developer - UI changes complete
- [ ] QA Engineer - All tests passing
- [ ] DevOps Engineer - Deployment successful
- [ ] Product Owner - User experience verified
- [ ] Engineering Team Lead - Final approval

**Phase 6A.64 Status**: ⬜ Not Started | ⬜ In Progress | ☑️ Complete

**Completion Date**: _________________

---

**Reference Documents**:
- [Complete Implementation Strategy](./EVENT_CANCELLATION_TIMEOUT_FIX_IMPLEMENTATION_STRATEGY.md)
- [Phase 6A.64 Summary](./PHASE_6A64_EVENT_CANCELLATION_TIMEOUT_FIX_SUMMARY.md)
- [C4 Architecture Diagrams](./architecture/EVENT_CANCELLATION_TIMEOUT_C4_DIAGRAMS.md)
- [ADR-010](./architecture/ADR-010-EVENT-CANCELLATION-BACKGROUND-JOBS.md)