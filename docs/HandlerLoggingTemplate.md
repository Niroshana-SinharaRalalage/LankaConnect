# Handler Logging Template - Phase 6A.X Observability

**Purpose**: Comprehensive logging pattern for all CQRS command/query handlers
**Scope**: 164 handlers across Application layer
**Pattern**: Structured logging with correlation, performance timing, and error tracking

---

## Handler Logging Pattern

### Required Components

1. **ILogger<T> Dependency Injection**
   ```csharp
   private readonly ILogger<THandler> _logger;

   public THandler(..., ILogger<THandler> logger)
   {
       // ... other dependencies
       _logger = logger;
   }
   ```

2. **LogContext.PushProperty for Correlation**
   ```csharp
   using (LogContext.PushProperty("Operation", "CommandName"))
   using (LogContext.PushProperty("EntityType", "AggregateRoot"))
   using (LogContext.PushProperty("UserId", request.UserId))
   using (LogContext.PushProperty("EntityId", request.EntityId))
   {
       // Handler logic here
   }
   ```

3. **Stopwatch for Performance Timing**
   ```csharp
   var stopwatch = Stopwatch.StartNew();
   // ... operation
   stopwatch.Stop();
   _logger.LogInformation("..., Duration={ElapsedMs}ms", ..., stopwatch.ElapsedMilliseconds);
   ```

4. **START Log (LogDebug)**
   ```csharp
   _logger.LogDebug("Handle START: CommandName - Key={Value}, ...", key, ...);
   ```

5. **COMPLETE Log (LogInformation)**
   ```csharp
   _logger.LogInformation(
       "Handle COMPLETE: CommandName - Key={Value}, Result={Result}, Duration={ElapsedMs}ms",
       key, result, stopwatch.ElapsedMilliseconds);
   ```

6. **FAILED Log (LogError) with Re-throw**
   ```csharp
   catch (Exception ex)
   {
       stopwatch.Stop();
       _logger.LogError(ex,
           "Handle FAILED: CommandName - Key={Value}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
           key, stopwatch.ElapsedMilliseconds, ex.Message);
       throw; // CRITICAL: Always re-throw
   }
   ```

7. **Business Validation Failures (LogWarning)**
   ```csharp
   if (validationResult.IsFailure)
   {
       _logger.LogWarning(
           "Handle VALIDATION FAILED: CommandName - Key={Value}, ValidationError={Error}",
           key, validationResult.Error);
       return Result<T>.Failure(validationResult.Error);
   }
   ```

---

## Complete Example: Command Handler

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Commands.CreateEvent;

