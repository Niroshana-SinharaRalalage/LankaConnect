using FluentAssertions;
using LankaConnect.Domain.Events;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Domain;

/// <summary>
/// TDD Tests for EventVideo entity (Epic 2 - Video Support Feature)
/// Tests the domain entity for event videos following EventImage pattern
/// </summary>
public class EventVideoTests
{
    #region Create - Success Cases

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var videoUrl = "https://blob.azure.com/videos/event123.mp4";
        var blobName = "event123.mp4";
        var thumbnailUrl = "https://blob.azure.com/thumbnails/event123-thumb.jpg";
        var thumbnailBlobName = "event123-thumb.jpg";
        var duration = TimeSpan.FromMinutes(5);
        var format = "mp4";
        var fileSizeBytes = 1024000L;
        var displayOrder = 1;

        // Act
        var video = EventVideo.Create(
            eventId,
            videoUrl,
            blobName,
            thumbnailUrl,
            thumbnailBlobName,
            duration,
            format,
            fileSizeBytes,
            displayOrder);

        // Assert
        video.Should().NotBeNull();
        video.Id.Should().NotBe(Guid.Empty);
        video.EventId.Should().Be(eventId);
        video.VideoUrl.Should().Be(videoUrl);
        video.BlobName.Should().Be(blobName);
        video.ThumbnailUrl.Should().Be(thumbnailUrl);
        video.ThumbnailBlobName.Should().Be(thumbnailBlobName);
        video.Duration.Should().Be(duration);
        video.Format.Should().Be(format);
        video.FileSizeBytes.Should().Be(fileSizeBytes);
        video.DisplayOrder.Should().Be(displayOrder);
        video.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullDuration_ShouldSucceed()
    {
        // Arrange
        var eventId = Guid.NewGuid();

        // Act
        var video = EventVideo.Create(
            eventId,
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            null, // Duration can be null
            "mp4",
            1024000L,
            1);

        // Assert
        video.Duration.Should().BeNull();
    }

    #endregion

    #region Create - Validation Failures

    [Fact]
    public void Create_WithEmptyVideoUrl_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "", // Empty URL
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L,
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Video URL*");
    }

    [Fact]
    public void Create_WithNullVideoUrl_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            null!, // Null URL
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L,
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Video URL*");
    }

    [Fact]
    public void Create_WithEmptyBlobName_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "https://blob.azure.com/video.mp4",
            "", // Empty blob name
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L,
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Blob name*");
    }

    [Fact]
    public void Create_WithEmptyThumbnailUrl_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "", // Empty thumbnail URL
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L,
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Thumbnail URL*");
    }

    [Fact]
    public void Create_WithEmptyFormat_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "", // Empty format
            1024000L,
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Format*");
    }

    [Fact]
    public void Create_WithZeroDisplayOrder_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L,
            0); // Invalid display order

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Display order*");
    }

    [Fact]
    public void Create_WithNegativeDisplayOrder_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            1024000L,
            -1); // Invalid display order

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Display order*");
    }

    [Fact]
    public void Create_WithNegativeFileSize_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => EventVideo.Create(
            Guid.NewGuid(),
            "https://blob.azure.com/video.mp4",
            "video.mp4",
            "https://blob.azure.com/thumb.jpg",
            "thumb.jpg",
            TimeSpan.FromMinutes(5),
            "mp4",
            -1024L, // Invalid file size
            1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*File size*");
    }

    #endregion
}
