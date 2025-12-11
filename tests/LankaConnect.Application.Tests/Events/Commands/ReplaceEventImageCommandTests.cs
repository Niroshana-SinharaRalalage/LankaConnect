using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.ReplaceEventImage;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for ReplaceEventImage command (Epic 2 - Image Replace Feature)
/// Tests the application layer handling of image replacement
/// </summary>
public class ReplaceEventImageCommandTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IImageService> _mockImageService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ReplaceEventImageCommandHandler _handler;

    public ReplaceEventImageCommandTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockImageService = new Mock<IImageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new ReplaceEventImageCommandHandler(
            _mockEventRepository.Object,
            _mockImageService.Object,
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
    public async Task Handle_WithValidRequest_ShouldReplaceImageSuccessfully()
    {
        // Arrange
        var @event = CreateTestEvent();
        var originalImage = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;
        var imageId = originalImage.Id;

        var command = new ReplaceEventImageCommand
        {
            EventId = @event.Id,
            ImageId = imageId,
            ImageData = new byte[] { 1, 2, 3, 4 },
            FileName = "new-image.jpg"
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockImageService
            .Setup(x => x.ValidateImage(command.ImageData, command.FileName))
            .Returns(Result.Success());

        _mockImageService
            .Setup(x => x.UploadImageAsync(command.ImageData, command.FileName, command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/new-image.jpg",
                BlobName = "new-image.jpg",
                SizeBytes = 12345,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        _mockImageService
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImageUrl.Should().Be("https://blob.azure.com/new-image.jpg");
        result.Value.BlobName.Should().Be("new-image.jpg");
        result.Value.Id.Should().Be(imageId, "should maintain same ID");

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockImageService.Verify(x => x.ValidateImage(command.ImageData, command.FileName), Times.Once);
        _mockImageService.Verify(x => x.UploadImageAsync(command.ImageData, command.FileName, command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockImageService.Verify(x => x.DeleteImageAsync("https://blob.azure.com/original.jpg", It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReplacingImage_ShouldDeleteOldBlobAfterSuccess()
    {
        // Arrange
        var @event = CreateTestEvent();
        var originalImage = @event.AddImage("https://blob.azure.com/old.jpg", "old.jpg").Value;

        var command = new ReplaceEventImageCommand
        {
            EventId = @event.Id,
            ImageId = originalImage.Id,
            ImageData = new byte[] { 5, 6, 7, 8 },
            FileName = "new.jpg"
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockImageService
            .Setup(x => x.ValidateImage(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Result.Success());

        _mockImageService
            .Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/new.jpg",
                BlobName = "new.jpg",
                SizeBytes = 12345,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        _mockImageService
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockImageService.Verify(x => x.DeleteImageAsync("https://blob.azure.com/old.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithInvalidImageData_ShouldReturnFailure()
    {
        // Arrange
        var command = new ReplaceEventImageCommand
        {
            EventId = Guid.NewGuid(),
            ImageId = Guid.NewGuid(),
            ImageData = new byte[] { 1, 2 },
            FileName = "invalid.jpg"
        };

        _mockImageService
            .Setup(x => x.ValidateImage(command.ImageData, command.FileName))
            .Returns(Result.Failure("Invalid image format"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid image format");
        _mockEventRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldReturnFailure()
    {
        // Arrange
        var command = new ReplaceEventImageCommand
        {
            EventId = Guid.NewGuid(),
            ImageId = Guid.NewGuid(),
            ImageData = new byte[] { 1, 2, 3, 4 },
            FileName = "image.jpg"
        };

        _mockImageService
            .Setup(x => x.ValidateImage(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Result.Success());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event");
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WithNonExistentImageId_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();
        var nonExistentImageId = Guid.NewGuid();

        var command = new ReplaceEventImageCommand
        {
            EventId = @event.Id,
            ImageId = nonExistentImageId,
            ImageData = new byte[] { 1, 2, 3, 4 },
            FileName = "image.jpg"
        };

        _mockImageService
            .Setup(x => x.ValidateImage(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Result.Success());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockImageService
            .Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/new.jpg",
                BlobName = "new.jpg",
                SizeBytes = 12345,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Image not found");
    }

    #endregion

    #region Rollback on Failure

    [Fact]
    public async Task Handle_WhenDomainOperationFails_ShouldRollbackUploadedBlob()
    {
        // Arrange
        var @event = CreateTestEvent();
        var invalidImageId = Guid.NewGuid(); // Non-existent image

        var command = new ReplaceEventImageCommand
        {
            EventId = @event.Id,
            ImageId = invalidImageId,
            ImageData = new byte[] { 1, 2, 3, 4 },
            FileName = "new.jpg"
        };

        _mockImageService
            .Setup(x => x.ValidateImage(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Result.Success());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockImageService
            .Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/new.jpg",
                BlobName = "new.jpg",
                SizeBytes = 12345,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        _mockImageService
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Verify rollback: newly uploaded blob should be deleted
        _mockImageService.Verify(x => x.DeleteImageAsync("https://blob.azure.com/new.jpg", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUploadFails_ShouldNotModifyEvent()
    {
        // Arrange
        var @event = CreateTestEvent();
        var originalImage = @event.AddImage("https://blob.azure.com/original.jpg", "original.jpg").Value;

        var command = new ReplaceEventImageCommand
        {
            EventId = @event.Id,
            ImageId = originalImage.Id,
            ImageData = new byte[] { 1, 2, 3, 4 },
            FileName = "new.jpg"
        };

        _mockImageService
            .Setup(x => x.ValidateImage(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Result.Success());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        _mockImageService
            .Setup(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Failure("Upload failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Upload failed");
        // Event should still have original image
        @event.Images.Should().ContainSingle();
        @event.Images.First().ImageUrl.Should().Be("https://blob.azure.com/original.jpg");
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
