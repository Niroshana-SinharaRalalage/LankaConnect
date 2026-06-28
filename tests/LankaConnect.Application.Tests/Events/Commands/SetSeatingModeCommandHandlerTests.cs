using FluentAssertions;
using LankaConnect.Application.Events.Commands.SetSeatingMode;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Seating Redesign Slice 1: TDD tests for SetSeatingModeCommandHandler.
/// Covers happy path, domain validation, event-not-found, and persistence behavior.
/// </summary>
public class SetSeatingModeCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<SetSeatingModeCommandHandler>> _mockLogger;
    private readonly SetSeatingModeCommandHandler _handler;

    public SetSeatingModeCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<SetSeatingModeCommandHandler>>();

        _handler = new SetSeatingModeCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    private static Event CreateTestEvent(bool withTieredTicketing)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("1 Test St", "Colombo", "WP", "00100", "Sri Lanka").Value;
        var location = EventLocation.Create(address).Value;
        var ticketPrice = Money.Create(0m, Currency.USD).Value;

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100,
            location,
            ticketPrice: ticketPrice
        ).Value;

        if (withTieredTicketing)
        {
            @event.SetTicketingMode(TicketingMode.Tiered);
        }

        return @event;
    }

    [Fact]
    public async Task Handle_TieredEvent_SwitchingToAssignedSeating_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent(withTieredTicketing: true);
        var command = new SetSeatingModeCommand(@event.Id, SeatingMode.AssignedSeating);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.SeatingMode.Should().Be(SeatingMode.AssignedSeating);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonTieredEvent_SwitchingToAssignedSeating_ShouldFailDomainValidation()
    {
        // Arrange — Tiered ticketing is a prerequisite for AssignedSeating.
        var @event = CreateTestEvent(withTieredTicketing: false);
        var command = new SetSeatingModeCommand(@event.Id, SeatingMode.AssignedSeating);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("tiered ticketing");
        @event.SeatingMode.Should().Be(SeatingMode.GeneralAdmission);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SwitchingBackToGeneralAdmission_ShouldSucceedAndClearLayout()
    {
        // Arrange
        var @event = CreateTestEvent(withTieredTicketing: true);
        @event.SetSeatingMode(SeatingMode.AssignedSeating);
        @event.SeatingMode.Should().Be(SeatingMode.AssignedSeating);

        var command = new SetSeatingModeCommand(@event.Id, SeatingMode.GeneralAdmission);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.SeatingMode.Should().Be(SeatingMode.GeneralAdmission);
        @event.VenueLayoutId.Should().BeNull();
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IdempotentSetToSameMode_ShouldSucceedWithoutSideEffects()
    {
        // Arrange — event already in GeneralAdmission; command is a no-op.
        var @event = CreateTestEvent(withTieredTicketing: true);
        var command = new SetSeatingModeCommand(@event.Id, SeatingMode.GeneralAdmission);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        @event.SeatingMode.Should().Be(SeatingMode.GeneralAdmission);
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new SetSeatingModeCommand(Guid.NewGuid(), SeatingMode.AssignedSeating);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Event not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ShouldPropagateException()
    {
        // Arrange — verify try/catch logs but does NOT swallow exceptions.
        var command = new SetSeatingModeCommand(Guid.NewGuid(), SeatingMode.AssignedSeating);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB boom"));

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB boom");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
