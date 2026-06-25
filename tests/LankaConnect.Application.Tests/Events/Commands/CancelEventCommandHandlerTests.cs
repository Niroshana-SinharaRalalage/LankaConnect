using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CancelEvent;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.92: TDD Tests for CancelEventCommandHandler
/// Tests paid event cancellation validation requiring organizer contact
/// </summary>
public class CancelEventCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CancelEventCommandHandler>> _mockLogger;
    private readonly CancelEventCommandHandler _handler;

    public CancelEventCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CancelEventCommandHandler>>();

        _handler = new CancelEventCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    private Event CreateTestEvent(Guid organizerId, bool isFree = true, bool hasOrganizerContact = false)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("Test Address", "Test City", "CA", "90001", "USA").Value;
        var location = EventLocation.Create(address).Value;

        // Create event with pricing that determines if free or paid
        var ticketPrice = isFree
            ? Money.Create(0m, Currency.USD).Value
            : Money.Create(25.00m, Currency.USD).Value;

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            100,
            location,
            ticketPrice: ticketPrice
        ).Value;

        // Publish the event so it can be cancelled
        @event.Publish();

        // Set organizer contact if required
        if (hasOrganizerContact)
        {
            @event.SetOrganizerContacts(
                publishContact: true,
                contacts: new List<(string name, string? email, string? phone)>
                {
                    ("Test Organizer", "organizer@test.com", "123-456-7890")
                });
        }

        return @event;
    }

    #region Phase 6A.92: Paid Event Cancellation Validation

    [Fact]
    public async Task Handle_PaidEventWithoutOrganizerContact_ShouldReturnFailure()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, isFree: false, hasOrganizerContact: false);
        var command = new CancelEventCommand(@event.Id, "Test cancellation");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(
            "In order to cancel a paid event, the organizer must publish contact details in the Event Management page first.");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PaidEventWithOrganizerContact_ShouldSucceed()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, isFree: false, hasOrganizerContact: true);
        var command = new CancelEventCommand(@event.Id, "Test cancellation");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FreeEventWithoutOrganizerContact_ShouldSucceed()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, isFree: true, hasOrganizerContact: false);
        var command = new CancelEventCommand(@event.Id, "Test cancellation");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Standard Failure Cases

    [Fact]
    public async Task Handle_EventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelEventCommand(Guid.NewGuid(), "Test cancellation");

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Event not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
