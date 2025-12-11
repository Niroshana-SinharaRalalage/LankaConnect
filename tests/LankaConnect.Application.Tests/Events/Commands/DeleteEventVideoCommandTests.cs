using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.DeleteEventVideo;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for DeleteEventVideo command (Epic 2 - Video Support)
/// Tests application layer handling of video deletion
/// </summary>
public class DeleteEventVideoCommandTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly DeleteEventVideoCommandHandler _handler;

    public DeleteEventVideoCommandTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new DeleteEventVideoCommandHandler(
            _mockEventRepository.Object,
            _mockUnitOfWork.Object);
    }

    private Event CreateTestEvent()
    {
        var title = EventTitle.Create("Test Event").Value;
        var description = EventDescription.Create("Test Description").Value;
        var startDate = DateTime.UtcNow.AddDays(7);
        var endDate = startDate.AddHours(2);

        return Event.Create(
            title,
            description,
            startDate,
            endDate,
            Guid.NewGuid(),
            100
        ).Value;
    }

    #region Success Cases

    [Fact]
    public async Task Handle_WithValidVideoId_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L).Value;

        var command = new DeleteEventVideoCommand
        {
            EventId = @event.Id,
            VideoId = video.Id
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Videos.Should().BeEmpty();
        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleVideos_ShouldRemoveCorrectOne()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video1 = @event.AddVideo("https://blob.azure.com/video1.mp4", "video1.mp4",
            "https://blob.azure.com/thumb1.jpg", "thumb1.jpg", TimeSpan.FromMinutes(3), "mp4", 1024000L).Value;
        var video2 = @event.AddVideo("https://blob.azure.com/video2.mp4", "video2.mp4",
            "https://blob.azure.com/thumb2.jpg", "thumb2.jpg", TimeSpan.FromMinutes(4), "mp4", 2048000L).Value;
        var video3 = @event.AddVideo("https://blob.azure.com/video3.mp4", "video3.mp4",
            "https://blob.azure.com/thumb3.jpg", "thumb3.jpg", TimeSpan.FromMinutes(5), "mp4", 3072000L).Value;

        var command = new DeleteEventVideoCommand
        {
            EventId = @event.Id,
            VideoId = video2.Id // Remove middle video
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.Videos.Should().HaveCount(2);
        @event.Videos.Should().NotContain(v => v.Id == video2.Id);
        @event.Videos.Should().Contain(v => v.Id == video1.Id);
        @event.Videos.Should().Contain(v => v.Id == video3.Id);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldReturnFailure()
    {
        // Arrange
        var command = new DeleteEventVideoCommand
        {
            EventId = Guid.NewGuid(),
            VideoId = Guid.NewGuid()
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event");
        result.Error.Should().Contain("not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentVideo_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.AddVideo("https://blob.azure.com/video.mp4", "video.mp4",
            "https://blob.azure.com/thumb.jpg", "thumb.jpg", TimeSpan.FromMinutes(5), "mp4", 1024000L);

        var nonExistentVideoId = Guid.NewGuid();

        var command = new DeleteEventVideoCommand
        {
            EventId = @event.Id,
            VideoId = nonExistentVideoId
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Video");
        result.Error.Should().Contain("not found");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Domain Event Verification

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var @event = CreateTestEvent();
        var video = @event.AddVideo(
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L).Value;

        @event.ClearDomainEvents(); // Clear AddVideo event

        var command = new DeleteEventVideoCommand
        {
            EventId = @event.Id,
            VideoId = video.Id
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        @event.DomainEvents.Should().NotBeEmpty();
        @event.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "VideoRemovedFromEventDomainEvent");
    }

    #endregion
}
