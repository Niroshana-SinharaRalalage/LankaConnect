# LankaConnect - Testing Strategy & Implementation Guide
## Technical Architecture Document

**Version:** 1.0  
**Last Updated:** January 2025  
**Status:** Final  
**Owner:** Platform Architecture Team

---

## 1. Executive Summary

This document defines the comprehensive testing strategy for the LankaConnect platform, emphasizing Test-Driven Development (TDD) workflows, automated testing patterns, and quality assurance practices. It provides practical implementation guidelines for unit testing, integration testing, end-to-end testing, and performance testing.

### 1.1 Document Purpose
- Define TDD workflows and best practices
- Establish testing patterns for Clean Architecture
- Specify testing frameworks and tools
- Create automation strategies
- Design quality gates and metrics

### 1.2 Testing Philosophy
- **Test First:** Write tests before implementation
- **Fast Feedback:** Tests run in seconds, not minutes
- **Comprehensive Coverage:** Aim for >80% code coverage
- **Automated Everything:** No manual regression testing
- **Shift Left:** Catch issues early in development

---

## 2. Test-Driven Development (TDD) Workflow

### 2.1 TDD Cycle Implementation
```csharp
// TDD Workflow Example: Creating a Service Booking Feature

// Step 1: Write a failing test (RED)
[Fact]
public async Task CreateBooking_WithValidData_ShouldReturnSuccess()
{
    // Arrange
    var command = new CreateBookingCommand
    {
        ServiceId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        ScheduledDate = DateTime.UtcNow.AddDays(1),
        Duration = TimeSpan.FromHours(2),
        Notes = "Fix kitchen sink"
    };

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeSuccess();
    result.Value.BookingId.Should().NotBeEmpty();
    result.Value.Status.Should().Be(BookingStatus.Pending);
}

// Step 2: Write minimal code to pass (GREEN)
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<BookingCreatedDto>>
{
    private readonly IBookingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<BookingCreatedDto>> Handle(
        CreateBookingCommand request, 
        CancellationToken cancellationToken)
    {
        var booking = Booking.Create(
            request.ServiceId,
            request.CustomerId,
            request.ScheduledDate,
            request.Duration,
            request.Notes);

        await _repository.AddAsync(booking);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(new BookingCreatedDto
        {
            BookingId = booking.Id,
            Status = booking.Status
        });
    }
}

// Step 3: Refactor with confidence (REFACTOR)
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<BookingCreatedDto>>
{
    private readonly IBookingRepository _repository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBookingValidator _validator;
    private readonly IEventBus _eventBus;

    public async Task<Result<BookingCreatedDto>> Handle(
        CreateBookingCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate service exists and is available
        var service = await _serviceRepository.GetByIdAsync(request.ServiceId);
        if (service == null)
            return Result.Failure<BookingCreatedDto>("Service not found");

        // Validate booking constraints
        var validationResult = await _validator.ValidateBookingAsync(request);
        if (!validationResult.IsValid)
            return Result.Failure<BookingCreatedDto>(validationResult.Errors);

        // Create booking with domain logic
        var booking = Booking.Create(
            request.ServiceId,
            request.CustomerId,
            request.ScheduledDate,
            request.Duration,
            request.Notes);

        // Check for conflicts
        var hasConflict = await _repository.HasConflictAsync(
            booking.ServiceId, 
            booking.ScheduledDate, 
            booking.Duration);
            
        if (hasConflict)
            return Result.Failure<BookingCreatedDto>("Time slot not available");

        // Persist and publish events
        await _repository.AddAsync(booking);
        await _unitOfWork.CommitAsync(cancellationToken);
        
        await _eventBus.PublishAsync(
            new BookingCreatedEvent(booking.Id, booking.ServiceId, booking.CustomerId),
            cancellationToken);

        return Result.Success(new BookingCreatedDto
        {
            BookingId = booking.Id,
            Status = booking.Status,
            ConfirmationNumber = booking.ConfirmationNumber
        });
    }
}
```