public class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("OrganizerId", request.OrganizerId))
        using (LogContext.PushProperty("EventTitle", request.Title))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "CreateEvent START: OrganizerId={OrganizerId}, Title={Title}",
                request.OrganizerId, request.Title);

            try
            {
                // Validate user exists and has permission
                var user = await _userRepository.GetByIdAsync(request.OrganizerId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent VALIDATION FAILED: User not found - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("User not found");
                }

                if (!user.Role.CanCreateEvents())
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent AUTHORIZATION FAILED: User lacks permission - OrganizerId={OrganizerId}, Role={Role}, Duration={ElapsedMs}ms",
                        request.OrganizerId, user.Role, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("You do not have permission to create events.");
                }

                // Create value objects and aggregate
                var titleResult = EventTitle.Create(request.Title);
                if (titleResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent VALIDATION FAILED: Invalid title - OrganizerId={OrganizerId}, Title={Title}, Error={Error}, Duration={ElapsedMs}ms",
                        request.OrganizerId, request.Title, titleResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(titleResult.Error);
                }

                // ... create event aggregate ...
                var @event = Event.Create(
                    titleResult.Value,
                    // ... other parameters
                );

                await _eventRepository.AddAsync(@event, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CreateEvent COMPLETE: EventId={EventId}, OrganizerId={OrganizerId}, Title={Title}, Duration={ElapsedMs}ms",
                    @event.Id, request.OrganizerId, request.Title, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(@event.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateEvent FAILED: OrganizerId={OrganizerId}, Title={Title}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.OrganizerId, request.Title, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
```

---

## Complete Example: Query Handler

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Events.Queries.GetEventById;

public class GetEventByIdQueryHandler : IQueryHandler<GetEventByIdQuery, EventDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetEventByIdQueryHandler> _logger;

    public GetEventByIdQueryHandler(
        IEventRepository eventRepository,
        ILogger<GetEventByIdQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEventById"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetEventById START: EventId={EventId}", request.EventId);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetEventById NOT FOUND: EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<EventDto>.Failure("Event not found");
                }

                var dto = MapToDto(@event);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEventById COMPLETE: EventId={EventId}, Title={Title}, Duration={ElapsedMs}ms",
                    request.EventId, @event.Title.Value, stopwatch.ElapsedMilliseconds);

                return Result<EventDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEventById FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
```

---

## Complete Example: Event Handler (Domain Events)

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Domain.Events.DomainEvents;

namespace LankaConnect.Application.Events.EventHandlers;

public class RegistrationConfirmedEventHandler : INotificationHandler<RegistrationConfirmedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RegistrationConfirmedEventHandler> _logger;

    public RegistrationConfirmedEventHandler(
        IEmailService emailService,
        IEventRepository eventRepository,
        ILogger<RegistrationConfirmedEventHandler> logger)
    {
        _emailService = emailService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(RegistrationConfirmedEvent notification, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RegistrationConfirmed"))
        using (LogContext.PushProperty("EntityType", "EventRegistration"))
        using (LogContext.PushProperty("RegistrationId", notification.RegistrationId))
        using (LogContext.PushProperty("EventId", notification.EventId))
        using (LogContext.PushProperty("AttendeeEmail", notification.AttendeeEmail))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "RegistrationConfirmed START: RegistrationId={RegistrationId}, EventId={EventId}, Email={Email}",
                notification.RegistrationId, notification.EventId, notification.AttendeeEmail);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(notification.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegistrationConfirmed SKIPPED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        notification.EventId, stopwatch.ElapsedMilliseconds);

                    return;
                }

                // Send confirmation email
                await _emailService.SendRegistrationConfirmationAsync(
                    notification.AttendeeEmail,
                    @event.Title.Value,
                    notification.RegistrationId,
                    cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "RegistrationConfirmed COMPLETE: RegistrationId={RegistrationId}, EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                    notification.RegistrationId, notification.EventId, notification.AttendeeEmail, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RegistrationConfirmed FAILED: RegistrationId={RegistrationId}, EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    notification.RegistrationId, notification.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
```

---

## Key Differences from Repository Pattern

| Aspect | Repository Pattern | Handler Pattern |
|--------|-------------------|-----------------|
| **LogContext Properties** | Operation, EntityType, technical IDs | Operation, EntityType, business context (UserId, EventId, etc.) |
| **START Log Content** | Method name + query parameters | Command/Query name + business intent |
| **COMPLETE Log Content** | Query results + duration | Business outcome + duration |
| **Validation Failures** | Logged as errors (DB constraints) | Logged as warnings (business rules) |
| **Authorization Failures** | N/A (no auth in repositories) | Logged as warnings with role context |
| **Exception Re-throw** | Always re-throw | Always re-throw (let MediatR/API handle) |

---

## Required Using Statements

```csharp
using System.Diagnostics; // For Stopwatch
using Microsoft.Extensions.Logging; // For ILogger<T>
using Serilog.Context; // For LogContext.PushProperty
```

---

## Batch Implementation Strategy

### Phase 3 - All 164 Handlers
- **Batch 1**: Auth (5) + Critical Events (15) = 20 handlers
- **Batch 2-8**: 20 handlers each
- **Batch 9**: Remaining 4 handlers

### Priority Order
1. Authentication (Auth/)
2. Event Management (Events/Commands/, Events/Queries/)
3. Email & Communications (Communications/)
4. User Management (Users/)
5. Business Features (Businesses/, Badges/, etc.)

---

## Testing Requirements

After each batch enhancement:
1. ✅ Build: 0 errors, 0 warnings
2. ✅ Deploy: Azure staging via deploy-staging.yml
3. ✅ Test: Verify logs in Azure Container App for affected operations
4. ✅ Verify: START/COMPLETE/FAILED logs appear with duration metrics
5. ✅ Validate: Correlation properties present in all log entries

---

## Notes

- **CRITICAL**: Always re-throw exceptions - handlers should NOT swallow errors
- **Performance**: Stopwatch has negligible overhead (~100ns)
- **Structured Logging**: Use named parameters `{ParamName}` not string interpolation
- **LogContext**: Automatically propagates to repository calls and downstream operations
- **Security**: Never log sensitive data (passwords, tokens, PII beyond what's necessary)

---

*Last Updated: 2026-01-18 - Phase 6A.X Observability Phase 3*
