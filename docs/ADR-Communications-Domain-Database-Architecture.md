# Architecture Decision Record: Communications Domain Database Integration Issues

**Status**: Resolved
**Date**: 2025-09-05  
**Decision Makers**: System Architecture Designer, TDD Team  
**Context**: Database integration test failures in Communications domain

## Problem Statement

Database integration tests for the Communications domain are failing due to schema configuration issues:

1. **Foreign Key Violations**: EmailMessage entities failing to insert due to missing foreign key relationships
2. **Entity Configuration Misalignment**: EF Core configurations don't match domain entity expectations
3. **Test Entity Creation Issues**: Integration tests using incompatible entity creation patterns

## Analysis of Current Issues

### 1. Schema Design Problems

**Current EmailMessage Schema**:
- No foreign key relationships defined to Users table
- EmailTemplate and EmailMessage are independent (no FK relationship)
- UserEmailPreferences has proper FK to Users (working correctly)

**Domain Entity Expectations**:
```csharp
// EmailMessage entity expects UserId relationship but DB schema has none
public class EmailMessage : BaseEntity
{
    // Properties suggest no direct User FK, uses email addresses instead
    public UserEmail FromEmail { get; private set; }
    public IReadOnlyList<string> ToEmails => _recipients.AsReadOnly();
}
```

**Integration Test Usage**:
```csharp
// Test tries to create EmailMessage with UserId that doesn't exist in schema
var message = EmailMessage.Create(
    user.Id,  // This suggests FK relationship that doesn't exist
    recipientEmail, 
    "Test Subject", 
    "Test Body",
    EmailPriority.Normal
).Value;
```

### 2. Entity Design Inconsistencies

**Constructor Mismatch**:
- Domain entity has multiple constructors with different parameters
- Some constructors expect `UserEmail`, others expect `user.Id`
- EF Core configuration expects specific field patterns

**Value Object Complexity**:
- `EmailTemplateCategory` uses complex value object with string conversion
- JSON serialization for email collections may cause issues
- PostgreSQL-specific `jsonb` types require proper configuration

## Architectural Decisions

### Decision 1: Simplified Domain Design for TDD

**Decision**: Use email-based identification rather than foreign key relationships for EmailMessage

**Rationale**:
- Communications domain should be loosely coupled to Users domain
- Email addresses provide natural identification
- Reduces database complexity and foreign key dependencies
- Aligns with distributed system principles

**Implementation**:
```csharp
public class EmailMessage : BaseEntity
{
    // Use email addresses, not User IDs
    public UserEmail FromEmail { get; private set; }
    public IReadOnlyList<string> ToEmails { get; private set; }
    
    // Remove UserId dependencies
    public static Result<EmailMessage> Create(
        UserEmail fromEmail,
        UserEmail toEmail,  // Changed from UserId
        string subject,
        string body,
        EmailType type = EmailType.Transactional)
    {
        // Implementation...
    }
}
```

### Decision 2: Separate Integration Test Strategy

**Decision**: Create domain-specific integration tests that don't require complex FK relationships

**Rationale**:
- TDD should focus on domain logic, not database relationships
- Integration tests should test repository contracts, not complex scenarios
- Separate concerns: domain logic vs. data persistence

**Implementation**:
```csharp
public class EmailRepositoryIntegrationTests : DatabaseIntegrationTestBase
{
    [Fact]
    public async Task EmailMessage_CreateWithEmailAddresses_ShouldWork()
    {
        // Arrange - Use email addresses directly
        var fromEmail = UserEmail.Create("sender@example.com").Value;
        var toEmail = UserEmail.Create("recipient@example.com").Value;
        var subject = EmailSubject.Create("Test Subject").Value;
        
        var message = EmailMessage.Create(fromEmail, subject, "Test Body").Value;
        message.AddRecipient(toEmail);

        // Act
        await EmailMessageRepository.AddAsync(message);
        await UnitOfWork.CommitAsync();

        // Assert
        var retrieved = await EmailMessageRepository.GetByIdAsync(message.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("sender@example.com", retrieved.FromEmail.Value);
    }
}
```

### Decision 3: EF Core Configuration Alignment

**Decision**: Update EF Core configurations to match simplified domain design

**Rationale**:
- Remove unnecessary foreign key constraints
- Focus on value object mapping
- Ensure JSON serialization works correctly

**Implementation**:
```csharp
public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_messages", "communications");

        // Remove FK relationships to Users table
        // Focus on value object mapping
        builder.OwnsOne(e => e.FromEmail, fromEmail =>
        {
            fromEmail.Property(f => f.Value)
                .HasColumnName("from_email")
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.OwnsOne(e => e.Subject, subject =>
        {
            subject.Property(s => s.Value)
                .HasColumnName("subject")
                .HasMaxLength(200)
                .IsRequired();
        });

        // Configure email collections as simple JSON
        builder.Property("_recipients")
            .HasColumnName("to_emails")
            .HasColumnType("jsonb")
            .IsRequired();
    }
}
```

## Implementation Plan

### Phase 1: Fix Domain Entity Constructors (Priority: High)
1. Standardize EmailMessage constructors to use email addresses
2. Remove UserId-based creation methods
3. Update value object creation patterns

### Phase 2: Simplify EF Core Configuration (Priority: High)
1. Remove unnecessary foreign key relationships
2. Focus on value object mapping
3. Test JSON serialization for email collections

### Phase 3: Create Focused Integration Tests (Priority: Medium)
1. Design tests that validate repository contracts
2. Use email-based entity creation
3. Separate domain logic tests from integration tests

### Phase 4: Database Migration Strategy (Priority: Medium)
1. Review if current schema needs FK constraints
2. Consider performance implications of loose coupling
3. Plan for eventual consistency if needed

## Consequences

### Positive
- **Reduced Complexity**: Eliminates foreign key dependency issues
- **Better TDD**: Domain tests focus on business logic
- **Loose Coupling**: Communications domain independent of Users domain
- **Easier Testing**: Integration tests become more straightforward

### Negative
- **Data Integrity**: No database-enforced referential integrity between emails and users
- **Query Complexity**: Joining emails with users requires application-level logic
- **Eventual Consistency**: May need to handle orphaned email addresses

### Mitigation Strategies
1. **Application-Level validation**: Validate email addresses exist in Users domain before sending
2. **Domain Services**: Create EmailValidationService to handle cross-domain validation
3. **Event-Driven Updates**: Use domain events to maintain consistency
4. **Monitoring**: Add logging for orphaned email addresses

## Next Steps

1. **Immediate**: Fix EmailMessage entity constructors and Create methods
2. **Short-term**: Update integration tests to use email-based approach
3. **Medium-term**: Consider adding domain services for cross-domain validation
4. **Long-term**: Evaluate if eventual consistency approach works for business needs

## Related ADRs
- ADR: Clean Architecture Principles
- ADR: Domain-Driven Design Patterns
- ADR: Test-Driven Development Strategy

## References
- Eric Evans: Domain-Driven Design
- Vaughn Vernon: Implementing Domain-Driven Design  
- Martin Fowler: Patterns of Enterprise Application Architecture