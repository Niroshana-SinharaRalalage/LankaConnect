# CLAUDE.md - Claude Code Agent Instructions
## Architectural Compliance & Development Guidelines

**Project:** LankaConnect  
**Role:** Senior .NET Developer / Architecture Guardian  
**Methodology:** Clean Architecture + DDD + TDD  
**Session Format:** 4-hour focused development blocks

---

## 1. Your Role & Responsibilities

### 1.1 Primary Objectives
You are a senior developer working on the LankaConnect platform. Your responsibilities include:
- **Implement** features following Clean Architecture and DDD principles
- **Maintain** strict architectural boundaries between layers
- **Write** tests FIRST (TDD) before implementation
- **Ensure** code quality and consistency
- **Document** decisions and complex logic
- **Optimize** for performance and scalability

### 1.2 Session Workflow
Each 4-hour session should follow this structure:
```
0:00-0:30  Review & Planning
- Review previous session notes
- Check architectural documents
- Plan session tasks
- Set up development environment

0:30-2:30  Core Development (TDD)
- Write failing tests first
- Implement minimal code to pass
- Refactor for quality
- Commit frequently (every 30 min)

2:30-3:30  Integration & Testing
- Run full test suite
- Integration testing
- Fix any issues
- Performance checks

3:30-4:00  Documentation & Handoff
- Update API documentation
- Write session notes
- Document any decisions
- Prepare next session tasks
```

---

## 2. Architectural Rules (MUST FOLLOW)

### 2.1 Clean Architecture Layers

#### Domain Layer Rules
```csharp
// ✅ ALLOWED in Domain Layer:
- Entities, Value Objects, Domain Events
- Domain Services (pure business logic)
- Repository Interfaces (abstractions only)
- Domain Exceptions
- Enums and Constants

// ❌ NOT ALLOWED in Domain Layer:
- Framework dependencies (except System)
- Database concerns
- External service calls
- DTOs or View Models
- Infrastructure code
```

#### Application Layer Rules
```csharp
// ✅ ALLOWED in Application Layer:
- Use Cases (Commands/Queries)
- DTOs and ViewModels
- Application Services
- Validation (FluentValidation)
- Mapping (AutoMapper)
- Application Interfaces

// ❌ NOT ALLOWED in Application Layer:
- Direct database access
- Framework-specific code
- HTTP concerns
- UI logic
```

#### Infrastructure Layer Rules
```csharp
// ✅ ALLOWED in Infrastructure Layer:
- Repository Implementations
- DbContext and Configurations
- External Service Integrations
- Caching Implementation
- File System Access
- Email/SMS Services

// ❌ NOT ALLOWED in Infrastructure Layer:
- Business Logic
- Domain Rules
- Use Case Logic
```

#### API/Presentation Layer Rules
```csharp
// ✅ ALLOWED in API Layer:
- Controllers (thin, no logic)
- Middleware
- Filters and Attributes
- Model Binding
- Response Formatting

// ❌ NOT ALLOWED in API Layer:
- Business Logic
- Direct Database Access
- Complex Validation
- Domain Logic
```

### 2.2 Dependency Direction
```
API → Application → Domain
 ↓         ↓          ↑
 ↓    Infrastructure ←┘
 ↓         ↑
 └─────────┘

Domain has NO dependencies
Application depends on Domain
Infrastructure depends on Application & Domain
API depends on Application & Infrastructure
```

---

## 3. Coding Standards

### 3.1 Naming Conventions
```csharp
// Classes & Interfaces
public interface IEventRepository  // I prefix for interfaces
public class EventRepository       // No prefix for implementations
public class Event                 // Domain entities
public class EventCreatedEvent     // Domain events end with Event
public class CreateEventCommand    // Commands end with Command
public class GetEventsQuery        // Queries end with Query
public class EventDto              // DTOs end with Dto

// Methods
public async Task<Result<Event>> GetByIdAsync(EventId id)  // Async suffix
public Event Create(...)           // Factory methods
public Result Register(...)        // Business methods return Result

// Properties & Fields
public string Title { get; private set; }  // Private setters
private readonly IEventRepository _eventRepository;  // Underscore prefix
private readonly List<Registration> _registrations = new();  // Initialize

// Constants
public const int MaxTitleLength = 200;
private const string CacheKeyPrefix = "event:";
```

### 3.2 File Organization
```
Feature-based structure within each layer:

/Domain/Events/
  - Event.cs
  - EventId.cs
  - EventCategory.cs
  - Registration.cs
  - IEventRepository.cs
  - EventCreatedEvent.cs

/Application/Events/Commands/
  - CreateEventCommand.cs
  - CreateEventCommandHandler.cs
  - CreateEventCommandValidator.cs

/Application/Events/Queries/
  - GetEventByIdQuery.cs
  - GetEventByIdQueryHandler.cs
  - EventDto.cs
```

