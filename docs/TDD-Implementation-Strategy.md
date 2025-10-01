# TDD Implementation Strategy for Infrastructure Dependencies

## Overview
This document defines the Test-Driven Development approach for implementing missing Infrastructure layer dependencies, following the established patterns in the LankaConnect codebase.

## Established Patterns Analysis

### EmailMessageRepository Pattern (Reference Implementation)
The `EmailMessageRepository` demonstrates the quality standards for all new implementations:

```csharp
✅ Key Patterns Observed:
- Result<T> pattern for all operations
- Comprehensive structured logging with Serilog
- LogContext properties for correlation
- Async operations with CancellationToken
- DbUpdateConcurrencyException handling
- Input validation and null checks
- AsNoTracking for read-only operations
- Proper exception categorization
```

## TDD Cycle Framework

### Phase 1: RED (Write Failing Tests)
```
Test Categories Required:
1. Unit Tests (Repository/Service logic)
2. Integration Tests (Database interactions)  
3. Contract Tests (Interface compliance)
4. Error Handling Tests (Exception scenarios)
```

### Phase 2: GREEN (Minimal Implementation)
```
Implementation Requirements:
- Satisfy interface contracts
- Return proper Result<T> types
- Handle basic error cases
- Include minimal logging
```

### Phase 3: REFACTOR (Quality Enhancement)
```
Quality Improvements:
- Comprehensive error handling
- Performance optimization
- Logging standardization
- Documentation completion
```

## Service-Specific TDD Strategies

### 1. IUserEmailPreferencesRepository

#### RED Phase Tests Required:
```csharp
// Unit Tests
GetByUserIdAsync_WithValidId_ReturnsPreferences()
GetByUserIdAsync_WithInvalidId_ReturnsNull()
GetByEmailAsync_WithValidEmail_ReturnsPreferences()
AddAsync_WithValidPreferences_SucceedsWithResult()
UpdateAsync_WithValidPreferences_SucceedsWithResult()
DeleteAsync_WithValidUserId_SucceedsWithResult()
GetUsersByPreferenceAsync_WithCriteria_ReturnsFilteredList()

// Integration Tests  
UserEmailPreferencesRepository_DatabaseOperations_EndToEndTest()
UserEmailPreferencesRepository_ConcurrentAccess_HandlesCorrectly()

// Error Handling Tests
AddAsync_WithDuplicateUserId_ReturnsFailureResult()
UpdateAsync_WithConcurrencyConflict_ReturnsFailureResult()
DatabaseConnection_Failed_ReturnsAppropriateError()
```

#### GREEN Phase Implementation:
```csharp
public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>, IUserEmailPreferencesRepository
{
    public UserEmailPreferencesRepository(AppDbContext context) : base(context) { }

    // Minimal implementations that pass tests
    public async Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
    
    // ... other minimal implementations
}
```

#### REFACTOR Phase Enhancements:
```csharp
// Add comprehensive logging, error handling, performance optimization
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
```

### 2. IEmailService

#### RED Phase Tests Required:
```csharp
// Service Contract Tests
SendEmailAsync_WithValidMessage_ReturnsSuccessResult()
SendEmailAsync_WithInvalidMessage_ReturnsFailureResult()
SendTemplatedEmailAsync_WithValidTemplate_ReturnsSuccessResult()
SendBulkEmailAsync_WithValidMessages_ReturnsSuccessResult()
ValidateTemplateAsync_WithExistingTemplate_ReturnsSuccessResult()

// Integration Tests
EmailService_EndToEndFlow_SendsActualEmail()
EmailService_BulkSending_HandlesPartialFailures()

// Dependency Integration Tests
EmailService_UsesSimpleEmailService_ForActualSending()
EmailService_UsesTemplateService_ForTemplateRendering()
EmailService_UsesRepository_ForPersistence()
```

#### GREEN Phase Implementation:
```csharp
public class EmailService : IEmailService
{
    private readonly ISimpleEmailService _simpleEmailService;
    private readonly IEmailTemplateService _templateService;
    private readonly IEmailMessageRepository _repository;
    private readonly ILogger<EmailService> _logger;

    // Minimal implementation focusing on contract satisfaction
    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrEmpty(emailMessage.ToEmail))
                return Result.Failure("Recipient email is required");

            // Delegate to simple email service
            return await _simpleEmailService.SendEmailAsync(/* ... */);
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }
}
```

