using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.AddVideoToEvent;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Commands;

/// <summary>
/// TDD Tests for AddVideoToEvent command (Epic 2 - Video Support)
/// Tests application layer handling of video uploads
/// </summary>
public class AddVideoToEventCommandTests
{
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<IImageService> _mockImageService; // For thumbnail uploads
    private readonly Mock<IAzureBlobStorageService> _mockBlobStorageService; // For video uploads
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly AddVideoToEventCommandHandler _handler;

    public AddVideoToEventCommandTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockImageService = new Mock<IImageService>();
        _mockBlobStorageService = new Mock<IAzureBlobStorageService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new AddVideoToEventCommandHandler(
            _mockEventRepository.Object,
            _mockImageService.Object,
            _mockBlobStorageService.Object,
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
    public async Task Handle_WithValidVideo_ShouldSucceed()
    {
        // Arrange
        var @event = CreateTestEvent();
        var command = new AddVideoToEventCommand
        {
            EventId = @event.Id,
            VideoData = new byte[] { 1, 2, 3, 4 },
            VideoFileName = "video.mp4",
            ThumbnailData = new byte[] { 5, 6, 7, 8 },
            ThumbnailFileName = "thumb.jpg",
            Duration = TimeSpan.FromMinutes(5),
            Format = "mp4"
        };

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Mock video upload (direct to blob storage)
        _mockBlobStorageService
            .Setup(x => x.UploadFileAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(("video.mp4", "https://blob.azure.com/video.mp4"));

        // Mock thumbnail upload
        _mockImageService
            .Setup(x => x.ValidateImage(command.ThumbnailData, command.ThumbnailFileName))
            .Returns(Result.Success());

        _mockImageService
            .Setup(x => x.UploadImageAsync(command.ThumbnailData, command.ThumbnailFileName, command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/thumb.jpg",
                BlobName = "thumb.jpg",
                SizeBytes = 50000,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        _mockUnitOfWork
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VideoUrl.Should().Be("https://blob.azure.com/video.mp4");
        result.Value.ThumbnailUrl.Should().Be("https://blob.azure.com/thumb.jpg");
        result.Value.Duration.Should().Be(TimeSpan.FromMinutes(5));

        _mockEventRepository.Verify(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _mockBlobStorageService.Verify(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once); // Video upload
        _mockImageService.Verify(x => x.UploadImageAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once); // Thumbnail only
        _mockUnitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public async Task Handle_WithEmptyVideo_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddVideoToEventCommand
        {
            EventId = Guid.NewGuid(),
            VideoData = Array.Empty<byte>(), // Empty video data
            VideoFileName = "video.mp4",
            ThumbnailData = new byte[] { 3, 4 },
            ThumbnailFileName = "thumb.jpg"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Video file cannot be empty");
        _mockEventRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentEvent_ShouldReturnFailure()
    {
        // Arrange
        var command = new AddVideoToEventCommand
        {
            EventId = Guid.NewGuid(),
            VideoData = new byte[] { 1, 2, 3, 4 },
            VideoFileName = "video.mp4",
            ThumbnailData = new byte[] { 5, 6 },
            ThumbnailFileName = "thumb.jpg"
        };

        // Mock thumbnail validation
        _mockImageService
            .Setup(x => x.ValidateImage(command.ThumbnailData, command.ThumbnailFileName))
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
    public async Task Handle_WhenMaxVideosReached_ShouldReturnFailure()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Add 3 videos (max limit)
        @event.AddVideo("https://blob.azure.com/v1.mp4", "v1.mp4", "https://blob.azure.com/t1.jpg", "t1.jpg",
            TimeSpan.FromMinutes(3), "mp4", 1024000L);
        @event.AddVideo("https://blob.azure.com/v2.mp4", "v2.mp4", "https://blob.azure.com/t2.jpg", "t2.jpg",
            TimeSpan.FromMinutes(4), "mp4", 2048000L);
        @event.AddVideo("https://blob.azure.com/v3.mp4", "v3.mp4", "https://blob.azure.com/t3.jpg", "t3.jpg",
            TimeSpan.FromMinutes(5), "mp4", 3072000L);

        var command = new AddVideoToEventCommand
        {
            EventId = @event.Id,
            VideoData = new byte[] { 1, 2, 3, 4 },
            VideoFileName = "video4.mp4",
            ThumbnailData = new byte[] { 5, 6 },
            ThumbnailFileName = "thumb4.jpg",
            Duration = TimeSpan.FromMinutes(2),
            Format = "mp4"
        };

        // Mock thumbnail validation
        _mockImageService
            .Setup(x => x.ValidateImage(command.ThumbnailData, command.ThumbnailFileName))
            .Returns(Result.Success());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Mock video upload
        _mockBlobStorageService
            .Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("video4.mp4", "https://blob.azure.com/video4.mp4"));

        // Mock thumbnail upload
        _mockImageService
            .Setup(x => x.UploadImageAsync(command.ThumbnailData, command.ThumbnailFileName, command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/thumb4.jpg",
                BlobName = "thumb4.jpg",
                SizeBytes = 50000,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("3");
        result.Error.Should().Contain("maximum");
    }

    #endregion

    #region Rollback Scenarios

    [Fact]
    public async Task Handle_WhenDomainOperationFails_ShouldRollbackUploads()
    {
        // Arrange
        var @event = CreateTestEvent();

        // Add 3 videos to trigger max limit
        @event.AddVideo("https://blob.azure.com/v1.mp4", "v1.mp4", "https://blob.azure.com/t1.jpg", "t1.jpg",
            TimeSpan.FromMinutes(3), "mp4", 1024000L);
        @event.AddVideo("https://blob.azure.com/v2.mp4", "v2.mp4", "https://blob.azure.com/t2.jpg", "t2.jpg",
            TimeSpan.FromMinutes(4), "mp4", 2048000L);
        @event.AddVideo("https://blob.azure.com/v3.mp4", "v3.mp4", "https://blob.azure.com/t3.jpg", "t3.jpg",
            TimeSpan.FromMinutes(5), "mp4", 3072000L);

        var command = new AddVideoToEventCommand
        {
            EventId = @event.Id,
            VideoData = new byte[] { 1, 2, 3, 4 },
            VideoFileName = "video.mp4",
            ThumbnailData = new byte[] { 5, 6 },
            ThumbnailFileName = "thumb.jpg",
            Duration = TimeSpan.FromMinutes(2),
            Format = "mp4"
        };

        // Mock thumbnail validation
        _mockImageService
            .Setup(x => x.ValidateImage(command.ThumbnailData, command.ThumbnailFileName))
            .Returns(Result.Success());

        _mockEventRepository
            .Setup(x => x.GetByIdAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        // Mock video upload
        _mockBlobStorageService
            .Setup(x => x.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("video.mp4", "https://blob.azure.com/video.mp4"));

        // Mock thumbnail upload
        _mockImageService
            .Setup(x => x.UploadImageAsync(command.ThumbnailData, command.ThumbnailFileName, command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ImageUploadResult>.Success(new ImageUploadResult
            {
                Url = "https://blob.azure.com/thumb.jpg",
                BlobName = "thumb.jpg",
                SizeBytes = 50000,
                ContentType = "image/jpeg",
                UploadedAt = DateTime.UtcNow
            }));

        // Mock deletions for rollback
        _mockBlobStorageService
            .Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockImageService
            .Setup(x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        // Verify both uploaded files are deleted (rollback)
        _mockBlobStorageService.Verify(x => x.DeleteFileAsync("video.mp4", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once); // Video
        _mockImageService.Verify(x => x.DeleteImageAsync("https://blob.azure.com/thumb.jpg", It.IsAny<CancellationToken>()), Times.Once); // Thumbnail
    }

    #endregion
}