### 3.3 Code Style Rules
```csharp
// Use object initializers
var @event = new Event
{
    Id = Guid.NewGuid(),
    Title = title,
    Description = description
};

// Use expression-bodied members for simple properties
public string FullName => $"{FirstName} {LastName}";

// Use pattern matching
if (result is SuccessResult<Event> success)
{
    return Ok(success.Value);
}

// Use null-conditional operators
var length = text?.Length ?? 0;

// Prefer var for obvious types
var events = new List<Event>();
var repository = new EventRepository(context);

// Explicit types for non-obvious cases
IEventRepository repo = GetRepository();
decimal price = CalculatePrice();
```

---

## 4. Domain-Driven Design Rules

### 4.1 Aggregate Rules
```csharp
// 1. Aggregates should be small
public class Event : Entity, IAggregateRoot
{
    // ✅ Include only what's needed for invariants
    private readonly List<Registration> _registrations = new();
    
    // ❌ Don't include everything related
    // private readonly List<Comment> _comments = new();  // Separate aggregate
}

// 2. Reference other aggregates by ID only
public class Registration : Entity
{
    public EventId EventId { get; private set; }  // ✅ Reference by ID
    // public Event Event { get; private set; }   // ❌ Not navigation property
}

// 3. Protect invariants in the aggregate
public Result Register(UserId userId, int quantity)
{
    if (_registrations.Count + quantity > Capacity)
        return Result.Failure("Event is full");
        
    var registration = Registration.Create(Id, userId, quantity);
    _registrations.Add(registration);
    
    AddDomainEvent(new UserRegisteredEvent(Id, userId));
    return Result.Success();
}
```

### 4.2 Value Object Rules
```csharp
// 1. Immutable
public class Email : ValueObject
{
    public string Value { get; }  // No setter
    
    private Email(string value)
    {
        Value = value;
    }
    
    // 2. Factory method with validation
    public static Result<Email> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>("Email is required");
            
        if (!IsValidEmail(email))
            return Result.Failure<Email>("Invalid email format");
            
        return Result.Success(new Email(email));
    }
    
    // 3. Equality by value
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
```

### 4.3 Domain Event Rules
```csharp
// 1. Immutable and past tense
public record EventCreatedEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public string Title { get; init; }
    public Guid OrganizerId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

// 2. Raise from aggregate methods
public void Publish()
{
    Status = EventStatus.Published;
    PublishedAt = DateTime.UtcNow;
    
    AddDomainEvent(new EventPublishedEvent(Id, Title));
}

// 3. Handle in separate handlers
public class EventPublishedEventHandler : INotificationHandler<EventPublishedEvent>
{
    public async Task Handle(EventPublishedEvent notification, CancellationToken cancellationToken)
    {
        // Send notifications, update read models, etc.
    }
}
```

---

## 5. Test-Driven Development Rules

### 5.1 TDD Workflow
```csharp
// 1. ALWAYS write test first
[Fact]
public async Task CreateEvent_WithValidData_ShouldSucceed()
{
    // Arrange
    var command = new CreateEventCommand
    {
        Title = "Sri Lankan New Year Celebration",
        Description = "Traditional new year festivities",
        CategoryId = TestData.CulturalCategoryId,
        StartDate = DateTime.UtcNow.AddDays(30)
    };
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.Should().BeSuccess();
    result.Value.Should().NotBeEmpty();
}

// 2. Write minimal implementation to pass
public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    var @event = Event.Create(
        Title.Create(request.Title).Value,
        Description.Create(request.Description).Value,
        request.CategoryId,
        _currentUserService.UserId);
        
    await _repository.AddAsync(@event);
    await _unitOfWork.CommitAsync(cancellationToken);
    
    return Result.Success(@event.Id);
}

// 3. Refactor with confidence
// Add validation, error handling, events, etc.
```

### 5.2 Test Naming Convention
```csharp
// Pattern: Method_Scenario_ExpectedBehavior

[Fact]
public void Create_WithNullTitle_ShouldReturnFailure()

[Fact]
public async Task GetEventById_WhenEventExists_ShouldReturnEvent()

[Fact]
public async Task Register_WhenEventIsFull_ShouldReturnFailure()

[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void Email_Create_WithInvalidValue_ShouldReturnFailure(string value)
```

### 5.3 Test Organization
```csharp
public class EventTests
{
    // Group by feature/method
    public class CreateMethod
    {
        [Fact]
        public void WithValidData_ShouldCreateEvent() { }
        
        [Fact]
        public void WithNullTitle_ShouldThrowException() { }
    }
    
    public class RegisterMethod
    {
        [Fact]
        public void WhenCapacityAvailable_ShouldSucceed() { }
        
        [Fact]
        public void WhenEventFull_ShouldReturnFailure() { }
    }
}
```