### 2.2 TDD Best Practices
```csharp
// Good TDD practices configuration
public class TddGuidelines
{
    // 1. One assertion per test
    [Fact]
    public void Service_WhenCreated_ShouldHaveCorrectName()
    {
        var service = Service.Create("Plumbing Service", _categoryId, _providerId);
        service.Name.Should().Be("Plumbing Service");
    }

    // 2. Test naming convention: Method_Scenario_ExpectedBehavior
    [Fact]
    public void AddReview_WithValidRating_ShouldUpdateAverageRating()
    {
        // Implementation
    }

    // 3. Arrange-Act-Assert pattern
    [Fact]
    public async Task GetServices_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        await SeedTestData(25);
        var query = new GetServicesQuery { PageNumber = 2, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Items.Should().HaveCount(10);
        result.Value.PageNumber.Should().Be(2);
        result.Value.TotalCount.Should().Be(25);
    }

    // 4. Test data builders for complex objects
    public class ServiceBuilder
    {
        private string _name = "Test Service";
        private Guid _categoryId = Guid.NewGuid();
        private decimal _price = 100m;

        public ServiceBuilder WithName(string name) 
        {
            _name = name;
            return this;
        }

        public ServiceBuilder WithPrice(decimal price) 
        {
            _price = price;
            return this;
        }

        public Service Build() => Service.Create(_name, _categoryId, _price);
    }
}
```

---

## 3. Testing Framework Configuration

### 3.1 Test Project Structure
```
tests/
├── UnitTests/
│   ├── LankaConnect.Domain.Tests/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Services/
│   ├── LankaConnect.Application.Tests/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Validators/
│   └── LankaConnect.Infrastructure.Tests/
│       ├── Repositories/
│       └── Services/
├── IntegrationTests/
│   ├── LankaConnect.API.IntegrationTests/
│   │   ├── Controllers/
│   │   └── Fixtures/
│   └── LankaConnect.Infrastructure.IntegrationTests/
│       ├── Database/
│       └── External/
└── E2ETests/
    └── LankaConnect.E2E.Tests/
        ├── Scenarios/
        └── Pages/
```

### 3.2 Testing Libraries and Tools
```xml
<!-- Test project dependencies -->
<ItemGroup>
  <!-- Core testing frameworks -->
  <PackageReference Include="xunit" Version="2.6.6" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  
  <!-- Assertion libraries -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Shouldly" Version="4.2.1" />
  
  <!-- Mocking frameworks -->
  <PackageReference Include="Moq" Version="4.20.70" />
  <PackageReference Include="NSubstitute" Version="5.1.0" />
  
  <!-- Test data generation -->
  <PackageReference Include="Bogus" Version="35.4.0" />
  <PackageReference Include="AutoFixture" Version="4.18.1" />
  
  <!-- Integration testing -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.1" />
  <PackageReference Include="Testcontainers" Version="3.7.0" />
  <PackageReference Include="Respawn" Version="6.1.0" />
  
  <!-- Performance testing -->
  <PackageReference Include="NBomber" Version="5.2.1" />
  <PackageReference Include="BenchmarkDotNet" Version="0.13.12" />
  
  <!-- Code coverage -->
  <PackageReference Include="coverlet.collector" Version="6.0.0" />
  <PackageReference Include="ReportGenerator" Version="5.2.0" />
</ItemGroup>
```

---

## 4. Unit Testing Patterns

