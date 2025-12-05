using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.SetPrimaryImage;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for SetPrimaryImage command (Phase 6A.13 - Primary Image Selection)
/// Tests the application layer handling of setting an image as primary/main thumbnail
/// </summary>
public class SetPrimaryImageCommandTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly SetPrimaryImageCommandHandler _handler;

    public SetPrimaryImageCommandTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new SetPrimaryImageCommandHandler(
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
    public async Task Handle_WithValidRequest_ShouldSetImageAsPrimarySuccessfully()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image1 = @event.AddImage("https://blob.azure.com/image1.jpg", "image1.jpg").Value;
        var image2 = @event.AddImage("https://blob.azure.com/image2.jpg", "image2.jpg").Value;
        var image3 = @event.AddImage("https://blob.azure.com/image3.jpg", "image3.jpg").Value;

        var command = new SetPrimaryImageCommand
        {
            EventId = @event.Id,
            ImageId = image2.Id
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
        image2.IsPrimary.Should().BeTrue("image2 should be marked as primary");
        image1.IsPrimary.Should().BeFalse("image1 should not be primary");
        image3.IsPrimary.Should().BeFalse("image3 should not be primary");

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSettingNewPrimary_ShouldUnmarkPreviousPrimary()
    {
        // Arrange
        var @event = CreateTestEvent();
        var image1 = @event.AddImage("https://blob.azure.com/image1.jpg", "image1.jpg").Value;
        var image2 = @event.AddImage("https://blob.azure.com/image2.jpg", "image2.jpg").Value;

        // Set image1 as primary first
        @event.SetPrimaryImage(image1.Id);
        image1.IsPrimary.Should().BeTrue("image1 should be primary initially");

        var command = new SetPrimaryImageCommand
        {
            EventId = @event.Id,
            ImageId = image2.Id // Now set image2 as primary
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
        image2.IsPrimary.Should().BeTrue("image2 should be the new primary");
        image1.IsPrimary.Should().BeFalse("image1 should no longer be primary");

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSingleImage_ShouldSetItAsPrimary()
    {
        // Arrange
        var @event = CreateTestEvent();
        var singleImage = @event.AddImage("https://blob.azure.com/only-image.jpg", "only-image.jpg").Value;

        var command = new SetPrimaryImageCommand
        {
            EventId = @event.Id,
            ImageId = singleImage.Id
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
        singleImage.IsPrimary.Should().BeTrue();

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Failure Cases

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldReturnFailure()
    {
        // Arrange
        var command = new SetPrimaryImageCommand
        {
            EventId = Guid.NewGuid(),
            ImageId = Guid.NewGuid()
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event with ID");
        result.Error.Should().Contain("not found");

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentImage_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        @event.AddImage("https://blob.azure.com/image1.jpg", "image1.jpg");

        var nonExistentImageId = Guid.NewGuid();

        var command = new SetPrimaryImageCommand
        {
            EventId = @event.Id,
            ImageId = nonExistentImageId // Image doesn't belong to this event
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image with ID");
        result.Error.Should().Contain("not found in this event");

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEventWithNoImages_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        // Event has no images

        var command = new SetPrimaryImageCommand
        {
            EventId = @event.Id,
            ImageId = Guid.NewGuid()
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image with ID");
        result.Error.Should().Contain("not found in this event");

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
