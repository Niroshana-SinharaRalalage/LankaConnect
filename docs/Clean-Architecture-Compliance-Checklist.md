# Clean Architecture Compliance Checklist

## Overview

This checklist ensures that all Infrastructure dependency implementations maintain Clean Architecture principles and comply with the established patterns in the LankaConnect codebase.

## Clean Architecture Layer Boundaries

### ✅ Domain Layer (Core/Innermost)
**Principle**: Domain layer has no dependencies on external frameworks or infrastructure

#### Entities and Value Objects
- [ ] No EF Core attributes or annotations in domain entities
- [ ] No System.ComponentModel.DataAnnotations in domain classes
- [ ] Value objects are immutable with private constructors
- [ ] Domain entities contain only business logic and rules
- [ ] No Infrastructure or Application layer references

#### Domain Services
- [ ] Pure business logic without external dependencies  
- [ ] Interfaces defined in Domain layer, implementations in Infrastructure
- [ ] No database, email, or external service calls in domain services

#### Example Compliance Check:
```csharp
// ✅ CORRECT - Clean domain entity
public class UserEmailPreferences : BaseEntity
{
    private readonly Dictionary<string, bool> _preferences;
    
    private UserEmailPreferences(Guid userId, Dictionary<string, bool> preferences)
    {
        UserId = userId;
        _preferences = preferences ?? new Dictionary<string, bool>();
    }
    
    public static Result<UserEmailPreferences> Create(Guid userId, Dictionary<string, bool> preferences)
    {
        // Domain validation logic only
        if (userId == Guid.Empty)
            return Result<UserEmailPreferences>.Failure("User ID is required");
            
        return Result<UserEmailPreferences>.Success(new UserEmailPreferences(userId, preferences));
    }
}

// ❌ INCORRECT - Infrastructure leakage
[Table("UserEmailPreferences")]  // EF Core attribute
public class UserEmailPreferences
{
    [Required]                   // Data annotation
    [Column("user_id")]         // Database-specific naming
    public Guid UserId { get; set; }
}
```

### ✅ Application Layer (Use Cases/Orchestration)
**Principle**: Application layer defines interfaces but doesn't implement infrastructure concerns

#### Command and Query Handlers
- [ ] Handlers only orchestrate workflow, no direct infrastructure calls
- [ ] Dependencies injected as abstractions/interfaces
- [ ] Result pattern used for all operations
- [ ] No direct database context usage in handlers
- [ ] No email sending, file I/O, or external API calls

#### Interface Definitions
- [ ] All infrastructure abstractions defined in Application.Common.Interfaces
- [ ] Interface contracts specify behavior, not implementation details
- [ ] Return types use domain objects or DTOs, not infrastructure types
- [ ] Async patterns with CancellationToken support

#### Example Compliance Check:
```csharp
// ✅ CORRECT - Clean application handler
public class GetUserEmailPreferencesQueryHandler : IRequestHandler<GetUserEmailPreferencesQuery, Result<GetUserEmailPreferencesResponse>>
{
    private readonly IUserEmailPreferencesRepository _repository;
    private readonly ILogger<GetUserEmailPreferencesQueryHandler> _logger;

    public GetUserEmailPreferencesQueryHandler(
        IUserEmailPreferencesRepository repository,
        ILogger<GetUserEmailPreferencesQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<GetUserEmailPreferencesResponse>> Handle(
        GetUserEmailPreferencesQuery request, 
        CancellationToken cancellationToken)
    {
        // Only orchestration logic - no infrastructure implementation
        var preferences = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        if (preferences == null)
            return Result<GetUserEmailPreferencesResponse>.Failure("User preferences not found");
            
        return Result<GetUserEmailPreferencesResponse>.Success(
            new GetUserEmailPreferencesResponse(preferences));
    }
}

// ❌ INCORRECT - Infrastructure in application layer
public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, Result>
{
    private readonly AppDbContext _context;     // Direct DbContext usage
    
    public async Task<Result> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        // Direct database operations in handler
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        
        // Direct SMTP client usage
        using var smtpClient = new SmtpClient("smtp.server.com");
        await smtpClient.SendMailAsync(/* ... */);
    }
}
```

### ✅ Infrastructure Layer (External Concerns)
**Principle**: Infrastructure implements Application interfaces and handles external dependencies

#### Repository Implementations
- [ ] Inherit from base Repository<T> class
- [ ] Implement Application layer interfaces
- [ ] Use Result pattern for all operations
- [ ] Include comprehensive logging with structured context
- [ ] Handle EF Core exceptions properly
- [ ] Use async operations with CancellationToken