### 4.1 Domain Entity Testing
```csharp
public class ServiceTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateService()
    {
        // Arrange
        var name = "Professional Plumbing";
        var categoryId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var basePrice = 150m;

        // Act
        var service = Service.Create(name, categoryId, providerId, basePrice);

        // Assert
        service.Should().NotBeNull();
        service.Name.Should().Be(name);
        service.CategoryId.Should().Be(categoryId);
        service.ProviderId.Should().Be(providerId);
        service.BasePrice.Should().Be(basePrice);
        service.IsActive.Should().BeTrue();
        service.Rating.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowException(string name)
    {
        // Act & Assert
        var act = () => Service.Create(name, Guid.NewGuid(), Guid.NewGuid(), 100m);
        
        act.Should().Throw<DomainException>()
           .WithMessage("Service name is required");
    }

    [Fact]
    public void AddReview_WithValidReview_ShouldUpdateRating()
    {
        // Arrange
        var service = new ServiceBuilder().Build();
        var review1 = new Review(Guid.NewGuid(), service.Id, 5, "Excellent");
        var review2 = new Review(Guid.NewGuid(), service.Id, 4, "Good");

        // Act
        service.AddReview(review1);
        service.AddReview(review2);

        // Assert
        service.Rating.Should().Be(4.5m);
        service.ReviewCount.Should().Be(2);
    }
}
```

### 4.2 Application Layer Testing
```csharp
public class CreateServiceCommandHandlerTests
{
    private readonly Mock<IServiceRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly CreateServiceCommandHandler _handler;

    public CreateServiceCommandHandlerTests()
    {
        _repositoryMock = new Mock<IServiceRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventBusMock = new Mock<IEventBus>();
        
        _handler = new CreateServiceCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _eventBusMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateService()
    {
        // Arrange
        var command = new CreateServiceCommand
        {
            Name = "Expert Plumbing",
            CategoryId = Guid.NewGuid(),
            ProviderId = Guid.NewGuid(),
            BasePrice = 200m,
            Description = "Professional plumbing services"
        };

        _repositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        result.Value.ServiceId.Should().NotBeEmpty();

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Service>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(
            It.IsAny<ServiceCreatedEvent>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateServiceCommand
        {
            Name = "Existing Service",
            CategoryId = Guid.NewGuid(),
            ProviderId = Guid.NewGuid()
        };

        _repositoryMock
            .Setup(x => x.ExistsAsync(command.Name, command.ProviderId))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFailure();
        result.Error.Should().Contain("already exists");
        
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Service>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

### 4.3 Validator Testing
```csharp
public class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator;
    private readonly Mock<IServiceRepository> _serviceRepositoryMock;

    public CreateBookingCommandValidatorTests()
    {
        _serviceRepositoryMock = new Mock<IServiceRepository>();
        _validator = new CreateBookingCommandValidator(_serviceRepositoryMock.Object);
    }

    [Fact]
    public async Task Validate_WithValidData_ShouldPass()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ServiceId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1),
            Duration = TimeSpan.FromHours(2),
            Notes = "Please bring tools"
        };

        _serviceRepositoryMock
            .Setup(x => x.ExistsAsync(command.ServiceId))
            .ReturnsAsync(true);

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(InvalidDates))]
    public async Task Validate_WithInvalidDate_ShouldFail(DateTime scheduledDate, string expectedError)
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            ServiceId = Guid.NewGuid(),
            ScheduledDate = scheduledDate
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedError));
    }

    public static IEnumerable<object[]> InvalidDates =>
        new List<object[]>
        {
            new object[] { DateTime.UtcNow.AddHours(-1), "past" },
            new object[] { DateTime.UtcNow.AddDays(91), "90 days" },
            new object[] { DateTime.MinValue, "required" }
        };
}
```

---

## 5. Integration Testing

### 5.1 WebApplicationFactory Setup
```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(PostgreSqlContainer.GetConnectionString());
            });

            // Replace external services with test doubles
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, FakeEmailService>();

            services.RemoveAll<ISmsService>();
            services.AddSingleton<ISmsService, FakeSmsService>();

            // Add test authentication
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:Redis"] = RedisContainer.GetConnectionString(),
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net",
                ["Storage:ConnectionString"] = "UseDevelopmentStorage=true"
            });
        });
    }

    // Testcontainers for real service dependencies
    private static readonly PostgreSqlContainer PostgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("lankaconnect_test")
        .WithUsername("test")
        .WithPassword("test123")
        .Build();

    private static readonly RedisContainer RedisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await PostgreSqlContainer.StartAsync();
        await RedisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await PostgreSqlContainer.DisposeAsync();
        await RedisContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