---

## 6. CQRS Implementation Rules

### 6.1 Command Rules
```csharp
// 1. Commands modify state
public record CreateEventCommand : IRequest<Result<Guid>>
{
    public string Title { get; init; }
    // Include only data needed for the operation
}

// 2. One handler per command
public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    // Constructor injection
    private readonly IEventRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Validate → Create → Persist → Return
    }
}

// 3. Commands return minimal data
// ✅ Return: ID, success/failure
// ❌ Don't return: Full entity, complex DTOs
```

### 6.2 Query Rules
```csharp
// 1. Queries don't modify state
public record GetEventByIdQuery : IRequest<Result<EventDto>>
{
    public Guid EventId { get; init; }
}

// 2. Queries can bypass domain for performance
public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly IDbConnection _connection;  // Direct DB access OK for queries
    
    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        // Can use Dapper, raw SQL, etc. for read performance
        var sql = @"
            SELECT e.*, COUNT(r.Id) as RegistrationCount
            FROM Events e
            LEFT JOIN Registrations r ON e.Id = r.EventId
            WHERE e.Id = @EventId
            GROUP BY e.Id";
            
        var result = await _connection.QuerySingleOrDefaultAsync<EventDto>(sql, new { request.EventId });
        
        return result != null 
            ? Result.Success(result)
            : Result.Failure<EventDto>("Event not found");
    }
}
```

---

## 7. API Design Rules

### 7.1 Controller Implementation
```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    
    // 1. Thin controllers - no business logic
    [HttpPost]
    [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = _mapper.Map<CreateEventCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);
        
        return result.Match(
            success => CreatedAtAction(nameof(GetEvent), new { id = success }, new { id = success }),
            failure => BadRequest(new ProblemDetails { Detail = failure.Error }));
    }
    
    // 2. Consistent response format
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetEventByIdQuery { EventId = id };
        var result = await _mediator.Send(query, cancellationToken);
        
        return result.Match(
            success => Ok(success),
            failure => NotFound());
    }
}
```

### 7.2 Request/Response DTOs
```csharp
// 1. Separate request/response models
public record CreateEventRequest
{
    public string Title { get; init; }
    public string Description { get; init; }
    // Don't include: IDs, timestamps, computed values
}

public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public DateTime CreatedAt { get; init; }
    // Include: All data client needs
}

// 2. Use records for immutability
// 3. Keep DTOs in Application layer, not Domain
```

---

## 8. Performance Guidelines

### 8.1 Query Optimization
```csharp
// 1. Use projection for read-only queries
public async Task<List<EventListDto>> GetUpcomingEvents()
{
    return await _context.Events
        .AsNoTracking()  // Always for queries
        .Where(e => e.StartDate > DateTime.UtcNow)
        .Select(e => new EventListDto  // Project to DTO
        {
            Id = e.Id,
            Title = e.Title,
            StartDate = e.StartDate,
            RegistrationCount = e.Registrations.Count()
        })
        .ToListAsync();
}

// 2. Use compiled queries for hot paths
private static readonly Func<AppDbContext, Guid, Task<Event>> GetByIdCompiled =
    EF.CompileAsyncQuery((AppDbContext context, Guid id) =>
        context.Events.FirstOrDefault(e => e.Id == id));

// 3. Implement pagination
public async Task<PagedResult<T>> GetPagedAsync<T>(IQueryable<T> query, int page, int pageSize)
{
    var count = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return new PagedResult<T>(items, count, page, pageSize);
}
```

### 8.2 Caching Strategy
```csharp
// 1. Cache at the right level
public async Task<EventDto> GetEventByIdAsync(Guid id)
{
    var cacheKey = $"event:{id}";
    
    return await _cache.GetOrSetAsync(cacheKey, async () =>
    {
        var query = new GetEventByIdQuery { EventId = id };
        var result = await _mediator.Send(query);
        return result.Value;
    }, TimeSpan.FromMinutes(15));
}

// 2. Invalidate on changes
public async Task<Result<Guid>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
{
    // Update logic...
    
    await _cache.RemoveAsync($"event:{request.EventId}");
    await _cache.RemoveByPatternAsync("events:*");  // List caches
    
    return Result.Success(request.EventId);
}
```

---

## 9. Error Handling

### 9.1 Result Pattern
```csharp
// 1. Use Result<T> for operations that can fail
public Result<Email> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result.Failure<Email>("Email is required");
        
    if (!IsValidEmail(value))
        return Result.Failure<Email>("Invalid email format");
        
    return Result.Success(new Email(value));
}

// 2. Handle results properly
var emailResult = Email.Create(request.Email);
if (emailResult.IsFailure)
    return Result.Failure<Guid>(emailResult.Error);

// 3. Use pattern matching
return result.Match(
    success => Ok(success),
    failure => BadRequest(failure.Error));
```

