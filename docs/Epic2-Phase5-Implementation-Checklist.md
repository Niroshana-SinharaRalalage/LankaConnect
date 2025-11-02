# Epic 2 Phase 5 - Implementation Checklist

**Project**: LankaConnect
**Phase**: Epic 2 Phase 5 (Advanced Features)
**Date**: 2025-11-02

---

## Overview

This checklist provides a step-by-step implementation guide for Epic 2 Phase 5. Follow this sequentially to ensure proper dependency order and continuous test coverage.

**Total Estimated Time**: 5 days (40 hours)

---

## Day 1: Domain Event Dispatching Setup (4 hours)

### Step 1: Add MediatR to AppDbContext (1 hour)

- [ ] **File**: `src/LankaConnect.Infrastructure/Data/AppDbContext.cs`
- [ ] Add `IMediator` field and inject via constructor
- [ ] Override `SaveChangesAsync()` to dispatch domain events
- [ ] Create `DispatchDomainEventsAsync()` private method
- [ ] Test: Create integration test to verify domain events are published

**Code Snippet**:
```csharp
private readonly IMediator _mediator;

public AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator)
    : base(options)
{
    _mediator = mediator;
}

public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var result = await base.SaveChangesAsync(cancellationToken);
    await DispatchDomainEventsAsync(cancellationToken);
    return result;
}

private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
{
    var domainEntities = ChangeTracker.Entries<BaseEntity>()
        .Where(x => x.Entity.DomainEvents.Any())
        .Select(x => x.Entity)
        .ToList();

    var domainEvents = domainEntities
        .SelectMany(x => x.DomainEvents)
        .ToList();

    domainEntities.ForEach(entity => entity.ClearDomainEvents());

    foreach (var domainEvent in domainEvents)
    {
        await _mediator.Publish(domainEvent, cancellationToken);
    }
}
```

### Step 2: Create RegistrationConfirmedEventHandler (1.5 hours)

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/RegistrationConfirmedEventHandler.cs`
- [ ] Implement `INotificationHandler<RegistrationConfirmedEvent>`
- [ ] Inject: `IEmailService`, `IUserRepository`, `IEventRepository`, `ILogger`
- [ ] Query user and event data
- [ ] Build email template parameters
- [ ] Call `SendTemplatedEmailAsync("RegistrationConfirmed", ...)`
- [ ] Handle errors gracefully (log, don't throw)

**Test File**: `tests/LankaConnect.Application.Tests/Events/EventHandlers/RegistrationConfirmedEventHandlerTests.cs`
- [ ] Test: Successful email sending with valid data
- [ ] Test: Missing user - logs warning, doesn't throw
- [ ] Test: Missing event - logs warning, doesn't throw
- [ ] Test: Email service failure - logs error, doesn't throw

### Step 3: Create Email Template (30 minutes)

- [ ] **Create template**: RegistrationConfirmed.cshtml (or .html)
- [ ] Template parameters: UserName, EventTitle, EventStartDate, EventLocation, RegistrationQuantity, EventDetailsUrl
- [ ] Seed template into database via migration or admin UI

**Template Location**: `src/LankaConnect.Infrastructure/Email/Templates/RegistrationConfirmed.cshtml`

### Step 4: Integration Test (1 hour)

- [ ] **File**: `tests/LankaConnect.CleanIntegrationTests/Events/RegistrationEmailNotificationTests.cs`
- [ ] Test: RSVP to event triggers RegistrationConfirmedEvent
- [ ] Test: Email is queued in `communications.email_messages` table
- [ ] Test: Email parameters match expected values

**Commit**: "feat(epic2-phase5): Add domain event dispatching and registration confirmation emails"

---

## Day 2: Remaining RSVP Email Handlers (6 hours)

### Step 5: RegistrationCancelledEventHandler (1.5 hours)

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/RegistrationCancelledEventHandler.cs`
- [ ] Similar pattern to RegistrationConfirmedEventHandler
- [ ] Template: RegistrationCancelled.cshtml
- [ ] Test file: `RegistrationCancelledEventHandlerTests.cs`

