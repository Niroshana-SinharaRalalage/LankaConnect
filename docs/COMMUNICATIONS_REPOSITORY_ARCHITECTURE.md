# Communications Repository Architecture Guide

## Executive Summary

This document provides comprehensive architectural guidance for implementing Communications repositories following Test-Driven Development (TDD) principles, consistent with your existing Clean Architecture patterns.

## Analysis of Existing Patterns

### Current Repository Architecture

Your codebase demonstrates excellent architectural patterns:

**Base Repository Pattern**: 
- `IRepository<T>` interface with comprehensive CRUD operations
- Generic `Repository<T>` implementation with structured logging
- Consistent async/await patterns with cancellation token support
- Built-in pagination support

**Domain-Driven Design Integration**:
- BaseEntity with domain events support
- Value objects for type safety (EmailSubject, VerificationToken)
- Rich domain entities with business logic
- Clear separation of concerns across layers

**Current Communications Interfaces**:
- `IEmailMessageRepository` extends `IRepository<EmailMessage>`
- `IEmailTemplateRepository` - specialized interface (not extending base)
- `IUserEmailPreferencesRepository` - specialized interface (not extending base)

## 1. Repository Interface Design Recommendations

### 1.1 Consistent Interface Design Pattern

**Problem Identified**: Your Communications repositories are inconsistent. `IEmailMessageRepository` extends `IRepository<T>` while others don't.

**Recommendation**: All repository interfaces should extend the base `IRepository<T>` for consistency:

```csharp
// ✅ RECOMMENDED PATTERN
public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    // Specialized methods only
    Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Dictionary<EmailTemplateCategory, int>> GetCategoryCountsAsync(bool? isActive = null, CancellationToken cancellationToken = default);
}

public interface IUserEmailPreferencesRepository : IRepository<UserEmailPreferences>
{
    // Specialized methods only
    Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserEmailPreferences?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
```

### 1.2 Enhanced IEmailMessageRepository Design

```csharp
public interface IEmailMessageRepository : IRepository<EmailMessage>
{
    // Queue Management
    Task<IReadOnlyList<EmailMessage>> GetQueuedEmailsAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailMessage>> GetFailedEmailsForRetryAsync(int maxRetryAttempts = 3, CancellationToken cancellationToken = default);
    
    // Status-based Queries
    Task<IReadOnlyList<EmailMessage>> GetEmailsByStatusAsync(EmailStatus status, int limit = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailMessage>> GetEmailsByTypeAsync(EmailType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    // Analytics & Reporting
    Task<EmailQueueStats> GetEmailQueueStatsAsync(CancellationToken cancellationToken = default);
    Task<Dictionary<EmailStatus, int>> GetStatusCountsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);
    
    // Bulk Operations
    Task<int> MarkAsProcessedAsync(IEnumerable<Guid> emailIds, CancellationToken cancellationToken = default);
    Task<int> MarkAsFailedAsync(IEnumerable<Guid> emailIds, string errorMessage, CancellationToken cancellationToken = default);
    
    // Archival
    Task<int> ArchiveOldEmailsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default);
}
```

## 2. TDD Strategy for Repository Implementations

### 2.1 TDD Test Structure Pattern

```csharp
// Test Class Structure
[TestFixture]
public class EmailMessageRepositoryTests
{
    private DbContextOptions<AppDbContext> _dbContextOptions;
    private AppDbContext _context;
    private EmailMessageRepository _repository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
    }

    [SetUp]
    public async Task SetUp()
    {
        _context = new AppDbContext(_dbContextOptions);
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();
        _repository = new EmailMessageRepository(_context);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    // Red-Green-Refactor Tests
    [Test]
    public async Task GetQueuedEmailsAsync_WhenNoQueuedEmails_ReturnsEmptyList()
    {
        // Arrange - Empty database
        
        // Act
        var result = await _repository.GetQueuedEmailsAsync();
        
        // Assert
        result.Should().BeEmpty();
    }
}
```

### 2.2 TDD Red-Green-Refactor Cycle

