# Epic 2 Phase 5 - Advanced Features: Executive Summary

**Project**: LankaConnect (Clean Architecture + DDD)
**Date**: 2025-11-02
**Author**: System Architect
**Estimated Time**: 5 days (40 hours)

---

## Overview

Epic 2 Phase 5 implements advanced features for the event management system:
1. **RSVP Email Notifications** - Automated emails for registration events
2. **Admin Approval Workflow** - Event approval/rejection with notifications
3. **Hangfire Background Jobs** - Scheduled reminders and status updates

All features follow Clean Architecture and DDD principles, integrating seamlessly with existing patterns (CQRS, MediatR, Result pattern).

---

## Key Architectural Decisions

### 1. Domain Event Handlers → Application Layer

**Decision**: Place event handlers in Application layer, not Domain.

**Rationale**:
- Handlers orchestrate cross-aggregate operations (query users, events, registrations)
- Handlers call infrastructure services (IEmailService)
- Domain layer remains pure (no infrastructure dependencies)

**Implementation**:
```
src/LankaConnect.Application/Events/EventHandlers/
├── RegistrationConfirmedEventHandler.cs
├── RegistrationCancelledEventHandler.cs
├── EventCancelledEventHandler.cs
└── EventPostponedEventHandler.cs
```

### 2. Email Sending → Use Existing Infrastructure

**Decision**: Leverage existing `IEmailService.SendTemplatedEmailAsync()` and `EmailQueueProcessor`.

**Rationale**:
- Infrastructure already supports async email queue processing
- No need for Hangfire email jobs (redundant)
- Simpler implementation, reuse tested code

**Pattern**:
```csharp
// Event handler
await _emailService.SendTemplatedEmailAsync(
    templateName: "RegistrationConfirmed",
    recipientEmail: user.Email.Value,
    parameters: templateParams,
    cancellationToken);

// EmailService (existing code):
// 1. Creates EmailMessage entity with Status = "Queued"
// 2. Saves to database
// 3. EmailQueueProcessor (background service) picks up and sends via SMTP
```

### 3. Admin Approval → Domain Methods

**Decision**: Implement `Approve()` and `Reject()` as methods on Event aggregate.

**Rationale**:
- Approval is a state transition with business rules
- Business logic belongs in domain layer
- Testable in isolation, reusable across use cases

**Domain Methods**:
```csharp
// Event.cs
public Result Approve()
{
    if (Status != EventStatus.UnderReview)
        return Result.Failure("Only events under review can be approved");

    Status = EventStatus.Published;
    RaiseDomainEvent(new EventApprovedEvent(Id, DateTime.UtcNow));
    return Result.Success();
}

public Result Reject(string reason)
{
    if (Status != EventStatus.UnderReview)
        return Result.Failure("Only events under review can be rejected");

    Status = EventStatus.Draft; // Allow resubmission
    CancellationReason = reason;
    RaiseDomainEvent(new EventRejectedEvent(Id, reason, DateTime.UtcNow));
    return Result.Success();
}
```

### 4. Hangfire → Scheduled Jobs Only

**Decision**: Use Hangfire for time-based recurring jobs, not event-driven tasks.

**Rationale**:
- Event-driven emails handled by domain event handlers (real-time)
- Hangfire ideal for scheduled batch operations (reminders, status updates)
- Clear separation of concerns

**Jobs**:
- `EventReminderJob` - Send reminders 24h before event (runs hourly)
- `EventStatusUpdateJob` - Activate/complete events (runs hourly)

### 5. Event Handlers → Fail-Silent Pattern

**Decision**: Event handlers log errors but never throw exceptions.

**Rationale**:
- Prevents handler failures from rolling back parent transactions
- Example: RSVP registration should succeed even if email fails
- Resilient system with proper logging/monitoring

**Pattern**:
```csharp
public async Task Handle(RegistrationConfirmedEvent notification, CancellationToken cancellationToken)
{
    try
    {
        // Query data, send email
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to send confirmation email");
        // DO NOT RETHROW - prevents transaction rollback
    }
}
```

---

## Architecture Answers to Key Questions

### Question 1: Domain Event Handlers Location

**Answer**: `Application/Events/EventHandlers/`

- Handlers implement `INotificationHandler<TDomainEvent>` from MediatR
- Auto-discovered by MediatR during `RegisterServicesFromAssembly()`
- No explicit registration in `DependencyInjection.cs` needed

