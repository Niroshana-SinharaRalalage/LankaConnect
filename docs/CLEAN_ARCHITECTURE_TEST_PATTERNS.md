# Clean Architecture Test Patterns for Domain Testing

## Overview

This document defines comprehensive test patterns that ensure 100% domain test coverage while maintaining Clean Architecture principles and separation of concerns.

## Architectural Testing Principles

### **1. Domain Isolation**
```csharp
// ✅ CORRECT: Domain tests should only test domain logic
[Test]
public void User_Create_WithValidData_ShouldSucceed()
{
    // Arrange - Pure domain objects only
    var email = Email.Create("user@example.com").Value;
    
    // Act - Domain behavior only
    var result = User.Create(email, "John", "Doe");
    
    // Assert - Domain state only
    result.IsSuccess.Should().BeTrue();
    result.Value.Email.Should().Be(email);
}

// ❌ WRONG: Never depend on infrastructure in domain tests
[Test]
public void User_Create_ShouldSaveToDatabase() // WRONG - Infrastructure concern
{
    var repository = new UserRepository(); // WRONG - Infrastructure dependency
    // ... test code
}
```

### **2. Behavior-Driven Testing**
```csharp
// ✅ CORRECT: Test business behavior, not implementation
[Test]
public void User_ChangePassword_WhenValidPassword_ShouldClearFailedAttempts()
{
    // Arrange
    var user = CreateUserWithFailedAttempts(3);
    
    // Act
    var result = user.ChangePassword("newHashedPassword");
    
    // Assert - Focus on business behavior
    result.IsSuccess.Should().BeTrue();
    user.FailedLoginAttempts.Should().Be(0);
    user.AccountLockedUntil.Should().BeNull();
}
```

### **3. Aggregate Boundary Respect**
```csharp
// ✅ CORRECT: Test aggregate as a cohesive unit
[Test]
public void Business_AddService_WhenBusinessActive_ShouldAddService()
{
    // Arrange - Complete aggregate state
    var business = CreateActiveBusiness();
    var service = CreateValidService();
    
    // Act - Through aggregate root
    var result = business.AddService(service);
    
    // Assert - Aggregate invariants maintained
    result.IsSuccess.Should().BeTrue();
    business.Services.Should().Contain(service);
    business.IsActive.Should().BeTrue(); // Invariant maintained
}
```

## Domain Test Pattern Catalog

### **1. Entity Test Pattern**

```csharp
public abstract class EntityTestBase<T> where T : BaseEntity
{
    protected abstract T CreateValidEntity();
    protected abstract T CreateInvalidEntity();
    
    [Test]
    public void Entity_Create_ShouldHaveValidId()
    {
        // Arrange & Act
        var entity = CreateValidEntity();
        
        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
    
    [Test] 
    public void Entity_MarkAsUpdated_ShouldUpdateTimestamp()
    {
        // Arrange
        var entity = CreateValidEntity();
        var originalUpdatedAt = entity.UpdatedAt;
        
        // Act
        entity.MarkAsUpdated();
        
        // Assert
        entity.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }
}

// Specific entity test implementation
public class UserTests : EntityTestBase<User>
{
    protected override User CreateValidEntity()
    {
        var email = Email.Create("test@example.com").Value;
        return User.Create(email, "John", "Doe").Value;
    }
    
    protected override User CreateInvalidEntity()
    {
        // Return entity that violates business rules
        return null; // Will be handled in specific tests
    }
    
    [Test]
    public void User_Create_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        Email invalidEmail = null;
        
        // Act
        var result = User.Create(invalidEmail, "John", "Doe");
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email is required");
    }
}
```

### **2. Value Object Test Pattern**