**RED Phase**: Write failing test
```csharp
[Test]
public async Task GetQueuedEmailsAsync_WhenEmailsQueued_ReturnsOnlyQueuedEmails()
{
    // Arrange
    var queuedEmail = EmailMessageTestDataBuilder.Create()
        .WithStatus(EmailStatus.Queued)
        .Build();
    var sentEmail = EmailMessageTestDataBuilder.Create()
        .WithStatus(EmailStatus.Sent)
        .Build();
        
    await _repository.AddAsync(queuedEmail);
    await _repository.AddAsync(sentEmail);
    await _context.SaveChangesAsync();
    
    // Act
    var result = await _repository.GetQueuedEmailsAsync();
    
    // Assert
    result.Should().HaveCount(1);
    result.First().Status.Should().Be(EmailStatus.Queued);
}
```

**GREEN Phase**: Minimal implementation to pass
```csharp
public async Task<IReadOnlyList<EmailMessage>> GetQueuedEmailsAsync(int batchSize = 50, CancellationToken cancellationToken = default)
{
    return await _dbSet
        .Where(e => e.Status == EmailStatus.Queued)
        .Take(batchSize)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}
```

**REFACTOR Phase**: Optimize and improve
```csharp
public async Task<IReadOnlyList<EmailMessage>> GetQueuedEmailsAsync(int batchSize = 50, CancellationToken cancellationToken = default)
{
    using (LogContext.PushProperty("Operation", "GetQueuedEmails"))
    using (LogContext.PushProperty("BatchSize", batchSize))
    {
        _logger.Debug("Getting queued emails with batch size {BatchSize}", batchSize);
        
        var result = await _dbSet
            .Where(e => e.Status == EmailStatus.Queued)
            .OrderBy(e => e.CreatedAt) // FIFO processing
            .Take(batchSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
            
        _logger.Debug("Retrieved {Count} queued emails", result.Count);
        return result;
    }
}
```

## 3. Repository Implementation Patterns

### 3.1 Consistent Base Repository Usage

```csharp
namespace LankaConnect.Infrastructure.Data.Repositories;

public class EmailMessageRepository : Repository<EmailMessage>, IEmailMessageRepository
{
    public EmailMessageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EmailMessage>> GetQueuedEmailsAsync(
        int batchSize = 50, 
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetQueuedEmails"))
        using (LogContext.PushProperty("BatchSize", batchSize))
        {
            _logger.Debug("Getting queued emails with batch size {BatchSize}", batchSize);
            
            var result = await _dbSet
                .Where(e => e.Status == EmailStatus.Queued)
                .OrderBy(e => e.CreatedAt)
                .Take(batchSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
                
            _logger.Debug("Retrieved {Count} queued emails", result.Count);
            return result;
        }
    }

    public async Task<EmailQueueStats> GetEmailQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEmailQueueStats"))
        {
            _logger.Debug("Getting email queue statistics");
            
            var stats = await _dbSet
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
                
            var result = new EmailQueueStats(
                queued: stats.FirstOrDefault(s => s.Status == EmailStatus.Queued)?.Count ?? 0,
                sent: stats.FirstOrDefault(s => s.Status == EmailStatus.Sent)?.Count ?? 0,
                failed: stats.FirstOrDefault(s => s.Status == EmailStatus.Failed)?.Count ?? 0,
                processing: stats.FirstOrDefault(s => s.Status == EmailStatus.Processing)?.Count ?? 0
            );
            
            _logger.Debug("Email queue stats retrieved: {@Stats}", result);
            return result;
        }
    }
}
```

### 3.2 Query Optimization Patterns

