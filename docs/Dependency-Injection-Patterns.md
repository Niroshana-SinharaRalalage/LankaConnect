# Dependency Injection Patterns for Infrastructure Layer

## Overview

This document defines the dependency injection patterns and service registration strategies for implementing missing Infrastructure layer dependencies in the LankaConnect application, following Clean Architecture principles and established patterns.

## Current Infrastructure DI Analysis

### Existing Registration Patterns
From `Infrastructure.DependencyInjection.cs`, the established patterns are:

```csharp
✅ Pattern Analysis:
1. DbContext with enhanced configuration and connection pooling
2. Redis caching with detailed configuration options
3. Scoped repositories registered individually 
4. Singleton services for external clients (BlobServiceClient)
5. Configuration binding with Options pattern
6. Environment-specific configurations
```

## Service Lifetime Guidelines

### Scoped Services (Per Request)
- **Repositories**: Data access with DbContext dependency
- **Application Services**: Business logic orchestration  
- **Unit of Work**: Transaction boundary management

### Transient Services  
- **Command/Query Handlers**: MediatR handlers (registered automatically)
- **Validators**: FluentValidation validators
- **Lightweight Services**: Services without expensive initialization

### Singleton Services
- **External Clients**: HTTP clients, storage clients
- **Configuration Objects**: Options, settings
- **Expensive Initialization**: Services with heavy startup cost

## Missing Service Registration Solutions

### 1. IMemoryCache Registration (Priority 1)

#### Current Issue
```csharp
// RazorEmailTemplateService requires IMemoryCache
public RazorEmailTemplateService(
    IMemoryCache cache,  // ← Not registered
    IOptions<EmailSettings> settings)
```

#### Solution: Add to Infrastructure DependencyInjection
```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing registrations ...

    // Add Memory Cache with template-specific configuration
    services.AddMemoryCache(options =>
    {
        // Configure cache limits for email templates
        options.SizeLimit = 100;                    // Max 100 cached templates
        options.CompactionPercentage = 0.25;        // Remove 25% when limit reached  
        options.ExpirationScanFrequency = TimeSpan.FromMinutes(5); // Cleanup frequency
    });

    // ... rest of registrations ...
}
```

#### Configuration Options
```csharp
// Allow cache configuration via appsettings.json
services.AddMemoryCache(options =>
{
    var cacheSettings = configuration.GetSection("MemoryCache");
    options.SizeLimit = cacheSettings.GetValue<int?>("SizeLimit") ?? 100;
    options.CompactionPercentage = cacheSettings.GetValue<double>("CompactionPercentage", 0.25);
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(
        cacheSettings.GetValue<int>("ExpirationScanFrequencyMinutes", 5));
});
```

### 2. IUserEmailPreferencesRepository Registration (Priority 2)

#### Repository Implementation Registration
```csharp
// Add to Infrastructure.DependencyInjection.cs after existing repositories
services.AddScoped<IUserEmailPreferencesRepository, UserEmailPreferencesRepository>();
```

#### Repository Implementation Pattern
```csharp
public class UserEmailPreferencesRepository : Repository<UserEmailPreferences>, IUserEmailPreferencesRepository
{
    // Constructor follows existing Repository pattern
    public UserEmailPreferencesRepository(AppDbContext context) : base(context)
    {
    }

    // Interface implementation using base Repository functionality
    // All methods return Result<T> following established pattern
}
```

### 3. IEmailService Registration (Priority 2)

#### Service Implementation Registration
```csharp
// Add to Infrastructure.DependencyInjection.cs after existing email services
services.AddScoped<IEmailService, EmailService>();
```

#### Implementation Pattern with Dependencies
```csharp
public class EmailService : IEmailService
{
    private readonly ISimpleEmailService _simpleEmailService;      // Already registered
    private readonly IEmailTemplateService _templateService;      // Already registered
    private readonly IEmailMessageRepository _repository;         // Already registered
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        ISimpleEmailService simpleEmailService,
        IEmailTemplateService templateService,
        IEmailMessageRepository repository,
        ILogger<EmailService> logger)
    {
        _simpleEmailService = simpleEmailService ?? throw new ArgumentNullException(nameof(simpleEmailService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Interface implementation following Result<T> pattern
}
```