### Question 2: Email Notification Pattern

**Answer**: Handlers inject `IEmailService` and send emails directly.

- No queuing needed - `EmailService` already queues emails internally
- Handlers query repositories to retrieve user/event data
- Bulk sending for event cancellation/postponement (avoid N+1 queries)

### Question 3: Admin Approval Commands

**Answer**: Create `Application/Events/Commands/AdminApproval/` folder.

- `ApproveEventCommand` + Handler
- `RejectEventCommand` + Handler + Validator
- Commands call domain methods (`event.Approve()`, `event.Reject()`)
- Domain events raised, handlers send approval/rejection emails

### Question 4: Hangfire Integration

**Answer**: Jobs in `Infrastructure/BackgroundJobs/`, registered in `Program.cs`.

- Jobs implement interfaces defined in Application layer (`IEventReminderJob`)
- Jobs inject repositories and `IEmailService` via DI
- Failures: Automatic retry (3 attempts), logged via `ILogger`
- Dashboard authorization: Custom filter restricts to admins

### Question 5: Testing Strategy

**Answer**: Unit tests with mocked dependencies, integration tests for end-to-end flows.

- Domain tests: Test `Approve()`/`Reject()` methods in isolation
- Handler tests: Mock `IEmailService`, repositories, verify email sent
- Job tests: Mock repositories, verify queries and email sending
- Integration tests: Use in-memory Hangfire storage, test full flows

---

## Folder Structure (Complete)

```
src/LankaConnect.Application/Events/
├── Commands/
│   └── AdminApproval/
│       ├── ApproveEvent/
│       │   ├── ApproveEventCommand.cs
│       │   └── ApproveEventCommandHandler.cs
│       └── RejectEvent/
│           ├── RejectEventCommand.cs
│           ├── RejectEventCommandHandler.cs
│           └── RejectEventCommandValidator.cs
├── EventHandlers/
│   ├── RegistrationConfirmedEventHandler.cs
│   ├── RegistrationCancelledEventHandler.cs
│   ├── EventCancelledEventHandler.cs
│   ├── EventPostponedEventHandler.cs
│   ├── EventApprovedEventHandler.cs
│   └── EventRejectedEventHandler.cs

src/LankaConnect.Domain/Events/
├── DomainEvents/
│   ├── EventApprovedEvent.cs
│   └── EventRejectedEvent.cs
└── Event.cs (add Approve() and Reject() methods)

src/LankaConnect.Infrastructure/
├── BackgroundJobs/
│   ├── EventReminderJob.cs
│   └── EventStatusUpdateJob.cs
└── Email/Templates/
    ├── RegistrationConfirmed.cshtml
    ├── RegistrationCancelled.cshtml
    ├── EventCancelled.cshtml
    ├── EventPostponed.cshtml
    ├── EventApproved.cshtml
    ├── EventRejected.cshtml
    └── EventReminder.cshtml

src/LankaConnect.API/
├── Controllers/EventsController.cs (add ApproveEvent, RejectEvent endpoints)
└── Authentication/HangfireDashboardAuthorizationFilter.cs

tests/LankaConnect.Application.Tests/Events/
├── Domain/EventApprovalTests.cs
├── EventHandlers/ (6 test files)
└── Commands/AdminApproval/ (2 test files)

tests/LankaConnect.Infrastructure.Tests/BackgroundJobs/
├── EventReminderJobTests.cs
└── EventStatusUpdateJobTests.cs

tests/LankaConnect.CleanIntegrationTests/
├── Events/RegistrationEmailNotificationTests.cs
├── Controllers/EventsControllerApprovalTests.cs
└── BackgroundJobs/HangfireJobsIntegrationTests.cs
```

---

## Implementation Timeline

### Day 1: Domain Event Dispatching + RSVP Confirmations (4 hours)
- Setup: Add `IMediator` to `AppDbContext`, dispatch domain events after save
- Create `RegistrationConfirmedEventHandler`
- Create email template
- Integration test

### Day 2: Remaining RSVP Email Handlers (6 hours)
- `RegistrationCancelledEventHandler`
- `EventCancelledEventHandler` (bulk notification)
- `EventPostponedEventHandler` (bulk notification)
- Email templates

