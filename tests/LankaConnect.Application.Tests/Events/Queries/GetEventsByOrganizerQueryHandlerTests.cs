using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Application.Events.Queries.GetEventsByOrganizer;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// TDD Tests for GetEventsByOrganizerQueryHandler
/// Phase 6A.88: Tests to verify organizer can see Draft events in Event Management
/// </summary>
public class GetEventsByOrganizerQueryHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<GetEventsByOrganizerQueryHandler>> _mockLogger;
    private readonly GetEventsByOrganizerQueryHandler _handler;

    private readonly Guid _organizerId = Guid.NewGuid();

    public GetEventsByOrganizerQueryHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<GetEventsByOrganizerQueryHandler>>();

        _handler = new GetEventsByOrganizerQueryHandler(
            _mockEventRepository.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    private Event CreateTestEvent(string title, Guid organizerId, EventStatus status = EventStatus.Published)
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
            organizerId,
            100
        ).Value;

        // Use reflection to set status - auto-property backing field uses compiler-generated name
        var backingField = typeof(Event).GetField("<Status>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        backingField?.SetValue(@event, status);

        return @event;
    }

    #region Phase 6A.88: Draft Events Visibility Tests

    [Fact]
    public async Task Handle_OrganizerEvents_ShouldIncludeDraftEvents()
    {
        // Arrange
        var draftEvent = CreateTestEvent("Draft Event", _organizerId, EventStatus.Draft);
        var publishedEvent = CreateTestEvent("Published Event", _organizerId, EventStatus.Published);
        var organizerEvents = new List<Event> { draftEvent, publishedEvent };

        _mockEventRepository
            .Setup(x => x.GetByOrganizerAsync(_organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizerEvents);

        var eventDtos = new List<EventDto>
        {
            new EventDto { Id = draftEvent.Id, Title = "Draft Event", Status = EventStatus.Draft },
            new EventDto { Id = publishedEvent.Id, Title = "Published Event", Status = EventStatus.Published }
        };

        // Mock GetEventsQuery to return events with IncludeAllStatuses=true
        _mockMediator
            .Setup(x => x.Send(It.Is<GetEventsQuery>(q => q.IncludeAllStatuses == true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(eventDtos));

        var query = new GetEventsByOrganizerQuery(_organizerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.Title == "Draft Event");
        result.Value.Should().Contain(e => e.Title == "Published Event");

        // Verify GetEventsQuery was called with IncludeAllStatuses=true
        _mockMediator.Verify(
            x => x.Send(It.Is<GetEventsQuery>(q => q.IncludeAllStatuses == true), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OrganizerEvents_ShouldIncludeUnderReviewEvents()
    {
        // Arrange
        var underReviewEvent = CreateTestEvent("UnderReview Event", _organizerId, EventStatus.UnderReview);
        var activeEvent = CreateTestEvent("Active Event", _organizerId, EventStatus.Active);
        var organizerEvents = new List<Event> { underReviewEvent, activeEvent };

        _mockEventRepository
            .Setup(x => x.GetByOrganizerAsync(_organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizerEvents);

        var eventDtos = new List<EventDto>
        {
            new EventDto { Id = underReviewEvent.Id, Title = "UnderReview Event", Status = EventStatus.UnderReview },
            new EventDto { Id = activeEvent.Id, Title = "Active Event", Status = EventStatus.Active }
        };

        _mockMediator
            .Setup(x => x.Send(It.Is<GetEventsQuery>(q => q.IncludeAllStatuses == true), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(eventDtos));

        var query = new GetEventsByOrganizerQuery(_organizerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.Title == "UnderReview Event");
        result.Value.Should().Contain(e => e.Title == "Active Event");
    }

    [Fact]
    public async Task Handle_OrganizerEventsWithFilters_ShouldIncludeDraftEvents()
    {
        // Arrange
        var draftEvent = CreateTestEvent("Draft Social Event", _organizerId, EventStatus.Draft);
        var publishedEvent = CreateTestEvent("Published Social Event", _organizerId, EventStatus.Published);
        var organizerEvents = new List<Event> { draftEvent, publishedEvent };

        _mockEventRepository
            .Setup(x => x.GetByOrganizerAsync(_organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizerEvents);

        var eventDtos = new List<EventDto>
        {
            new EventDto { Id = draftEvent.Id, Title = "Draft Social Event", Status = EventStatus.Draft },
            new EventDto { Id = publishedEvent.Id, Title = "Published Social Event", Status = EventStatus.Published }
        };

        // With filters, GetEventsQuery should still have IncludeAllStatuses=true
        _mockMediator
            .Setup(x => x.Send(
                It.Is<GetEventsQuery>(q =>
                    q.IncludeAllStatuses == true &&
                    q.Category == EventCategory.Social),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(eventDtos));

        var query = new GetEventsByOrganizerQuery(
            _organizerId,
            Category: EventCategory.Social
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(e => e.Title == "Draft Social Event");

        // Verify GetEventsQuery was called with IncludeAllStatuses=true AND the filter
        _mockMediator.Verify(
            x => x.Send(
                It.Is<GetEventsQuery>(q =>
                    q.IncludeAllStatuses == true &&
                    q.Category == EventCategory.Social),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_OrganizerEventsWithSearchTerm_ShouldIncludeDraftEvents()
    {
        // Arrange
        var draftEvent = CreateTestEvent("Draft Concert", _organizerId, EventStatus.Draft);
        var organizerEvents = new List<Event> { draftEvent };

        _mockEventRepository
            .Setup(x => x.GetByOrganizerAsync(_organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizerEvents);

        var eventDtos = new List<EventDto>
        {
            new EventDto { Id = draftEvent.Id, Title = "Draft Concert", Status = EventStatus.Draft }
        };

        _mockMediator
            .Setup(x => x.Send(
                It.Is<GetEventsQuery>(q =>
                    q.IncludeAllStatuses == true &&
                    q.SearchTerm == "Concert"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(eventDtos));

        var query = new GetEventsByOrganizerQuery(
            _organizerId,
            SearchTerm: "Concert"
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(e => e.Title == "Draft Concert");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Handle_WithEmptyOrganizerId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetEventsByOrganizerQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Organizer ID");
    }

    [Fact]
    public async Task Handle_WhenNoEventsForOrganizer_ShouldReturnEmptyList()
    {
        // Arrange
        _mockEventRepository
            .Setup(x => x.GetByOrganizerAsync(_organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event>());

        var query = new GetEventsByOrganizerQuery(_organizerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Regression Prevention Tests

    [Fact]
    public async Task Handle_ShouldOnlyReturnOrganizerOwnEvents()
    {
        // Arrange
        var otherOrganizerId = Guid.NewGuid();
        var myEvent = CreateTestEvent("My Event", _organizerId, EventStatus.Published);
        var organizerEvents = new List<Event> { myEvent };

        _mockEventRepository
            .Setup(x => x.GetByOrganizerAsync(_organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizerEvents);

        // GetEventsQuery might return events from all organizers
        var allEventDtos = new List<EventDto>
        {
            new EventDto { Id = myEvent.Id, Title = "My Event", Status = EventStatus.Published },
            new EventDto { Id = Guid.NewGuid(), Title = "Other's Event", Status = EventStatus.Published }
        };

        _mockMediator
            .Setup(x => x.Send(It.IsAny<GetEventsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<EventDto>>.Success(allEventDtos));

        var query = new GetEventsByOrganizerQuery(_organizerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should only contain the organizer's own event, filtered by ID
        result.Value.Should().Contain(e => e.Title == "My Event");
    }

    #endregion
}