**Template Parameters**: UserName, EventTitle, EventStartDate, CancellationDate

### Step 6: EventCancelledEventHandler (2 hours)

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/EventCancelledEventHandler.cs`
- [ ] Query all confirmed registrations for event
- [ ] Batch fetch users via `GetByIdsAsync()`
- [ ] Use `SendBulkEmailAsync()` for all attendees
- [ ] Template: EventCancelled.cshtml
- [ ] Test file: `EventCancelledEventHandlerTests.cs`

**Template Parameters**: UserName, EventTitle, EventStartDate, CancellationReason, OrganizerName

**Special Considerations**:
- Bulk notification (potentially 100+ emails)
- Use batch queries to avoid N+1 problem
- Test with multiple attendees

### Step 7: EventPostponedEventHandler (2 hours)

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/EventPostponedEventHandler.cs`
- [ ] Similar to EventCancelledEventHandler (notify all attendees)
- [ ] Template: EventPostponed.cshtml
- [ ] Test file: `EventPostponedEventHandlerTests.cs`

**Template Parameters**: UserName, EventTitle, OriginalStartDate, PostponementReason, OrganizerName

### Step 8: Email Templates (30 minutes)

- [ ] Create RegistrationCancelled.cshtml
- [ ] Create EventCancelled.cshtml
- [ ] Create EventPostponed.cshtml
- [ ] Seed all templates into database

**Commit**: "feat(epic2-phase5): Add RSVP cancellation and event status notification emails"

---

## Day 3: Admin Approval Workflow - Domain & Application (4 hours)

### Step 9: Domain Layer - Approve/Reject Methods (1 hour)

- [ ] **File**: `src/LankaConnect.Domain/Events/Event.cs`
- [ ] Add `Approve()` method
  - Business rule: Only UnderReview events can be approved
  - Set Status = Published
  - Raise `EventApprovedEvent`
- [ ] Add `Reject(string reason)` method
  - Business rule: Only UnderReview events can be rejected
  - Set Status = Draft (allow organizer to resubmit)
  - Store reason in `CancellationReason` field
  - Raise `EventRejectedEvent`

**Test File**: `tests/LankaConnect.Application.Tests/Events/Domain/EventApprovalTests.cs`
- [ ] Test: Approve UnderReview event succeeds
- [ ] Test: Approve Published event fails
- [ ] Test: Reject UnderReview event succeeds
- [ ] Test: Reject without reason fails
- [ ] Test: Domain events are raised

### Step 10: Domain Events (30 minutes)

