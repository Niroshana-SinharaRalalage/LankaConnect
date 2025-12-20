using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using LankaConnect.IntegrationTests.Common;

namespace LankaConnect.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for Event Repository location-based queries using PostGIS.
/// Epic 2 Phase 1 Day 3 - Tests radius searches, city searches, and nearest events.
/// </summary>
public class EventRepositoryLocationTests : DockerComposeWebApiTestBase
{
    private EventRepository _repository = null!;
    private AppDbContext _context = null!;
    private readonly IGeoLocationService _geoLocationService = new GeoLocationService();
    private readonly ILogger<EventRepository> _logger = NullLogger<EventRepository>.Instance;

    [Fact]
    public async Task GetEventsByRadiusAsync_Should_Return_Events_Within_25_Miles()
    {
        // Arrange - Create events at different locations
        // Colombo city center: 6.9271, 79.8612
        var colomboEvent = await CreateTestEventWithLocationAsync(
            title: "Colombo Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        // Mount Lavinia (within 25 miles of Colombo): 6.8378, 79.8636
        var mountLaviniaEvent = await CreateTestEventWithLocationAsync(
            title: "Mount Lavinia Event",
            latitude: 6.8378m,
            longitude: 79.8636m,
            city: "Mount Lavinia");

        // Kandy (far from Colombo, > 25 miles): 7.2906, 80.6337
        var kandyEvent = await CreateTestEventWithLocationAsync(
            title: "Kandy Event",
            latitude: 7.2906m,
            longitude: 80.6337m,
            city: "Kandy");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(colomboEvent);
        await _repository.AddAsync(mountLaviniaEvent);
        await _repository.AddAsync(kandyEvent);
        await _context.CommitAsync();

        // Act - Search within 25 miles of Colombo city center
        var events = await _repository.GetEventsByRadiusAsync(6.9271m, 79.8612m, 25);

        // Assert - Should return Colombo and Mount Lavinia events, but not Kandy
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Title.Value == "Colombo Event");
        Assert.Contains(events, e => e.Title.Value == "Mount Lavinia Event");
        Assert.DoesNotContain(events, e => e.Title.Value == "Kandy Event");
    }

    [Fact]
    public async Task GetEventsByRadiusAsync_Should_Return_Events_Within_50_Miles()
    {
        // Arrange - Create events at different distances
        // Colombo city center: 6.9271, 79.8612
        var colomboEvent = await CreateTestEventWithLocationAsync(
            title: "Colombo Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        // Gampaha (within 50 miles): 7.0873, 79.9990
        var gampahaEvent = await CreateTestEventWithLocationAsync(
            title: "Gampaha Event",
            latitude: 7.0873m,
            longitude: 79.9990m,
            city: "Gampaha");

        // Nuwara Eliya (far, > 50 miles): 6.9497, 80.7891
        var nuwaraEliyaEvent = await CreateTestEventWithLocationAsync(
            title: "Nuwara Eliya Event",
            latitude: 6.9497m,
            longitude: 80.7891m,
            city: "Nuwara Eliya");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(colomboEvent);
        await _repository.AddAsync(gampahaEvent);
        await _repository.AddAsync(nuwaraEliyaEvent);
        await _context.CommitAsync();

        // Act - Search within 50 miles of Colombo
        var events = await _repository.GetEventsByRadiusAsync(6.9271m, 79.8612m, 50);

        // Assert - Should include Colombo and Gampaha, but not Nuwara Eliya
        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e.Title.Value == "Colombo Event");
        Assert.Contains(events, e => e.Title.Value == "Gampaha Event");
    }

    [Fact]
    public async Task GetEventsByRadiusAsync_Should_Return_Events_Within_100_Miles()
    {
        // Arrange - All events in Sri Lanka are within 100 miles of Colombo
        var colomboEvent = await CreateTestEventWithLocationAsync(
            title: "Colombo Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        var kandyEvent = await CreateTestEventWithLocationAsync(
            title: "Kandy Event",
            latitude: 7.2906m,
            longitude: 80.6337m,
            city: "Kandy");

        var galleEvent = await CreateTestEventWithLocationAsync(
            title: "Galle Event",
            latitude: 6.0535m,
            longitude: 80.2210m,
            city: "Galle");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(colomboEvent);
        await _repository.AddAsync(kandyEvent);
        await _repository.AddAsync(galleEvent);
        await _context.CommitAsync();

        // Act - Search within 100 miles of Colombo
        var events = await _repository.GetEventsByRadiusAsync(6.9271m, 79.8612m, 100);

        // Assert - Should return all events
        Assert.Equal(3, events.Count);
    }