### Day 3: Admin Approval - Domain & Application (4 hours)
- Domain: `Approve()` and `Reject()` methods + domain events
- Application: `ApproveEventCommand`, `RejectEventCommand` + handlers
- Event handlers: `EventApprovedEventHandler`, `EventRejectedEventHandler`

### Day 4: Admin Approval - API & Hangfire Setup (4 hours)
- API: `ApproveEvent`, `RejectEvent` endpoints with admin authorization
- Install Hangfire packages
- Configure Hangfire with PostgreSQL storage
- Create dashboard authorization filter

### Day 5: Hangfire Background Jobs (8 hours)
- `EventReminderJob` implementation + tests
- `EventStatusUpdateJob` implementation + tests
- Add repository methods for job queries
- Register recurring jobs in `Program.cs`
- Integration testing

**Total: 26 hours core implementation + 14 hours testing/documentation = 40 hours**

---

## Integration Points

### Domain Event Flow
```
Event.Register(userId)
  → RaiseDomainEvent(RegistrationConfirmedEvent)
  → UnitOfWork.CommitAsync()
    → AppDbContext.SaveChangesAsync()
      → DispatchDomainEventsAsync()
        → IMediator.Publish(RegistrationConfirmedEvent)
          → RegistrationConfirmedEventHandler.Handle()
            → IEmailService.SendTemplatedEmailAsync()
              → EmailQueueProcessor sends via SMTP (async)
```

### Admin Approval Flow
```
POST /api/events/{id}/approve [Authorize(AdminOnly)]
  → ApproveEventCommand
    → ApproveEventCommandHandler
      → event.Approve() (domain method)
        → RaiseDomainEvent(EventApprovedEvent)
      → UnitOfWork.CommitAsync()
        → EventApprovedEventHandler
          → Send approval email to organizer
```

### Hangfire Job Flow
```
Program.cs registers RecurringJob.AddOrUpdate(EventReminderJob, Cron.Hourly)
  → Hangfire Server polls every hour
    → EventReminderJob.SendEventRemindersAsync()
      → Query events starting in 24h
      → Get registrations + users (batch queries)
      → Send bulk emails via IEmailService
```

---

## Dependencies Summary

| Layer | Dependencies |
|-------|-------------|
| **Domain** | None (pure domain logic) |
| **Application** | Domain, IEmailService, Repositories, MediatR |
| **Infrastructure** | Domain, Application (interfaces), Hangfire, EF Core, SMTP |
| **API** | Application, Infrastructure (DI registration) |

**Key Principle**: Dependency Inversion
- Application defines interfaces (`IEmailService`, `IEventReminderJob`)
- Infrastructure implements interfaces
- Domain has ZERO dependencies on other layers

---

## Package Additions

**Add to `Directory.Packages.props`**:
```xml
<PackageVersion Include="Hangfire.AspNetCore" Version="1.8.14" />
<PackageVersion Include="Hangfire.PostgreSql" Version="1.20.9" />
```

**Add to `Infrastructure.csproj`**:
```xml
<PackageReference Include="Hangfire.AspNetCore" />
<PackageReference Include="Hangfire.PostgreSql" />
```

---

## Configuration Changes

**Infrastructure/DependencyInjection.cs**:
```csharp
// Add Hangfire with PostgreSQL storage
services.AddHangfire(config => { /* ... */ });
services.AddHangfireServer(options => { /* ... */ });

// Register job implementations
services.AddScoped<IEventReminderJob, EventReminderJob>();
services.AddScoped<IEventStatusUpdateJob, EventStatusUpdateJob>();
```

**API/Program.cs**:
```csharp
// Add Hangfire dashboard with admin authorization
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});

// Schedule recurring jobs
RecurringJob.AddOrUpdate<IEventReminderJob>(...);
RecurringJob.AddOrUpdate<IEventStatusUpdateJob>(...);
```

---

## Repository Methods to Add

**IEventRepository**:
```csharp
Task<IEnumerable<Event>> GetEventsStartingBetweenAsync(
    DateTime startDate, DateTime endDate, CancellationToken cancellationToken);

Task<IEnumerable<Event>> GetPublishedEventsStartingBeforeAsync(
    DateTime date, CancellationToken cancellationToken);

Task<IEnumerable<Event>> GetActiveEventsEndingBeforeAsync(
    DateTime date, CancellationToken cancellationToken);
```