- [ ] **File**: `src/LankaConnect.Domain/Events/DomainEvents/EventApprovedEvent.cs`
```csharp
public record EventApprovedEvent(
    Guid EventId,
    DateTime ApprovedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

- [ ] **File**: `src/LankaConnect.Domain/Events/DomainEvents/EventRejectedEvent.cs`
```csharp
public record EventRejectedEvent(
    Guid EventId,
    string Reason,
    DateTime RejectedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

### Step 11: ApproveEventCommand (1 hour)

- [ ] **File**: `src/LankaConnect.Application/Events/Commands/AdminApproval/ApproveEvent/ApproveEventCommand.cs`
- [ ] **File**: `src/LankaConnect.Application/Events/Commands/AdminApproval/ApproveEvent/ApproveEventCommandHandler.cs`
- [ ] Query event from repository
- [ ] Call `@event.Approve()`
- [ ] Commit via UnitOfWork

**Test File**: `tests/LankaConnect.Application.Tests/Events/Commands/AdminApproval/ApproveEventCommandHandlerTests.cs`
- [ ] Test: Approve UnderReview event succeeds
- [ ] Test: Event not found returns failure
- [ ] Test: Approve Published event returns failure
- [ ] Test: Domain event is raised

### Step 12: RejectEventCommand (1 hour)

- [ ] **File**: `src/LankaConnect.Application/Events/Commands/AdminApproval/RejectEvent/RejectEventCommand.cs`
- [ ] **File**: `src/LankaConnect.Application/Events/Commands/AdminApproval/RejectEvent/RejectEventCommandHandler.cs`
- [ ] **File**: `src/LankaConnect.Application/Events/Commands/AdminApproval/RejectEvent/RejectEventCommandValidator.cs`
  - Validate reason is not empty

**Test File**: `tests/LankaConnect.Application.Tests/Events/Commands/AdminApproval/RejectEventCommandHandlerTests.cs`
- [ ] Test: Reject with valid reason succeeds
- [ ] Test: Reject without reason fails validation
- [ ] Test: Reject Published event returns failure

### Step 13: Event Handlers for Approval Emails (30 minutes)

- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/EventApprovedEventHandler.cs`
  - Send email to organizer with approval confirmation
- [ ] **File**: `src/LankaConnect.Application/Events/EventHandlers/EventRejectedEventHandler.cs`
  - Send email to organizer with rejection reason

**Test Files**: `EventApprovedEventHandlerTests.cs`, `EventRejectedEventHandlerTests.cs`

**Commit**: "feat(epic2-phase5): Add admin approval workflow with domain events"

---

## Day 4: Admin Approval - API Layer & Hangfire Setup (4 hours)

### Step 14: EventsController Endpoints (1 hour)

- [ ] **File**: `src/LankaConnect.API/Controllers/EventsController.cs`
- [ ] Add `ApproveEvent(Guid id)` endpoint
  - `[HttpPost("{id}/approve")]`
  - `[Authorize(Policy = "AdminOnly")]`
  - Send `ApproveEventCommand`
- [ ] Add `RejectEvent(Guid id, RejectEventRequest request)` endpoint
  - `[HttpPost("{id}/reject")]`
  - `[Authorize(Policy = "AdminOnly")]`
  - Send `RejectEventCommand`
- [ ] Add `RejectEventRequest` record with `Reason` property

**Test File**: `tests/LankaConnect.CleanIntegrationTests/Controllers/EventsControllerApprovalTests.cs`
- [ ] Test: Admin can approve event (returns 200 OK)
- [ ] Test: Non-admin cannot approve event (returns 403 Forbidden)
- [ ] Test: Approve non-existent event (returns 404 Not Found)
- [ ] Test: Reject with reason succeeds
- [ ] Test: Reject without reason fails validation (400 Bad Request)

### Step 15: Email Templates for Approval (30 minutes)

- [ ] Create EventApproved.cshtml
  - Parameters: OrganizerName, EventTitle, ApprovalDate, EventDetailsUrl
- [ ] Create EventRejected.cshtml
  - Parameters: OrganizerName, EventTitle, RejectionReason, RejectionDate
- [ ] Seed templates into database

**Commit**: "feat(epic2-phase5): Add admin approval API endpoints and email templates"

### Step 16: Install Hangfire Packages (30 minutes)

- [ ] **File**: `Directory.Packages.props`
- [ ] Add Hangfire.AspNetCore v1.8.14
- [ ] Add Hangfire.PostgreSql v1.20.9

- [ ] **File**: `src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj`
- [ ] Add PackageReference for Hangfire.AspNetCore
- [ ] Add PackageReference for Hangfire.PostgreSql

- [ ] Run `dotnet restore`

### Step 17: Configure Hangfire in Infrastructure (1.5 hours)

- [ ] **File**: `src/LankaConnect.Infrastructure/DependencyInjection.cs`
- [ ] Add Hangfire configuration with PostgreSQL storage
- [ ] Configure Hangfire server options (WorkerCount, PollingInterval)
- [ ] Register job interfaces and implementations

**Code Snippet**:
```csharp
// Add Hangfire with PostgreSQL storage
services.AddHangfire(config =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
        {
            options.UseNpgsqlConnection(connectionString);
        });
});

// Add Hangfire server
services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
});

