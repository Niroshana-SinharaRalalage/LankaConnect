using System.Linq.Expressions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.CreateNewsletter;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Events;
using LankaConnect.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Phase 6A.85: TDD tests for CreateNewsletterCommandHandler
/// Test Scenario: When user selects "All Locations", backend must populate ALL metro areas
///
/// Root Cause Fix: Newsletter.MetroAreaIds must contain all 84 metros for matching logic to work:
///   Newsletter.MetroAreaIds ∩ Subscriber.MetroAreaIds = Matched Recipients
/// </summary>
public class CreateNewsletterCommandHandlerTests
{
    private readonly Mock<INewsletterRepository> _mockNewsletterRepository;
    private readonly Mock<IEmailGroupRepository> _mockEmailGroupRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateNewsletterCommandHandler>> _mockLogger;
    private readonly Mock<DbSet<MetroArea>> _mockMetroAreaDbSet;
    private readonly Mock<DbSet<EmailGroup>> _mockEmailGroupDbSet;
    private readonly CreateNewsletterCommandHandler _handler;

    public CreateNewsletterCommandHandlerTests()
    {
        _mockNewsletterRepository = new Mock<INewsletterRepository>();
        _mockEmailGroupRepository = new Mock<IEmailGroupRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CreateNewsletterCommandHandler>>();
        _mockMetroAreaDbSet = new Mock<DbSet<MetroArea>>();
        _mockEmailGroupDbSet = new Mock<DbSet<EmailGroup>>();

        // Setup current user
        _mockCurrentUserService.Setup(x => x.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _mockCurrentUserService.Setup(x => x.IsAdmin).Returns(false);

        _handler = new CreateNewsletterCommandHandler(
            _mockNewsletterRepository.Object,
            _mockEmailGroupRepository.Object,
            _mockCurrentUserService.Object,
            _mockDbContext.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    /// <summary>
    /// Phase 6A.85 - Test #1: CRITICAL BUG FIX TEST
    /// When user selects "All Locations" (targetAllLocations = true), backend must:
    /// 1. Query events.metro_areas table
    /// 2. Get all active metro area IDs (84 metros)
    /// 3. Pass those IDs to Newsletter.Create()
    ///
    /// This test will FAIL until we implement the fix in CreateNewsletterCommandHandler
    /// </summary>
    [Fact]
    public async Task Handle_WhenTargetAllLocationsTrue_ShouldPopulateAllMetroAreas()
    {
        // Arrange
        var command = new CreateNewsletterCommand(
            Title: "Important Announcement",
            Description: "This affects all locations",
            EmailGroupIds: new List<Guid>(),
            IncludeNewsletterSubscribers: true,
            EventId: null,
            MetroAreaIds: null,         // Frontend sends null when "All Locations" selected
            TargetAllLocations: true,   // User selected "All Locations"
            IsAnnouncementOnly: false
        );

        // Create 84 mock metro areas (matching production database)
        var allMetroAreas = CreateMockMetroAreas(84);

        // Setup DbContext to return DbSet<MetroArea>
        SetupDbContextWithMetroAreas(allMetroAreas);

        // Capture the Newsletter that gets created
        Newsletter? capturedNewsletter = null;
        _mockNewsletterRepository
            .Setup(x => x.AddAsync(It.IsAny<Newsletter>(), It.IsAny<CancellationToken>()))
            .Callback<Newsletter, CancellationToken>((newsletter, _) => capturedNewsletter = newsletter)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue("newsletter should be created successfully");

        capturedNewsletter.Should().NotBeNull("newsletter should be captured");
        capturedNewsletter!.TargetAllLocations.Should().BeTrue("target all locations flag should be set");

        // CRITICAL ASSERTION: MetroAreaIds should contain ALL 84 metros
        capturedNewsletter.MetroAreaIds.Should().HaveCount(84,
            "when targetAllLocations=true, ALL metro areas must be populated for matching logic to work");

        // Verify all metro IDs are present
        var expectedMetroIds = allMetroAreas.Select(m => m.Id).ToList();
        capturedNewsletter.MetroAreaIds.Should().BeEquivalentTo(expectedMetroIds,
            "metro area IDs should match the database metro areas");

        _mockNewsletterRepository.Verify(
            x => x.AddAsync(It.IsAny<Newsletter>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockUnitOfWork.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Phase 6A.85 - Test #2: Verify specific metro areas still work
    /// When user selects specific metros, those should be passed through as-is
    /// </summary>
    [Fact]
    public async Task Handle_WhenSpecificMetroAreasProvided_ShouldUseProvidedMetroAreas()
    {
        // Arrange
        var ohioMetro1 = Guid.Parse("39111111-1111-1111-1111-111111111001"); // Cleveland
        var ohioMetro2 = Guid.Parse("39111111-1111-1111-1111-111111111002"); // Columbus
        var ohioMetro3 = Guid.Parse("39111111-1111-1111-1111-111111111003"); // Cincinnati

        var command = new CreateNewsletterCommand(
            Title: "Ohio Event Newsletter",
            Description: "Only for Ohio subscribers",
            EmailGroupIds: new List<Guid>(),
            IncludeNewsletterSubscribers: true,
            EventId: null,
            MetroAreaIds: new List<Guid> { ohioMetro1, ohioMetro2, ohioMetro3 },
            TargetAllLocations: false,  // User selected specific metros
            IsAnnouncementOnly: false
        );

        Newsletter? capturedNewsletter = null;
        _mockNewsletterRepository
            .Setup(x => x.AddAsync(It.IsAny<Newsletter>(), It.IsAny<CancellationToken>()))
            .Callback<Newsletter, CancellationToken>((newsletter, _) => capturedNewsletter = newsletter)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedNewsletter.Should().NotBeNull();
        capturedNewsletter!.TargetAllLocations.Should().BeFalse();

        // Should use exactly the provided metro areas
        capturedNewsletter.MetroAreaIds.Should().HaveCount(3);
        capturedNewsletter.MetroAreaIds.Should().Contain(ohioMetro1);
        capturedNewsletter.MetroAreaIds.Should().Contain(ohioMetro2);
        capturedNewsletter.MetroAreaIds.Should().Contain(ohioMetro3);
    }

    /// <summary>
    /// Phase 6A.85 - Test #3: Edge case - targetAllLocations=true but user manually provided metros
    /// This shouldn't happen in UI, but handle gracefully
    /// </summary>
    [Fact]
    public async Task Handle_WhenTargetAllLocationsTrue_AndMetroAreasProvided_ShouldQueryAllMetros()
    {
        // Arrange
        var partialMetros = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var command = new CreateNewsletterCommand(
            Title: "Edge Case Newsletter",
            Description: "Testing edge case",
            EmailGroupIds: new List<Guid>(),
            IncludeNewsletterSubscribers: true,
            EventId: null,
            MetroAreaIds: partialMetros, // But somehow provided specific metros (edge case)
            TargetAllLocations: true,   // User selected "All Locations"
            IsAnnouncementOnly: false
        );

        var allMetroAreas = CreateMockMetroAreas(84);
        SetupDbContextWithMetroAreas(allMetroAreas);

        Newsletter? capturedNewsletter = null;
        _mockNewsletterRepository
            .Setup(x => x.AddAsync(It.IsAny<Newsletter>(), It.IsAny<CancellationToken>()))
            .Callback<Newsletter, CancellationToken>((newsletter, _) => capturedNewsletter = newsletter)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedNewsletter.Should().NotBeNull();

        // When targetAllLocations=true, ALWAYS query all metros (ignore provided partial list)
        // This matches architectural guidance: targetAllLocations is authoritative
        capturedNewsletter!.MetroAreaIds.Should().HaveCount(84,
            "targetAllLocations=true should override any partially provided metro list");
    }

    /// <summary>
    /// Phase 6A.85 - Test #4: Empty metro areas check (only Active metros)
    /// Should only query active metros from database
    /// </summary>
    [Fact]
    public async Task Handle_WhenTargetAllLocationsTrue_ShouldOnlyQueryActiveMetroAreas()
    {
        // Arrange
        var command = new CreateNewsletterCommand(
            Title: "Test Newsletter",
            Description: "Testing active metros only",
            EmailGroupIds: new List<Guid>(),
            IncludeNewsletterSubscribers: true,
            EventId: null,
            MetroAreaIds: null,
            TargetAllLocations: true,
            IsAnnouncementOnly: false
        );

        // Create mix of active and inactive metros
        var activeMetros = CreateMockMetroAreas(80, isActive: true);
        var inactiveMetros = CreateMockMetroAreas(4, isActive: false);
        var allMetros = activeMetros.Concat(inactiveMetros).ToList();

        SetupDbContextWithMetroAreas(allMetros);

        Newsletter? capturedNewsletter = null;
        _mockNewsletterRepository
            .Setup(x => x.AddAsync(It.IsAny<Newsletter>(), It.IsAny<CancellationToken>()))
            .Callback<Newsletter, CancellationToken>((newsletter, _) => capturedNewsletter = newsletter)
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedNewsletter.Should().NotBeNull();

        // Should only include ACTIVE metros (80, not 84)
        capturedNewsletter!.MetroAreaIds.Should().HaveCount(80,
            "only active metro areas should be included");

        var expectedActiveIds = activeMetros.Select(m => m.Id);
        capturedNewsletter.MetroAreaIds.Should().BeEquivalentTo(expectedActiveIds);

        var inactiveIds = inactiveMetros.Select(m => m.Id);
        foreach (var inactiveId in inactiveIds)
        {
            capturedNewsletter.MetroAreaIds.Should().NotContain(inactiveId,
                "inactive metros should be excluded");
        }
    }

    #region Test Helpers

    private List<MetroArea> CreateMockMetroAreas(int count, bool isActive = true)
    {
        var metros = new List<MetroArea>();
        var states = new[] { "AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA" };

        for (int i = 0; i < count; i++)
        {
            var state = states[i % states.Length];
            // Keep lat/long within valid ranges: lat (-90 to 90), long (-180 to 180)
            var lat = 30.0 + (i % 50) * 0.5;  // Range: 30.0 to 54.5
            var lon = -120.0 + (i % 50) * 0.5; // Range: -120.0 to -95.5

            var metro = MetroArea.Create(
                id: Guid.NewGuid(),
                name: $"Metro {i + 1}",
                state: state,
                centerLatitude: lat,
                centerLongitude: lon,
                radiusMiles: 50,
                isStateLevelArea: false,
                isActive: isActive
            );
            metros.Add(metro);
        }

        return metros;
    }

    private void SetupDbContextWithMetroAreas(List<MetroArea> metroAreas)
    {
        // Setup queryable DbSet<MetroArea>
        var queryableMetros = metroAreas.AsQueryable();

        _mockMetroAreaDbSet.As<IQueryable<MetroArea>>()
            .Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<MetroArea>(queryableMetros.Provider));

        _mockMetroAreaDbSet.As<IQueryable<MetroArea>>()
            .Setup(m => m.Expression)
            .Returns(queryableMetros.Expression);

        _mockMetroAreaDbSet.As<IQueryable<MetroArea>>()
            .Setup(m => m.ElementType)
            .Returns(queryableMetros.ElementType);

        _mockMetroAreaDbSet.As<IQueryable<MetroArea>>()
            .Setup(m => m.GetEnumerator())
            .Returns(() => queryableMetros.GetEnumerator());

        // Setup IApplicationDbContext to return the mocked DbSet when cast to DbContext
        var mockDbContext = new Mock<DbContext>();
        mockDbContext.Setup(x => x.Set<MetroArea>()).Returns(_mockMetroAreaDbSet.Object);
        mockDbContext.Setup(x => x.Set<EmailGroup>()).Returns(_mockEmailGroupDbSet.Object);

        _mockDbContext.As<DbContext>().Setup(x => x.Set<MetroArea>()).Returns(_mockMetroAreaDbSet.Object);
        _mockDbContext.As<DbContext>().Setup(x => x.Set<EmailGroup>()).Returns(_mockEmailGroupDbSet.Object);
    }

    #endregion
}

#region Test Infrastructure for Async Queries

/// <summary>
/// Helper class to enable async LINQ queries in unit tests
/// Required for testing EF Core async operations with mocked DbSet
/// </summary>
internal class TestAsyncQueryProvider<TEntity> : IQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }

    public T Current => _inner.Current;
}

#endregion
