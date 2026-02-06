using System.Linq.Expressions;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.BackgroundJobs;
using LankaConnect.Application.Events.Commands.SendEventNotification;
using LankaConnect.Application.Events.Repositories;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// Phase 6A.61: TDD Tests for SendEventNotification command
/// Following TDD red-green-refactor pattern
/// </summary>
public class SendEventNotificationCommandHandlerTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IEventNotificationHistoryRepository> _mockHistoryRepository;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly SendEventNotificationCommandHandler _handler;

    public SendEventNotificationCommandHandlerTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockHistoryRepository = new Mock<IEventNotificationHistoryRepository>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _handler = new SendEventNotificationCommandHandler(
            _mockEventRepository.Object,
            _mockCurrentUserService.Object,
            _mockHistoryRepository.Object,
            _mockBackgroundJobClient.Object,
            _mockUnitOfWork.Object,
            Mock.Of<ILogger<SendEventNotificationCommandHandler>>());
    }

    private Event CreateTestEvent(Guid organizerId, bool publish = true)
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);
        var address = Address.Create("Test Address", "Test City", "CA", "90001", "USA").Value;
        var location = EventLocation.Create(address).Value;

        var @event = Event.Create(
            title,
            description,
            startDate,
            endDate,
            organizerId,
            100,
            location
        ).Value;

        // Publish the event to change status from Draft to Published
        if (publish)
        {
            @event.Publish();
        }

        return @event;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId);
        var command = new SendEventNotificationCommand(@event.Id);

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(organizerId);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var historyResult = EventNotificationHistory.Create(@event.Id, organizerId, 0);
        _mockHistoryRepository
            .Setup(x => x.AddAsync(It.IsAny<EventNotificationHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventNotificationHistory h, CancellationToken ct) => h);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Note: Cannot mock Enqueue extension method, so we mock the underlying Create method
        _mockBackgroundJobClient
            .Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
            .Returns("job-123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
        _mockHistoryRepository.Verify(x => x.AddAsync(It.IsAny<EventNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockBackgroundJobClient.Verify(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_EventNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new SendEventNotificationCommand(Guid.NewGuid());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Event not found");
        _mockHistoryRepository.Verify(x => x.AddAsync(It.IsAny<EventNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ShouldReturnFailure()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId);
        var command = new SendEventNotificationCommand(@event.Id);

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(differentUserId);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not authorized");
        _mockHistoryRepository.Verify(x => x.AddAsync(It.IsAny<EventNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DraftEvent_ShouldReturnFailure()
    {
        // Arrange
        var organizerId = Guid.NewGuid();
        var @event = CreateTestEvent(organizerId, publish: false); // Keep as Draft

        var command = new SendEventNotificationCommand(@event.Id);

        _mockCurrentUserService
            .Setup(x => x.UserId)
            .Returns(organizerId);

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("status");
        _mockHistoryRepository.Verify(x => x.AddAsync(It.IsAny<EventNotificationHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