```csharp
public abstract class ValueObjectTestBase<T> where T : ValueObject
{
    protected abstract T CreateValidValueObject();
    protected abstract T CreateEqualValueObject();
    protected abstract T CreateDifferentValueObject();
    protected abstract object[] GetInvalidConstructorParams();
    
    [Test]
    public void ValueObject_Equality_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var valueObject1 = CreateValidValueObject();
        var valueObject2 = CreateEqualValueObject();
        
        // Act & Assert
        valueObject1.Should().Be(valueObject2);
        (valueObject1 == valueObject2).Should().BeTrue();
        valueObject1.GetHashCode().Should().Be(valueObject2.GetHashCode());
    }
    
    [Test]
    public void ValueObject_Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var valueObject1 = CreateValidValueObject();
        var valueObject2 = CreateDifferentValueObject();
        
        // Act & Assert
        valueObject1.Should().NotBe(valueObject2);
        (valueObject1 != valueObject2).Should().BeTrue();
    }
    
    [Test]
    public void ValueObject_Create_WithInvalidData_ShouldThrowOrReturnFailure()
    {
        // Arrange
        var invalidParams = GetInvalidConstructorParams();
        
        // Act & Assert
        Action act = () => (T)Activator.CreateInstance(typeof(T), invalidParams);
        act.Should().Throw<ArgumentException>()
           .Or.Throw<DomainException>();
    }
}

// Specific value object test implementation
public class EmailTests : ValueObjectTestBase<Email>
{
    protected override Email CreateValidValueObject()
        => Email.Create("test@example.com").Value;
        
    protected override Email CreateEqualValueObject()
        => Email.Create("test@example.com").Value;
        
    protected override Email CreateDifferentValueObject()
        => Email.Create("different@example.com").Value;
        
    protected override object[] GetInvalidConstructorParams()
        => new object[] { "invalid-email" };
    
    [Test]
    public void Email_Create_WithValidEmail_ShouldSucceed()
    {
        // Arrange
        var validEmails = new[]
        {
            "user@example.com",
            "first.last@subdomain.example.com",
            "user+tag@example.com"
        };
        
        foreach (var email in validEmails)
        {
            // Act
            var result = Email.Create(email);
            
            // Assert
            result.IsSuccess.Should().BeTrue($"'{email}' should be valid");
            result.Value.Value.Should().Be(email.ToLowerInvariant());
        }
    }
    
    [Test]
    public void Email_Create_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var invalidEmails = new[]
        {
            "",
            " ",
            "invalid",
            "@example.com",
            "user@",
            "user space@example.com"
        };
        
        foreach (var email in invalidEmails)
        {
            // Act
            var result = Email.Create(email);
            
            // Assert
            result.IsFailure.Should().BeTrue($"'{email}' should be invalid");
        }
    }
}
```

### **3. Aggregate Test Pattern**

```csharp
public abstract class AggregateTestBase<T> where T : class, IAggregateRoot
{
    protected abstract T CreateValidAggregate();
    
    [Test]
    public void Aggregate_DomainEvents_ShouldBeTracked()
    {
        // Arrange
        var aggregate = CreateValidAggregate();
        var initialEventCount = aggregate.DomainEvents.Count;
        
        // Act - Perform operation that should raise events
        PerformOperationThatRaisesEvent(aggregate);
        
        // Assert
        aggregate.DomainEvents.Count.Should().BeGreaterThan(initialEventCount);
    }
    
    [Test]
    public void Aggregate_ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var aggregate = CreateValidAggregate();
        PerformOperationThatRaisesEvent(aggregate);
        aggregate.DomainEvents.Should().NotBeEmpty();
        
        // Act
        aggregate.ClearDomainEvents();
        
        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }
    
    protected abstract void PerformOperationThatRaisesEvent(T aggregate);
}

// Specific aggregate implementation
public class BusinessTests : AggregateTestBase<Business>
{
    protected override Business CreateValidAggregate()
    {
        var profile = CreateValidBusinessProfile();
        var location = CreateValidBusinessLocation();
        return Business.Create(profile, location).Value;
    }
    
    protected override void PerformOperationThatRaisesEvent(Business aggregate)
    {
        aggregate.Activate(); // Should raise BusinessActivatedEvent
    }
    
    [Test]
    public void Business_Activate_WhenInactive_ShouldRaiseActivatedEvent()
    {
        // Arrange
        var business = CreateInactiveBusiness();
        business.ClearDomainEvents(); // Clear any creation events
        
        // Act
        business.Activate();
        
        // Assert
        business.IsActive.Should().BeTrue();
        business.DomainEvents.Should().ContainSingle()
                .Which.Should().BeOfType<BusinessActivatedEvent>();
    }
}
```

### **4. Domain Service Test Pattern**

```csharp
public class EmailTemplateCategoryServiceTests
{
    private readonly EmailTemplateCategoryService _service;
    
    public EmailTemplateCategoryServiceTests()
    {
        _service = new EmailTemplateCategoryService();
    }
    
    [Test]
    public void GetCategoryForEmailType_WithTransactionalEmail_ShouldReturnSystemCategory()
    {
        // Arrange
        var emailType = EmailType.Transactional;
        
        // Act
        var result = _service.GetCategoryForEmailType(emailType);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be("System");
        result.Value.Priority.Should().Be(1);
    }
    
    [Test]
    public void GetCategoryForEmailType_WithUnknownEmailType_ShouldReturnDefault()
    {
        // Arrange
        var emailType = (EmailType)999; // Invalid enum value
        
        // Act
        var result = _service.GetCategoryForEmailType(emailType);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Category.Should().Be("General");
        result.Value.Priority.Should().Be(5);
    }
}
```