### 5.2 API Integration Tests
```csharp
public class ServiceControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ServiceControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetServices_WithValidRequest_ShouldReturnPagedResults()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Test");

        // Act
        var response = await _client.GetAsync(
            "/api/services?category=plumbing&location=colombo&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<ServiceDto>>(content);
        
        result.Should().NotBeNull();
        result.Items.Should().NotBeNull();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task CreateService_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreateServiceCommand
        {
            Name = "Premium Plumbing Service",
            CategoryId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            Description = "Expert plumbing solutions",
            BasePrice = 250m
        };

        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Test");

        // Act
        var response = await _client.PostAsJsonAsync("/api/services", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ServiceCreatedDto>(content);
        
        result.Should().NotBeNull();
        result.ServiceId.Should().NotBeEmpty();
    }
}
```

### 5.3 Database Integration Tests
```csharp
public class ServiceRepositoryIntegrationTests : IAsyncLifetime
{
    private AppDbContext _context;
    private ServiceRepository _repository;
    private Respawner _respawner;

    public async Task InitializeAsync()
    {
        var container = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .Build();
            
        await container.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(container.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();

        _repository = new ServiceRepository(_context);

        // Setup Respawner for database cleanup
        _respawner = await Respawner.CreateAsync(
            container.GetConnectionString(),
            new RespawnerOptions
            {
                TablesToIgnore = new[] { "__EFMigrationsHistory" }
            });
    }

    [Fact]
    public async Task GetServicesByCategory_WithExistingData_ShouldReturnFilteredResults()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var services = GenerateServices(categoryId, 20);
        
        await _context.Services.AddRangeAsync(services);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetServicesByCategoryAsync(
            categoryId, "Colombo", 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(15); // Filtered by location
        result.Items.Should().BeInDescendingOrder(s => s.Rating);
    }

    public async Task DisposeAsync()
    {
        await _respawner.ResetAsync(_context.Database.GetConnectionString());
        await _context.DisposeAsync();
    }
}
```

---

## 6. End-to-End Testing

### 6.1 Playwright Configuration
```csharp
// E2E test setup with Playwright
public class E2ETestBase : IAsyncLifetime
{
    protected IPlaywright Playwright { get; private set; }
    protected IBrowser Browser { get; private set; }
    protected IBrowserContext Context { get; private set; }
    protected IPage Page { get; private set; }

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !Debugger.IsAttached
        });

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = "https://localhost:7001",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 }
        });

        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Page.CloseAsync();
        await Context.CloseAsync();
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}
```

### 6.2 E2E Test Scenarios
```csharp
public class BookingFlowE2ETests : E2ETestBase
{
    [Fact]
    public async Task CompleteBookingFlow_ShouldSucceed()
    {
        // Navigate to home page
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Search for a service
        await Page.FillAsync("[data-testid=search-input]", "plumbing");
        await Page.SelectOptionAsync("[data-testid=location-select]", "Colombo");
        await Page.ClickAsync("[data-testid=search-button]");

        // Wait for results
        await Page.WaitForSelectorAsync("[data-testid=service-card]");
        var serviceCards = await Page.QuerySelectorAllAsync("[data-testid=service-card]");
        serviceCards.Count.Should().BeGreaterThan(0);

        // Select first service
        await Page.ClickAsync("[data-testid=service-card]:first-child");
        await Page.WaitForURLAsync("**/services/*");

        // Check service details
        await Page.WaitForSelectorAsync("[data-testid=service-name]");
        var serviceName = await Page.TextContentAsync("[data-testid=service-name]");
        serviceName.Should().NotBeNullOrEmpty();

        // Click book now
        await Page.ClickAsync("[data-testid=book-now-button]");

        // Fill booking form
        await Page.FillAsync("[data-testid=booking-date]", DateTime.Now.AddDays(2).ToString("yyyy-MM-dd"));
        await Page.SelectOptionAsync("[data-testid=booking-time]", "10:00");
        await Page.FillAsync("[data-testid=booking-notes]", "Please fix the kitchen sink");

        // Submit booking
        await Page.ClickAsync("[data-testid=submit-booking]");

        // Verify confirmation
        await Page.WaitForSelectorAsync("[data-testid=booking-confirmation]");
        var confirmationNumber = await Page.TextContentAsync("[data-testid=confirmation-number]");
        confirmationNumber.Should().MatchRegex(@"BK-\d{6}");

        // Take screenshot for evidence
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = $"screenshots/booking-confirmation-{DateTime.Now:yyyyMMddHHmmss}.png"
        });
    }
}
```

