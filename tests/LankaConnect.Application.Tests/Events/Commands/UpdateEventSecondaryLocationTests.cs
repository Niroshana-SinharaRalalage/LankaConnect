using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateEvent;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Modules.Communications.Domain;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 7C.1: TDD tests for UpdateEventCommandHandler — LocationName + SecondaryLocation set/clear.
/// </summary>
public class UpdateEventSecondaryLocationTests
{
    private readonly Mock<IEventRepository> _mockEventRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IEmailGroupQueries> _mockEmailGroupRepository = new();
    private readonly Mock<IApplicationDbContext> _mockDbContext = new();
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService = new();
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService = new();
    private readonly Mock<ILogger<UpdateEventCommandHandler>> _mockLogger = new();

    private UpdateEventCommandHandler CreateHandler()
        => new(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockEmailGroupRepository.Object,
            _mockDbContext.Object,
            _mockRevenueCalculatorService.Object,
            _mockTimeZoneLookupService.Object,
            _mockLogger.Object);

    private Event CreateTestEvent(Guid organizerId, bool withSecondary = false)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        var address = Address.Create("Primary St", "Columbus", "OH", "43215", "USA").Value;
        var location = EventLocation.Create(address, name: "Old Hall").Value;

        var @event = Event.Create(title, description, startDate, endDate, organizerId, 100, location).Value;

        if (withSecondary)
        {
            var secAddress = Address.Create("Old Lot St", "Columbus", "OH", "43215", "USA").Value;
            var secInner = EventLocation.Create(secAddress, name: "Old Lot").Value;
            var secondary = EventSecondaryLocation.Create(SecondaryLocationType.ParkingLot, secInner).Value;
            @event.SetSecondaryLocation(secondary);
        }

        return @event;
    }

    private void SetupStandardMocks(Guid eventId, Event @event)
    {
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState(It.IsAny<string>()))
            .Returns("America/New_York");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_UpdatesLocationName()
    {
        var eventId = Guid.NewGuid();
        var @event = CreateTestEvent(Guid.NewGuid());
        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated",
            Description: "Updated",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            LocationAddress: "Primary St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            LocationName: "New Hall Name"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.Location!.Name.Should().Be("New Hall Name");
    }

    [Fact]
    public async Task Handle_AddsSecondaryLocation_WhenNonePreviouslySet()
    {
        var eventId = Guid.NewGuid();
        var @event = CreateTestEvent(Guid.NewGuid(), withSecondary: false);
        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated",
            Description: "Updated",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            LocationAddress: "Primary St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            SecondaryLocationType: SecondaryLocationType.SecondaryVenue,
            SecondaryLocationName: "Annex",
            SecondaryLocationAddress: "222 Annex Ave",
            SecondaryLocationCity: "Columbus",
            SecondaryLocationState: "OH",
            SecondaryLocationZipCode: "43215",
            SecondaryLocationCountry: "USA"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.SecondaryLocation.Should().NotBeNull();
        @event.SecondaryLocation!.Type.Should().Be(SecondaryLocationType.SecondaryVenue);
        @event.SecondaryLocation.Location.Name.Should().Be("Annex");
    }

    [Fact]
    public async Task Handle_ReplacesSecondaryLocation_WhenAlreadySet()
    {
        var eventId = Guid.NewGuid();
        var @event = CreateTestEvent(Guid.NewGuid(), withSecondary: true);
        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated",
            Description: "Updated",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            LocationAddress: "Primary St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            SecondaryLocationType: SecondaryLocationType.SecondaryVenue,
            SecondaryLocationName: "Overflow",
            SecondaryLocationAddress: "333 Overflow Rd",
            SecondaryLocationCity: "Columbus",
            SecondaryLocationState: "OH",
            SecondaryLocationZipCode: "43215",
            SecondaryLocationCountry: "USA"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.SecondaryLocation!.Type.Should().Be(SecondaryLocationType.SecondaryVenue);
        @event.SecondaryLocation.Location.Name.Should().Be("Overflow");
        @event.SecondaryLocation.Location.Address.Street.Should().Be("333 Overflow Rd");
    }

    [Fact]
    public async Task Handle_ClearsSecondaryLocation_WhenNoTypeProvided()
    {
        var eventId = Guid.NewGuid();
        var @event = CreateTestEvent(Guid.NewGuid(), withSecondary: true);
        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated",
            Description: "Updated",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            LocationAddress: "Primary St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA"
            // no SecondaryLocationType -> clear
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.SecondaryLocation.Should().BeNull();
        @event.HasSecondaryLocation().Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SecondaryTypeButMissingAddress_ReturnsValidationFailure()
    {
        var eventId = Guid.NewGuid();
        var @event = CreateTestEvent(Guid.NewGuid());
        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated",
            Description: "Updated",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            LocationAddress: "Primary St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            SecondaryLocationType: SecondaryLocationType.ParkingLot
            // no address / city for secondary -> should fail
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Secondary location");
    }
}
