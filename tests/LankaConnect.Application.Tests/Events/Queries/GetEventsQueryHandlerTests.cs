using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Domain.Business.ValueObjects;
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
    private readonly Mock<IRegistrationRepository> _mockRegistrationRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GetEventsQueryHandler>> _mockLogger;
    private readonly GetEventsQueryHandler _handler;

    public GetEventsQueryHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockRegistrationRepository = new Mock<IRegistrationRepository>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GetEventsQueryHandler>>();

        _handler = new GetEventsQueryHandler(
            _mockEventRepository.Object,
            _mockUserRepository.Object,
            _mockRegistrationRepository.Object,
            _mockDbContext.Object,
            _mockCurrentUserService.Object,
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

    #region Issue #23 - Guest User Coordinate Filtering Bug

    /// <summary>
    /// Helper method to create a test event with optional location and coordinates.
    /// Used for Issue #23 tests.
    /// </summary>
    private Event CreateTestEventWithLocation(
        string title,
        EventStatus status,
        bool hasLocation = false,
        bool hasCoordinates = false,
        decimal latitude = 34.0522m,
        decimal longitude = -118.2437m)
    {
        var @event = CreateTestEvent(title, status);

        if (hasLocation)
        {
            var address = Address.Create("123 Test St", "Los Angeles", "CA", "90001", "USA").Value;

            EventLocation location;
            if (hasCoordinates)
            {
                var coordinates = GeoCoordinate.Create(latitude, longitude).Value;
                location = EventLocation.Create(address, coordinates).Value;
            }
            else
            {
                location = EventLocation.Create(address).Value;
            }

            @event.SetLocation(location);
        }

        return @event;
    }

    /// <summary>
    /// Issue #23 TDD Test: Guest users with geolocation should see ALL events,
    /// including events without coordinates.
    ///
    /// BUG: SortEventsByDistance() filters out events without coordinates,
    /// and then remainingEvents.Clear() is called, losing those events forever.
    ///
    /// Expected: Both events with and without coordinates should be returned.
    /// Actual (BUG): Only events with coordinates are returned.
    /// </summary>
    [Fact]
    public async Task Handle_GuestUserWithCoordinates_ShouldReturnEventsWithAndWithoutCoordinates()
    {
        // Arrange
        // Create events: some with coordinates, some without
        var eventWithCoords = CreateTestEventWithLocation(
            "Event With Coords",
            EventStatus.Published,
            hasLocation: true,
            hasCoordinates: true,
            latitude: 34.0522m,
            longitude: -118.2437m);

        var eventWithoutCoords = CreateTestEventWithLocation(
            "Event Without Coords",
            EventStatus.Published,
            hasLocation: true,
            hasCoordinates: false);

        var eventWithNoLocation = CreateTestEventWithLocation(
            "Event No Location",
            EventStatus.Published,
            hasLocation: false,
            hasCoordinates: false);

        var allEvents = new List<Event>
        {
            eventWithCoords,
            eventWithoutCoords,
            eventWithNoLocation
        };

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto
            {
                Id = e.Id,
                Title = e.Title.Value,
                Status = e.Status,
                StartDate = e.StartDate.GetValueOrDefault() // Phase 8YA-2 TODO: test fixture should accept DateTime?
            });

        // Guest user with coordinates (triggers location-based sorting)
        var query = new GetEventsQuery(
            Latitude: 34.0522m,
            Longitude: -118.2437m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Issue #23 FIX: ALL three events should be returned
        // - Event with coordinates (sorted by distance)
        // - Event without coordinates (appended, sorted by date)
        // - Event with no location (appended, sorted by date)
        result.Value.Should().HaveCount(3,
            "Guest users should see ALL events, including those without coordinates");

        result.Value.Should().Contain(e => e.Title == "Event With Coords");
        result.Value.Should().Contain(e => e.Title == "Event Without Coords");
        result.Value.Should().Contain(e => e.Title == "Event No Location");
    }

    /// <summary>
    /// Issue #23 TDD Test: Events with coordinates should be sorted by distance
    /// and appear before events without coordinates.
    /// </summary>
    [Fact]
    public async Task Handle_GuestUserWithCoordinates_EventsWithCoordsAppearBeforeEventsWithout()
    {
        // Arrange
        // Create event WITH coordinates near the query location
        var nearbyEvent = CreateTestEventWithLocation(
            "Nearby Event",
            EventStatus.Published,
            hasLocation: true,
            hasCoordinates: true,
            latitude: 34.0522m,  // Same as query location
            longitude: -118.2437m);

        // Create event WITHOUT coordinates
        var noCoordEvent = CreateTestEventWithLocation(
            "No Coord Event",
            EventStatus.Published,
            hasLocation: true,
            hasCoordinates: false);

        var allEvents = new List<Event> { noCoordEvent, nearbyEvent }; // Order: no-coord first

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto
            {
                Id = e.Id,
                Title = e.Title.Value,
                Status = e.Status,
                StartDate = e.StartDate.GetValueOrDefault() // Phase 8YA-2 TODO: test fixture should accept DateTime?
            });

        // Guest user with coordinates
        var query = new GetEventsQuery(
            Latitude: 34.0522m,
            Longitude: -118.2437m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        // Events with coordinates should appear BEFORE events without coordinates
        var resultList = result.Value.ToList();
        resultList[0].Title.Should().Be("Nearby Event",
            "Events with coordinates (sorted by distance) should come first");
        resultList[1].Title.Should().Be("No Coord Event",
            "Events without coordinates should be appended at the end");
    }

    /// <summary>
    /// Issue #23 TDD Test: Multiple events without coordinates should be
    /// sorted by StartDate (ascending).
    /// </summary>
    [Fact]
    public async Task Handle_GuestUserWithCoordinates_EventsWithoutCoordsSortedByDate()
    {
        // Arrange
        var eventNoCoords1 = CreateTestEventWithLocation(
            "Event No Coords - Later",
            EventStatus.Published,
            hasLocation: true,
            hasCoordinates: false);

        var eventNoCoords2 = CreateTestEventWithLocation(
            "Event No Coords - Earlier",
            EventStatus.Published,
            hasLocation: true,
            hasCoordinates: false);

        // Use reflection to set different start dates
        var dateField = typeof(Event).GetField("<StartDate>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        dateField?.SetValue(eventNoCoords1, DateTime.UtcNow.AddDays(10));
        dateField?.SetValue(eventNoCoords2, DateTime.UtcNow.AddDays(5));

        var allEvents = new List<Event> { eventNoCoords1, eventNoCoords2 }; // Later first

        _mockEventRepository
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto
            {
                Id = e.Id,
                Title = e.Title.Value,
                Status = e.Status,
                StartDate = e.StartDate.GetValueOrDefault() // Phase 8YA-2 TODO: test fixture should accept DateTime?
            });

        // Guest user with coordinates
        var query = new GetEventsQuery(
            Latitude: 34.0522m,
            Longitude: -118.2437m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        // Events without coordinates should be sorted by date (earlier first)
        var resultList = result.Value.ToList();
        resultList[0].Title.Should().Be("Event No Coords - Earlier",
            "Events without coordinates should be sorted by StartDate ascending");
        resultList[1].Title.Should().Be("Event No Coords - Later");
    }

    #endregion

    #region Issue #33 - SearchAsync IncludeAllStatuses Parameter

    /// <summary>
    /// Issue #33 TDD Test: When searching with SearchTerm and IncludeAllStatuses=false (default),
    /// SearchAsync should be called with includeAllStatuses=false, excluding Draft/UnderReview.
    /// </summary>
    [Fact]
    public async Task Handle_WithSearchTermAndIncludeAllStatusesFalse_ShouldPassFlagToSearchAsync()
    {
        // Arrange
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var searchResults = new List<Event> { publishedEvent };

        _mockEventRepository
            .Setup(x => x.SearchAsync(
                "test",                                     // searchTerm
                It.IsAny<int>(),                           // limit
                It.IsAny<int>(),                           // offset
                It.IsAny<EventCategory?>(),                // category
                It.IsAny<bool?>(),                         // isFreeOnly
                It.IsAny<DateTime?>(),                     // startDateFrom
                It.IsAny<bool>(),                          // excludeCancelled
                false,                                      // includeAllStatuses = false (expected)
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((searchResults, 1));

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Default: IncludeAllStatuses = false
        var query = new GetEventsQuery(SearchTerm: "test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockEventRepository.Verify(
            x => x.SearchAsync(
                "test",
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<EventCategory?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                false,  // includeAllStatuses should be false
                It.IsAny<CancellationToken>()),
            Times.Once,
            "SearchAsync should be called with includeAllStatuses=false when IncludeAllStatuses is not set");
    }

    /// <summary>
    /// Issue #33 TDD Test: When searching with SearchTerm and IncludeAllStatuses=true,
    /// SearchAsync should be called with includeAllStatuses=true, including Draft/UnderReview.
    /// This is used for Dashboard Event Management where organizers need to see their draft events.
    /// </summary>
    [Fact]
    public async Task Handle_WithSearchTermAndIncludeAllStatusesTrue_ShouldPassFlagToSearchAsync()
    {
        // Arrange
        var draftEvent = CreateTestEvent("Draft Event", EventStatus.Draft);
        var publishedEvent = CreateTestEvent("Published Event", EventStatus.Published);
        var searchResults = new List<Event> { draftEvent, publishedEvent };

        _mockEventRepository
            .Setup(x => x.SearchAsync(
                "test",                                     // searchTerm
                It.IsAny<int>(),                           // limit
                It.IsAny<int>(),                           // offset
                It.IsAny<EventCategory?>(),                // category
                It.IsAny<bool?>(),                         // isFreeOnly
                It.IsAny<DateTime?>(),                     // startDateFrom
                It.IsAny<bool>(),                          // excludeCancelled
                true,                                       // includeAllStatuses = true (expected)
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((searchResults, 2));

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value, Status = e.Status });

        // Issue #33 FIX: IncludeAllStatuses = true (for Dashboard Event Management)
        var query = new GetEventsQuery(SearchTerm: "test", IncludeAllStatuses: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.Title == "Draft Event");
        result.Value.Should().Contain(e => e.Title == "Published Event");

        _mockEventRepository.Verify(
            x => x.SearchAsync(
                "test",
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<EventCategory?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                true,  // includeAllStatuses should be true
                It.IsAny<CancellationToken>()),
            Times.Once,
            "SearchAsync should be called with includeAllStatuses=true when IncludeAllStatuses is set");
    }

    /// <summary>
    /// Issue #33 TDD Test: Dashboard Event Management search should return organizer's Draft events.
    /// This simulates the flow: Dashboard → GetEventsByOrganizer → GetEventsQuery(IncludeAllStatuses: true)
    /// </summary>
    [Fact]
    public async Task Handle_DashboardSearchWithIncludeAllStatuses_ShouldReturnDraftEvents()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var draftEvent = CreateTestEvent("My Draft Event", EventStatus.Draft);
        var publishedEvent = CreateTestEvent("My Published Event", EventStatus.Published);

        // Use reflection to set OrganizerId
        var orgIdField = typeof(Event).GetField("<OrganizerId>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        orgIdField?.SetValue(draftEvent, organizerId);
        orgIdField?.SetValue(publishedEvent, organizerId);

        var searchResults = new List<Event> { draftEvent, publishedEvent };

        _mockEventRepository
            .Setup(x => x.SearchAsync(
                "My",                                       // searchTerm
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<EventCategory?>(),
                It.IsAny<bool?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<bool>(),
                true,                                       // includeAllStatuses = true
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((searchResults, 2));

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto
            {
                Id = e.Id,
                Title = e.Title.Value,
                Status = e.Status,
                OrganizerId = organizerId
            });

        // Simulating Dashboard Event Management query with IncludeAllStatuses=true
        var query = new GetEventsQuery(SearchTerm: "My", IncludeAllStatuses: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2,
            "Dashboard Event Management search should return Draft events when IncludeAllStatuses=true");
        result.Value.Should().Contain(e => e.Status == EventStatus.Draft,
            "Draft events should be included in Dashboard search results");
    }

    #endregion
}
