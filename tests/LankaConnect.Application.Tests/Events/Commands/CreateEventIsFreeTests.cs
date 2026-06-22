using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// IsFreeEvent fix: Tests for CreateEventCommandHandler correctly setting IsFreeEvent flag
/// when frontend sends IsFree=true.
/// </summary>
public class CreateEventIsFreeTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEmailGroupQueries> _mockEmailGroupRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService;
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService;
    private readonly Mock<ILogger<CreateEventCommandHandler>> _mockLogger;

    public CreateEventIsFreeTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
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
            _mockUserRepository.Object,
            _mockUnitOfWork.Object,
            _mockEmailGroupRepository.Object,
            _mockDbContext.Object,
            _mockRevenueCalculatorService.Object,
            _mockTimeZoneLookupService.Object,
            _mockLogger.Object);
    }

    private User CreateOrganizerUser(Guid userId)
    {
        var email = Email.Create("organizer@test.com").Value;
        var user = User.Create(email, "Test", "Organizer", UserRole.EventOrganizer).Value;
        typeof(User).GetProperty("Id")?.SetValue(user, userId);
        return user;
    }

    private void SetupStandardMocks(Guid organizerId)
    {
        var user = CreateOrganizerUser(organizerId);

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Revenue calculator returns failure by default (informational only, doesn't block event creation)
        _mockRevenueCalculatorService
            .Setup(x => x.CalculateBreakdownAsync(It.IsAny<LankaConnect.Domain.Shared.ValueObjects.Money>(), It.IsAny<EventLocation?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RevenueBreakdown>.Failure("Not configured for testing"));
    }

    #region IsFree flag tests

    [Fact]
    public async Task Handle_WithIsFreeTrue_ShouldSetIsFreeEventFlag()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Free Community Gathering",
            Description: "A free event for the community",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            IsFree: true
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.IsFreeEvent.Should().BeTrue("Event created with IsFree=true should have IsFreeEvent=true");
        capturedEvent.IsFree().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithIsFreeNull_ShouldDefaultIsFreeEventToFalse()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Event Without IsFree Flag",
            Description: "An event that does not send IsFree flag",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100
            // IsFree not set - defaults to null
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.IsFreeEvent.Should().BeFalse("Event created without IsFree flag should default to false (paid)");
    }

    [Fact]
    public async Task Handle_WithIsFreeFalse_ShouldNotCallSetAsFreeEvent()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Paid Event",
            Description: "A paid event with pricing",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            TicketPriceAmount: 25.00m,
            TicketPriceCurrency: Currency.USD,
            IsFree: false
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.IsFreeEvent.Should().BeFalse("Paid event should have IsFreeEvent=false");
    }

    [Fact]
    public async Task Handle_WithIsFreeTrueAndNoPricing_ShouldSetZeroTicketPrice()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        SetupStandardMocks(organizerId);

        Event? capturedEvent = null;
        _mockEventRepository
            .Setup(x => x.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var command = new CreateEventCommand(
            Title: "Free Event",
            Description: "A free event",
            StartDate: DateTime.UtcNow.AddDays(7),
            EndDate: DateTime.UtcNow.AddDays(7).AddHours(4),
            OrganizerId: organizerId,
            Capacity: 100,
            IsFree: true
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TicketPrice.Should().NotBeNull("SetAsFreeEvent should set $0 ticket price");
        capturedEvent.TicketPrice!.Amount.Should().Be(0m, "Free event should have $0 ticket price");
    }

    #endregion
}
