# Epic 2 Phase 5 - Architecture Diagrams

**Project**: LankaConnect
**Phase**: Epic 2 Phase 5 (Advanced Features)
**Date**: 2025-11-02

---

## 1. Domain Event Publishing Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                                     │
│                                                                           │
│  ┌──────────────────────┐                                                │
│  │   Event Aggregate    │                                                │
│  │  (e.g., Register)    │                                                │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ RaiseDomainEvent()   │  Stores event in _domainEvents collection     │
│  │ RegistrationConfirmed│                                                │
│  └──────────────────────┘                                                │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
                          │
                          │ Domain events stored in memory
                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│                                                                           │
│  ┌──────────────────────┐                                                │
│  │ UnitOfWork.CommitAsync│                                               │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ AppDbContext         │  1. SaveChangesAsync() - Persist to DB        │
│  │ .SaveChangesAsync()  │  2. DispatchDomainEventsAsync() - Publish     │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ IMediator.Publish()  │  Publishes each domain event                  │
│  └──────────┬───────────┘                                                │
│                                                                           │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              │ MediatR routes to registered handlers
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                                   │
│                                                                           │
│  ┌────────────────────────────────────────────────────────────┐         │
│  │ RegistrationConfirmedEventHandler                          │         │
│  │ : INotificationHandler<RegistrationConfirmedEvent>         │         │
│  └──────────┬─────────────────────────────────────────────────┘         │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 1. Query User        │  IUserRepository.GetByIdAsync()               │
│  │ 2. Query Event       │  IEventRepository.GetByIdAsync()              │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 3. Build Parameters  │  { UserName, EventTitle, etc. }               │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 4. Send Email        │  IEmailService.SendTemplatedEmailAsync()      │
│  └──────────┬───────────┘                                                │
│                                                                           │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│                                                                           │
│  ┌──────────────────────┐                                                │
│  │ EmailService         │  1. Create EmailMessage entity                │
│  │ .SendTemplatedEmail  │  2. Save as "Queued" status                   │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ EmailQueueProcessor  │  Background service polls for queued emails   │
│  │ (Background Service) │  Sends via SMTP                               │
│  └──────────────────────┘                                                │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Admin Approval Workflow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            API LAYER                                     │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ EventsController.ApproveEvent(Guid id)                   │           │
│  │ [Authorize(Policy = "AdminOnly")]                        │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ Mediator.Send()      │  Send ApproveEventCommand                     │
│  └──────────┬───────────┘                                                │
│                                                                           │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                                   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ ApproveEventCommandHandler                               │           │
│  │ : ICommandHandler<ApproveEventCommand>                   │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 1. Get Event         │  IEventRepository.GetByIdAsync()              │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 2. event.Approve()   │  DOMAIN METHOD                                │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 3. UnitOfWork.Commit │  Persist changes + Dispatch events            │
│  └──────────┬───────────┘                                                │
│                                                                           │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                                     │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ Event.Approve()                                          │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ Business Rules       │  1. Check Status == UnderReview               │
│  └──────────┬───────────┘  2. Return failure if invalid                 │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ State Transition     │  Status = Published                           │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ Raise Domain Event   │  RaiseDomainEvent(EventApprovedEvent)         │
│  └──────────┬───────────┘                                                │
│                                                                           │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              │ (Domain event dispatched after commit)
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                                   │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ EventApprovedEventHandler                                │           │
│  │ : INotificationHandler<EventApprovedEvent>               │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 1. Get Organizer     │  IUserRepository.GetByIdAsync(OrganizerId)    │
│  │ 2. Get Event         │  IEventRepository.GetByIdAsync(EventId)       │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 3. Send Email        │  IEmailService.SendTemplatedEmailAsync()      │
│  │    to Organizer      │  Template: "EventApproved"                    │
│  └──────────────────────┘                                                │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Hangfire Background Jobs Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          API STARTUP (Program.cs)                        │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ Register Recurring Jobs                                  │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ├────────────────────┬────────────────────────────┐          │
│             ▼                    ▼                            ▼          │
│  ┌─────────────────┐  ┌─────────────────┐       ┌─────────────────┐    │
│  │ EventReminder   │  │ EventStatus     │  ...  │ Future Jobs     │    │
│  │ Job (Hourly)    │  │ Update (Hourly) │       │                 │    │
│  └─────────────────┘  └─────────────────┘       └─────────────────┘    │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
                          │
                          │ Hangfire Server polls for scheduled jobs
                          ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      HANGFIRE SERVER                                     │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ Worker Threads (Default: 5)                              │           │
