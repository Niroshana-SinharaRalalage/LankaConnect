using LankaConnect.Modules.Identity.Contracts;
using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Domain.Communications;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 7C.1: TDD tests for CreateEventCommandHandler — LocationName + SecondaryLocation.
/// </summary>
public class CreateEventSecondaryLocationTests
{
    private readonly Mock<IEventRepository> _mockEventRepository = new();
    private readonly Mock<IIdentityQueries> _mockIdentityQueries = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IEmailGroupQueries> _mockEmailGroupRepository = new();
    private readonly Mock<IApplicationDbContext> _mockDbContext = new();
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService = new();
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService = new();
    private readonly Mock<ILogger<CreateEventCommandHandler>> _mockLogger = new();

    private CreateEventCommandHandler CreateHandler()
        => new(
            _mockEventRepository.Object,
            _mockIdentityQueries.Object,
            _mockUnitOfWork.Object,
            _mockEmailGroupRepository.Object,
            _mockDbContext.Object,
            _mockRevenueCalculatorService.Object,
            _mockTimeZoneLookupService.Object,
            _mockLogger.Object);

    private UserSummaryDto CreateOrganizerUser(Guid userId)
    {
        return new UserSummaryDto(
            Id: userId,
            Email: "organizer@test.com",
            FirstName: "Test",
            LastName: "Organizer",
            DisplayName: "Test Organizer",
            Role: UserRoleDto.EventOrganizer,
            Status: UserStatusDto.Active,
            EmailVerified: true,
            CreatedAt: System.DateTime.UtcNow,
            UpdatedAt: null);
    }

    private void SetupStandardMocks(Guid organizerId)
    {
        _mockIdentityQueries
            .Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrganizerUser(organizerId));

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
    public async Task Handle_WithLocationName_PersistsNameOnEventLocation()
    {
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Named Venue Event",
            Description: "Event at a named venue",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Main St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            LocationName: "Park Community Hall"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Location.Should().NotBeNull();
        capturedEvent.Location!.Name.Should().Be("Park Community Hall");
    }

    [Fact]
    public async Task Handle_WithSecondaryParkingLot_PersistsSecondaryLocation()
    {
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Event With Parking",
            Description: "Primary venue plus parking lot",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Main St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            LocationName: "Park Community Hall",
            SecondaryLocationType: SecondaryLocationType.ParkingLot,
            SecondaryLocationName: "North Lot",
            SecondaryLocationAddress: "500 Side St",
            SecondaryLocationCity: "Columbus",
            SecondaryLocationState: "OH",
            SecondaryLocationZipCode: "43215",
            SecondaryLocationCountry: "USA"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.SecondaryLocation.Should().NotBeNull();
        capturedEvent.SecondaryLocation!.Type.Should().Be(SecondaryLocationType.ParkingLot);
        capturedEvent.SecondaryLocation.Location.Name.Should().Be("North Lot");
        capturedEvent.SecondaryLocation.Location.Address.Street.Should().Be("500 Side St");
        capturedEvent.SecondaryLocation.Location.Address.City.Should().Be("Columbus");
    }

    [Fact]
    public async Task Handle_WithSecondaryVenue_PersistsSecondaryLocation()
    {
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Multi-Venue Event",
            Description: "Primary plus overflow venue",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Main St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            SecondaryLocationType: SecondaryLocationType.SecondaryVenue,
            SecondaryLocationName: "Overflow Hall",
            SecondaryLocationAddress: "222 Annex Ave",
            SecondaryLocationCity: "Columbus",
            SecondaryLocationState: "OH",
            SecondaryLocationZipCode: "43215",
            SecondaryLocationCountry: "USA"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.SecondaryLocation.Should().NotBeNull();
        capturedEvent.SecondaryLocation!.Type.Should().Be(SecondaryLocationType.SecondaryVenue);
        capturedEvent.SecondaryLocation.Location.Name.Should().Be("Overflow Hall");
    }

    [Fact]
    public async Task Handle_WithSecondaryTypeButMissingAddress_ReturnsValidationFailure()
    {
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        var command = new CreateEventCommand(
            Title: "Invalid Secondary",
            Description: "Type without address",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Main St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA",
            SecondaryLocationType: SecondaryLocationType.ParkingLot
            // No secondary address/city -> should fail
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Secondary location");
        _mockEventRepository.Verify(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutSecondaryType_DoesNotPersistSecondaryLocation()
    {
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "No Secondary",
            Description: "Primary only",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Main St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA"
        );

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent!.SecondaryLocation.Should().BeNull();
        capturedEvent.HasSecondaryLocation().Should().BeFalse();
    }
}