#### Service Implementations  
- [ ] Implement Application layer service interfaces
- [ ] Handle external API calls, file I/O, email sending
- [ ] Use configuration through Options pattern
- [ ] Include retry logic and error handling
- [ ] Follow established logging patterns

#### Example Compliance Check:
```csharp
// ✅ CORRECT - Clean infrastructure implementation
public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>, IUserEmailPreferencesRepository
{
    public UserEmailPreferencesRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("UserId", userId))
        {
            _logger.Debug("Getting user email preferences for user {UserId}", userId);
            
            var result = await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
                
            _logger.Debug("Retrieved preferences for user {UserId}: {Found}", userId, result != null);
            return result;
        }
    }

    public async Task<Result> AddAsync(UserEmailPreferences preferences, CancellationToken cancellationToken = default)
    {
        try
        {
            await base.AddAsync(preferences, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.Information("User email preferences added for user {UserId}", preferences.UserId);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.Warning("Duplicate user email preferences for user {UserId}", preferences.UserId);
            return Result.Failure("User email preferences already exist");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add user email preferences for user {UserId}", preferences.UserId);
            return Result.Failure($"Failed to add user email preferences: {ex.Message}");
        }
    }
}

// ❌ INCORRECT - Violates separation of concerns
public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>
{
    // Business logic in infrastructure layer
    public async Task<Result> UpdateUserEmailPreferences(Guid userId, bool allowMarketing)
    {
        var preferences = await GetByUserIdAsync(userId);
        
        // Business rules should be in domain layer
        if (preferences.User.IsMinor && allowMarketing)
        {
            return Result.Failure("Minors cannot opt into marketing emails");
        }
        
        // This business logic belongs in domain entity or application service
        preferences.UpdateMarketingPreference(allowMarketing);
        
        await UpdateAsync(preferences);
        return Result.Success();
    }
}
```

## Dependency Flow Validation

### ✅ Allowed Dependencies
```
Infrastructure → Application (interfaces only)
Infrastructure → Domain (entities, value objects)
Application → Domain (entities, value objects)
Infrastructure → External Libraries (EF Core, Azure SDK, etc.)
```

### ❌ Prohibited Dependencies
```
Domain → Application (never)
Domain → Infrastructure (never)  
Application → Infrastructure (concrete implementations)
Domain → External Libraries (EF Core, System.Data, etc.)
```

#### Validation Commands
```bash
# Check for prohibited references using dependency analysis
dotnet list src/LankaConnect.Domain/LankaConnect.Domain.csproj reference
# Should only see: LankaConnect.Shared (if any)

dotnet list src/LankaConnect.Application/LankaConnect.Application.csproj reference  
# Should only see: LankaConnect.Domain, MediatR, FluentValidation

dotnet list src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj reference
# Should see: LankaConnect.Domain, LankaConnect.Application, EF Core, Azure SDK, etc.
```

## Implementation Quality Standards

### ✅ Result Pattern Usage
- [ ] All repository methods return Result<T> or Result
- [ ] All service operations return Result<T> or Result  
- [ ] Proper error messages that don't expose internal details
- [ ] Success and failure cases properly handled

```csharp
// ✅ CORRECT - Consistent Result pattern
public async Task<Result<UserEmailPreferences>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
{
    try 
    {
        var preferences = await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        
        return preferences != null 
            ? Result<UserEmailPreferences>.Success(preferences)
            : Result<UserEmailPreferences>.Failure("User preferences not found");
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed to retrieve user preferences for {UserId}", userId);
        return Result<UserEmailPreferences>.Failure("Failed to retrieve user preferences");
    }
}

// ❌ INCORRECT - Inconsistent return types
public async Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId)  // Nullable return
public async Task<bool> AddAsync(UserEmailPreferences preferences)      // Boolean success
public async Task UpdateAsync(UserEmailPreferences preferences)         // Void (throws exceptions)
```

### ✅ Logging Standards
- [ ] Structured logging with Serilog
- [ ] LogContext properties for correlation
- [ ] Appropriate log levels (Debug, Information, Warning, Error)
- [ ] No sensitive data in logs
- [ ] Performance logging for slow operations