### 4. IEmailStatusRepository Resolution (Priority 3)

#### Analysis: Separate vs Consolidated Repository

**Current Situation**:
- `IEmailStatusRepository` provides query-focused operations
- `IEmailMessageRepository` provides full CRUD operations
- Both work with the same `EmailMessage` entity

#### Recommended Solution: Interface Segregation with Single Implementation
```csharp
// EmailMessageRepository implements both interfaces
public class EmailMessageRepository : Repository<EmailMessage>, 
    IEmailMessageRepository, IEmailStatusRepository
{
    // Existing IEmailMessageRepository implementation
    // + IEmailStatusRepository implementation
}

// Register single implementation for both interfaces
services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
services.AddScoped<IEmailStatusRepository>(provider => 
    provider.GetRequiredService<IEmailMessageRepository>());
```

#### Alternative: Decorator Pattern for Specialized Queries
```csharp
// If query optimization is needed
public class EmailStatusQueryRepository : IEmailStatusRepository
{
    private readonly IEmailMessageRepository _baseRepository;
    
    public EmailStatusQueryRepository(IEmailMessageRepository baseRepository)
    {
        _baseRepository = baseRepository;
    }
    
    // Optimized query implementations
}

// Registration
services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
services.AddScoped<IEmailStatusRepository, EmailStatusQueryRepository>();
```

## Complete Registration Pattern

### Updated Infrastructure.DependencyInjection.cs
```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // ... existing DbContext and cache configurations ...

    // ✅ PRIORITY 1: Add Memory Cache
    services.AddMemoryCache(options =>
    {
        var cacheSettings = configuration.GetSection("MemoryCache");
        options.SizeLimit = cacheSettings.GetValue<int?>("SizeLimit") ?? 100;
        options.CompactionPercentage = cacheSettings.GetValue<double>("CompactionPercentage", 0.25);
        options.ExpirationScanFrequency = TimeSpan.FromMinutes(
            cacheSettings.GetValue<int>("ExpirationScanFrequencyMinutes", 5));
    });

    // ... existing repository registrations ...
    
    // ✅ PRIORITY 2: Add Communications Repositories
    services.AddScoped<IEmailMessageRepository, EmailMessageRepository>();
    services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
    services.AddScoped<IUserEmailPreferencesRepository, UserEmailPreferencesRepository>();
    
    // ✅ PRIORITY 3: Email Status Repository (consolidated approach)
    services.AddScoped<IEmailStatusRepository>(provider => 
        provider.GetRequiredService<IEmailMessageRepository>());

    // ... existing service registrations ...

    // ✅ PRIORITY 2: Add Email Services  
    services.AddScoped<ISimpleEmailService, SimpleEmailService>();        // Already exists
    services.AddScoped<IEmailTemplateService, RazorEmailTemplateService>(); // Already exists
    services.AddScoped<IEmailService, EmailService>();                     // New implementation

    // ... rest of existing registrations ...
    
    return services;
}
```

## Configuration Management Patterns

### 1. Options Pattern for Service Configuration
```csharp
// Email service configuration
public class EmailServiceOptions
{
    public const string SectionName = "EmailService";
    
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    public int BulkEmailBatchSize { get; set; } = 50;
    public bool EnableDetailedLogging { get; set; } = false;
}

// Registration
services.Configure<EmailServiceOptions>(configuration.GetSection(EmailServiceOptions.SectionName));

// Usage in EmailService
public class EmailService : IEmailService
{
    private readonly EmailServiceOptions _options;
    
    public EmailService(IOptions<EmailServiceOptions> options)
    {
        _options = options.Value;
    }
}
```

### 2. Environment-Specific Configuration
```csharp
// Different cache configurations per environment
services.AddMemoryCache(options =>
{
    var isDevelopment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
    var isProduction = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Production";
    
    if (isDevelopment)
    {
        options.SizeLimit = 50;           // Smaller cache for dev
        options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
    }
    else if (isProduction)  
    {
        options.SizeLimit = 500;          // Larger cache for prod
        options.ExpirationScanFrequency = TimeSpan.FromMinutes(10);
    }
});
```

## Service Validation and Health Checks