**IUserRepository**:
```csharp
Task<IEnumerable<User>> GetByIdsAsync(
    IEnumerable<Guid> userIds, CancellationToken cancellationToken);
```

---

## Email Templates Required

1. **RegistrationConfirmed** - Sent to attendee when they RSVP
2. **RegistrationCancelled** - Sent to attendee when they cancel RSVP
3. **EventCancelled** - Sent to all attendees when event is cancelled
4. **EventPostponed** - Sent to all attendees when event is postponed
5. **EventApproved** - Sent to organizer when admin approves event
6. **EventRejected** - Sent to organizer when admin rejects event
7. **EventReminder** - Sent to all attendees 24h before event (Hangfire job)

**Storage**: Database-backed in `communications.email_templates` table.

---

## Testing Coverage

- **Domain Tests**: 20 tests (Event.Approve(), Event.Reject(), business rules)
- **Application Handler Tests**: 24 tests (6 handlers × 4 tests each)
- **Application Command Tests**: 10 tests (approve/reject commands)
- **Infrastructure Job Tests**: 10 tests (reminder job, status update job)
- **Integration Tests**: 15 tests (end-to-end flows, API endpoints)

**Total**: ~79 new tests

**Coverage Target**: > 90% maintained

---

## Security Considerations

1. **Admin Approval Endpoints**: `[Authorize(Policy = "AdminOnly")]` required
2. **Hangfire Dashboard**: Restricted to authenticated admins via custom filter
3. **Email Content Sanitization**: User input (event titles, names) sanitized in templates
4. **Audit Logging**: All approval/rejection actions logged with admin user ID

---

## Performance Considerations

1. **Bulk Email Sending**: Use `SendBulkEmailAsync()` for event cancellation/postponement
2. **Batch Queries**: `GetByIdsAsync()` to avoid N+1 queries in bulk notifications
3. **Hangfire Worker Count**: Default 5 workers, adjust based on load
4. **Database Indexes**: Ensure indexes on `events.start_date`, `events.status`, `registrations.event_id`

---

## Monitoring & Observability

1. **Structured Logging**: All handlers and jobs log via `ILogger`
2. **Hangfire Dashboard**: Monitor job execution, success/failure rates at `/hangfire`
3. **Email Delivery Tracking**: Query `communications.email_messages` table for failed sends
4. **Domain Event Metrics**: Log event publishing and handler execution times

---

## Success Criteria

- [ ] All 4 RSVP email handlers implemented and tested
- [ ] Admin approval workflow functional (approve/reject endpoints with AdminOnly policy)
- [ ] Hangfire dashboard accessible to admins at `/hangfire`
- [ ] EventReminderJob runs hourly and sends reminders 24h before events
- [ ] EventStatusUpdateJob runs hourly and transitions event statuses
- [ ] All 7 email templates created and seeded in database
- [ ] Zero compilation errors maintained
- [ ] Test coverage > 90%
- [ ] All integration tests passing
- [ ] Documentation complete (architecture guide, checklist, diagrams)

---

## Next Steps

1. **Review Documents**:
   - `Epic2-Phase5-Architecture-Guide.md` (comprehensive technical guidance)
   - `Epic2-Phase5-Implementation-Checklist.md` (step-by-step tasks)
   - `Epic2-Phase5-Architecture-Diagrams.md` (visual flows)
   - `Epic2-Phase5-Summary.md` (this document)

2. **Create Task Breakdown**: Add tasks to project tracker based on checklist

3. **Begin Implementation**: Start with Day 1 (Domain Event Dispatching)

4. **Continuous Testing**: Maintain zero-tolerance for compilation errors, > 90% test coverage

5. **Deploy**: After testing, deploy to staging → production

---

## Document References

- **Architecture Guide**: `docs/Epic2-Phase5-Architecture-Guide.md` (49 pages)
- **Implementation Checklist**: `docs/Epic2-Phase5-Implementation-Checklist.md` (23 steps)
- **Architecture Diagrams**: `docs/Epic2-Phase5-Architecture-Diagrams.md` (10 diagrams)
- **Progress Tracker**: `docs/PROGRESS_TRACKER.md` (update after completion)

---

**Document Status**: Executive Summary
**Date**: 2025-11-02
**Approved By**: System Architect
**Ready for Implementation**: Yes