    [Fact]
    public async Task GetEventsByRadiusAsync_Should_Only_Return_Published_Upcoming_Events()
    {
        // Arrange - Create events with different statuses
        var publishedEvent = await CreateTestEventWithLocationAsync(
            title: "Published Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo",
            status: EventStatus.Published,
            startDate: DateTime.UtcNow.AddDays(7));

        var draftEvent = await CreateTestEventWithLocationAsync(
            title: "Draft Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo",
            status: EventStatus.Draft,
            startDate: DateTime.UtcNow.AddDays(7));

        var pastEvent = await CreateTestEventWithLocationAsync(
            title: "Past Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo",
            status: EventStatus.Published,
            startDate: DateTime.UtcNow.AddDays(-7));

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(publishedEvent);
        await _repository.AddAsync(draftEvent);
        await _repository.AddAsync(pastEvent);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetEventsByRadiusAsync(6.9271m, 79.8612m, 25);

        // Assert - Should only return the published upcoming event
        Assert.Single(events);
        Assert.Equal("Published Event", events.First().Title.Value);
    }

    [Fact]
    public async Task GetEventsByRadiusAsync_Should_Return_Empty_When_No_Events_In_Radius()
    {
        // Arrange - Create event far away
        var event1 = await CreateTestEventWithLocationAsync(
            title: "Far Event",
            latitude: 7.2906m,
            longitude: 80.6337m,
            city: "Kandy");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(event1);
        await _context.CommitAsync();

        // Act - Search in a location with no events (near Galle, far from Kandy)
        var events = await _repository.GetEventsByRadiusAsync(6.0535m, 80.2210m, 10);

        // Assert - Should return empty
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsByRadiusAsync_Should_Exclude_Events_Without_Location()
    {
        // Arrange - Create event with location and without location
        var eventWithLocation = await CreateTestEventWithLocationAsync(
            title: "Event With Location",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        var eventWithoutLocation = await CreateTestEventWithoutLocationAsync(
            title: "Event Without Location");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(eventWithLocation);
        await _repository.AddAsync(eventWithoutLocation);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetEventsByRadiusAsync(6.9271m, 79.8612m, 25);

        // Assert - Should only return event with location
        Assert.Single(events);
        Assert.Equal("Event With Location", events.First().Title.Value);
    }

    [Fact]
    public async Task GetEventsByCityAsync_Should_Return_Events_In_Specified_City()
    {
        // Arrange
        var colomboEvent1 = await CreateTestEventWithLocationAsync(
            title: "Colombo Event 1",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        var colomboEvent2 = await CreateTestEventWithLocationAsync(
            title: "Colombo Event 2",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        var kandyEvent = await CreateTestEventWithLocationAsync(
            title: "Kandy Event",
            latitude: 7.2906m,
            longitude: 80.6337m,
            city: "Kandy");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(colomboEvent1);
        await _repository.AddAsync(colomboEvent2);
        await _repository.AddAsync(kandyEvent);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetEventsByCityAsync("Colombo");

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("Colombo", e.Location!.Address.City));
    }

    [Fact]
    public async Task GetEventsByCityAsync_Should_Be_Case_Insensitive()
    {
        // Arrange
        var event1 = await CreateTestEventWithLocationAsync(
            title: "Event 1",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(event1);
        await _context.CommitAsync();

        // Act - Search with different casing
        var eventsLower = await _repository.GetEventsByCityAsync("colombo");
        var eventsUpper = await _repository.GetEventsByCityAsync("COLOMBO");
        var eventsMixed = await _repository.GetEventsByCityAsync("CoLoMbO");

        // Assert - All should return the same event
        Assert.Single(eventsLower);
        Assert.Single(eventsUpper);
        Assert.Single(eventsMixed);
    }

    [Fact]
    public async Task GetEventsByCityAsync_Should_Filter_By_State_When_Provided()
    {
        // Arrange
        var westernColombo = await CreateTestEventWithLocationAsync(
            title: "Western Colombo Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo",
            state: "Western Province");

        var centralColombo = await CreateTestEventWithLocationAsync(
            title: "Central Colombo Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo",
            state: "Central Province");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(westernColombo);
        await _repository.AddAsync(centralColombo);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetEventsByCityAsync("Colombo", "Western Province");

        // Assert
        Assert.Single(events);
        Assert.Equal("Western Colombo Event", events.First().Title.Value);
    }

    [Fact]
    public async Task GetEventsByCityAsync_Should_Return_Empty_For_Invalid_City()
    {
        // Arrange
        var event1 = await CreateTestEventWithLocationAsync(
            title: "Event 1",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(event1);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetEventsByCityAsync("NonExistentCity");

        // Assert
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsByCityAsync_Should_Return_Empty_For_Empty_City_Name()
    {
        // Arrange
        var event1 = await CreateTestEventWithLocationAsync(
            title: "Event 1",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(event1);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetEventsByCityAsync("");

        // Assert
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetNearestEventsAsync_Should_Return_Events_Ordered_By_Distance()
    {
        // Arrange - Create events at different distances from search point (Colombo: 6.9271, 79.8612)
        // Closest: Colombo Fort (6.9270, 79.8500) - ~1 km
        var closestEvent = await CreateTestEventWithLocationAsync(
            title: "Closest Event",
            latitude: 6.9270m,
            longitude: 79.8500m,
            city: "Colombo Fort");

        // Medium: Mount Lavinia (6.8378, 79.8636) - ~10 km
        var mediumEvent = await CreateTestEventWithLocationAsync(
            title: "Medium Event",
            latitude: 6.8378m,
            longitude: 79.8636m,
            city: "Mount Lavinia");

        // Farthest: Negombo (7.2084, 79.8358) - ~30 km
        var farthestEvent = await CreateTestEventWithLocationAsync(
            title: "Farthest Event",
            latitude: 7.2084m,
            longitude: 79.8358m,
            city: "Negombo");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(farthestEvent);  // Add in random order
        await _repository.AddAsync(closestEvent);
        await _repository.AddAsync(mediumEvent);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetNearestEventsAsync(6.9271m, 79.8612m, maxResults: 10);

        // Assert - Events should be ordered by distance (closest first)
        Assert.Equal(3, events.Count);
        Assert.Equal("Closest Event", events[0].Title.Value);
        Assert.Equal("Medium Event", events[1].Title.Value);
        Assert.Equal("Farthest Event", events[2].Title.Value);
    }

    [Fact]
    public async Task GetNearestEventsAsync_Should_Respect_MaxResults_Parameter()
    {
        // Arrange - Create 5 events
        for (int i = 0; i < 5; i++)
        {
            var @event = await CreateTestEventWithLocationAsync(
                title: $"Event {i}",
                latitude: 6.9271m + (i * 0.01m),
                longitude: 79.8612m,
                city: "City" + i);

            _context = DbContext;
            _repository = new EventRepository(_context, _geoLocationService, _logger);

            await _repository.AddAsync(@event);
        }
        await _context.CommitAsync();

        // Act - Request only 3 nearest events
        var events = await _repository.GetNearestEventsAsync(6.9271m, 79.8612m, maxResults: 3);

        // Assert
        Assert.Equal(3, events.Count);
    }

    [Fact]
    public async Task GetNearestEventsAsync_Should_Only_Return_Published_Upcoming_Events()
    {
        // Arrange
        var publishedEvent = await CreateTestEventWithLocationAsync(
            title: "Published Event",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo",
            status: EventStatus.Published,
            startDate: DateTime.UtcNow.AddDays(7));

        var draftEvent = await CreateTestEventWithLocationAsync(
            title: "Draft Event",
            latitude: 6.9270m,
            longitude: 79.8500m,
            city: "Colombo Fort",
            status: EventStatus.Draft,
            startDate: DateTime.UtcNow.AddDays(7));

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(publishedEvent);
        await _repository.AddAsync(draftEvent);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetNearestEventsAsync(6.9271m, 79.8612m, maxResults: 10);

        // Assert
        Assert.Single(events);
        Assert.Equal("Published Event", events.First().Title.Value);
    }

    [Fact]
    public async Task GetNearestEventsAsync_Should_Exclude_Events_Without_Coordinates()
    {
        // Arrange
        var eventWithCoordinates = await CreateTestEventWithLocationAsync(
            title: "Event With Coordinates",
            latitude: 6.9271m,
            longitude: 79.8612m,
            city: "Colombo");

        var eventWithoutCoordinates = await CreateTestEventWithoutLocationAsync(
            title: "Event Without Coordinates");

        _context = DbContext;
        _repository = new EventRepository(_context, _geoLocationService, _logger);

        await _repository.AddAsync(eventWithCoordinates);
        await _repository.AddAsync(eventWithoutCoordinates);
        await _context.CommitAsync();

        // Act
        var events = await _repository.GetNearestEventsAsync(6.9271m, 79.8612m, maxResults: 10);

        // Assert
        Assert.Single(events);
        Assert.Equal("Event With Coordinates", events.First().Title.Value);
    }

    // Helper methods to create test events
    private Task<Event> CreateTestEventWithLocationAsync(
        string title,
        decimal latitude,
        decimal longitude,
        string city,
        string state = "Test State",
        EventStatus status = EventStatus.Published,
        DateTime? startDate = null)
    {
        // Create EventTitle
        var titleResult = EventTitle.Create(title);
        if (!titleResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create EventTitle: {titleResult.Error}");

        // Create EventDescription
        var descriptionResult = EventDescription.Create($"Description for {title}");
        if (!descriptionResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create EventDescription: {descriptionResult.Error}");

        // Create Address
        var addressResult = Address.Create(
            "123 Test Street",
            city,
            state,
            "12345",
            "Sri Lanka");
        if (!addressResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create Address: {addressResult.Error}");

        // Create GeoCoordinate
        var coordinatesResult = GeoCoordinate.Create(latitude, longitude);
        if (!coordinatesResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create GeoCoordinate: {coordinatesResult.Error}");

        // Create EventLocation
        var locationResult = EventLocation.Create(addressResult.Value, coordinatesResult.Value);
        if (!locationResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create EventLocation: {locationResult.Error}");

        // Create Event
        var eventStartDate = startDate ?? DateTime.UtcNow.AddDays(7);
        var eventEndDate = eventStartDate.AddHours(2);

        var eventResult = Event.Create(
            titleResult.Value,
            descriptionResult.Value,
            eventStartDate,
            eventEndDate,
            Guid.NewGuid(), // organizerId
            100,
            locationResult.Value);

        if (!eventResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create Event: {eventResult.Error}");

        var @event = eventResult.Value;

        // Set status to Published if requested
        if (status == EventStatus.Published)
        {
            @event.Publish();
        }

        return Task.FromResult(@event);
    }

    private Task<Event> CreateTestEventWithoutLocationAsync(
        string title,
        EventStatus status = EventStatus.Published,
        DateTime? startDate = null)
    {
        // Create EventTitle
        var titleResult = EventTitle.Create(title);
        if (!titleResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create EventTitle: {titleResult.Error}");

        // Create EventDescription
        var descriptionResult = EventDescription.Create($"Description for {title}");
        if (!descriptionResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create EventDescription: {descriptionResult.Error}");

        // Create Event without location
        var eventStartDate = startDate ?? DateTime.UtcNow.AddDays(7);
        var eventEndDate = eventStartDate.AddHours(2);

        var eventResult = Event.Create(
            titleResult.Value,
            descriptionResult.Value,
            eventStartDate,
            eventEndDate,
            Guid.NewGuid(), // organizerId
            100,
            location: null);  // No location

        if (!eventResult.IsSuccess)
            throw new InvalidOperationException($"Failed to create Event: {eventResult.Error}");

        var @event = eventResult.Value;

        // Set status to Published if requested
        if (status == EventStatus.Published)
        {
            @event.Publish();
        }

        return Task.FromResult(@event);
    }
}