// Register background jobs
services.AddScoped<IEventReminderJob, EventReminderJob>();
services.AddScoped<IEventStatusUpdateJob, EventStatusUpdateJob>();
```

### Step 18: Dashboard Authorization Filter (30 minutes)

- [ ] **File**: `src/LankaConnect.API/Authentication/HangfireDashboardAuthorizationFilter.cs`
- [ ] Implement `IDashboardAuthorizationFilter`
- [ ] Check `User.Identity.IsAuthenticated` and `User.IsInRole("Admin")`

**Test**: Manual verification (cannot easily unit test)

**Commit**: "feat(epic2-phase5): Install and configure Hangfire with PostgreSQL storage"

---

## Day 5: Hangfire Background Jobs (8 hours)

### Step 19: EventReminderJob Interface & Implementation (3 hours)

- [ ] **File**: `src/LankaConnect.Application/Common/Interfaces/IEventReminderJob.cs`
```csharp
public interface IEventReminderJob
{
    Task SendEventRemindersAsync(CancellationToken cancellationToken);
}
```

- [ ] **File**: `src/LankaConnect.Infrastructure/BackgroundJobs/EventReminderJob.cs`
- [ ] Inject: `IEventRepository`, `IRegistrationRepository`, `IUserRepository`, `IEmailService`, `ILogger`
- [ ] Query events starting in 24 hours
- [ ] For each event:
  - Get confirmed registrations
  - Batch fetch users
  - Build email messages
  - Send bulk emails

**Repository Method Needed** (add to `IEventRepository`):
```csharp
Task<IEnumerable<Event>> GetEventsStartingBetweenAsync(
    DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
```

**Implementation** (`EventRepository.cs`):
```csharp
public async Task<IEnumerable<Event>> GetEventsStartingBetweenAsync(
    DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
{
    return await _context.Events
        .Where(e => e.StartDate >= startDate && e.StartDate <= endDate)
        .Include(e => e.Registrations)
        .ToListAsync(cancellationToken);
}
```

**Test File**: `tests/LankaConnect.Infrastructure.Tests/BackgroundJobs/EventReminderJobTests.cs`
- [ ] Test: Events within 24h window trigger reminders
- [ ] Test: No reminders sent for events outside window
- [ ] Test: Only confirmed registrations receive reminders
- [ ] Test: Cancelled events are skipped

### Step 20: EventStatusUpdateJob Interface & Implementation (3 hours)

- [ ] **File**: `src/LankaConnect.Application/Common/Interfaces/IEventStatusUpdateJob.cs`
```csharp
public interface IEventStatusUpdateJob
{
    Task UpdateEventStatusAsync(CancellationToken cancellationToken);
}
```

- [ ] **File**: `src/LankaConnect.Infrastructure/BackgroundJobs/EventStatusUpdateJob.cs`
- [ ] Inject: `IEventRepository`, `IUnitOfWork`, `ILogger`
- [ ] Query published events where `StartDate <= now` (should be Active)
- [ ] Call `@event.ActivateEvent()` for each
- [ ] Query active events where `EndDate <= now` (should be Completed)
- [ ] Call `@event.Complete()` for each
- [ ] Commit all changes

**Repository Methods Needed**:
```csharp
Task<IEnumerable<Event>> GetPublishedEventsStartingBeforeAsync(
    DateTime date, CancellationToken cancellationToken);

Task<IEnumerable<Event>> GetActiveEventsEndingBeforeAsync(
    DateTime date, CancellationToken cancellationToken);
```

**Test File**: `tests/LankaConnect.Infrastructure.Tests/BackgroundJobs/EventStatusUpdateJobTests.cs`
- [ ] Test: Published event past start date is activated
- [ ] Test: Active event past end date is completed
- [ ] Test: Events not meeting criteria are skipped

### Step 21: Register Recurring Jobs in Program.cs (1 hour)

- [ ] **File**: `src/LankaConnect.API/Program.cs`
- [ ] Add Hangfire dashboard middleware with authorization
- [ ] Register recurring jobs using `RecurringJob.AddOrUpdate()`

**Code Snippet**:
```csharp
// After app.UseAuthorization();

// Add Hangfire Dashboard with authorization
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
    DashboardTitle = "LankaConnect Background Jobs"
});