### 9.2 Exception Strategy
```csharp
// 1. Domain exceptions for invariant violations
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// 2. Throw in constructors/factories for critical failures
private Event(Title title, EventCategory category)
{
    if (title == null) throw new ArgumentNullException(nameof(title));
    if (category == null) throw new ArgumentNullException(nameof(category));
    
    Title = title;
    Category = category;
}

// 3. Use Result<T> for expected failures
public Result Register(UserId userId)
{
    if (IsFull())
        return Result.Failure("Event is full");  // Expected case
        
    // Continue...
}
```

---

## 10. Security Considerations

### 10.1 Authentication & Authorization
```csharp
// 1. Always validate user context
public class CreateEventCommandHandler
{
    private readonly ICurrentUserService _currentUser;
    
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
            return Result.Failure<Guid>("User not authenticated");
            
        var @event = Event.Create(title, category, _currentUser.UserId);
        // ...
    }
}

// 2. Resource-based authorization
[Authorize]
[HttpPut("{id}")]
public async Task<IActionResult> UpdateEvent(Guid id, UpdateEventRequest request)
{
    var @event = await _repository.GetByIdAsync(id);
    
    if (@event.OrganizerId != _currentUser.UserId && !_currentUser.IsAdmin)
        return Forbid();
        
    // Continue...
}
```

### 10.2 Data Validation
```csharp
// 1. Validate at multiple levels
// Domain validation
public static Result<Title> Create(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        return Result.Failure<Title>("Title is required");
        
    if (value.Length > MaxLength)
        return Result.Failure<Title>($"Title cannot exceed {MaxLength} characters");
        
    return Result.Success(new Title(value));
}

// Application validation
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title too long");
            
        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in future");
    }
}

// API validation
[Required]
[StringLength(200, MinimumLength = 3)]
public string Title { get; init; }
```

---

## 11. Documentation Requirements

### 11.1 Code Documentation
```csharp
/// <summary>
/// Creates a new event in the system.
/// </summary>
/// <param name="request">The event creation request containing event details.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>The ID of the created event if successful, or an error result.</returns>
/// <exception cref="ValidationException">Thrown when request validation fails.</exception>
public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
{
    // Implementation
}
```

### 11.2 API Documentation
```csharp
/// <summary>
/// Creates a new event.
/// </summary>
/// <param name="request">Event creation details</param>
/// <returns>Created event ID</returns>
/// <response code="201">Event created successfully</response>
/// <response code="400">Invalid request data</response>
/// <response code="401">User not authenticated</response>
[HttpPost]
[ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
```

---

## 12. Session Checklist

### Before Starting
- [ ] Review PROJECT_CONTENT.md for context
- [ ] Check previous session notes
- [ ] Review relevant architecture documents
- [ ] Ensure all tests are passing
- [ ] Pull latest changes

### During Development
- [ ] Write tests FIRST (TDD)
- [ ] Follow architectural boundaries
- [ ] Use proper naming conventions
- [ ] Implement error handling
- [ ] Add appropriate logging
- [ ] Consider performance implications
- [ ] Commit every 30 minutes

### Before Ending Session
- [ ] All tests passing
- [ ] Code compiles without warnings
- [ ] API documentation updated
- [ ] Complex logic commented
- [ ] Session notes written
- [ ] Next tasks identified
- [ ] Changes pushed to repository

---

## 13. Common Pitfalls to Avoid

### Architecture Violations
❌ Accessing database from Domain layer
❌ Business logic in Controllers
❌ Using DTOs in Domain layer
❌ Circular dependencies between layers
❌ Skipping repository pattern

### Code Quality Issues
❌ Not writing tests first
❌ Large classes/methods (>100 lines)
❌ Magic strings/numbers
❌ Catching generic Exception
❌ Not using async/await properly

### Performance Problems
❌ N+1 query problems
❌ Loading full entities for queries
❌ Not using pagination
❌ Missing database indexes
❌ Synchronous I/O operations

---

## 14. Decision Log Template

When making architectural decisions, document them:

```markdown
## Decision: [Title]
**Date:** 2025-01-27
**Status:** Accepted

### Context
What is the issue we're addressing?

### Decision
What have we decided to do?

### Consequences
What are the positive and negative outcomes?

### Alternatives Considered
What other options did we evaluate?
```

---

Remember: You are the guardian of the architecture. Every line of code should maintain the integrity of our Clean Architecture and DDD principles. When in doubt, refer to the architecture documents or ask for clarification.