│  │  - Poll every 15 seconds for due jobs                    │           │
│  │  - Execute jobs in background                            │           │
│  │  - Automatic retry on failure (3 attempts)               │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ Hangfire Dashboard   │  /hangfire URL - Admin access only            │
│  │ - Job monitoring     │                                                │
│  │ - Manual triggers    │                                                │
│  │ - Retry management   │                                                │
│  └──────────────────────┘                                                │
│                                                                           │
└─────────────┼───────────────────────────────────────────────────────────┘
              │
              │ Execute job
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│                                                                           │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ EventReminderJob.SendEventRemindersAsync()               │           │
│  └──────────┬───────────────────────────────────────────────┘           │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 1. Query Events      │  GetEventsStartingBetweenAsync(now, now+24h)  │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 2. Get Registrations │  For each event: GetByEventIdAsync()          │
│  │    (Confirmed)       │                                                │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 3. Batch Fetch Users │  GetByIdsAsync(userIds)                       │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 4. Build Email Batch │  Create EmailMessageDto[] for all attendees   │
│  └──────────┬───────────┘                                                │
│             │                                                             │
│             ▼                                                             │
│  ┌──────────────────────┐                                                │
│  │ 5. Send Bulk Emails  │  IEmailService.SendBulkEmailAsync()           │
│  └──────────────────────┘                                                │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

**EventStatusUpdateJob Flow**:
```
EventStatusUpdateJob.UpdateEventStatusAsync()
  │
  ├─> Query Published Events (StartDate <= now)
  │   └─> Call event.ActivateEvent() for each
  │
  ├─> Query Active Events (EndDate <= now)
  │   └─> Call event.Complete() for each
  │
  └─> UnitOfWork.CommitAsync() - Persist all status changes
```

---

## 4. Clean Architecture Layer Dependencies

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            API LAYER                                     │
│  - Controllers (EventsController)                                        │
│  - Program.cs (Hangfire setup)                                           │
│  - Authentication filters                                                │
│                                                                           │
│  Dependencies: Application, Infrastructure (DI only)                     │
└─────────────┬───────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      APPLICATION LAYER                                   │
│  - Commands (ApproveEvent, RejectEvent)                                  │
│  - Queries (existing)                                                    │
│  - Event Handlers (RegistrationConfirmedEventHandler, etc.)             │
│  - Interfaces (IEmailService, IEventReminderJob, etc.)                  │
│                                                                           │
│  Dependencies: Domain (entities, value objects, domain events)           │
└─────────────┬───────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                      INFRASTRUCTURE LAYER                                │
│  - EmailService (implements IEmailService)                               │
│  - EventReminderJob (implements IEventReminderJob)                       │
│  - EventStatusUpdateJob (implements IEventStatusUpdateJob)               │
│  - Repositories (EventRepository, UserRepository)                        │
│  - AppDbContext (domain event dispatching)                               │
│  - Hangfire configuration                                                │
│                                                                           │
│  Dependencies: Domain, Application (interfaces only)                     │
└─────────────┬───────────────────────────────────────────────────────────┘
              │
              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN LAYER                                     │
│  - Event aggregate (with Approve/Reject methods)                         │
│  - Domain events (RegistrationConfirmedEvent, EventApprovedEvent, etc.) │
│  - Value objects (EventTitle, EventLocation, etc.)                       │
│  - Enums (EventStatus)                                                   │
│                                                                           │
│  Dependencies: NONE (pure domain logic)                                  │
└─────────────────────────────────────────────────────────────────────────┘
```

**Key Principle**: **Dependency Inversion**
- Application defines interfaces (IEmailService, IEventReminderJob)
- Infrastructure implements interfaces
- Domain has zero dependencies on other layers

---

## 5. Email Notification Flow Matrix

| Trigger Event | Handler | Template | Recipients | Timing |
|--------------|---------|----------|------------|--------|
| **User Registers** | RegistrationConfirmedEventHandler | RegistrationConfirmed | Single attendee | Real-time (via domain event) |
| **User Cancels RSVP** | RegistrationCancelledEventHandler | RegistrationCancelled | Single attendee | Real-time (via domain event) |
| **Event Cancelled** | EventCancelledEventHandler | EventCancelled | All confirmed attendees | Real-time (via domain event, bulk send) |
| **Event Postponed** | EventPostponedEventHandler | EventPostponed | All confirmed attendees | Real-time (via domain event, bulk send) |
| **Event Approved** | EventApprovedEventHandler | EventApproved | Organizer | Real-time (via domain event) |
| **Event Rejected** | EventRejectedEventHandler | EventRejected | Organizer | Real-time (via domain event) |
| **Event Starting in 24h** | EventReminderJob | EventReminder | All confirmed attendees | Scheduled (Hangfire hourly) |

---

## 6. Hangfire Job Scheduling Timeline

```
Time:  00:00  01:00  02:00  03:00  ... 23:00  00:00
        │      │      │      │           │      │
        ▼      ▼      ▼      ▼           ▼      ▼
       ┌──────────────────────────────────────────┐
       │   EventReminderJob                       │  Runs every hour
       │   - Query events starting in 24h         │  (Cron.Hourly)
       │   - Send reminder emails to attendees    │
       └──────────────────────────────────────────┘

       ┌──────────────────────────────────────────┐
       │   EventStatusUpdateJob                   │  Runs every hour
       │   - Activate events (Published → Active) │  (Cron.Hourly)
       │   - Complete events (Active → Completed) │
       └──────────────────────────────────────────┘
