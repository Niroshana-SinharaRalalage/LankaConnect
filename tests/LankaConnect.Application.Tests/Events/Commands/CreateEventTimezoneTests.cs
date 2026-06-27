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
/// Issue #55: TDD Tests for CreateEventCommandHandler timezone setting.
/// Tests that TimeZoneId is correctly set based on event location state.
/// </summary>
public class CreateEventTimezoneTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IIdentityQueries> _mockIdentityQueries;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEmailGroupQueries> _mockEmailGroupRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService;
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService;
    private readonly Mock<ILogger<CreateEventCommandHandler>> _mockLogger;

    public CreateEventTimezoneTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockIdentityQueries = new Mock<IIdentityQueries>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEmailGroupRepository = new Mock<IEmailGroupQueries>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockRevenueCalculatorService = new Mock<IRevenueCalculatorService>();
        _mockTimeZoneLookupService = new Mock<ITimeZoneLookupService>();
        _mockLogger = new Mock<ILogger<CreateEventCommandHandler>>();
    }

    private CreateEventCommandHandler CreateHandler()
    {
        return new CreateEventCommandHandler(
            _mockEventRepository.Object,
            _mockIdentityQueries.Object,
            _mockUnitOfWork.Object,
            _mockEmailGroupRepository.Object,
            _mockDbContext.Object,
            _mockRevenueCalculatorService.Object,
            _mockTimeZoneLookupService.Object,
            _mockLogger.Object);
    }

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

    #region Issue #55: TimeZone Setting Tests

    [Fact]
    public async Task Handle_EventWithCaliforniaLocation_ShouldSetPacificTimezone()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var user = CreateOrganizerUser(organizerId);

        _mockIdentityQueries
            .Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("CA"))
            .Returns("America/Los_Angeles");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "California Beach Party",
            Description: "A fun beach event in California",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Beach Blvd",
            LocationCity: "Los Angeles",
            LocationState: "CA",
            LocationZipCode: "90001",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TimeZoneId.Should().Be("America/Los_Angeles");

        _mockTimeZoneLookupService.Verify(x => x.GetTimeZoneFromState("CA"), Times.Once);
    }

    [Fact]
    public async Task Handle_EventWithOhioLocation_ShouldSetEasternTimezone()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var user = CreateOrganizerUser(organizerId);

        _mockIdentityQueries
            .Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("OH"))
            .Returns("America/New_York");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Ohio Community Event",
            Description: "A community gathering in Ohio",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 50,
            LocationAddress: "456 Main St",
            LocationCity: "Columbus",
            LocationState: "OH",
            LocationZipCode: "43215",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TimeZoneId.Should().Be("America/New_York");

        _mockTimeZoneLookupService.Verify(x => x.GetTimeZoneFromState("OH"), Times.Once);
    }

    [Fact]
    public async Task Handle_EventWithTexasLocation_ShouldSetCentralTimezone()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var user = CreateOrganizerUser(organizerId);

        _mockIdentityQueries
            .Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("TX"))
            .Returns("America/Chicago");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Texas BBQ Event",
            Description: "A BBQ event in Texas",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 200,
            LocationAddress: "789 Ranch Road",
            LocationCity: "Houston",
            LocationState: "TX",
            LocationZipCode: "77001",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TimeZoneId.Should().Be("America/Chicago");

        _mockTimeZoneLookupService.Verify(x => x.GetTimeZoneFromState("TX"), Times.Once);
    }

    [Fact]
    public async Task Handle_VirtualEventWithoutLocation_ShouldSetDefaultTimezone()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var user = CreateOrganizerUser(organizerId);

        _mockIdentityQueries
            .Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        // Virtual event - no location
        var command = new CreateEventCommand(
            Title: "Virtual Webinar",
            Description: "An online webinar event",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(2),
            OrganizerId: organizerId,
            Capacity: 500
            // No location fields - virtual event
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TimeZoneId.Should().Be("America/New_York", "Virtual events should use default Eastern timezone");
    }

    [Fact]
    public async Task Handle_EventWithUnknownState_ShouldSetDefaultTimezone()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var user = CreateOrganizerUser(organizerId);

        _mockIdentityQueries
            .Setup(x => x.GetUserByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Unknown state returns default timezone
        _mockTimeZoneLookupService
            .Setup(x => x.GetTimeZoneFromState("XX"))
            .Returns("America/New_York");

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Event in Unknown State",
            Description: "An event in an unknown state",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            LocationAddress: "123 Unknown St",
            LocationCity: "Unknown City",
            LocationState: "XX",  // Invalid state code
            LocationZipCode: "00000",
            LocationCountry: "USA"
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TimeZoneId.Should().Be("America/New_York", "Unknown states should use default Eastern timezone");
    }

    #endregion
}