```csharp
// ✅ OPTIMIZED: Use indexes and efficient queries
public async Task<IReadOnlyList<EmailMessage>> GetEmailsByTypeAsync(
    EmailType type, 
    DateTime? fromDate = null, 
    DateTime? toDate = null, 
    CancellationToken cancellationToken = default)
{
    var query = _dbSet.AsNoTracking()
        .Where(e => e.Type == type);

    if (fromDate.HasValue)
        query = query.Where(e => e.CreatedAt >= fromDate.Value);

    if (toDate.HasValue)
        query = query.Where(e => e.CreatedAt <= toDate.Value);

    return await query
        .OrderByDescending(e => e.CreatedAt)
        .ToListAsync(cancellationToken);
}

// ✅ BULK OPERATIONS: Efficient batch processing
public async Task<int> MarkAsProcessedAsync(
    IEnumerable<Guid> emailIds, 
    CancellationToken cancellationToken = default)
{
    var ids = emailIds.ToList();
    
    return await _context.Database.ExecuteSqlRawAsync(
        "UPDATE communications.email_messages SET status = {0}, updated_at = {1} WHERE id = ANY({2})",
        EmailStatus.Processing,
        DateTime.UtcNow,
        ids.ToArray(),
        cancellationToken);
}
```

## 4. Integration Testing Strategy

### 4.1 Test Database Configuration

```csharp
public class IntegrationTestBase : IDisposable
{
    protected readonly AppDbContext Context;
    private readonly DbConnection _connection;

    protected IntegrationTestBase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected async Task<T> AddEntityAsync<T>(T entity) where T : BaseEntity
    {
        Context.Set<T>().Add(entity);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear(); // Ensure fresh queries
        return entity;
    }

    public void Dispose()
    {
        Context?.Dispose();
        _connection?.Dispose();
    }
}

[TestFixture]
public class EmailMessageRepositoryIntegrationTests : IntegrationTestBase
{
    private EmailMessageRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _repository = new EmailMessageRepository(Context);
    }

    [Test]
    public async Task Integration_EmailMessageCRUD_WorksCorrectly()
    {
        // Arrange
        var emailMessage = EmailMessageTestDataBuilder.Create()
            .WithSubject(EmailSubject.Create("Test Subject").Value)
            .WithStatus(EmailStatus.Queued)
            .Build();

        // Act & Assert - Create
        await _repository.AddAsync(emailMessage);
        await Context.SaveChangesAsync();

        // Act & Assert - Read
        var retrieved = await _repository.GetByIdAsync(emailMessage.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Subject.Value.Should().Be("Test Subject");

        // Act & Assert - Update
        retrieved.MarkAsSent();
        _repository.Update(retrieved);
        await Context.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(emailMessage.Id);
        updated!.Status.Should().Be(EmailStatus.Sent);
    }
}
```

### 4.2 Repository-Specific Integration Tests

```csharp
[TestFixture]
public class EmailMessageRepositoryQueryTests : IntegrationTestBase
{
    private EmailMessageRepository _repository;

    [SetUp]
    public void SetUp()
    {
        _repository = new EmailMessageRepository(Context);
    }

    [Test]
    public async Task GetQueuedEmailsAsync_WithMultipleStatuses_ReturnsOnlyQueued()
    {
        // Arrange
        var queuedEmails = Enumerable.Range(1, 3)
            .Select(_ => EmailMessageTestDataBuilder.Create()
                .WithStatus(EmailStatus.Queued)
                .Build())
            .ToList();

        var sentEmails = Enumerable.Range(1, 2)
            .Select(_ => EmailMessageTestDataBuilder.Create()
                .WithStatus(EmailStatus.Sent)
                .Build())
            .ToList();

        foreach (var email in queuedEmails.Concat(sentEmails))
        {
            await AddEntityAsync(email);
        }

        // Act
        var result = await _repository.GetQueuedEmailsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(e => e.Status.Should().Be(EmailStatus.Queued));
    }

    [Test]
    public async Task GetEmailQueueStatsAsync_WithVariousStatuses_ReturnsCorrectCounts()
    {
        // Arrange
        await AddEntityAsync(EmailMessageTestDataBuilder.Create().WithStatus(EmailStatus.Queued).Build());
        await AddEntityAsync(EmailMessageTestDataBuilder.Create().WithStatus(EmailStatus.Queued).Build());
        await AddEntityAsync(EmailMessageTestDataBuilder.Create().WithStatus(EmailStatus.Sent).Build());
        await AddEntityAsync(EmailMessageTestDataBuilder.Create().WithStatus(EmailStatus.Failed).Build());

        // Act
        var stats = await _repository.GetEmailQueueStatsAsync();

        // Assert
        stats.Queued.Should().Be(2);
        stats.Sent.Should().Be(1);
        stats.Failed.Should().Be(1);
        stats.Processing.Should().Be(0);
    }
}
```