---

## 7. Test Data Management

### 7.1 Test Data Builders
```csharp
public class TestDataGenerator
{
    private readonly Faker _faker = new Faker();

    public Service GenerateService(Guid? categoryId = null, Guid? providerId = null)
    {
        return new ServiceBuilder()
            .WithName(_faker.Company.CompanyName())
            .WithCategory(categoryId ?? Guid.NewGuid())
            .WithProvider(providerId ?? Guid.NewGuid())
            .WithPrice(_faker.Random.Decimal(100, 1000))
            .WithRating(_faker.Random.Decimal(3, 5))
            .Build();
    }

    public List<Service> GenerateServices(int count)
    {
        return Enumerable.Range(0, count)
            .Select(_ => GenerateService())
            .ToList();
    }

    public Customer GenerateCustomer()
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = _faker.Internet.Email(),
            PhoneNumber = _faker.Phone.PhoneNumber(),
            Address = new Address
            {
                Street = _faker.Address.StreetAddress(),
                City = _faker.Address.City(),
                PostalCode = _faker.Address.ZipCode()
            }
        };
    }
}

// Fluent builder pattern for complex test data
public class BookingBuilder
{
    private Guid _serviceId = Guid.NewGuid();
    private Guid _customerId = Guid.NewGuid();
    private DateTime _scheduledDate = DateTime.UtcNow.AddDays(1);
    private TimeSpan _duration = TimeSpan.FromHours(2);
    private BookingStatus _status = BookingStatus.Pending;

    public BookingBuilder ForService(Guid serviceId)
    {
        _serviceId = serviceId;
        return this;
    }

    public BookingBuilder ForCustomer(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public BookingBuilder ScheduledAt(DateTime date)
    {
        _scheduledDate = date;
        return this;
    }

    public BookingBuilder WithStatus(BookingStatus status)
    {
        _status = status;
        return this;
    }

    public Booking Build()
    {
        var booking = Booking.Create(_serviceId, _customerId, _scheduledDate, _duration);
        
        if (_status != BookingStatus.Pending)
        {
            // Use reflection or internal methods to set status for testing
            typeof(Booking).GetProperty("Status")
                ?.SetValue(booking, _status);
        }

        return booking;
    }
}
```

### 7.2 Database Seeding
```csharp
public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly TestDataGenerator _generator;

    public DatabaseSeeder(AppDbContext context)
    {
        _context = context;
        _generator = new TestDataGenerator();
    }

    public async Task SeedAsync()
    {
        await SeedCategoriesAsync();
        await SeedProvidersAsync();
        await SeedServicesAsync();
        await SeedCustomersAsync();
        await SeedBookingsAsync();
        await _context.SaveChangesAsync();
    }

    private async Task SeedCategoriesAsync()
    {
        var categories = new[]
        {
            new Category { Id = Guid.NewGuid(), Name = "Plumbing", Icon = "wrench" },
            new Category { Id = Guid.NewGuid(), Name = "Electrical", Icon = "bolt" },
            new Category { Id = Guid.NewGuid(), Name = "Cleaning", Icon = "broom" },
            new Category { Id = Guid.NewGuid(), Name = "Carpentry", Icon = "hammer" }
        };

        await _context.Categories.AddRangeAsync(categories);
    }

    private async Task SeedServicesAsync()
    {
        var providers = await _context.Providers.ToListAsync();
        var categories = await _context.Categories.ToListAsync();

        foreach (var provider in providers)
        {
            var services = Enumerable.Range(0, Random.Shared.Next(2, 5))
                .Select(_ => _generator.GenerateService(
                    categories[Random.Shared.Next(categories.Count)].Id,
                    provider.Id))
                .ToList();

            await _context.Services.AddRangeAsync(services);
        }
    }
}
```