### 1. Service Resolution Validation
```csharp
// Add to Program.cs for startup validation
public static void Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();
    
    // Validate critical service registrations
    ValidateServiceRegistrations(host.Services);
    
    host.Run();
}

private static void ValidateServiceRegistrations(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var provider = scope.ServiceProvider;
    
    try
    {
        // Test critical service resolution
        provider.GetRequiredService<IEmailService>();
        provider.GetRequiredService<IUserEmailPreferencesRepository>();
        provider.GetRequiredService<IEmailStatusRepository>();
        provider.GetRequiredService<IMemoryCache>();
        
        Console.WriteLine("✅ All critical services registered successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Service registration validation failed: {ex.Message}");
        throw;
    }
}
```

### 2. Health Checks for External Dependencies
```csharp
// Add health checks for registered services
services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<EmailServiceHealthCheck>("email-service")
    .AddCheck<CacheHealthCheck>("memory-cache");

public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IEmailService _emailService;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test email service connectivity
            var result = await _emailService.ValidateTemplateAsync("test-template", cancellationToken);
            return result.IsSuccess 
                ? HealthCheckResult.Healthy() 
                : HealthCheckResult.Unhealthy(result.Error);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Email service check failed: {ex.Message}");
        }
    }
}
```

## Testing Service Registration

### 1. Unit Tests for DI Configuration
```csharp
[Test]
public void AddInfrastructure_RegistersAllRequiredServices()
{
    // Arrange
    var services = new ServiceCollection();
    var configuration = CreateTestConfiguration();
    
    // Act
    services.AddInfrastructure(configuration);
    var provider = services.BuildServiceProvider();
    
    // Assert
    provider.GetService<IEmailService>().Should().NotBeNull();
    provider.GetService<IUserEmailPreferencesRepository>().Should().NotBeNull();
    provider.GetService<IEmailStatusRepository>().Should().NotBeNull();
    provider.GetService<IMemoryCache>().Should().NotBeNull();
}
```

### 2. Integration Tests for Service Resolution
```csharp
[Test]
public async Task EmailService_CanBeResolvedAndUsed_InIntegrationTest()
{
    // Arrange
    using var factory = new WebApplicationFactory<Program>();
    using var scope = factory.Services.CreateScope();
    
    // Act
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var result = await emailService.ValidateTemplateAsync("welcome", CancellationToken.None);
    
    // Assert
    result.Should().NotBeNull();
    // Additional assertions based on expected behavior
}
```

## Performance Considerations

### 1. Service Lifetime Impact
```csharp
// Prefer scoped for stateful services with DbContext
services.AddScoped<IEmailService, EmailService>();      // ✅ Correct

// Avoid singleton for services with scoped dependencies  
services.AddSingleton<IEmailService, EmailService>();   // ❌ Will fail - DbContext is scoped
```

### 2. Lazy Initialization for Expensive Services
```csharp
// For services with expensive initialization
services.AddScoped<IEmailService>(provider => new Lazy<EmailService>(() => 
    new EmailService(
        provider.GetRequiredService<ISimpleEmailService>(),
        provider.GetRequiredService<IEmailTemplateService>(),
        provider.GetRequiredService<IEmailMessageRepository>(),
        provider.GetRequiredService<ILogger<EmailService>>()
    )));
```

### 3. Connection Pooling and Resource Management
```csharp
// Configure connection pooling for DbContext (already implemented)
services.AddDbContext<AppDbContext>(options => { /* ... */ }, ServiceLifetime.Scoped);

// Configure HTTP client pooling if needed
services.AddHttpClient<IExternalEmailService, ExternalEmailService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "LankaConnect/1.0");
});
```

## Validation Checklist

- [ ] All missing interfaces have concrete implementations registered
- [ ] Service lifetimes are appropriate for their dependencies
- [ ] Configuration objects are properly bound using Options pattern
- [ ] Environment-specific configurations are handled
- [ ] Service resolution is validated at startup
- [ ] Health checks are implemented for external dependencies
- [ ] Unit tests verify service registration
- [ ] Integration tests validate end-to-end service resolution
- [ ] Performance impact is measured and acceptable

This dependency injection pattern ensures all Infrastructure layer dependencies are properly registered while maintaining Clean Architecture principles and established code quality standards.