# Phase 6A.4: Stripe Payment Service Architecture Guidance

**Document Version**: 1.0
**Date**: 2025-11-24
**Status**: Architectural Decision Record (ADR)
**Phase**: 6A.4 - Stripe Payment Integration (50% Complete)

---

## Executive Summary

This document provides comprehensive architectural guidance for implementing the Stripe Payment Service layer while maintaining Clean Architecture principles, DDD patterns, and ensuring zero compilation errors. Based on analysis of your existing codebase patterns, this guidance aligns with established conventions in LankaConnect.

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Architectural Decisions](#architectural-decisions)
3. [Service Layer Design](#service-layer-design)
4. [Repository Pattern Implementation](#repository-pattern-implementation)
5. [Dependency Injection Setup](#dependency-injection-setup)
6. [Error Handling Strategy](#error-handling-strategy)
7. [TDD Strategy](#tdd-strategy)
8. [Security Best Practices](#security-best-practices)
9. [Code Structure](#code-structure)

---

## Current State Analysis

### Completed (50%)

**Domain Layer**:
- `User.cs`: Extended with Stripe properties (StripeCustomerId, StripeSubscriptionId, SubscriptionStatus)
- Domain methods: `SetStripeCustomerId()`, `ActivateSubscription()`, `UpdateSubscriptionStatus()`, `CancelSubscription()`
- Domain events: `UserSubscriptionActivatedEvent`, `UserSubscriptionStatusChangedEvent`, `UserSubscriptionCanceledEvent`

**Infrastructure Layer**:
- `StripeCustomer.cs`: Infrastructure entity for tracking Stripe customer sync data
- `StripeWebhookEvent.cs`: Infrastructure entity for webhook idempotency
- EF Core configurations: `StripeCustomerConfiguration`, `StripeWebhookEventConfiguration`
- Migration applied to Azure staging database

**Application Layer**:
- `IStripePaymentService`: Interface already defined with Cultural Intelligence billing operations

### Existing Patterns Identified

1. **Result Pattern**: Used throughout (`LankaConnect.Domain.Common.Result<T>`)
2. **Repository Pattern**: Generic `Repository<T>` with `IRepository<T>` interface
3. **Service Pattern**: Services in Infrastructure layer (e.g., `PasswordHashingService`, `JwtTokenService`)
4. **DI Registration**: Uses extension methods in `Program.cs` (`.AddInfrastructure()`, `.AddApplication()`)
5. **Error Handling**: Custom exceptions + Result pattern hybrid approach
6. **Logging**: Serilog with structured logging throughout

---

## Architectural Decisions

### ADR-001: Service Layer Placement

**Decision**: Place `StripePaymentService` in **Infrastructure Layer**

**Location**: `LankaConnect.Infrastructure/Payments/Services/StripePaymentService.cs`

**Rationale**:
1. **Clean Architecture Compliance**: The service directly calls Stripe API (external dependency), which is an infrastructure concern
2. **Dependency Direction**: Domain and Application layers should NOT depend on external APIs
3. **Existing Pattern**: Your codebase already places external integration services in Infrastructure:
   - `EmailService` (calls external email providers) → Infrastructure
   - `AzureBlobStorageService` (calls Azure Storage) → Infrastructure
   - `JwtTokenService` (calls external crypto libraries) → Infrastructure
4. **Interface Location**: `IStripePaymentService` stays in Application layer (already exists) for dependency inversion

**File Structure**:
```
LankaConnect.Infrastructure/
└── Payments/
    ├── Entities/
    │   ├── StripeCustomer.cs (✅ exists)
    │   └── StripeWebhookEvent.cs (✅ exists)
    ├── Configurations/
    │   ├── StripeCustomerConfiguration.cs (✅ exists)
    │   └── StripeWebhookEventConfiguration.cs (✅ exists)
    ├── Services/
    │   └── StripePaymentService.cs (🔨 to implement)
    └── Repositories/
        ├── StripeCustomerRepository.cs (🔨 to implement)
        └── StripeWebhookEventRepository.cs (🔨 to implement)
```

---

### ADR-002: Repository Pattern Implementation

**Decision**: Create **specific repository interfaces and implementations**

**Repositories to Create**:

1. **IStripeCustomerRepository** (Application layer)
2. **StripeCustomerRepository** (Infrastructure layer)
3. **IStripeWebhookEventRepository** (Application layer)
4. **StripeWebhookEventRepository** (Infrastructure layer)

**Rationale**:
1. **Existing Pattern**: Your codebase uses specific repositories (e.g., `IBusinessRepository`, `IEventRepository`, `IUserRepository`)
2. **Specialized Queries**: Stripe repositories need domain-specific methods:
   - `GetByStripeCustomerIdAsync(string stripeCustomerId)`
   - `GetByUserIdAsync(Guid userId)`
   - `IsWebhookEventProcessedAsync(string eventId)`
3. **SRP**: Separation of concerns - payment repositories should not be generic
4. **Testability**: Easier to mock specific interfaces than generic repositories

**DO NOT**: Use generic `IRepository<T>` directly - it lacks specialized query methods needed for Stripe operations.

---

### ADR-003: Stripe SDK Dependency Injection

**Decision**: Use **Option A - IStripeClient Singleton**

**Implementation**:
```csharp
services.AddSingleton<IStripeClient>(sp =>
    new StripeClient(configuration["Stripe:SecretKey"]));
```

**Rationale**:
1. **Testability**: IStripeClient can be mocked for unit tests
2. **Configuration Isolation**: Keeps Stripe configuration separate from global static state
3. **DI Compliance**: Follows standard DI patterns used throughout your codebase
4. **Thread Safety**: StripeClient is thread-safe and designed for singleton use
5. **Official Recommendation**: Stripe.net SDK documentation recommends this approach

**DO NOT**: Use `StripeConfiguration.ApiKey` (global static state - difficult to test and violates DI principles)

---

### ADR-004: Service Interface Design

**Decision**: Implement existing `IStripePaymentService` interface + Add MVP-specific methods

**Existing Interface** (from `LankaConnect.Application/Common/Interfaces/IStripePaymentService.cs`):
```csharp
public interface IStripePaymentService
{
    Task<Result> CreateSubscriptionAsync(CreateStripeSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result> CreateEnterpriseSubscriptionAsync(CreateEnterpriseSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChargeUsageAsync(ChargeUsageRequest request, CancellationToken cancellationToken = default);
    Task<Result> CreatePartnerPayoutAsync(CreatePartnerPayoutRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    Task<Result> UpdateSubscriptionAsync(UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result<StripeWebhookEvent>> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);
}
```

**Phase 1 MVP - Priority Methods** (Implement First):
1. `CreateCheckoutSessionAsync()` - For Event Organizer subscription ($10/month)
2. `CreateCustomerPortalSessionAsync()` - For subscription management
3. `ProcessWebhookAsync()` - For subscription lifecycle events
4. `CreateOrUpdateCustomerAsync()` - Sync user to Stripe

**Phase 2 Methods** (Defer):
- Enterprise subscriptions
- Usage-based charging
- Partner payouts

**Extended Interface for MVP**:
```csharp
public interface IStripePaymentService
{
    // Phase 1 MVP - Checkout
    Task<Result<string>> CreateCheckoutSessionAsync(
        Guid userId,
        UserRole targetRole,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    // Phase 1 MVP - Customer Portal
    Task<Result<string>> CreateCustomerPortalSessionAsync(
        Guid userId,
        string returnUrl,
        CancellationToken cancellationToken = default);

    // Phase 1 MVP - Customer Sync
    Task<Result<string>> CreateOrUpdateCustomerAsync(
        Guid userId,
        string email,
        string name,
        CancellationToken cancellationToken = default);

    // Phase 1 MVP - Webhooks
    Task<Result> ProcessWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);

    // Existing (Phase 2)
    Task<Result> CreateSubscriptionAsync(CreateStripeSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task<Result> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    // ... (other Phase 2 methods)
}
```

---

### ADR-005: Error Handling Strategy

**Decision**: Use **Result<T> Pattern** for all service methods + Custom StripeException mapping

**Pattern**:
```csharp
public async Task<Result<string>> CreateCustomerAsync(Guid userId, string email, string name)
{
    try
    {
        // Stripe API call
        var customerOptions = new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Metadata = new Dictionary<string, string>
            {
                { "UserId", userId.ToString() }
            }
        };

        var customer = await _stripeClient.Customers.CreateAsync(customerOptions);

        // Save to database
        var stripeCustomer = StripeCustomer.Create(
            userId,
            customer.Id,
            email,
            name,
            DateTime.UtcNow);

        await _stripeCustomerRepository.AddAsync(stripeCustomer);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Stripe customer created: {CustomerId} for user {UserId}", customer.Id, userId);
        return Result<string>.Success(customer.Id);
    }
    catch (StripeException ex)
    {
        _logger.LogError(ex, "Stripe API error creating customer for user {UserId}", userId);
        return Result<string>.Failure($"Payment service error: {ex.StripeError?.Message ?? ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error creating Stripe customer for user {UserId}", userId);
        return Result<string>.Failure("An unexpected error occurred while creating customer");
    }
}
```

**Exception Mapping**:
- `StripeException` → Result.Failure (user-friendly message)
- `CardException` → Result.Failure (payment method error)
- `RateLimitException` → Result.Failure (retry later)
- `ApiConnectionException` → Result.Failure (network error)
- Generic `Exception` → Result.Failure (unexpected error)

**Retry Policy** (for transient failures):
```csharp
// Use Polly for retry logic (add package: Polly)
private readonly IAsyncPolicy<Result> _retryPolicy = Policy<Result>
    .Handle<ApiConnectionException>()
    .Or<RateLimitException>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

---

### ADR-006: Webhook Security Implementation

**Decision**: Multi-layered webhook security approach

**Security Layers**:

1. **Signature Validation** (CRITICAL):
```csharp
public async Task<Result> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
{
    try
    {
        // 1. Validate webhook signature
        var stripeEvent = EventUtility.ConstructEvent(
            payload,
            signature,
            _webhookSecret,
            throwOnApiVersionMismatch: false);

        // 2. Check idempotency (prevent duplicate processing)
        var existingEvent = await _webhookRepository.GetByEventIdAsync(stripeEvent.Id, cancellationToken);
        if (existingEvent != null)
        {
            _logger.LogInformation("Webhook event {EventId} already processed", stripeEvent.Id);
            return Result.Success();
        }

        // 3. Record webhook attempt
        var webhookEvent = StripeWebhookEvent.Create(stripeEvent.Id, stripeEvent.Type);
        await _webhookRepository.AddAsync(webhookEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Process event (transaction boundary)
        var result = stripeEvent.Type switch
        {
            "customer.subscription.created" => await HandleSubscriptionCreatedAsync(stripeEvent, cancellationToken),
            "customer.subscription.updated" => await HandleSubscriptionUpdatedAsync(stripeEvent, cancellationToken),
            "customer.subscription.deleted" => await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken),
            "invoice.payment_succeeded" => await HandlePaymentSucceededAsync(stripeEvent, cancellationToken),
            "invoice.payment_failed" => await HandlePaymentFailedAsync(stripeEvent, cancellationToken),
            _ => Result.Success() // Ignore unhandled event types
        };

        // 5. Mark as processed
        if (result.IsSuccess)
        {
            webhookEvent.MarkAsProcessed();
        }
        else
        {
            webhookEvent.RecordAttempt(result.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
    catch (StripeException ex)
    {
        _logger.LogError(ex, "Invalid webhook signature");
        return Result.Failure("Invalid webhook signature");
    }
}
```

2. **Idempotency Check** (StripeWebhookEvent table):
   - Prevents duplicate processing if Stripe retries
   - Records attempt count and errors

3. **Transaction Boundaries**:
   - Use database transactions for webhook processing
   - Rollback on failure to maintain consistency

4. **Retry Handling**:
   - Track attempt count in `StripeWebhookEvent.AttemptCount`
   - Implement exponential backoff for failed webhooks
   - Alert on repeated failures (>3 attempts)

---

## Service Layer Design

### File Structure

```
LankaConnect.Infrastructure/
└── Payments/
    ├── Services/
    │   └── StripePaymentService.cs
    ├── Repositories/
    │   ├── StripeCustomerRepository.cs
    │   └── StripeWebhookEventRepository.cs
    └── Options/
        └── StripeOptions.cs (configuration)
```

### StripePaymentService Class Outline

```csharp
namespace LankaConnect.Infrastructure.Payments.Services;

public class StripePaymentService : IStripePaymentService
{
    private readonly IStripeClient _stripeClient;
    private readonly IStripeCustomerRepository _customerRepository;
    private readonly IStripeWebhookEventRepository _webhookRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly StripeOptions _options;

    public StripePaymentService(
        IStripeClient stripeClient,
        IStripeCustomerRepository customerRepository,
        IStripeWebhookEventRepository webhookRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<StripePaymentService> logger,
        IOptions<StripeOptions> options)
    {
        _stripeClient = stripeClient;
        _customerRepository = customerRepository;
        _webhookRepository = webhookRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _options = options.Value;
    }

    // Phase 1 MVP Methods
    public async Task<Result<string>> CreateCheckoutSessionAsync(...) { }
    public async Task<Result<string>> CreateCustomerPortalSessionAsync(...) { }
    public async Task<Result<string>> CreateOrUpdateCustomerAsync(...) { }
    public async Task<Result> ProcessWebhookAsync(...) { }

    // Private helper methods
    private async Task<Result> HandleSubscriptionCreatedAsync(...) { }
    private async Task<Result> HandleSubscriptionUpdatedAsync(...) { }
    private async Task<Result> HandleSubscriptionDeletedAsync(...) { }
    private async Task<Result> HandlePaymentSucceededAsync(...) { }
    private async Task<Result> HandlePaymentFailedAsync(...) { }

    // Price ID mapping (hardcoded for MVP)
    private string GetPriceIdForRole(UserRole role)
    {
        return role switch
        {
            UserRole.EventOrganizer => _options.EventOrganizerPriceId,
            UserRole.BusinessOwner => _options.BusinessOwnerPriceId,
            UserRole.EventOrganizerAndBusinessOwner => _options.CombinedPriceId,
            _ => throw new ArgumentException($"No subscription for role {role}")
        };
    }
}
```

---

## Repository Pattern Implementation

### IStripeCustomerRepository Interface

**Location**: `LankaConnect.Application/Common/Interfaces/IStripeCustomerRepository.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.Payments.Entities;

namespace LankaConnect.Application.Common.Interfaces;

public interface IStripeCustomerRepository : IRepository<StripeCustomer>
{
    Task<StripeCustomer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<StripeCustomer?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

### StripeCustomerRepository Implementation

**Location**: `LankaConnect.Infrastructure/Payments/Repositories/StripeCustomerRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Infrastructure.Payments.Entities;

namespace LankaConnect.Infrastructure.Payments.Repositories;

public class StripeCustomerRepository : Repository<StripeCustomer>, IStripeCustomerRepository
{
    public StripeCustomerRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StripeCustomer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.UserId == userId, cancellationToken);
    }

    public async Task<StripeCustomer?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.StripeCustomerId == stripeCustomerId, cancellationToken);
    }

    public async Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(sc => sc.UserId == userId, cancellationToken);
    }
}
```

### IStripeWebhookEventRepository Interface

**Location**: `LankaConnect.Application/Common/Interfaces/IStripeWebhookEventRepository.cs`

```csharp
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.Payments.Entities;

namespace LankaConnect.Application.Common.Interfaces;

public interface IStripeWebhookEventRepository : IRepository<StripeWebhookEvent>
{
    Task<StripeWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default);
    Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StripeWebhookEvent>> GetUnprocessedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default);
}
```

### StripeWebhookEventRepository Implementation

**Location**: `LankaConnect.Infrastructure/Payments/Repositories/StripeWebhookEventRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.Infrastructure.Payments.Entities;

namespace LankaConnect.Infrastructure.Payments.Repositories;

public class StripeWebhookEventRepository : Repository<StripeWebhookEvent>, IStripeWebhookEventRepository
{
    public StripeWebhookEventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StripeWebhookEvent?> GetByEventIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(e => e.EventId == eventId && e.Processed, cancellationToken);
    }

    public async Task<IReadOnlyList<StripeWebhookEvent>> GetUnprocessedEventsAsync(int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => !e.Processed && e.AttemptCount < maxAttempts)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
```

---

## Dependency Injection Setup

### Configuration Classes

**StripeOptions.cs** (`LankaConnect.Infrastructure/Payments/Options/StripeOptions.cs`):

```csharp
namespace LankaConnect.Infrastructure.Payments.Options;

public class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    // Price IDs for MVP subscriptions
    public string EventOrganizerPriceId { get; set; } = string.Empty;
    public string BusinessOwnerPriceId { get; set; } = string.Empty;
    public string CombinedPriceId { get; set; } = string.Empty;

    // Trial configuration
    public int TrialDays { get; set; } = 180; // 6 months (first 6 months free)
}
```

### DI Registration Extension Method

**Location**: Update `LankaConnect.Infrastructure/DependencyInjection.cs` (or create new file)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Payments.Services;
using LankaConnect.Infrastructure.Payments.Repositories;
using LankaConnect.Infrastructure.Payments.Options;

namespace LankaConnect.Infrastructure;

public static partial class DependencyInjection
{
    public static IServiceCollection AddStripePaymentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure Stripe options
        services.Configure<StripeOptions>(
            configuration.GetSection(StripeOptions.SectionName));

        // Register Stripe client (singleton - thread-safe)
        services.AddSingleton<IStripeClient>(sp =>
        {
            var options = configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
            if (string.IsNullOrWhiteSpace(options?.SecretKey))
                throw new InvalidOperationException("Stripe:SecretKey configuration is missing");

            return new StripeClient(options.SecretKey);
        });

        // Register repositories (scoped - per request)
        services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
        services.AddScoped<IStripeWebhookEventRepository, StripeWebhookEventRepository>();

        // Register payment service (scoped - per request)
        services.AddScoped<IStripePaymentService, StripePaymentService>();

        return services;
    }
}
```

### Update Infrastructure Registration

**Location**: Update `LankaConnect.Infrastructure/DependencyInjection.cs` main method:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing registrations ...

    // Add Stripe payment services
    services.AddStripePaymentServices(configuration);

    return services;
}
```

### Configuration (appsettings.json)

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "EventOrganizerPriceId": "price_...",
    "BusinessOwnerPriceId": "price_...",
    "CombinedPriceId": "price_...",
    "TrialDays": 180
  }
}
```

**SECURITY**: Use Azure Key Vault or environment variables for production secrets.

---

## TDD Strategy

### Unit Testing Approach

**Test Framework**: xUnit (already used in your codebase)
**Mocking**: Moq (already used in your codebase)

### Mock IStripeClient Pattern

Create a wrapper interface for better testability:

**IStripeApiClient.cs** (optional abstraction layer):

```csharp
public interface IStripeApiClient
{
    Task<Customer> CreateCustomerAsync(CustomerCreateOptions options, CancellationToken cancellationToken = default);
    Task<Subscription> CreateSubscriptionAsync(SubscriptionCreateOptions options, CancellationToken cancellationToken = default);
    Task<Session> CreateCheckoutSessionAsync(SessionCreateOptions options, CancellationToken cancellationToken = default);
    Task<BillingPortal.Session> CreatePortalSessionAsync(BillingPortal.SessionCreateOptions options, CancellationToken cancellationToken = default);
}
```

**Rationale**: IStripeClient is from Stripe.net SDK and cannot be easily mocked. Creating a wrapper makes testing easier.

### Test Structure

**Test Project**: `LankaConnect.Infrastructure.Tests/Payments/Services/StripePaymentServiceTests.cs`

```csharp
public class StripePaymentServiceTests
{
    private readonly Mock<IStripeClient> _stripeClientMock;
    private readonly Mock<IStripeCustomerRepository> _customerRepoMock;
    private readonly Mock<IStripeWebhookEventRepository> _webhookRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<StripePaymentService>> _loggerMock;
    private readonly StripeOptions _options;
    private readonly StripePaymentService _sut;

    public StripePaymentServiceTests()
    {
        _stripeClientMock = new Mock<IStripeClient>();
        _customerRepoMock = new Mock<IStripeCustomerRepository>();
        _webhookRepoMock = new Mock<IStripeWebhookEventRepository>();
        _userRepoMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<StripePaymentService>>();

        _options = new StripeOptions
        {
            SecretKey = "sk_test_fake",
            PublishableKey = "pk_test_fake",
            WebhookSecret = "whsec_test_fake",
            EventOrganizerPriceId = "price_test_eo",
            TrialDays = 180
        };

        _sut = new StripePaymentService(
            _stripeClientMock.Object,
            _customerRepoMock.Object,
            _webhookRepoMock.Object,
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object,
            Options.Create(_options));
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldReturnSuccess_WhenCustomerCreated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";

        var stripeCustomer = new Customer { Id = "cus_test123" };

        _stripeClientMock
            .Setup(x => x.Customers.CreateAsync(It.IsAny<CustomerCreateOptions>(), null, default))
            .ReturnsAsync(stripeCustomer);

        // Act
        var result = await _sut.CreateOrUpdateCustomerAsync(userId, email, name, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("cus_test123", result.Value);
        _customerRepoMock.Verify(x => x.AddAsync(It.IsAny<StripeCustomer>(), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldReturnFailure_WhenStripeThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _stripeClientMock
            .Setup(x => x.Customers.CreateAsync(It.IsAny<CustomerCreateOptions>(), null, default))
            .ThrowsAsync(new StripeException("API error"));

        // Act
        var result = await _sut.CreateOrUpdateCustomerAsync(userId, "test@example.com", "Test", default);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Payment service error", result.Error);
    }
}
```

### Integration Testing

**Test Stripe in Test Mode**:
- Use Stripe test API keys (sk_test_...)
- Use Stripe test webhook endpoint
- Use Stripe CLI for webhook testing locally

**Webhook Signature Testing**:
```bash
# Install Stripe CLI
stripe listen --forward-to https://localhost:7001/api/webhooks/stripe

# Trigger test events
stripe trigger customer.subscription.created
```

---

## Security Best Practices

### 1. Secret Management

**Development**:
```json
// appsettings.Development.json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

**Production**:
```csharp
// Use Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:Endpoint"]),
    new DefaultAzureCredential());
```

**Environment Variables** (Azure App Service):
```
Stripe__SecretKey=sk_live_...
Stripe__PublishableKey=pk_live_...
Stripe__WebhookSecret=whsec_...
```

### 2. Webhook Endpoint Security

**Controller Implementation**:

```csharp
[ApiController]
[Route("api/webhooks")]
[AllowAnonymous] // Webhooks don't use JWT auth
public class WebhooksController : ControllerBase
{
    private readonly IStripePaymentService _paymentService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IStripePaymentService paymentService,
        ILogger<WebhooksController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        try
        {
            // Read raw body (required for signature validation)
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            // Get signature header
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature))
            {
                _logger.LogWarning("Webhook request missing Stripe-Signature header");
                return BadRequest("Missing signature");
            }

            // Process webhook (signature validated inside service)
            var result = await _paymentService.ProcessWebhookAsync(payload, signature);

            if (result.IsFailure)
            {
                _logger.LogError("Webhook processing failed: {Error}", result.Error);
                return BadRequest(result.Error);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return StatusCode(500, "Internal server error");
        }
    }
}
```

### 3. PCI Compliance

**IMPORTANT**: Never store sensitive payment data:
- ❌ DO NOT store credit card numbers
- ❌ DO NOT store CVV codes
- ❌ DO NOT store full card details
- ✅ Only store Stripe customer/subscription IDs
- ✅ Use Stripe Checkout/Elements for payment collection

**Your Implementation**:
- Use `Stripe.Checkout.Session` for payment flow (PCI-compliant)
- Redirect users to Stripe-hosted checkout page
- Store only `StripeCustomerId` and `StripeSubscriptionId` in database

---

## Code Structure

### Phase 1 MVP Implementation Order

**Week 1: Foundation**
1. Create repository interfaces and implementations
2. Implement DI registration
3. Write unit tests for repositories
4. Create StripePaymentService skeleton

**Week 2: Core Features**
5. Implement `CreateOrUpdateCustomerAsync()`
6. Implement `CreateCheckoutSessionAsync()`
7. Implement `CreateCustomerPortalSessionAsync()`
8. Write unit tests for service methods

**Week 3: Webhooks**
9. Implement `ProcessWebhookAsync()` with signature validation
10. Implement webhook handlers (subscription.created, updated, deleted)
11. Create webhook controller
12. Test webhooks with Stripe CLI

**Week 4: Integration**
13. Integration testing with Stripe test mode
14. Update frontend integration
15. Documentation and deployment
16. Production webhook endpoint setup

---

## Next Steps

### Immediate Actions (Today)

1. **Review and Approve** this architectural guidance
2. **Create GitHub Issue** for Phase 6A.4 implementation
3. **Update PROGRESS_TRACKER.md** with detailed subtasks
4. **Install Stripe.net NuGet Package**:
   ```bash
   cd src/LankaConnect.Infrastructure
   dotnet add package Stripe.net
   ```

### Implementation Checklist

- [ ] Create repository interfaces in Application layer
- [ ] Implement repositories in Infrastructure layer
- [ ] Create StripeOptions configuration class
- [ ] Update DependencyInjection.cs with Stripe services
- [ ] Implement StripePaymentService (MVP methods only)
- [ ] Create unit tests for repositories
- [ ] Create unit tests for service
- [ ] Create WebhooksController
- [ ] Add Stripe configuration to appsettings
- [ ] Test with Stripe CLI locally
- [ ] Integration testing in Azure staging
- [ ] Update PHASE_6A4_STRIPE_PAYMENT_SUMMARY.md
- [ ] Update all PRIMARY tracking documents

---

## References

### Existing Code Patterns
- **Result Pattern**: `LankaConnect.Domain.Common.Result<T>`
- **Repository Pattern**: `LankaConnect.Infrastructure.Data.Repositories.Repository<T>`
- **Service Pattern**: `LankaConnect.Infrastructure.Security.Services.PasswordHashingService`
- **DI Extensions**: `LankaConnect.Infrastructure.DependencyInjection`

### Stripe Documentation
- [Stripe.net SDK](https://github.com/stripe/stripe-dotnet)
- [Checkout Sessions](https://stripe.com/docs/api/checkout/sessions)
- [Customer Portal](https://stripe.com/docs/billing/subscriptions/integrating-customer-portal)
- [Webhooks](https://stripe.com/docs/webhooks)
- [Testing](https://stripe.com/docs/testing)

### LankaConnect Documentation
- [PHASE_6A_MASTER_INDEX.md](./PHASE_6A_MASTER_INDEX.md)
- [CLAUDE.md](../CLAUDE.md) - Development guidelines
- [Master Requirements Specification.md](./Master%20Requirements%20Specification.md)

---

## Approval

**Architect**: [Pending Review]
**Tech Lead**: [Pending Review]
**Date**: 2025-11-24

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-11-24 | Claude (System Architect) | Initial architectural guidance document |