```csharp
// ✅ CORRECT - Structured logging with context
public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
{
    using (LogContext.PushProperty("Operation", "SendEmail"))
    using (LogContext.PushProperty("EmailType", emailMessage.Type))  
    using (LogContext.PushProperty("RecipientEmail", emailMessage.ToEmail))
    {
        _logger.Information("Sending email of type {EmailType} to {RecipientEmail}", 
            emailMessage.Type, emailMessage.ToEmail);
            
        var stopwatch = Stopwatch.StartNew();
        var result = await _smtpService.SendAsync(emailMessage, cancellationToken);
        stopwatch.Stop();
        
        if (result.IsSuccess)
        {
            _logger.Information("Email sent successfully in {Duration}ms", stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.Warning("Email sending failed: {Error}", result.Error);
        }
        
        return result;
    }
}
```

### ✅ Error Handling Standards
- [ ] All exceptions caught and converted to Result pattern
- [ ] Specific exception types handled appropriately
- [ ] Database concurrency conflicts handled
- [ ] External service failures gracefully handled
- [ ] Retry logic for transient failures

```csharp
// ✅ CORRECT - Comprehensive error handling
public async Task<Result> AddAsync(UserEmailPreferences preferences, CancellationToken cancellationToken = default)
{
    try
    {
        await base.AddAsync(preferences, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        _logger.Warning(ex, "Concurrency conflict adding user preferences for {UserId}", preferences.UserId);
        return Result.Failure("Another process has modified these preferences. Please try again.");
    }
    catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
    {
        _logger.Warning("Duplicate user preferences for {UserId}", preferences.UserId);
        return Result.Failure("User preferences already exist");
    }
    catch (OperationCanceledException)
    {
        _logger.Information("Add operation cancelled for user {UserId}", preferences.UserId);
        return Result.Failure("Operation was cancelled");
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Unexpected error adding user preferences for {UserId}", preferences.UserId);
        return Result.Failure("An unexpected error occurred while adding user preferences");
    }
}
```

## Testing Standards

### ✅ Unit Test Requirements
- [ ] Each repository method has unit tests
- [ ] Each service method has unit tests  
- [ ] Error scenarios are tested
- [ ] Result pattern returns are validated
- [ ] Mocking used for external dependencies

### ✅ Integration Test Requirements
- [ ] End-to-end database operations tested
- [ ] Service integrations tested
- [ ] Configuration binding tested
- [ ] Performance benchmarks included

```csharp
// ✅ CORRECT - Comprehensive unit tests
[TestFixture]
public class UserEmailPreferencesRepositoryTests
{
    private AppDbContext _context;
    private UserEmailPreferencesRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _context = CreateInMemoryContext();
        _repository = new UserEmailPreferencesRepository(_context);
    }

    [Test]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var preferences = UserEmailPreferencesTestDataBuilder.Create()
            .WithUserId(userId)
            .Build();
        
        await _context.UserEmailPreferences.AddAsync(preferences);
        await _context.SaveChangesAsync();
        
        // Act
        var result = await _repository.GetByUserIdAsync(userId);
        
        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
    }

    [Test]
    public async Task AddAsync_WithValidPreferences_ReturnsSuccess()
    {
        // Arrange
        var preferences = UserEmailPreferencesTestDataBuilder.Create().Build();
        
        // Act
        var result = await _repository.AddAsync(preferences);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        _context.UserEmailPreferences.Should().Contain(p => p.Id == preferences.Id);
    }
}
```

## Validation Checklist Summary

### Domain Layer Compliance
- [ ] No external framework dependencies
- [ ] Pure business logic only
- [ ] Immutable value objects with factory methods
- [ ] Result pattern for domain operations
- [ ] No infrastructure concerns

### Application Layer Compliance  
- [ ] Interface definitions only, no implementations
- [ ] MediatR handlers orchestrate workflow only
- [ ] Dependencies injected as abstractions
- [ ] Result pattern consistently used
- [ ] No direct infrastructure calls

### Infrastructure Layer Compliance
- [ ] Implements Application layer interfaces
- [ ] Follows established Repository pattern
- [ ] Result pattern for all operations
- [ ] Comprehensive error handling
- [ ] Structured logging with correlation
- [ ] Configuration through Options pattern
- [ ] Async operations with CancellationToken

### Cross-Cutting Concerns
- [ ] Dependency flow rules followed
- [ ] No circular dependencies
- [ ] Proper service lifetimes in DI
- [ ] Comprehensive unit and integration tests
- [ ] Performance requirements met
- [ ] Security considerations addressed

### Migration Readiness
- [ ] All dependency injection registrations complete
- [ ] EF Core configurations for value objects done
- [ ] Database migrations generate successfully
- [ ] Application starts without DI errors
- [ ] End-to-end functionality works

This checklist ensures that all Infrastructure implementations maintain the high-quality standards and architectural principles established in the LankaConnect codebase.