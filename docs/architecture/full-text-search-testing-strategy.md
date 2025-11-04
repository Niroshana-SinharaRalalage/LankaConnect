# Testing Strategy: PostgreSQL Full-Text Search

## Testing Pyramid

```
        /\
       /  \      E2E Tests (5%)
      /----\     Integration Tests (25%)
     /------\    Unit Tests (70%)
    /--------\
```

## 1. Unit Tests (Application Layer)

### Query Handler Tests

```csharp
// Tests/Application/Events/Queries/SearchEventsQueryHandlerTests.cs
public class SearchEventsQueryHandlerTests
{
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<SearchEventsQueryHandler>> _mockLogger;
    private readonly SearchEventsQueryHandler _handler;

    public SearchEventsQueryHandlerTests()
    {
        _mockRepository = new Mock<IEventRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<SearchEventsQueryHandler>>();
        _handler = new SearchEventsQueryHandler(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedResults()
    {
        // Arrange
        var query = new SearchEventsQuery("cricket", Page: 1, PageSize: 20);
        var specification = new EventSearchSpecification("cricket");

        var mockEvents = new List<Event>
        {
            Event.Create("Cricket Match", "Description", "Sports", DateTime.UtcNow, DateTime.UtcNow.AddHours(3), "Stadium", 100),
            Event.Create("Cricket Tournament", "Description", "Sports", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), "Ground", 50)
        };

        _mockRepository
            .Setup(r => r.CountSearchAsync(It.IsAny<EventSearchSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _mockRepository
            .Setup(r => r.SearchAsync(It.IsAny<EventSearchSpecification>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEvents);

        var mockDtos = mockEvents.Select(e => new EventSearchResultDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description
        }).ToList();

        _mockMapper
            .Setup(m => m.Map<IReadOnlyList<EventSearchResultDto>>(mockEvents))
            .Returns(mockDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        var query = new SearchEventsQuery("nonexistent", Page: 1, PageSize: 20);

        _mockRepository
            .Setup(r => r.CountSearchAsync(It.IsAny<EventSearchSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Theory]
    [InlineData("  cricket   tournament  ", "cricket tournament")] // Excessive whitespace
    [InlineData("cricket\t\ttournament", "cricket tournament")] // Tabs
    [InlineData("  cricket  ", "cricket")] // Leading/trailing spaces
    public async Task Handle_SanitizesSearchTerm(string input, string expectedSanitized)
    {
        // Arrange
        var query = new SearchEventsQuery(input, Page: 1, PageSize: 20);

        _mockRepository
            .Setup(r => r.CountSearchAsync(
                It.Is<EventSearchSpecification>(s => s.SearchTerm == expectedSanitized),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .Verifiable();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify();
    }

    [Fact]
    public async Task Handle_WithFilters_PassesCorrectSpecification()
    {
        // Arrange
        var query = new SearchEventsQuery(
            "cricket",
            Page: 1,
            PageSize: 20,
            Category: "Sports",
            IsFreeOnly: true,
            StartDateFrom: new DateTime(2025, 11, 1));

        _mockRepository
            .Setup(r => r.CountSearchAsync(
                It.Is<EventSearchSpecification>(s =>
                    s.SearchTerm == "cricket" &&
                    s.Category == "Sports" &&
                    s.IsFreeOnly == true &&
                    s.StartDateFrom == new DateTime(2025, 11, 1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .Verifiable();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify();
    }

    [Fact]
    public async Task Handle_LogsSearchInformation()
    {
        // Arrange
        var query = new SearchEventsQuery("cricket", Page: 1, PageSize: 20);

        _mockRepository
            .Setup(r => r.CountSearchAsync(It.IsAny<EventSearchSpecification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Searching events")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

### Validator Tests

```csharp
// Tests/Application/Events/Queries/SearchEventsQueryValidatorTests.cs
public class SearchEventsQueryValidatorTests
{
    private readonly SearchEventsQueryValidator _validator;

    public SearchEventsQueryValidatorTests()
    {
        _validator = new SearchEventsQueryValidator();
    }

    [Theory]
    [InlineData("cricket")]
    [InlineData("cricket tournament")]
    [InlineData("cricket-tournament")]
    [InlineData("cricket's match")]
    public async Task Validate_ValidSearchTerm_Passes(string searchTerm)
    {
        // Arrange
        var query = new SearchEventsQuery(searchTerm);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Validate_EmptySearchTerm_Fails(string searchTerm)
    {
        // Arrange
        var query = new SearchEventsQuery(searchTerm);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SearchTerm");
    }

    [Fact]
    public async Task Validate_SearchTermTooLong_Fails()
    {
        // Arrange
        var searchTerm = new string('a', 501);
        var query = new SearchEventsQuery(searchTerm);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("500 characters"));
    }