// Schedule recurring jobs
RecurringJob.AddOrUpdate<IEventReminderJob>(
    "send-event-reminders",
    job => job.SendEventRemindersAsync(CancellationToken.None),
    Cron.Hourly);

RecurringJob.AddOrUpdate<IEventStatusUpdateJob>(
    "update-event-status",
    job => job.UpdateEventStatusAsync(CancellationToken.None),
    Cron.Hourly);
```

### Step 22: Integration Testing (1 hour)

- [ ] **File**: `tests/LankaConnect.CleanIntegrationTests/BackgroundJobs/HangfireJobsIntegrationTests.cs`
- [ ] Test: EventReminderJob can be executed manually
- [ ] Test: EventStatusUpdateJob can be executed manually
- [ ] Test: Jobs appear in Hangfire dashboard
- [ ] Manual testing: Access `/hangfire` dashboard as admin

**Commit**: "feat(epic2-phase5): Add Hangfire background jobs for event reminders and status updates"

---

## Final Steps

### Step 23: End-to-End Testing (2 hours)

- [ ] Create test event with registrations
- [ ] Verify registration confirmation email sent
- [ ] Cancel RSVP, verify cancellation email
- [ ] Submit event for approval
- [ ] Admin approves event, verify approval email
- [ ] Cancel event, verify all attendees notified
- [ ] Manually trigger EventReminderJob, verify reminders sent
- [ ] Manually trigger EventStatusUpdateJob, verify status updates

### Step 24: Documentation & Cleanup (1 hour)

- [ ] Update PROGRESS_TRACKER.md with Epic 2 Phase 5 completion
- [ ] Document Hangfire dashboard URL and access instructions
- [ ] Document email templates and parameters
- [ ] Remove any unused code or TODO comments

### Step 25: Final Commit & Review

- [ ] Run full test suite: `dotnet test`
- [ ] Verify 0 compilation errors
- [ ] Verify test coverage > 90%
- [ ] Create final commit: "docs(epic2-phase5): Complete Advanced Features implementation"
- [ ] Create pull request with architecture guide and implementation notes

---

## Troubleshooting Checklist

### Domain Events Not Firing
- [ ] Verify `IMediator` is injected into `AppDbContext`
- [ ] Verify `DispatchDomainEventsAsync()` is called in `SaveChangesAsync()`
- [ ] Verify domain event handlers are in Application assembly (auto-discovered by MediatR)

### Emails Not Sending
- [ ] Verify email templates exist in database (`communications.email_templates`)
- [ ] Verify SMTP settings in `appsettings.json`
- [ ] Check `communications.email_messages` table for queued/failed emails
- [ ] Verify `EmailQueueProcessor` background service is running
- [ ] Check application logs for email errors

### Hangfire Jobs Not Running
- [ ] Verify Hangfire tables created in database (run migrations)
- [ ] Verify PostgreSQL connection string is correct
- [ ] Check Hangfire dashboard for job status
- [ ] Verify job interfaces registered in DI
- [ ] Check application logs for Hangfire errors

### Authorization Issues
- [ ] Verify AdminOnly policy registered in `Program.cs`
- [ ] Verify user has Admin role in database
- [ ] Verify JWT token contains role claim
- [ ] Check API response for 403 Forbidden vs 401 Unauthorized

---

## Success Criteria

- [ ] All 4 RSVP email handlers implemented and tested
- [ ] Admin approval workflow functional (approve/reject endpoints)
- [ ] Hangfire dashboard accessible to admins
- [ ] EventReminderJob runs hourly and sends reminders
- [ ] EventStatusUpdateJob runs hourly and updates event statuses
- [ ] All email templates created and seeded
- [ ] Zero compilation errors
- [ ] Test coverage > 90% maintained
- [ ] Integration tests passing
- [ ] Documentation updated

**Estimated Total Time**: 40 hours (5 days)

**Actual Time Spent**: _____ hours

**Completion Date**: __________

**Deployed to**: [ ] Development [ ] Staging [ ] Production

---

**Document Status**: Implementation Checklist
**Last Updated**: 2025-11-02
