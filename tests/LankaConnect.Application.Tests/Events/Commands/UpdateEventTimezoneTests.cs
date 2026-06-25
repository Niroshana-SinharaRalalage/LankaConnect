using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateEvent;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Issue #55: TDD Tests for UpdateEventCommandHandler timezone setting.
/// Tests that TimeZoneId is correctly updated when event location changes.
/// </summary>
public class UpdateEventTimezoneTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEmailGroupQueries> _mockEmailGroupRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService;
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService;
    private readonly Mock<ILogger<UpdateEventCommandHandler>> _mockLogger;

    public UpdateEventTimezoneTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEmailGroupRepository = new Mock<IEmailGroupQueries>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockRevenueCalculatorService = new Mock<IRevenueCalculatorService>();
        _mockTimeZoneLookupService = new Mock<ITimeZoneLookupService>();
        _mockLogger = new Mock<ILogger<UpdateEventCommandHandler>>();
    }

    private UpdateEventCommandHandler CreateHandler()
    {
        return new UpdateEventCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockEmailGroupRepository.Object,
            _mockDbContext.Object,
            _mockRevenueCalculatorService.Object,
            _mockTimeZoneLookupService.Object,
            _mockLogger.Object);
    }

    private Event CreateTestEvent(Guid organizerId, string? state = "OH")
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        EventLocation? location = null;
        if (!string.IsNullOrEmpty(state))
        {
            var address = Address.Create("Test Address", "Test City", state, "12345", "USA").Value;
            location = EventLocation.Create(address).Value;
        }

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            100,
            location
        ).Value;

        return @event;
    }

    #region Issue #55: TimeZone Update Tests

    [Fact]
    public async Task Handle_LocationChangedFromOhioToCalifornia_ShouldUpdateToPacificTimezone()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, "OH");

        // Set initial timezone (simulating existing event with timezone)
        @event.SetTimeZone("America/New_York");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("CA"))
            .Returns("America/Los_Angeles");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated Event Title",
            Description: "Updated description",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            LocationAddress: "456 California Ave",
            LocationCity: "San Francisco",
            LocationState: "CA",  // Changed from OH to CA
            LocationZipCode: "94102",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.TimeZoneId.Should().Be("America/Los_Angeles");

        _mockTimeZoneLookupService.Verify(x => x.GetTimeZoneFromState("CA"), Times.Once);
    }

    [Fact]
    public async Task Handle_LocationChangedFromCaliforniaToTexas_ShouldUpdateToCentralTimezone()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, "CA");

        // Set initial timezone
        @event.SetTimeZone("America/Los_Angeles");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("TX"))
            .Returns("America/Chicago");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Texas Event",
            Description: "Event moved to Texas",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 100,
            LocationAddress: "789 Texas Blvd",
            LocationCity: "Austin",
            LocationState: "TX",  // Changed from CA to TX
            LocationZipCode: "78701",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.TimeZoneId.Should().Be("America/Chicago");

        _mockTimeZoneLookupService.Verify(x => x.GetTimeZoneFromState("TX"), Times.Once);
    }

    [Fact]
    public async Task Handle_LocationRemovedBecomeVirtual_ShouldKeepExistingTimezone()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, "CA");

        // Set initial timezone
        @event.SetTimeZone("America/Los_Angeles");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Convert to virtual event (no location)
        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Virtual Event",
            Description: "Converted to virtual event",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(2),
            Capacity: 500
            // No location fields - converted to virtual
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        // Should keep existing timezone when converted to virtual
        @event.TimeZoneId.Should().Be("America/Los_Angeles");
    }

    [Fact]
    public async Task Handle_VirtualEventGetsLocation_ShouldSetTimezoneBasedOnState()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, null); // Virtual event

        // Set default timezone for virtual event
        @event.SetTimeZone("America/New_York");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("WA"))
            .Returns("America/Los_Angeles");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Add location to virtual event
        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Washington Event",
            Description: "Event now has a physical location",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 100,
            LocationAddress: "123 Seattle Way",
            LocationCity: "Seattle",
            LocationState: "WA",  // Adding location in Washington
            LocationZipCode: "98101",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.TimeZoneId.Should().Be("America/Los_Angeles");

        _mockTimeZoneLookupService.Verify(x => x.GetTimeZoneFromState("WA"), Times.Once);
    }

    #endregion
}
