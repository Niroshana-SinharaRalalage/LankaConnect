using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// TDD Tests for GetEventsQueryHandler
/// Phase 6A.88: Tests for IncludeAllStatuses flag to control Draft/UnderReview visibility
/// </summary>
public class GetEventsQueryHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GetEventsQueryHandler>> _mockLogger;
    private readonly GetEventsQueryHandler _handler;

    public GetEventsQueryHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GetEventsQueryHandler>>();

        _handler = new GetEventsQueryHandler(
            _mockEventRepository.Object,
            _mockUserRepository.Object,
            _mockDbContext.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    private Event CreateTestEvent(string title, EventStatus status)
    {
        var eventTitle = EventTitle.Create(title).Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        var @event = Event.Create(
            eventTitle,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100
        ).Value;

        // Use reflection to set status - auto-property backing field uses compiler-generated name
        var statusProperty = typeof(Event).GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var backingField = typeof(Event).GetField("<Status>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(@event, status);

        return @event;
    }

    #region IncludeAllStatuses Tests - Phase 6A.88

    [Fact]
    public async Task Handle_WithIncludeAllStatusesFalse_ShouldExcludeDraftEvents()
    {
        // Arrange
        var draftEvent = CreateTestEvent("Draft Event", EventStatus.Draft);
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var allEvents = new List<Event> { draftEvent, publishedEvent };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Default: IncludeAllStatuses = false
        var query = new GetEventsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().NotContain(e => e.Title == "Draft Event");
        result.Value.Should().Contain(e => e.Title == "Published Event");
    }

    [Fact]
    public async Task Handle_WithIncludeAllStatusesFalse_ShouldExcludeUnderReviewEvents()
    {
        // Arrange
        var underReviewEvent = CreateTestEvent("UnderReview Event", EventStatus.UnderReview);
        var activeEvent = CreateTestEvent("Active Event", EventStatus.Active);
        var allEvents = new List<Event> { underReviewEvent, activeEvent };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Default: IncludeAllStatuses = false
        var query = new GetEventsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.Should().NotContain(e => e.Title == "UnderReview Event");
        result.Value.Should().Contain(e => e.Title == "Active Event");
    }

    [Fact]
    public async Task Handle_WithIncludeAllStatusesTrue_ShouldIncludeDraftEvents()
    {
        // Arrange
        var draftEvent = CreateTestEvent("Draft Event", EventStatus.Draft);
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var allEvents = new List<Event> { draftEvent, publishedEvent };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Phase 6A.88: IncludeAllStatuses = true
        var query = new GetEventsQuery(IncludeAllStatuses: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.Title == "Draft Event");
        result.Value.Should().Contain(e => e.Title == "Published Event");
    }

    [Fact]
    public async Task Handle_WithIncludeAllStatusesTrue_ShouldIncludeUnderReviewEvents()
    {
        // Arrange
        var underReviewEvent = CreateTestEvent("UnderReview Event", EventStatus.UnderReview);
        var activeEvent = CreateTestEvent("Active Event", EventStatus.Active);
        var allEvents = new List<Event> { underReviewEvent, activeEvent };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Phase 6A.88: IncludeAllStatuses = true
        var query = new GetEventsQuery(IncludeAllStatuses: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.Title == "UnderReview Event");
        result.Value.Should().Contain(e => e.Title == "Active Event");
    }

    [Fact]
    public async Task Handle_WithIncludeAllStatusesTrue_ShouldIncludeAllStatuses()
    {
        // Arrange - Create events with various statuses
        var draftEvent = CreateTestEvent("Draft Event", EventStatus.Draft);
        var underReviewEvent = CreateTestEvent("UnderReview Event", EventStatus.UnderReview);
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var activeEvent = CreateTestEvent("Active Event", EventStatus.Active);
        var cancelledEvent = CreateTestEvent("Cancelled Event", EventStatus.Cancelled);

        var allEvents = new List<Event>
        {
            draftEvent, underReviewEvent, publishedEvent, activeEvent, cancelledEvent
        };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Phase 6A.88: IncludeAllStatuses = true
        var query = new GetEventsQuery(IncludeAllStatuses: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value.Should().Contain(e => e.Status == EventStatus.Draft);
        result.Value.Should().Contain(e => e.Status == EventStatus.UnderReview);
        result.Value.Should().Contain(e => e.Status == EventStatus.Published);
        result.Value.Should().Contain(e => e.Status == EventStatus.Active);
        result.Value.Should().Contain(e => e.Status == EventStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_DefaultBehavior_ShouldExcludeDraftAndUnderReview()
    {
        // Arrange - Verify default behavior (backward compatibility)
        var draftEvent = CreateTestEvent("Draft Event", EventStatus.Draft);
        var underReviewEvent = CreateTestEvent("UnderReview Event", EventStatus.UnderReview);
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var activeEvent = CreateTestEvent("Active Event", EventStatus.Active);
        var cancelledEvent = CreateTestEvent("Cancelled Event", EventStatus.Cancelled);

        var allEvents = new List<Event>
        {
            draftEvent, underReviewEvent, publishedEvent, activeEvent, cancelledEvent
        };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Default query without IncludeAllStatuses
        var query = new GetEventsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3); // Published, Active, Cancelled
        result.Value.Should().NotContain(e => e.Status == EventStatus.Draft);
        result.Value.Should().NotContain(e => e.Status == EventStatus.UnderReview);
        result.Value.Should().Contain(e => e.Status == EventStatus.Published);
        result.Value.Should().Contain(e => e.Status == EventStatus.Active);
        result.Value.Should().Contain(e => e.Status == EventStatus.Cancelled);
    }

    #endregion

    #region Existing Functionality Tests (Regression Prevention)

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldUseStatusSpecificQuery()
    {
        // Arrange
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var publishedEvents = new List<Event> { publishedEvent };

        _mockEventRepository
            .Setup(x => x.GetEventsByStatusAsync(EventStatus.Published, It.IsAny<CancellationToken>()))
            .ReturnsAsync(publishedEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        var query = new GetEventsQuery(Status: EventStatus.Published);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEventRepository.Verify(
            x => x.GetEventsByStatusAsync(EventStatus.Published, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCityFilter_ShouldUseCitySpecificQuery()
    {
        // Arrange
        var colomboEvent = CreateTestEvent("Colombo Event", EventStatus.Published);
        var colomboEvents = new List<Event> { colomboEvent };

        _mockEventRepository
            .Setup(x => x.GetEventsByCityAsync("Colombo", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(colomboEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        var query = new GetEventsQuery(City: "Colombo");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEventRepository.Verify(
            x => x.GetEventsByCityAsync("Colombo", It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