---

## 8. Performance Testing

### 8.1 Load Testing with NBomber
```csharp
public class LoadTests
{
    [Fact]
    public void ServiceSearch_LoadTest()
    {
        var scenario = Scenario.Create("service_search_scenario", async context =>
        {
            var httpClient = new HttpClient();
            var locations = new[] { "Colombo", "Galle", "Kandy", "Jaffna" };
            var categories = new[] { "plumbing", "electrical", "cleaning" };

            var location = locations[Random.Shared.Next(locations.Length)];
            var category = categories[Random.Shared.Next(categories.Length)];

            var response = await httpClient.GetAsync(
                $"https://api.lankaconnect.lk/api/services?category={category}&location={location}");

            return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(2)),
            Simulation.RampPerSec(rate: 10, interval: TimeSpan.FromSeconds(10), during: TimeSpan.FromMinutes(1))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert performance requirements
        stats.AllOkCount.Should().BeGreaterThan(stats.AllFailCount);
        stats.ScenarioStats[0].Ok.Request.RPS.Should().BeGreaterThan(80);
        stats.ScenarioStats[0].Ok.Latency.Mean.Should().BeLessThan(500);
    }
}
```

### 8.2 Stress Testing
```csharp
public class StressTests
{
    [Fact]
    public async Task Database_UnderHeavyLoad_ShouldMaintainPerformance()
    {
        // Arrange
        var tasks = new List<Task<TimeSpan>>();
        var concurrentRequests = 1000;
        var repository = new ServiceRepository(_context);

        // Act
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(MeasureQueryTime(repository));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var averageTime = results.Average(t => t.TotalMilliseconds);
        var p95Time = results.OrderBy(t => t).Skip((int)(results.Length * 0.95)).First();

        averageTime.Should().BeLessThan(100); // Average under 100ms
        p95Time.TotalMilliseconds.Should().BeLessThan(200); // P95 under 200ms
    }

    private async Task<TimeSpan> MeasureQueryTime(IServiceRepository repository)
    {
        var stopwatch = Stopwatch.StartNew();
        await repository.GetServicesByCategoryAsync(Guid.NewGuid(), "Colombo");
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }
}
```

---

## 9. Test Automation & CI/CD

### 9.1 GitHub Actions Test Pipeline
```yaml
name: Test Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Run unit tests with coverage
      run: |
        dotnet test tests/UnitTests/**/*.csproj \
          --no-restore \
          --collect:"XPlat Code Coverage" \
          --results-directory ./coverage \
          -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
    
    - name: Generate coverage report
      run: |
        dotnet tool install -g dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:coverage/**/coverage.opencover.xml \
          -targetdir:coverage/report \
          -reporttypes:Html_Dark
    
    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage/**/coverage.opencover.xml
        flags: unittests
        fail_ci_if_error: true

  integration-tests:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_PASSWORD: test123
          POSTGRES_DB: lankaconnect_test
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432
      
      redis:
        image: redis:7-alpine
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 6379:6379
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Run integration tests
      env:
        ConnectionStrings__DefaultConnection: Host=localhost;Database=lankaconnect_test;Username=postgres;Password=test123
        ConnectionStrings__Redis: localhost:6379
      run: |
        dotnet test tests/IntegrationTests/**/*.csproj \
          --no-restore \
          --logger "trx;LogFileName=integration-tests.trx"
    
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: integration-test-results
        path: '**/integration-tests.trx'

  e2e-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Install Playwright
      run: pwsh tests/E2ETests/bin/Debug/net8.0/playwright.ps1 install
    
    - name: Run E2E tests
      run: dotnet test tests/E2ETests/**/*.csproj
    
    - name: Upload test videos
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: e2e-test-videos
        path: '**/videos/**'
```