## 5. Performance Considerations

### 5.1 Database Indexing Strategy

```sql
-- Email Messages Performance Indexes
CREATE INDEX IX_EmailMessages_Status_CreatedAt 
ON communications.email_messages (status, created_at);

CREATE INDEX IX_EmailMessages_Type_CreatedAt 
ON communications.email_messages (type, created_at);

CREATE INDEX IX_EmailMessages_Status_RetryCount 
ON communications.email_messages (status, retry_count) 
WHERE status = 'Failed';

-- Email Templates Performance Indexes
CREATE INDEX IX_EmailTemplates_Category_IsActive 
ON communications.email_templates (category, is_active);

CREATE INDEX IX_EmailTemplates_Name_IsActive 
ON communications.email_templates (name, is_active);

-- User Email Preferences Performance Indexes
CREATE INDEX IX_UserEmailPreferences_UserId 
ON communications.user_email_preferences (user_id);

CREATE INDEX IX_UserEmailPreferences_Email 
ON communications.user_email_preferences (email);
```

### 5.2 Query Optimization Patterns

```csharp
// ✅ EFFICIENT: Use compiled queries for frequent operations
private static readonly Func<AppDbContext, EmailStatus, int, IAsyncEnumerable<EmailMessage>> 
    GetEmailsByStatusCompiled = EF.CompileAsyncQuery(
        (AppDbContext context, EmailStatus status, int limit) =>
            context.EmailMessages
                .Where(e => e.Status == status)
                .OrderBy(e => e.CreatedAt)
                .Take(limit));

public async Task<IReadOnlyList<EmailMessage>> GetEmailsByStatusAsync(
    EmailStatus status, 
    int limit = 100, 
    CancellationToken cancellationToken = default)
{
    var emails = new List<EmailMessage>();
    await foreach (var email in GetEmailsByStatusCompiled(_context, status, limit)
        .WithCancellation(cancellationToken))
    {
        emails.Add(email);
    }
    return emails;
}

// ✅ EFFICIENT: Projection queries for analytics
public async Task<Dictionary<EmailStatus, int>> GetStatusCountsAsync(
    DateTime? fromDate = null, 
    CancellationToken cancellationToken = default)
{
    var query = _dbSet.AsNoTracking();
    
    if (fromDate.HasValue)
        query = query.Where(e => e.CreatedAt >= fromDate.Value);

    return await query
        .GroupBy(e => e.Status)
        .Select(g => new { Status = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
}
```

### 5.3 Connection and Memory Management

```csharp
// ✅ EFFICIENT: Streaming large datasets
public async IAsyncEnumerable<EmailMessage> StreamQueuedEmailsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var email in _dbSet
        .Where(e => e.Status == EmailStatus.Queued)
        .OrderBy(e => e.CreatedAt)
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken))
    {
        yield return email;
    }
}

// ✅ EFFICIENT: Batch processing with pagination
public async Task<Result<int>> ProcessQueuedEmailsBatchAsync(
    int batchSize = 100,
    CancellationToken cancellationToken = default)
{
    try
    {
        var processedCount = 0;
        int page = 1;

        while (true)
        {
            var (emails, totalCount) = await GetPagedAsync(
                page, 
                batchSize, 
                e => e.Status == EmailStatus.Queued, 
                cancellationToken);

            if (!emails.Any())
                break;

            // Process batch...
            processedCount += emails.Count;
            page++;
        }

        return Result<int>.Success(processedCount);
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Error processing queued emails batch");
        return Result<int>.Failure($"Batch processing failed: {ex.Message}");
    }
}
```

## 6. Error Handling and Result Pattern Integration

### 6.1 Repository Result Pattern Implementation

