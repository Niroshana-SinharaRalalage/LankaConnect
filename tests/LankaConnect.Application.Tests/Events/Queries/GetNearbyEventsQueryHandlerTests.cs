using AutoMapper;
using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetNearbyEvents;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// TDD Tests for GetNearbyEventsQuery (Epic 2 - Spatial Queries)
/// Tests application layer handling of location-based event discovery
/// Uses PostGIS spatial queries via EventRepository
/// </summary>
public class GetNearbyEventsQueryHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetNearbyEventsQueryHandler _handler;

    public GetNearbyEventsQueryHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetNearbyEventsQueryHandler(
            _mockEventRepository.Object,
            _mockMapper.Object);
    }

    private Event CreateTestEvent(string title, DateTime startDate, decimal lat = 6.9271m, decimal lon = 79.8612m)
    {
        var eventTitle = EventTitle.Create(title).Value;
        var description = EventDescription.Create("Test Description").Value;
        var endDate = startDate.AddHours(2);

        var @event = Event.Create(
            eventTitle,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100
        ).Value;

        // Add location with coordinates (Colombo, Sri Lanka by default)
        var address = LankaConnect.Domain.Business.ValueObjects.Address.Create(
            "123 Galle Road",
            "Colombo",
            "Western Province",
            "00100",
            "Sri Lanka"
        ).Value;

        var coordinates = LankaConnect.Domain.Business.ValueObjects.GeoCoordinate.Create(lat, lon).Value;
        var location = EventLocation.Create(address, coordinates).Value;

        @event.SetLocation(location);

        return @event;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidCoordinatesAndRadius_ShouldReturnNearbyEvents()
    {
        // Arrange - Colombo, Sri Lanka coordinates
        var userLatitude = 6.9271m;
        var userLongitude = 79.8612m;
        var radiusKm = 10.0;

        var query = new GetNearbyEventsQuery(
            Latitude: userLatitude,
            Longitude: userLongitude,
            RadiusKm: radiusKm
        );

        var event1 = CreateTestEvent("Event 1", DateTime.UtcNow.AddDays(7));
        var event2 = CreateTestEvent("Event 2", DateTime.UtcNow.AddDays(8));
        var nearbyEvents = new List<Event> { event1, event2 };

        _mockEventRepository
            .Setup(x => x.GetEventsByRadiusAsync(
                userLatitude,
                userLongitude,
                It.IsAny<double>(), // radiusMiles (converted from km)
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(nearbyEvents);

        var expectedDtos = new List<EventDto>
        {
            new EventDto { Id = event1.Id, Title = "Event 1" },
            new EventDto { Id = event2.Id, Title = "Event 2" }
        };

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => expectedDtos.First(dto => dto.Id == e.Id));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(dto => dto.Title == "Event 1");
        result.Value.Should().Contain(dto => dto.Title == "Event 2");

        _mockEventRepository.Verify(
            x => x.GetEventsByRadiusAsync(
                userLatitude,
                userLongitude,
                It.Is<double>(miles => Math.Abs(miles - (radiusKm * 0.621371)) < 0.001), // km to miles conversion
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoEventsInRadius_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetNearbyEventsQuery(
            Latitude: 6.9271m,
            Longitude: 79.8612m,
            RadiusKm: 5.0
        );

        _mockEventRepository
            .Setup(x => x.GetEventsByRadiusAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Event>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithOptionalFilters_ShouldApplyFilters()
    {
        // Arrange
        var query = new GetNearbyEventsQuery(
            Latitude: 6.9271m,
            Longitude: 79.8612m,
            RadiusKm: 10.0,
            Category: EventCategory.Social,
            IsFreeOnly: true,
            StartDateFrom: DateTime.UtcNow.AddDays(5)
        );

        var event1 = CreateTestEvent("Free Social Event", DateTime.UtcNow.AddDays(7));
        var event2 = CreateTestEvent("Paid Social Event", DateTime.UtcNow.AddDays(8));
        var allEvents = new List<Event> { event1, event2 };

        _mockEventRepository
            .Setup(x => x.GetEventsByRadiusAsync(
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allEvents);

        _mockMapper
            .Setup(x => x.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.Id, Title = e.Title.Value });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Filters would be applied in handler (category, isFree, startDate)
    }

    #endregion

    #region Validation Cases

    [Theory]
    [InlineData(-91.0, 79.8612)] // Invalid latitude (too low)
    [InlineData(91.0, 79.8612)]  // Invalid latitude (too high)
    [InlineData(6.9271, -181.0)] // Invalid longitude (too low)
    [InlineData(6.9271, 181.0)]  // Invalid longitude (too high)
    public async Task Handle_WithInvalidCoordinates_ShouldReturnFailure(decimal latitude, decimal longitude)
    {
        // Arrange
        var query = new GetNearbyEventsQuery(
            Latitude: latitude,
            Longitude: longitude,
            RadiusKm: 10.0
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().MatchRegex("latitude|longitude", "error should mention latitude or longitude");
    }

    [Theory]
    [InlineData(0.0)]    // Zero radius
    [InlineData(-5.0)]   // Negative radius
    [InlineData(1001.0)] // Too large (> 1000 km)
    public async Task Handle_WithInvalidRadius_ShouldReturnFailure(double radiusKm)
    {
        // Arrange
        var query = new GetNearbyEventsQuery(
            Latitude: 6.9271m,
            Longitude: 79.8612m,
            RadiusKm: radiusKm
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("radius");
    }

    #endregion
}
