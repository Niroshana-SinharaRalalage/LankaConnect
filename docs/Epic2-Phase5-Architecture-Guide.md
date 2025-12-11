# Epic 2 Phase 5 - Advanced Features: Architecture Guide

**Project**: LankaConnect (Clean Architecture + DDD)
**Date**: 2025-11-02
**Author**: System Architect
**Phase**: Epic 2 Phase 5 (Advanced Features)

---

## Executive Summary

This document provides comprehensive architectural guidance for implementing Epic 2 Phase 5 features:
1. RSVP Email Notifications (domain event handlers)
2. Admin Approval Workflow (commands + domain events)
3. Hangfire Background Jobs (recurring jobs for reminders and status updates)

All recommendations follow Clean Architecture, DDD principles, and existing codebase patterns (CQRS with MediatR, Result pattern, domain event publishing).

---

## Table of Contents

1. [Domain Event Handlers Architecture](#1-domain-event-handlers-architecture)
2. [Email Notification Pattern](#2-email-notification-pattern)
3. [Admin Approval Commands](#3-admin-approval-commands)
4. [Hangfire Integration](#4-hangfire-integration)
5. [Testing Strategy](#5-testing-strategy)
6. [Implementation Folder Structure](#6-implementation-folder-structure)
7. [Integration Points](#7-integration-points)

---

## 1. Domain Event Handlers Architecture

### 1.1 Location and Naming

**Recommendation**: Place handlers in **Application layer** at `Application/Events/EventHandlers/`

**Rationale**:
- Domain events are raised by domain aggregates (already implemented in Event aggregate)
- Handlers contain **application concerns** (email sending, external service calls)
- Handlers orchestrate **cross-aggregate operations** (querying users, sending notifications)
- This maintains domain layer purity (no infrastructure dependencies)

**Pattern**: `INotificationHandler<TDomainEvent>` from MediatR

**File Structure**:
```
src/LankaConnect.Application/Events/EventHandlers/
├── RegistrationConfirmedEventHandler.cs
├── RegistrationCancelledEventHandler.cs
├── EventCancelledEventHandler.cs
├── EventPostponedEventHandler.cs
├── EventApprovedEventHandler.cs (Phase 5 - Admin Approval)
└── EventRejectedEventHandler.cs (Phase 5 - Admin Approval)
```

### 1.2 Handler Implementation Pattern

**Example: RegistrationConfirmedEventHandler**

```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationConfirmedEvent to send confirmation email to attendee
/// </summary>
public class RegistrationConfirmedEventHandler : INotificationHandler<RegistrationConfirmedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(RegistrationConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling RegistrationConfirmedEvent for Event {EventId}, User {UserId}",
            notification.EventId, notification.AttendeeId);

        try
        {
            // Retrieve user and event data
            var user = await _userRepository.GetByIdAsync(notification.AttendeeId, cancellationToken);
            var @event = await _eventRepository.GetByIdAsync(notification.EventId, cancellationToken);

            if (user == null || @event == null)
            {
                _logger.LogWarning("Unable to send registration confirmation - User or Event not found");
                return; // Fail silently - don't throw exceptions in event handlers
            }

            // Prepare template parameters
            var parameters = new Dictionary<string, object>
            {
                { "UserName", user.FirstName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("dddd, MMMM d, yyyy h:mm tt") },
                { "EventLocation", @event.Location?.Address.ToString() ?? "Virtual Event" },
                { "RegistrationQuantity", notification.Quantity },
                { "EventDetailsUrl", $"https://lankaconnect.com/events/{@event.Id}" }
            };

            // Send templated email (fire-and-forget for now)
            var result = await _emailService.SendTemplatedEmailAsync(
                "RegistrationConfirmed",
                user.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Failed to send registration confirmation email: {Error}", result.Error);
                // Do NOT throw - event handlers should not fail transactions
            }
            else
            {
                _logger.LogInformation("Registration confirmation email sent to {Email}", user.Email.Value);
            }
        }
        catch (Exception ex)
        {
            // Log but do not rethrow - prevent event handler from failing parent transaction
            _logger.LogError(ex, "Error handling RegistrationConfirmedEvent for Event {EventId}",
                notification.EventId);
        }
    }
}
```

**Key Pattern Elements**:
1. Inject `IEmailService`, repositories, and `ILogger`
2. Retrieve necessary data (user, event) using repositories
3. Build email template parameters
4. Call `SendTemplatedEmailAsync()` with template name
5. **Error handling**: Log failures but DO NOT throw exceptions (prevents transaction rollback)
6. Fail silently for missing data (idempotency consideration)

### 1.3 Domain Event Publishing Mechanism

**Current Implementation**: Events are raised via `BaseEntity.RaiseDomainEvent()` and stored in `_domainEvents` collection.

**Publishing Point**: Domain events must be published **after CommitAsync()** to ensure database consistency.

**Recommendation**: Add domain event dispatcher to `UnitOfWork.CommitAsync()` or `AppDbContext.SaveChangesAsync()`.

**Implementation Location**: `Infrastructure/Data/AppDbContext.cs`

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Dispatch domain events AFTER successful save
    var result = await base.SaveChangesAsync(cancellationToken);

    // Publish domain events via MediatR
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

**Note**: Requires injecting `IMediator` into `AppDbContext` (pattern already used in Application layer).

### 1.4 Wiring Up Handlers in DependencyInjection

**No explicit registration needed!** MediatR's `RegisterServicesFromAssembly()` automatically discovers all `INotificationHandler<>` implementations.

**Existing Configuration** (`Application/DependencyInjection.cs`):
```csharp
services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(assembly); // Scans for INotificationHandler<>
});
```

This will automatically register:
- `RegistrationConfirmedEventHandler`
- `RegistrationCancelledEventHandler`
- `EventCancelledEventHandler`
- `EventPostponedEventHandler`
- etc.

**Action Required**: No changes to `DependencyInjection.cs` - handlers are auto-discovered.

---

## 2. Email Notification Pattern

### 2.1 Direct Email Sending vs Background Queue

**Recommendation**: **Direct email sending** in Phase 5 (simple implementation).

**Rationale**:
1. Your `EmailService` already has queue support (`EmailQueueProcessor` background service)
2. Existing pattern: `EmailService.SendTemplatedEmailAsync()` creates domain entity, marks as queued, saves to DB
3. `EmailQueueProcessor` (already registered in `Infrastructure/DependencyInjection.cs` line 169) processes queued emails
4. No additional complexity needed - existing infrastructure handles async processing

**Pattern**:
```csharp
// In event handler
var result = await _emailService.SendTemplatedEmailAsync(
    templateName: "RegistrationConfirmed",
    recipientEmail: user.Email.Value,
    parameters: templateParams,
    cancellationToken);
```

**Behind the scenes** (already implemented):
1. `EmailService.SendTemplatedEmailAsync()` creates `EmailMessage` domain entity
2. Entity is saved to `communications.email_messages` table with status = "Queued"
3. `EmailQueueProcessor` background service picks up queued emails and sends via SMTP
4. Asynchronous processing without additional job infrastructure

**Alternative (Future Enhancement)**: Use Hangfire for email queue processing if you want more visibility/retry logic.

### 2.2 Email Template Requirements

**Templates Needed** (create in `Infrastructure/Email/Templates/`):

1. **RegistrationConfirmed.cshtml**
   - Parameters: UserName, EventTitle, EventStartDate, EventLocation, RegistrationQuantity, EventDetailsUrl

2. **RegistrationCancelled.cshtml**
   - Parameters: UserName, EventTitle, EventStartDate, CancellationDate

3. **EventCancelled.cshtml**
   - Parameters: UserName, EventTitle, EventStartDate, CancellationReason, OrganizerName

4. **EventPostponed.cshtml**
   - Parameters: UserName, EventTitle, OriginalStartDate, PostponementReason, OrganizerName

5. **EventApproved.cshtml** (Admin Approval)
   - Parameters: OrganizerName, EventTitle, ApprovalDate, EventDetailsUrl

6. **EventRejected.cshtml** (Admin Approval)
   - Parameters: OrganizerName, EventTitle, RejectionReason, RejectionDate

**Template Storage**: Database-backed templates in `communications.email_templates` table.

**Seeding**: Create migration to seed default templates (or admin UI to create them).

### 2.3 Retrieving Event and User Data in Handlers

**Pattern**: Inject repositories and query directly.

```csharp
// Inject repositories
private readonly IUserRepository _userRepository;
private readonly IEventRepository _eventRepository;

// In Handle method
var user = await _userRepository.GetByIdAsync(notification.AttendeeId, cancellationToken);
var @event = await _eventRepository.GetByIdAsync(notification.EventId, cancellationToken);

// For EventCancelled/Postponed (notify all attendees)
var registrations = await _registrationRepository.GetByEventIdAsync(notification.EventId, cancellationToken);
foreach (var registration in registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
{
    var attendee = await _userRepository.GetByIdAsync(registration.UserId, cancellationToken);
    // Send email to attendee
}
```

**Performance Consideration**: For bulk notifications (EventCancelled, EventPostponed), consider:
1. Batch email sending using `IEmailService.SendBulkEmailAsync()`
2. Or enqueue individual emails and let `EmailQueueProcessor` handle async delivery

**Recommendation**: Use bulk sending for event cancellation/postponement to avoid N+1 queries.

```csharp
// EventCancelledEventHandler - Bulk Notification Example
var registrations = await _registrationRepository.GetByEventIdAsync(notification.EventId, cancellationToken);
var confirmedUserIds = registrations
    .Where(r => r.Status == RegistrationStatus.Confirmed)
    .Select(r => r.UserId)
    .ToList();

// Batch fetch users
var users = await _userRepository.GetByIdsAsync(confirmedUserIds, cancellationToken);

// Build bulk email messages
var emailMessages = users.Select(user => new EmailMessageDto
{
    ToEmail = user.Email.Value,
    ToName = $"{user.FirstName} {user.LastName}",
    Subject = $"Event Cancelled: {@event.Title.Value}",
    // ... template rendering
}).ToList();

// Send bulk
await _emailService.SendBulkEmailAsync(emailMessages, cancellationToken);
```

---

## 3. Admin Approval Commands

### 3.1 Folder Structure

**Recommendation**: Create new folder `Application/Events/Commands/AdminApproval/`

```
src/LankaConnect.Application/Events/Commands/AdminApproval/
├── ApproveEvent/
│   ├── ApproveEventCommand.cs
│   ├── ApproveEventCommandHandler.cs
│   └── ApproveEventCommandValidator.cs
└── RejectEvent/
    ├── RejectEventCommand.cs
    ├── RejectEventCommandHandler.cs
    └── RejectEventCommandValidator.cs
```

### 3.2 Domain Methods for Approval

**Recommendation**: Add approval/rejection logic to **Event aggregate** (domain layer).

**Rationale**: Approval is a state transition with business rules (only UnderReview events can be approved/rejected).

**Implementation** (`Domain/Events/Event.cs`):

```csharp
public Result Approve()
{
    if (Status != EventStatus.UnderReview)
        return Result.Failure("Only events under review can be approved");

    Status = EventStatus.Published; // Approved events become published
    MarkAsUpdated();

    // Raise domain event
    RaiseDomainEvent(new EventApprovedEvent(Id, DateTime.UtcNow));

    return Result.Success();
}

public Result Reject(string reason)
{
    if (Status != EventStatus.UnderReview)
        return Result.Failure("Only events under review can be rejected");

    if (string.IsNullOrWhiteSpace(reason))
        return Result.Failure("Rejection reason is required");

    Status = EventStatus.Draft; // Rejected events return to draft for corrections
    CancellationReason = reason.Trim(); // Reuse field for rejection reason
    MarkAsUpdated();

    // Raise domain event
    RaiseDomainEvent(new EventRejectedEvent(Id, reason.Trim(), DateTime.UtcNow));

    return Result.Success();
}
```

**New Domain Events** (`Domain/Events/DomainEvents/`):

```csharp
public record EventApprovedEvent(
    Guid EventId,
    DateTime ApprovedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record EventRejectedEvent(
    Guid EventId,
    string Reason,
    DateTime RejectedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

### 3.3 Command Implementation

**ApproveEventCommand.cs**:
```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Commands.AdminApproval.ApproveEvent;

public record ApproveEventCommand(Guid EventId) : ICommand;
```

**ApproveEventCommandHandler.cs**:
```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Commands.AdminApproval.ApproveEvent;

public class ApproveEventCommandHandler : ICommandHandler<ApproveEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveEventCommandHandler> _logger;

    public ApproveEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApproveEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(ApproveEventCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Approving event {EventId}", request.EventId);

        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        var approveResult = @event.Approve();
        if (approveResult.IsFailure)
            return approveResult;

        // EF Core change tracking will automatically update the event
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Event {EventId} approved successfully", request.EventId);
        return Result.Success();
    }
}
```

**Similar pattern for RejectEventCommand** with `reason` parameter.

### 3.4 Event Handler for Email Notifications

**EventApprovedEventHandler.cs**:
```csharp
public class EventApprovedEventHandler : INotificationHandler<EventApprovedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventApprovedEventHandler> _logger;

    public async Task Handle(EventApprovedEvent notification, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdAsync(notification.EventId, cancellationToken);
        var organizer = await _userRepository.GetByIdAsync(@event.OrganizerId, cancellationToken);

        var parameters = new Dictionary<string, object>
        {
            { "OrganizerName", organizer.FirstName },
            { "EventTitle", @event.Title.Value },
            { "ApprovalDate", notification.ApprovedAt.ToString("MMMM d, yyyy") },
            { "EventDetailsUrl", $"https://lankaconnect.com/events/{@event.Id}" }
        };

        await _emailService.SendTemplatedEmailAsync(
            "EventApproved",
            organizer.Email.Value,
            parameters,
            cancellationToken);
    }
}
```

**EventRejectedEventHandler.cs** - similar pattern with rejection reason.

### 3.5 API Endpoints (Controllers)

**EventsController Enhancement** (`API/Controllers/EventsController.cs`):

```csharp
/// <summary>
/// Approve an event (Admin only)
/// </summary>
[HttpPost("{id}/approve")]
[Authorize(Policy = "AdminOnly")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> ApproveEvent(Guid id, CancellationToken cancellationToken)
{
    var command = new ApproveEventCommand(id);
    var result = await Mediator.Send(command, cancellationToken);

    return result.IsSuccess ? Ok() : BadRequest(result.Error);
}

/// <summary>
/// Reject an event (Admin only)
/// </summary>
[HttpPost("{id}/reject")]
[Authorize(Policy = "AdminOnly")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> RejectEvent(Guid id, [FromBody] RejectEventRequest request, CancellationToken cancellationToken)
{
    var command = new RejectEventCommand(id, request.Reason);
    var result = await Mediator.Send(command, cancellationToken);

    return result.IsSuccess ? Ok() : BadRequest(result.Error);
}

public record RejectEventRequest(string Reason);
```

---

## 4. Hangfire Integration

### 4.1 Package Installation

**Add to `Directory.Packages.props`**:
```xml
<ItemGroup>
  <PackageVersion Include="Hangfire.AspNetCore" Version="1.8.14" />
  <PackageVersion Include="Hangfire.PostgreSql" Version="1.20.9" />
</ItemGroup>
```

**Add to `Infrastructure.csproj`**:
```xml
<ItemGroup>
  <PackageReference Include="Hangfire.AspNetCore" />
  <PackageReference Include="Hangfire.PostgreSql" />
</ItemGroup>
```

### 4.2 Configuration Location

**Recommendation**: Configure Hangfire in **Infrastructure layer** (DI) and **API layer** (middleware).

**Infrastructure/DependencyInjection.cs** - Add Hangfire services:
```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing code ...

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
        options.WorkerCount = 5; // Adjust based on workload
        options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
    });

    // Register background job services
    services.AddScoped<IEventReminderJob, EventReminderJob>();
    services.AddScoped<IEventStatusUpdateJob, EventStatusUpdateJob>();

    return services;
}
```

**API/Program.cs** - Add Hangfire middleware:
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
    Cron.Hourly); // Run every hour

RecurringJob.AddOrUpdate<IEventStatusUpdateJob>(
    "update-event-status",
    job => job.UpdateEventStatusAsync(CancellationToken.None),
    Cron.Hourly); // Run every hour
```

### 4.3 Hangfire Dashboard Authorization

**Create Authorization Filter** (`API/Authentication/HangfireDashboardAuthorizationFilter.cs`):

```csharp
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace LankaConnect.API.Authentication;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow access only to authenticated admins
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}
```

### 4.4 Background Job Implementation

**Folder Structure**:
```
src/LankaConnect.Infrastructure/BackgroundJobs/
├── IEventReminderJob.cs
├── EventReminderJob.cs
├── IEventStatusUpdateJob.cs
└── EventStatusUpdateJob.cs
```

**IEventReminderJob.cs** (Application Interface):
```csharp
namespace LankaConnect.Application.Common.Interfaces;

public interface IEventReminderJob
{
    Task SendEventRemindersAsync(CancellationToken cancellationToken);
}
```

**EventReminderJob.cs** (Infrastructure Implementation):
```csharp
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.BackgroundJobs;

public class EventReminderJob : IEventReminderJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<EventReminderJob> _logger;

    public EventReminderJob(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<EventReminderJob> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _userRepository = userRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendEventRemindersAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting event reminder job");

        // Find events starting in 24 hours
        var now = DateTime.UtcNow;
        var reminderWindow = now.AddHours(24);

        var upcomingEvents = await _eventRepository.GetEventsStartingBetweenAsync(
            now, reminderWindow, cancellationToken);

        var publishedEvents = upcomingEvents.Where(e => e.Status == EventStatus.Published).ToList();

        _logger.LogInformation("Found {Count} events requiring reminders", publishedEvents.Count);

        foreach (var @event in publishedEvents)
        {
            try
            {
                // Get all confirmed registrations
                var registrations = await _registrationRepository.GetByEventIdAsync(@event.Id, cancellationToken);
                var confirmedRegistrations = registrations
                    .Where(r => r.Status == RegistrationStatus.Confirmed)
                    .ToList();

                // Batch fetch users
                var userIds = confirmedRegistrations.Select(r => r.UserId).ToList();
                var users = await _userRepository.GetByIdsAsync(userIds, cancellationToken);

                // Build email messages
                var emailMessages = users.Select(user => new EmailMessageDto
                {
                    ToEmail = user.Email.Value,
                    ToName = $"{user.FirstName} {user.LastName}",
                    Subject = $"Reminder: {@event.Title.Value} starts tomorrow",
                    HtmlBody = BuildReminderEmailBody(user, @event)
                }).ToList();

                // Send bulk reminders
                await _emailService.SendBulkEmailAsync(emailMessages, cancellationToken);

                _logger.LogInformation("Sent {Count} reminders for event {EventId}",
                    emailMessages.Count, @event.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminders for event {EventId}", @event.Id);
                // Continue with next event
            }
        }

        _logger.LogInformation("Event reminder job completed");
    }

    private string BuildReminderEmailBody(User user, Event @event)
    {
        // Build simple HTML email (or use template)
        return $@"
            <html>
            <body>
                <h2>Event Reminder</h2>
                <p>Hi {user.FirstName},</p>
                <p>This is a reminder that you're registered for:</p>
                <h3>{@event.Title.Value}</h3>
                <p><strong>When:</strong> {@event.StartDate:dddd, MMMM d, yyyy h:mm tt}</p>
                <p><strong>Where:</strong> {@event.Location?.Address.ToString() ?? "Virtual Event"}</p>
                <p>See you there!</p>
            </body>
            </html>";
    }
}
```

**EventStatusUpdateJob.cs**:
```csharp
public class EventStatusUpdateJob : IEventStatusUpdateJob
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EventStatusUpdateJob> _logger;

    public async Task UpdateEventStatusAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting event status update job");

        var now = DateTime.UtcNow;

        // Find published events that should be activated (start date passed)
        var eventsToActivate = await _eventRepository.GetPublishedEventsStartingBeforeAsync(now, cancellationToken);

        foreach (var @event in eventsToActivate)
        {
            var activateResult = @event.ActivateEvent();
            if (activateResult.IsSuccess)
            {
                _logger.LogInformation("Activated event {EventId}", @event.Id);
            }
        }

        // Find active events that should be completed (end date passed)
        var eventsToComplete = await _eventRepository.GetActiveEventsEndingBeforeAsync(now, cancellationToken);

        foreach (var @event in eventsToComplete)
        {
            @event.Complete();
            _logger.LogInformation("Completed event {EventId}", @event.Id);
        }

        // Commit all status changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Event status update job completed: {ActivatedCount} activated, {CompletedCount} completed",
            eventsToActivate.Count(), eventsToComplete.Count());
    }
}
```

**Required Repository Methods** (add to `IEventRepository`):
```csharp
Task<IEnumerable<Event>> GetEventsStartingBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
Task<IEnumerable<Event>> GetPublishedEventsStartingBeforeAsync(DateTime date, CancellationToken cancellationToken);
Task<IEnumerable<Event>> GetActiveEventsEndingBeforeAsync(DateTime date, CancellationToken cancellationToken);
```

### 4.5 Job Failure Handling

**Hangfire Automatic Retry**: Failed jobs are automatically retried (10 attempts by default).

**Custom Retry Policy** (optional):
```csharp
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Delete)]
public async Task SendEventRemindersAsync(CancellationToken cancellationToken)
{
    // Implementation
}
```

**Logging**: All failures are logged via `ILogger` - monitor via Serilog/Application Insights.

---

## 5. Testing Strategy

### 5.1 Domain Event Handler Tests

**Location**: `tests/LankaConnect.Application.Tests/Events/EventHandlers/`

**Pattern**: Unit tests with mocked dependencies.

**Example**:
```csharp
public class RegistrationConfirmedEventHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<ILogger<RegistrationConfirmedEventHandler>> _loggerMock;
    private readonly RegistrationConfirmedEventHandler _handler;

    public RegistrationConfirmedEventHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _loggerMock = new Mock<ILogger<RegistrationConfirmedEventHandler>>();
        _handler = new RegistrationConfirmedEventHandler(
            _emailServiceMock.Object,
            _userRepositoryMock.Object,
            _eventRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_SendsConfirmationEmail()
    {
        // Arrange
        var @event = CreateTestEvent();
        var user = CreateTestUser();
        var domainEvent = new RegistrationConfirmedEvent(@event.Id, user.Id, 2, DateTime.UtcNow);

        _eventRepositoryMock.Setup(x => x.GetByIdAsync(@event.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _emailServiceMock.Setup(x => x.SendTemplatedEmailAsync(
                "RegistrationConfirmed",
                user.Email.Value,
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(x => x.SendTemplatedEmailAsync(
            "RegistrationConfirmed",
            user.Email.Value,
            It.Is<Dictionary<string, object>>(d =>
                d["EventTitle"].ToString() == @event.Title.Value &&
                d["UserName"].ToString() == user.FirstName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMissingUser_LogsWarningAndDoesNotThrow()
    {
        // Arrange
        var domainEvent = new RegistrationConfirmedEvent(Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow);
        _userRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("User or Event not found")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _emailServiceMock.Verify(x => x.SendTemplatedEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

### 5.2 Admin Approval Command Tests

**Location**: `tests/LankaConnect.Application.Tests/Events/Commands/AdminApproval/`

**Pattern**: Similar to existing command tests (e.g., `PublishEventCommandHandlerTests`).

**Example**:
```csharp
public class ApproveEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithUnderReviewEvent_ApprovesAndPublishesEvent()
    {
        // Arrange
        var @event = Event.Create(...).Value;
        @event.SubmitForReview(); // Status = UnderReview

        var command = new ApproveEventCommand(@event.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Status.Should().Be(EventStatus.Published);
        @event.DomainEvents.Should().ContainSingle(e => e is EventApprovedEvent);
    }

    [Fact]
    public async Task Handle_WithPublishedEvent_ReturnsFailure()
    {
        // Arrange
        var @event = Event.Create(...).Value;
        @event.Publish(); // Status = Published (not UnderReview)

        var command = new ApproveEventCommand(@event.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only events under review can be approved");
    }
}
```

### 5.3 Hangfire Job Tests

**Location**: `tests/LankaConnect.Infrastructure.Tests/BackgroundJobs/`

**Pattern**: Unit tests with mocked repositories and email service.

**Example**:
```csharp
public class EventReminderJobTests
{
    [Fact]
    public async Task SendEventRemindersAsync_WithUpcomingEvents_SendsReminders()
    {
        // Arrange
        var events = new List<Event>
        {
            CreatePublishedEvent(startDate: DateTime.UtcNow.AddHours(23)) // Within 24h window
        };

        _eventRepositoryMock.Setup(x => x.GetEventsStartingBetweenAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        await _job.SendEventRemindersAsync(CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(x => x.SendBulkEmailAsync(
            It.Is<IEnumerable<EmailMessageDto>>(emails => emails.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 5.4 Integration Tests

**Email Sending**: Use in-memory SMTP server (e.g., `Smtp4Dev`) or mock SMTP client.

**Hangfire Jobs**: Use in-memory storage for testing:
```csharp
services.AddHangfire(config => config.UseInMemoryStorage());
```

**Domain Event Publishing**: Test end-to-end flow in integration tests:
```csharp
[Fact]
public async Task RegisterForEvent_PublishesDomainEvent_AndSendsEmail()
{
    // Arrange
    var command = new RsvpToEventCommand(eventId, userId, 2);

    // Act
    await _mediator.Send(command);

    // Assert
    // Verify domain event was raised and handler sent email
    _emailServiceMock.Verify(x => x.SendTemplatedEmailAsync(...), Times.Once);
}
```

---

## 6. Implementation Folder Structure

### 6.1 Complete Folder Tree

```
src/LankaConnect.Application/Events/
├── Commands/
│   ├── AdminApproval/
│   │   ├── ApproveEvent/
│   │   │   ├── ApproveEventCommand.cs
│   │   │   ├── ApproveEventCommandHandler.cs
│   │   │   └── ApproveEventCommandValidator.cs
│   │   └── RejectEvent/
│   │       ├── RejectEventCommand.cs
│   │       ├── RejectEventCommandHandler.cs
│   │       └── RejectEventCommandValidator.cs
│   ├── ... (existing commands)
├── EventHandlers/
│   ├── RegistrationConfirmedEventHandler.cs
│   ├── RegistrationCancelledEventHandler.cs
│   ├── EventCancelledEventHandler.cs
│   ├── EventPostponedEventHandler.cs
│   ├── EventApprovedEventHandler.cs
│   └── EventRejectedEventHandler.cs
└── ... (queries, DTOs, etc.)

src/LankaConnect.Domain/Events/
├── DomainEvents/
│   ├── EventApprovedEvent.cs
│   └── EventRejectedEvent.cs
└── Event.cs (add Approve() and Reject() methods)

src/LankaConnect.Infrastructure/
├── BackgroundJobs/
│   ├── EventReminderJob.cs
│   └── EventStatusUpdateJob.cs
└── Email/
    └── Templates/ (create email templates)

src/LankaConnect.API/Controllers/
└── EventsController.cs (add ApproveEvent and RejectEvent endpoints)

tests/LankaConnect.Application.Tests/Events/
├── EventHandlers/
│   ├── RegistrationConfirmedEventHandlerTests.cs
│   ├── RegistrationCancelledEventHandlerTests.cs
│   ├── EventCancelledEventHandlerTests.cs
│   ├── EventPostponedEventHandlerTests.cs
│   ├── EventApprovedEventHandlerTests.cs
│   └── EventRejectedEventHandlerTests.cs
└── Commands/
    └── AdminApproval/
        ├── ApproveEventCommandHandlerTests.cs
        └── RejectEventCommandHandlerTests.cs

tests/LankaConnect.Infrastructure.Tests/BackgroundJobs/
├── EventReminderJobTests.cs
└── EventStatusUpdateJobTests.cs
```

---

## 7. Integration Points

### 7.1 Domain Event Flow

```
Event Aggregate (Domain Layer)
  └─> Raises Domain Event (e.g., RegistrationConfirmedEvent)
      └─> Stored in _domainEvents collection

UnitOfWork.CommitAsync() (Infrastructure Layer)
  └─> Calls AppDbContext.SaveChangesAsync()
      └─> Dispatches Domain Events via MediatR
          └─> MediatR finds registered INotificationHandler<RegistrationConfirmedEvent>
              └─> RegistrationConfirmedEventHandler.Handle() (Application Layer)
                  └─> Queries repositories (User, Event)
                  └─> Calls IEmailService.SendTemplatedEmailAsync()
                      └─> EmailService creates EmailMessage entity, saves as "Queued"
                          └─> EmailQueueProcessor (background service) picks up and sends via SMTP
```

### 7.2 Admin Approval Flow

```
EventsController.ApproveEvent() (API Layer)
  └─> Sends ApproveEventCommand via MediatR
      └─> ApproveEventCommandHandler.Handle() (Application Layer)
          └─> Calls @event.Approve() (Domain Layer)
              └─> Changes Status to Published
              └─> Raises EventApprovedEvent
          └─> UnitOfWork.CommitAsync()
              └─> Dispatches EventApprovedEvent
                  └─> EventApprovedEventHandler.Handle()
                      └─> Sends approval email to organizer
```

### 7.3 Hangfire Job Flow

```
Program.cs (API Startup)
  └─> Schedules RecurringJob for EventReminderJob (hourly)

Hangfire Server (Infrastructure Layer)
  └─> Every hour, executes EventReminderJob.SendEventRemindersAsync()
      └─> Queries IEventRepository.GetEventsStartingBetweenAsync()
      └─> For each event:
          └─> Queries IRegistrationRepository.GetByEventIdAsync()
          └─> Queries IUserRepository.GetByIdsAsync()
          └─> Calls IEmailService.SendBulkEmailAsync()
```

### 7.4 Dependencies Summary

| Layer | Dependencies |
|-------|-------------|
| **Domain** | None (pure domain logic) |
| **Application** | Domain, IEmailService, Repositories, MediatR |
| **Infrastructure** | Domain, Application interfaces, Hangfire, EF Core |
| **API** | Application, Infrastructure (DI registration) |

---

## 8. Recommended Implementation Order

### Phase 5 Day 1: RSVP Email Notifications (2 days)

1. **Setup Domain Event Dispatching** (2 hours)
   - Modify `AppDbContext.SaveChangesAsync()` to dispatch domain events
   - Inject `IMediator` into `AppDbContext`
   - Test domain event publishing

2. **Create Event Handlers** (4 hours)
   - `RegistrationConfirmedEventHandler`
   - `RegistrationCancelledEventHandler`
   - `EventCancelledEventHandler`
   - `EventPostponedEventHandler`

3. **Create Email Templates** (2 hours)
   - Create 4 Razor email templates
   - Seed templates into database via migration

4. **Unit Tests** (2 hours)
   - Test each event handler with mocked dependencies
   - Test error handling (missing user/event)

### Phase 5 Day 2: Admin Approval Workflow (1 day)

1. **Domain Layer** (2 hours)
   - Add `Approve()` and `Reject()` methods to Event aggregate
   - Create `EventApprovedEvent` and `EventRejectedEvent`
   - Unit test domain methods

2. **Application Layer** (2 hours)
   - Create `ApproveEventCommand` + Handler
   - Create `RejectEventCommand` + Handler
   - Create `EventApprovedEventHandler`
   - Create `EventRejectedEventHandler`
   - Unit tests for commands and handlers

3. **API Layer** (1 hour)
   - Add `ApproveEvent` and `RejectEvent` endpoints to `EventsController`
   - Add `[Authorize(Policy = "AdminOnly")]` attributes

4. **Email Templates** (1 hour)
   - Create approval/rejection email templates
   - Seed into database

### Phase 5 Days 3-4: Hangfire Background Jobs (2 days)

1. **Package Installation & Configuration** (2 hours)
   - Install Hangfire packages
   - Configure Hangfire in `Infrastructure/DependencyInjection.cs`
   - Add Hangfire middleware to `Program.cs`
   - Create dashboard authorization filter

2. **EventReminderJob** (3 hours)
   - Create `IEventReminderJob` interface
   - Implement `EventReminderJob`
   - Add repository methods: `GetEventsStartingBetweenAsync()`
   - Register recurring job in `Program.cs`
   - Unit tests

3. **EventStatusUpdateJob** (3 hours)
   - Create `IEventStatusUpdateJob` interface
   - Implement `EventStatusUpdateJob`
   - Add repository methods: `GetPublishedEventsStartingBeforeAsync()`, `GetActiveEventsEndingBeforeAsync()`
   - Register recurring job in `Program.cs`
   - Unit tests

4. **Integration Testing** (2 hours)
   - Test Hangfire jobs with in-memory storage
   - Test dashboard authorization
   - Manual testing of recurring jobs

### Total Estimated Time: 5 days

---

## 9. Key Architectural Decisions (ADRs)

### ADR 1: Domain Event Handlers in Application Layer
**Decision**: Place event handlers in Application layer, not Domain.
**Rationale**: Handlers orchestrate cross-aggregate operations and call infrastructure services (email), which violates domain purity.
**Consequences**: Clean separation of concerns, testable handlers, domain remains infrastructure-agnostic.

### ADR 2: Direct Email Sending via Existing EmailService
**Decision**: Use existing `IEmailService.SendTemplatedEmailAsync()` and `EmailQueueProcessor` for async email delivery.
**Rationale**: Infrastructure already supports queued email processing; no need for additional Hangfire email jobs.
**Consequences**: Simpler implementation, leverage existing tested code, reduce Hangfire job complexity.

### ADR 3: Admin Approval as Domain Methods
**Decision**: Implement `Approve()` and `Reject()` as domain methods on Event aggregate.
**Rationale**: Approval is a state transition with business rules (only UnderReview events can be approved).
**Consequences**: Business logic in domain layer, reusable across use cases, testable in isolation.

### ADR 4: Hangfire for Scheduled Jobs Only
**Decision**: Use Hangfire for recurring scheduled jobs (reminders, status updates), not for event-driven email sending.
**Rationale**: Event-driven emails are better handled by domain event handlers; Hangfire is ideal for time-based cron jobs.
**Consequences**: Clear separation: domain events = real-time reactions, Hangfire = scheduled batch operations.

### ADR 5: Fail-Silent Event Handlers
**Decision**: Event handlers should log errors but not throw exceptions.
**Rationale**: Prevents event handler failures from rolling back parent transactions (e.g., RSVP registration).
**Consequences**: More resilient system, but requires robust logging and monitoring to catch silent failures.

---

## 10. Security Considerations

### 10.1 Hangfire Dashboard Authorization
- Restrict dashboard access to authenticated admins only
- Use custom `IDashboardAuthorizationFilter` with role check
- Consider IP whitelisting for production environments

### 10.2 Email Content Sanitization
- Sanitize user-generated content (event titles, names) before including in emails
- Use parameterized templates to prevent XSS in email HTML
- Validate email addresses before sending

### 10.3 Admin Approval Authorization
- Enforce `[Authorize(Policy = "AdminOnly")]` on approval endpoints
- Log all approval/rejection actions with admin user ID for audit trail
- Consider requiring approval from multiple admins for high-value events

---

## 11. Performance Considerations

### 11.1 Bulk Email Sending
- Use `IEmailService.SendBulkEmailAsync()` for event cancellation/postponement notifications
- Batch database queries (e.g., `GetByIdsAsync()`) to avoid N+1 queries
- Consider rate limiting SMTP sends to avoid throttling

### 11.2 Hangfire Job Optimization
- Use background queue for long-running jobs
- Set appropriate `WorkerCount` based on server resources (default: 5)
- Monitor job execution times and adjust polling intervals

### 11.3 Database Indexes
- Ensure indexes on:
  - `events.start_date` (for reminder queries)
  - `events.status` (for status update queries)
  - `registrations.event_id` + `registrations.status` (for bulk notifications)

---

## 12. Monitoring and Observability

### 12.1 Structured Logging
- Use `LogContext.PushProperty()` in Hangfire jobs for job-specific logging
- Log all email sending attempts (success/failure)
- Track domain event handler execution times

### 12.2 Hangfire Dashboard Metrics
- Monitor job success/failure rates
- Track average job execution times
- Set up alerts for recurring job failures

### 12.3 Email Delivery Tracking
- Monitor `communications.email_messages` table for failed sends
- Create dashboard query for email delivery rates
- Set up alerts for high email failure rates

---

## Conclusion

This architecture follows Clean Architecture and DDD principles while leveraging existing codebase patterns:
- **Domain events** raised by aggregates, handled in Application layer
- **CQRS with MediatR** for commands and event handlers
- **Result pattern** for error handling without exceptions
- **Repository pattern** for data access abstraction
- **Background jobs** via Hangfire for scheduled operations

All recommendations are production-ready and testable. Implementation can proceed incrementally (RSVP notifications → Admin approval → Hangfire jobs) with continuous testing and zero-tolerance for compilation errors.

**Estimated Total Implementation Time**: 5 days (40 hours)

**Next Steps**:
1. Review this guide with development team
2. Create Epic 2 Phase 5 task breakdown in project tracker
3. Begin implementation with Day 1: Domain Event Dispatching + RSVP Handlers
4. Maintain test coverage above 90% throughout implementation

---

**Document Status**: Final
**Review Date**: 2025-11-02
**Approved By**: System Architect