```csharp
// Enhanced repository methods with Result pattern
public async Task<Result<EmailMessage>> CreateEmailMessageAsync(
    EmailMessage emailMessage, 
    CancellationToken cancellationToken = default)
{
    try
    {
        using (LogContext.PushProperty("Operation", "CreateEmailMessage"))
        using (LogContext.PushProperty("EmailId", emailMessage.Id))
        {
            await AddAsync(emailMessage, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.Information("Email message created successfully {EmailId}", emailMessage.Id);
            return Result<EmailMessage>.Success(emailMessage);
        }
    }
    catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
    {
        _logger.Warning("Email message creation failed due to duplicate {EmailId}", emailMessage.Id);
        return Result<EmailMessage>.Failure("Email message already exists");
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed to create email message {EmailId}", emailMessage.Id);
        return Result<EmailMessage>.Failure($"Failed to create email message: {ex.Message}");
    }
}

public async Task<Result<EmailMessage>> UpdateEmailStatusAsync(
    Guid emailId, 
    EmailStatus status, 
    string? errorMessage = null,
    CancellationToken cancellationToken = default)
{
    try
    {
        var email = await GetByIdAsync(emailId, cancellationToken);
        if (email == null)
            return Result<EmailMessage>.Failure($"Email message not found: {emailId}");

        email.UpdateStatus(status, errorMessage);
        Update(email);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<EmailMessage>.Success(email);
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed to update email status {EmailId} to {Status}", emailId, status);
        return Result<EmailMessage>.Failure($"Failed to update email status: {ex.Message}");
    }
}
```

### 6.2 Exception Handling Extensions

```csharp
public static class DbExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("duplicate key") == true ||
               ex.InnerException?.Message?.Contains("UNIQUE constraint") == true;
    }

    public static bool IsForeignKeyViolation(this DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("foreign key constraint") == true ||
               ex.InnerException?.Message?.Contains("REFERENCE constraint") == true;
    }
}

// Repository base class enhancement
public abstract class RepositoryBase<T> : Repository<T> where T : BaseEntity
{
    protected RepositoryBase(AppDbContext context) : base(context) { }

    protected async Task<Result> HandleDbUpdateAsync(
        Func<Task> operation,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await operation();
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            _logger.Warning("Unique constraint violation in {Operation}", operationName);
            return Result.Failure("A record with the same key already exists");
        }
        catch (DbUpdateException ex) when (ex.IsForeignKeyViolation())
        {
            _logger.Warning("Foreign key constraint violation in {Operation}", operationName);
            return Result.Failure("Referenced record does not exist");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error in {Operation}", operationName);
            return Result.Failure($"{operationName} failed: {ex.Message}");
        }
    }
}
```

## 7. Implementation Checklist

### 7.1 Repository Implementation Steps

1. **Interface Consistency**: ✅ Update all Communication repository interfaces to extend `IRepository<T>`
2. **Base Repository Usage**: ✅ Implement concrete repositories extending `Repository<T>`
3. **Specialized Methods**: ✅ Add domain-specific query methods
4. **Result Pattern Integration**: ✅ Wrap operations in Result types for error handling
5. **Performance Optimization**: ✅ Implement efficient queries and indexing
6. **Logging Integration**: ✅ Use structured logging with context
7. **Test Coverage**: ✅ Implement comprehensive unit and integration tests

### 7.2 Testing Strategy Checklist

1. **TDD Implementation**: ✅ Follow Red-Green-Refactor cycle
2. **Unit Tests**: ✅ Mock dependencies, test individual methods
3. **Integration Tests**: ✅ Real database, full repository functionality
4. **Performance Tests**: ✅ Test query efficiency and bulk operations
5. **Error Scenario Tests**: ✅ Test exception handling and Result patterns
6. **Data Builder Pattern**: ✅ Use test data builders for consistent test data

## Conclusion

This architecture provides a solid foundation for implementing Communications repositories with:

- **Consistency**: All repositories follow the same patterns
- **Testability**: Comprehensive TDD approach with proper test structure
- **Performance**: Optimized queries and efficient data access patterns
- **Maintainability**: Clean separation of concerns and error handling
- **Scalability**: Designed for high-volume email processing scenarios

The implementation should follow the established patterns in your codebase while addressing the specific needs of the Communications domain.