    [Theory]
    [InlineData("cricket; DROP TABLE events;--")]
    [InlineData("cricket<script>alert('xss')</script>")]
    [InlineData("cricket{test}")]
    [InlineData("cricket[test]")]
    [InlineData("cricket|test")]
    public async Task Validate_InvalidCharacters_Fails(string searchTerm)
    {
        // Arrange
        var query = new SearchEventsQuery(searchTerm);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("invalid characters"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_InvalidPage_Fails(int page)
    {
        // Arrange
        var query = new SearchEventsQuery("cricket", Page: page);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task Validate_InvalidPageSize_Fails(int pageSize)
    {
        // Arrange
        var query = new SearchEventsQuery("cricket", PageSize: pageSize);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PageSize");
    }
}
```

## 2. Integration Tests (Infrastructure + Database)

### Repository Integration Tests (Testcontainers)

```csharp
// Tests/Infrastructure/Repositories/EventRepositoryIntegrationTests.cs
using Testcontainers.PostgreSql;

public class EventRepositoryIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres;
    private ApplicationDbContext _context;
    private EventRepository _repository;
    private ILogger<EventRepository> _logger;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("lankaconnect_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgres.StartAsync();

        // Setup DbContext
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.MigrateAsync();

        _logger = new Mock<ILogger<EventRepository>>().Object;
        _repository = new EventRepository(_context, _logger);

        // Seed test data
        await SeedTestData();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task SeedTestData()
    {
        var events = new[]
        {
            Event.Create("Cricket Championship", "Annual cricket tournament with teams", "Sports",
                DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(10).AddHours(6), "Stadium A", 100),

            Event.Create("Cricket Training Camp", "Youth cricket training program", "Sports",
                DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(5).AddHours(4), "Ground B", 50),

            Event.Create("Football Match", "International football game", "Sports",
                DateTime.UtcNow.AddDays(15), DateTime.UtcNow.AddDays(15).AddHours(2), "Stadium C", 150),

            Event.Create("Music Concert", "Live music performance", "Entertainment",
                DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(7).AddHours(3), "Theater D", 0), // Free event
        };

        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAsync_FindsCricketEvents_OrderedByRelevance()
    {
        // Arrange
        var specification = new EventSearchSpecification("cricket");

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Contains("cricket", e.Title.ToLower()));

        // First result should be more relevant (title + description match)
        Assert.Contains("Championship", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_MultiTermQuery_FindsMatchingEvents()
    {
        // Arrange
        var specification = new EventSearchSpecification("cricket tournament");

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert
        Assert.Single(results);
        Assert.Contains("Championship", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var specification = new EventSearchSpecification("match", category: "Sports");

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert
        Assert.Single(results);
        Assert.Equal("Football Match", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_WithIsFreeOnlyFilter_ReturnsOnlyFreeEvents()
    {
        // Arrange
        var specification = new EventSearchSpecification("concert", isFreeOnly: true);

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].TicketPrice == null || results[0].TicketPrice == 0);
    }

    [Fact]
    public async Task SearchAsync_WithStartDateFilter_ReturnsEventsAfterDate()
    {
        // Arrange
        var filterDate = DateTime.UtcNow.AddDays(8);
        var specification = new EventSearchSpecification("cricket", startDateFrom: filterDate);

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].StartDate >= filterDate);
    }

    [Fact]
    public async Task SearchAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var specification = new EventSearchSpecification("cricket");

        // Act
        var page1 = await _repository.SearchAsync(specification, page: 1, pageSize: 1);
        var page2 = await _repository.SearchAsync(specification, page: 2, pageSize: 1);

        // Assert
        Assert.Single(page1);
        Assert.Single(page2);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task CountSearchAsync_ReturnsCorrectCount()
    {
        // Arrange
        var specification = new EventSearchSpecification("cricket");

        // Act
        var count = await _repository.CountSearchAsync(specification);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task SearchAsync_NoResults_ReturnsEmptyList()
    {
        // Arrange
        var specification = new EventSearchSpecification("nonexistent");

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_SpecialCharacters_HandledSafely()
    {
        // Arrange
        var specification = new EventSearchSpecification("cricket & tournament");

        // Act
        var results = await _repository.SearchAsync(specification, page: 1, pageSize: 10);

        // Assert - Should not throw exception
        Assert.NotNull(results);
    }

    [Fact]
    public async Task SearchAsync_Performance_CompletesQuickly()
    {
        // Arrange
        var specification = new EventSearchSpecification("cricket");
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _repository.SearchAsync(specification, page: 1, pageSize: 10);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Query took {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## 3. API Integration Tests

```csharp
// Tests/API/Controllers/EventsControllerIntegrationTests.cs
public class EventsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EventsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SearchEvents_ValidQuery_ReturnsOkWithResults()
    {
        // Act
        var response = await _client.GetAsync("/api/events/search?q=cricket&page=1&pageSize=20");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PagedResult<EventSearchResultDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 0);
    }

    [Fact]
    public async Task SearchEvents_EmptyQuery_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/events/search?q=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchEvents_IncludesPaginationHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/events/search?q=cricket&page=1&pageSize=10");

        // Assert
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.True(response.Headers.Contains("X-Total-Pages"));
        Assert.True(response.Headers.Contains("X-Current-Page"));
        Assert.True(response.Headers.Contains("X-Page-Size"));
    }
}
```

## 4. Performance Tests

```csharp
// Tests/Performance/SearchPerformanceTests.cs
public class SearchPerformanceTests
{
    [Fact]
    public async Task SearchPerformance_Under50ms_For1000Events()
    {
        // Arrange: Database with 1000 events
        // Act: Execute search
        // Assert: Duration < 50ms
    }

    [Fact]
    public async Task SearchPerformance_Under100ms_ForComplexFilters()
    {
        // Test with all filters applied
    }
}
```

## Key Testing Decisions

1. **Testcontainers**: Real PostgreSQL for integration tests (not in-memory)
2. **Unit Tests**: Mock repository, focus on handler logic
3. **Integration Tests**: Test actual full-text search with real data
4. **No In-Memory Database**: SQLite doesn't support PostgreSQL full-text search
5. **Performance Assertions**: Ensure queries complete within SLA
6. **Edge Case Coverage**: Empty results, special characters, pagination boundaries

## Test Coverage Goals

- Unit Tests: 90%+ coverage
- Integration Tests: All repository methods
- API Tests: All endpoints and status codes
- Performance Tests: Critical paths under load
