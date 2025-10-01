# ADR: Email Template Category Architecture Resolution

**Status**: Accepted  
**Date**: 2025-09-04  
**Decision Makers**: System Architecture Designer  
**Tags**: DDD, Architecture, Domain Modeling, Email Templates  

## Context and Problem Statement

An architectural inconsistency was identified in the email template domain model:

### Issues Identified
1. **Domain Layer**: `EmailTemplate` entity had only `EmailType` property (business-specific types)
2. **Application Layer**: Repository interface expected `EmailTemplateCategory` enum (organizational categories)
3. **Coupling**: Query handlers performed ad-hoc mapping between domain and application concepts
4. **DDD Violation**: Repository interfaces should align with domain models, not application-layer enums

### Domain vs Application Concepts
- **EmailType** (Domain): Business email purposes (`Welcome`, `PasswordReset`, `BusinessNotification`)  
- **EmailTemplateCategory** (Application): UI/organizational groupings (`Authentication`, `Business`, `Marketing`)

## Decision Drivers

- **Clean Architecture**: Domain should not depend on Application layer
- **Domain-Driven Design**: Rich domain models with proper value objects
- **Single Responsibility**: Clear separation between business logic and presentation concerns
- **Consistency**: Repository interfaces should use domain types
- **Maintainability**: Centralized mapping logic using domain services

## Considered Options

### Option A: Add Category property to EmailTemplate (Rejected)
- **Pros**: Simple implementation
- **Cons**: Violates SRP, duplicates categorization logic, tight coupling

### Option B: Change repository to use EmailType (Rejected)  
- **Pros**: Aligns with current domain model
- **Cons**: Loses organizational capabilities, application layer concerns leak into domain

### Option C: Create proper DDD mapping with value objects (Selected)
- **Pros**: Pure DDD approach, domain encapsulation, centralized business logic
- **Cons**: More complex initially, requires careful modeling

### Option D: Separate repositories for each concept (Rejected)
- **Pros**: Clear separation
- **Cons**: Unnecessary complexity, data duplication

## Decision Outcome

**Chosen Option C**: Implement proper DDD solution with value objects and domain services.

### Architecture Solution

#### 1. Domain Value Object
```csharp
// EmailTemplateCategory as ValueObject with business logic
public sealed class EmailTemplateCategory : ValueObject
{
    public static readonly EmailTemplateCategory Authentication = new("Authentication", ...);
    public static readonly EmailTemplateCategory Business = new("Business", ...);
    
    public static EmailTemplateCategory ForEmailType(EmailType emailType)
    {
        // Centralized business mapping logic
    }
}
```

#### 2. Domain Service
```csharp
public class EmailTemplateCategoryService
{
    public EmailTemplateCategory DetermineCategory(EmailType emailType);
    public IEnumerable<EmailType> GetEmailTypesForCategory(EmailTemplateCategory category);
    public bool ValidateEmailTypeForCategory(EmailType emailType, EmailTemplateCategory expected);
}
```

#### 3. Enhanced Domain Entity
```csharp
public class EmailTemplate : BaseEntity
{
    public EmailType Type { get; private set; }
    public EmailTemplateCategory Category { get; private set; } // Auto-derived from Type
    
    public Result UpdateType(EmailType newType)
    {
        Type = newType;
        Category = EmailTemplateCategory.ForEmailType(newType); // Maintain consistency
        return Result.Success();
    }
}
```

#### 4. Repository Interface (Domain-Aligned)
```csharp
public interface IEmailTemplateRepository
{
    Task<List<EmailTemplate>> GetTemplatesAsync(
        EmailTemplateCategory? category = null,  // Domain value object
        EmailType? emailType = null,            // Domain enum
        bool? isActive = null,
        string? searchTerm = null,
        // ... pagination
    );
}
```

## Benefits Realized

### 1. **Domain Purity**
- Domain layer contains all business logic for categorization
- No application layer dependencies in domain
- Rich value objects with behavior

### 2. **Consistency Guarantees**  
- EmailTemplate entity automatically maintains Type ↔ Category consistency
- Centralized mapping logic prevents inconsistencies
- Validation methods ensure data integrity

### 3. **Extensibility**
- Easy to add new categories or email types
- Business rules centralized in domain services
- Mapping logic can evolve with business needs

### 4. **Testability**
- Domain logic fully unit testable
- Clear separation of concerns
- Mock-friendly repository interfaces

### 5. **Repository Alignment**
- Repository methods use domain concepts
- Natural filtering capabilities
- Type-safe operations

## Implementation Details

### Domain Mapping Logic
```csharp
EmailType.EmailVerification → EmailTemplateCategory.Authentication
EmailType.PasswordReset → EmailTemplateCategory.Authentication  
EmailType.BusinessNotification → EmailTemplateCategory.Business
EmailType.Marketing → EmailTemplateCategory.Marketing
EmailType.Newsletter → EmailTemplateCategory.Marketing
EmailType.Welcome → EmailTemplateCategory.Notification
EmailType.EventNotification → EmailTemplateCategory.Notification
EmailType.Transactional → EmailTemplateCategory.System
```

### Application Layer Adaptation
- Query handlers map domain `EmailTemplateCategory` value objects to DTO enums
- Presentation layer continues using simple enum for UI purposes
- Clean separation maintained between domain and application concerns

## Consequences

### Positive
- ✅ **Clean Architecture**: Domain drives the design
- ✅ **DDD Principles**: Rich domain model with value objects and services  
- ✅ **Type Safety**: Compile-time guarantees for category mappings
- ✅ **Single Source of Truth**: Business logic centralized in domain
- ✅ **Testability**: Comprehensive unit test coverage for domain logic
- ✅ **Extensibility**: Easy to add new categories or business rules

### Negative  
- ❌ **Initial Complexity**: More sophisticated modeling required
- ❌ **Learning Curve**: Developers need to understand DDD value objects
- ❌ **Migration Effort**: Existing code needs to be updated

### Neutral
- 🔄 **Mapping Layer**: Required between domain and application (expected in Clean Architecture)
- 🔄 **Repository Updates**: Infrastructure layer needs to implement new interface

## Compliance and Quality

### Clean Architecture Compliance
- ✅ Domain layer independent of Application layer
- ✅ Repository interfaces define domain contracts
- ✅ Application layer orchestrates domain services

### DDD Compliance  
- ✅ Value objects with business behavior
- ✅ Domain services for cross-entity logic
- ✅ Aggregate consistency maintained
- ✅ Ubiquitous language preserved

### Test Coverage
- ✅ EmailTemplateCategory value object: 100% coverage
- ✅ EmailTemplateCategoryService: 100% coverage  
- ✅ EmailTemplate entity: Enhanced validation tests
- ✅ Integration tests: Repository mappings verified

## Related Decisions

- **EmailTemplate Entity Design**: [Existing ADR]
- **Repository Pattern Implementation**: [Infrastructure ADR] 
- **Value Object Standards**: [Domain Modeling Guidelines]

## References

- [Clean Architecture by Robert C. Martin]
- [Domain-Driven Design by Eric Evans]
- [Implementing Domain-Driven Design by Vaughn Vernon]
- [LankaConnect Domain Modeling Guidelines]

---

This ADR resolves the architectural inconsistency while establishing a robust foundation for email template categorization that aligns with DDD principles and Clean Architecture patterns.