```

**Performance Considerations**:
- Jobs run in parallel (separate worker threads)
- Each job completes in < 5 seconds for typical load (100 events/hour)
- Automatic retry on failure (3 attempts)
- Failed jobs logged and visible in dashboard

---

## 7. Database Schema Changes

### New Tables (Hangfire)
```
hangfire.job
hangfire.jobparameter
hangfire.jobqueue
hangfire.server
hangfire.state
hangfire.hash
hangfire.list
hangfire.set
hangfire.counter
```
(Created automatically by Hangfire on first run)

### Modified Tables
```
events.events
  - No schema changes needed (Approve/Reject use existing fields)

communications.email_messages
  - No schema changes needed (queued emails handled by existing EmailQueueProcessor)

communications.email_templates
  - New templates added via seed data:
    * RegistrationConfirmed
    * RegistrationCancelled
    * EventCancelled
    * EventPostponed
    * EventApproved
    * EventRejected
    * EventReminder
```

---

## 8. Testing Pyramid

```
                          ┌───────────────┐
                          │  Integration  │  EventsControllerApprovalTests
                          │     Tests     │  HangfireJobsIntegrationTests
                          │   (10 tests)  │  RegistrationEmailNotificationTests
                          └───────┬───────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │    Application Tests      │  Command handler tests
                    │       (40 tests)          │  Event handler tests
                    │  - Command handlers       │  Query tests
                    │  - Event handlers         │
                    │  - Validators             │
                    └─────────────┬─────────────┘
                                  │
                ┌─────────────────┴─────────────────┐
                │        Domain Tests               │  Event.Approve() tests
                │         (20 tests)                │  Event.Reject() tests
                │  - Event aggregate methods        │  Domain event tests
                │  - Business rule validation       │
                └───────────────────────────────────┘
```

**Test Coverage Target**: > 90%
- Domain: 100% (pure logic, fully testable)
- Application: 95% (handlers with mocked dependencies)
- Infrastructure: 80% (integration tests for jobs)
- API: 85% (controller endpoint tests)

---

## 9. Error Handling Strategy

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ERROR HANDLING LAYERS                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  API Layer (Controllers)                                                 │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ - Return 400 BadRequest for validation failures          │           │
│  │ - Return 404 NotFound for missing resources              │           │
│  │ - Return 403 Forbidden for authorization failures        │           │
│  │ - Global exception handler catches unhandled exceptions  │           │
│  └──────────────────────────────────────────────────────────┘           │
│                                                                           │
│  Application Layer (Handlers)                                            │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ - Command handlers return Result<T> (success/failure)    │           │
│  │ - Event handlers catch exceptions, log, DO NOT RETHROW   │           │
│  │ - ValidationPipelineBehavior validates inputs            │           │
│  └──────────────────────────────────────────────────────────┘           │
│                                                                           │
│  Infrastructure Layer (Services)                                         │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ - EmailService returns Result (success/failure)          │           │
│  │ - Hangfire jobs catch exceptions, log, retry (3x)        │           │
│  │ - Repository methods throw on database errors            │           │
│  └──────────────────────────────────────────────────────────┘           │
│                                                                           │
│  Domain Layer (Entities)                                                 │
│  ┌──────────────────────────────────────────────────────────┐           │
│  │ - Business rule violations return Result.Failure()       │           │
│  │ - No exceptions thrown for domain logic                  │           │
│  │ - Invariants enforced in constructors                    │           │
│  └──────────────────────────────────────────────────────────┘           │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

**Critical Rule**: **Event handlers NEVER throw exceptions**
- Reason: Prevents transaction rollback in parent operation
- Example: RSVP registration should succeed even if email fails to send
- Solution: Log error, mark email as failed, continue

---

## 10. Monitoring Dashboard URLs

```
Production URLs:
├── Application:        https://lankaconnect.com
├── API:                https://api.lankaconnect.com
├── Swagger:            https://api.lankaconnect.com/swagger
└── Hangfire Dashboard: https://api.lankaconnect.com/hangfire
    └── Authorization: Admin role required

Development URLs:
├── Application:        http://localhost:3000
├── API:                http://localhost:5000
├── Swagger:            http://localhost:5000/swagger
└── Hangfire Dashboard: http://localhost:5000/hangfire
```

**Hangfire Dashboard Features**:
- View scheduled jobs and their next run times
- Monitor job execution history (success/failure)
- Manually trigger jobs for testing
- Retry failed jobs
- View detailed job logs and parameters

---

## Conclusion

These diagrams illustrate the complete architecture for Epic 2 Phase 5:
- **Domain event flow**: Real-time notifications triggered by business operations
- **Admin approval workflow**: Secure approval process with email notifications
- **Hangfire background jobs**: Scheduled tasks for reminders and status updates
- **Clean Architecture**: Proper separation of concerns across layers
- **Error handling**: Robust strategies at each layer to prevent cascading failures

All components are designed to be testable, maintainable, and follow existing codebase patterns.

---

**Document Status**: Architecture Diagrams
**Last Updated**: 2025-11-02