### **5. Specification Test Pattern**

```csharp
public class BusinessSearchSpecificationTests
{
    [Test]
    public void BusinessSearchSpecification_WithNameFilter_ShouldMatchCorrectBusinesses()
    {
        // Arrange
        var businesses = new[]
        {
            CreateBusiness("Tech Solutions"),
            CreateBusiness("Software Solutions"),
            CreateBusiness("Hardware Store")
        };
        
        var specification = new BusinessSearchSpecification(
            searchTerm: "Solutions",
            category: null,
            location: null
        );
        
        // Act
        var matchingBusinesses = businesses.Where(b => specification.IsSatisfiedBy(b));
        
        // Assert
        matchingBusinesses.Should().HaveCount(2);
        matchingBusinesses.Should().Contain(b => b.Profile.Name == "Tech Solutions");
        matchingBusinesses.Should().Contain(b => b.Profile.Name == "Software Solutions");
    }
    
    [Test]
    public void BusinessSearchSpecification_WithCategoryFilter_ShouldMatchCategory()
    {
        // Arrange
        var businesses = new[]
        {
            CreateBusinessWithCategory(BusinessCategory.Technology),
            CreateBusinessWithCategory(BusinessCategory.Restaurant),
            CreateBusinessWithCategory(BusinessCategory.Technology)
        };
        
        var specification = new BusinessSearchSpecification(
            searchTerm: null,
            category: BusinessCategory.Technology,
            location: null
        );
        
        // Act
        var matchingBusinesses = businesses.Where(b => specification.IsSatisfiedBy(b));
        
        // Assert
        matchingBusinesses.Should().HaveCount(2);
        matchingBusinesses.Should().AllSatisfy(b => 
            b.Profile.Category.Should().Be(BusinessCategory.Technology));
    }
}
```

## Test Data Builder Patterns

### **1. Fluent Builder Pattern**

```csharp
public class UserTestDataBuilder
{
    private Email _email = Email.Create("test@example.com").Value;
    private string _firstName = "John";
    private string _lastName = "Doe";
    private UserRole _role = UserRole.User;
    private bool _isEmailVerified = false;
    private int _failedAttempts = 0;
    
    public UserTestDataBuilder WithEmail(string email)
    {
        _email = Email.Create(email).Value;
        return this;
    }
    
    public UserTestDataBuilder WithName(string firstName, string lastName)
    {
        _firstName = firstName;
        _lastName = lastName;
        return this;
    }
    
    public UserTestDataBuilder WithRole(UserRole role)
    {
        _role = role;
        return this;
    }
    
    public UserTestDataBuilder WithEmailVerified(bool isVerified = true)
    {
        _isEmailVerified = isVerified;
        return this;
    }
    
    public UserTestDataBuilder WithFailedAttempts(int attempts)
    {
        _failedAttempts = attempts;
        return this;
    }
    
    public User Build()
    {
        var user = User.Create(_email, _firstName, _lastName, _role).Value;
        
        if (_isEmailVerified)
            user.VerifyEmail();
            
        for (int i = 0; i < _failedAttempts; i++)
            user.RecordFailedLoginAttempt();
            
        return user;
    }
    
    public static UserTestDataBuilder AUser() => new();
    public static UserTestDataBuilder AnAdmin() => new UserTestDataBuilder().WithRole(UserRole.Admin);
    public static UserTestDataBuilder ALockedUser() => new UserTestDataBuilder().WithFailedAttempts(5);
}

// Usage in tests
[Test]
public void User_Login_WhenAccountLocked_ShouldFail()
{
    // Arrange
    var user = UserTestDataBuilder.ALockedUser()
                    .WithEmail("locked@example.com")
                    .Build();
    
    // Act & Assert
    user.IsAccountLocked.Should().BeTrue();
}
```

### **2. Factory Pattern for Complex Objects**

