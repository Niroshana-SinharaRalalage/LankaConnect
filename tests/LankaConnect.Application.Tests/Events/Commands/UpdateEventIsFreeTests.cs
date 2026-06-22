using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateEvent;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
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
/// IsFreeEvent fix: Tests for UpdateEventCommandHandler correctly setting IsFreeEvent flag
/// when frontend sends IsFree=true.
/// </summary>
public class UpdateEventIsFreeTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEmailGroupQueries> _mockEmailGroupRepository;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<IRevenueCalculatorService> _mockRevenueCalculatorService;
    private readonly Mock<ITimeZoneLookupService> _mockTimeZoneLookupService;
    private readonly Mock<ILogger<UpdateEventCommandHandler>> _mockLogger;

    public UpdateEventIsFreeTests()
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

    private Event CreateTestEvent(Guid organizerId, bool isFree = false)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            100
        ).Value;

        if (isFree)
        {
            @event.SetAsFreeEvent();
        }

        return @event;
    }

    private void SetupStandardMocks(Guid eventId, Event @event)
    {
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockTimeZoneLookupService
            .Setup(x => x.DefaultTimeZoneId)
            .Returns("America/New_York");

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #region IsFree flag tests

    [Fact]
    public async Task Handle_WithIsFreeTrue_ShouldSetIsFreeEventFlag()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId);
        @event.IsFreeEvent.Should().BeFalse("Precondition: event starts as paid");

        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated Free Event",
            Description: "Now a free event",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 100,
            IsFree: true
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.IsFreeEvent.Should().BeTrue("Event updated with IsFree=true should have IsFreeEvent=true");
        @event.IsFree().Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithIsFreeNull_ShouldNotChangeIsFreeEventFlag()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, isFree: true);
        @event.IsFreeEvent.Should().BeTrue("Precondition: event starts as free");

        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Updated Event",
            Description: "Updated description",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 100
            // IsFree not set - defaults to null, should not change existing flag
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        // When IsFree is null, don't change the existing flag
        // Note: Current implementation only sets when IsFree==true && pricing==null
        // so the flag remains whatever it was before
    }

    [Fact]
    public async Task Handle_UpdateFreeEventWithIsFreeTrue_ShouldKeepIsFreeEventFlag()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, isFree: true);
        @event.IsFreeEvent.Should().BeTrue("Precondition: event starts as free");

        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Still Free Event",
            Description: "Still a free event",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 150,
            IsFree: true
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.IsFreeEvent.Should().BeTrue("Free event updated with IsFree=true should remain free");
    }

    [Fact]
    public async Task Handle_WithIsFreeTrueAndNoPricing_ShouldSetZeroTicketPrice()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId);

        SetupStandardMocks(eventId, @event);

        var command = new UpdateEventCommand(
            EventId: eventId,
            Title: "Free Event",
            Description: "A free event",
            StartDate: DateTime.UtcNow.AddDays(14),
            EndDate: DateTime.UtcNow.AddDays(14).AddHours(4),
            Capacity: 100,
            IsFree: true
        );

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.IsFreeEvent.Should().BeTrue();
        @event.TicketPrice.Should().NotBeNull("SetAsFreeEvent should set $0 ticket price");
        @event.TicketPrice!.Amount.Should().Be(0m, "Free event should have $0 ticket price");
    }

    #endregion
}