### 3. EF Core Configuration (BusinessLocation)

#### RED Phase Tests Required:
```csharp
// Entity Configuration Tests
BusinessLocationConfiguration_CanSaveAndRetrieve_CorrectlyMapsProperties()
BusinessLocationConfiguration_HandleNullCoordinates_StoresCorrectly()
BusinessLocationConfiguration_ValueObjectEquality_WorksCorrectly()

// Migration Tests
BusinessLocationMigration_GeneratesCorrectSchema()
BusinessLocationMigration_CanRunUpAndDown_WithoutDataLoss()
```

#### GREEN Phase Implementation:
```csharp
public class BusinessLocationConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.OwnsOne(b => b.Location, location =>
        {
            location.OwnsOne(l => l.Address, address =>
            {
                address.Property(a => a.Street).HasColumnName("Address_Street");
                address.Property(a => a.City).HasColumnName("Address_City");
                // ... other address properties
            });

            location.OwnsOne(l => l.Coordinates, coords =>
            {
                coords.Property(c => c.Latitude).HasColumnName("Coordinates_Latitude");
                coords.Property(c => c.Longitude).HasColumnName("Coordinates_Longitude");
            });
        });
    }
}
```

## Testing Infrastructure Requirements

### Test Data Builders
```csharp
// Create test data builders for consistent test setup
public class UserEmailPreferencesTestDataBuilder
{
    private UserEmailPreferences _preferences;
    
    public UserEmailPreferencesTestDataBuilder WithUserId(Guid userId) { /* ... */ }
    public UserEmailPreferencesTestDataBuilder WithEmailEnabled(bool enabled) { /* ... */ }
    public UserEmailPreferences Build() => _preferences;
}
```

### Test Database Configuration
```csharp
// Use in-memory database for unit tests
public class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        return new AppDbContext(options);
    }
}
```

### Integration Test Helpers
```csharp
// Database seeding and cleanup
public class DatabaseTestHelper
{
    public static async Task SeedTestDataAsync(AppDbContext context) { /* ... */ }
    public static async Task CleanupTestDataAsync(AppDbContext context) { /* ... */ }
}
```

## Quality Gates and Validation

### Code Quality Metrics
- **Test Coverage**: Minimum 90% for all new implementations
- **Cyclomatic Complexity**: Maximum 10 per method
- **Documentation**: XML documentation for all public members
- **Performance**: All database operations under 100ms for single operations

### Architecture Compliance Checklist
- [ ] No Infrastructure references in Application layer
- [ ] All repository methods return Result<T> types
- [ ] Comprehensive structured logging with correlation IDs
- [ ] Proper async/await usage with CancellationToken support
- [ ] Input validation and null checks
- [ ] Exception handling with appropriate error messages

### Testing Standards
- [ ] Each repository has unit tests for all interface methods
- [ ] Integration tests cover database operations and transactions
- [ ] Error handling tests cover all exception scenarios
- [ ] Performance tests validate acceptable response times
- [ ] Concurrency tests ensure thread-safety

## Implementation Timeline

### Day 1: Critical Dependencies
```
Morning: IMemoryCache registration and BusinessLocation EF config
Afternoon: Test and validate migration success
```

### Day 2: Repository Implementation  
```
Morning: UserEmailPreferencesRepository TDD cycle
Afternoon: EmailService TDD cycle
```

### Day 3: Integration and Validation
```
Morning: EmailStatusRepository analysis and resolution
Afternoon: End-to-end integration testing
```

## Success Criteria

1. **All Tests Pass**: Red-Green-Refactor cycle completed for each service
2. **Migration Success**: EF Core migrations run without dependency errors
3. **Architecture Compliance**: Clean Architecture boundaries maintained
4. **Performance Standards**: All operations meet response time requirements
5. **Quality Metrics**: Code coverage, complexity, and documentation standards met

This TDD strategy ensures systematic implementation while maintaining the high quality standards established in the existing codebase.