```csharp
public static class EmailMessageTestFactory
{
    public static EmailMessage CreateTransactionalEmail(
        string recipientEmail = "test@example.com",
        string subject = "Test Subject",
        string content = "Test Content")
    {
        var fromEmail = Email.Create("noreply@lankaconnect.com").Value;
        var toEmail = Email.Create(recipientEmail).Value;
        var emailSubject = EmailSubject.Create(subject).Value;
        
        var message = EmailMessage.Create(
            fromEmail, 
            emailSubject, 
            content, 
            $"<p>{content}</p>", 
            EmailType.Transactional
        ).Value;
        
        message.AddRecipient(toEmail);
        return message;
    }
    
    public static EmailMessage CreateFailedEmail(int retryCount = 1)
    {
        var email = CreateTransactionalEmail();
        for (int i = 0; i < retryCount; i++)
        {
            email.MarkAsFailed($"Failure {i + 1}", DateTime.UtcNow.AddMinutes(5));
        }
        return email;
    }
}
```

## Coverage Validation Patterns

### **1. Comprehensive Edge Case Testing**

```csharp
[Test]
public void Email_Create_EdgeCases_ShouldHandleCorrectly()
{
    var testCases = new[]
    {
        // Valid edge cases
        ("a@b.co", true, "Minimal valid email"),
        ("test.email+tag@example-domain.com", true, "Complex valid email"),
        
        // Invalid edge cases  
        ("", false, "Empty string"),
        ("   ", false, "Whitespace only"),
        ("plainaddress", false, "No @ symbol"),
        ("@missingdomain.com", false, "Missing local part"),
        ("missing.domain@.com", false, "Missing domain"),
        ("toolong" + new string('x', 255) + "@domain.com", false, "Too long")
    };
    
    foreach (var (email, shouldSucceed, description) in testCases)
    {
        // Act
        var result = Email.Create(email);
        
        // Assert
        if (shouldSucceed)
        {
            result.IsSuccess.Should().BeTrue($"{description} should be valid: '{email}'");
        }
        else
        {
            result.IsFailure.Should().BeTrue($"{description} should be invalid: '{email}'");
        }
    }
}
```

### **2. State Transition Testing**

```csharp
[Test]
public void EmailMessage_StatusTransitions_ShouldFollowValidPaths()
{
    var validTransitions = new[]
    {
        (EmailStatus.Pending, EmailStatus.Queued),
        (EmailStatus.Queued, EmailStatus.Sending),
        (EmailStatus.Sending, EmailStatus.Sent),
        (EmailStatus.Sent, EmailStatus.Delivered),
        (EmailStatus.Queued, EmailStatus.Failed),
        (EmailStatus.Sending, EmailStatus.Failed),
        (EmailStatus.Failed, EmailStatus.Queued) // Retry
    };
    
    var invalidTransitions = new[]
    {
        (EmailStatus.Delivered, EmailStatus.Pending),
        (EmailStatus.Sent, EmailStatus.Sending),
        (EmailStatus.Failed, EmailStatus.Sent)
    };
    
    foreach (var (from, to) in validTransitions)
    {
        // Arrange
        var email = CreateEmailInStatus(from);
        
        // Act & Assert
        var result = TransitionEmailToStatus(email, to);
        result.IsSuccess.Should().BeTrue($"Transition from {from} to {to} should be valid");
    }
    
    foreach (var (from, to) in invalidTransitions)
    {
        // Arrange
        var email = CreateEmailInStatus(from);
        
        // Act & Assert
        var result = TransitionEmailToStatus(email, to);
        result.IsFailure.Should().BeTrue($"Transition from {from} to {to} should be invalid");
    }
}
```

## Test Organization Structure

```
tests/LankaConnect.Domain.Tests/
├── Common/
│   ├── BaseEntityTests.cs
│   ├── ResultTests.cs
│   ├── ValueObjectTests.cs
│   └── Patterns/
│       ├── EntityTestBase.cs
│       ├── ValueObjectTestBase.cs
│       └── AggregateTestBase.cs
├── Users/
│   ├── UserTests.cs
│   ├── ValueObjects/
│   │   ├── EmailTests.cs
│   │   ├── PhoneNumberTests.cs
│   │   └── RefreshTokenTests.cs
│   └── TestBuilders/
│       └── UserTestDataBuilder.cs
├── Business/
├── Communications/
├── Events/
├── Community/
└── TestHelpers/
    ├── DomainTestBase.cs
    ├── TestDataFactory.cs
    └── FluentAssertionsExtensions.cs
```

## Quality Gates

### **Coverage Requirements**
- **Line Coverage**: 100%
- **Branch Coverage**: 100%
- **Mutation Score**: >85%

### **Test Quality Standards**
- Each test focuses on single behavior
- Clear AAA (Arrange, Act, Assert) structure
- Descriptive test names explaining business scenarios
- No infrastructure dependencies
- Fast execution (<5ms per test)

This comprehensive pattern catalog ensures that all domain components can be tested systematically while maintaining Clean Architecture principles and achieving 100% test coverage.