### 9.2 Quality Gates
```yaml
# SonarQube quality gate configuration
sonar.projectKey=lankaconnect
sonar.organization=lankaconnect-org

# Quality gate thresholds
sonar.qualitygate.wait=true
sonar.coverage.exclusions=**/*Tests.cs,**/Migrations/**
sonar.cs.opencover.reportsPaths=coverage/**/coverage.opencover.xml

# Minimum thresholds
sonar.coverage.minimum=80
sonar.duplications.maximum=5
sonar.bugs.maximum=0
sonar.vulnerabilities.maximum=0
sonar.code_smells.maximum=10
```

---

## 10. Testing Best Practices Summary

### 10.1 TDD Workflow for Claude Code Agents
```markdown
## Daily TDD Workflow

1. **Start with a failing test**
   - Write test for the next feature
   - Run test to confirm it fails
   - Commit the failing test

2. **Write minimal code to pass**
   - Implement just enough to make test green
   - Don't add extra functionality
   - Run tests frequently

3. **Refactor with confidence**
   - Improve code structure
   - Extract methods/classes
   - Run tests after each change

4. **Commit frequently**
   - Commit after each test passes
   - Commit after successful refactoring
   - Push at end of each feature

## Testing Checklist
- [ ] Unit test coverage > 80%
- [ ] Integration tests for all API endpoints
- [ ] E2E tests for critical user flows
- [ ] Performance tests meet SLAs
- [ ] All tests run in CI/CD
- [ ] No ignored or skipped tests
- [ ] Test data is isolated
- [ ] Tests run in parallel
- [ ] Mock external dependencies
- [ ] Use test containers for databases
```

### 10.2 Common Testing Patterns
```csharp
// Pattern 1: Object Mother for complex test data
public class ObjectMother
{
    public static Service SimpleService() => 
        new ServiceBuilder().Build();
    
    public static Service PremiumService() => 
        new ServiceBuilder()
            .WithPrice(500m)
            .WithRating(4.8m)
            .WithReviewCount(150)
            .Build();
    
    public static Booking ConfirmedBooking() =>
        new BookingBuilder()
            .WithStatus(BookingStatus.Confirmed)
            .ScheduledAt(DateTime.UtcNow.AddDays(3))
            .Build();
}

// Pattern 2: Custom assertions for domain objects
public static class CustomAssertions
{
    public static void ShouldBeValidService(this Service service)
    {
        service.Should().NotBeNull();
        service.Id.Should().NotBeEmpty();
        service.Name.Should().NotBeNullOrWhiteSpace();
        service.BasePrice.Should().BePositive();
        service.IsActive.Should().BeTrue();
    }
    
    public static void ShouldBeInPendingState(this Booking booking)
    {
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.ConfirmedAt.Should().BeNull();
        booking.CompletedAt.Should().BeNull();
    }
}

// Pattern 3: Test base class for common setup
public abstract class TestBase : IDisposable
{
    protected readonly IServiceCollection Services;
    protected readonly ServiceProvider Provider;
    protected readonly IMapper Mapper;
    
    protected TestBase()
    {
        Services = new ServiceCollection();
        ConfigureServices(Services);
        Provider = Services.BuildServiceProvider();
        Mapper = Provider.GetRequiredService<IMapper>();
    }
    
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddAutoMapper(typeof(ServiceProfile).Assembly);
        services.AddLogging(builder => builder.AddDebug());
    }
    
    public void Dispose()
    {
        Provider?.Dispose();
    }
}
```

---

## 11. Conclusion

This Testing Strategy & Implementation Guide provides a comprehensive foundation for maintaining high-quality code throughout the LankaConnect platform development. By following TDD principles, leveraging appropriate testing frameworks, and maintaining comprehensive test coverage, the development team can confidently build and refactor the system while ensuring reliability and performance.

The combination of unit tests, integration tests, E2E tests, and performance tests creates a robust safety net that enables rapid development while maintaining system stability. The automated test pipeline ensures that quality standards are consistently met across all code changes.